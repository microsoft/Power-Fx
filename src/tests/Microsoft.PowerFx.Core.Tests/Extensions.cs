// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Core.Tests
{
    public static class Extensions
    {
        public static void AddBehaviorFunction(this PowerFxConfig config)
        {
            config.AddFunction(new BehaviorFunction());
        }

        public static string GetErrBehaviorPropertyExpectedMessage()
        {
            return StringResources.GetErrorResource(TexlStrings.ErrBehaviorPropertyExpected).GetSingleValue(ErrorResource.ShortMessageTag);
        }
    }
}
