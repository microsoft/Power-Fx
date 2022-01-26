// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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
    internal class NamedFormulas
    {
        /// <summary>
        /// A script containing one or more named formulas.
        /// </summary>
        public readonly string Script;

        // The language settings used for parsing this script.
        // May be null if the script is to be parsed in the current locale.
        public readonly ILanguageSettings Loc;

        public bool IsParsed => _formulasResult != null;

        public bool HasParseErrors { get; private set; }

        private Dictionary<DName, TexlNode> _formulasResult;

        private List<TexlError> _errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedFormulas"/> class.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="loc"></param>
        public NamedFormulas(string script, ILanguageSettings loc = null)
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

            return _errors == null;
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

        /// <summary>
        /// Returns a Tuple of a DName and Formula object for each named formula.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tuple<DName, Formula>> GetNamedFormulas()
        {
            var formulas = new List<Tuple<DName, Formula>>();
            if (_formulasResult != null)
            {
                foreach (var kvp in _formulasResult)
                {
                    formulas.Add(Tuple.Create(kvp.Key, GetFormula(kvp.Value)));
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
