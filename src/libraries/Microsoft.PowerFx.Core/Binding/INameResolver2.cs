using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Binding
{
    internal interface INameResolver2 : INameResolver
    {
        IReadOnlyDictionary<string, IGlobalSymbol> GlobalSymbols { get; }
    }
}
