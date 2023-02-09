// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
    }
}
