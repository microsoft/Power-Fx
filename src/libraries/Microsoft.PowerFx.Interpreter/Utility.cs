// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx
{
    internal static class Utility
    {
        // Helper. Given a type Foo<T>,  extract the T when genericDef is Foo<>.
        public static bool TryGetElementType(Type type, Type genericDef, out Type elementType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDef)
            {
                elementType = type.GenericTypeArguments[0];
                return true;
            }
            else
            {
                elementType = null;
                return false;
            }
        }
    }
}
