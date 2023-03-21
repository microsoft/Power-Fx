// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// This encapsulates a named formula and user defined functions: its original script, the parsed result, and any parse errors.
    /// </summary>
    internal sealed class UserDefinitions
    {
        // Used to provide a hook to check semantics on a user defined function
        public IUserDefinitionSemanticsHandler UserDefinitionSemanticsHandler { get; }

        public INameResolver NameResolver { get; }

        public UserDefinitions(IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler, INameResolver nameResolver)
        {
            Contracts.AssertValue(userDefinitionSemanticsHandler);
            Contracts.AssertValue(nameResolver);

            UserDefinitionSemanticsHandler = userDefinitionSemanticsHandler;
            NameResolver = nameResolver;
        }

        /// <summary>
        /// Parses provided script for user defnitions (Named formulas and UDFs).
        /// Binds and registers user defined function declarations.
        /// </summary>
        /// <param name="script">A script containing one or more UDFs and named formulas. </param>
        /// <param name="userDefinitionResult"></param>
        /// <param name="loc">The language settings used for parsing this script.</param>
        /// <returns>True if there are no parse errors. </returns>
        public bool ProcessUserDefnitions(string script, out UserDefinitionResult userDefinitionResult, CultureInfo loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            var parseResult = TexlParser.ParseUserDefinitionScript(script, loc);
            var functions = DefineUserDefinedFunctions(parseResult.UDFs, out var errors);

            errors.AddRange(parseResult.Errors);
            userDefinitionResult = new UserDefinitionResult(functions, ExpressionError.New(errors, CultureInfo.InvariantCulture), parseResult.NamedFormulas);

            return parseResult.HasErrors;
        }

        private IEnumerable<UserDefinedFunction> DefineUserDefinedFunctions(IEnumerable<UDF> uDFs, out List<TexlError> errors)
        {
            Contracts.AssertValue(uDFs);

            var userDefinedFunctions = new List<UserDefinedFunction>();
            var symbolTable = (NameResolver as SymbolTable).VerifyValue();
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

            BindFunctionDeclarations(uDFs, errors);

            return userDefinedFunctions;
        }

        private void BindFunctionDeclarations(IEnumerable<UDF> uDFs, List<TexlError> errors)
        {
            var glue = new Glue2DocumentBinderGlue();
            var symbolTable = (NameResolver as SymbolTable).VerifyValue();

            foreach (var udf in uDFs)
            {
                var bindingConfig = new BindingConfig(udf.IsImperative);
                var binding = TexlBinding.Run(glue, udf.Body, NameResolver, bindingConfig);

                UserDefinedFunction.CheckTypesOnDeclaration(binding.CheckTypesContext, udf.Args, udf.ReturnType, binding.ResultType, binding.ErrorContainer);
                UserDefinitionSemanticsHandler.CheckSemanticsOnDeclaration(binding, udf.Args, binding.ErrorContainer);

                if (binding.ErrorContainer.HasErrors())
                {
                    symbolTable.RemoveFunction(udf.Ident.Name);
                }

                errors.AddRange(binding.ErrorContainer.GetErrors());
            }
        }
    }
}
