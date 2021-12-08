// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        public EnumStore EnumStore => _enumStore;
        public CultureInfo CultureInfo => _cultureInfo;

        public PowerFxConfig()
        {
            _enumStore = new EnumStore();
            _cultureInfo = CultureInfo.CurrentCulture;
        }

        public PowerFxConfig(EnumStore enumStore, CultureInfo cultureInfo)
        {
            _enumStore = enumStore;
            _cultureInfo = cultureInfo;
        }
    }
}
