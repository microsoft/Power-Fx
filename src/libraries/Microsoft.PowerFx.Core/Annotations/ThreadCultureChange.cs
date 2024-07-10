// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#nullable enable

namespace Microsoft.PowerFx.Core.Annotations
{
    internal class ThreadCultureChange : IDisposable
    {
        private readonly CultureInfo? _origCulture;

        private readonly IDisposable _dispose;

        public ThreadCultureChange(CultureInfo? newCulture)
        {
            var guard = new GuardSingleThreaded();

            if (newCulture != null)
            {
                _origCulture = CultureInfo.CurrentCulture;
                CultureInfo.CurrentCulture = newCulture;
            }

            _dispose = guard.Enter();
        }

        public void Dispose()
        {
            if (_origCulture != null)
            {
                CultureInfo.CurrentCulture = _origCulture;
            }

            _dispose.Dispose();
        }
    }
}
