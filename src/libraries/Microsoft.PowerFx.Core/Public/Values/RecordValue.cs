// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represent a Record. Records have named fields which can be other values. 
    /// </summary>
    public abstract class RecordValue : ValidFormulaValue
    {
        public abstract IEnumerable<NamedValue> Fields { get; }

        internal RecordValue(IRContext irContext)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is RecordType);
        }

        public static RecordValue Empty()
        {
            var type = new RecordType();
            return new InMemoryRecordValue(IRContext.NotInSource(type), new List<NamedValue>());
        }

        public virtual FormulaValue GetField(string name)
        {
            return GetField(IRContext.NotInSource(FormulaType.Blank), name);
        }

        internal virtual FormulaValue GetField(IRContext irContext, string name)
        {
            // Derived class can have more optimized lookup.
            foreach (var field in Fields)
            {
                if (name == field.Name)
                {
                    return field.Value;
                }
            }

            return new BlankValue(irContext);
        }

        // Return an object, which can be used as 'dynamic' to fetch fields. 
        public override object ToObject()
        {
            var e = new ExpandoObject();
            IDictionary<string, object> dict = e;
            foreach (var field in Fields)
            {
                dict[field.Name] = field.Value?.ToObject();
            }

            return e;
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
