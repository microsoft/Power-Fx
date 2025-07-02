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
        /// Gets the value of the name, or an empty string if the value is null.
        /// </summary>
        public string Value => _value ?? string.Empty;

        /// <summary>
        /// Gets a value indicating whether the name is valid.
        /// </summary>
        public bool IsValid => _value != null;

        /// <summary>
        /// Converts the <see cref="DName"/> to its string representation.
        /// </summary>
        /// <param name="name">The <see cref="DName"/> instance.</param>
        /// <returns>The string value of the name.</returns>
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
        /// Determines whether the specified <see cref="DName"/> is equal to the current <see cref="DName"/>.
        /// </summary>
        /// <param name="other">The <see cref="DName"/> to compare with the current <see cref="DName"/>.</param>
        /// <returns>true if the specified <see cref="DName"/> is equal to the current <see cref="DName"/>; otherwise, false.</returns>
        public bool Equals(DName other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Determines whether the value of the current <see cref="DName"/> is equal to the specified string.
        /// </summary>
        /// <param name="other">The string to compare with the current <see cref="DName"/>.</param>
        /// <returns>true if the value of the current <see cref="DName"/> is equal to the specified string; otherwise, false.</returns>
        public bool Equals(string other)
        {
            Contracts.AssertValueOrNull(other);
            return Value == other;
        }

        /// <summary>
        /// Determines whether two <see cref="DName"/> instances have the same value.
        /// </summary>
        /// <param name="name1">The first <see cref="DName"/> to compare.</param>
        /// <param name="name2">The second <see cref="DName"/> to compare.</param>
        /// <returns>true if the values of the two <see cref="DName"/> instances are equal; otherwise, false.</returns>
        public static bool operator ==(DName name1, DName name2) => name1.Value == name2.Value;

        /// <summary>
        /// Determines whether the specified string and <see cref="DName"/> have the same value.
        /// </summary>
        /// <param name="str">The string to compare.</param>
        /// <param name="name">The <see cref="DName"/> to compare.</param>
        /// <returns>true if the string and <see cref="DName"/> have the same value; otherwise, false.</returns>
        public static bool operator ==(string str, DName name)
        {
            Contracts.AssertValueOrNull(str);
            return str == name.Value;
        }

        /// <summary>
        /// Determines whether the specified <see cref="DName"/> and string have the same value.
        /// </summary>
        /// <param name="name">The <see cref="DName"/> to compare.</param>
        /// <param name="str">The string to compare.</param>
        /// <returns>true if the <see cref="DName"/> and string have the same value; otherwise, false.</returns>
        public static bool operator ==(DName name, string str)
        {
            Contracts.AssertValueOrNull(str);
            return name.Value == str;
        }

        /// <summary>
        /// Determines whether two <see cref="DName"/> instances have different values.
        /// </summary>
        /// <param name="name1">The first <see cref="DName"/> to compare.</param>
        /// <param name="name2">The second <see cref="DName"/> to compare.</param>
        /// <returns>true if the values of the two <see cref="DName"/> instances are not equal; otherwise, false.</returns>
        public static bool operator !=(DName name1, DName name2) => name1.Value != name2.Value;

        /// <summary>
        /// Determines whether the specified string and <see cref="DName"/> have different values.
        /// </summary>
        /// <param name="str">The string to compare.</param>
        /// <param name="name">The <see cref="DName"/> to compare.</param>
        /// <returns>true if the string and <see cref="DName"/> have different values; otherwise, false.</returns>
        public static bool operator !=(string str, DName name)
        {
            Contracts.AssertValueOrNull(str);
            return str != name.Value;
        }

        /// <summary>
        /// Determines whether the specified <see cref="DName"/> and string have different values.
        /// </summary>
        /// <param name="name">The <see cref="DName"/> to compare.</param>
        /// <param name="str">The string to compare.</param>
        /// <returns>true if the <see cref="DName"/> and string have different values; otherwise, false.</returns>
        public static bool operator !=(DName name, string str)
        {
            Contracts.AssertValueOrNull(str);
            return name.Value != str;
        }

        /// <summary>
        /// Returns whether the given name is a valid <see cref="DName" />. 
        /// </summary>
        /// <param name="strName">The name to validate.</param>
        /// <returns>true if the name is valid; otherwise, false.</returns>
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

        /// <summary>
        /// Takes a name and makes it into a valid <see cref="DName" />.
        /// If the name contains all spaces, an underscore is prepended to the name.
        /// </summary>
        /// <param name="strName">The name to validate and possibly modify.</param>
        /// <param name="fModified">Set to true if the name was modified to be valid; otherwise, false.</param>
        /// <returns>A valid <see cref="DName"/> instance.</returns>
        public static DName MakeValid(string strName, out bool fModified)
        {
            Contracts.AssertValueOrNull(strName);

            if (string.IsNullOrEmpty(strName))
            {
                fModified = true;
                return new DName(StrUnderscore);
            }

            var fAllSpaces = true;
            var fHasDisallowedWhiteSpaceCharacters = false;

            fModified = false;

            // $$$ Needs optimization
            for (var i = 0; i < strName.Length; i++)
            {
                var fIsSpace = strName[i] == ChSpace;
                var fIsDisallowedWhiteSpace = CharacterUtils.IsTabulation(strName[i]) || CharacterUtils.IsLineTerm(strName[i]);
                fAllSpaces = fAllSpaces && (fIsDisallowedWhiteSpace || fIsSpace);
                fHasDisallowedWhiteSpaceCharacters |= fIsDisallowedWhiteSpace;
            }

            if (fHasDisallowedWhiteSpaceCharacters)
            {
                fModified = true;
                var builder = new StringBuilder(strName.Length);

                for (var i = 0; i < strName.Length; i++)
                {
                    if (CharacterUtils.IsTabulation(strName[i]) || CharacterUtils.IsLineTerm(strName[i]))
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
