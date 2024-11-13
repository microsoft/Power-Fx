// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests.Shared
{
    public class OptionSetListTests
    {
        [Fact]
        public void AddOptionSetToList()
        {
            OptionSetList list = new OptionSetList();

            SingleSourceDisplayNameProvider dnp = new SingleSourceDisplayNameProvider();
            dnp = dnp.AddField(new DName("logical1"), new DName("display1"));
            dnp = dnp.AddField(new DName("logical2"), new DName("display2"));

            OptionSet os1 = new OptionSet("os1", dnp);
            OptionSet os2 = new OptionSet("os1", dnp);

            OptionSet os = list.TryAdd(os1);
            Assert.Same(os1, os);

            // twice the same, nothing added
            os = list.TryAdd(os1);
            Assert.Same(os1, os);
            Assert.Single(list.OptionSets);

            // still the same, nothing added
            os = list.TryAdd(os2);
            Assert.Same(os1, os);
            Assert.Single(list.OptionSets);

            dnp = dnp.AddField(new DName("logical3"), new DName("display3"));
            OptionSet os3 = new OptionSet("os3", dnp);

            // new optionSet
            os = list.TryAdd(os3);
            Assert.NotSame(os, os1);
            Assert.Equal(2, list.OptionSets.Count());

            // try a name conflict now
            OptionSet os4 = new OptionSet("os1", dnp);

            InvalidOperationException ioe = Assert.Throws<InvalidOperationException>(() => list.TryAdd(os4));
            Assert.Equal("Optionset name conflict (os1)", ioe.Message);           
        }
    }
}
