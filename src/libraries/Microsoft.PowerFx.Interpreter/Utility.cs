﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

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

        /// <summary>
        /// Get a service from the <paramref name="serviceProvider"/>,
        /// Returns null if not present.
        /// </summary>
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }

        public static bool AggregateHasExpandedType(this DType self)
        {
            var ret = false;

            if (self.IsAggregate)
            {
                var record = self.ToRecord();

                ret = record.GetAllNames(DPath.Root).Any(name => name.Type.IsExpandEntity);
            }

            return ret;
        }
    }
}
