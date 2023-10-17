// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    public class FormulaTypeSerializerSettings
    {
        /// <summary>
        /// Functions which takes in a logical name of <see cref="AggregateType"/> and returns its <see cref="RecordType"/>.
        /// </summary>
        public Func<string, RecordType> LogicalNameToRecordType { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaTypeSerializerSettings"/> class.
        /// </summary>
        public FormulaTypeSerializerSettings()
        {
            Func<string, RecordType> debugHelper = (dummy) => throw new InvalidOperationException("Lazy type converter not registered");
            this.LogicalNameToRecordType = debugHelper;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaTypeSerializerSettings"/> class.
        /// </summary>
        /// <param name="logicalNameToRecordType">Functions which takes in a logical name of <see cref="AggregateType"/> and returns its <see cref="RecordType"/>.
        /// This is needed only for de-serialization of Dataverse or Lazy Aggregate types.</param>
        public FormulaTypeSerializerSettings(Func<string, RecordType> logicalNameToRecordType)
            : this()
        {
            this.LogicalNameToRecordType = logicalNameToRecordType ?? this.LogicalNameToRecordType;
        }
    }
}
