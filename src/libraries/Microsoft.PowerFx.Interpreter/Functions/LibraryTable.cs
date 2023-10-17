// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static async ValueTask<FormulaValue> LookUp(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            // Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];
            var arg2 = (LambdaFormulaValue)(args.Length > 2 ? args[2] : null);

            var rows = await LazyFilterAsync(runner, context, arg0.Rows, arg1).ConfigureAwait(false);
            var row = rows.FirstOrDefault();

            if (row != null)
            {
                if (args.Length == 2)
                {
                    return row.ToFormulaValue() ?? new BlankValue(irContext);
                }
                else
                {
                    var childContext = context.SymbolContext.WithScopeValues(row.Value);
                    return await arg2.EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);
                }
            }

            return new BlankValue(irContext);
        }

        public static FormulaValue First(IRContext irContext, TableValue[] args)
        {
            var arg0 = args[0];

#pragma warning disable CS0618 // Type or member is obsolete
            if (arg0 is QueryableTableValue tableQueryable)
            {
#pragma warning restore CS0618 // Type or member is obsolete
                try
                {
                    return tableQueryable.FirstN(1).Rows.FirstOrDefault()?.ToFormulaValue() ?? new BlankValue(irContext);
                }
                catch (NotDelegableException)
                {
                }
            }

            return arg0.Rows.FirstOrDefault()?.ToFormulaValue() ?? new BlankValue(irContext);
        }

        public static FormulaValue Last(IRContext irContext, TableValue[] args)
        {
            return args[0].Rows.LastOrDefault()?.ToFormulaValue() ?? new BlankValue(irContext);
        }

        public static FormulaValue FirstN(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new BlankValue(irContext);
            }

            if (args[0] is not TableValue)
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var arg0 = (TableValue)args[0];
            var arg1 = (NumberValue)args[1];

