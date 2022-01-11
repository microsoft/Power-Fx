// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Functions;

namespace Microsoft.PowerFx
{
    // Drives the actual recalc based on the graph . 
    // $$$ This is a really bad, inefficient, buggy implementation. 
    // Barely complete enough to demonstrate the public interface.
    // This implements IScope as a means to intercept dependencies and evaluate. 
    internal class RecalcEngineWorker : IScope
    {
        // recomputed values. 
        private readonly Dictionary<string, FormulaValue> _calcs = new Dictionary<string, FormulaValue>();

        // Send updates on these vars. 
        // These are the ones we propagate too. 
        private readonly HashSet<string> _sendUpdates = new HashSet<string>();

        private readonly RecalcEngine _parent;

        private readonly CultureInfo _cultureInfo;

        public RecalcEngineWorker(RecalcEngine parent, CultureInfo cultureInfo = null)
        {
            _parent = parent;
            _cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
        }

        // Start
        public void Recalc(string name)
        {
            RecalcWorkerAndPropagate(name);

            // Dispatch update hooks. 
            foreach (var varName in _sendUpdates.OrderBy(x => x))
            {
                var info = _parent.Formulas[varName];

                var newValue = info._value;
                info._onUpdate?.Invoke(varName, newValue);
            }
        }

        // Just recalc Name. 
        private void RecalcWorker2(string name)
        {
            if (_calcs.ContainsKey(name))
            {
                return; // already computed. 
            }

            var fi = _parent.Formulas[name];

            // Now calculate this node. Will recalc any dependencies if needed.                 
            if (fi._binding != null)
            {
                var binding = fi._binding;

                (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(binding);

                var scope = this;
                var v = new EvalVisitor(_cultureInfo);

                var newValue = irnode.Accept(v, SymbolContext.New());

                var equal = fi._value != null && // null on initial run. 
                    RuntimeHelpers.AreEqual(newValue, fi._value);

                if (!equal)
                {
                    _sendUpdates.Add(name);
                }

                fi._value = newValue;
            }

            _calcs[name] = fi._value;
        }

        // Recalc Name and any downstream formulas that may now be updated.
        private void RecalcWorkerAndPropagate(string name)
        {
            RecalcWorker2(name);

            var fi = _parent.Formulas[name];

            // Propagate changes.
            foreach (var x in fi._usedBy)
            {
                RecalcWorkerAndPropagate(x);
            }
        }

        // Intercept any dependencies this formula has and ensure the dependencies are re-evaluated. 
        FormulaValue IScope.Resolve(string name)
        {
            if (!_calcs.TryGetValue(name, out var value))
            {
                // Dependency is not yet recalced. 
                RecalcWorker2(name);

                value = _calcs[name];
            }

            return value;
        }
    } // end class RecalcHelper
}
