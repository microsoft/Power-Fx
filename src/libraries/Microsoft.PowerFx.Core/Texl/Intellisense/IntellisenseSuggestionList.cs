// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Intellisense
{
    // Specialized IntellisenseSuggestion list that allows for some effient operations on the list.
    // For example, checking if the list contains a suggestion with a particular display name is
    // O(1) for this class instead of an O(N) search.
    internal sealed class IntellisenseSuggestionList : IList<IntellisenseSuggestion>
    {
        private readonly Dictionary<string, int> _displayNameToCount;
        private readonly Dictionary<string, List<IntellisenseSuggestion>> _textToSuggestions;
        private readonly List<IntellisenseSuggestion> _backingList;

        public int Count => ((IList<IntellisenseSuggestion>)_backingList).Count;

        public bool IsReadOnly => ((IList<IntellisenseSuggestion>)_backingList).IsReadOnly;

        public IntellisenseSuggestionList()
            : base()
        {
            _displayNameToCount = new Dictionary<string, int>();
            _textToSuggestions = new Dictionary<string, List<IntellisenseSuggestion>>();
            _backingList = new List<IntellisenseSuggestion>();
        }

        public IntellisenseSuggestion this[int index]
        {
            get => _backingList[index];
            set
            {
                DecrementDictionaries(this[index]);
                _backingList[index] = value;
                IncrementDictionaries(value);
            }
        }

        public void Add(IntellisenseSuggestion item)
        {
            IncrementDictionaries(item);
            _backingList.Add(item);
        }

        public void AddRange(IEnumerable<IntellisenseSuggestion> items)
        {
            foreach (var item in items)
            {
                IncrementDictionaries(item);
            }

            _backingList.AddRange(items);
        }

        public bool Remove(IntellisenseSuggestion item)
        {
            var result = _backingList.Remove(item);
            if (result)
            {
                DecrementDictionaries(item);
            }

            return result;
        }

        public void RemoveRange(int index, int count)
        {
            for (var i = index; i < index + count; i++)
            {
                DecrementDictionaries(this[i]);
            }

            _backingList.RemoveRange(index, count);
        }

        public void RemoveAt(int index)
        {
            DecrementDictionaries(this[index]);
            _backingList.RemoveAt(index);
        }

        public int RemoveAll(Predicate<IntellisenseSuggestion> pred)
        {
            foreach (var item in this)
            {
                if (pred(item))
                {
                    DecrementDictionaries(item);
                }
            }

            return _backingList.RemoveAll(pred);
        }

        public void Clear()
        {
            _backingList.Clear();
            _displayNameToCount.Clear();
            _textToSuggestions.Clear();
        }

        public void Insert(int index, IntellisenseSuggestion item)
        {
            IncrementDictionaries(item);
            _backingList.Insert(index, item);
        }

        public void InsertRange(int index, IEnumerable<IntellisenseSuggestion> collection)
        {
            foreach (var item in collection)
            {
                IncrementDictionaries(item);
            }

            _backingList.InsertRange(index, collection);
        }

        public bool ContainsSuggestion(string displayText)
        {
            return _displayNameToCount.ContainsKey(displayText);
        }

        public List<IntellisenseSuggestion> SuggestionsForText(string text)
        {
            return _textToSuggestions.ContainsKey(text) ? new List<IntellisenseSuggestion>(_textToSuggestions[text]) : new List<IntellisenseSuggestion>();
        }

        public void UpdateDisplayText(int index, UIString newText)
        {
            DecrementDictionaries(this[index]);
            this[index].DisplayText = newText;
            IncrementDictionaries(this[index]);
        }

        private void IncrementDictionaries(IntellisenseSuggestion item)
        {
            var displayText = item.DisplayText.Text;
            if (!_displayNameToCount.ContainsKey(displayText))
            {
                _displayNameToCount[displayText] = 0;
            }

            _displayNameToCount[displayText] += 1;

            var sugText = item.Text;
            if (!_textToSuggestions.ContainsKey(sugText))
            {
                _textToSuggestions[sugText] = new List<IntellisenseSuggestion>();
            }

            _textToSuggestions[sugText].Add(item);
        }

        private void DecrementDictionaries(IntellisenseSuggestion item)
        {
            Contracts.Assert(_displayNameToCount.ContainsKey(item.DisplayText.Text));
            Contracts.Assert(_textToSuggestions.ContainsKey(item.Text));

            var displayText = item.DisplayText.Text;
            _displayNameToCount[displayText] -= 1;
            if (_displayNameToCount[displayText] == 0)
            {
                _displayNameToCount.Remove(displayText);
            }

            var sugText = item.Text;
            _textToSuggestions[sugText].Remove(item);
            if (_textToSuggestions[sugText].Count() == 0)
            {
                _textToSuggestions.Remove(sugText);
            }
        }

        public int IndexOf(IntellisenseSuggestion item)
        {
            return ((IList<IntellisenseSuggestion>)_backingList).IndexOf(item);
        }

        public bool Contains(IntellisenseSuggestion item)
        {
            return ((IList<IntellisenseSuggestion>)_backingList).Contains(item);
        }

        public void CopyTo(IntellisenseSuggestion[] array, int arrayIndex)
        {
            ((IList<IntellisenseSuggestion>)_backingList).CopyTo(array, arrayIndex);
        }

        public IEnumerator<IntellisenseSuggestion> GetEnumerator()
        {
            return ((IList<IntellisenseSuggestion>)_backingList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<IntellisenseSuggestion>)_backingList).GetEnumerator();
        }

        public void Sort()
        {
            _backingList.Sort();
        }

        public int FindIndex(Predicate<IntellisenseSuggestion> pred)
        {
            return _backingList.FindIndex(pred);
        }
    }
}
