// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Localization
{
    /// <summary>
    /// Key Type of string resources related to errors. 
    /// Used by BaseError in DocError.cs to ensure that it is passed a key as opposed to a generic string, such as the contents of the error message. 
    /// Existing keys for error messages are split between here (for general document errors) and Strings.cs (for Texl errors).
    /// </summary>
    [ThreadSafeImmutable]
    public struct ErrorResourceKey : IEquatable<ErrorResourceKey>
    {
        public string Key { get; }

        public ErrorResourceKey(string key)
        {
            Contracts.AssertNonEmpty(key);

            Key = key;
        }

        public bool Equals(ErrorResourceKey other)
        {
            return other.Key == Key;
        }

        public override bool Equals(object obj)
        {
            return obj is ErrorResourceKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public static bool operator ==(ErrorResourceKey left, ErrorResourceKey right) => string.Equals(left.Key, right.Key, StringComparison.Ordinal);

        public static bool operator !=(ErrorResourceKey left, ErrorResourceKey right) => !string.Equals(left.Key, right.Key, StringComparison.Ordinal);
    }
}