#pragma warning disable CS0618 // Type or member is obsolete
            if (arg0 is QueryableTableValue queryableTable)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                try
                {
                    return queryableTable.FirstN((int)arg1.Value);
                }
                catch (NotDelegableException)
                {
                }
            }

            var rows = arg0.Rows.Take((int)arg1.Value);
            return new InMemoryTableValue(irContext, rows);
        }

        public static FormulaValue LastN(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new BlankValue(irContext);
            }

            if (args[0] is not TableValue)
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var arg0 = (TableValue)args[0];
            var arg1 = (NumberValue)args[1];

            // $$$ How to do on a streaming service?            
            var allRows = arg0.Rows.ToArray();
            var len = allRows.Length;
            var take = (int)arg1.Value; // $$$ rounding?

            var rows = allRows.Skip(len - take).Take(take);

            return new InMemoryTableValue(irContext, rows);
        }

        // Create new table / record
        public static async ValueTask<FormulaValue> AddColumns(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var newColumns = NamedLambda.Parse(args);
            if (args[0] is TableValue sourceArg)
            {
                var tableType = (TableType)irContext.ResultType;
                var recordIRContext = new IRContext(irContext.SourceContext, tableType.ToRecord());
                var rows = await LazyAddColumnsAsync(runner, context, sourceArg.Rows, recordIRContext, newColumns).ConfigureAwait(false);

                return new InMemoryTableValue(irContext, rows);
            }
            else
            {
                var recordResult = await AddColumnsRecordAsync(runner, context, DValue<RecordValue>.Of(args[0] as RecordValue), irContext, newColumns).ConfigureAwait(false);
                return recordResult.Value;
            }
        }

        public static async ValueTask<FormulaValue> DropColumns(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var columnsToRemove = args.Skip(1).ToArray();
            if (args[0] is TableValue sourceArg)
            {
                var tableType = (TableType)irContext.ResultType;
                var recordIRContext = new IRContext(irContext.SourceContext, tableType.ToRecord());
                var rows = await LazyDropColumnsAsync(runner, context, sourceArg.Rows, recordIRContext, columnsToRemove).ConfigureAwait(false);

                return new InMemoryTableValue(irContext, rows);
            }

            var recordArg = (RecordValue)args[0];
            var recordResult = DropColumnsRecord(runner, context, DValue<RecordValue>.Of(recordArg), irContext, columnsToRemove).Value;
            return recordResult;
        }

        public static async ValueTask<FormulaValue> ShowColumns(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var columnsToRemain = new HashSet<string>(args.OfType<StringValue>().Select(sv => sv.Value));
            IEnumerable<NamedFormulaType> fields;
            if (args[0] is TableValue tableValue)
            {
                fields = tableValue.Type.GetFieldTypes();
            }
            else
            {
                fields = ((RecordValue)args[0]).Type.GetFieldTypes();
            }

            var columnsToRemove = fields.Where(x => !columnsToRemain.Contains(x.Name)).Select(x => FormulaValue.New(x.Name));

            List<FormulaValue> newArgs = new List<FormulaValue>()
            {
                args[0]
            };

            foreach (var fv in columnsToRemove)
            {
                newArgs.Add(fv);
            }

            // Leveraging DropColumns function to remove all unnecessary columns.
            return await DropColumns(runner, context, irContext, newArgs.ToArray()).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<DValue<RecordValue>>> LazyAddColumnsAsync(EvalVisitor runner, EvalVisitorContext context, IEnumerable<DValue<RecordValue>> sources, IRContext recordIRContext, NamedLambda[] newColumns)
        {
            var list = new List<DValue<RecordValue>>();

            foreach (var row in sources)
            {
                runner.CheckCancel();
                var newRow = await AddColumnsRecordAsync(runner, context, row, recordIRContext, newColumns).ConfigureAwait(false);
                list.Add(newRow);
            }

            return list;
        }

        private static async Task<DValue<RecordValue>> AddColumnsRecordAsync(EvalVisitor runner, EvalVisitorContext context, DValue<RecordValue> source, IRContext recordIRContext, NamedLambda[] newColumns)
        {
            if (!source.IsValue)
            {
                return source;
            }

            var fields = new List<NamedValue>(source.Value.Fields);

            var childContext = context.SymbolContext.WithScopeValues(source.Value);

            foreach (var column in newColumns)
            {
                runner.CheckCancel();

                var value = await column.Lambda.EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);
                fields.Add(new NamedValue(column.Name, value));
            }

            return DValue<RecordValue>.Of(new InMemoryRecordValue(recordIRContext, fields.ToArray()));
        }

        private static async Task<IEnumerable<DValue<RecordValue>>> LazyDropColumnsAsync(EvalVisitor runner, EvalVisitorContext context, IEnumerable<DValue<RecordValue>> sources, IRContext recordIRContext, FormulaValue[] columnsToRemove)
        {
            var list = new List<DValue<RecordValue>>();
            var columnNames = new HashSet<string>(columnsToRemove.OfType<StringValue>().Select(sv => sv.Value));

            foreach (var row in sources)
            {
                runner.CheckCancel();

                list.Add(DropColumnsRecord(runner, context, row, recordIRContext, columnsToRemove));
            }

            return list;
        }

        private static DValue<RecordValue> DropColumnsRecord(EvalVisitor runner, EvalVisitorContext context, DValue<RecordValue> source, IRContext recordIRContext, FormulaValue[] columnsToRemove)
        {
            var columnNames = new HashSet<string>(columnsToRemove.OfType<StringValue>().Select(sv => sv.Value));
            if (source.IsValue)
            {
                return DValue<RecordValue>.Of(
                    new InMemoryRecordValue(
                        recordIRContext,
                        source.Value.Fields.Where(f => !columnNames.Contains(f.Name)).ToArray()));
            }
            else
            {
                return source;
            }
        }

        // CountRows
        public static FormulaValue CountRows(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = args[0];

            if (arg0 is BlankValue)
            {
                return NumberOrDecimalValue(irContext, 0);
            }

            if (arg0 is TableValue table)
            {
                int count = 0;

                foreach (DValue<RecordValue> row in table.Rows)
                {
                    if (row.IsError)
                    {
                        return row.Error;
                    }

                    count++;
                }                
                
                return NumberOrDecimalValue(irContext, count);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        // Count
        public static FormulaValue Count(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = args[0];
            var count = 0;

            if (arg0 is BlankValue)
            {
                return NumberOrDecimalValue(irContext, 0);
            }

            if (arg0 is TableValue table)
            {
                foreach (var row in table.Rows)
                {
                    if (row.IsBlank)
                    {
                        continue;
                    }
                    else if (row.IsError)
                    {
                        return row.Error;
                    }

                    var field = row.Value.Fields.First().Value;

                    if (field is ErrorValue error)
                    {
                        return error;
                    }

                    if (field is NumberValue || field is DecimalValue)
                    {
                        count++;
                    }
                }

                return NumberOrDecimalValue(irContext, count);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        // CountA
        public static FormulaValue CountA(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = args[0];
            if (arg0 is BlankValue)
            {
                return NumberOrDecimalValue(irContext, 0);
            }

            if (arg0 is TableValue table)
            {
                var count = 0;

                foreach (var row in table.Rows)
                {
                    if (row.IsBlank)
                    {
                        continue;
                    }
                    else if (row.IsError)
                    {
                        return row.Error;
                    }

                    var field = row.Value.Fields.First().Value;

                    if (field is ErrorValue error)
                    {
                        return error;
                    }

                    if (field is not BlankValue)
                    {
                        count++;
                    }
                }

                return NumberOrDecimalValue(irContext, count);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static async ValueTask<FormulaValue> CountIf(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return NumberOrDecimalValue(irContext, 0);
            }

            // Streaming 
            var sources = (TableValue)args[0];
            var filters = args.Skip(1).Cast<LambdaFormulaValue>().ToArray();

            var count = 0;

            foreach (var row in sources.Rows)
            {
                runner.CheckCancel();

                SymbolContext childContext = context.SymbolContext.WithScopeValues(row.ToFormulaValue());

                var include = true;
                for (var i = 0; i < filters.Length; i++)
                {
                    runner.CheckCancel();

                    var result = await filters[i].EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);

                    if (result is ErrorValue error)
                    {
                        return error;
                    }
                    else if (result is BlankValue)
                    {
                        include = false;
                        break;
                    }

                    include = ((BooleanValue)result).Value;

                    if (!include)
                    {
                        break;
                    }
                }

                if (include)
                {
                    count++;
                }
            }

            return NumberOrDecimalValue(irContext, count);
        }

        // Filter ([1,2,3,4,5], Value > 5)
        public static async ValueTask<FormulaValue> FilterTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
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

#pragma warning disable CS0618 // Type or member is obsolete
            if (arg0 is QueryableTableValue tableQueryable)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                try
                {
                    return tableQueryable.Filter(arg1, runner, context);
                }
                catch (NotDelegableException)
                {
                }
            }

            var rows = await LazyFilterAsync(runner, context, arg0.Rows, arg1).ConfigureAwait(false);

            return new InMemoryTableValue(irContext, rows);
        }

        public static FormulaValue IndexTable(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (NumberValue)args[1];
            var rowIndex = (int)arg1.Value;

            return arg0.Index(rowIndex).ToFormulaValue();
        }

        public static FormulaValue Shuffle(IServiceProvider services, IRContext irContext, FormulaValue[] args)
        {
            var table = (TableValue)args[0];
            var records = table.Rows;

            var random = services.GetService<IRandomService>(_defaultRandService);

            var shuffledRecords = records.OrderBy(a => random.SafeNextDouble()).ToList();
            return new InMemoryTableValue(irContext, shuffledRecords);
        }

        private static async Task<(DValue<RecordValue> row, FormulaValue lambdaValue)> ApplyLambda(EvalVisitor runner, EvalVisitorContext context, DValue<RecordValue> row, LambdaFormulaValue lambda)
        {
            if (!row.IsValue)
            {
                return (row, row.ToFormulaValue());
            }

            var childContext = context.SymbolContext.WithScopeValues(row.Value);
            var lambdaValue = await lambda.EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);

            return (row, lambdaValue);
        }

        public static async ValueTask<FormulaValue> DistinctTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var values = arg0.Rows.Select(row => ApplyLambda(runner, context, row, arg1));
            
            var pairs = new List<(DValue<RecordValue> row, FormulaValue distinctValue)>();

            foreach (var pair in values)
            {
                runner.CheckCancel();
                pairs.Add(await pair.ConfigureAwait(false));
            }

            return DistinctValueType(pairs, irContext);
        }

        public static async ValueTask<FormulaValue> SortTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];
            var arg2 = (StringValue)args[2];

            var isDescending = arg2.Value.Equals("descending", StringComparison.OrdinalIgnoreCase);
