// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

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
                        if (!cdsTableInfo.IsArgTypeValidForMutation(argTypes[i], out var invalidFieldName))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrRecordContainsInvalidFields_Arg, string.Join(", ", invalidFieldName));
                        }
                    }
                }
            }
        }
    }
}
