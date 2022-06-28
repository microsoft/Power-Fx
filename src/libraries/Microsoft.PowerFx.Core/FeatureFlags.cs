// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Preview
{
    /// <summary>
    /// Hosts can enable these flags to try out early features. 
    /// Any flags will eventually default to true and then get removed once the feature is finalized. 
    /// Removing a flag is a breaking change and requires a semver update. 
    /// Flags can only be set once. 
    /// </summary>
    public static class FeatureFlags
    {
        /// <summary>
        /// allows setting flags as will during tests.
        /// </summary>
        internal static bool _inTests = false;

        private static bool _stringInterpolation = false;
        private static bool _noValueInRecordArrays = false;

        /// <summary>
        /// Enable String Interpolation feature. 
        /// Added 12/3/2021.
        /// </summary>
        public static bool StringInterpolation
        {
            get => _stringInterpolation;
            set
            {
                // Once flipped, can't unflip. 
                if (!value && !_inTests) 
                { 
                    throw new NotSupportedException("Can't change field after it's set");
                }

                _stringInterpolation = value;
            }
        }

        public static bool NoValueInRecordArrays
        {
            get => _noValueInRecordArrays;
            set
            {
                // Once flipped, can't unflip. 
                if (!value && !_inTests)
                {
                    throw new NotSupportedException("Can't change field after it's set");
                }

                _noValueInRecordArrays = value;
            }
        }
    }
}
