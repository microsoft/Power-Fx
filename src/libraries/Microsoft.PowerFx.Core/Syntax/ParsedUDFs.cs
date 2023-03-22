// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax
{
    /// <summary>
    /// This encapsulates a named formula: its original script, the parsed result, and any parse errors.
    /// </summary>
    internal class ParsedUDFs
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

        public readonly bool NumberIsFloat;

        public ParsedUDFs(string script, CultureInfo loc = null, bool numberIsFloat = false)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            Loc = loc;
            NumberIsFloat = numberIsFloat;
        }

        public ParseUDFsResult GetParsed()
        {
            return TexlParser.ParseUDFsScript(Script, Loc, NumberIsFloat);
        }
    }
}
