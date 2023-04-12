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

        private UserDefinitions(string script, INameResolver globalNameResolver, IBinderGlue documentBinderGlue, CultureInfo loc = null, bool numberAsFloat = false, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null)
        {
            _globalNameResolver = globalNameResolver;
            _documentBinderGlue = documentBinderGlue;
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _loc = loc;
            _numberAsFloat = numberAsFloat;
            _userDefinitionSemanticsHandler = userDefinitionSemanticsHandler;
        }

        public static ParseUserDefinitionResult Parse(string script, bool numberAsFloat = false, CultureInfo loc = null)
        {
            return TexlParser.ParseUserDefinitionScript(script, numberAsFloat, loc);
        }

        public static bool ProcessUserDefnitions(string script, INameResolver globalNameResolver, IBinderGlue documentBinderGlue, out UserDefinitionResult userDefinitionResult, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null, CultureInfo loc = null, bool numberAsFloat = false)
        {
            var userDefinitions = new UserDefinitions(script, globalNameResolver, documentBinderGlue, loc, numberAsFloat, userDefinitionSemanticsHandler);

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
                CheckParameters(udf.Args, errors, out var paramsRecordType);
                CheckReturnType(udf.ReturnType, errors);

                var udfName = udf.Ident.Name;
                var func = new UserDefinedFunction(udfName.Value, udf.ReturnType, udf.Body, udf.IsImperative, udf.Args, paramsRecordType);

                if (texlFunctionSet.AnyWithName(udfName) || BuiltinFunctionsCore._library.AnyWithName(udfName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionAlreadyDefined, udfName));
                    continue;
                }

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
                var binding = udf.BindBody(_globalNameResolver, _documentBinderGlue, _userDefinitionSemanticsHandler, functionNameResolver);
                udf.CheckTypesOnDeclaration(binding.CheckTypesContext, actualBodyReturnType: binding.ResultType, binding.ErrorContainer);
                errors.AddRange(binding.ErrorContainer.GetErrors());
            }
        }

        private void CheckParameters(ISet<UDFArg> args, List<TexlError> errors, out RecordType paramRecordType)
        {
            paramRecordType = RecordType.Empty();
            var argsAlreadySeen = new HashSet<string>();

            foreach (var arg in args)
            {
                if (argsAlreadySeen.Contains(arg.VarIdent.Name))
                {
                    errors.Add(new TexlError(arg.VarIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_DuplicateParameter, arg.VarIdent.Name));
                }
                else
                {
                    argsAlreadySeen.Add(arg.VarIdent.Name);

                    if (arg.VarType.GetFormulaType()._type.Kind.Equals(DType.Unknown.Kind))
                    {
                        errors.Add(new TexlError(arg.VarType, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, arg.VarType.Name));
                    }

                    paramRecordType = paramRecordType.Add(new NamedFormulaType(arg.VarIdent.ToString(), arg.VarType.GetFormulaType()));
                }
            }
        }

        private void CheckReturnType(IdentToken returnType, List<TexlError> errors)
        {
            var returnTypeFormulaType = returnType.GetFormulaType()._type;

            if (returnTypeFormulaType.Kind.Equals(DType.Unknown.Kind))
            {
                errors.Add(new TexlError(returnType, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, returnType.Name));
            }
        }
    }
}
