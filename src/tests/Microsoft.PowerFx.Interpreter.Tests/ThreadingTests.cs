// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Do static analysis to look for potential threading issues. 
    public class ThreadingTests
    {
        [Fact]
        public void CheckInterpreter()
        {
            var asm = typeof(RecalcEngine).Assembly;
            var bugsFieldType = new HashSet<Type>();
            var bugNames = new HashSet<string>();

            AnalyzeThreadSafety.CheckStatics(asm, bugsFieldType, bugNames);
        }

        // $$$ Supersedes ImmutabilityTests. This is more aggressive (incldues private fields). 
        [Fact]
        public void CheckImmutableType()
        {            
            var asm1 = typeof(RecalcEngine).Assembly;
            var asm = typeof(Types.FormulaType).Assembly;

            foreach (Type type in asm.GetTypes().Concat(asm1.GetTypes()))
            {
                var attr = type.GetCustomAttribute<ThreadSafeImmutableAttribute>();
                if (attr == null)
                {
                    continue;
                }
                                
                bool ok = AnalyzeThreadSafety.VerifyThreadSafeImmutable(type);
            }
        }
    }
}
