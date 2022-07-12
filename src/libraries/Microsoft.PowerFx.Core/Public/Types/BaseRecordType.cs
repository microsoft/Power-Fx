// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represents a Record type within PowerFx. If this is subclassed, it's quite likely that 
    /// <see cref="RecordValue"/> should be as well. 
    /// If the type is known in advance and easy to construct, use <see cref="RecordType"/> instead of
    /// deriving from this. 
    /// </summary>
    public abstract class BaseRecordType : AggregateType
    {
        // The internal constructor allows us to wrap known DTypes, while the public constructor
        // will create a DType that wraps derived TryGetField/Fields/Identity calls
        internal BaseRecordType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsRecord);
        }

        public BaseRecordType(ITypeIdentity identity, IEnumerable<string> fieldNames) 
            : base(identity, fieldNames, true)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }
        
        public BaseTableType ToTable()
        {
            return new TableType(DType.ToTable());
        }
    }
}
