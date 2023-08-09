// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    public class FormulaTypeSerializerSerttings
    {
        /// <summary>
        /// Functions which takes in a logical name of <see cref="AggregateType"/> and returns its <see cref="RecordType"/>.
        /// </summary>
        public readonly Func<string, RecordType> LogicalNameToRecordType;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaTypeSerializerSerttings"/> class.
        /// </summary>
        public FormulaTypeSerializerSerttings()
        {
            Func<string, RecordType> debugHelper = (dummy) => throw new InvalidOperationException("Lazy type converter not registered");
            this.LogicalNameToRecordType = debugHelper;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaTypeSerializerSerttings"/> class.
        /// </summary>
        /// <param name="logicalNameToRecordType">Functions which takes in a logical name of <see cref="AggregateType"/> and returns its <see cref="RecordType"/>.
        /// This is needed only for de-serialization of Dataverse or Lazy Aggregate types.</param>
        public FormulaTypeSerializerSerttings(Func<string, RecordType> logicalNameToRecordType)
            : this()
        {
            this.LogicalNameToRecordType = logicalNameToRecordType ?? this.LogicalNameToRecordType;
        }
    }
}
