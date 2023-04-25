// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

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

        /// <summary>
        /// The language settings used for parsing this script.
        /// May be null if the script is to be parsed in the current locale.
        /// </summary>
        private readonly CultureInfo _loc;
        private readonly bool _numberAsFloat;
        private readonly IUserDefinitionSemanticsHandler _userDefinitionSemanticsHandler;
        private readonly Features _features;

        private UserDefinitions(string script, INameResolver globalNameResolver, IBinderGlue documentBinderGlue, BindingConfig bindingConfig, CultureInfo loc = null, bool numberAsFloat = false, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null, Features features = null)
        {
            _features = features ?? Features.None;
            _globalNameResolver = globalNameResolver;
            _documentBinderGlue = documentBinderGlue;
            _bindingConfig = bindingConfig;
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _loc = loc;
            _numberAsFloat = numberAsFloat;
            _userDefinitionSemanticsHandler = userDefinitionSemanticsHandler;
        }

        public static ParseUserDefinitionResult Parse(string script, bool numberAsFloat = false, CultureInfo loc = null)
        {
            return TexlParser.ParseUserDefinitionScript(script, numberAsFloat, loc);
        }

        public static bool ProcessUserDefnitions(string script, INameResolver globalNameResolver, IBinderGlue documentBinderGlue, BindingConfig bindingConfig, out UserDefinitionResult userDefinitionResult, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null, CultureInfo loc = null, bool numberAsFloat = false, Features features = null)
        {
            var userDefinitions = new UserDefinitions(script, globalNameResolver, documentBinderGlue, bindingConfig, loc, numberAsFloat, userDefinitionSemanticsHandler, features);

            return userDefinitions.ProcessUserDefnitions(out userDefinitionResult);
        }

        public bool ProcessUserDefnitions(out UserDefinitionResult userDefinitionResult)
        {
            var parseResult = TexlParser.ParseUserDefinitionScript(_script, _numberAsFloat, _loc);

            if (parseResult.HasErrors)
            {
                userDefinitionResult = new UserDefinitionResult(Enumerable.Empty<UserDefinedFunction>(), parseResult.Errors, parseResult.NamedFormulas);
                return false;
            }
               
            var functions = CreateUserDefinedFunctions(parseResult.UDFs, out var errors);

            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());
            userDefinitionResult = new UserDefinitionResult(functions, errors, parseResult.NamedFormulas);

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
                if (texlFunctionSet.AnyWithName(udfName) || BuiltinFunctionsCore._library.AnyWithName(udfName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionAlreadyDefined, udfName));
                    continue;
                }

                if (!(CheckParameters(udf.Args, errors) & CheckReturnType(udf.ReturnType, errors)))
                {
                    continue;
                }
                
                var func = new UserDefinedFunction(udfName.Value, udf.ReturnType, udf.Body, udf.IsImperative, udf.Args);

                texlFunctionSet.Add(func);
                userDefinedFunctions.Add(func);
            }

            BindUserDefinedFunctions(userDefinedFunctions, ReadOnlySymbolTable.NewDefault(texlFunctionSet) as INameResolver, errors);

            return userDefinedFunctions;
        }

        private void BindUserDefinedFunctions(IEnumerable<UserDefinedFunction> userDefinedFunctions, INameResolver functionNameResolver, List<TexlError> errors)
        {
            foreach (var udf in userDefinedFunctions)
            {
                var binding = udf.BindBody(_globalNameResolver, _documentBinderGlue, _bindingConfig, _userDefinitionSemanticsHandler, _features, functionNameResolver);
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
                if (argsAlreadySeen.Contains(arg.VarIdent.Name))
                {
                    errors.Add(new TexlError(arg.VarIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_DuplicateParameter, arg.VarIdent.Name));
                    isParamCheckSuccessful = false;
                }
                else
                {
                    argsAlreadySeen.Add(arg.VarIdent.Name);

                    if (arg.VarType.GetFormulaType()._type.Kind.Equals(DType.Unknown.Kind))
                    {
                        errors.Add(new TexlError(arg.VarType, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, arg.VarType.Name));
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
