// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Count(source:*)
    internal sealed class CountFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool CheckTypesAndSemanticsOnly => true;

        public CountFunction()
            : base("Count", TexlStrings.AboutCount, FunctionCategories.Table | FunctionCategories.MathAndStat, DType.Number, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CountArg1 };
        }

        protected override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // The argument should be a table of one column.
            var argType = argTypes[0];
            IEnumerable<TypedName> columns;
            if (!argType.IsTable)
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedTable_Func, Name);
            }
            else if ((columns = argType.GetNames(DPath.Root)).Count() != 1)
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedTableCol_Func, Name);
            }
            else if (!DType.Number.Accepts(columns.Single().Type))
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Warning, args[0], TexlStrings.ErrInvalidSchemaNeedNumCol_Col, columns.Single().Name);
            }

            return fValid;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
