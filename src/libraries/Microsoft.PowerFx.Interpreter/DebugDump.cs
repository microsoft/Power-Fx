// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Intellisense;
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
        public static string Dump(ReadOnlySymbolTable symbolTable)
        {
            using var tw = new StringWriter();
            Dump(symbolTable, tw);
            return tw.ToString();
        }

        public static void Dump(ReadOnlySymbolTable symbolTable, TextWriter tw, string indent = "")
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
                    foreach (var func in symbolTable.Functions)
                    {
                        tw.WriteLine($"{indent} {func.Name}");
                    }
                }
            }             

            tw.WriteLine();
        }

        public static string Dump(ReadOnlySymbolValues symbolValues)
        {
            using var tw = new StringWriter();
            Dump(symbolValues, tw);
            return tw.ToString();
        }

        public static void Dump(ReadOnlySymbolValues symbolValues, TextWriter tw, string indent = "")
        {
            tw.WriteLine($"{indent}SymbolValues '{symbolValues.DebugName}_{symbolValues.GetHashCode()}'");
            var symbolTable = symbolValues.SymbolTable;

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

        public static string Dump(FormulaType type)
        {
            return type?.ToString();
        }

        public static string Dump(FormulaValue value)
        {
            var settings = new FormulaValueSerializerSettings
            {
                 UseCompactRepresentation = true
            };
            var sb = new StringBuilder();
            value.ToExpression(sb, settings);
            return sb.ToString();
        }        

        public static string Dump(TexlNode parseNode)
        {
            return TexlPretty.PrettyPrint(parseNode);
        }

        internal static string Dump(IntermediateNode eval)
        {
            var str = eval?.ToString();
            return str;
        }

        public static string Dump(CheckResult check)
        {
            using var tw = new StringWriter();
            Dump(check, tw);
            return tw.ToString();
        }

        public static void Dump(CheckResult check, TextWriter tw, string indent = "")
        {
            tw.WriteLine($"{indent}CheckResult");
            tw.WriteLine();

            if (check.Parse != null)
            {
                tw.WriteLine(Dump(check.Parse.Root));
                tw.WriteLine();
            }

            if (check.Binding != null)
            {
                tw.WriteLine($"{indent}Binding: {Dump(check.ReturnType)}");
                tw.WriteLine();
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
                var run = check.GetEvaluator();
                if (run is ParsedExpression run2)
                {
                    tw.WriteLine($"{indent}IR:");
                    tw.WriteLine(Dump(run2._irnode));
                    tw.WriteLine();
                }
            }

            // Symbols last - they can be very large 
            if (check.AllSymbols != null)
            {
                tw.WriteLine($"{indent}Symbols:");
                Dump(check.AllSymbols, tw, indent + "   ");
            }
        }
    }
}
