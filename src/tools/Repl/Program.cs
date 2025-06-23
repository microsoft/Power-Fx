﻿// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Repl;
using Microsoft.PowerFx.Repl.Functions;
using Microsoft.PowerFx.Repl.Services;
using Microsoft.PowerFx.Types;
using YamlDotNet.Serialization;

namespace Microsoft.PowerFx
{
#pragma warning disable CS0618 // Type or member is obsolete

    public static class ConsoleRepl
    { 
        private const string OptionFormatTable = "FormatTable";

        private const string OptionNumberIsFloat = "NumberIsFloat";
        private static bool _numberIsFloat = false;

        private const string OptionLargeCallDepth = "LargeCallDepth";
        private static bool _largeCallDepth = false;

        private const string OptionFeaturesNone = "FeaturesNone";

        private const string OptionPowerFxV1 = "PowerFxV1";

        private const string OptionHashCodes = "HashCodes";

        private const string OptionStackTrace = "StackTrace";
        private static bool _stackTrace = false;

        private const string OptionTextFirst = "TextFirst";
        private static bool _textFirst = false;

        private const string OptionAllowSideEffects = "AllowSideEffects";
        private static bool _allowSideEffects = true;

        private const string OptionNumberedPrompts = "NumberedPrompts";
        private static bool _numberedPrompts = false;

#if MATCHCOMPARE
        // to enable, place this in Solution Items/Directiory.Build.Props:
        //  <PropertyGroup>
        //      <DefineConstants>$(DefineConstants);MATCHCOMPARE</DefineConstants>
        //  </PropertyGroup>

        private const string OptionMatchCompare = "MatchCompare";
        private static bool _matchCompare = true;
#endif

        private const string OptionUDF = "UserDefinedFunctions";
        private static bool _enableUDFs = true;

        private static readonly Features _features = Features.PowerFxV1;

        private static StandardFormatter _standardFormatter;

        private static CultureInfo _cultureInfo = CultureInfo.CurrentCulture;

        private static bool _reset;

        private static RecalcEngine ReplRecalcEngine()
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
                { OptionStackTrace, OptionStackTrace },
                { OptionTextFirst, OptionTextFirst },
                { OptionAllowSideEffects, OptionAllowSideEffects },
                { OptionNumberedPrompts, OptionNumberedPrompts },
#if MATCHCOMPARE
                { OptionMatchCompare, OptionMatchCompare },
#endif
                { OptionUDF, OptionUDF },
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
            config.EnableJsonFunctions();
            config.EnableOptionSetInfo();
            config.EnableJoinFunction();

            config.AddFunction(new AssertFunction());

            config.AddFunction(new ResetFunction());
            config.AddFunction(new Option0Function());
            config.AddFunction(new Option1Function());
            config.AddFunction(new Option2Function());
            config.AddFunction(new Run1Function());
            config.AddFunction(new Run2Function());
            config.AddFunction(new Language1Function());

            var optionsSet = new OptionSet("Options", DisplayNameUtility.MakeUnique(options));

#if MATCHCOMPARE
            if (_matchCompare)
            {
                // requires PCRE2 DLL (pcre2-16d.dll) on the path and Node.JS installed
                // can also use RegEx_PCRE2 and RegEx_NodeJS directly too
                Functions.RegEx_Compare.EnableRegExFunctions(config, new TimeSpan(0, 0, 5), regexCacheSize: 100, includeDotNet: true, includeNode: true, includePCRE2: true);
            }
            else
#endif
            {
#pragma warning disable CS0618 // Type or member is obsolete
                config.EnableRegExFunctions(new TimeSpan(0, 0, 5), regexCacheSize: 100);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            config.AddOptionSet(optionsSet);

            return new RecalcEngine(config);
        }

        public static void Main()
        {
            var enabled = new StringBuilder();

            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var version = typeof(RecalcEngine).Assembly.GetName().Version.ToString();
            Console.WriteLine($"Microsoft Power Fx Console Formula REPL, Version {version}");

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Enter Excel formulas.  Use \"Help()\" for details, \"Option()\" for options.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            REPL(Console.In, prompt: true, echo: false, printResult: true, lineNumber: null);
        }

#pragma warning disable CS0618

        // Hook repl engine with customizations.
        private class MyRepl : PowerFxREPL
        {
            public int _promptNumber = 1;

            public override string Prompt => _numberedPrompts ? $"\n{_promptNumber++}>> " : "\n>> ";

