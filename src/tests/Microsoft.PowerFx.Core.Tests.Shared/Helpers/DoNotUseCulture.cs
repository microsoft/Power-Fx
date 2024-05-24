// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Microsoft.PowerFx
{
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations

    // Used to verify that that we're not accessing this culture. 
    // For example, if we want to verify we don't access Thread.CurrentCulture. 
    // then just set it to DoNotUseCulture and any access will throw. 
    [DebuggerDisplay("DoNotUse Culture")]
    public class DoNotUseCulture : CultureInfo
    {
        public DoNotUseCulture()
            : base("en-US")
        {
        }

        public override Calendar Calendar => throw new NotImplementedException();

        public override object Clone() => throw new NotImplementedException();

        public override CompareInfo CompareInfo => throw new NotImplementedException();

        public override DateTimeFormatInfo DateTimeFormat { get => throw new NotImplementedException(); set => base.DateTimeFormat = value; }

        // This one is special - used by parser. 
        public override NumberFormatInfo NumberFormat { get => throw new NotImplementedException(); set => base.NumberFormat = value; }

        public override string DisplayName => throw new NotImplementedException();

        public override string EnglishName => throw new NotImplementedException();

        public override bool Equals(object value) => throw new NotImplementedException();

        public override object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                // Commonly used for formatting errors...
                // $$$ Shouldn't call this either - it means we're processing error messages eagerly
                return base.GetFormat(formatType);
            }

            throw new NotImplementedException();
        }

        public override int GetHashCode() => throw new NotImplementedException();

        public override bool IsNeutralCulture => throw new NotImplementedException();

        public override int KeyboardLayoutId => throw new NotImplementedException();

        public override int LCID => throw new NotImplementedException();

        public override string Name => throw new NotImplementedException();

        public override string NativeName => throw new NotImplementedException();

        public override TextInfo TextInfo => throw new NotImplementedException();

        public override Calendar[] OptionalCalendars => throw new NotImplementedException();

        public override CultureInfo Parent => throw new NotImplementedException();

        public override string ThreeLetterISOLanguageName => throw new NotImplementedException();

        public override string ThreeLetterWindowsLanguageName => throw new NotImplementedException();

        public override string TwoLetterISOLanguageName => throw new NotImplementedException();

        public override string ToString() => throw new NotImplementedException();
    }
}
