// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public.Config;

namespace Microsoft.PowerFx.Core.Tests
{
    internal class InternalSetup
    {
        internal string HandlerName { get; set; }

        internal TexlParser.Flags Flags { get; set; }

        internal Feature Feature { get; set; }

        internal static InternalSetup Parse(string setupHandlerName)
        {
            var iSetup = new InternalSetup();

            if (string.IsNullOrWhiteSpace(setupHandlerName))
            {
                return iSetup;
            }
            
            var parts = setupHandlerName.Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
            
            foreach (var part in parts.ToArray())
            {
                if (Enum.TryParse<TexlParser.Flags>(part, out var flag))
                {
                    iSetup.Flags |= flag;
                    parts.Remove(part);
                }
                else if (Enum.TryParse<Feature>(part, out var f))
                {
                    iSetup.Feature |= f;
                    parts.Remove(part); 
                }                
            }

            if (parts.Count > 1)
            {
                throw new ArgumentException("Too many setup handler names!");
            }
            
            iSetup.HandlerName = parts.FirstOrDefault();            
            return iSetup;
        }
    }
}
