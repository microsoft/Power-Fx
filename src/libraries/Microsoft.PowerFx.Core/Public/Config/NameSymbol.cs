// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Used between Symbol Table resolver and IR.
    /// NameInfo data to associate a symbol at bind time with a runtime config at runtime. 
    /// Object provided by resolver data in <see cref="NameLookupInfo.Data"/>.
    /// IR then recognizes this and will look these values up via ReadOnlySymbolValues.TryGetValue.
    /// </summary>
    [DebuggerDisplay("{Name} ({Owner.DebugName}:{SlotIndex})")]
    internal class NameSymbol : ISymbolSlot
    {
        public NameSymbol(string name, bool mutable)
        {
            Name = name;
            IsMutable = mutable;
        }

        public NameSymbol(DName name, bool mutable)
            : this(name.Value, mutable)
        {
        }

        public string Name { get; private set; }

        /// <summary>
        /// Can this variable be passed to Set(). If not, then this is a binding failure. 
        /// </summary>
        public bool IsMutable { get; private set; }

        public ReadOnlySymbolTable Owner { get; set; }

        public int SlotIndex { get; set; } = -1;

        public void DisposeSlot()
        {
            SlotIndex = -1;
        }        
    }

    /// <summary>
    /// A "slot" is the opaque handle to acts as a storage location for a symbol. 
    /// It can be used for both get and set on symbols. 
    /// The IR will refer to slots when accessing symbols. Unique slots mean unique symbols.
    /// Runtime allocates storage for these slots.
    /// </summary>
    public interface ISymbolSlot
    {
        /// <summary>
        /// Symbol Table that this slot refers to. 
        /// </summary>
        ReadOnlySymbolTable Owner { get; }

        /// <summary>
        /// A densely packed integer. Slot is invalid if less than 0.
        /// </summary>
        int SlotIndex { get; }
    }

    internal static class SymbolSlotExtensions
    {
        internal static string DebugName(this ReadOnlySymbolTable symTable)
        {
            // Include GetHashCode() to tell difference between differnt instances of a
            // symbol tables with the same name. 
            return $"{symTable.DebugName}_{symTable.GetHashCode()}";
        }

        public static string DebugName(this ISymbolSlot slot)
        {
            if (slot == null)
            {
                return $"'null'";
            }

            var symTableName = slot.Owner.DebugName();

            if (slot is NameSymbol info)
            {
                return $"'{info.Name}:{symTableName}'";
            }

            return $"'{slot.SlotIndex}:{symTableName}'";
        }

        public static bool IsDisposed(this ISymbolSlot slot)
        {
            return slot.SlotIndex < 0;
        }

        public static void ThrowIfDisposed(this ISymbolSlot slot)
        {
            if (slot.IsDisposed())
            {
                // this can happen if we try to use a slot that was already disposed. 
                // For example, if we try to run an IR tree that refers to a removed variable.
                throw new InvalidOperationException($"Slot {slot.DebugName()} was disposed");
            }
        }
    }
}
