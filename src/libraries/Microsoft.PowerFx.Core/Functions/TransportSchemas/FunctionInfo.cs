// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Functions.TransportSchemas
{
    [TransportType(TransportKind.ByValue)]
    internal sealed class FunctionInfo
    {
        public string Label;
        public string Detail;
        public string Documentation;
        public FunctionSignature[] Signatures;

        
        internal FunctionInfo Merge(FunctionInfo with)
        {
            return new FunctionInfo()
            {
                Label = this.Label,
                Detail = this.Detail,
                Documentation = this.Documentation,
                Signatures = this.Signatures.Concat(with.Signatures).ToArray()
            };
        }
    }
}