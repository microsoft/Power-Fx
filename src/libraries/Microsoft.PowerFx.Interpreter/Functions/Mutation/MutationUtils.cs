// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class MutationUtils
    {
        public static void CheckForReadOnlyFields(DType dataSourceType, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            if (dataSourceType.AssociatedDataSources.Any())
            {
                var tableDsInfo = dataSourceType.AssociatedDataSources.Single();

                if (tableDsInfo is IExternalCdsDataSource cdsTableInfo)
                {
                    for (int i = 0; i < argTypes.Length; i++)
                    {
                        if (!cdsTableInfo.IsArgTypeValidForMutation(argTypes[i], out var invalidFieldNames))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrRecordContainsInvalidFields_Arg, string.Join(", ", invalidFieldNames));
                        }
                    }
                }
            }
        }

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
    }
}
