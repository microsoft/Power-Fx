// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Schemas
{
    public class ControlTokens : IEnumerable<ControlToken>
    {      
        private readonly ICollection<ControlToken> _controlTokens;
        private readonly IDictionary<string, ControlToken> _controlTokenDict;

        public ControlTokens()
        {
            _controlTokens = new List<ControlToken>();
            _controlTokenDict = new Dictionary<string, ControlToken>();
        }

        public void Add(ControlToken controlToken)
        {
            _controlTokens.Add(controlToken);
            _controlTokenDict.Add(controlToken.Name, controlToken);
        }

        public bool Contains(string controlName)
        {
            return _controlTokenDict.ContainsKey(controlName);
        }

        public ControlToken GetControlToken(string controlName)
        {
            if (_controlTokenDict.TryGetValue(controlName, out ControlToken controlTokenObj))
            {
                return controlTokenObj;
            }

            return null;
        }

        public IEnumerator<ControlToken> GetEnumerator()
        {
            return _controlTokens.GetEnumerator();
        }

        public int Size()
        {
            return _controlTokens.Count;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
