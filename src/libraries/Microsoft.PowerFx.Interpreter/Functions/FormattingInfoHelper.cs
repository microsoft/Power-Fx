// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.PowerFx.Functions
{
    internal static class FormattingInfoHelper
    {
        internal static FormattingInfo CreateFormattingInfo() => new FormattingInfo(CultureInfo.InvariantCulture, TimeZoneInfo.Local);

        internal static FormattingInfo FromServiceProvider(IServiceProvider serviceProvider) => new FormattingInfo(serviceProvider.GetService<CultureInfo>(), serviceProvider.GetService<TimeZoneInfo>());

        public static FormattingInfo FromEvalVisitor(EvalVisitor runner) => new FormattingInfo(runner.CultureInfo, runner.TimeZoneInfo);
    }
}
