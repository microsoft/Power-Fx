// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Record to describe File information for host. 
    /// C# property names match Power Fx field names.
    /// </summary>
    public class PowerFxFileInfo
    {
        internal static readonly DType _fileInfoType = DType.CreateRecord(new TypedName[]
        {
             new TypedName(DType.String, new DName("Name")),
             new TypedName(DType.Decimal, new DName("Size")),
             new TypedName(DType.String, new DName("MIMEType"))
        });

        /// <summary>
        /// Full name of the file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Size of the file in Bytes.
        /// </summary>
        public decimal Size { get; set; }

        /// <summary>
        /// MIMEType of the file.
        /// </summary>
        public string MIMEType { get; set; }
    }
}
