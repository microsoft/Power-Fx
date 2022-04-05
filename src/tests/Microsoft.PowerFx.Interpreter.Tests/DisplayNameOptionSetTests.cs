// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // OptionSets are only in the interpreter. If we move to core, we can move these tests to core too.
    public class DisplayNameOptionSetTests
    {
        private readonly Engine _engine = new Engine(new PowerFxConfig());

        [Theory]
        [InlineData("OptionSet.Option1 <> OptionSet.Option2", "OptionSet.option_1 <> OptionSet.option_2", false, "")]
        [InlineData("OptionSet.Option1 <> OptionSet.option_2", "OptionSet.option_1 <> OptionSet.option_2", false, "")]
        [InlineData("OptionSet.option_1 <> OptionSet.option_2", "OptionSet.Option1 <> OptionSet.Option2", true, "")]
        [InlineData("OptionSet.option_1 <> OptionSet.Option2", "OptionSet.Option1 <> OptionSet.Option2", true, "")]
        [InlineData("TopOSDisplay.Option1 <> OptionSet.Option2", "OptionSet.option_1 <> OptionSet.option_2", false, "TopOSDisplay")]
        [InlineData("TopOSDisplay.Option1 <> TopOSDisplay.option_2", "OptionSet.option_1 <> OptionSet.option_2", false, "TopOSDisplay")]
        [InlineData("OptionSet.option_1 <> OptionSet.option_2", "TopOSDisplay.Option1 <> TopOSDisplay.Option2", true, "TopOSDisplay")]
        [InlineData("TopOSDisplay.option_1 <> OptionSet.Option2", "TopOSDisplay.Option1 <> TopOSDisplay.Option2", true, "TopOSDisplay")]
        public void OptionSetDisplayNames(string inputExpression, string outputExpression, bool toDisplay, string optionSetDisplayName)
        {            
            var config = new PowerFxConfig(null);
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() 
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            config.AddOptionSet(optionSet, string.IsNullOrEmpty(optionSetDisplayName) ? default : new DName(optionSetDisplayName));
            
            var engine = new Engine(config);

            if (toDisplay)
            {
                var outDisplayExpression = engine.GetDisplayExpression(inputExpression, new RecordType());
                Assert.Equal(outputExpression, outDisplayExpression);
            }
            else
            {
                var outInvariantExpression = engine.GetInvariantExpression(inputExpression, new RecordType());
                Assert.Equal(outputExpression, outInvariantExpression);
            }
        }

        [Fact]
        public void PowerFxConfigCollisionsThrow()
        {
            var config = new PowerFxConfig(null);
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() 
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            var otherOptionSet = new OptionSet("OtherOptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() 
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));
            config.AddEntity(optionSet, new DName("SomeDisplayName"));

            Assert.Throws<NameCollisionException>(() => config.AddEntity(otherOptionSet, new DName("OptionSet")));
            Assert.Throws<NameCollisionException>(() => config.AddEntity(otherOptionSet, new DName("SomeDisplayName")));
                        
            config.AddEntity(otherOptionSet, new DName("NonColliding"));

            Assert.True(config.TryGetSymbol(new DName("OptionSet"), out _, out var displayName));
            Assert.Equal("SomeDisplayName", displayName.Value);
            Assert.True(config.TryGetSymbol(new DName("OtherOptionSet"), out _, out displayName));
            Assert.Equal("NonColliding", displayName.Value);
            Assert.True(config.TryGetSymbol(new DName("NonColliding"), out _, out displayName));
            Assert.Equal("NonColliding", displayName.Value);
            Assert.True(config.TryGetSymbol(new DName("SomeDisplayName"), out _, out displayName));
            Assert.Equal("SomeDisplayName", displayName.Value);
        }
    }
}
