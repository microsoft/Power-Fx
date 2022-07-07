// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    // Represents an intellisense suggestion.
    // Implements IComparable as Suggestion will be used in a List that will be sorted.
    // List.Sort() calls CompareTo.
    internal sealed class IntellisenseSuggestion : IComparable<IntellisenseSuggestion>, IEquatable<IntellisenseSuggestion>, IIntellisenseSuggestion
    {
        private readonly List<IIntellisenseSuggestion> _overloads;
        private int _argIndex;

        /// <summary>
        /// This is valid if the current kind is of SuggestionKind.Function, else -1.
        /// This is used to filter out suggestions that have less arguments than ArgIndex.
        /// </summary>
        private readonly int _argCount;

        /// <summary>
        /// Gets the sort priority for this suggestion. 0 is lowest priority.
        /// </summary>
        internal uint SortPriority { get; set; }

        /// <summary>
        /// Gets the text form of the DisplayText  for the suggestion.
        /// </summary>
        internal string Text { get; }

        /// <summary>
        /// This is an internal field that stores the simple function name if the suggestion is a Function.
        /// </summary>
        internal string FunctionName { get; private set; }

        /// <summary>
        /// This is an internal field that stores the type of the suggestion.
        /// </summary>
        internal DType Type { get; private set; }

        /// <summary>
        /// The exact matching string. This is used for sorting the suggestions.
        /// </summary>
        public string ExactMatch { get; private set; }

        /// <summary>
        /// Description for the current function parameter.
        /// For example: This is used to provide parameter description for the highlighted function parameter.
        /// </summary>
        public string FunctionParameterDescription { get; private set; }

        /// <summary>
        /// Description, suitable for UI consumption.
        /// </summary>
        public string Definition { get; private set; }

        /// <summary>
        /// A boolean value indicating if the suggestion matches the expected type in the rule.
        /// </summary>
        public bool IsTypeMatch { get; private set; }

        /// <summary>
        /// A boolean value indicating if the suggestion is the primary output property.
        /// </summary>
        public bool ShouldPreselect { get; private set; }

        /// <summary>
        /// Returns the list of suggestions for the overload of the function.
        /// This is populated only if the suggestion kind is a function and if the function has overloads.
        /// </summary>
        public IEnumerable<IIntellisenseSuggestion> Overloads => _overloads;

        /// <summary>
        /// The Kind of Suggestion.
        /// </summary>
        public SuggestionKind Kind { get; private set; }

        /// <summary>
        /// What kind of icon to display next to the suggestion.
        /// </summary>
        public SuggestionIconKind IconKind { get; private set; }

        /// <summary>
        /// This is the string that will be displayed to the user.
        /// </summary>
        public UIString DisplayText { get; internal set; }

        /// <summary>
        /// Indicates if there are errors.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// This is valid only if the Kind is Function, else -1.
        /// </summary>
        public int ArgIndex
        {
            get => _argIndex;

            internal set
            {
                Contracts.Assert(value >= 0);
                if (Kind == SuggestionKind.Function)
                {
                    _argIndex = value;
                    foreach (IntellisenseSuggestion s in _overloads)
                    {
                        s.ArgIndex = value;
                    }
                }
            }
        }

        public IntellisenseSuggestion(UIString text, SuggestionKind kind, SuggestionIconKind iconKind, DType type, int argCount, string definition, string functionName, string functionParamDescription)
            : this(text, kind, iconKind, type, string.Empty, argCount, definition, functionName, functionParamDescription, 0)
        {
        }

        public IntellisenseSuggestion(UIString text, SuggestionKind kind, SuggestionIconKind iconKind, DType type, string exactMatch, int argCount, string definition, string functionName, uint sortPriority = 0)
            : this(text, kind, iconKind, type, exactMatch, argCount, definition, functionName, string.Empty, sortPriority)
        {
        }

        public IntellisenseSuggestion(UIString text, SuggestionKind kind, SuggestionIconKind iconKind, DType type, string exactMatch, int argCount, string definition, string functionName, string functionParamDescription, uint sortPriority = 0)
        {
            Contracts.AssertValue(text);
            Contracts.AssertNonEmpty(text.Text);
            Contracts.AssertValid(type);
            Contracts.AssertValue(exactMatch);
            Contracts.AssertValue(definition);
            Contracts.Assert(argCount >= -1);
            Contracts.AssertValueOrNull(functionName);
            Contracts.AssertValueOrNull(functionParamDescription);

            DisplayText = text;
            _overloads = new List<IIntellisenseSuggestion>();
            Text = text.Text;
            Kind = kind;
            IconKind = iconKind;
            Type = type;
            _argIndex = -1;
            ExactMatch = exactMatch;
            _argCount = argCount;
            FunctionName = functionName;
            SortPriority = sortPriority;
            ShouldPreselect = sortPriority != 0;

            FunctionParameterDescription = functionParamDescription ?? string.Empty;
            Definition = definition;
            IsTypeMatch = false;
        }

        public IntellisenseSuggestion(TexlFunction function, string exactMatch, UIString displayText)
            : this(displayText, SuggestionKind.Function, SuggestionIconKind.Function, function.VerifyValue().ReturnType, exactMatch, -1, function.Description, function.VerifyValue().Name, 0)
        {
            Contracts.AssertValue(function);

            foreach (var signature in function.GetSignatures())
            {
                var count = 0;
                var argumentSeparator = string.Empty;
                var listSep = TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator + " ";
                var funcDisplayString = new StringBuilder(Text);
                funcDisplayString.Append('(');
                foreach (var arg in signature)
                {
                    funcDisplayString.Append(argumentSeparator);
                    funcDisplayString.Append(arg(null));
                    argumentSeparator = listSep;
                    count++;
                }

                funcDisplayString.Append(')');
                _overloads.Add(new IntellisenseSuggestion(new UIString(funcDisplayString.ToString()), SuggestionKind.Function, SuggestionIconKind.Function, function.VerifyValue().ReturnType, exactMatch, count, function.Description, function.Name, 0));
            }
        }

        // For debugging.
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", Text, Kind);
        }

        // For debugging.
        internal void AppendTo(StringBuilder sb)
        {
            Contracts.AssertValue(sb);
            if (Kind == SuggestionKind.Function)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Function {0}, arg index {1}", Text, ArgIndex));
                sb.AppendLine("Overloads:");
                foreach (var overload in _overloads)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, overload.DisplayText.ToString()));
                }
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}", Text, Kind);
            }

            sb.AppendLine();
        }

        public int CompareTo(IntellisenseSuggestion other)
        {
            Contracts.AssertValueOrNull(other);

            if (other == null)
            {
                return -1;
            }

            var thisIsExactMatch = IsExactMatch(Text, ExactMatch);
            var otherIsExactMatch = IsExactMatch(other.Text, other.ExactMatch);

            if (thisIsExactMatch && !otherIsExactMatch)
            {
                return -1;
            }
            else if (!thisIsExactMatch && otherIsExactMatch)
            {
                return 1;
            }

            if (SortPriority != other.SortPriority)
            {
                return (int)(other.SortPriority - SortPriority);
            }

            return CultureInfo.InvariantCulture.CompareInfo.Compare(Text, other.Text, CompareOptions.None);
        }

        public bool Equals(IntellisenseSuggestion other)
        {
            Contracts.AssertValueOrNull(other);

            if (other == null)
            {
                return false;
            }

            // REVIEW ragru/hekum: Here comparing the _overloads is not necessary because all the possible overloads for a
            // function are gathered under one name and hence there won't be 2 function suggestions with the same name.
            return Text == other.Text
                && Type.Equals(other.Type)
                && FunctionName == other.FunctionName
                && _argCount == other._argCount
                && _argIndex == other._argIndex;
        }

        public override bool Equals(object other)
        {
            Contracts.AssertValueOrNull(other);

            if (other == null)
            {
                return false;
            }

            var otherSuggestion = other as IntellisenseSuggestion;
            if (otherSuggestion == null)
            {
                return false;
            }
            else
            {
                return Equals(otherSuggestion);
            }
        }

        public override int GetHashCode()
        {
            var funcHashCode = FunctionName == null ? 0 : FunctionName.GetHashCode();
            return Text.GetHashCode() ^ Type.GetHashCode() ^ funcHashCode ^ _argCount ^ _argIndex;
        }

        public static bool operator ==(IntellisenseSuggestion suggestion1, IntellisenseSuggestion suggestion2)
        {
            if ((object)suggestion1 == null || ((object)suggestion2) == null)
            {
                return Equals(suggestion1, suggestion2);
            }

            return suggestion1.Equals(suggestion2);
        }

        public static bool operator !=(IntellisenseSuggestion suggestion1, IntellisenseSuggestion suggestion2)
        {
            if (suggestion1 == null || suggestion2 == null)
            {
                return !Equals(suggestion1, suggestion2);
            }

            return !suggestion1.Equals(suggestion2);
        }

        // Removes the function overloads which have args less than the given value
        internal void RemoveOverloads(int argCount)
        {
            Contracts.Assert(argCount >= 0);

            if (Kind == SuggestionKind.Function)
            {
                foreach (IntellisenseSuggestion a in _overloads)
                {
                    if (a._argCount < argCount)
                    {
                        _overloads.Remove(a);
                    }
                }
            }
        }

        // Adds the function overloads to the overload list.
        internal void AddOverloads(IEnumerable<IIntellisenseSuggestion> suggestions)
        {
            Contracts.AssertValue(suggestions);

            _overloads.AddRange(suggestions);
        }

        // Sets the value of IsTypeMatch to true.
        internal void SetTypematch()
        {
            IsTypeMatch = true;
        }

        private bool IsExactMatch(string input, string match)
        {
            Contracts.AssertValue(input);
            Contracts.AssertValue(match);
            return input.Equals(match, StringComparison.OrdinalIgnoreCase);
        }
    }
}
