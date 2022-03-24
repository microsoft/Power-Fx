// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core.Utils
{
    // DName refers to a string that is valid as the name of a table/column.
    // That is any string that:
    // - does not consist entirely of space characters.
    [ThreadSafeImmutable]
    public struct DName : ICheckable, IEquatable<DName>, IEquatable<string>
    {
        private const string StrUnderscore = "_";
        private const char ChSpace = ' ';
        private readonly string _value;

        public DName(string value)
        {
            Contracts.Assert(IsValidDName(value));
            _value = value;
        }

        public string Value => _value ?? string.Empty;

        public bool IsValid => _value != null;

        public static implicit operator string(DName name) => name.Value;

        public override string ToString()
        {
            return Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Contracts.AssertValueOrNull(obj);

            if (!(obj is DName))
            {
                return false;
            }

            return Equals((DName)obj);
        }

        public bool Equals(DName other)
        {
            return Value == other.Value;
        }

        public bool Equals(string other)
        {
            Contracts.AssertValueOrNull(other);
            return Value == other;
        }

        public static bool operator ==(DName name1, DName name2) => name1.Value == name2.Value;

        public static bool operator ==(string str, DName name)
        {
            Contracts.AssertValueOrNull(str);
            return str == name.Value;
        }

        public static bool operator ==(DName name, string str)
        {
            Contracts.AssertValueOrNull(str);
            return name.Value == str;
        }

        public static bool operator !=(DName name1, DName name2) => name1.Value != name2.Value;

        public static bool operator !=(string str, DName name)
        {
            Contracts.AssertValueOrNull(str);
            return str != name.Value;
        }

        public static bool operator !=(DName name, string str)
        {
            Contracts.AssertValueOrNull(str);
            return name.Value != str;
        }

        // Returns whether the given name is a valid DName as defined above.
        public static bool IsValidDName(string strName)
        {
            Contracts.AssertValueOrNull(strName);

            if (string.IsNullOrEmpty(strName))
            {
                return false;
            }

            for (var i = 0; i < strName.Length; i++)
            {
                var ch = strName[i];
                if (!CharacterUtils.IsSpace(ch))
                {
                    return true;
                }
            }

            return false;
        }

        // Takes a name and makes it into a valid DName
        // If the name contains all spaces, an underscore is prepended to the name.
        // Returns whether it had to be changed to be a valid DName in the fModified arg.
        public static DName MakeValid(string strName, out bool fModified)
        {
            Contracts.AssertValueOrNull(strName);

            if (string.IsNullOrEmpty(strName))
            {
                fModified = true;
                return new DName(StrUnderscore);
            }

            var fAllSpaces = true;
            fModified = false;

            for (var i = 0; i < strName.Length; i++)
            {
                fAllSpaces = fAllSpaces && (strName[i] == ChSpace);
            }

            if (!fAllSpaces)
            {
                return new DName(strName);
            }

            fModified = true;

            return new DName(StrUnderscore + strName);
        }
    }
}
