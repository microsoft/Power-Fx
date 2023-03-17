// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ServiceProviderTests
    {
        [Fact]
        public void Test()
        {
            var r1 = new BasicServiceProvider();
            Assert.Throws<ArgumentNullException>(() => r1.AddService<MyService>(null));

            Assert.Throws<ArgumentNullException>(() => r1.AddService(null, new MyService()));

            // Nulls are ignored, don't crash
            var r2 = new BasicServiceProvider(null);
            Assert.Null(r2.GetService(typeof(MyService)));

            r2 = new BasicServiceProvider((IServiceProvider[])null);
            Assert.Null(r2.GetService(typeof(MyService)));

            r2 = new BasicServiceProvider(new IServiceProvider[0]);
            Assert.Null(r2.GetService(typeof(MyService)));
        }

        [Fact]
        public void ServiceChaining()
        {
            var service1 = new MyService();
            var r1 = new BasicServiceProvider();
            var otherService = new OtherService();

            r1.AddService(service1);
            r1.AddService(otherService);

            // Lookup succeeds
            var lookup = r1.GetService(typeof(MyService));
            Assert.Same(lookup, service1);

            var r2 = new BasicServiceProvider();
            var r21 = new BasicServiceProvider(r2, null, r1);

            // Finds in child
            lookup = r21.GetService(typeof(MyService));
            Assert.Same(lookup, service1);

            // Shadowing 
            var service2 = new MyService();
            Assert.NotSame(service1, service2);

            r2.AddService(service2);

            var lookup1 = r1.GetService(typeof(MyService));
            Assert.Same(lookup1, service1);

            lookup = r2.GetService(typeof(MyService));
            Assert.Same(lookup, service2);

            lookup = r21.GetService(typeof(MyService));
            Assert.Same(lookup, service2);

            // Lookup other - found only in r1. 
            var other1 = r21.GetService(typeof(OtherService));
            Assert.Same(otherService, other1);

            // Missing 
            var notFound = r21.GetService(typeof(NotFoundService));
            Assert.Null(notFound);
        }

        private class BaseService
        {
        }

        private class MyService : BaseService
        {
        }

        private class OtherService
        {
        }

        private class NotFoundService
        {
        }

        // Composing 
        [Fact]
        public void Sharing()
        {
            var r1 = new BasicServiceProvider();
            var r2 = new BasicServiceProvider(r1);

            // Ensure wrapper doesn't just take a snapshot; returns live results. 
            var service1 = new MyService();

            r1.AddService(typeof(MyService), service1);
            var service2 = r2.GetService(typeof(MyService));

            Assert.Same(service1, service2);
        }

        [Fact]
        public void Mismatch()
        {
            var r1 = new BasicServiceProvider();
            Assert.Throws<InvalidOperationException>(() => r1.AddService(typeof(MyService), new OtherService()));
        }

        [Fact]
        public void Derived()
        {
            var derivedService = new MyService();
            var r1 = new BasicServiceProvider();

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

        [Theory]
        [InlineData("User.Name", true, "test")]
        [InlineData("User.Age", true, 21d)]
        [InlineData("User.SomeField", false, null)]
        public async Task AddHostObjectBasicTests(string expression, bool isCheckSuccess, object expected)
        {
            var symbol = new SymbolTable();
            
            var userType = RecordType.Empty()
                .Add("Name", FormulaType.String)
                .Add("Age", FormulaType.Number);

            symbol.AddHostObject("User", userType, GetUserObject);

            var config = new PowerFxConfig() { SymbolTable = symbol };
            var engine = new RecalcEngine(config);
            var check = engine.Check(expression);
            Assert.Equal(isCheckSuccess, check.IsSuccess);

            if (expected != null)
            {
                var runtimeConfig = new RuntimeConfig();
                runtimeConfig.AddService<User>(new User() { Name = "test", Age = 21 });

                var res = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig);
            
                Assert.Equal(expected, res.ToObject());
            }
        }

        [Fact]
        public async Task AddHostObjectTypeMismatchTest()
        {
            var symbol = new SymbolTable();

            var userType = RecordType.Empty()
                .Add("Name", FormulaType.String);

            symbol.AddHostObject("User", userType, GetUserObject);

            var config = new PowerFxConfig() { SymbolTable = symbol };
            var engine = new RecalcEngine(config);
            var check = engine.Check("User.Name");
            Assert.True(check.IsSuccess);

            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.AddService<User>(new User() { Name = "test", Age = 21 });

            var res = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig);
            Assert.IsType<ErrorValue>(res);
            Assert.NotNull(((ErrorValue)res).Errors.Where((error) => error.Kind.Equals(ErrorKind.InvalidArgument)));
        }

        [Fact]
        public async Task AddHostObjectCustomErrorTest()
        {
            var symbol = new SymbolTable();

            var userType = RecordType.Empty()
                .Add("Name", FormulaType.String)
                .Add("Age", FormulaType.Number);

            symbol.AddHostObject("User", userType, GetUserObject);

            var config = new PowerFxConfig() { SymbolTable = symbol };
            var engine = new RecalcEngine(config);
            var check = engine.Check("User.Name");
            Assert.True(check.IsSuccess);

            var runtimeConfig = new RuntimeConfig();

            var res = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig);
            Assert.IsType<ErrorValue>(res);
            Assert.NotNull(((ErrorValue)res).Errors.Where((error) => error.Kind.Equals(ErrorKind.Custom)));
        }

        [Fact]
        public void AddHostObjectCollisonTest()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("User", FormulaType.String);
            Assert.Throws<NameCollisionException>(() => symbol.AddHostObject("User", FormulaType.String, (serviceProvider) => FormulaValue.New("test")));
        }

        public FormulaValue GetUserObject(IServiceProvider serviceProvider)
        {
            var cache = new TypeMarshallerCache();
            var user = (User)serviceProvider.GetService(typeof(User));
            
            // if user object was not added via service provider.
            if (user == null)
            {
                // this exception is catch by Fx, and converted to an error.
                throw new CustomFunctionErrorException("User was not added to service");
            }

            var userFV = cache.Marshal(user);
            return userFV;
        }

        private class User
        {
            public string Name { get; set; }
            
            public int Age { get; set; }
        }
    }
}