#pragma warning disable CS0618 // Type or member is obsolete
            if (arg0 is QueryableTableValue queryableTable)
            {
#pragma warning restore CS0618 // Type or member is obsolete
                try
                {
                    return queryableTable.Sort(arg1, isDescending, runner, context);
                }
                catch (NotDelegableException)
                {
                }
            }

            var pairs = new List<(DValue<RecordValue> row, FormulaValue distinctValue)>();

            foreach (var pair in arg0.Rows.Select(row => ApplyLambda(runner, context, row, arg1)))
            {
                runner.CheckCancel();

                pairs.Add(await pair.ConfigureAwait(false));
            }

            bool allNumbers = true, allDecimals = true, allStrings = true, allBooleans = true, allDatetimes = true, allDates = true, allOptionSets = true;

            foreach (var (row, sortValue) in pairs)
            {
                runner.CheckCancel();

                allNumbers &= IsValueTypeErrorOrBlank<NumberValue>(sortValue);
                allDecimals &= IsValueTypeErrorOrBlank<DecimalValue>(sortValue);
                allStrings &= IsValueTypeErrorOrBlank<StringValue>(sortValue);
                allBooleans &= IsValueTypeErrorOrBlank<BooleanValue>(sortValue);
                allDatetimes &= IsValueTypeErrorOrBlank<DateTimeValue>(sortValue);
                allDates &= IsValueTypeErrorOrBlank<DateValue>(sortValue);
                allOptionSets &= IsValueTypeErrorOrBlank<OptionSetValue>(sortValue);

                if (sortValue is ErrorValue errorValue)
                {
                    return errorValue;
                }
            }

            if (!(allNumbers || allDecimals || allStrings || allBooleans || allDatetimes || allDates || allOptionSets))
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var compareToResultModifier = 1;
            if (isDescending)
            {
                compareToResultModifier = -1;
            }

            if (allNumbers)
            {
                return SortValueType<NumberValue, double>(pairs, irContext, compareToResultModifier);
            }
            else if (allDecimals)
            {
                return SortValueType<DecimalValue, decimal>(pairs, irContext, compareToResultModifier);
            }
            else if (allStrings)
            {
                return SortValueType<StringValue, string>(pairs, irContext, compareToResultModifier);
            }
            else if (allBooleans)
            {
                return SortValueType<BooleanValue, bool>(pairs, irContext, compareToResultModifier);
            }
            else if (allDatetimes)
            {
                return SortValueType<DateTimeValue, DateTime>(pairs, irContext, compareToResultModifier);
            }
            else if (allDates)
            {
                return SortValueType<DateValue, DateTime>(pairs, irContext, compareToResultModifier);
            }
            else if (allOptionSets)
            {
                return SortOptionSet(pairs, irContext, compareToResultModifier);
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static async ValueTask<FormulaValue> AsType(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (RecordValue)args[0];
            var arg1 = (TableValue)args[1];

            try
            {
                var result = arg1.CastRecord(arg0, runner.CancellationToken);
                return result.ToFormulaValue();
            }
            catch (CustomFunctionErrorException e)
            {
                return new ErrorValue(irContext, e.ExpressionError);
            }
        }

        private static bool IsValueTypeErrorOrBlank<T>(FormulaValue val)
            where T : FormulaValue
        {
            return val is T || val is BlankValue || val is ErrorValue;
        }

        private static FormulaValue DistinctValueType(List<(DValue<RecordValue> row, FormulaValue distinctValue)> pairs, IRContext irContext)
        {
            var lookup = new HashSet<object>();
            var result = new List<DValue<RecordValue>>();
            var name = ((TableType)irContext.ResultType).SingleColumnFieldName;

            foreach (var (row, distinctValue) in pairs)
            {
                if (distinctValue is ErrorValue errorValue)
                {
                    return errorValue;
                }

                if (!distinctValue.Type._type.IsPrimitive)
                {
                    return CommonErrors.OnlyPrimitiveValuesAllowed(irContext);
                }

                var key = distinctValue.ToObject();

                if (!lookup.Contains(key))
                {
                    var insert = FormulaValue.NewRecordFromFields(new NamedValue(name, distinctValue));
                    lookup.Add(key);
                    result.Add(DValue<RecordValue>.Of(insert));
                }
            }

            return new InMemoryTableValue(irContext, result);
        }

        private static FormulaValue SortValueType<TPFxPrimitive, TDotNetPrimitive>(List<(DValue<RecordValue> row, FormulaValue sortValue)> pairs, IRContext irContext, int compareToResultModifier)
            where TPFxPrimitive : PrimitiveValue<TDotNetPrimitive>
            where TDotNetPrimitive : IComparable<TDotNetPrimitive>
        {
            pairs.Sort((a, b) =>
            {
                if (a.sortValue is BlankValue)
                {
                    return b.sortValue is BlankValue ? 0 : 1;
                }
                else if (b.sortValue is BlankValue)
                {
                    return -1;
                }

                var n1 = a.sortValue as TPFxPrimitive;
                var n2 = b.sortValue as TPFxPrimitive;
                return n1.Value.CompareTo(n2.Value) * compareToResultModifier;
            });

            return new InMemoryTableValue(irContext, pairs.Select(pair => pair.row));
        }

        private static FormulaValue SortOptionSet(List<(DValue<RecordValue> row, FormulaValue sortValue)> pairs, IRContext irContext, int compareToResultModifier)
        {
            pairs.Sort((a, b) =>
            {
                if (a.sortValue is BlankValue)
                {
                    return b.sortValue is BlankValue ? 0 : 1;
                }
                else if (b.sortValue is BlankValue)
                {
                    return -1;
                }

                var n1 = a.sortValue as OptionSetValue;
                var n2 = b.sortValue as OptionSetValue;

                return string.Compare(n1.Option, n2.Option, StringComparison.Ordinal) * compareToResultModifier;
            });

            return new InMemoryTableValue(irContext, pairs.Select(pair => pair.row));
        }        

        private static FormulaValue Refresh(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is IRefreshable r)
            {
                r.Refresh();
                return FormulaValue.New(true);
            }

            return CommonErrors.CustomError(irContext, "Only managed connections can be refreshed.");
        }

        private static async Task<DValue<RecordValue>> LazyFilterRowAsync(
           EvalVisitor runner,
           EvalVisitorContext context,
           DValue<RecordValue> row,
           LambdaFormulaValue filter)
        {
            SymbolContext childContext = context.SymbolContext.WithScopeValues(row.ToFormulaValue());

            // Filter evals to a boolean 
            var result = await filter.EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);
            var include = false;
            if (result is BooleanValue booleanValue)
            {
                include = booleanValue.Value;
            }
            else if (result is OptionSetValue optionSetValue)
            {
                var boolValue = optionSetValue.ExecutionValue as bool?;
                include = boolValue ?? false;
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
            EvalVisitorContext context,
            IEnumerable<DValue<RecordValue>> sources,
            LambdaFormulaValue filter,
            int topN = int.MaxValue)
        {
            var results = new List<DValue<RecordValue>>();

            // Filter needs to allow running in parallel. 
            foreach (var row in sources)
            {
                runner.CheckCancel();

                var task = LazyFilterRowAsync(runner, context, row, filter);
                
                results.Add(await task.ConfigureAwait(false));
            }
                        
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
