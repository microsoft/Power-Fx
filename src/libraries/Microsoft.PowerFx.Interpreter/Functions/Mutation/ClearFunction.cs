// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    internal class ClearFunction : BuiltinFunction, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public ClearFunction()
            : base("Clear", AboutClear, FunctionCategories.Behavior, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { ClearCollectionArg };
        }

        public override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            DType dataSourceType = argTypes[0];

            // Need a collection for the 1st arg
            DType collectionType = argTypes[0];

            if (!collectionType.IsTable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name);
                fValid = false;
            }

            // Clear returns the new (cleared) collection, so the return schema is the same as the collection schema.
            returnType = collectionType;

            return fValid;
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            if (args[0] is ErrorValue errorValue)
            {
                return errorValue;
            }

            if (args[0] is BlankValue)
            {
                return FormulaValue.NewBlank(FormulaType.Boolean);
            }

            var datasource = (TableValue)args[0];
            var ret = await datasource.ClearAsync(cancellationToken);

            return ret.ToFormulaValue();
        }
    }
}
