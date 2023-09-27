// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Schemas
{
    internal class ControlTokenDictionary : IEnumerable<KeyValuePair<string, ControlToken>>
    {      
        private readonly IDictionary<string, ControlToken> _controlTokens;

        public ControlTokenDictionary()
        {
            _controlTokens = new Dictionary<string, ControlToken>();
        }

        public void Add(string controlName, ControlToken controlToken)
        {
            _controlTokens.Add(controlName, controlToken);
        }

        public IEnumerator<KeyValuePair<string, ControlToken>> GetEnumerator()
        {
            return _controlTokens.GetEnumerator();
        }

        public bool Contains(string controlName)
        {
            return _controlTokens.ContainsKey(controlName);
        }

        public ControlToken Get(string controlName)
        {
            if (_controlTokens.TryGetValue(controlName, out var controlToken))
            {
                return controlToken;
            }

            return null;
        }

        public IEnumerable<ControlToken> GetControlTokens()
        {
            return _controlTokens.Values;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
