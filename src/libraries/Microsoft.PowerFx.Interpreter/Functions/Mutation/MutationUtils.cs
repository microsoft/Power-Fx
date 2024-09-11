// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class MutationUtils
    {
        /// <summary>
        /// Merges all records into a single record. Collisions are resolved by last-one-wins.
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

                if (fv is BlankValue)
                {
                    continue;
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

        public static async Task<FormulaValue> RemoveCore(FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FormulaValue arg0;

            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                arg0 = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }
            else
            {
                arg0 = args[0];
            }

            if (arg0 is BlankValue || arg0 is ErrorValue)
            {
                return arg0;
            }

            // If any of the argN (N>0) is error, return the error.
            foreach (var arg in args.Skip(1))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (arg is ErrorValue)
                {
                    return arg;
                }

                if (arg is TableValue tableValue)
                {
                    var errorRecord = tableValue.Rows.First(row => row.IsError);
                    if (errorRecord != null)
                    {
                        return errorRecord.Error;
                    }
                }
            }

            var all = false;
            var datasource = (TableValue)arg0;

            if (args.Count() >= 3 && args.Last() is OptionSetValue opv)
            {
                all = opv.Option == "All";
            }

            List<RecordValue> recordsToRemove = null;

            if (args[1] is TableValue sourceTable)
            {
                recordsToRemove = sourceTable.Rows.Select(row => row.Value).ToList();
            }
            else
            {
               recordsToRemove = args
                    .Skip(1)
                    .Where(arg => arg is RecordValue)
                    .Select(row => (RecordValue)row)
                    .ToList();
            }

            // At this point all errors have been handled.
            var response = await datasource.RemoveAsync(recordsToRemove, all, cancellationToken).ConfigureAwait(false);

            if (response.IsError)
            {
                var errors = new List<ExpressionError>();
                foreach (var error in response.Error.Errors)
                {
                    errors.Add(new ExpressionError()
                    {
                        ResourceKey = RuntimeStringResources.ErrRecordNotFound,
                        Kind = ErrorKind.NotFound
                    });
                }

                return new ErrorValue(IRContext.NotInSource(irContext), errors);
            }

            return irContext == FormulaType.Void ? FormulaValue.NewVoid() : FormulaValue.NewBlank();
        }
    }
}
