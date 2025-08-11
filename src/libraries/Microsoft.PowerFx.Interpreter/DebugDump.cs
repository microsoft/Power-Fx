﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Helpers for getting a debugging log for various Power Fx core objects. 
    /// These should just be used for diagnostic purposes / logging and can change at any time. 
    /// They can also be invoked under a debugger. 
    /// </summary>
    internal static class DebugDump
    {
        public static string Dump(this ReadOnlySymbolTable symbolTable)
        {
            using var tw = new StringWriter();
            Dump(symbolTable, tw);
            return tw.ToString();
        }

        public static void Dump(this ReadOnlySymbolTable symbolTable, TextWriter tw, string indent = "")
        {
            // These should also align with intellisense APIs. 
            tw.WriteLine($"{indent}SymbolTable '{symbolTable.DebugName()}', hash={symbolTable.VersionHash.GetHashCode()}");

            if (symbolTable is ComposedReadOnlySymbolTable composed)
            {
                tw.WriteLine(indent + " Composed {");
                foreach (var table in composed.SubTables)
                {
                    Dump(table, tw, indent + "  ");
                }

                tw.WriteLine(indent + "}");
            }
            else
            {
                var values = (IGlobalSymbolNameResolver)symbolTable;
                foreach (var kv in values.GlobalSymbols)
                {
                    var name = kv.Key;
                    var info = kv.Value;

                    tw.WriteLine($"{indent} {name}:{info.Type}");
                }

                // Functions
                if (symbolTable.Functions.Any())
                {
                    tw.WriteLine();
                    tw.WriteLine($"{indent} Functions ({symbolTable.Functions.Count()}) total:");

                    foreach (var funcName in symbolTable.Functions.FunctionNames)
                    {
                        tw.WriteLine($"{indent} {funcName}");
                    }
                }
            }

            tw.WriteLine();
        }

        public static string Dump(this ReadOnlySymbolValues symbolValues)
        {
            using var tw = new StringWriter();
            Dump(symbolValues, tw);
            return tw.ToString();
        }

        public static void Dump(this ReadOnlySymbolValues symbolValues, TextWriter tw, string indent = "")
        {
            tw.WriteLine($"{indent}SymbolValues '{symbolValues.DebugName}_{symbolValues.GetHashCode()}'");
            var symbolTable = symbolValues.SymbolTable;

            if (symbolValues is ComposedReadOnlySymbolValues composed)
            {
                composed.DebugDumpChildren(tw, indent + "  ");
            }

            var values = (IGlobalSymbolNameResolver)symbolTable;
            foreach (var kv in values.GlobalSymbols)
            {
                var name = kv.Key;
                var info = kv.Value;

                if (info.Data is ISymbolSlot slot)
                {
                    var value = symbolValues.Get(slot);

                    var str = Dump(value);

                    tw.WriteLine($"{indent} {name}:{info.Type}= {str}   ({slot.Owner.DebugName()})");
                }
                else
                {
                    tw.WriteLine($"{indent} {name}:{info.Type}=????");
                }
            }
        }

        public static string Dump(this FormulaType type)
        {
            return type?.ToString();
        }

        public static string Dump(this FormulaValue value)
        {
            var settings = new FormulaValueSerializerSettings
            {
                UseCompactRepresentation = true
            };
            var sb = new StringBuilder();
            value.ToExpression(sb, settings);
            return sb.ToString();
        }

        public static string Dump(this TexlNode parseNode)
        {
            return TexlPretty.PrettyPrint(parseNode);
        }

        internal static string Dump(this IntermediateNode eval)
        {
            var str = eval?.ToString();
            return str;
        }

        public static string Dump(this CheckResult check)
        {
            using var tw = new StringWriter();
            Dump(check, tw);
            return tw.ToString();
        }

        public static void Dump(this CheckResult check, TextWriter tw, string indent = "")
        {
            tw.WriteLine($"{indent}CheckResult");
            tw.WriteLine();

            if (check.Parse != null)
            {
                tw.WriteLine(Dump(check.Parse.Root));
                tw.WriteLine();
            }

            try
            {
                if (check.Binding != null)
                {
                    tw.WriteLine($"{indent}Binding: {Dump(check.ReturnType)}");
                    tw.WriteLine();
                }
            }
            catch
            {
                tw.WriteLine($"{indent}No Binding");
            }

            if (check.Errors.Any())
            {
                tw.WriteLine($"{indent}Errors:");
                foreach (var error in check.Errors)
                {
                    tw.WriteLine($"{indent}  {error}");
                }

                tw.WriteLine();
            }
            else
            {
                try
                {
                    var run = check.GetEvaluator();
                    if (run is ParsedExpression run2)
                    {
                        tw.WriteLine($"{indent}IR:");
                        tw.WriteLine(Dump(run2._irnode));
                        tw.WriteLine();
                    }
                }
                catch
                {
                    tw.WriteLine($"{indent}No IR");
                }
            }

            // Symbols last - they can be very large 
            try
            {
                if (check.Symbols != null)
                {
                    tw.WriteLine($"{indent}Symbols:");
                    Dump(check.Symbols, tw, indent + "   ");
                }
            }
            catch
            {
                tw.WriteLine($"{indent}No Symbols");
            }
        }
    }
}
