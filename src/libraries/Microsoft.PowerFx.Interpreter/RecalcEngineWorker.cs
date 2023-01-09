// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

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
                var info = _parent.GetByName(varName);

                var newValue = _parent.GetValue(varName);
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

            var fi = _parent.GetByName(name);

            // Now calculate this node. Will recalc any dependencies if needed.                 
            if (fi._binding != null)
            {
                var binding = fi._binding;

                (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(binding);

                var scope = this;

                var symbols = _parent._symbolValues;

                var runtimeConfig = new RuntimeConfig(symbols, _cultureInfo);
                var v = new EvalVisitor(runtimeConfig, CancellationToken.None);

                var newValue = irnode.Accept(v, new EvalVisitorContext(SymbolContext.New(), new StackDepthCounter(_parent.Config.MaxCallDepth))).Result;

                var val = _parent.GetValue(name);
                var equal = val is BlankValue && // blank on initial run. 
                    RuntimeHelpers.AreEqual(newValue, val);

                if (!equal)
                {
                    _sendUpdates.Add(name);

                    _parent._symbolValues.Set(fi.Slot, newValue);
                }
            }

            _calcs[name] = _parent.GetValue(name);
        }

        // Recalc Name and any downstream formulas that may now be updated.
        private void RecalcWorkerAndPropagate(string name)
        {
            RecalcWorker2(name);

            var fi = _parent.GetByName(name);

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
