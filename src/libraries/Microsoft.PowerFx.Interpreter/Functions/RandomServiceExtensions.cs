// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.PowerFx.Functions
{
    internal static class RandomServiceExtensions
    {
        public static decimal NextDecimal(this IRandomService random)
        {
            // Decimal can have up to 29 digits, but with 29 digits it would only go to a 7.9 mantissa. So limiting it to 28 significant digits gives a uniform distribution
            var randChars = new char[30];
            randChars[0] = '0';
            randChars[1] = '.';

            for (int i = 2; i < 30; i++)
            {
                randChars[i] = (char)(48.0 + (random.NextDouble() * 10.0));
            }

            var randStr = new string(randChars);
            if (decimal.TryParse(randStr, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalResult))
            {
                return decimalResult;
            }

            return (decimal)random.NextDouble();
        }
    }
}
