// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class BlobValue : PrimitiveValue<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobValue"/> class.
        /// The blob value can be used for opaque document types such as files.
        /// </summary>
        internal BlobValue(IRContext irContext, string id) 
            : base(irContext, id)
        {
            Contract.Assert(irContext.ResultType == FormulaType.Blob);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            throw new InvalidOperationException();
        }

        public override object ToObject()
        {
            throw new InvalidOperationException();
        }

        public override string ToString()
        {
            return "o";
        }

        public override void Visit(IValueVisitor visitor)
        {
            throw new NotSupportedException();
        }
    }
}
