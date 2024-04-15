// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Patch(dataSource:*[], Record, Updates1, Updates2,…)
    internal class PatchImpl : PatchFunction, IAsyncTexlFunction3
    {
        public async Task<FormulaValue> InvokeAsync(FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = args[0];

            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }

            if (arg0 is not TableValue tableValue)
            {
                return arg0;
            }

            if (args[1] is not RecordValue baseRecord)
            {
                return args[1];
            }

            var dvalue = MutationUtils.MergeRecords(args.Skip(2));

            if (!dvalue.IsValue)
            {
                return dvalue.ToFormulaValue();
            }

            var result = await tableValue.PatchAsync(baseRecord, dvalue.Value, cancellationToken).ConfigureAwait(false);

            // If the base record is not found, then append update record.
            if (result.IsError && result.Error is ErrorValue errorvalue && errorvalue.Errors.Any(err => err.Kind == ErrorKind.NotFound))
            {
                return (await tableValue.AppendAsync(dvalue.Value, cancellationToken).ConfigureAwait(false)).ToFormulaValue();
            }

            return result.ToFormulaValue();
        }
    }

    // If arg1 is pure PFx record, it will return a runtime not supported error.
    // Patch(DS, record_with_keys_and_updates)
    internal class PatchSingleRecordImpl : PatchSingleRecordFunction, IAsyncTexlFunction3
    {
        public async Task<FormulaValue> InvokeAsync(FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = args[0];

            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }

            if (arg0 is not TableValue tableValue)
            {
                return arg0;
            }

            if (args[1] is not RecordValue recordValue)
            {
                return args[1];
            }

            DValue<RecordValue> result = null;

            result = await tableValue.PatchAsync(recordValue, cancellationToken).ConfigureAwait(false);

            // If the base record is not found, then append update record.
            if (result.IsError && result.Error is ErrorValue errorvalue && errorvalue.Errors.Any(err => err.Kind == ErrorKind.NotFound))
            {
                return (await tableValue.AppendAsync(recordValue, cancellationToken).ConfigureAwait(false)).ToFormulaValue();
            }

            return result.ToFormulaValue();
        }
    }

    // Patch(DS, table_of_rows, table_of_updates)
    internal class PatchAggregateImpl : PatchAggregateFunction, IAsyncTexlFunction3
    {
        public async Task<FormulaValue> InvokeAsync(FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = args[0];

            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }

            if (arg0 is not TableValue tableValue)
            {
                return arg0;
            }

            var arg1Rows = ((TableValue)args[1]).Rows;
            var arg2Rows = ((TableValue)args[2]).Rows;

            if (arg1Rows.Count() != arg2Rows.Count())
            {
                return CommonErrors.GenericInvalidArgument(IRContext.NotInSource(tableValue.Type), "Both aggregate args must have the same number of records.");
            }

            List<DValue<RecordValue>> resultRows = new List<DValue<RecordValue>>();

            for (int i = 0; i < arg1Rows.Count(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var baseRecord = arg1Rows.ElementAt(i);
                var updatesRecord = arg2Rows.ElementAt(i);

                if (baseRecord.IsError)
                {
                    return baseRecord.ToFormulaValue();
                }

                if (baseRecord.IsBlank)
                {
                    continue;
                }

                if (updatesRecord.IsError)
                {
                    return updatesRecord.ToFormulaValue();
                }

                if (updatesRecord.IsBlank)
                {
                    continue;
                }

                DValue<RecordValue> result = null;

                result = await tableValue.PatchAsync(baseRecord.Value, updatesRecord.Value, cancellationToken).ConfigureAwait(false);

                // If the base record is not found, then append update record.
                if (result.IsError && result.Error is ErrorValue errorvalue && errorvalue.Errors.Any(err => err.Kind == ErrorKind.NotFound))
                {
                    result = await tableValue.AppendAsync(updatesRecord.Value, cancellationToken).ConfigureAwait(false);
                }

                if (result.IsError)
                {
                    return result.ToFormulaValue();
                }

                resultRows.Add(result);
            }

            return new InMemoryTableValue(IRContext.NotInSource(tableValue.Type), resultRows);
        }
    }

    // If arg1 is pure PFx record, it will return a runtime not supported error.
    // Patch(DS, table_of_rows_with_updates)
    internal class PatchAggregateSingleTableImpl : PatchAggregateSingleTableFunction, IAsyncTexlFunction3
    {
        public async Task<FormulaValue> InvokeAsync(FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = args[0];

            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }

            if (arg0 is not TableValue tableValue)
            {
                return arg0;
            }

            if (args[1] is not TableValue tableUpdates)
            {
                return args[1];
            }

            List<DValue<RecordValue>> resultRows = new List<DValue<RecordValue>>();

            foreach (var row in tableUpdates.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (row.IsError)
                {
                    return row.ToFormulaValue();
                }

                if (row.IsBlank)
                {
                    continue;
                }

                DValue<RecordValue> result = null;

                result = await tableValue.PatchAsync(row.Value, cancellationToken).ConfigureAwait(false);

                // If the base record is not found, then append update record.
                if (result.IsError && result.Error is ErrorValue errorvalue && errorvalue.Errors.Any(err => err.Kind == ErrorKind.NotFound))
                {
                    result = await tableValue.AppendAsync(row.Value, cancellationToken).ConfigureAwait(false);
                }

                if (result.IsError)
                {
                    return result.ToFormulaValue();
                }

                resultRows.Add(result);
            }

            return new InMemoryTableValue(IRContext.NotInSource(tableValue.Type), resultRows);
        }
    }

    internal class CollectImpl : CollectFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            return await new CollectProcess().Process(runtimeServiceProvider, irContext, args, cancellationToken).ConfigureAwait(false);
        }
    }

    internal class CollectScalarImpl : CollectScalarFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            return await new CollectProcess().Process(runtimeServiceProvider, irContext, args, cancellationToken).ConfigureAwait(false);
        }
    }

    internal class CollectProcess
    {
        internal async Task<FormulaValue> Process(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            FormulaValue arg0;
            var argc = args.Length;
            var features = runtimeServiceProvider.GetService<Features>();

            if (!features.PowerFxV1CompatibilityRules)
            {
                throw new InvalidOperationException("Collect funtion can only be executed if PowerFx V1 feature is active.");
            }

            // Need to check if the Lazy first argument has been evaluated since it may have already been
            // evaluated in the ClearCollect case.
            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }
            else
            {
                arg0 = args[0];
            }

            if (arg0 is BlankValue)
            {
                return arg0;
            }

            if (arg0 is ErrorValue)
            {
                return arg0;
            }

            if (arg0 is not TableValue)
            {
                return CommonErrors.RuntimeTypeMismatch(IRContext.NotInSource(arg0.Type));
            }

            var tableValue = arg0 as TableValue;

            List<DValue<RecordValue>> resultRows = new List<DValue<RecordValue>>();

            for (int i = 1; i < argc; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var arg = args[i];

                if (arg is TableValue argTableValue)
                {
                    foreach (DValue<RecordValue> row in argTableValue.Rows)
                    {
                        if (row.IsBlank)
                        {
                            continue;
                        }
                        else if (row.IsError)
                        {
                            return row.Error;
                        }
                        else
                        {
                            var recordValueCopy = (RecordValue)row.ToFormulaValue().MaybeShallowCopy();
                            resultRows.Add(await tableValue.AppendAsync(recordValueCopy, cancellationToken).ConfigureAwait(false));
                        }
                    }
                }
                else if (arg is RecordValue)
                {
                    var recordValueCopy = CompileTimeTypeWrapperRecordValue.AdjustType(tableValue.Type.ToRecord(), (RecordValue)arg.MaybeShallowCopy());
                    resultRows.Add(await tableValue.AppendAsync(recordValueCopy, cancellationToken).ConfigureAwait(false));
                }
                else if (arg is ErrorValue)
                {
                    return arg;
                }
            }

            if (resultRows.Count == 0)
            {
                return FormulaValue.NewBlank(arg0.Type);
            }

            if (irContext._type.IsTable)
            {
                return CompileTimeTypeWrapperTableValue.AdjustType(tableValue.Type, new InMemoryTableValue(IRContext.NotInSource(arg0.Type), resultRows));
            }
            else
            {
                return CompileTimeTypeWrapperRecordValue.AdjustType(tableValue.Type.ToRecord(), (RecordValue)resultRows.First().ToFormulaValue());
            }
        }

        private RecordValue CreateRecordFromPrimitive(TableValue tableValue, FormulaValue arg)
        {
            return FormulaValue.NewRecordFromFields(tableValue.Type.ToRecord(), new NamedValue("Value", arg));
        }
    }
}
