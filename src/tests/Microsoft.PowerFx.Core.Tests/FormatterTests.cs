// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Logging;
using Xunit;
using static Microsoft.PowerFx.Core.Parser.TexlParser;

namespace Microsoft.PowerFx.Tests
{
    public sealed class FormatterTests
    {
        [Theory]
        [InlineData(
            "Collect(Yep, { a: [1], b: \"Hello\" })",
            "Collect(#$firstname$#, { #$fieldname$#:[ #$number$# ], #$fieldname$#:#$string$# })")]
        [InlineData(
            "Set(x, 10 + 3); Launch(\"example.com\", ThisItem.Text, Parent.Text)",
            "Set(#$firstname$#, #$number$# + #$number$#) ; Launch(#$string$#, #$firstname$#.#$righthandid$#, Parent.#$righthandid$#)")]
        public void TestStucturalPrint(string script, string expected)
        {
            var result = ParseScript(
                script,
                flags: Flags.EnableExpressionChaining);

            Assert.Equal(expected, StructuralPrint.Print(result.Root));
        }

        [Theory]
        [InlineData("Back()", false)]
        [InlineData("false", false)]
        [InlineData("\"Are you sure you want to delete this \r\nreceipt?\"", false)]
        [InlineData("RGBA(\n    255,\n    255,\n    255,\n    1\n)", false)]
        [InlineData("RGBA(\n    255,\n    /*r   */255,   ", true)]
        public void TestSeverityLevelsForPrettyPrint(string script, bool expected)
        {
            var result = ParseScript(
                script,
                flags: Flags.EnableExpressionChaining);

            // Can't pretty print a script with errors.
            var hasErrorsWithSeverityHigherThanWarning = false;

            if (result.Errors != null && result.Errors.Any(x => x.Severity > DocumentErrorSeverity.Warning))
            {
                hasErrorsWithSeverityHigherThanWarning = true;
            }

            Assert.Equal(hasErrorsWithSeverityHigherThanWarning, expected);
        }

        [Theory]
        [InlineData("Back()", "Back()")]
        [InlineData("false", "false")]
        [InlineData("\"Are you sure you want to delete this \r\nreceipt?\"", "\"Are you sure you want to delete this \r\nreceipt?\"")]
        [InlineData("RGBA(\n    255,\n    255,\n    255,\n    1\n)", "RGBA(255,255,255,1)")]
        [InlineData("RGBA(\n    255,\n    /*r   */255,\n    255,\n    1\n)//com   ", "RGBA(255,\n    /*r   */255,255,1)//com   ")]
        [InlineData("If(\n    Text(\n        Coalesce(\n            Sum(\n                Filter(\n                    Expenses,\n                    BudgetTitle = Gallery1.Selected.BudgetTitle && BudgetId=Gallery1.Selected.BudgetId\n                ),\n                Value(Expense)\n            ),\n            0\n        ),\n        \"$#,##\"\n    )=\"$\",\n    \"$0\",\n    Text(\n        Coalesce(\n            Sum(\n                Filter(\n                    Expenses,\n                    BudgetId = Gallery1.Selected.BudgetId\n                ),\n                Value(Expense)\n            ),\n            0\n        ),\n        \"$#,##\"\n    )\n)", "If(Text(Coalesce(Sum(Filter(Expenses,BudgetTitle=Gallery1.Selected.BudgetTitle&&BudgetId=Gallery1.Selected.BudgetId),Value(Expense)),0),\"$#,##\")=\"$\",\"$0\",Text(Coalesce(Sum(Filter(Expenses,BudgetId=Gallery1.Selected.BudgetId),Value(Expense)),0),\"$#,##\"))")]
        [InlineData("If(\n    Text(\n        Value(ThisItem.Expense)\n    )= \"0\",\n    \"$\",\n    Text(\n        Value(ThisItem.Expense),\n        \"$#,##\"\n    )\n)", "If(Text(Value(ThisItem.Expense))=\"0\",\"$\",Text(Value(ThisItem.Expense),\"$#,##\"))")]
        public void TestRemoveWhiteSpace(string script, string expected)
        {
            var result = TexlLexer.LocalizedInstance.RemoveWhiteSpace(script);
            Assert.NotNull(result);
            Assert.Equal(result, expected);
        }

