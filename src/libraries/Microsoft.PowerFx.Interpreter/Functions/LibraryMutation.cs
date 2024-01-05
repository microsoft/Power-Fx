// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        internal static Dictionary<TexlFunction, IAsyncTexlFunction> MutationFunctions()
        {
            return new Dictionary<TexlFunction, IAsyncTexlFunction>()
            {
                { new CollectFunction(), new CollectImplementation() },
                { new CollectScalarFunction(), new CollectImplementation() },
            };
        }

        internal class CollectImplementation : IAsyncTexlFunction
        {
            public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                FormulaValue arg0;
                var argc = args.Length;
                var returnIsTable = args.Length > 2;

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
                else if (arg0 is ErrorValue)
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
                        returnIsTable = true;

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
                                var recordValueCopy = FormulaValue.NewRecordFromFields(row.Value.Fields);
                                resultRows.Add(await tableValue.AppendAsync(recordValueCopy, cancellationToken).ConfigureAwait(false));
                            }
                        }
                    }
                    else if (arg is RecordValue recordValue)
                    {
                        var recordValueCopy = FormulaValue.NewRecordFromFields(recordValue.Fields);
                        resultRows.Add(await tableValue.AppendAsync(recordValueCopy, cancellationToken).ConfigureAwait(false));
                    }
                    else if (arg is ErrorValue)
                    {
                        return arg;
                    }
                    else if (arg is BlankValue && !tableValue.Type._type.IsSingleColumnTable)
                    {
                        continue;
                    }
                    else
                    {
                        NamedValue namedValue = new NamedValue(tableValue.Type.SingleColumnFieldName, arg);
                        var singleColumnRecord = FormulaValue.NewRecordFromFields(namedValue);

                        resultRows.Add(await tableValue.AppendAsync(singleColumnRecord, cancellationToken).ConfigureAwait(false));
                    }
                }

                if (resultRows.Count == 0)
                {
                    return FormulaValue.NewBlank(arg0.Type);
                }
                else if (returnIsTable)
                {
                    return CompileTimeTypeWrapperTableValue.AdjustType(tableValue.Type, new InMemoryTableValue(IRContext.NotInSource(arg0.Type), resultRows));
                }
                else
                {
                    return CompileTimeTypeWrapperRecordValue.AdjustType(tableValue.Type.ToRecord(), (RecordValue)resultRows.First().ToFormulaValue());
                }
            }
        }
    }
}
