// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // These are "satellite" classes for RecalcEngine.
    // If these were interfaces, then RecalcEngine would just implement the interfaces.
    // But they're classes, and C# doesn't allow multiple class inheritence, so 
    // we create lightweight classes that just point back to the RecalcEngine for all work. 
    internal class RecalcSymbolValues : ReadOnlySymbolValues
    {
        private readonly RecalcEngine _parent;

        public RecalcSymbolValues(RecalcEngine parent)
        {
            _parent = parent;
        }

        protected override ReadOnlySymbolTable GetSymbolTableSnapshotWorker()
        {
            return _parent._symbolTable;
        }

        public override bool TryGetValue(string name, out FormulaValue value)
        {
            if (_parent.Formulas.TryGetValue(name, out var info))
            {
                value = info.Value;
                return true;
            }

            value = null;
            return false;
        }
    }

    /// <summary>
    /// <see cref="INameResolver"/> implementation for <see cref="RecalcEngine"/>.
    /// </summary>
    internal class RecalcEngineResolver : SymbolTable, IGlobalSymbolNameResolver
    {
        private new readonly RecalcEngine _parent;

        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols => _parent.Formulas.Select(kvp => new KeyValuePair<string, NameLookupInfo>(kvp.Key, Create(kvp.Key, kvp.Value)));

        private NameLookupInfo Create(string name, RecalcFormulaInfo recalcFormulaInfo)
        {
            return new NameLookupInfo(BindKind.PowerFxResolvedObject, recalcFormulaInfo.Value.Type._type, DPath.Root, 0, recalcFormulaInfo);
        }

        public RecalcEngineResolver(RecalcEngine parent)
        {
            _parent = parent;
            DebugName = "RecalcEngineResolver";
        }

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            var str = name.Value;

            if (_parent.Formulas.TryGetValue(str, out var fi))
            {
                nameInfo = Create(str, fi);
                return true;
            }

            nameInfo = default;
            return false;
        }
    }
}
