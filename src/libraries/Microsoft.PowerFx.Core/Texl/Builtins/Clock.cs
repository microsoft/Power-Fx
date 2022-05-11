// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class ClockFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ClockFunction(string functionInvariantName, TexlStrings.StringGetter functionDescription)
            : base(new DPath().Append(new DName(LanguageConstants.InvariantClockNamespace)), functionInvariantName, functionDescription, FunctionCategories.DateTime, DType.CreateTable(new TypedName(DType.String, new DName("Value"))), 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }

    // Clock.AmPm()
    internal sealed class AmPmFunction : ClockFunction
    {
        public AmPmFunction()
            : base("AmPm", TexlStrings.AboutClock__AmPm)
        {
        }
    }

    // Clock.AmPmShort()
    internal sealed class AmPmShortFunction : ClockFunction
    {
        public AmPmShortFunction()
            : base("AmPmShort", TexlStrings.AboutClock__AmPmShort)
        {
        }
    }

    // Clock.IsClock24()
    internal sealed class IsClock24Function : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public IsClock24Function()
            : base(new DPath().Append(new DName(LanguageConstants.InvariantClockNamespace)), "IsClock24", TexlStrings.AboutClock__IsClock24, FunctionCategories.DateTime, DType.Boolean, 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
