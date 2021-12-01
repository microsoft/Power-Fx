using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    /// <summary>
    /// Lookup info for a local rule scope.
    /// </summary>
    internal struct ScopedNameLookupInfo
    {
        public readonly DType Type;
        public readonly int ArgIndex;
        public readonly DName Name;
        public readonly DName Namespace;
        public readonly bool IsStateful;

        public ScopedNameLookupInfo(DType type, int argIndex, DName namesp, DName name, bool isStateful)
        {
            Contracts.AssertValid(type);
            Contracts.AssertValid(name);

            Type = type;
            ArgIndex = argIndex;
            Namespace = namesp;
            Name = name;
            IsStateful = isStateful;
        }

    }
}