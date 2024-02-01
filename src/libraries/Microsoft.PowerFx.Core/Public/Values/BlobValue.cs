// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class BlobValue : FileValue
    {        
        public BlobValue(ResourceManager resourceManager, string str, bool isBase64Encoded, FileType fileType = FileType.Any)
            : base(resourceManager, str, isBase64Encoded, fileType)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Blob);            
        }

        public override string ResourceIdentifier => "blobmanager";

        internal BlobValue(IRContext irContext) 
            : base(null, null, false, FileType.Unknown)
        {            
        }      
      
        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
