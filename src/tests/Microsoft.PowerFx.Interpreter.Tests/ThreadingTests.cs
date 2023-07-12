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

        // $$$ Supersedes ImmutabilityTests.
        // This is more aggressive (includes private fields), but they don't all pass. So assert is disabled.
        // Run this test under a debugger, and failure list is written to Debugger output window.
        // Per https://github.com/microsoft/Power-Fx/issues/1519, enable assert here. 
        [Fact]
        public void CheckImmutableTypeInInterpreter()
        {
            // Per https://github.com/microsoft/Power-Fx/issues/1519,
            // Add ThreadSafeImmutable and get these to pass. 
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(Core.IR.Nodes.IntermediateNode));
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(ReadOnlySymbolValues));
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(ComposedReadOnlySymbolValues));
            AnalyzeThreadSafety.VerifyThreadSafeImmutable(typeof(ParsedExpression));

            var assemblies = new Assembly[] 
            {
                typeof(RecalcEngine).Assembly,
                typeof(Types.FormulaType).Assembly
            };

            AnalyzeThreadSafety.CheckImmutableTypes(assemblies);
        }
    }
}
