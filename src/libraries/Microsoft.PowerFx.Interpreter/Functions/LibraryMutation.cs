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

                // !!!! This should be moved to IR.
                else if (arg is BlankValue)
                {
                    if (tableValue.Type._type.IsSingleColumnTable && tableValue.Type.GetFieldTypes().First().Name.Value == "Value")
                    {
                        resultRows.Add(await tableValue.AppendAsync(CreateRecordFromPrimitive(tableValue, arg), cancellationToken).ConfigureAwait(false));
                    }                    
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
