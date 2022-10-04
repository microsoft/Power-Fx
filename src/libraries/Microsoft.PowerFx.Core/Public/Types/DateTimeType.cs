// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class DateTimeType : FormulaType
    {
        internal DateTimeType()
            : base(DType.DateTime)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "DateTime";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            var dateTimeMin = System.DateTime.MinValue.ToUniversalTime();

            sb.Append($"DateTimeValue({CharacterUtils.ToPlainText(dateTimeMin.ToString("o"))})");
        }
    }
}
