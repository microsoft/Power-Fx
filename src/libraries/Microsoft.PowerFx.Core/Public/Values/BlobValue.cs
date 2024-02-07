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
        internal BlobContent Content { get; }

        internal BlobValue(BlobContent resourceElement)
             : base(IRContext.NotInSource(FormulaType.Blob))
        {
            Content = resourceElement ?? throw new ArgumentNullException(nameof(resourceElement));
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
            return await Content.GetAsByteArrayAsync(token).ConfigureAwait(false);
        }

        public virtual async Task<string> GetAsStringAsync(Encoding encoding, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await Content.GetAsStringAsync(encoding, token).ConfigureAwait(false);
        }

        public virtual async Task<string> GetAsBase64Async(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await Content.GetAsBase64Async(token).ConfigureAwait(false);
        }
    }
}
