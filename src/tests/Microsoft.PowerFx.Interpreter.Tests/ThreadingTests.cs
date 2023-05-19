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
            // https://github.com/microsoft/Power-Fx/issues/1519
            // Add ThreadSafeImmutable and get these to pass. 
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(Core.IR.Nodes.IntermediateNode));
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(ReadOnlySymbolValues));
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(ComposedReadOnlySymbolValues));
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(ParsedExpression));

            var asm1 = typeof(RecalcEngine).Assembly;
            var asm = typeof(Types.FormulaType).Assembly;

            foreach (Type type in asm.GetTypes().Concat(asm1.GetTypes()))
            {
                // includes base types 
                var attr = type.GetCustomAttribute<ThreadSafeImmutableAttribute>();
                if (attr == null)
                {
                    continue;
                }

                // Common pattern is a writeable derived type (like Dict vs. IReadOnlyDict). 
                var attrNotSafe = type.GetCustomAttribute<NotThreadSafeAttribute>(inherit: false);
                if (attrNotSafe != null)
                {
                    attr = type.GetCustomAttribute<ThreadSafeImmutableAttribute>(inherit: false);
                    if (attr != null)
                    {
                        Assert.True(false); // Class can't have both safe & unsafe together. 
                    }

                    continue;
                }

                bool ok = AnalyzeThreadSafety.VerifyThreadSafeImmutable(type);

                // https://github.com/microsoft/Power-Fx/issues/1519
                // Assert.True(ok);                
            }
        }
    }
}
