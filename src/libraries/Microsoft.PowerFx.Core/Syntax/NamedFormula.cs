// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax
{
    /// <summary>
    /// This encapsulates a named formula: its original script, the parsed result, and any parse errors.
    /// </summary>
    internal class NamedFormula
    {
        /// <summary>
        /// A script containing one or more named formulas.
        /// </summary>
        public readonly string Script;

        // The language settings used for parsing this script.
        // May be null if the script is to be parsed in the current locale.
        public readonly ILanguageSettings Loc;

        public Dictionary<DName, TexlNode> FormulasResult;

        private List<TexlError> _errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedFormula"/> class.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="loc"></param>
        public NamedFormula(string script, ILanguageSettings loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            Loc = loc;
        }

        /// <summary>
        /// Ensures that the named formulas have been parsed and if not, parses them.
        /// </summary>
        /// <returns></returns>
        public bool EnsureParsed()
        {
            if (FormulasResult == null)
            {
                var result = TexlParser.ParseFormulasScript(Script, loc: Loc);
                FormulasResult = result.NamedFormulas;
                _errors = result.Errors;
            }

            return _errors == null;
        }

        /// <summary>
        /// Gets the formula part of the script associated with a given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string TryGetSubscript(DName name)
        {
            var nodeExists = FormulasResult.TryGetValue(name, out var node);
            return nodeExists ? node.GetCompleteSpan().GetFragment(Script) : null;
        }
    }
}
