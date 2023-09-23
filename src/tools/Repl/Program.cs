// <copyright file="Program.cs" company="PlaceholderCompany">
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
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.REPL;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public class TestREPL : RecalcEngineREPL
    {
        public bool _hashCodes = false;
        public bool _formatTable = true;

        public TestREPL(PowerFxConfig config, bool outputConsole)
            : base(config, outputConsole)
        {
        }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public override FormulaValue? Eval(string expr, TextWriter? output, bool echo)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            Match match;

            // IR pretty printer: IR( <expr> )
            if ((match = Regex.Match(expr, @"^\s*IR\((?<expr>.*)\)\s*$", RegexOptions.Singleline)).Success)
            {
                var opts = this.GetParserOptions();
                var cr = Engine.Check(match.Groups["expr"].Value, options: opts);
                var ir = cr.PrintIR();
                if (_outputConsole)
                { 
                    Console.WriteLine(ir);
                }

                return null;
            }

            // Evertyhing else
            else
            {
                return base.Eval(expr, output, echo);
            }
        }

        public override string PrintResult(FormulaValue value, bool minimal = false)
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
    }

    public static class ConsoleRepl
    {
        private static TestREPL _repl;

        private const string OptionFormatTable = "FormatTable";

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

        private static readonly Features _features = Features.PowerFxV1;

        private static void ResetEngine()
        {
            var config = new PowerFxConfig(_features);

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

#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));
#pragma warning restore CS0618 // Type or member is obsolete

            config.AddOptionSet(optionsSet);

            _repl = new TestREPL(config, true);
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
                if (propertyInfo.PropertyType == typeof(bool) && ((bool)propertyInfo.GetValue(_repl.Engine.Config.Features)) == true)
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

            _repl.REPL(Console.In, null, false);
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
                    _repl._formatTable = value.Value;
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

                    _repl.REPL(fileReader, outputWriter, true);
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
                List<string> funcNames = _repl.Engine.SupportedFunctions.FunctionNames.ToList();
                
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
