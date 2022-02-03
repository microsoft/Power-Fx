// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types.Enums;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// A container that allows for compiler customization.
    /// </summary>
    public sealed class PowerFxConfig
    {
        private bool _isLocked;
        private readonly Dictionary<string, TexlFunction> _extraFunctions;

        internal ImmutableEnvironmentSymbolTable ImmutableEnvironmentSymbolTable;

        internal IReadOnlyDictionary<string, TexlFunction> ExtraFunctions => _extraFunctions;

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
            ImmutableEnvironmentSymbolTable = new ImmutableEnvironmentSymbolTable();
        }

        public void AddFunction(ReflectionFunction function)
        {
            CheckUnlocked();

            var texlFunction = function.GetTexlFunction();
            _extraFunctions.Add(texlFunction.GetUniqueTexlRuntimeName(), texlFunction);
        }

        public void AddOptionSet(OptionSet optionSet)
        {
            CheckUnlocked();

            ImmutableEnvironmentSymbolTable = ImmutableEnvironmentSymbolTable.With(optionSet);
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
