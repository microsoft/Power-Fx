// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// A container that allows for compiler customization.
    /// </summary>
    public sealed class PowerFxConfig
    {
        private bool _isLocked;
        private readonly Dictionary<string, TexlFunction> _extraFunctions;
        private readonly Dictionary<DName, IExternalEntity> _environmentSymbols;
        private SingleSourceDisplayNameProvider _environmentSymbolDisplayNameProvider;

        internal IReadOnlyDictionary<string, TexlFunction> ExtraFunctions => _extraFunctions;

        internal IReadOnlyDictionary<DName, IExternalEntity> EnvironmentSymbols => _environmentSymbols;

        internal EnumStore EnumStore { get; }

        internal CultureInfo CultureInfo { get; }

        private PowerFxConfig(CultureInfo cultureInfo, EnumStore enumStore) 
        {
            CultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
            _isLocked = false;
            _extraFunctions = new Dictionary<string, TexlFunction>();
            _environmentSymbols = new Dictionary<DName, IExternalEntity>();
            _environmentSymbolDisplayNameProvider = new SingleSourceDisplayNameProvider();
            EnumStore = enumStore;
        }      

        public PowerFxConfig(CultureInfo cultureInfo = null)
            : this(cultureInfo, new EnumStore()) 
        {
        }

        /// <summary>
        /// Stopgap until Enum Store is refactored. Do not rely on, this will be removed. 
        /// </summary>
        internal static PowerFxConfig BuildWithEnumStore(CultureInfo cultureInfo, EnumStore enumStore)
        {
            return new PowerFxConfig(cultureInfo, enumStore);
        }

        internal void AddEntity(IExternalEntity entity, DName displayName = default)
        {
            CheckUnlocked();

            if (displayName != default) 
            {
                // Attempt to update display name provider before symbol table,
                // since it can throw on collision and we want to leave the config in a good state
                _environmentSymbolDisplayNameProvider = _environmentSymbolDisplayNameProvider.AddField(entity.EntityName, displayName);
            }

            _environmentSymbols.Add(entity.EntityName, entity);
        }

        internal void AddFunction(TexlFunction function)
        {
            CheckUnlocked();

            _extraFunctions.Add(function.GetUniqueTexlRuntimeName(), function);
        }
                
        internal bool TryGetSymbol(DName name, out IExternalEntity symbol, out DName displayName)
        {
            var lookupName = name;
            if (_environmentSymbolDisplayNameProvider.TryGetLogicalName(name, out var logicalName))
            {
                lookupName = logicalName;
                displayName = name;
            }
            else if (_environmentSymbolDisplayNameProvider.TryGetDisplayName(name, out displayName))
            {
                lookupName = name;
            }

            return _environmentSymbols.TryGetValue(lookupName, out symbol);
        }

        internal void Lock()
        { 
            CheckUnlocked();

            _isLocked = true;
        }

        private void CheckUnlocked()
        {
            if (_isLocked)
            {
                throw new InvalidOperationException("This PowerFxConfig instance is locked");
            }
        }
    }
}
