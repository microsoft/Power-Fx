// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class InjectServiceProviderFunction : TexlFunction, IAsyncConnectorTexlFunction
    {
        public InjectServiceProviderFunction()
            : base(DPath.Root, nameof(InjectServiceProviderFunction), nameof(InjectServiceProviderFunction), (loc) => nameof(InjectServiceProviderFunction), FunctionCategories.REST, DType.EmptyTable, 0, 1, 1)
        {
        }

        public override bool IsSelfContained => true;

        public override bool IsAsync => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TableArg1 };
        }

        public Task<FormulaValue> InvokeAsync(FormulaValue[] args, IServiceProvider context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Creation of the table with its context
            CdpTableValueWithServiceProvider connectorTableWithServiceProvider = new CdpTableValueWithServiceProvider(args[0] as CdpTableValue, context);
            return Task.FromResult<FormulaValue>(connectorTableWithServiceProvider);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            bool b = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            returnType = argTypes[0];
            return b;
        }
    }
}
