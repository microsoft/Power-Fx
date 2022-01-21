// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.Types.Enums;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// A container that allows for compiler customization.
    /// </summary>
    public sealed class PowerFxConfig
    {
        private readonly EnumStore _enumStore;
        private readonly CultureInfo _cultureInfo;

        internal EnumStore EnumStore => _enumStore;

        public CultureInfo CultureInfo => _cultureInfo;

        public PowerFxConfig()
        {
            _enumStore = new EnumStore();
            _cultureInfo = CultureInfo.CurrentCulture;
        }

        public PowerFxConfig(CultureInfo cultureInfo)
        {
            _enumStore = new EnumStore();
            _cultureInfo = cultureInfo;
        }

        internal PowerFxConfig(EnumStore enumStore, CultureInfo cultureInfo)
        {
            _enumStore = enumStore;
            _cultureInfo = cultureInfo;
        }
    }
}
