using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.IR.Symbols
{
    internal interface IGlobalSymbol
    {
        public string Name { get; }
        public string Description { get; }
        public FormulaType Type { get; }
    }
}
