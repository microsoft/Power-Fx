// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
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

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            sb.Append($"GUID({CharacterUtils.ToPlainText(System.Guid.Empty.ToString("N"))})");
        }
    }
}
