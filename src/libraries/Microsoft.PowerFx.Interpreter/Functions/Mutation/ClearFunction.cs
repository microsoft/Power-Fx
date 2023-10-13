﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    internal class ClearFunction : BuiltinFunction
    {
        public override bool IsSelfContained => false;

        public override bool MutatesArg0 => true;

        // Unlike the other mutation functions, there is no need to Lazy evaluate the first argument since there is only one arg.

        public ClearFunction()
            : base("Clear", AboutClear, FunctionCategories.Behavior, DType.Boolean, 0, 1, 1, DType.EmptyTable)
        {            
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { ClearCollectionArg };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.Kind is DKind.Boolean);

            // Need a collection for the 1st arg
            DType collectionType = argTypes[0];

            if (!collectionType.IsTable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name);
                fValid = false;
            }

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);
        }
    }

    internal class ClearFunctionImpl : IFunctionImplementation           
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (args[0] is ErrorValue errorValue)
            {
                return errorValue;
            }

            if (args[0] is BlankValue)
            {
                return FormulaValue.NewBlank(FormulaType.Boolean);
            }

            var datasource = (TableValue)args[0];
            var ret = await datasource.ClearAsync(cancellationToken).ConfigureAwait(false);

            return ret.ToFormulaValue();
        }

        public async Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FormulaValue[] args = serviceProvider.GetService<FunctionExecutionContext>().Arguments;
            return await InvokeAsync(args, cancellationToken).ConfigureAwait(false);
        }
    }
}
