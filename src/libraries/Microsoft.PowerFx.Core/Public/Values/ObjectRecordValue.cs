// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Reflection;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using static Microsoft.PowerFx.Core.ObjectMarshallerProvider;

namespace Microsoft.PowerFx.Core
{    
    /// <summary>
    /// Represent a Record that's backed by a DotNet object. 
    /// Field access is lazy.
    /// </summary>
    public class ObjectRecordValue : RecordValue
    {
        /// <summary>
        /// The host object that this was originally created around. 
        /// </summary>
        public object Source { get; private set; }

        private readonly ObjectMarshaler _mapping;

        internal ObjectRecordValue(IRContext irContext, object source, ObjectMarshaler marshaler) 
            : base(irContext)
        {
            Source = source;
            _mapping = marshaler;
        }

        public override IEnumerable<NamedValue> Fields => _mapping.GetFields(Source);

        internal override FormulaValue GetField(IRContext irContext, string name)
        {
            var value = _mapping.TryGetField(Source, name);
            if (value != null)
            {
                return value;
            }
            else
            {
                // Missing field. Should be compiler time error...
                return new ErrorValue(irContext);
            }
        }
    }
}
