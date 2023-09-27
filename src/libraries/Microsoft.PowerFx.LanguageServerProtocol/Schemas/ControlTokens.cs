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
    internal class ControlTokens 
    {      
        private readonly ICollection<ControlToken> _controlTokens;
        private readonly ICollection<string> _controlTokenNames;

        public ControlTokens()
        {
            _controlTokens = new List<ControlToken>();
            _controlTokenNames = new List<string>();
        }

        public void Add(ControlToken controlToken)
        {
            _controlTokens.Add(controlToken);
            _controlTokenNames.Add(controlToken.Name);
        }

        public bool Contains(string controlName)
        {
            return _controlTokenNames.Contains(controlName);
        }

        public ControlToken GetControlToken(string controlName)
        {
            return _controlTokens.Where((token) => token.Name == controlName).Last();
        }

        public IEnumerable<ControlToken> GetControlTokens()
        {
            return _controlTokens;
        }

        public int Size()
        {
            return _controlTokens.Count;
        }
    }
}
