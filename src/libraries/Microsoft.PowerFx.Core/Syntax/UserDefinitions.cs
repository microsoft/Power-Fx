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
        private readonly INameResolver _globalNameResolver;
        private readonly IBinderGlue _documentBinderGlue;
        private readonly BindingConfig _bindingConfig;

        /// <summary>
        /// A script containing one or more UDFs.
        /// </summary>
        private readonly string _script;
        private readonly ParserOptions _parserOptions;
        private readonly Features _features;
        private static readonly ISet<string> _restrictedUDFNames = new HashSet<string> { "Type", "IsType", "AsType" };

        private UserDefinitions(string script, INameResolver globalNameResolver, IBinderGlue documentBinderGlue, BindingConfig bindingConfig, ParserOptions parserOptions, Features features = null)
        {
            _features = features ?? Features.None;
            _globalNameResolver = globalNameResolver;
            _documentBinderGlue = documentBinderGlue;
            _bindingConfig = bindingConfig;
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _parserOptions = parserOptions;
        }

        public static ParseUserDefinitionResult Parse(string script, ParserOptions parserOptions)
        {
            return TexlParser.ParseUserDefinitionScript(script, parserOptions);
        }

        public static bool ProcessUserDefinitions(string script, INameResolver globalNameResolver, IBinderGlue documentBinderGlue, BindingConfig bindingConfig, ParserOptions parserOptions, out UserDefinitionResult userDefinitionResult, Features features = null, bool shouldBindBody = false)
        {
            var userDefinitions = new UserDefinitions(script, globalNameResolver, documentBinderGlue, bindingConfig, parserOptions, features);

            return userDefinitions.ProcessUserDefinitions(shouldBindBody, out userDefinitionResult);
        }

        private bool ProcessUserDefinitions(bool shouldBindBody, out UserDefinitionResult userDefinitionResult)
        {
            var parseResult = TexlParser.ParseUserDefinitionScript(_script, _parserOptions);

            if (parseResult.HasErrors)
            {
                userDefinitionResult = new UserDefinitionResult(Enumerable.Empty<UserDefinedFunction>(), parseResult.Errors, parseResult.NamedFormulas);
                return false;
            }
               
            var functions = CreateUserDefinedFunctions(parseResult.UDFs, shouldBindBody, out var errors);

            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());
            userDefinitionResult = new UserDefinitionResult(functions, errors, parseResult.NamedFormulas);

            return true;
        }

        private IEnumerable<UserDefinedFunction> CreateUserDefinedFunctions(IEnumerable<UDF> uDFs, bool shouldBindBody, out List<TexlError> errors)
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

            if (shouldBindBody)
            {
                BindUserDefinedFunctions(userDefinedFunctions, ReadOnlySymbolTable.NewDefault(texlFunctionSet) as INameResolver, errors);
            }

            return userDefinedFunctions;
        }

        private void BindUserDefinedFunctions(IEnumerable<UserDefinedFunction> userDefinedFunctions, INameResolver functionNameResolver, List<TexlError> errors)
        {
            foreach (var udf in userDefinedFunctions)
            {
                var binding = udf.BindBody(_globalNameResolver, _documentBinderGlue, _bindingConfig, _features, functionNameResolver);
                udf.CheckTypesOnDeclaration(binding.CheckTypesContext, actualBodyReturnType: binding.ResultType, binding.ErrorContainer);
                errors.AddRange(binding.ErrorContainer.GetErrors());
            }
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

                    if (arg.TypeIdent.GetFormulaType()._type.Kind.Equals(DType.Unknown.Kind))
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

            if (returnTypeFormulaType.Kind.Equals(DType.Unknown.Kind))
            {
                errors.Add(new TexlError(returnType, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, returnType.Name));
                isReturnTypeCheckSuccessful = false;
            }

            return isReturnTypeCheckSuccessful;
        }
    }
}
