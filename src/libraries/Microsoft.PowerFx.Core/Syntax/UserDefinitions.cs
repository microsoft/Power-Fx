// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.App;
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
        /// <summary>
        /// A script containing one or more UDFs.
        /// </summary>
        private readonly string _script;
        private readonly INameResolver _nameResolver;
        private readonly IUserDefinitionSemanticsHandler _userDefinitionSemanticsHandler;

        /// <summary>
        /// The language settings used for parsing this script.
        /// May be null if the script is to be parsed in the current locale.
        /// </summary>
        private readonly CultureInfo _loc;

        private UserDefinitions(string script, INameResolver nameResolver, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null, CultureInfo loc = null)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _userDefinitionSemanticsHandler = userDefinitionSemanticsHandler;
            _loc = loc;
        }

        public static ParseUserDefinitionResult Parse(string script, CultureInfo loc = null)
        {
            return TexlParser.ParseUserDefinitionScript(script, loc);
        }

        public static bool ProcessUserDefnitions(string script, INameResolver nameResolver, out UserDefinitionResult userDefinitionResult, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null, CultureInfo loc = null)
        {
            var userDefinitions = new UserDefinitions(script, nameResolver, userDefinitionSemanticsHandler, loc);

            return userDefinitions.ProcessUserDefnitions(out userDefinitionResult);
        }

        public bool ProcessUserDefnitions(out UserDefinitionResult userDefinitionResult)
        {
            var parseResult = TexlParser.ParseUserDefinitionScript(_script, _loc);

            if (parseResult.HasErrors)
            {
                userDefinitionResult = new UserDefinitionResult(Enumerable.Empty<UserDefinedFunction>(), parseResult.Errors, parseResult.NamedFormulas);
                return false;
            }
               
            var functions = DefineUserDefinedFunctions(parseResult.UDFs, out var errors);

            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());
            userDefinitionResult = new UserDefinitionResult(functions, errors, parseResult.NamedFormulas);

            return true;
        }

        private IEnumerable<UserDefinedFunction> DefineUserDefinedFunctions(IEnumerable<UDF> uDFs, out List<TexlError> errors)
        {
            Contracts.AssertValue(uDFs);

            var userDefinedFunctions = new List<UserDefinedFunction>();
            var texlFunctionSet = new TexlFunctionSet();
            errors = new List<TexlError>();

            foreach (var udf in uDFs)
            {
                var udfName = udf.Ident.Name;
                var func = new UserDefinedFunction(udfName, udf.ReturnType.GetFormulaType(), udf.Body, udf.IsImperative, udf.Args.Select(arg => arg.VarType.GetFormulaType()).ToArray());

                if (texlFunctionSet.AnyWithName(udfName) || BuiltinFunctionsCore._library.AnyWithName(udfName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionAlreadyDefined, udfName));

                    continue;
                }

                texlFunctionSet.Add(func);
                userDefinedFunctions.Add(func);
            }

            BindFunctionDeclarations(uDFs, texlFunctionSet, errors);

            return userDefinedFunctions;
        }

        private void BindFunctionDeclarations(IEnumerable<UDF> uDFs, TexlFunctionSet udfFunctionSet, List<TexlError> errors)
        {
            var glue = new Glue2DocumentBinderGlue();
            var argsAlreadySeen = new HashSet<string>();

            foreach (var udf in uDFs)
            {
                argsAlreadySeen.Clear();
                var record = RecordType.Empty();

                foreach (var arg in udf.Args)
                {
                    if (!argsAlreadySeen.Add(arg.VarIdent.Name))
                    {
                        errors.Add(new TexlError(arg.VarIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_DuplicateParameter, arg.VarIdent.Name));
                    }
                    else
                    {
                        argsAlreadySeen.Add(arg.VarIdent.Name);
                        record = record.Add(new NamedFormulaType(arg.VarIdent.ToString(), arg.VarType.GetFormulaType()));
                    }
                }

                var bindingConfig = new BindingConfig(udf.IsImperative);
                var binding = TexlBinding.Run(glue, udf.Body, ReadOnlySymbolTable.Compose(ReadOnlySymbolTable.NewFromRecord(record), ReadOnlySymbolTable.NewDefault(udfFunctionSet)), bindingConfig);

                UserDefinedFunction.CheckTypesOnDeclaration(binding.CheckTypesContext, udf.Args, udf.ReturnType, binding.ResultType, binding.ErrorContainer);
                _userDefinitionSemanticsHandler?.CheckSemanticsOnDeclaration(binding, udf.Args, binding.ErrorContainer);
                errors.AddRange(binding.ErrorContainer.GetErrors());
            }
        }
    }
}
