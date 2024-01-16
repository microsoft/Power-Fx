// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

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
            var parseResult = TexlParser.ParseUserDefinitionScript(_script, _parserOptions);
            
            // Parser returns both complete & incomplete UDFs, and we are only interested in creating TexlFunctions for valid UDFs. 
            var functions = CreateUserDefinedFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), out var errors);

            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());
            userDefinitionResult = new UserDefinitionResult(
                functions,
                parseResult.Errors != null ? errors.Union(parseResult.Errors) : errors,
                parseResult.NamedFormulas);

            return true;
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
    }
}
