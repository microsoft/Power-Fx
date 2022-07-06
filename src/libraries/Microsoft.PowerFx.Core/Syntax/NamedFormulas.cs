// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// This encapsulates a named formula: its original script, the parsed result, and any parse errors.
    /// </summary>
    internal class NamedFormulas
    {
        /// <summary>
        /// A script containing one or more named formulas.
        /// </summary>
        public readonly string Script;

        // The language settings used for parsing this script.
        // May be null if the script is to be parsed in the current locale.
        public readonly CultureInfo Loc;

        public bool IsParsed => _formulasResult != null;

        public bool HasParseErrors { get; private set; }

        private IEnumerable<KeyValuePair<IdentToken, TexlNode>> _formulasResult;

        private IEnumerable<TexlError> _errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedFormulas"/> class.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="loc"></param>
        public NamedFormulas(string script, CultureInfo loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            Loc = loc;
        }

        /// <summary>
        /// Ensures that the named formulas have been parsed and if not, parses them.
        /// </summary>
        /// <returns>Tuple of IdentToken and formula.</returns>
        public IEnumerable<(IdentToken token, Formula formula)> EnsureParsed()
        {
            if (_formulasResult == null)
            {
                Contracts.AssertValue(Script);
                Contracts.AssertValueOrNull(Loc);
                var result = TexlParser.ParseFormulasScript(Script, loc: Loc);
                _formulasResult = result.NamedFormulas;
                _errors = result.Errors;
                HasParseErrors = result.HasError;
                Contracts.AssertValue(_formulasResult);
            }

            return GetNamedFormulas();
        }

        /// <summary>
        /// Returns any parse errors.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TexlError> GetParseErrors()
        {
            Contracts.AssertValue(Script);
            Contracts.Assert(IsParsed, "Should call EnsureParsed() first!");
            return _errors ?? Enumerable.Empty<TexlError>();
        }

        private IEnumerable<(IdentToken token, Formula formula)> GetNamedFormulas()
        {
            var formulas = new List<(IdentToken, Formula)>();
            if (_formulasResult != null)
            {
                foreach (var kvp in _formulasResult)
                {
                    formulas.Add((kvp.Key, GetFormula(kvp.Value)));
                }
            }

            return formulas;
        }

        private Formula GetFormula(TexlNode node)
        {
            return new Formula(node.GetCompleteSpan().GetFragment(Script), node);
        }
    }
}
