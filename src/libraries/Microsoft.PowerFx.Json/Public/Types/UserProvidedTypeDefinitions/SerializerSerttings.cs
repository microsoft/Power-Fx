// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    public class SerializerSerttings
    {
        internal readonly Func<string, RecordType> LogicalNameToRecordType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerSerttings"/> class.
        /// </summary>
        /// <param name="logicalNameToRecordType">This is needed only for de-serialization of Dataverse or Lazy Aggregate types.</param>
        public SerializerSerttings(Func<string, RecordType> logicalNameToRecordType)
        {
            Func<string, RecordType> debugHelper = (dummy) => throw new InvalidOperationException("Lazy type converter not registered");
            this.LogicalNameToRecordType = logicalNameToRecordType ?? debugHelper;
        }
    }
}
