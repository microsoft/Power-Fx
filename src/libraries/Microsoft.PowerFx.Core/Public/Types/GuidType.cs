// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class GuidType : FormulaType
    {
        internal GuidType()
            : base(DType.Guid)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Guid";
        }

        internal override string DefaultExpressionValue()
        {
            return $"GUID({CharacterUtils.ToPlainText(System.Guid.Empty.ToString("N"))})";
        }
    }
}