        // Note going forward.  Ok, so here's the issue with this test.  there is currently a changable variable
        // place designed to alter minimun characters needed for a break to be made.  This was designed for future user ability
        // to change the char min limit.  Currently it is set at 50 chars.  Originally it was set at 0.
        // point being, everytime this is changed, it will change the "expected" text below and will break this test.
        [Theory]
        [InlineData("foo[@bar]", "foo[@bar]")]
        [InlineData("Back()", "Back()")]
        [InlineData("false", "false")]
        [InlineData("\"Are you sure you want to delete this \r\nreceipt?\"", "\"Are you sure you want to delete this \r\nreceipt?\"")]
        [InlineData("RGBA(255,255,255,1)", "RGBA(\n    255,\n    255,\n    255,\n    1\n)")]
        [InlineData("RGBA(255, /*r*/255, 255, 1)//com", "RGBA(\n    255,/*r*/\n    255,\n    255,\n    1\n)//com")]
        [InlineData("ColorFade(Button4.BorderColor, 20%)", "ColorFade(\n    Button4.BorderColor,\n    20%\n)")]
        [InlineData("If(!IsBlank(NewAddressText.Text)&&!IsBlank(NewCityText.Text)&&!IsBlank(NewZipText.Text)&&!IsBlank(NewStateText.Text)&&!IsBlank(NewTitleText.Text)&&!IsBlank(NewSubTitleText.Text)||!IsBlank(NewTitleText.Text)&&!IsBlank(NewSubTitleText.Text)&&Radio1_4.Selected.Value=\"Use GPS for current Location\", true)", "If(\n    !IsBlank(NewAddressText.Text) && !IsBlank(NewCityText.Text) && !IsBlank(NewZipText.Text) && !IsBlank(NewStateText.Text) && !IsBlank(NewTitleText.Text) && !IsBlank(NewSubTitleText.Text) || !IsBlank(NewTitleText.Text) && !IsBlank(NewSubTitleText.Text) && Radio1_4.Selected.Value = \"Use GPS for current Location\",\n    true\n)")]
        [InlineData("Set(ErrorC,0);ForAll(myData,Patch('[dbo].[BookingData]',First(Filter('[dbo].[BookingData]',ID=myData[@ID])),{FiscalYearValue:myData[@FiscalYearValue],ModifiedBy:TextInput1.Text}));If(CountRows(Errors('[dbo].[BookingData]'))>ErrorC,Set(Err,CountRows(Errors('[dbo].[BookingData]'))&\" \"&First(Errors('[dbo].[BookingData]')).Message),Set(Err,\"Success\"))", "Set(\n    ErrorC,\n    0\n);\nForAll(\n    myData,\n    Patch(\n        '[dbo].[BookingData]',\n        First(\n            Filter(\n                '[dbo].[BookingData]',\n                ID = myData[@ID]\n            )\n        ),\n        {\n            FiscalYearValue: myData[@FiscalYearValue],\n            ModifiedBy: TextInput1.Text\n        }\n    )\n);\nIf(\n    CountRows(Errors('[dbo].[BookingData]')) > ErrorC,\n    Set(\n        Err,\n        CountRows(Errors('[dbo].[BookingData]')) & \" \" & First(Errors('[dbo].[BookingData]')).Message\n    ),\n    Set(\n        Err,\n        \"Success\"\n    )\n)")]
        [InlineData("\"(\"&RoundUp(Value(UsedHrsText.Text/8),1) &\" DAYS)\"", "\"(\" & RoundUp(\n    Value(UsedHrsText.Text / 8),\n    1\n) & \" DAYS)\"")]
        [InlineData("If(true = !true,false,true)", "If(\n    true = !true,\n    false,\n    true\n)")]
        [InlineData("If(true > -1,false,true)", "If(\n    true > -1,\n    false,\n    true\n)")]
        [InlineData("If(1 <= -1,-11 >= -1,-11 < 1)", "If(\n    1 <= -1,\n    -11 >= -1,\n    -11 < 1\n)")]
        [InlineData("If(true <> -1,false,true)", "If(\n    true <> -1,\n    false,\n    true\n)")]
        [InlineData("If(true = Not true,false,true)", "If(\n    true = Not true,\n    false,\n    true\n)")]
        [InlineData("If(7% = !true,false,true)", "If(\n    7% = !true,\n    false,\n    true\n)")]
        [InlineData("-Label1.X & -Label1.Y", "-Label1.X & -Label1.Y")]
        [InlineData("(-Label1.X) & (-Label1.Y)", "(-Label1.X) & (-Label1.Y)")]
        [InlineData("UsedHrsText.Text-TextBox7_1.Text", "UsedHrsText.Text - TextBox7_1.Text")]
        [InlineData("If(!IsBlank(Address.Text) || !IsBlank(City.Text) || !IsBlank(States.Text) || !IsBlank(ZipCode.Text),SubmitForm(Form1),UpdateContext({error1:true}))", "If(\n    !IsBlank(Address.Text) || !IsBlank(City.Text) || !IsBlank(States.Text) || !IsBlank(ZipCode.Text),\n    SubmitForm(Form1),\n    UpdateContext({error1: true})\n)")]
        [InlineData("If(!IsBlank(Address.Text) && !IsBlank(City.Text) && !IsBlank(States.Text) && !IsBlank(ZipCode.Text),SubmitForm(Form1),UpdateContext({error1:true}))", "If(\n    !IsBlank(Address.Text) && !IsBlank(City.Text) && !IsBlank(States.Text) && !IsBlank(ZipCode.Text),\n    SubmitForm(Form1),\n    UpdateContext({error1: true})\n)")]
        [InlineData("If(CountRows(Filter('Time Off Requests','Created On'>=Today()&&Owner=LookUp(Users_1,'User Name'=User().Email,User)))>0,Navigate([@'Attendance Already Submitted'],ScreenTransition.Cover),NewForm(AttendanceForm));Navigate('Save Attendance',Fade)", "If(\n    CountRows(\n        Filter(\n            'Time Off Requests',\n            'Created On' >= Today() && Owner = LookUp(\n                Users_1,\n                'User Name' = User().Email,\n                User\n            )\n        )\n    ) > 0,\n    Navigate(\n        [@'Attendance Already Submitted'],\n        ScreenTransition.Cover\n    ),\n    NewForm(AttendanceForm)\n);\nNavigate(\n    'Save Attendance',\n    Fade\n)")]
        [InlineData("[1]", "[1]")]
        [InlineData("[1, 2, 3]", "[\n    1,\n    2,\n    3\n]")]
        [InlineData("If(true, [1, 2, 3], [3])", "If(\n    true,\n    [\n        1,\n        2,\n        3\n    ],\n    [3]\n)")]
        [InlineData("((((1 + 2))))", "((((1 + 2))))")]
        [InlineData("((1 + 2) + 3)", "((1 + 2) + 3)")]
        [InlineData("(1 + (2 + 3))", "(1 + (2 + 3))")]
        [InlineData("(1 * 2) + 3)", "(1 * 2) + 3)")]
        [InlineData("Namespace.Call(1, 2, 3)", "Namespace.Call(\n    1,\n    2,\n    3\n)")]
        [InlineData("ColorFade(RGBA(56,96,178,1),-(1+3)%%)", "ColorFade(\n    RGBA(\n        56,\n        96,\n        178,\n        1\n    ),\n    -(1 + 3)%%\n)")]
        [InlineData("/*jj*/\r\nRGBA(255, 255, 255, 1)\n//yes", "/*jj*/\r\nRGBA(\n    255,\n    255,\n    255,\n    1\n)\n//yes")]
        [InlineData("/*jj*/\nRGBA(\n    /*j2*/\n    255,\n    255,\n    255,\n    1\n)\n//yes", "/*jj*/\nRGBA(\n    /*j2*/\n    255,\n    255,\n    255,\n    1\n)\n//yes")]
        [InlineData("/*x*/Call(/*a*/1/*b*/;/*c*/2/*d*/;/*e*/3/*f*/, /*g*/4/*h*/)/*y*/", "/*x*/Call(/*a*/\n    1/*b*/;\n    /*c*/2/*d*/;\n    /*e*/3/*f*/,/*g*/\n    4/*h*/\n)/*y*/")]
        [InlineData("/*a*/[/*b*/1/*c*/,/*d*/2/*e*/]/*f*/", "/*a*/[\n    /*b*/1/*c*/,\n    /*d*/2/*e*/\n]/*f*/")]
        [InlineData("/*a*/{/*b*/name/*c*/:/*d*/1/*e*/,\n/*f*/name2/*g*/:/*h*/2/*i*/}/*j*/", "/*a*/{\n    /*b*/\n    name/*c*/: /*d*/1/*e*/,\n    /*f*/\n    name2/*g*/: /*h*/2/*i*/\n}/*j*/")]
        //// Make sure there's no lost trivia
        [InlineData("/*a*/foo/*b*/[/*c*/@/*d*/bar/*e*/]/*f*/", "/*a*/foo/*b*/[/*c*/@/*d*/bar/*e*/]/*f*/")]
        [InlineData("1; /*a*/2/*b*/; 3", "1;\n/*a*/2/*b*/;\n3")]
        [InlineData("/*a*/1/*b*/+/*c*/2/*d*/-/*e*/3/*f*/", "/*a*/1 /*b*/+/*c*/ 2 /*d*/-/*e*/ 3/*f*/")]
        public void TestPrettyPrint(string script, string expected)
        {
            var result = Format(script);
            Assert.NotNull(result);
            Assert.Equal(expected, result);

            // Ensure idempotence
            result = Format(result);
            Assert.NotNull(result);
            Assert.Equal(expected, result);
        }
    }
}
