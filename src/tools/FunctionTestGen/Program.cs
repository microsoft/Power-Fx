// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace FunctionTestGen
{
    public class Program
    {
        private static RecalcEngine _engine;
        private const string OptionFormatTable = "FormatTable";

        private static void ResetEngine()
        {
            Features toenable = 0;
            foreach (Features feature in (Features[])Enum.GetValues(typeof(Features)))
            {
                toenable |= feature;
            }

            var config = new PowerFxConfig(toenable);

            var optionsSet = new OptionSet("Options", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                { OptionFormatTable, OptionFormatTable },
            }));

            config.AddOptionSet(optionsSet);

            _engine = new RecalcEngine(config);
        }

        public static void Main(string[] args)
        {
            ResetEngine();
            var skippedFunctions = new HashSet<string>() { "Acos", "Asin", "DateAdd", "DateDiff", "Date", "Time", "DateTime" };

            foreach (var texlFunction in BuiltinFunctionsCore.BuiltinFunctionsLibrary)
            {
                if (HasAnyUntypedObject(texlFunction) || texlFunction.MaxArity == 0)
                {
                    skippedFunctions.Add(texlFunction.Name);
                }
            }

            foreach (var texlFunction in BuiltinFunctionsCore.BuiltinFunctionsLibrary)
            {
                if (texlFunction.HasLambdas ||
                    !texlFunction.SupportsParamCoercion ||
                    skippedFunctions.Contains(texlFunction.Name) ||
                    HasAnyUnsupportedTypes(texlFunction))
                {
                    continue;
                }

                PrintTest(texlFunction);
            }
        }

        private static void PrintTest(TexlFunction texlFunction)
        {
            for (var arity = texlFunction.MinArity; arity <= texlFunction.MaxArity; arity++)
            {
                if (arity == 0)
                {
                    continue;
                }

                var callArgDefaults = new string[arity];
                var callArgsParseJSON = new string[arity];

                for (var i = 0; i < arity; i++)
                {
                    if (texlFunction.ParamTypes.Length > i)
                    {
                        callArgDefaults[i] = GetDefaultValue(texlFunction.ParamTypes[i].Kind);
                        callArgsParseJSON[i] = GetParseJSONValue(texlFunction.ParamTypes[i].Kind);
                    }
                    else
                    {
                        return;
                    }
                }

                var defaultCall = $"{texlFunction.Name}({string.Join(", ", callArgDefaults)})";
                try
                {
                    var defaultResult = _engine.Eval(defaultCall);

                    if (defaultResult is not ErrorValue)
                    {
                        //Console.WriteLine(">> " + defaultCall);
                        var printDefaultResult = ValueToString(defaultResult);

                        //Console.WriteLine(printDefaultResult);

                        //Console.WriteLine();

                        for (var i = 0; i < arity; i++)
                        {
                            var callArgs = new string[arity];
                            for (var j = 0; j < arity; j++)
                            {
                                if (j == i)
                                {
                                    callArgs[j] = callArgsParseJSON[j];
                                }
                                else
                                {
                                    callArgs[j] = callArgDefaults[j];
                                }
                            }

                            var call = $"{texlFunction.Name}({string.Join(", ", callArgs)})";
                            var result = _engine.Eval(call);

                            if (result is ErrorValue)
                            {
                                Console.WriteLine(">> " + defaultCall);
                                Console.WriteLine("Delta error!");

                                Console.WriteLine();
                                continue;
                            }

                            var printResult = ValueToString(result);
                            if (printResult != printDefaultResult)
                            {
                                Console.WriteLine(">> " + defaultCall);
                                Console.WriteLine("Delta result!");

                                Console.WriteLine();
                                continue;
                            }

                            Console.WriteLine($">> {defaultCall} = {call}");
                            Console.WriteLine("true");

                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private static string ValueToString(FormulaValue value)
        {
            var sb = new StringBuilder();

            var settings = new FormulaValueSerializerSettings()
            {
                UseCompactRepresentation = true,
            };

            // Serializer will produce a human-friedly representation of the value
            value.ToExpression(sb, settings);

            return sb.ToString();
        }

        private static string GetDefaultValue(DKind kind)
        {
            switch (kind)
            {
                case DKind.String:
                    return "\"Hello World\"";
                case DKind.Number:
                    return "5";
                case DKind.Boolean:
                    return "true";
                case DKind.Time:
                    return "TimeValue(\"12:08:45.000Z\")";
                case DKind.Date:
                    return "DateValue(\"2022-12-19\")";
                case DKind.DateTime:
                    return "DateTimeValue(\"2022-12-19T12:08:45.000Z\")";
                case DKind.Color:
                    return "ColorValue(\"#aabbccdd\")";
                case DKind.Guid:
                    return "GUID(\"5cc45615-f759-4a53-b225-d3a2497f60ad\")";
                default:
                    return string.Empty;
            }
        }

        private static string GetParseJSONValue(DKind kind)
        {
            switch (kind)
            {
                case DKind.String:
                    return "ParseJSON(\"\"\"Hello World\"\"\")";
                case DKind.Number:
                    return "ParseJSON(\"5\")";
                case DKind.Boolean:
                    return "ParseJSON(\"true\")";
                case DKind.Time:
                    return "ParseJSON(\"\"\"12:08:45.000Z\"\"\")";
                case DKind.Date:
                    return "ParseJSON(\"\"\"2022-12-19\"\"\")";
                case DKind.DateTime:
                    return "ParseJSON(\"\"\"2022-12-19T12:08:45.000Z\"\"\")";
                case DKind.Color:
                    return "ParseJSON(\"\"\"#aabbccdd\"\"\")";
                case DKind.Guid:
                    return "ParseJSON(\"\"\"5cc45615-f759-4a53-b225-d3a2497f60ad\"\"\")";
                default:
                    return string.Empty;
            }
        }

        private static bool HasAnyUnsupportedTypes(TexlFunction function)
        {
            foreach (var type in function.ParamTypes)
            {
                if (!IsSupportedType(type.Kind))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyUntypedObject(TexlFunction function)
        {
            foreach (var type in function.ParamTypes)
            {
                if (type.Kind == DKind.UntypedObject)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedType(DKind kind)
        {
            switch (kind)
            {
                case DKind.String:
                case DKind.Number:
                case DKind.Boolean:
                case DKind.Time:
                case DKind.Date:
                case DKind.DateTime:
                case DKind.Color:
                case DKind.Guid:
                    return true;
                default:
                    return false;
            }
        }
    }
}
