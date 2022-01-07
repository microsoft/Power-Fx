// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax
{
    internal sealed class NamedFormula
    {
        public readonly string Script;

        // The language settings used for parsing this script.
        // May be null if the script is to be parsed in the current locale.
        public readonly ILanguageSettings Loc;

        private List<TexlError> _errors;

        internal Dictionary<DName, TexlNode> _formulasResult;

        public NamedFormula(string script, ILanguageSettings loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            Loc = loc;
        }

        public bool EnsureParsed()
        {
            if(_formulasResult == null)
            {
                var result = TexlParser.ParseFormulasScript(Script, loc: Loc);
                _formulasResult = result.NamedFormulas;
                _errors = result.Errors;
            }
            return _errors == null;
        }
    }
}
