// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class PublicSurfaceTests
    {
        [Fact]
        public void Test()
        {
            var asm = typeof(Parser.TexlParser).Assembly;

            var allowed = new HashSet<string>()
            {
                "Microsoft.PowerFx.Core.Texl.Intellisense.IIntellisenseResult",
                "Microsoft.PowerFx.Core.Texl.Intellisense.IIntellisenseSuggestion",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SuggestionIconKind",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SuggestionKind",
                "Microsoft.PowerFx.Core.Texl.Intellisense.UIString",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp.ParameterInformation",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp.SignatureHelp",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp.SignatureInformation",                
                "Microsoft.PowerFx.Core.PowerFxConfig",
                "Microsoft.PowerFx.Core.Public.CheckResult",
                "Microsoft.PowerFx.Core.Public.ErrorKind",
                "Microsoft.PowerFx.Core.Public.ExpressionError",
                "Microsoft.PowerFx.Core.Public.FormulaWithParameters",
                "Microsoft.PowerFx.Core.Public.IExpression",
                "Microsoft.PowerFx.Core.Public.IPowerFxEngine",
                "Microsoft.PowerFx.Core.Public.IPowerFxScope",
                "Microsoft.PowerFx.Core.Public.IPowerFxScopeDisplayName",
                "Microsoft.PowerFx.Core.Public.TokenResultType",
                "Microsoft.PowerFx.Core.Public.Values.BlankValue",
                "Microsoft.PowerFx.Core.Public.Values.BooleanValue",
                "Microsoft.PowerFx.Core.Public.Values.UntypedObjectValue",
                "Microsoft.PowerFx.Core.Public.Values.IUntypedObject",
                "Microsoft.PowerFx.Core.Public.Values.DateTimeValue",
                "Microsoft.PowerFx.Core.Public.Values.DateValue",
                "Microsoft.PowerFx.Core.Public.Values.DValue`1",
                "Microsoft.PowerFx.Core.Public.Values.ErrorValue",
                "Microsoft.PowerFx.Core.Public.Values.FormulaValue",
                "Microsoft.PowerFx.Core.Public.Values.IValueVisitor",
                "Microsoft.PowerFx.Core.Public.Values.NamedValue",
                "Microsoft.PowerFx.Core.Public.Values.NumberValue",
                "Microsoft.PowerFx.Core.Public.Values.OptionSetValue",
                "Microsoft.PowerFx.Core.Public.Values.PrimitiveValue`1",
                "Microsoft.PowerFx.Core.Public.Values.RecordValue",
                "Microsoft.PowerFx.Core.Public.Values.StringValue",
                "Microsoft.PowerFx.Core.Public.Values.TableValue",
                "Microsoft.PowerFx.Core.Public.Values.TimeValue",
                "Microsoft.PowerFx.Core.Public.Values.ValidFormulaValue",
                "Microsoft.PowerFx.Core.Public.Types.AggregateType",
                "Microsoft.PowerFx.Core.Public.Types.BlankType",
                "Microsoft.PowerFx.Core.Public.Types.BooleanType",
                "Microsoft.PowerFx.Core.Public.Types.UntypedObjectType",
                "Microsoft.PowerFx.Core.Public.Types.DateTimeNoTimeZoneType",
                "Microsoft.PowerFx.Core.Public.Types.DateTimeType",
                "Microsoft.PowerFx.Core.Public.Types.DateType",
                "Microsoft.PowerFx.Core.Public.Types.FormulaType",
                "Microsoft.PowerFx.Core.Public.Types.ITypeVistor",
                "Microsoft.PowerFx.Core.Public.Types.NamedFormulaType",
                "Microsoft.PowerFx.Core.Public.Types.NumberType",
                "Microsoft.PowerFx.Core.Public.Types.OptionSetValueType",
                "Microsoft.PowerFx.Core.Public.Types.RecordType",
                "Microsoft.PowerFx.Core.Public.Types.StringType",
                "Microsoft.PowerFx.Core.Public.Types.TableType",
                "Microsoft.PowerFx.Core.Public.Types.TimeType",
                "Microsoft.PowerFx.Core.Public.Types.HyperlinkType",
                "Microsoft.PowerFx.Core.Public.Types.ExternalTypeKind",
                "Microsoft.PowerFx.Core.Public.Types.ExternalType",
                "Microsoft.PowerFx.Core.Localization.ErrorResourceKey",
                "Microsoft.PowerFx.Core.Localization.Span",
                "Microsoft.PowerFx.Core.Functions.Publish.Capabilities",
                "Microsoft.PowerFx.Core.Functions.DLP.RequiredDataSourcePermissions",
                "Microsoft.PowerFx.Core.Errors.DocumentErrorSeverity",
                "Microsoft.PowerFx.Core.App.IExternalEnabledFeatures",
                "Microsoft.PowerFx.Core.App.DefaultEnabledFeatures",
                "Microsoft.PowerFx.Core.Utils.DName",
                "Microsoft.PowerFx.Core.Utils.ICheckable",
                "Microsoft.PowerFx.Core.NameCollisionException",
            };

            var sb = new StringBuilder();
            var count = 0;
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                var name = type.FullName;
                if (!allowed.Contains(name))
                {
                    sb.AppendLine(name);
                    count++;
                }

                allowed.Remove(name);
            }

            Assert.True(count == 0, $"Unexpected public types: {sb}");

            // Types we expect to be in the assembly are all there. 
            Assert.Empty(allowed);
        }
    }
}
