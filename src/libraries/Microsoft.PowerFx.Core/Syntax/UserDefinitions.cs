// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax.SourceInformation;
using Attribute = Microsoft.PowerFx.Core.Parser.Attribute;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// This encapsulates a named formula and user-defined functions: its original script, the parsed result, and any parse errors.
    /// </summary>
    internal sealed class UserDefinitions
    {
        /// <summary>
        /// A script containing one or more UDFs.
        /// </summary>
        private readonly string _script;
        private readonly ParserOptions _parserOptions;
        private readonly Features _features;

        private UserDefinitions(string script, ParserOptions parserOptions, Features features = null)
        {
            _features = features ?? Features.None;
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _parserOptions = parserOptions;
        }

        /// <summary>
        /// Parses a script with both named formulas, user-defined functions and user-defined types.
        /// </summary>
        /// <param name="script">Script with named formulas, user-defined functions and user-defined types.</param>
        /// <param name="parserOptions">Options for parsing an expression.</param>
        /// <param name="features">Power Fx feature flags.</param>
        /// <returns><see cref="ParseUserDefinitionResult"/>.</returns>
        public static ParseUserDefinitionResult Parse(string script, ParserOptions parserOptions, Features features = null)
        {
            var parseResult = TexlParser.ParseUserDefinitionScript(script, parserOptions, features);

            if (parserOptions.AllowAttributes)
            {
                var userDefinitions = new UserDefinitions(script, parserOptions, features);
                parseResult = userDefinitions.ProcessPartialAttributes(parseResult);
            }

            return parseResult;
        }

// This code is intended as a prototype of the Partial attribute system, for use in solution layering cases
// Provides order-independent ways of merging named formulas
        #region Partial Attributes

        private static readonly string _renamedFormulaGuid = Guid.NewGuid().ToString("N");

        private enum PartialOperationKind
        {
            Error,
            And,
            Or,
            Table,
            Record
        }

        private static PartialOperationKind GetPartialOperationKind(string operationName)
        {
            switch (operationName)
            {
                case "And":
                    return PartialOperationKind.And;
                case "Or":
                    return PartialOperationKind.Or;
                case "Table":
                    return PartialOperationKind.Table;
                case "Record":
                    return PartialOperationKind.Record;
                default:
                    return PartialOperationKind.Error;
            }
        }

        private static Attribute GetPartialAttribute(IReadOnlyList<Attribute> attributes)
        {
            return attributes?.FirstOrDefault(a => a.Name.Name.Value == "Partial");
        }

        /// <summary>
        /// For NamedFormulas with partial attributes,
        /// validates that the same attribute is applied to all matching names,
        /// then applies name mangling to all, and constructs a separate
        /// formula with the operation applied and the original name.
        /// </summary>
        private ParseUserDefinitionResult ProcessPartialAttributes(ParseUserDefinitionResult parsed)
        {
            var groupedFormulas = parsed.NamedFormulas.GroupBy(nf => nf.Ident.Name.Value);
            var errors = parsed.Errors?.ToList() ?? new List<TexlError>();
            var newFormulas = new List<NamedFormula>();

            foreach (var nameGroup in groupedFormulas)
            {
                var name = nameGroup.Key;
                var firstPartialAttribute = nameGroup.Select(nf => GetPartialAttribute(nf.Attributes)).FirstOrDefault(a => a != null);

                if (firstPartialAttribute == null || nameGroup.Count() == 1)
                {
                    newFormulas.AddRange(nameGroup);
                    continue;
                }

                var firstOperationName = firstPartialAttribute.Arguments.Count > 0 ? firstPartialAttribute.Arguments[0] : null;
                var firstOperation = firstOperationName != null ? GetPartialOperationKind(firstOperationName) : PartialOperationKind.Error;

                var updatedGroupFormulas = new List<NamedFormula>();
                var id = 0;
                foreach (var formula in nameGroup)
                {
                    var partialAttribute = GetPartialAttribute(formula.Attributes);

                    if (partialAttribute == null || partialAttribute.Name.Name.Value != "Partial")
                    {
                        errors.Add(new TexlError(formula.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrOnlyPartialAttribute));
                        continue;
                    }

                    var operationName = partialAttribute.Arguments.Count > 0 ? partialAttribute.Arguments[0] : null;
                    var operation = operationName != null ? GetPartialOperationKind(operationName) : PartialOperationKind.Error;

                    if (operation != firstOperation || operationName != firstOperationName)
                    {
                        errors.Add(new TexlError(partialAttribute.Name, DocumentErrorSeverity.Severe, TexlStrings.ErrOperationDoesntMatch));
                        continue;
                    }

                    var newName = new IdentToken(name + _renamedFormulaGuid + id, formula.Ident.Span, isNonSourceIdentToken: true);
                    id++;
                    updatedGroupFormulas.Add(new NamedFormula(newName, formula.Formula, formula.StartingIndex, formula.ColonEqual, formula.Attributes));
                }

                if (firstOperation == PartialOperationKind.Error)
                {
                    var errorToken = firstPartialAttribute.Arguments.Count > 0 ? firstPartialAttribute.ArgumentTokens[0] : (Token)firstPartialAttribute.Name;
                    errors.Add(new TexlError(errorToken, DocumentErrorSeverity.Severe, TexlStrings.ErrUnknownPartialOp));

                    // None of the "namemangled" formulas are valid at this point, even if they all matched, as we're not using a valid partial operation.
                    updatedGroupFormulas.Clear();
                }

                if (updatedGroupFormulas.Count != nameGroup.Count())
                {
                    // Not all matched, don't use renamed formulas
                    newFormulas.AddRange(nameGroup);
                    continue;
                }

                newFormulas.AddRange(updatedGroupFormulas);
                newFormulas.Add(
                    new NamedFormula(
                        new IdentToken(name, firstPartialAttribute.Name.Span, isNonSourceIdentToken: true),
                        GetPartialCombinedFormula(name, firstOperation, updatedGroupFormulas),
                        0,
                        colonEqual: true,
                        nameGroup.First().Attributes));
            }

            return new ParseUserDefinitionResult(newFormulas, parsed.UDFs, parsed.DefinedTypes, errors, parsed.Comments, parsed.UserDefinitionSourceInfos, parsed.DefinitionsLikely);
        }

        private Formula GetPartialCombinedFormula(string name, PartialOperationKind operationKind, IList<NamedFormula> formulas)
        {
            return operationKind switch
            {
                PartialOperationKind.And => GeneratePartialFunction("And", name, formulas),
                PartialOperationKind.Or => GeneratePartialFunction("Or", name, formulas),
                PartialOperationKind.Table => GeneratePartialFunction("Table", name, formulas),
                PartialOperationKind.Record => GeneratePartialFunction("MergeRecords", name, formulas),
                _ => throw new InvalidOperationException("Unknown partial op while generating merged NF")
            };
        }

        private Formula GeneratePartialFunction(string functionName, string name, IList<NamedFormula> formulas)
        {
            var listSeparator = TexlLexer.GetLocalizedInstance(_parserOptions.Culture).LocalizedPunctuatorListSeparator;

            // We're going to construct these texlnodes by hand so the spans match up with real code locations
            var script = $"{functionName}({string.Join($"{listSeparator} ", Enumerable.Range(0, formulas.Count).Select(i => name + _renamedFormulaGuid + i))})";

            var arguments = new List<TexlNode>();
            var id = 0;
            foreach (var nf in formulas)
            {
                arguments.Add(new FirstNameNode(ref id, nf.Ident, new Identifier(nf.Ident)));
            }

            var firstPartialAttribute = GetPartialAttribute(formulas.First().Attributes);
            var firstToken = (Token)firstPartialAttribute.Name;

            var functionCall = new CallNode(
                ref id,
                firstToken,
                new SourceList(firstToken),
                new Identifier(new IdentToken(functionName, firstToken.Span, true)),
                headNode: null,
                args: new ListNode(ref id, tok: firstToken, args: arguments.ToArray(), delimiters: null, sourceList: new SourceList(firstToken)),
                tokParenClose: firstToken);

            return new Formula(script, functionCall);
        }
        #endregion
    }
}
