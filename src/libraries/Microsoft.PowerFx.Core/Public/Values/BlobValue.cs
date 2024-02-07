// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class BlobValue : ValidFormulaValue
    {
        internal BlobContent ResourceElement { get; }

        internal BlobValue(BlobContent resourceElement)
             : base(IRContext.NotInSource(FormulaType.Blob))
        {
            ResourceElement = resourceElement ?? throw new ArgumentNullException(nameof(resourceElement));
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings) => throw new NotImplementedException();

        public override object ToObject() => this;

        public async Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await ResourceElement.GetAsByteArrayAsync(token).ConfigureAwait(false);
        }

        public virtual async Task<string> GetAsStringAsync(Encoding encoding, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await ResourceElement.GetAsStringAsync(encoding, token).ConfigureAwait(false);
        }

        public virtual async Task<string> GetAsBase64Async(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await ResourceElement.GetAsBase64Async(token).ConfigureAwait(false);
        }
    }
}
