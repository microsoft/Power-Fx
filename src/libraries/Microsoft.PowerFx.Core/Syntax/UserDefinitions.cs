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
        private static readonly ISet<string> _restrictedUDFNames = new HashSet<string> { "Type", "IsType", "AsType" };

        // Exposing it so hosts can filter out the intellisense suggestions
        public static readonly ISet<DType> RestrictedTypes = new HashSet<DType> { DType.DateTimeNoTimeZone, DType.ObjNull,  DType.Decimal };

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
            return TexlParser.ParseUserDefinitionScript(script, parserOptions);
        }

        /// <summary>
        /// Parses and creates user definitions (named formulas, user defined functions).
        /// </summary>
        /// <param name="script">Script with named formulas, user defined functions and user defined types.</param>
        /// <param name="parserOptions">Options for parsing an expression.</param>
        /// <param name="userDefinitionResult"><see cref="UserDefinitionResult"/>.</param>
        /// <param name="features">PowerFx feature flags.</param>
        /// <returns>True if there are no parser errors.</returns>
        public static bool ProcessUserDefinitions(string script, ParserOptions parserOptions, out UserDefinitionResult userDefinitionResult, Features features = null)
        {
            var userDefinitions = new UserDefinitions(script, parserOptions, features);

            return userDefinitions.ProcessUserDefinitions(out userDefinitionResult);
        }

        private bool ProcessUserDefinitions(out UserDefinitionResult userDefinitionResult)
        {
            var parseResult = Parse(_script, _parserOptions);

            if (_parserOptions.AllowAttributes)
            {
                parseResult = ProcessPartialAttributes(parseResult);
            }
            
            // Parser returns both complete & incomplete UDFs, and we are only interested in creating TexlFunctions for valid UDFs. 
            var functions = CreateUserDefinedFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), out var errors);

            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());
            userDefinitionResult = new UserDefinitionResult(
                functions,
                parseResult.Errors != null ? errors.Union(parseResult.Errors) : errors,
                parseResult.NamedFormulas);

            return true;
        }

        /// <summary>
        /// Process user script and returns user defined functions and named formulas.
        /// </summary>
        /// <param name="script">User script containing UDFs and/or named formulas.</param>
        /// <param name="parseCulture">CultureInfo to parse the script.</param>
        /// <param name="features">Features.</param>
        /// <returns>Tuple.</returns>
        /// <exception cref="InvalidOperationException">Throw if the user script contains errors.</exception>
        public static UserDefinitionResult Process(string script, CultureInfo parseCulture, Features features = null)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = false,
                Culture = parseCulture ?? CultureInfo.InvariantCulture
            };

            var sb = new StringBuilder();

            ProcessUserDefinitions(script, options, out var userDefinitionResult, features);

            if (userDefinitionResult.HasErrors)
            {
                sb.AppendLine("Something went wrong when parsing named formulas and/or user defined functions.");

                foreach (var error in userDefinitionResult.Errors)
                {
                    error.FormatCore(sb);
                }

                throw new InvalidOperationException(sb.ToString());
            }

            return userDefinitionResult;
        }

        private IEnumerable<UserDefinedFunction> CreateUserDefinedFunctions(IEnumerable<UDF> uDFs, out List<TexlError> errors)
        {
            Contracts.AssertValue(uDFs);

            var userDefinedFunctions = new List<UserDefinedFunction>();
            var texlFunctionSet = new TexlFunctionSet();
            errors = new List<TexlError>();

            foreach (var udf in uDFs)
            {
                var udfName = udf.Ident.Name;
                if (_restrictedUDFNames.Contains(udfName) || texlFunctionSet.AnyWithName(udfName) || BuiltinFunctionsCore._library.AnyWithName(udfName) || BuiltinFunctionsCore.OtherKnownFunctions.Contains(udfName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionAlreadyDefined, udfName));
                    continue;
                }

                var parametersOk = CheckParameters(udf.Args, errors);
                var returnTypeOk = CheckReturnType(udf.ReturnType, errors);
                if (!parametersOk || !returnTypeOk)
                {
                    continue;
                }

                var func = new UserDefinedFunction(udfName.Value, udf.ReturnType.GetFormulaType()._type, udf.Body, udf.IsImperative, udf.Args);

                texlFunctionSet.Add(func);
                userDefinedFunctions.Add(func);
            }

            return userDefinedFunctions;
        }

        private bool CheckParameters(ISet<UDFArg> args, List<TexlError> errors)
        {
            var isParamCheckSuccessful = true;
            var argsAlreadySeen = new HashSet<string>();

            foreach (var arg in args)
            {
                if (argsAlreadySeen.Contains(arg.NameIdent.Name))
                {
                    errors.Add(new TexlError(arg.NameIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_DuplicateParameter, arg.NameIdent.Name));
                    isParamCheckSuccessful = false;
                }
                else
                {
                    argsAlreadySeen.Add(arg.NameIdent.Name);

                    var parameterType = arg.TypeIdent.GetFormulaType()._type;
                    if (parameterType.Kind.Equals(DType.Unknown.Kind) || RestrictedTypes.Contains(parameterType))
                    {
                        errors.Add(new TexlError(arg.TypeIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, arg.TypeIdent.Name));
                        isParamCheckSuccessful = false;
                    }
                }
            }

            return isParamCheckSuccessful;
        }

        private bool CheckReturnType(IdentToken returnType, List<TexlError> errors)
        {
            var returnTypeFormulaType = returnType.GetFormulaType()._type;
            var isReturnTypeCheckSuccessful = true;

            if (returnTypeFormulaType.Kind.Equals(DType.Unknown.Kind) || RestrictedTypes.Contains(returnTypeFormulaType))
            {
                errors.Add(new TexlError(returnType, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, returnType.Name));
                isReturnTypeCheckSuccessful = false;
            }

            return isReturnTypeCheckSuccessful;
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
                PartialAttribute.AttributeOperationKind.PartialTable => GeneratePartialFunction("TableConcatenate", name, formulas),
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
