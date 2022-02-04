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

        internal IReadOnlyDictionary<string, TexlFunction> ExtraFunctions => _extraFunctions;

        internal IReadOnlyDictionary<DName, IExternalEntity> EnvironmentSymbols => _environmentSymbols;

        internal EnumStore EnumStore { get; }

        internal CultureInfo CultureInfo { get; }        

        public PowerFxConfig(CultureInfo cultureInfo = null)
            : this(null, cultureInfo)
        {
        }

        internal PowerFxConfig(EnumStore enumStore = null, CultureInfo cultureInfo = null)
        {
            EnumStore = enumStore ?? new EnumStore();
            CultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
            _isLocked = false;
            _extraFunctions = new Dictionary<string, TexlFunction>();
            _environmentSymbols = new Dictionary<DName, IExternalEntity>();
        }

        internal void AddEntity(IExternalEntity entity)
        {
            CheckUnlocked();

            _environmentSymbols.Add(entity.EntityName, entity);
        }

        internal void AddFunction(TexlFunction function)
        {
            CheckUnlocked();

            _extraFunctions.Add(function.GetUniqueTexlRuntimeName(), function);
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
