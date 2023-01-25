// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Types;

namespace PowerFxHostSamples
{
    public static class ConsoleRepl
    {
        private static RecalcEngine _engine;
        private static bool _formatTable = true;
        private const string OptionFormatTable = "FormatTable";

        private static void ResetEngine()
        {
            Features toenable = 0;
            foreach (Features feature in (Features[])Enum.GetValues(typeof(Features)))
            {
                toenable |= feature;
            }

            var config = new PowerFxConfig(toenable);
            config.SymbolTable.EnableMutationFunctions();

            config.AddFunction(new HelpFunction());
            config.AddFunction(new ResetFunction());
            config.AddFunction(new ExitFunction());
            config.AddFunction(new OptionFunction());
            config.AddFunction(new ResetImportFunction());
            config.AddFunction(new ImportFunction());

            var optionsSet = new OptionSet("Options", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
                                            {
                                                    { OptionFormatTable, OptionFormatTable },
                                            }));

            config.AddOptionSet(optionsSet);

            _engine = new RecalcEngine(config);
        }

        public static void Main()
        {
            var enabled = new StringBuilder();

            ResetEngine();

            var version = typeof(RecalcEngine).Assembly.GetName().Version.ToString();
            Console.WriteLine($"Microsoft Power Fx Console Formula REPL, Version {version}");

            foreach (Features feature in (Features[])Enum.GetValues(typeof(Features)))
            {
                if ((_engine.Config.Features & feature) == feature && feature != Features.None)
                {
                    enabled.Append(" " + feature.ToString());
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
            var parserOptions = new ParserOptions { AllowsSideEffects = true };

            var parse = _engine.Parse(expr);
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

                                var check = _engine.Check(arg1expr);
                                if (check.IsSuccess)
                                {
                                    var arg1Type = check.ReturnType;

                                    varValue = check.GetEvaluator().Eval();
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

        public static void REPL(TextReader input, bool echo)
        {
            string expr;

            // main loop
            while ((expr = ReadFormula(input, echo)) != null)
            {
                Match match;

                try
                {
                    // variable assignment: Set( <ident>, <expr> )
                    if (TryMatchSet(expr, out var varName, out var varValue))
                    {
                        Console.WriteLine(varName + ": " + PrintResult(varValue));
                    }

                    // formula definition: <ident> = <formula>
                    else if ((match = Regex.Match(expr, @"^\s*(?<ident>\w+)\s*=(?<formula>.*)$", RegexOptions.Singleline)).Success)
                    {
                        _engine.SetFormula(match.Groups["ident"].Value, match.Groups["formula"].Value, OnUpdate);
                    }

                    // function definition: <ident>( <ident> : <type>, ... ) : <type> = <formula>
                    //                      <ident>( <ident> : <type>, ... ) : <type> { <formula>; <formula>; ... }
                    else if (Regex.IsMatch(expr, @"^\s*\w+\((\s*\w+\s*\:\s*\w+\s*,?)*\)\s*\:\s*\w+\s*(\=|\{).*$", RegexOptions.Singleline))
                    {
                        var res = _engine.DefineFunctions(expr);
                        if (res.Errors.Count() > 0)
                        {
                            throw new Exception("Error: " + res.Errors.First());
                        }
                    }

                    // eval and print everything else
                    else
                    {
                        var opts = new ParserOptions { AllowsSideEffects = true };
                        var result = _engine.Eval(expr, options: opts);

                        if (result is ErrorValue errorValue)
                        {
                            throw new Exception("Error: " + errorValue.Errors[0].Message);
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
                    Console.ResetColor();
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

        public static string ReadFormula(TextReader input, bool echo)
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

                if (echo && !Regex.IsMatch(exprPartial, "^\\s*$"))
                {
                    Console.Write("\n>> " + exprPartial);
                }
            }
            while (usefulCount == 0);

            return exprPartial;
        }

        private static string PrintResult(FormulaValue value, bool minimal = false)
        {
            string resultString;

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
                    resultString = "{";
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
                        }
                    }

                    if (columnCount == 0)
                    {
                        return minimal ? string.Empty : "Blank()";
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

                    // special treatment for single column table named Value
                    if (columnWidth.Length == 1 && table.Rows.First().Value != null && table.Rows.First().Value.Fields.First().Name == "Value")
                    {
                        var separator = string.Empty;
                        resultString = "[";
                        foreach (var row in table.Rows)
                        {
                            resultString += separator + PrintResult(row.Value.Fields.First().Value);
                            separator = ", ";
                        }

                        resultString += "]";
                    }

                    // otherwise a full table treatment is needed
                    else if (_formatTable)
                    {
                        resultString = "\n ";
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
                            if (row.Value != null)
                            {
                                column = 0;
                                resultString += "\n ";
                                foreach (var field in row.Value.Fields)
                                {
                                    resultString += " " + PrintResult(field.Value, true).PadLeft(columnWidth[column]) + "  ";
                                    column++;
                                }
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
                            resultString += separator + PrintResult(row.Value);
                            separator = ", ";
                        }

                        resultString += "]";
                    }
                }
            }
            else
            {
                resultString = value.ToExpression();
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
                if (option.Value.ToLower() == OptionFormatTable.ToLower())
                {
                    _formatTable = value.Value;
                    return value;
                }
                else
                {
                    return FormulaValue.NewError(new ExpressionError()
                    {
                        Kind = ErrorKind.InvalidArgument,
                        Severity = ErrorSeverity.Critical,
                        Message = $"Invalid option name: {option.Value}."
                    });
                }
            }
        }

        private class ImportFunction : ReflectionFunction
        {
            public ImportFunction()
                : base("Import", FormulaType.Boolean, new[] { FormulaType.String })
            {
            }

            public FormulaValue Execute(StringValue fileNameSV)
            {
                var fileName = fileNameSV.Value;
                if (File.Exists(fileName))
                {
                    TextReader fileReader = new StreamReader(fileName);
                    ConsoleRepl.REPL(fileReader, true);
                    fileReader.Close();
                }
                else
                {
                    return FormulaValue.NewError(new ExpressionError()
                    {
                        Kind = ErrorKind.InvalidArgument,
                        Severity = ErrorSeverity.Critical,
                        Message = $"File not found: {fileName}."
                    });
                }

                return FormulaValue.New(true);
            }
        }

        private class ResetImportFunction : ReflectionFunction
        {
            public ResetImportFunction()
                : base("ResetImport", FormulaType.Boolean, new[] { FormulaType.String })
            {
            }

            public FormulaValue Execute(StringValue fileNameSV)
            {
                var import = new ImportFunction();
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
#pragma warning disable CS0618 // Type or member is obsolete
                var funcNames = _engine.Config.FunctionInfos.Select(x => x.Name).Distinct();
#pragma warning restore CS0618 // Type or member is obsolete

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
