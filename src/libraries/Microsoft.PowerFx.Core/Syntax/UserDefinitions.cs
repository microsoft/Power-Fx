// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
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
        public readonly string Script;

        /// <summary>
        /// The language settings used for parsing this script.
        /// May be null if the script is to be parsed in the current locale.
        /// </summary>
        public readonly CultureInfo Loc;

        public UserDefinitions(string script, CultureInfo loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            Loc = loc;
        }

        public ParseUserDefinitionResult Parse()
        {
            return TexlParser.ParseUserDefinitionScript(Script, Loc);
        }

        public bool ProcessUserDefnitions(INameResolver nameResolver,  out UserDefinitionResult userDefinitionResult, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null)
        {
            Contracts.AssertValue(nameResolver);
            Contracts.AssertValueOrNull(userDefinitionSemanticsHandler);

            var parseResult = Parse();
            var functions = DefineUserDefinedFunctions(parseResult.UDFs, nameResolver, userDefinitionSemanticsHandler, out var errors);

            errors.AddRange(parseResult.Errors);
            userDefinitionResult = new UserDefinitionResult(functions, errors, parseResult.NamedFormulas);

            return parseResult.HasErrors;
        }

        private IEnumerable<UserDefinedFunction> DefineUserDefinedFunctions(IEnumerable<UDF> uDFs,  INameResolver nameResolver, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler, out List<TexlError> errors)
        {
            Contracts.AssertValue(uDFs);

            var userDefinedFunctions = new List<UserDefinedFunction>();
            var symbolTable = (nameResolver as SymbolTable).VerifyValue();
            errors = new List<TexlError>();

            foreach (var udf in uDFs)
            {
                var udfName = udf.Ident.Name;
                if (symbolTable.Functions.AnyWithName(udfName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionAlreadyDefined, udfName));
                }

                var func = new UserDefinedFunction(udfName, udf.ReturnType.GetFormulaType(), udf.Body, udf.IsImperative, udf.Args.Select(arg => arg.VarType.GetFormulaType()).ToArray());

                userDefinedFunctions.Add(func);
                symbolTable.AddFunction(func);
            }

            BindFunctionDeclarations(uDFs, nameResolver, userDefinitionSemanticsHandler, errors);

            return userDefinedFunctions;
        }

        private void BindFunctionDeclarations(IEnumerable<UDF> uDFs, INameResolver nameResolver, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler, List<TexlError> errors)
        {
            var glue = new Glue2DocumentBinderGlue();
            var symbolTable = (nameResolver as SymbolTable).VerifyValue();

            foreach (var udf in uDFs)
            {
                var bindingConfig = new BindingConfig(udf.IsImperative);
                var binding = TexlBinding.Run(glue, udf.Body, nameResolver, bindingConfig);

                UserDefinedFunction.CheckTypesOnDeclaration(binding.CheckTypesContext, udf.Args, udf.ReturnType, binding.ResultType, binding.ErrorContainer);
                userDefinitionSemanticsHandler?.CheckSemanticsOnDeclaration(binding, udf.Args, binding.ErrorContainer);

                if (binding.ErrorContainer.HasErrors())
                {
                    symbolTable.RemoveFunction(udf.Ident.Name);
                }

                errors.AddRange(binding.ErrorContainer.GetErrors());
            }
        }
    }
}
