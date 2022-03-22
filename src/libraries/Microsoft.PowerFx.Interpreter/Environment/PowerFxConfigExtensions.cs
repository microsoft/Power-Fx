// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    public static class PowerFxConfigExtensions
    {
        public static void AddOptionSet(this PowerFxConfig powerFxConfig, OptionSet optionSet, DName optionalDisplayName = default)
        {
            powerFxConfig.AddEntity(optionSet, optionalDisplayName);
        }

        public static void AddFunction(this PowerFxConfig powerFxConfig, ReflectionFunction function)
        {
            powerFxConfig.AddFunction(function.GetTexlFunction());
        }
    }
}