            public MyRepl()
            {
                this.Engine = ReplRecalcEngine();

                _standardFormatter = new StandardFormatter();
                this.ValueFormatter = _standardFormatter;
                this.HelpProvider = new MyHelpProvider();

                var bsp = new BasicServiceProvider();
                bsp.AddService(_cultureInfo);
                this.InnerServices = bsp;

                this.AllowSetDefinitions = true;
                this.AllowUserDefinedFunctions = _enableUDFs;
                this.AllowImport = true;

                this.EnableSampleUserObject();
                this.AddPseudoFunction(new IRPseudoFunction());
                this.AddPseudoFunction(new CIRPseudoFunction());
                this.AddPseudoFunction(new SuggestionsPseudoFunction());

                this.ParserOptions = new ParserOptions() { AllowsSideEffects = _allowSideEffects, NumberIsFloat = _numberIsFloat, TextFirst = _textFirst };
            }

            public override async Task OnEvalExceptionAsync(Exception e, CancellationToken cancel)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);

                if (ConsoleRepl._stackTrace)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.ResetColor();
            }
        }

        public static void REPL(TextReader input, bool prompt, bool echo, bool printResult, uint? lineNumber)
        {
            while (true)
            {
                var repl = new MyRepl() { Echo = echo, PrintResult = printResult };

                while (!_reset)
                {
                    if (prompt)
                    {
                        repl.WritePromptAsync().Wait();
                    }

                    var line = input.ReadLine();

                    // End of file
                    if (line == null)
                    {
                        return;
                    }

                    repl.HandleLineAsync(line, lineNumber: lineNumber++).Wait();

                    // Exit() function called
                    if (repl.ExitRequested)
                    {
                        return;
                    }
                }

                _reset = false;
            }
        }

        private class ResetFunction : ReflectionFunction
        {
            public BooleanValue Execute()
            {
                _reset = true;
                return FormulaValue.New(true);
            }
        }

        private class Run1Function : ReflectionFunction
        {
            public Run1Function()
                : base("Run", FormulaType.Void, new[] { FormulaType.String })
            {
            }

            public FormulaValue Execute(StringValue file)
            {
                var run2 = new Run2Function();
                return run2.Execute(file, FormulaValue.New(false));
            }
        }

        private class Run2Function : ReflectionFunction
        {
            public Run2Function()
                : base("Run", FormulaType.Void, new[] { FormulaType.String, FormulaType.Boolean })
            {
            }

            public FormulaValue Execute(StringValue file, BooleanValue echo)
            {
                try
                {
                    var reader = new StreamReader(file.Value);
                    REPL(reader, prompt: false, echo: echo.Value, printResult: echo.Value, lineNumber: 1);
                    reader.Dispose();
                }
                catch (Exception ex)
                {
                    var error = new ExpressionError() { Message = ex.Message };
                    return FormulaValue.NewError(error);
                }

                return FormulaValue.NewVoid();
            }
        }

        private class Option0Function : ReflectionFunction
        {
            public Option0Function()
                : base("Option", FormulaType.String)
            {
            }

            public FormulaValue Execute()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("\n");

                sb.Append(CultureInfo.InvariantCulture, $"{"FormatTable:",-42}{_standardFormatter.FormatTable}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"HashCodes:",-42}{_standardFormatter.HashCodes}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"NumberIsFloat:",-42}{_numberIsFloat}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"LargeCallDepth:",-42}{_largeCallDepth}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"StackTrace:",-42}{_stackTrace}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"TextFirst:",-42}{_textFirst}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"UserDefinedFunctions:",-42}{_enableUDFs}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"AllowSideEffects:",-42}{_allowSideEffects}\n");
                sb.Append(CultureInfo.InvariantCulture, $"{"NumberedPrompts:",-42}{_numberedPrompts}\n");
#if MATCHCOMPARE
                sb.Append(CultureInfo.InvariantCulture, $"{"MatchCompare:",-42}{_matchCompare}\n");
#endif

                foreach (var prop in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (prop.PropertyType == typeof(bool) && prop.CanWrite)
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"{prop.Name + ((bool)prop.GetValue(Features.PowerFxV1) ? " (V1)" : string.Empty) + ":",-42}{prop.GetValue(_features)}\n");
                    }
                }

                return FormulaValue.New(sb.ToString());
            }
        }

        // displays a single setting
        private class Option1Function : ReflectionFunction
        {
            public Option1Function()
                : base("Option", FormulaType.Boolean, new[] { FormulaType.String })
            {
            }

            public FormulaValue Execute(StringValue option)
            {
                if (string.Equals(option.Value, OptionFormatTable, StringComparison.OrdinalIgnoreCase))
                {
                    return BooleanValue.New(_standardFormatter.FormatTable);
                }

                if (string.Equals(option.Value, OptionNumberIsFloat, StringComparison.OrdinalIgnoreCase))
                {
                    return BooleanValue.New(_numberIsFloat);
                }

                if (string.Equals(option.Value, OptionLargeCallDepth, StringComparison.OrdinalIgnoreCase))
                {
                    return BooleanValue.New(_largeCallDepth);
                }

                if (string.Equals(option.Value, OptionHashCodes, StringComparison.OrdinalIgnoreCase))
                {
                    return BooleanValue.New(_standardFormatter.HashCodes);
                }

                if (string.Equals(option.Value, OptionStackTrace, StringComparison.OrdinalIgnoreCase))
                {
                    return BooleanValue.New(_stackTrace);
                }

                if (string.Equals(option.Value, OptionAllowSideEffects, StringComparison.OrdinalIgnoreCase))
                {
                    return BooleanValue.New(_allowSideEffects);
                }

                if (string.Equals(option.Value, OptionUDF, StringComparison.OrdinalIgnoreCase))
                {
                    return BooleanValue.New(_enableUDFs);
                }

                return FormulaValue.NewError(new ExpressionError()
                {
                    Kind = ErrorKind.InvalidArgument,
                    Severity = ErrorSeverity.Critical,
                    Message = $"Invalid option name: {option.Value}.  Use \"Option()\" to see available Options enum names."
                });
            }
        }

        // change a setting
        private class Option2Function : ReflectionFunction
        {
            // explicit constructor needed so that the return type from Execute can be FormulaValue and acoomodate both booleans and errors
            public Option2Function()
                : base("Option", FormulaType.Boolean, new[] { FormulaType.String, FormulaType.Boolean })
            {
            }

            public FormulaValue Execute(StringValue option, BooleanValue value)
            {
                if (string.Equals(option.Value, OptionFormatTable, StringComparison.OrdinalIgnoreCase))
                {   
                    _standardFormatter.FormatTable = value.Value;
                    return value;
                }

                if (string.Equals(option.Value, OptionNumberIsFloat, StringComparison.OrdinalIgnoreCase))
                {
                    _numberIsFloat = value.Value;
                    _reset = true;
                    return value;
                }

                if (string.Equals(option.Value, OptionTextFirst, StringComparison.OrdinalIgnoreCase))
                {
                    _textFirst = value.Value;
                    _reset = true;
                    return value;
                }

                if (string.Equals(option.Value, OptionUDF, StringComparison.OrdinalIgnoreCase))
                {
                    _enableUDFs = value.Value;
                    _reset = true;
                    return value;
                }

                if (string.Equals(option.Value, OptionAllowSideEffects, StringComparison.OrdinalIgnoreCase))
                {
                    _allowSideEffects = value.Value;
                    _reset = true;
                    return value;
                }

                if (string.Equals(option.Value, OptionLargeCallDepth, StringComparison.OrdinalIgnoreCase))
                {
                    _largeCallDepth = value.Value;
                    _reset = true;
                    return value;
                }

                if (string.Equals(option.Value, OptionHashCodes, StringComparison.OrdinalIgnoreCase))
                {
                    _standardFormatter.HashCodes = value.Value;
                    return value;
                }

                if (string.Equals(option.Value, OptionStackTrace, StringComparison.OrdinalIgnoreCase))
                {
                    _stackTrace = value.Value;
                    return value;
                }

                if (string.Equals(option.Value, OptionNumberedPrompts, StringComparison.OrdinalIgnoreCase))
                {
                    _numberedPrompts = value.Value;
                    return value;
                }

#if MATCHCOMPARE
                if (string.Equals(option.Value, OptionMatchCompare, StringComparison.OrdinalIgnoreCase))
                {
                    _matchCompare = value.Value;
                    _reset = true;
                    return value;
                }
#endif

                if (string.Equals(option.Value, OptionPowerFxV1, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var prop in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                            if (prop.PropertyType == typeof(bool) && prop.CanWrite)
                        {
                            prop.SetValue(_features, value.Value);
                        }
                    }

                    _reset = true;
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

                    _reset = true;
                    return value;
                }

                var featureProperty = typeof(Features).GetProperty(option.Value, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (featureProperty?.CanWrite == true)
                {
                    featureProperty.SetValue(_features, value.Value);
                    _reset = true;
                    return value;
                }

                return FormulaValue.NewError(new ExpressionError()
                {
                    Kind = ErrorKind.InvalidArgument,
                    Severity = ErrorSeverity.Critical,
                    Message = $"Invalid option name: {option.Value}.  Use \"Option()\" to see available Options enum names."
                });
            }
        }

        // set the language
        private class Language1Function : ReflectionFunction
        {
            public Language1Function()
                : base("Language", FormulaType.Void, new[] { FormulaType.String })
            {
            }

            public FormulaValue Execute(StringValue lang)
            {
                var cultureInfo = new CultureInfo(lang.Value);

                _cultureInfo = cultureInfo;

                _reset = true;

                return FormulaValue.NewVoid();
            }
        }

        private class MyHelpProvider : HelpProvider
        {
            public override async Task Execute(PowerFxREPL repl, CancellationToken cancel, string context = null)
            {
                if (context?.ToLowerInvariant() == "options" || context?.ToLowerInvariant() == "option")
                {
                    var msg =
@"

Options.AllowSideEffects
    Enables functions with side effects (Set, Notify, Collect, etc).
    Resets the engine.

Options.EnableUDFs
    Enables UserDefinedFunctions to be added.
    Resets the engine.

Options.FormatTable
    Displays tables in a tabular format rather than using Table() function notation.

Options.PowerFxV1
    Sets all the feature flags for Power Fx 1.0.
    Resets the engine.

Options.HashCodes        
    When printing, includes hash codes of each object to better understand references.
    This can be very helpful for debugging copy-on-mutation semantics.

Options.None
    Removed all the feature flags, which is even less than Canvas uses.
    Resets the engine.

Options.NumberedPrompts
    Adds a prompt number to make it easier to discuss results with others.
    Reset, by calling Reset() or changing an option that resets, will restart the numbering.

Options.NumberIsFloat
    By default, literal numeric values such as ""1.23"" and the return type from the 
    Value function are treated as decimal values.  Turning this flag on changes that
    to floating point instead.  To test, ""1e300"" is legal in floating point but not decimal.
    Resets the engine.

Options.LargeCallDepth
    Expands the call stack for testing complex user defined functions.
    Resets the engine.

Options.StackTrace
    Displays the full stack trace when an exception is encountered.

Options.TextFirst
    Use the Text First parser mode, where the formula is interpreted as if it 
    started with string interpolation, and formulas that begin with `=` are interpreted
    as a literal formula.
    Rests the engine.

"
#if MATCHCOMPARE
+
@"Options.MatchCompare
    For Match functions, compares the results between the .NET, PCRE2, and Node engines.
    Performance will be slower than normal as all three engines are consulted.
    Rests the engine.

"
#endif
;

                    await WriteAsync(repl, msg, cancel)
                        .ConfigureAwait(false);

                    return;
                }

                var pre =
@"
<formula> alone is evaluated and the result displayed.
    Example: 1+1 or ""Hello, World""
Set( <identifier>, <formula> ) creates or changes a variable's value.
    Example: Set( x, x+1 )

<identifier> = <formula> defines a named formula with automatic recalc.
    Example: F = m * a

Available functions (case sensitive):
";

                var post =
@"
Available operators: = <> <= >= + - * / % ^ && And || Or ! Not in exactin 

Record syntax is { < field >: < value >, ... } without quoted field names.
    Example: { Name: ""Joe"", Age: 29 }
Use the Table function for a list of records.  
    Example: Table( { Name: ""Joe"" }, { Name: ""Sally"" } )
Use [ <value>, ... ] for a single column table, field name is ""Value"".
    Example: [ 1, 2, 3 ] 
Records and Tables can be arbitrarily nested.

Use Option( Options.FormatTable, false ) to disable table formatting.
Use Option() to see the list of all options with their current value.
Use Help( ""Options"" ) for more information.

Use Language( ""en-US"" ) to set culture info.

Once a formula is defined or a variable's type is defined, it cannot be changed.
Use Reset() to clear all formulas and variables.
";

                await WriteAsync(repl, pre, cancel)
                    .ConfigureAwait(false);

                await WriteAsync(repl, FormatFunctionsList(repl.FunctionNames), cancel)
                    .ConfigureAwait(false);

                await WriteAsync(repl, $"\nFormula reference: {FormulaRefURL}\n", cancel)
                    .ConfigureAwait(false);

                await WriteAsync(repl, post, cancel)
                    .ConfigureAwait(false);
            }
        }
    }
}
