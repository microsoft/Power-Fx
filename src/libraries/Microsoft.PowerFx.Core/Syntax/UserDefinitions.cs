// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// This encapsulates a named formula and user defined functions: its original script, the parsed result, and any parse errors.
    /// </summary>
    internal sealed class UserDefinitions
    {
        /// <summary>
        /// A script containing one or more UDFs.
        /// </summary>
        private readonly string _script;
        private readonly ParserOptions _parserOptions;
        private readonly Features _features;

        // Exposing it so hosts can filter out the intellisense suggestions
        public static readonly ISet<DType> RestrictedTypes = new HashSet<DType> { DType.DateTimeNoTimeZone, DType.ObjNull, DType.Decimal, DType.Hyperlink };

        private UserDefinitions(string script, ParserOptions parserOptions, Features features = null)
        {
            _features = features ?? Features.None;
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _parserOptions = parserOptions;
        }

        /// <summary>
        /// Parses a script with both named formulas, user defined functions and user defined types.
        /// </summary>
        /// <param name="script">Script with named formulas, user defined functions and user defined types.</param>
        /// <param name="parserOptions">Options for parsing an expression.</param>
        /// <returns><see cref="ParseUserDefinitionResult"/>.</returns>
        public static ParseUserDefinitionResult Parse(string script, ParserOptions parserOptions)
        {
            var parseResult = TexlParser.ParseUserDefinitionScript(script, parserOptions);

            if (parserOptions.AllowAttributes)
            {
                var userDefinitions = new UserDefinitions(script, parserOptions);
                parseResult = userDefinitions.ProcessPartialAttributes(parseResult);
            }

            return parseResult;
        }

// This code is intended as a prototype of the Partial attribute system, for use in solution layering cases
// Provides order-independent ways of merging named formulas
        #region Partial Attributes

        private static readonly string _renamedFormulaGuid = Guid.NewGuid().ToString("N");

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
                var firstAttribute = nameGroup.Select(nf => nf.Attribute).FirstOrDefault(att => att != null);

                if (firstAttribute == null || nameGroup.Count() == 1)
                {
                    newFormulas.AddRange(nameGroup);
                    continue;
                }

                var updatedGroupFormulas = new List<NamedFormula>();
                var id = 0;
                foreach (var formula in nameGroup)
                {
                    // This is just for the prototype, since we only have the one kind.
                    if (formula.Attribute.AttributeName.Name != "Partial")
                    {
                        errors.Add(new TexlError(formula.Attribute.AttributeOperationToken, DocumentErrorSeverity.Severe, TexlStrings.ErrOnlyPartialAttribute));
                        continue;
                    }

                    if (!firstAttribute.SameAttribute(formula.Attribute))
                    {
                        errors.Add(new TexlError(formula.Attribute.AttributeOperationToken, DocumentErrorSeverity.Severe, TexlStrings.ErrOperationDoesntMatch));
                        continue;
                    }

                    var newName = new IdentToken(name + _renamedFormulaGuid + id, formula.Ident.Span, isNonSourceIdentToken: true);
                    id++;
                    updatedGroupFormulas.Add(new NamedFormula(newName, formula.Formula, formula.StartingIndex, formula.Attribute));
                }

                if (firstAttribute.AttributeOperation == PartialAttribute.AttributeOperationKind.Error)
                {
                    errors.Add(new TexlError(firstAttribute.AttributeOperationToken, DocumentErrorSeverity.Severe, TexlStrings.ErrUnknownPartialOp));

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
                        new IdentToken(name, firstAttribute.AttributeName.Span, isNonSourceIdentToken: true),
                        GetPartialCombinedFormula(name, firstAttribute.AttributeOperation, updatedGroupFormulas),
                        0,
                        firstAttribute));
            }

            return new ParseUserDefinitionResult(newFormulas, parsed.UDFs, parsed.DefinedTypes, errors, parsed.Comments);
        }

        private Formula GetPartialCombinedFormula(string name, PartialAttribute.AttributeOperationKind operationKind, IList<NamedFormula> formulas)
        {
            return operationKind switch
            {
                PartialAttribute.AttributeOperationKind.PartialAnd => GeneratePartialFunction("And", name, formulas),
                PartialAttribute.AttributeOperationKind.PartialOr => GeneratePartialFunction("Or", name, formulas),
                PartialAttribute.AttributeOperationKind.PartialTable => GeneratePartialFunction("Table", name, formulas),
                PartialAttribute.AttributeOperationKind.PartialRecord => GeneratePartialFunction("MergeRecords", name, formulas),
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

            var firstAttributeOpToken = formulas.First().Attribute.AttributeOperationToken;

            var functionCall = new CallNode(
                ref id,
                firstAttributeOpToken,
                new SourceList(firstAttributeOpToken),
                new Identifier(new IdentToken(functionName, firstAttributeOpToken.Span, true)),
                headNode: null,
                args: new ListNode(ref id, tok: firstAttributeOpToken, args: arguments.ToArray(), delimiters: null, sourceList: new SourceList(firstAttributeOpToken)),
                tokParenClose: firstAttributeOpToken);

            return new Formula(script, functionCall);
        }
        #endregion
    }
}
