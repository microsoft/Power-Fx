﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
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

        [Fact]
        public void TestRowScope()
        {
            var r1 = new SymbolValues();
            r1.Add("b", FormulaValue.New(1));
            Assert.Equal("(RuntimeValues)", r1.DebugName);

            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("a", FormulaValue.New(10)));

            var r2 = ReadOnlySymbolValues.NewRowScope(record, r1);
            Assert.Equal("(rowScope),(RuntimeValues)", r2.DebugName);

            var engine = new RecalcEngine();

            var result = engine.EvalAsync("ThisRecord.a + a + b", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(21.0, result.ToObject());
        }

        [Fact]
        public void TestRowScopeNoParent()
        {
            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("a", FormulaValue.New(10)));

            var r2 = ReadOnlySymbolValues.NewRowScope(record);
            Assert.Equal("(rowScope)", r2.DebugName);

            var engine = new RecalcEngine();

            var result = engine.EvalAsync("ThisRecord.a + a", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(20.0, result.ToObject());
        }

        // Ensure the RowScope is lazy and doesn't call fields. 
        [Fact]
        public void TestRowScopeLazy()
        {
            var r1 = new SymbolValues();
            r1.Add("b", FormulaValue.New(1));

            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("a", FormulaValue.New(10)));

            record = LazyType.Wrap(record); // Ensure we don't call RecordType.Fields

            var r2 = ReadOnlySymbolValues.NewRowScope(record, r1);

            var table = r2.GetSymbolTableSnapshot();
            
            var engine = new RecalcEngine();

            var result = engine.EvalAsync("ThisRecord.a + a + b", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(21.0, result.ToObject());
        }

        // Type to ensure we don't call Fields. 
        private class LazyType : RecordType
        {
            private readonly RecordType _inner;

            public LazyType(RecordType inner)
            {
                _inner = inner;
            }

            // Wrap the existing record, but ensure we don't call FieldNames on it. 
            // This ensures operations stay lazy. 
            public static RecordValue Wrap(RecordValue record)
            {
                return FormulaValue.NewRecordFromFields(new LazyType(record.Type), record.Fields);
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                return _inner.TryGetFieldType(name, out type);
            }

            // Explicit don't enumerate fields
            public override IEnumerable<string> FieldNames => throw new NotImplementedException();

            public override bool Equals(object other) => throw new NotImplementedException();
            
            public override int GetHashCode() => throw new NotImplementedException();
        }

        // Test Composing symbol tables. 
        [Fact]
        public void Compose()
        {
            var culture1 = new CultureInfo("en-us");

            var r1 = new SymbolValues();
            r1.Add("x", FormulaValue.New("x1"));
            r1.AddService(culture1);

            var r2 = new SymbolValues();
            r2.Add("y", FormulaValue.New("y2"));
            r2.Add("x", FormulaValue.New("x2"));

            // Compose 
            var r3 = ReadOnlySymbolValues.Compose(r1, r2);

            var found = r3.TryGetValue("x", out var result);
            Assert.True(found);
            Assert.Equal("x1", result.ToObject());

            found = r3.TryGetValue("y", out result);
            Assert.True(found);
            Assert.Equal("y2", result.ToObject());

            found = r3.TryGetValue("missing", out result);
            Assert.False(found);
            Assert.Null(result);

            var culture2 = r3.GetService<CultureInfo>();
            Assert.Same(culture1, culture2);

            // Flipping the order changes precedence
            r3 = ReadOnlySymbolValues.Compose(r2, r1);
            found = r3.TryGetValue("x", out result);
            Assert.True(found);
            Assert.Equal("x2", result.ToObject());

            culture2 = r3.GetService<CultureInfo>();
            Assert.Same(culture1, culture2);           
        }

        [Fact]
        public void ComposeNest()
        {
            var r1 = new SymbolValues();
            r1.Add("x", FormulaValue.New("x1"));

            var r2 = new SymbolValues();
            r2.Add("y", FormulaValue.New("y2"));

            var r3 = new SymbolValues();
            r3.Add("z", FormulaValue.New("z3"));

            // Compose can nest
            var r2_3 = ReadOnlySymbolValues.Compose(r2, r3);

            var rAll = ReadOnlySymbolValues.Compose(r1, r3);

            var found = rAll.TryGetValue("z", out var result);
            Assert.True(found);
            Assert.Equal("z3", result.ToObject());
        }

        [Fact]
        public void TestNew()
        {
            var dict = new Dictionary<string, NumberValue>
            {
                { "a", FormulaValue.New(1) },
                { "b", FormulaValue.New(10) }
            };

            var r1 = new SymbolValues();
            r1.Add("global", FormulaValue.New(100));

            var r2 = ReadOnlySymbolValues.New(dict, r1);

            var engine = new RecalcEngine();
            var result = engine.EvalAsync("a + b + global", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(111.0, result.ToObject());
        }
    }
}
