// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Patch(dataSource:*[], Record, Updates1, Updates2,…)
    internal class PatchImpl : PatchFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            MutationProcessUtils.ThrowIfPFxV1NotActive(runtimeServiceProvider.GetService<Features>(), "Patch");

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

            var dvalue = MutationProcessUtils.MergeRecords(args.Skip(2));

            if (!dvalue.IsValue)
            {
                return dvalue.ToFormulaValue();
            }

            // If base record is {}, then collect.
            if (baseRecord.IsEmptyRecord)
            {
                return (await tableValue.AppendAsync(dvalue.Value, cancellationToken).ConfigureAwait(false)).ToFormulaValue();
            }
            else
            {
                return (await tableValue.PatchAsync(baseRecord, dvalue.Value, cancellationToken).ConfigureAwait(false)).ToFormulaValue();
            }
        }
    }

    // !!!TODO If working with collections, this wont have any effectiveness.
    // This is due to the fact that collections have no keys.

    // Patch(DS, record_with_keys_and_updates)
    internal class PatchSingleRecordImpl : PatchSingleRecordFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            MutationProcessUtils.ThrowIfPFxV1NotActive(runtimeServiceProvider.GetService<Features>(), "Patch");

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

            return (await tableValue.PatchAsync(recordValue, cancellationToken).ConfigureAwait(false)).ToFormulaValue();
        }
    }

    // Patch(DS, table_of_rows, table_of_updates)
    internal class PatchAggregateImpl : PatchAggregateFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            MutationProcessUtils.ThrowIfPFxV1NotActive(runtimeServiceProvider.GetService<Features>(), "Patch");

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

            // !!!TODO Do we need to check if args[1] and args[2] counters are equal?
            // PA does not give any error if they are not equal.
            if (arg1Rows.Count() != arg2Rows.Count())
            {
                return CommonErrors.CustomError(IRContext.NotInSource(tableValue.Type), "Both aggregate args must have the same number of records.");
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

                // If base record is {}, then collect.
                if (baseRecord.Value.IsEmptyRecord)
                {
                    result = await tableValue.AppendAsync(updatesRecord.Value, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    result = await tableValue.PatchAsync(baseRecord.Value, updatesRecord.Value, cancellationToken).ConfigureAwait(false);
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

    // !!!TODO If working with collections, this wont have any effectiveness.
    // This is due to the fact that collections have no keys.

    // Patch(DS, table_of_rows_with_updates)
    internal class PatchAggregateSingleTableImpl : PatchAggregateSingleTableFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            MutationProcessUtils.ThrowIfPFxV1NotActive(runtimeServiceProvider.GetService<Features>(), "Patch");

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

                var result = await tableValue.PatchAsync(row.Value, cancellationToken).ConfigureAwait(false);

                if (result.IsError)
                {
                    return result.ToFormulaValue();
                }

                resultRows.Add(result);
            }

            return new InMemoryTableValue(IRContext.NotInSource(tableValue.Type), resultRows);
        }
    }

    // Patch(Record, Updates1, Updates2,…)
    internal class PatchRecordImpl : PatchRecordFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            MutationProcessUtils.ThrowIfPFxV1NotActive(runtimeServiceProvider.GetService<Features>(), "Patch");

            return MutationProcessUtils.MergeRecords(args).ToFormulaValue();
        }
    }

    internal class MutationProcessUtils
    {
        public static void ThrowIfPFxV1NotActive(Features features, string functionName)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            if (!features.PowerFxV1CompatibilityRules)
            {
                throw new InvalidOperationException($"{functionName} function can only be executed if PowerFx V1 feature is active.");
            }
        }

        /// <summary>
        /// Merges all records starting from startIndex in args into a single record. Collisions are resolved by last-one-wins.
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        public static DValue<RecordValue> MergeRecords(IEnumerable<FormulaValue> records)
        {
            var mergedFields = new Dictionary<string, FormulaValue>();

            foreach (FormulaValue fv in records)
            {
                if (fv is ErrorValue errorValue)
                {
                    return DValue<RecordValue>.Of(errorValue);
                }

                if (fv is RecordValue recordValue)
                {
                    foreach (var field in recordValue.Fields)
                    {
                        mergedFields[field.Name] = field.Value;
                    }
                }
            }

            return DValue<RecordValue>.Of(FormulaValue.NewRecordFromFields(mergedFields.Select(kvp => new NamedValue(kvp.Key, kvp.Value))));
        }
    }
}
