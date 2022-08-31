// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    public class SuggestTests : IntellisenseTestBase
    {      
        /// <summary>
        /// This method does the same as <see cref="Suggest"/>, but filters the suggestions by their text so
        /// that they can be more easily compared.
        /// </summary>
        /// <param name="expression">
        /// Test case wherein the presence of the `|` character indicates cursor position.  See
        /// <see cref="TestSuggest"/> for more details.
        /// </param>
        /// <param name="contextTypeString">
        /// The type that defines names and types that are valid in <see cref="expression"/>.
        /// </param>
        /// <returns>
        /// List of string representing suggestions.
        /// </returns>
        private string[] SuggestStrings(string expression, PowerFxConfig config, string contextTypeString = null)
        {
            Assert.NotNull(expression);

            var intellisense = Suggest(expression, config, contextTypeString);
            return intellisense.Suggestions.Select(suggestion => suggestion.DisplayText.Text).ToArray();
        }

        private string[] SuggestStrings(string expression, PowerFxConfig config, RecordType context)
        {
            Assert.NotNull(expression);

            var intellisense = Suggest(expression, config, context);
            return intellisense.Suggestions.Select(suggestion => suggestion.DisplayText.Text).ToArray();
        }
        
        internal static PowerFxConfig Default => PowerFxConfig.BuildWithEnumStore(null, new EnumStoreBuilder().WithDefaultEnums());

        // No enums, no functions. Adding functions will add back in associated enums, so to be truly empty, ensure no functions. 
        private PowerFxConfig EmptyEverything => PowerFxConfig.BuildWithEnumStore(null, new EnumStoreBuilder(), new TexlFunction[0]);

        // No extra enums, but standard functions (which will include some enums).
        private PowerFxConfig MinimalEnums => PowerFxConfig.BuildWithEnumStore(null, new EnumStoreBuilder().WithRequiredEnums(BuiltinFunctionsCore.BuiltinFunctionsLibrary));        

        /// <summary>
        /// Compares expected suggestions with suggestions made by PFx Intellisense for a given
        /// <see cref="expression"/> and cursor position. The cursor position is determined by the index of
        /// the | character in <see cref="expression"/>, which will be removed but the test is run. Note that
        /// the use of the `|` char is for this reason disallowed from test cases except to indicate cursor
        /// position. Note also that these test suggestion order as well as contents.
        /// </summary>
        /// <param name="expression">
        /// Expression on which intellisense will be run.
        /// </param>
        /// <param name="expectedSuggestions">
        /// A list of arguments that will be compared with the names of the output of
        /// <see cref="Workspace.Suggest"/> in order.
        /// </param>
        [Theory]

        // CommentNodeSuggestionHandler
        [InlineData("// No| intellisense inside comment")]

        // RecordNodeSuggestionHandler
        [InlineData("{} |", "As", "exactin", "in")]
        [InlineData("{'complex record':{column:{}}} |", "As", "exactin", "in")]

        // DottedNameNodeSuggestionHandler
        [InlineData("{a:{},b:{},c:{}}.|", "a", "b", "c")]
        [InlineData("$\"Hello { First(Table({a:{},b:{},c:{}})).| } World\"", "a", "b", "c")]
        [InlineData("$\"Hello { First(Table({a:{},b:{},c:{}})).|   \"", "a", "b", "c")]
        [InlineData("$\"Hello { {a:{},b:{},c:{}}.|  \"", "a", "b", "c")]
        [InlineData("First([$\"{ {a:1,b:2,c:3}.|", "a", "b", "c")]
        [InlineData("$\"Hello {\"|")]
        [InlineData("$\"Hello {}\"|")]
        [InlineData("$\"Hello {|}\"")]
        [InlineData("$\"{ {a:{},b:{},c:{}}.}{|}\"")]
        [InlineData("$\"{ {a:{},b:{},c:{}}}{|}\"")]
        [InlineData("$ |")]
        [InlineData("$\"foo {|")]
        [InlineData("$\"foo { {a:| } } \"")]
        [InlineData("{abc:{},ab:{},a:{}}.|ab", "ab", "a", "abc")]
        [InlineData("{abc:{},ab:{},a:{}}.ab|", "ab", "abc")]
        [InlineData("{abc:{},ab:{},a:{}}.ab|c", "abc", "ab")]
        [InlineData("{abc:{},ab:{},a:{}}.abc|", "abc")]
        [InlineData("{abc:{},ab:{},a:{}}.| abc", "a", "ab", "abc")]
        [InlineData("{abc:{},ab:{},a:{}}.abc |", "As", "exactin", "in")]
        [InlineData("{az:{},z:{}}.|", "az", "z")]
        [InlineData("{az:{},z:{}}.z|", "z", "az")]

        // We don't recommend anything for one column tables only if the one column table is referenced
        // by the following dotted name access.
        [InlineData("[\"test\"].Value.| ")]
        [InlineData("[{test:\",test\"}].test.| ")]

        // We do, however, if the one column table is a literal.
        [InlineData("[\"test\"].| ", "Value")]
        [InlineData("Calendar.|", "MonthsLong", "MonthsShort", "WeekdaysLong", "WeekdaysShort")]
        [InlineData("Calendar.Months|", "MonthsLong", "MonthsShort")]
        [InlineData("Color.AliceBl|", "AliceBlue")]
        [InlineData("Color.Pale|", "PaleGoldenRod", "PaleGreen", "PaleTurquoise", "PaleVioletRed")]

        // CallNodeSuggestionHandler
        [InlineData("ForAll|([1],Value)", "ForAll")]
        [InlineData("at|(", "Atan", "Atan2", "Concat", "Concatenate", "Date", "DateAdd", "DateDiff", "DateTimeValue", "DateValue")]
        [InlineData("Atan |(")]
        [InlineData("Clock.A|(", "Clock.AmPm", "Clock.AmPmShort")]
        [InlineData("ForAll([\"test\"],EndsWith(|))", "Value")]
        [InlineData("ForAll([1],Value) |", "As", "exactin", "in")]

        // BoolLitNodeSuggestionHandler
        [InlineData("true|", "true")]
        [InlineData("tru|e", "true", "Trunc")]
        [InlineData("false |", "-", "&", "&&", "*", "/", "^", "||", "+", "<", "<=", "<>", "=", ">", ">=", "And", "As", "exactin", "in", "Or")]

        // BinaryOpNodeSuggestionHandler
        [InlineData("1 +|", "+")]
        [InlineData("1 |+", "-", "&", "&&", "*", "/", "^", "||", "+", "<", "<=", "<>", "=", ">", ">=", "And", "As", "exactin", "in", "Or")]
        [InlineData("\"1\" in|", "in", "exactin")]
        [InlineData("true &|", "&", "&&")]

        // UnaryOpNodeSuggestionHandler
        [InlineData("Not| false", "Not", "NotificationType", "NotificationType.Error", "NotificationType.Information", "NotificationType.Success", "NotificationType.Warning", "ErrorKind.FileNotFound", "ErrorKind.NotFound", "ErrorKind.NotSupported", "Icon.Note", "Icon.Notebook")]
        [InlineData("| Not false")]
        [InlineData("Not |")]

        // StrInterpSuggestionHandler
        [InlineData("With( {Apples:3}, $\"We have {appl|", "Apples")]
        [InlineData("With( {Apples:3}, $\"We have {appl|} apples.", "Apples")]
        [InlineData("$\"This is a randomly generated number: {rand|", "Rand", "RandBetween")]

        // StrNumLitNodeSuggestionHandler
        [InlineData("1 |", "-", "&", "&&", "*", "/", "^", "||", "+", "<", "<=", "<>", "=", ">", ">=", "And", "As", "exactin", "in", "Or")]
        [InlineData("1|0")]
        [InlineData("\"Clock|\"")]

        // FirstNameNodeSuggestionHandler
        [InlineData("Tru|", "true", "Trunc")] // Though it recommends only a boolean, the suggestions are still provided by the first name handler
        [InlineData("[@Bo|]", "BorderStyle", "VirtualKeyboardMode")]

        // FunctionRecordNameSuggestionHandler
        [InlineData("Error({Kin|d:0})", "Kind:")]
        [InlineData("Error({|Kind:0, Test:\"\"})", "Kind:", "Test:")]

        // ErrorNodeSuggestionHandler
        [InlineData("ForAll([0],`|", "ThisRecord", "Value")]
        [InlineData("ForAll(-],|", "ThisRecord")]
        [InlineData("ForAll()~|")]
        [InlineData("With( {Apples:3}, $\"We have {Apples} apples|")]

        // BlankNodeSuggestionHandler
        [InlineData("|")]

        // AddSuggestionsForEnums
        [InlineData("Edit|", "DataSourceInfo.EditPermission", "DisplayMode.Edit", "ErrorKind.EditPermissions", "FormMode.Edit", "Icon.Edit", "RecordInfo.EditPermission", "SelectedState.Edit")]
        [InlineData("Value(Edit|", "DataSourceInfo.EditPermission", "DisplayMode.Edit", "ErrorKind.EditPermissions", "FormMode.Edit", "Icon.Edit", "RecordInfo.EditPermission", "SelectedState.Edit")]
        [InlineData("DisplayMode.E|", "Edit", "Disabled", "View")]
        [InlineData("Disabled|", "DisplayMode.Disabled")]
        [InlineData("DisplayMode.D|", "Disabled", "Edit")]
        [InlineData("DisplayMode|", "DisplayMode", "DisplayMode.Disabled", "DisplayMode.Edit", "DisplayMode.View")]
        [InlineData("$\"Hello {DisplayMode|} World!\"", "DisplayMode", "DisplayMode.Disabled", "DisplayMode.Edit", "DisplayMode.View")]
        public void TestSuggest(string expression, params string[] expectedSuggestions)
        {
            // Note that the expression string needs to have balanced quotes or we hit a bug in NUnit running the tests:
            //   https://github.com/nunit/nunit3-vs-adapter/issues/691

            Preview.FeatureFlags.StringInterpolation = true;
            var actualSuggestions = SuggestStrings(expression, Default);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }

        /// <summary>
        /// In cases for Intellisense with an empty enum store and no function list.
        /// </summary>
        [Theory]
        [InlineData("Color.AliceBl|")]
        [InlineData("Color.Pale|")]
        [InlineData("Edit|")]
        [InlineData("DisplayMode.E|")]
        [InlineData("Disabled|")]
        [InlineData("DisplayMode.D|")]
        [InlineData("DisplayMode|")]
        public void TestSuggestEmptyEnumList(string expression, params string[] expectedSuggestions)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var actualSuggestions = SuggestStrings(expression, EmptyEverything);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }

        /// <summary>
        /// In cases for Intellisense with an empty enum store, but still builtin functions. 
        /// </summary>
        [Theory]
        [InlineData("Calendar.|", "MonthsLong", "MonthsShort", "WeekdaysLong", "WeekdaysShort")]
        [InlineData("Calendar.Months|", "MonthsLong", "MonthsShort")]
        public void TestSuggestEmptyAll(string expression, params string[] expectedSuggestions)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var actualSuggestions = SuggestStrings(expression, MinimalEnums);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }

        /// <summary>
        /// In cases for which Intellisense produces exceedingly numerous results it may be sufficient that
        /// they (the cases) be validated based on whether they return suggestions at all.
        /// </summary>
        [Theory]

        // CallNodeSuggestionHandler
        [InlineData("| ForAll([1],Value)")]

        // BoolLitNodeSuggestionHandler
        [InlineData("t|rue")]
        [InlineData("f|alse")]
        [InlineData("| false")]

        // UnaryOpNodeSuggestionHandler
        [InlineData("|Not false")]

        // FirstNameNodeSuggestionHandler
        [InlineData("| Test", "![Test: s]")]
        [InlineData("[@|]")]
        [InlineData("[@|")]
        public void TestNonEmptySuggest(string expression, string context = null)
        {
            var actualSuggestions = SuggestStrings(expression, Default, context);
            Assert.True(actualSuggestions.Length > 0);
        }

        [Theory]

        // FirstNameNodeSuggestionHandleractualSuggestions = IntellisenseResult
        [InlineData("Test|", "![Test1: s, Test2: n, Test3: h]", "Test1", "Test2", "Test3")]
        [InlineData("RecordName[|", "![RecordName: ![StringName: s, NumberName: n]]", "@NumberName", "@StringName")]
        [InlineData("RecordName[|", "![RecordName: ![]]")]
        [InlineData("Test |", "![Test: s]", "-", "&", "&&", "*", "/", "^", "||", "+", "<", "<=", "<>", "=", ">", ">=", "And", "As", "exactin", "in", "Or")]

        // ErrorNodeSuggestionHandler
        [InlineData("ForAll(Table,`|", "![Table: *[Column: s]]", "Column", "ThisRecord")]
        public void TestSuggestWithContext(string expression, string context, params string[] expectedSuggestions)
        {
            Assert.NotNull(context);

            var actualSuggestions = SuggestStrings(expression, Default, context);
            Assert.Equal(expectedSuggestions, actualSuggestions);
        }
        
        [Theory]
        [InlineData("So|", true, "SomeString")]
        [InlineData("Loop.Loop.Loop.So|", true, "SomeString")]
        [InlineData("Loop.|", true, "Loop", "Record", "SomeString", "TableLoop")]
        [InlineData("Record.|", false, "Foo")]
        [InlineData("Loop.Loop.Record.|", false, "Foo")]
        [InlineData("Filter(TableLoop, EndsWith(|", true, "SomeString")]
        [InlineData("Loop.L|o", true, "Loop", "TableLoop")]
        public void TestSuggestLazyTypes(string expression, bool requiresExpansion, params string[] expectedSuggestions)
        {
            var lazyInstance = new LazyRecursiveRecordType();
            var config = PowerFxConfig.BuildWithEnumStore(
                null,
                new EnumStoreBuilder(),
                new TexlFunction[] { BuiltinFunctionsCore.EndsWith, BuiltinFunctionsCore.Filter, BuiltinFunctionsCore.Table });
            var actualSuggestions = SuggestStrings(expression, config, lazyInstance);
            Assert.Equal(expectedSuggestions, actualSuggestions);
            
            // Intellisense requires iterating the field names for some operations
            Assert.Equal(requiresExpansion, lazyInstance.EnumerableIterated);
        }

        private class LazyRecursiveRecordType : RecordType
        {
            public override IEnumerable<string> FieldNames => GetFieldNames();

            public bool EnumerableIterated = false;

            public LazyRecursiveRecordType()
                : base()
            {
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                switch (name)
                {
                    case "SomeString":
                        type = FormulaType.String;
                        return true;
                    case "TableLoop":
                        type = ToTable();
                        return true;
                    case "Loop":
                        type = this;
                        return true;
                    case "Record":
                        type = RecordType.Empty().Add("Foo", FormulaType.Number);
                        return true;
                    default:
                        type = FormulaType.Blank;
                        return false;
                }
            }

            private IEnumerable<string> GetFieldNames()
            {
                EnumerableIterated = true;

                yield return "SomeString";
                yield return "Loop";
                yield return "Record";
                yield return "TableLoop";
            }

            public override bool Equals(object other)
            {
                return other is LazyRecursiveRecordType; // All the same 
            }

            public override int GetHashCode()
            {
                return 1;
            }   
        }
    }
}
