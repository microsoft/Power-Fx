// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Repl;
using Microsoft.PowerFx.Repl.Functions;
using Microsoft.PowerFx.Repl.Services;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
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

        private static readonly Features _features = Features.PowerFxV1;

        private static StandardFormatter _standardFormatter;

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
            config.EnableJsonFunctions();

            config.AddFunction(new ResetFunction());
            config.AddFunction(new ExitFunction());
            config.AddFunction(new Option0Function());
            config.AddFunction(new Option1Function());
            config.AddFunction(new Option2Function());

            var optionsSet = new OptionSet("Options", DisplayNameUtility.MakeUnique(options));

#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));
#pragma warning restore CS0618 // Type or member is obsolete

            config.AddOptionSet(optionsSet);

            return new RecalcEngine(config);
        }

        public static void Main()
        {
            var enabled = new StringBuilder();

            Console.InputEncoding = System.Text.Encoding.Unicode;
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            var version = typeof(RecalcEngine).Assembly.GetName().Version.ToString();
            Console.WriteLine($"Microsoft Power Fx Console Formula REPL, Version {version}");

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Enter Excel formulas.  Use \"Help()\" for details, \"Option()\" for options.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            REPL();
        }

        // Hook repl engine with customizations.
#pragma warning disable CS0618 // Type or member is obsolete
        private class MyRepl : PowerFxREPL
#pragma warning restore CS0618 // Type or member is obsolete
        {
            public MyRepl()
            {
                this.Engine = ReplRecalcEngine();

                _standardFormatter = new StandardFormatter();
                this.ValueFormatter = _standardFormatter;
                this.HelpProvider = new MyHelpProvider();

                this.AllowSetDefinitions = true;
                this.EnableSampleUserObject();
                this.AddPseudoFunction(new IRPseudoFunction());
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

        public static void REPL()
        {
            while (true)
            {
                var repl = new MyRepl();

                while (!_reset)
                {
                    repl.WritePromptAsync().Wait();
                    var line = Console.ReadLine();
                    repl.HandleLineAsync(line).Wait();
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

        private class ExitFunction : ReflectionFunction
        {
            public BooleanValue Execute()
            {
                System.Environment.Exit(0);
                return FormulaValue.New(true);
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

                sb.Append($"{"FormatTable:",-42}{_standardFormatter.FormatTable}\n");
                sb.Append($"{"HashCodes:",-42}{_standardFormatter.HashCodes}\n");
                sb.Append($"{"NumberIsFloat:",-42}{_numberIsFloat}\n");
                sb.Append($"{"LargeCallDepth:",-42}{_largeCallDepth}\n");
                sb.Append($"{"StackTrace:",-42}{_stackTrace}\n");

                foreach (var prop in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (prop.PropertyType == typeof(bool) && prop.CanWrite)
                    {
                        sb.Append($"{prop.Name + ((bool)prop.GetValue(Features.PowerFxV1) ? " (V1)" : string.Empty) + ":",-42}{prop.GetValue(_features)}\n");
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

                if (string.Equals(option.Value, OptionPowerFxV1, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var prop in typeof(Features).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (prop.PropertyType == typeof(bool) && prop.CanWrite && (bool)prop.GetValue(Features.PowerFxV1))
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

        private class MyHelpProvider : HelpProvider
        {
#pragma warning disable CS0618 // Type or member is obsolete
            public override async Task Execute(PowerFxREPL repl, CancellationToken cancel, string context = null)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                if (context?.ToLowerInvariant() == "options" || context?.ToLowerInvariant() == "option")
                {
                    var msg =
@"
Options.FormatTable
    Displays tables in a tabular format rather than using Table() function notation.

Options.HashCodes        
    When printing, includes hash codes of each object to better understand references.
    This can be very helpful for debugging copy-on-mutation semantics.

Options.NumberIsFloat
    By default, literal numeric values such as ""1.23"" and the return type from the 
    Value function are treated as decimal values.  Turning this flag on changes that
    to floating point instead.  To test, ""1e300"" is legal in floating point but not decimal.

Options.LargeCallDepth
    Expands the call stack for testing complex user defined functions.

Options.StackTrace
    Displays the full stack trace when an exception is encountered.

Options.PowerFxV1
    Sets all the feature flags for Power Fx 1.0.

Options.None
    Removed all the feature flags, which is even less than Canvas uses.

";

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
