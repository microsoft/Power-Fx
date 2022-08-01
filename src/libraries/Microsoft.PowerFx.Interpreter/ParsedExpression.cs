﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal class ParsedExpression : IExpression
    {
        internal IntermediateNode _irnode;
        private readonly ScopeSymbol _topScopeSymbol;
        private readonly CultureInfo _cultureInfo;
        private readonly StackDepthCounter _stackMarker;

        internal ParsedExpression(IntermediateNode irnode, ScopeSymbol topScope, StackDepthCounter stackMarker, CultureInfo cultureInfo = null)
        {
            _irnode = irnode;
            _topScopeSymbol = topScope;
            _stackMarker = stackMarker;
            _cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
        }

        public async Task<FormulaValue> EvalAsync(RecordValue parameters, CancellationToken cancel)
        {
            var ev2 = new EvalVisitor(_cultureInfo, cancel);
            try
            {
                var newValue = await _irnode.Accept(ev2, new EvalVisitorContext(SymbolContext.NewTopScope(_topScopeSymbol, parameters), _stackMarker));
                return newValue;
            }
            catch (MaxCallDepthException maxCallDepthException)
            {
                return maxCallDepthException.ToErrorValue(_irnode.IRContext);
            }
        }

        internal async Task<FormulaValue> EvalAsyncInternal(RecordValue parameters, CancellationToken cancel, StackDepthCounter stackMarker)
        {
            // We don't catch the max call depth exception here becuase someone could swallow the error with an "IfError" check.
            // Instead we only catch at the top of parsed expression, which is the above function.
            var ev2 = new EvalVisitor(_cultureInfo, cancel);
            var newValue = await _irnode.Accept(ev2, new EvalVisitorContext(SymbolContext.NewTopScope(_topScopeSymbol, parameters), stackMarker));
            return newValue;
        }
    }
}
