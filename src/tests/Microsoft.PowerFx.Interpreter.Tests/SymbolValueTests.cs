// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SymbolValueTests
    {
        // Mutating variables still invalidates a symbol table.
        [Fact]
        public async Task MutationProtectionForSymbolValues()
        {
            var locals = new SymbolValues();
            var s1 = locals.GetSymbolTableSnapshot();
            var v1 = s1.VersionHash;

            locals.Add("local", FormulaValue.New(3));

            // Symbol table we handed out is now stale. 
            var v1b = s1.VersionHash;
            Assert.NotEqual(v1, v1b);

            // Fetch a new current one.
            var s2 = locals.GetSymbolTableSnapshot();
            var v2 = s2.VersionHash;

            Assert.NotEqual(v1, v2);
        }

        [Fact]
        public void Test()
        {
            var locals = new SymbolValues();
            locals.Add("a", FormulaValue.New(2));

            // Duplicate add fails 
            Assert.Throws<ArgumentException>(() => locals.Add("a", FormulaValue.New(2)));

            var ret = locals.TryGetValue("a", out var v1);
            Assert.True(ret);
            Assert.Equal(2.0, v1.ToObject());

            ret = locals.TryGetValue("missing", out v1);
            Assert.False(ret);
            Assert.Null(v1);

            var symbols = locals.GetSymbolTableSnapshot();
            Assert.Null(symbols.Parent);

            // Internal hook is null;
            var a = new DName("a");
            ret = symbols.TryLookup(a, out var info);
            Assert.False(ret);

            // Lookup via name resolver succeeds.
            INameResolver resolver = symbols;
            ret = resolver.Lookup(a, out info);
            Assert.True(ret);
            Assert.Equal(Core.Types.DKind.Number, info.Type.Kind);
        }

        [Fact]
        public void ShadowValues()
        {
            var r1 = new SymbolValues();
            var r2 = new SymbolValues
            {
                Parent = r1
            };
            r1.Add("x", FormulaValue.New(2));
            r1.Add("y", FormulaValue.New(3));

            r2.Add("x", FormulaValue.New("str")); // Shadows r1.X

            // Shadowed            
            var ret = r1.TryGetValue("x", out var v1);
            Assert.True(ret);
            Assert.Equal(FormulaType.Number, v1.Type);

            ret = r2.TryGetValue("x", out v1);
            Assert.True(ret);
            Assert.Equal(FormulaType.String, v1.Type);

            // Inherits
            ret = r2.TryGetValue("y", out v1);
            Assert.True(ret);
            Assert.Equal(FormulaType.Number, v1.Type);
        }

        [Fact]
        public void Services()
        {
            var service1 = new MyService();
            var r1 = new SymbolValues();

            r1.AddService(service1);

            // Lookup succeeds
            var lookup = r1.GetService(typeof(MyService));
            Assert.Same(lookup, service1);

            var r2 = new SymbolValues()
            {
                Parent = r1
            };

            // Finds in child
            lookup = r2.GetService(typeof(MyService));
            Assert.Same(lookup, service1);

            // Shadowing 
            var service2 = new MyService();
            Assert.NotSame(service1, service2);

            r2.AddService(service2);

            lookup = r2.GetService(typeof(MyService));
            Assert.Same(lookup, service2);
        }

        private class BaseService
        {
        }

        private class MyService : BaseService
        {   
        }

        [Fact]
        public void Derived()
        {
            var derivedService = new MyService();
            var r1 = new SymbolValues();
            
            r1.AddService(derivedService);

            // Lookup must be exact type; doesn't lookup by base class.
            var lookup = r1.GetService(typeof(BaseService));
            Assert.Null(lookup);

            // Base and derived can coexist 
            BaseService baseService = new MyService();
            r1.AddService(baseService);
            
            lookup = r1.GetService(typeof(BaseService));
            Assert.Same(baseService, lookup);

            lookup = r1.GetService(typeof(MyService));
            Assert.Same(derivedService, lookup);
        }
    }
}
