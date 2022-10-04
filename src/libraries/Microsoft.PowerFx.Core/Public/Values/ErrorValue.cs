// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// A Runtime error. 
    /// </summary>
    public class ErrorValue : FormulaValue
    {
        private readonly List<ExpressionError> _errors = new List<ExpressionError>();

        internal ErrorValue(IRContext irContext)
            : base(irContext)
        {
        }

        internal ErrorValue(IRContext irContext, ExpressionError error)
            : this(irContext)
        {
            Add(error);
        }

        internal ErrorValue(IRContext irContext, List<ExpressionError> errors)
            : this(irContext)
        {
            _errors = errors;
        }

        public IReadOnlyList<ExpressionError> Errors => _errors;

        public void Add(ExpressionError error)
        {
            _errors.Add(error);
        }

        public override object ToObject()
        {
            // This is strongly typed already. 
            return this;
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal static ErrorValue Combine(IRContext irContext, params ErrorValue[] values)
        {
            return Combine(irContext, (IEnumerable<ErrorValue>)values);
        }

        internal static ErrorValue Combine(IRContext irContext, IEnumerable<ErrorValue> values)
        {
            return new ErrorValue(irContext, new List<ExpressionError>(CombineErrors(values)));
        }

        private static IEnumerable<ExpressionError> CombineErrors(IEnumerable<ErrorValue> values)
        {
            foreach (var v in values)
            {
                foreach (var error in v._errors)
                {
                    yield return error;
                }
            }
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {            
            var flag = true;

            sb.Append("Error(Table(");

            foreach (var errorValue in _errors)
            {
                if (!flag)
                {
                    sb.Append(",");
                }

                flag = false;

                sb.Append("{Kind:ErrorKind.");
                sb.Append(errorValue.Kind.ToString());

                if (errorValue.Message != null)
                {
                    sb.Append(", Message:");
                    sb.Append(CharacterUtils.ToPlainText(errorValue.Message));
                }

                sb.Append("}");
            }

            sb.Append("))");
        }
    }
}
