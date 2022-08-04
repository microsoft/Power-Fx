// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types.BuiltInLazyTypes
{
    // Type represents attachment type which are delay loaded fields at runtime
    internal class AttachmentType : RecordType
    {
        public DType Attachment { get; }

        public AttachmentType(DType attachmentType) 
            : base()
        {
            Attachment = attachmentType;
        }

        public override IEnumerable<string> FieldNames => Attachment.GetRootFieldNames().Select(name => name.Value);

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            if (!Attachment.TryGetType(new DName(name), out var dType))
            {
                type = Blank;
                return false;
            }

            type = Build(dType);
            return true;
        }

        public override bool Equals(object other)
        {
            if (other is not AttachmentType otherAttachmentType)
            {
                return false;
            }

            return Attachment.Equals(otherAttachmentType.Attachment);
        }

        public override int GetHashCode()
        {
            return Attachment.GetHashCode();
        }
    }
}
