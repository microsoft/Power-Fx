// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    internal class ParsedExpression : IExpression
    {
        internal IntermediateNode _irnode;
        private readonly ScopeSymbol _topScopeSymbol;
        private readonly CultureInfo _cultureInfo;

        internal ParsedExpression(IntermediateNode irnode, ScopeSymbol topScope, CultureInfo cultureInfo = null)
        {
            _irnode = irnode;
            _topScopeSymbol = topScope;
            _cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
        }

        public FormulaValue Eval(RecordValue parameters)
        {
            var ev2 = new EvalVisitor(_cultureInfo);
            var newValue = _irnode.Accept(ev2, SymbolContext.NewTopScope(_topScopeSymbol, parameters));

            return newValue;
        }
    }
}
