// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// This encapsulates a named formula and user defined functions: its original script, the parsed result, and any parse errors.
    /// </summary>
    internal class NamedFormulasAndUDFs
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

        public NamedFormulasAndUDFs(string script, CultureInfo loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            Loc = loc;
        }

        public ParseNamedFormulasAndUDFResult Parse()
        {
            return TexlParser.ParseNamedFormulasAndUDFsScript(Script, Loc);
        }
    }
}
