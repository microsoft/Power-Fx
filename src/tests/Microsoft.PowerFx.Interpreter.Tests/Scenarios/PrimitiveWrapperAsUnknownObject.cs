// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Wrap a .net object as an UntypedObject.
    // This will lazily marshal through the object as it's accessed.
    [DebuggerDisplay("{_source}")]
    public class PrimitiveWrapperAsUnknownObject : SupportsFxValue, ISupportsArray, ISupportsProperties
    {
        public readonly object _source;

        public PrimitiveWrapperAsUnknownObject(object source)
            : base(source)
        {
            _source = source;
        }

        public static UntypedObjectValue New(object source)
        {
            return FormulaValue.New(new PrimitiveWrapperAsUnknownObject(source));
        }

        public int Length => ((Array)_source).Length;            

        public IUntypedObject this[int index]
        {
            get
            {
                var a = (Array)_source;

                // Fx infastructure already did this check,
                // so we're only invoked in success case. 
                Assert.True(index >= 0 && index <= a.Length);

                var value = a.GetValue(index);
                if (value == null)
                {
                    return null;
                }

                return new PrimitiveWrapperAsUnknownObject(value);
            }
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {            
            Type t = _source.GetType();
            PropertyInfo prop = t.GetProperty(value, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
            {
                // Fx semantics are to return blank for missing properties. 
                // No way to signal error here. 
                result = null;
                return false;
            }

            var obj = prop.GetValue(_source);

            if (obj == null)
            {
                result = null;
                return false;
            }
            else
            {
                result = new PrimitiveWrapperAsUnknownObject(obj);
            }

            return true;
        }
    }
}
