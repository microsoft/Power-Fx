// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static async ValueTask<FormulaValue> LookUp(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            // Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];
            var arg2 = (LambdaFormulaValue)(args.Length > 2 ? args[2] : null);

            var rows = await LazyFilterAsync(runner, symbolContext, arg0.Rows, arg1);
            var row = rows.FirstOrDefault();

            if (row != null)
            {
                if (args.Length == 2)
                {
                    return row.ToFormulaValue() ?? new BlankValue(irContext);
                }
                else
                {
                    var childContext = symbolContext.WithScopeValues(row.Value);
                    var value = await arg2.EvalAsync(runner, childContext);

                    if (value is NumberValue number)
                    {
                        value = FiniteChecker(irContext, 0, number);
                    }

                    return value;
                }
            }

            return new BlankValue(irContext);
        }

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
        public static async ValueTask<FormulaValue> AddColumns(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var sourceArg = (TableValue)args[0];

            var newColumns = NamedLambda.Parse(args);

            var tableType = (TableType)irContext.ResultType;
            var recordIRContext = new IRContext(irContext.SourceContext, tableType.ToRecord());
            var rows = await LazyAddColumnsAsync(runner, symbolContext, sourceArg.Rows, recordIRContext, newColumns);

            return new InMemoryTableValue(irContext, rows);
        }

        private static async Task<IEnumerable<DValue<RecordValue>>> LazyAddColumnsAsync(EvalVisitor runner, SymbolContext context, IEnumerable<DValue<RecordValue>> sources, IRContext recordIRContext, NamedLambda[] newColumns)
        {
            var list = new List<DValue<RecordValue>>();

            foreach (var row in sources)
            {
                if (row.IsValue)
                {
                    // $$$ this is super inefficient... maybe a custom derived RecordValue? 
                    var fields = new List<NamedValue>(row.Value.Fields);

                    var childContext = context.WithScopeValues(row.Value);

                    foreach (var column in newColumns)
                    {
                        var value = await column.Lambda.EvalAsync(runner, childContext);
                        fields.Add(new NamedValue(column.Name, value));
                    }

                    list.Add(DValue<RecordValue>.Of(new InMemoryRecordValue(recordIRContext, fields.ToArray())));
                }
                else
                {
                    list.Add(row);
                }
            }

            return list;
        }

        // CountRows
        public static FormulaValue CountRows(IRContext irContext, TableValue[] args)
        {
            var arg0 = args[0];

            // Streaming 
            var count = arg0.Count();
            return new NumberValue(irContext, count);
        }

        public static async ValueTask<FormulaValue> CountIf(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
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
                    var result = await filter.EvalAsync(runner, childContext);

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
        public static async ValueTask<FormulaValue> FilterTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
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

            var rows = await LazyFilterAsync(runner, symbolContext, arg0.Rows, arg1);
            
            return new InMemoryTableValue(irContext, rows);
        }

        public static FormulaValue IndexTable(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (NumberValue)args[1];
            var rowIndex = (int)arg1.Value;

            return arg0.Index(rowIndex).ToFormulaValue();
        }

        public static async ValueTask<FormulaValue> SortTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];
            var arg2 = (StringValue)args[2];

            var pairs = arg0.Rows.Select(row =>
            {
                if (row.IsValue)
                {
                    var childContext = symbolContext.WithScopeValues(row.Value);
                    return new KeyValuePair<DValue<RecordValue>, FormulaValue>(row, arg1.EvalAsync(runner, childContext).Result);
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
        
        public static async ValueTask<FormulaValue> SortByColumns(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var table = (TableValue)args[0];
            var tableType = (TableType)table.Type;

            var records = table.Rows.ToList();
            var errors = new List<ErrorValue>();
            for (var argIndex = 1; argIndex < args.Length; argIndex += 2)
            {
                var fieldName = ((StringValue)args[argIndex]).Value;
                if (tableType.MaybeGetFieldType(fieldName) is not NumberType and not StringType
                    and not BooleanType and not DateTimeType and not DateType)
                {
                    // column not found error
                    errors.Add(CommonErrors.RuntimeTypeMismatch(irContext));
                    break;
                }
            }

            if (errors.Count != 0)
            {
                return ErrorValue.Combine(irContext, errors);
            }

            foreach (var record in records)
            {
                if (record.IsError)
                {
                    errors.Add((ErrorValue)record.ToFormulaValue());
                    continue;
                }

                for (var argIndex = 1; argIndex < args.Length; argIndex += 2)
                {
                    var fieldName = ((StringValue)args[argIndex]).Value;
                    var fieldValue = record.Value.GetField(fieldName);
                    if (fieldValue is ErrorValue fieldErrorValue)
                    {
                        errors.Add(fieldErrorValue);
                    }
                }
            }

            if (errors.Count != 0)
            {
                return ErrorValue.Combine(irContext, errors);
            }

            records.Sort((record1, record2) =>
            {
                for (var argIndex = 1; argIndex < args.Length; argIndex += 2)
                {
                    var fieldName = ((StringValue)args[argIndex]).Value;
                    var fieldType = tableType.GetFieldType(fieldName);
                    var a = record1.Value.GetField(fieldName);
                    var b = record2.Value.GetField(fieldName);
                    if (a is BlankValue && b is not BlankValue)
                    {
                        return 1;
                    }
                    else if (b is BlankValue)
                    {
                        return -1;
                    }

                    var compareToResultModifier = 1;
                    if (argIndex + 1 < args.Length && ((StringValue)args[argIndex + 1]).Value.ToLower() == "descending")
                    {
                        compareToResultModifier = -1;
                    }

                    var compareResult = 0;
                    switch (fieldType)
                    {
                        case NumberType:
                            compareResult = CompareColumns<NumberValue, double>(a, b);
                            break;
                        case StringType:
                            compareResult = CompareColumns<StringValue, string>(a, b);
                            break;
                        case BooleanType:
                            compareResult = CompareColumns<BooleanValue, bool>(a, b);
                            break;
                        case DateType:
                            compareResult = CompareColumns<DateValue, DateTime>(a, b);
                            break;
                        case DateTimeType:
                            compareResult = CompareColumns<DateTimeValue, DateTime>(a, b);
                            break;
                    }

                    if (compareResult != 0)
                    {
                        return compareResult * compareToResultModifier;
                    }
                }

                return 0;
            });

            return new InMemoryTableValue(irContext, records);
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

        private static int CompareColumns<TPFxPrimitive, TDotNetPrimitive>(FormulaValue a, FormulaValue b)
            where TPFxPrimitive : PrimitiveValue<TDotNetPrimitive>
            where TDotNetPrimitive : IComparable<TDotNetPrimitive>
        {
            var n1 = (TPFxPrimitive)a;
            var n2 = (TPFxPrimitive)b;
            return n1.Value.CompareTo(n2.Value);
        }

        private static async Task<DValue<RecordValue>> LazyFilterRowAsync(
           EvalVisitor runner,
           SymbolContext context,
           DValue<RecordValue> row,
           LambdaFormulaValue filter)
        {
            SymbolContext childContext;
            
            // Issue #263 Filter should be able to handle empty rows
            if (row.IsValue)
            {
                childContext = context.WithScopeValues(row.Value);
            }
            else if (row.IsBlank)
            {
                childContext = context.WithScopeValues(RecordValue.Empty());
            }
            else
            {
                return null;
            }

            // Filter evals to a boolean 
            var result = await filter.EvalAsync(runner, childContext);
            var include = false;
            if (result is BooleanValue booleanValue)
            {
                include = booleanValue.Value;
            }
            else if (result is ErrorValue errorValue)
            {
                return DValue<RecordValue>.Of(errorValue);
            }

            if (include)
            {
                return row;
            }

            return null;
        }

        private static async Task<DValue<RecordValue>[]> LazyFilterAsync(
            EvalVisitor runner,
            SymbolContext context,
            IEnumerable<DValue<RecordValue>> sources,
            LambdaFormulaValue filter, 
            int topN = int.MaxValue)
        {
            var tasks = new List<Task<DValue<RecordValue>>>();
            
            // Filter needs to allow running in parallel. 
            foreach (var row in sources)
            {
                runner.CheckCancel();

                var task = LazyFilterRowAsync(runner, context, row, filter);
                tasks.Add(task);
            }

            // WhenAll will allow running tasks in parallel. 
            var results = await Task.WhenAll(tasks);

            // Remove all nulls. 
            var final = results.Where(x => x != null);

            return final.ToArray();
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
                    var columnName = ((StringValue)args[i]).Value;
                    var arg1 = (LambdaFormulaValue)args[i + 1];
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
