// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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

        /// <summary>
        /// The language settings used for parsing this script.
        /// May be null if the script is to be parsed in the current locale.
        /// </summary>
        private readonly CultureInfo _loc;
        private readonly bool _numberAsFloat;

        private UserDefinitions(string script, CultureInfo loc = null, bool numberAsFloat = false)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _loc = loc;
            _numberAsFloat = numberAsFloat;
        }

        public static ParseUserDefinitionResult Parse(string script, bool numberAsFloat = false, CultureInfo loc = null)
        {
            return TexlParser.ParseUserDefinitionScript(script, numberAsFloat, loc);
        }

        public static bool ProcessUserDefnitions(string script, out UserDefinitionResult userDefinitionResult, CultureInfo loc = null, bool numberAsFloat = false)
        {
            var userDefinitions = new UserDefinitions(script, loc, numberAsFloat);

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
                var func = new UserDefinedFunction(udfName, udf.ReturnType, udf.Body, udf.IsImperative, udf.Args);

                if (texlFunctionSet.AnyWithName(udfName) || BuiltinFunctionsCore._library.AnyWithName(udfName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionAlreadyDefined, udfName));

                    continue;
                }

                texlFunctionSet.Add(func);
                userDefinedFunctions.Add(func);
            }

            return userDefinedFunctions;
        }
    }
}
