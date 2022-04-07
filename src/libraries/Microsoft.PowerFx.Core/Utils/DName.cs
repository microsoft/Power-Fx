// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// A string representing a valid name of a table, column or variable name.
    /// A valid name does not consist entirely of space characters.
    /// </summary>
    [ThreadSafeImmutable]
    public struct DName : ICheckable, IEquatable<DName>, IEquatable<string>
    {
        private const string StrUnderscore = "_";
        private const char ChSpace = ' ';
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DName"/> struct.
        /// </summary>
        /// <param name="value">The value of the name.</param>
        public DName(string value)
        {
            Contracts.Assert(IsValidDName(value));
            _value = value;
        }

        /// <summary>
        /// The value of the name.
        /// </summary>
        public string Value => _value ?? string.Empty;

        /// <summary>
        /// Whether the name is valid.
        /// </summary>
        public bool IsValid => _value != null;

        /// <summary>
        /// String representation of the name value.
        /// </summary>
        /// <param name="name"></param>
        public static implicit operator string(DName name) => name.Value;

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            Contracts.AssertValueOrNull(obj);

            if (!(obj is DName))
            {
                return false;
            }

            return Equals((DName)obj);
        }

        /// <summary>
        /// Whether two names are equal.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(DName other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Whether the name is equal to a string value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns whether the given name is a valid <see cref="DName" />. 
        /// </summary>
        /// <param name="strName"></param>
        /// <returns></returns>
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
                if (!char.IsWhiteSpace(ch))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Takes a name and makes it into a valid <see cref="DName" />.
        /// If the name contains all spaces, an underscore is prepended to the name.
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="fModified">Whether it had to be changed to be a valid <see cref="DName" />.</param>
        public static DName MakeValid(string strName, out bool fModified)
        {
            Contracts.AssertValueOrNull(strName);

            if (string.IsNullOrEmpty(strName))
            {
                fModified = true;
                return new DName(StrUnderscore);
            }

            var fAllSpaces = true;
            var fHasSpecialWhiteSpaceCharacters = false;
            
            fModified = false;

            for (var i = 0; i < strName.Length; i++)
            {
                bool fIsSpace = strName[i] == ChSpace;
                bool fIsWhiteSpace = char.IsWhiteSpace(strName[i]);
                fAllSpaces = fAllSpaces && fIsWhiteSpace;
                fHasSpecialWhiteSpaceCharacters = fHasSpecialWhiteSpaceCharacters || (fIsWhiteSpace && !fIsSpace);
            }

            if (fHasSpecialWhiteSpaceCharacters)
            {
                fModified = true;
                StringBuilder builder = new StringBuilder(strName.Length);

                for (int i = 0; i < strName.Length; i++)
                {
                    if (char.IsWhiteSpace(strName[i]))
                    {
                        builder.Append(ChSpace);
                    }
                    else
                    {
                        builder.Append(strName[i]);
                    }
                }

                strName = builder.ToString();
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
