﻿// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public static class ConsoleRepl
    {
        private static RecalcEngine _engine;

        private const string OptionFormatTable = "FormatTable";
        private static bool _formatTable = true;

        private const string OptionNumberIsFloat = "NumberIsFloat";
        private static bool _numberIsFloat = false;

        private const string OptionLargeCallDepth = "LargeCallDepth";
        private static bool _largeCallDepth = false;

        private const string OptionFeaturesNone = "FeaturesNone";

        private const string OptionPowerFxV1 = "PowerFxV1";

        private const string OptionHashCodes = "HashCodes";
        private static bool _hashCodes = false;

        private const string OptionStackTrace = "StackTrace";
        private static bool _stackTrace = false;

        private static readonly BasicUserInfo _userInfo = new BasicUserInfo
        {
            FullName = "Susan Burk",
            Email = "susan@contoso.com",
            DataverseUserId = new Guid("aa1d4f65-044f-4928-a95f-30d4c8ebf118"),
            TeamsMemberId = "29:1DUjC5z4ttsBQa0fX2O7B0IDu30R",
            EntraObjectId = new Guid("99999999-044f-4928-a95f-30d4c8ebf118"),
        };

        private static readonly Features _features = Features.PowerFxV1;

        private static void ResetEngine()
        {
            var props = new Dictionary<string, object>
            {
                { "FullName", _userInfo.FullName },
                { "Email", _userInfo.Email },
                { "DataverseUserId", _userInfo.DataverseUserId },
                { "TeamsMemberId", _userInfo.TeamsMemberId }
            };

            var allKeys = props.Keys.ToArray();
            SymbolTable userSymbolTable = new SymbolTable();

            userSymbolTable.AddUserInfoObject(allKeys);

            var config = new PowerFxConfig(_features) { SymbolTable = userSymbolTable };

            if (_largeCallDepth)
            {
                config.MaxCallDepth = 200;
            }

            Dictionary<string, string> options = new Dictionary<string, string>
            {
                { OptionFormatTable, OptionFormatTable },
                { OptionNumberIsFloat, OptionNumberIsFloat },
                { OptionLargeCallDepth, OptionLargeCallDepth },
                { OptionFeaturesNone, OptionFeaturesNone },
                { OptionPowerFxV1, OptionPowerFxV1 },
                { OptionHashCodes, OptionHashCodes },
                { OptionStackTrace, OptionStackTrace }
            };

            foreach (var featureProperty in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (featureProperty.PropertyType == typeof(bool) && featureProperty.CanWrite)
                {
                    var feature = featureProperty.Name;
                    options.Add(feature.ToString(), feature.ToString());
                }
            }

            config.SymbolTable.EnableMutationFunctions();

            config.EnableSetFunction();
            config.EnableParseJSONFunction();

            config.AddFunction(new HelpFunction());
            config.AddFunction(new ResetFunction());
            config.AddFunction(new ExitFunction());
            config.AddFunction(new OptionFunction());
            config.AddFunction(new ResetImportFunction());
            config.AddFunction(new ImportFunction1Arg());
            config.AddFunction(new ImportFunction2Arg());

            var optionsSet = new OptionSet("Options", DisplayNameUtility.MakeUnique(options));

            config.AddOptionSet(optionsSet);

            _engine = new RecalcEngine(config);
        }

        public static void Main()
        {
            var enabled = new StringBuilder();

            Console.InputEncoding = System.Text.Encoding.Unicode;
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            ResetEngine();

            var version = typeof(RecalcEngine).Assembly.GetName().Version.ToString();
            Console.WriteLine($"Microsoft Power Fx Console Formula REPL, Version {version}");

            foreach (var propertyInfo in typeof(Features).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (propertyInfo.PropertyType == typeof(bool) && ((bool)propertyInfo.GetValue(_engine.Config.Features)) == true)
                {
                    enabled.Append(" " + propertyInfo.Name);
                }
            }

            if (enabled.Length == 0)
            {
                enabled.Append(" <none>");
            }

            Console.WriteLine($"Experimental features enabled:{enabled}");

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine($"Enter Excel formulas.  Use \"Help()\" for details.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            REPL(Console.In, false);
        }

        // Pattern match for Set(x,y) so that we can define the variable
        public static bool TryMatchSet(string expr, out string arg0name, out FormulaValue varValue)
        {
            var parserOptions = _engine.GetDefaultParserOptionsCopy();
            parserOptions.AllowsSideEffects = true;

            var parse = Engine.Parse(expr, options: parserOptions);
            if (parse.IsSuccess)
            {
                if (parse.Root.Kind == Microsoft.PowerFx.Syntax.NodeKind.Call)
                {
                    if (parse.Root is Microsoft.PowerFx.Syntax.CallNode call)
                    {
                        if (call.Head.Name.Value == "Set")
                        {
                            // Infer type based on arg1. 
                            var arg0 = call.Args.ChildNodes[0];
                            if (arg0 is Microsoft.PowerFx.Syntax.FirstNameNode arg0node)
                            {
                                arg0name = arg0node.Ident.Name.Value;

                                var arg1 = call.Args.ChildNodes[1];
                                var arg1expr = arg1.GetCompleteSpan().GetFragment(expr);

                                var check = _engine.Check(arg1expr, GetParserOptions(), GetSymbolTable());
                                if (check.IsSuccess)
                                {
                                    var arg1Type = check.ReturnType;

                                    varValue = check.GetEvaluator().Eval(GetRuntimeConfig());
                                    _engine.UpdateVariable(arg0name, varValue);

                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            varValue = null;
            arg0name = null;
            return false;
        }

        private static ParserOptions GetParserOptions()
        {
            return new ParserOptions() { AllowsSideEffects = true, NumberIsFloat = _numberIsFloat };
        }

        private static ReadOnlySymbolTable GetSymbolTable()
        {
            return _engine.EngineSymbols;
        }

        private static RuntimeConfig GetRuntimeConfig()
        {
            var rc = new RuntimeConfig();
            rc.SetUserInfo(_userInfo);
            return rc;
        }

        private static FormulaValue Eval(string expressionText)
        {
            CheckResult checkResult = _engine.Check(expressionText, GetParserOptions(), GetSymbolTable());
            checkResult.ThrowOnErrors();

            IExpressionEvaluator evaluator = checkResult.GetEvaluator();
            return evaluator.Eval(GetRuntimeConfig());
        }

        public static void REPL(TextReader input, bool echo = false, TextWriter output = null)
        {
            string expr;

            // main loop
            while ((expr = ReadFormula(input, output, echo)) != null)
            {
                Match match;

                try
                {
                    // variable assignment: Set( <ident>, <expr> )
                    if (TryMatchSet(expr, out var varName, out var varValue))
                    {
                        Console.WriteLine(varName + ": " + PrintResult(varValue));
                        output?.WriteLine(varName + ": " + PrintResult(varValue));
                    }

                    // IR pretty printer: IR( <expr> )
                    else if ((match = Regex.Match(expr, @"^\s*IR\((?<expr>.*)\)\s*$", RegexOptions.Singleline)).Success)
                    {
                        var opts = new ParserOptions() { AllowsSideEffects = true, NumberIsFloat = _numberIsFloat };
                        var cr = _engine.Check(match.Groups["expr"].Value, options: opts);
                        var ir = cr.PrintIR();
                        Console.WriteLine(ir);
                        output?.WriteLine(ir);
                    }

                    // named formula definition: <ident> = <formula>
                    else if ((match = Regex.Match(expr, @"^\s*(?<ident>(\w+|'([^']|'')+'))\s*=(?<formula>.*)$", RegexOptions.Singleline)).Success &&
                              !Regex.IsMatch(match.Groups["ident"].Value, "^\\d") &&
                              match.Groups["ident"].Value != "true" && match.Groups["ident"].Value != "false" && match.Groups["ident"].Value != "blank")
                    {
                        var ident = match.Groups["ident"].Value;
                        if (ident.StartsWith('\''))
                        {
                            ident = ident.Substring(1, ident.Length - 2).Replace("''", "'", StringComparison.Ordinal);
                        }

                        _engine.SetFormula(ident, match.Groups["formula"].Value, OnUpdate);
                    }

                    // function definition: <ident>( <ident> : <type>, ... ) : <type> = <formula>
                    //                      <ident>( <ident> : <type>, ... ) : <type> { <formula>; <formula>; ... }
                    else if (Regex.IsMatch(expr, @"^\s*\w+\((\s*\w+\s*\:\s*\w+\s*,?)*\)\s*\:\s*\w+\s*(\=|\{).*$", RegexOptions.Singleline))
                    {
                        var res = _engine.DefineFunctions(expr, _numberIsFloat);
                        if (res.Errors.Count() > 0)
                        {
                            throw new Exception("Error: " + res.Errors.First());
                        }
                    }

                    // eval and print everything else
                    else
                    {
                        var opts = new ParserOptions() { AllowsSideEffects = true, NumberIsFloat = _numberIsFloat };
                        var rc = new RuntimeConfig();
                        rc.SetUserInfo(_userInfo);
                        var result = Eval(expr);

                        if (output != null)
                        {
                            // same algorithm used by BaseRunner.cs
                            var sb = new StringBuilder();
                            var settings = new FormulaValueSerializerSettings()
                            {
                                UseCompactRepresentation = true,
                            };

                            // Serializer will produce a human-friedly representation of the value
                            result.ToExpression(sb, settings);

                            output.Write(sb.ToString() + "\n\n");
                        }

                        if (result is ErrorValue errorValue)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error: {errorValue.Errors[0].Kind} - {errorValue.Errors[0].Message}");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine(PrintResult(result));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);

                    if (_stackTrace)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    Console.ResetColor();
                    output?.WriteLine(Regex.Replace(e.InnerException.Message, "\r\n", "|") + "\n");
                }
            }
        }

        private static void OnUpdate(string name, FormulaValue newValue)
        {
            Console.Write($"{name}: ");
            if (newValue is ErrorValue errorValue)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + errorValue.Errors[0].Message);
                Console.ResetColor();
            }
            else
            {
                if (newValue is TableValue)
                {
                    Console.WriteLine();
                }

                Console.WriteLine(PrintResult(newValue));
            }
        }

        public static string ReadFormula(TextReader input, TextWriter output = null, bool echo = false)
        {
            string exprPartial;
            int usefulCount;

            // read
            do
            {
                string exprOne;
                int parenCount;

                exprPartial = null;

                do
                {
                    bool doubleQuote, singleQuote;
                    bool lineComment, blockComment;
                    char last;

                    if (exprPartial == null && !echo)
                    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                        Console.Write("\n> ");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                    }

                    exprOne = input.ReadLine();

                    if (exprOne == null)
                    {
                        Console.Write("\n");
                        return exprPartial;
                    }

                    exprPartial += exprOne + "\n";

                    // determines if the parens, curly braces, and square brackets are closed
                    // taking into escaping, block, and line comments
                    // and continues reading lines if they are not, with a blank link terminating
                    parenCount = 0;
                    doubleQuote = singleQuote = lineComment = blockComment = false;
                    last = '\0';
                    usefulCount = 0;
                    foreach (var c in exprPartial)
                    {
                        // don't need to worry about escaping as it looks like two 
                        if (c == '"' && !singleQuote)
                        {
                            doubleQuote = !doubleQuote; // strings that are back to back
                        }

                        if (c == '\'' && !doubleQuote)
                        {
                            singleQuote = !singleQuote;
                        }

                        if (c == '*' && last == '/' && !blockComment)
                        {
                            blockComment = true;
                            usefulCount--;                         // compensates for the last character already being added
                        }

                        if (c == '/' && last == '*' && blockComment)
                        {
                            blockComment = false;
                            usefulCount--;
                        }

                        if (!doubleQuote && !singleQuote && !blockComment && !lineComment && c == '/' && last == '/')
                        {
                            lineComment = true;
                            usefulCount--;
                        }

                        if (c == '\n')
                        {
                            lineComment = false;
                        }

                        if (!lineComment && !blockComment && !doubleQuote && !singleQuote)
                        {
                            if (c == '(' || c == '{' || c == '[')
                            {
                                parenCount++;
                            }

                            if (c == ')' || c == '}' || c == ']')
                            {
                                parenCount--;
                            }
                        }

                        if (!char.IsWhiteSpace(c) && !lineComment && !blockComment)
                        {
                            usefulCount++;
                        }

                        last = c;
                    }
                }
                while (!Regex.IsMatch(exprOne, "^\\s*$") && (parenCount != 0 || Regex.IsMatch(exprOne, "(=|=\\>)\\s*$")));

                if (echo)
                {
                    // skip >> for comments and setup
                    if (Regex.IsMatch(exprPartial, "^\\s*//") || Regex.IsMatch(exprPartial, "^#SETUP"))
                    {
                        Console.Write(exprPartial);
                        output?.Write(exprPartial + "\n");
                        usefulCount = 0;
                    }
                    else if (!Regex.IsMatch(exprPartial, "^\\s*$"))
                    {
                        Console.Write("\n>> " + exprPartial);
                        output?.Write(">> " + exprPartial);
                    }
                }
            }
            while (usefulCount == 0);

            return exprPartial;
        }

        private static string PrintResult(FormulaValue value, bool minimal = false)
        {
            string resultString = string.Empty;

            if (value is BlankValue)
            {
                resultString = minimal ? string.Empty : "Blank()";
            }
            else if (value is ErrorValue errorValue)
            {
                resultString = minimal ? "<error>" : "<Error: " + errorValue.Errors[0].Message + ">";
            }
            else if (value is UntypedObjectValue)
            {
                resultString = minimal ? "<untyped>" : "<Untyped: Use Value, Text, Boolean, or other functions to establish the type>";
            }
            else if (value is StringValue str)
            {
                resultString = minimal ? str.Value : str.ToExpression();
            }
            else if (value is RecordValue record)
            {
                if (minimal)
                {
                    resultString = "<record>";
                }
                else
                {
                    var separator = string.Empty;
                    if (_hashCodes)
                    {
                        resultString += "#" + record.GetHashCode() + "#";
                    }

                    resultString += "{";
                    foreach (var field in record.Fields)
                    {
                        resultString += separator + $"{field.Name}:";
                        resultString += PrintResult(field.Value);
                        separator = ", ";
                    }

                    resultString += "}";
                }
            }
            else if (value is TableValue table)
            {
                if (minimal)
                {
                    resultString = "<table>";
                }
                else
                {
                    var columnCount = 0;
                    foreach (var row in table.Rows)
                    {
                        if (row.Value != null)
                        {
                            columnCount = Math.Max(columnCount, row.Value.Fields.Count());
                            break;
                        }
                    }

                    if (columnCount == 0)
                    {
                        return minimal ? string.Empty : "Table()";
                    }

                    var columnWidth = new int[columnCount];

                    foreach (var row in table.Rows)
                    {
                        if (row.Value != null)
                        {
                            var column = 0;
                            foreach (var field in row.Value.Fields)
                            {
                                columnWidth[column] = Math.Max(columnWidth[column], PrintResult(field.Value, true).Length);
                                column++;
                            }
                        }
                    }

                    if (_hashCodes)
                    {
                        resultString += "#" + table.GetHashCode() + "#";
                    }

                    // special treatment for single column table named Value
                    if (columnWidth.Length == 1 && table.Rows.First().Value != null && table.Rows.First().Value.Fields.First().Name == "Value")
                    {
                        var separator = string.Empty;
                        resultString += "[";
                        foreach (var row in table.Rows)
                        {
                            resultString += separator;

                            if (_hashCodes)
                            {
                                resultString += "#" + row.Value.GetHashCode() + "# ";
                            }

                            resultString += PrintResult(row.Value.Fields.First().Value);
                            separator = ", ";
                        }

                        resultString += "]";
                    }

                    // otherwise a full table treatment is needed
                    else if (_formatTable)
                    {
                        resultString += "\n ";
                        var column = 0;

                        foreach (var row in table.Rows)
                        {
                            if (row.Value != null)
                            {
                                column = 0;
                                foreach (var field in row.Value.Fields)
                                {
                                    columnWidth[column] = Math.Max(columnWidth[column], field.Name.Length);
                                    resultString += " " + field.Name.PadLeft(columnWidth[column]) + "  ";
                                    column++;
                                }

                                break;
                            }
                        }

                        resultString += "\n ";

                        foreach (var width in columnWidth)
                        {
                            resultString += new string('=', width + 2) + " ";
                        }

                        foreach (var row in table.Rows)
                        {
                            column = 0;
                            resultString += "\n ";
                            if (row.Value != null)
                            {
                                foreach (var field in row.Value.Fields)
                                {
                                    resultString += " " + PrintResult(field.Value, true).PadLeft(columnWidth[column]) + "  ";
                                    column++;
                                }
                            }
                            else
                            {
                                resultString += row.IsError ? row.Error?.Errors?[0].Message : "Blank()";
                            }
                        }
                    }
                    else
                    {
                        // table without formatting 

                        resultString = "[";
                        var separator = string.Empty;
                        foreach (var row in table.Rows)
                        {
                            resultString += separator;

                            if (_hashCodes)
                            {
                                resultString += "#" + row.Value.GetHashCode() + "# ";
                            }

                            resultString += PrintResult(row.Value);
                            separator = ", ";
                        }

                        resultString += "]";
                    }
                }
            }
            else
            {
                var sb = new StringBuilder();
                var settings = new FormulaValueSerializerSettings()
                {
                    UseCompactRepresentation = true,
                };
                value.ToExpression(sb, settings);

                resultString = sb.ToString();
            }

            return resultString;
        }

        private class ResetFunction : ReflectionFunction
        {
            public BooleanValue Execute()
            {
                ResetEngine();
                return FormulaValue.New(true);
            }
        }

        private class ExitFunction : ReflectionFunction
        {
            public BooleanValue Execute()
            {
                System.Environment.Exit(0);
                return FormulaValue.New(true);
            }
        }

        private class OptionFunction : ReflectionFunction
        {
            // explicit constructor needed so that the return type from Execute can be FormulaValue and acoomodate both booleans and errors
            public OptionFunction()
                : base("Option", FormulaType.Boolean, new[] { FormulaType.String, FormulaType.Boolean })
            {
            }

            public FormulaValue Execute(StringValue option, BooleanValue value)
            {
                if (string.Equals(option.Value, OptionFormatTable, StringComparison.OrdinalIgnoreCase))
                {
                    _formatTable = value.Value;
                    return value;
                }

                if (string.Equals(option.Value, OptionNumberIsFloat, StringComparison.OrdinalIgnoreCase))
                {
                    _numberIsFloat = value.Value;
                    return value;
                }

                if (string.Equals(option.Value, OptionLargeCallDepth, StringComparison.OrdinalIgnoreCase))
                {
                    _largeCallDepth = value.Value;
                    ResetEngine();
                    return value;
                }

                if (string.Equals(option.Value, OptionHashCodes, StringComparison.OrdinalIgnoreCase))
                {
                    _hashCodes = value.Value;
                    return value;
                }

                if (string.Equals(option.Value, OptionStackTrace, StringComparison.OrdinalIgnoreCase))
                {
                    _stackTrace = value.Value;
                    return value;
                }

                if (string.Equals(option.Value, OptionPowerFxV1, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var prop in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (prop.PropertyType == typeof(bool) && prop.CanWrite && (bool)prop.GetValue(Features.PowerFxV1))
                        {
                            prop.SetValue(_features, value.Value);
                        }
                    }

                    ResetEngine();
                    return value;
                }

                if (string.Equals(option.Value, OptionFeaturesNone, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var prop in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (prop.PropertyType == typeof(bool) && prop.CanWrite)
                        {
                            prop.SetValue(_features, value.Value);
                        }
                    }

                    ResetEngine();
                    return value;
                }

                var featureProperty = typeof(Features).GetProperty(option.Value, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (featureProperty?.CanWrite == true)
                {
                    featureProperty.SetValue(_features, value.Value);
                    ResetEngine();
                    return value;
                }

                return FormulaValue.NewError(new ExpressionError()
                {
                        Kind = ErrorKind.InvalidArgument,
                        Severity = ErrorSeverity.Critical,
                        Message = $"Invalid option name: {option.Value}."
                });
            }
        }

        private class ImportFunction1Arg : ReflectionFunction
        {
            public BooleanValue Execute(StringValue fileNameSV)
            {
                var if2 = new ImportFunction2Arg();
                return if2.Execute(fileNameSV, null);
            }
        }

        private class ImportFunction2Arg : ReflectionFunction
        {
            public BooleanValue Execute(StringValue fileNameSV, StringValue outputSV)
            {
                var fileName = fileNameSV.Value;
                if (File.Exists(fileName))
                {
                    TextReader fileReader = new StreamReader(fileName, true);
                    TextWriter outputWriter = null;

                    if (outputSV != null)
                    {
                        outputWriter = new StreamWriter(outputSV.Value, false, System.Text.Encoding.UTF8);
                    }

                    ConsoleRepl.REPL(fileReader, true, outputWriter);
                    fileReader.Close();
                    outputWriter?.Close();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"File not found: {fileName}");
                    Console.ResetColor();
                    return BooleanValue.New(false);
                }

                return BooleanValue.New(true);
            }
        }

        private class ResetImportFunction : ReflectionFunction
        {
            public BooleanValue Execute(StringValue fileNameSV)
            {
                var import = new ImportFunction1Arg();
                if (File.Exists(fileNameSV.Value))
                {
                    ResetEngine();
                }

                return import.Execute(fileNameSV);
            }
        }

        private class HelpFunction : ReflectionFunction
        {
            public BooleanValue Execute()
            {
                var column = 0;
                var funcList = string.Empty;
                List<string> funcNames = _engine.SupportedFunctions.FunctionNames.ToList();
                
                funcNames.Sort();
                foreach (var func in funcNames)
                {
                    funcList += $"  {func,-14}";
                    if (++column % 5 == 0)
                    {
                        funcList += "\n";
                    }
                }

                funcList += "  Set";

                // If we return a string, it gets escaped. 
                // Just write to console 
                Console.WriteLine(
                @"
<formula> alone is evaluated and the result displayed.
    Example: 1+1 or ""Hello, World""
Set( <identifier>, <formula> ) creates or changes a variable's value.
    Example: Set( x, x+1 )

<identifier> = <formula> defines a named formula with automatic recalc.
    Example: F = m * a
<identifier>( <param> : <type>, ... ) : <type> = <formula> 
        extends a named formula with parameters, creating a function.
    Example: F( m: Number, a: Number ): Number = m * a
<identifier>( <param> : <type>, ... ) : <type> { 
       <expression>; <expression>; ...
       }  defines a block function with chained formulas.
    Example: Log( message: String ): None 
             { 
                    Collect( LogTable, message );
                    Notify( message );
             }
Supported types: Number, String, Boolean, DateTime, Date, Time

Available functions (all are case sensitive):
" + funcList + @"

Available operators: = <> <= >= + - * / % && And || Or ! Not in exactin 

Record syntax is { < field >: < value >, ... } without quoted field names.
    Example: { Name: ""Joe"", Age: 29 }
Use the Table function for a list of records.  
    Example: Table( { Name: ""Joe"" }, { Name: ""Sally"" } )
Use [ <value>, ... ] for a single column table, field name is ""Value"".
    Example: [ 1, 2, 3 ] 
Records and Tables can be arbitrarily nested.

Use Option( Options.FormatTable, false ) to disable table formatting.

Once a formula is defined or a variable's type is defined, it cannot be changed.
Use the Reset() function to clear all formulas and variables.
");

                return FormulaValue.New(true);
            }
        }
    }
}
