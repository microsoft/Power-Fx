// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static FormulaValue First(IRContext irContext, TableValue[] args)
        {
            return args[0].Rows.FirstOrDefault()?.ToFormulaValue() ?? new BlankValue(irContext);
        }

        public static FormulaValue Last(IRContext irContext, TableValue[] args)
        {
            return args[0].Rows.LastOrDefault()?.ToFormulaValue() ?? new BlankValue(irContext);
        }

        public static FormulaValue FirstN(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (NumberValue)args[1];

            var rows = arg0.Rows.Take((int)arg1.Value);
            return new InMemoryTableValue(irContext, rows);
        }

        public static FormulaValue LastN(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (NumberValue)args[1];

            // $$$ How to do on a streaming service?            
            var allRows = arg0.Rows.ToArray();
            var len = allRows.Length;
            var take = (int)arg1.Value; // $$$ rounding?

            var rows = allRows.Skip(len - take).Take(take);

            return new InMemoryTableValue(irContext, rows);
        }

        // Create new table
        public static FormulaValue AddColumns(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var sourceArg = (TableValue)args[0];

            var newColumns = NamedLambda.Parse(args);

            var tableType = (TableType)irContext.ResultType;
            var recordIRContext = new IRContext(irContext.SourceContext, tableType.ToRecord());
            var rows = LazyAddColumns(runner, symbolContext, sourceArg.Rows, recordIRContext, newColumns);

            return new InMemoryTableValue(irContext, rows);
        }

        private static IEnumerable<DValue<RecordValue>> LazyAddColumns(EvalVisitor runner, SymbolContext context, IEnumerable<DValue<RecordValue>> sources, IRContext recordIRContext, NamedLambda[] newColumns)
        {
            foreach (var row in sources)
            {
                if (row.IsValue)
                {
                    // $$$ this is super inefficient... maybe a custom derived RecordValue? 
                    var fields = new List<NamedValue>(row.Value.Fields);

                    var childContext = context.WithScopeValues(row.Value);

                    foreach (var column in newColumns)
                    {
                        var value = column.Lambda.Eval(runner, childContext);
                        fields.Add(new NamedValue(column.Name, value));
                    }

                    yield return DValue<RecordValue>.Of(new InMemoryRecordValue(recordIRContext, fields.ToArray()));
                }
                else
                {
                    yield return row;
                }
            }
        }

        // CountRows
        public static FormulaValue CountRows(IRContext irContext, TableValue[] args)
        {
            var arg0 = args[0];

            // Streaming 
            var count = arg0.Rows.Count();
            return new NumberValue(irContext, count);
        }

        public static FormulaValue CountIf(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            // Streaming 
            var sources = (TableValue)args[0];
            var filter = (LambdaFormulaValue)args[1];

            var count = 0;

            var errors = new List<ErrorValue>();

            foreach (var row in sources.Rows)
            {
                if (row.IsValue)
                {
                    var childContext = symbolContext.WithScopeValues(row.Value);
                    var result = filter.Eval(runner, childContext);

                    if (result is ErrorValue error)
                    {
                        errors.Add(error);
                        continue;
                    }

                    var include = ((BooleanValue)result).Value;

                    if (include)
                    {
                        count++;
                    }
                }

                if (row.IsError)
                {
                    errors.Add(row.Error);
                }
            }

            if (errors.Count != 0)
            {
                return ErrorValue.Combine(irContext, errors);
            }

            return new NumberValue(irContext, count);
        }

        // Filter ([1,2,3,4,5], Value > 5)
        public static FormulaValue FilterTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            // Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            if (args.Length > 2)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = "Filter() only supports one predicate",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Validation
                });
            }

            var rows = LazyFilter(runner, symbolContext, arg0.Rows, arg1);

            return new InMemoryTableValue(irContext, rows);
        }

        public static FormulaValue SortTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];
            var arg2 = (StringValue)args[2];

            var pairs = arg0.Rows.Select(row =>
            {
                if (row.IsValue)
                {
                    var childContext = symbolContext.WithScopeValues(row.Value);
                    return new KeyValuePair<DValue<RecordValue>, FormulaValue>(row, arg1.Eval(runner, childContext));
                }

                return new KeyValuePair<DValue<RecordValue>, FormulaValue>(row, row.ToFormulaValue());
            }).ToList();

            var errors = new List<ErrorValue>(pairs.Select(pair => pair.Value).OfType<ErrorValue>());

            var allNumbers = pairs.All(pair => IsValueTypeErrorOrBlank<NumberValue>(pair.Value));
            var allStrings = pairs.All(pair => IsValueTypeErrorOrBlank<StringValue>(pair.Value));
            var allBooleans = pairs.All(pair => IsValueTypeErrorOrBlank<BooleanValue>(pair.Value));

            if (!(allNumbers || allStrings || allBooleans))
            {
                errors.Add(CommonErrors.RuntimeTypeMismatch(irContext));
                return ErrorValue.Combine(irContext, errors);
            }

            if (errors.Count != 0)
            {
                return ErrorValue.Combine(irContext, errors);
            }

            var compareToResultModifier = 1;
            if (arg2.Value.ToLower() == "descending")
            {
                compareToResultModifier = -1;
            }

            if (allNumbers)
            {
                return SortValueType<NumberValue, double>(pairs, irContext, compareToResultModifier);
            }
            else if (allStrings)
            {
                return SortValueType<StringValue, string>(pairs, irContext, compareToResultModifier);
            }
            else
            {
                return SortValueType<BooleanValue, bool>(pairs, irContext, compareToResultModifier);
            }
        }

        private static bool IsValueTypeErrorOrBlank<T>(FormulaValue val)
            where T : FormulaValue
        {
            return val is T || val is BlankValue || val is ErrorValue;
        }

        private static FormulaValue SortValueType<TPFxPrimitive, TDotNetPrimitive>(List<KeyValuePair<DValue<RecordValue>, FormulaValue>> pairs, IRContext irContext, int compareToResultModifier)
            where TPFxPrimitive : PrimitiveValue<TDotNetPrimitive>
            where TDotNetPrimitive : IComparable<TDotNetPrimitive>
        {
            pairs.Sort((a, b) =>
            {
                if (a.Value is BlankValue)
                {
                    return b.Value is BlankValue ? 0 : 1;
                }
                else if (b.Value is BlankValue)
                {
                    return -1;
                }

                var n1 = a.Value as TPFxPrimitive;
                var n2 = b.Value as TPFxPrimitive;
                return n1.Value.CompareTo(n2.Value) * compareToResultModifier;
            });

            return new InMemoryTableValue(irContext, pairs.Select(pair => pair.Key));
        }

        private static IEnumerable<DValue<RecordValue>> LazyFilter(
            EvalVisitor runner,
            SymbolContext context,
            IEnumerable<DValue<RecordValue>> sources,
            LambdaFormulaValue filter)
        {
            foreach (var row in sources)
            {
                if (row.IsValue)
                {
                    var childContext = context.WithScopeValues(row.Value);

                    // Filter evals to a boolean 
                    var result = filter.Eval(runner, childContext);
                    var include = false;
                    if (result is BooleanValue booleanValue)
                    {
                        include = booleanValue.Value;
                    }
                    else if (result is ErrorValue errorValue)
                    {
                        yield return DValue<RecordValue>.Of(errorValue);
                    }

                    if (include)
                    {
                        yield return row;
                    }
                }
            }
        }

        // AddColumns accepts pairs of args. 
        private class NamedLambda
        {
            public string Name;

            public LambdaFormulaValue Lambda;

            public static NamedLambda[] Parse(FormulaValue[] args)
            {
                var l = new List<NamedLambda>();

                for (var i = 1; i < args.Length; i += 2)
                {
                    var columnName = ((StringValue)args[1]).Value;
                    var arg1 = (LambdaFormulaValue)args[2];
                    l.Add(new NamedLambda
                    {
                        Name = columnName,
                        Lambda = arg1
                    });
                }

                return l.ToArray();
            }
        }
    }
}
