// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Parser;

namespace Microsoft.PowerFx.Interpreter.Tests.Helpers
{
    internal class InternalSetup
    {
        internal string HandlerName { get; set; }

        internal TexlParser.Flags Flags { get; set; }

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
            }

            if (parts.Count > 1)
            {
                throw new ArgumentException("Too many setup handler names!");
            }
            else
            {
                iSetup.HandlerName = parts.FirstOrDefault();
            }

            return iSetup;
        }
    }
}
