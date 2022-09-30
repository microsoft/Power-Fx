// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class DateType : FormulaType
    {
        internal DateType()
            : base(DType.Date)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Date";
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            var dateTimeMin = System.DateTime.MinValue.ToUniversalTime();

            sb.Append($"DateValue({CharacterUtils.ToPlainText(dateTimeMin.Date.ToString("o"))})");
        }
    }
}
