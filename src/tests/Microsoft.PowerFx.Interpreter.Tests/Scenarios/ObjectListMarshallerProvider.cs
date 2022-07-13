// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.PowerFx.Interpreter.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    /// <summary>
    /// Marshal a heterogenous List of Object using <see cref="UntypedObjectType"/>.
    /// </summary>
    internal class ObjectListMarshallerProvider : ITypeMarshallerProvider
    {
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, out ITypeMarshaller marshaller)
        {
            if (type == typeof(List<object>))
            {
                marshaller = new ObjectListMarshaller();
                return true;
            }

            marshaller = null;
            return false;
        }

        private class ObjectListMarshaller : ITypeMarshaller
        {
            public FormulaType Type => _type.ToTable();

            private readonly RecordType _type;

            public ObjectListMarshaller()
            {
                _type = new KnownRecordType().Add(TableValue.ValueName, FormulaType.UntypedObject);
            }

            public FormulaValue Marshal(object value)
            {
                var list = (IEnumerable<object>)value;

                var fxRecords = new List<RecordValue>();
                foreach (var item in list)
                {
                    var objFx = PrimitiveWrapperAsUnknownObject.New(item);

                    var record = FormulaValue.NewRecordFromFields(new NamedValue(TableValue.ValueName, objFx));
                    fxRecords.Add(record);
                }

                return FormulaValue.NewTable(_type, fxRecords.ToArray());
            }
        }
    }
}
