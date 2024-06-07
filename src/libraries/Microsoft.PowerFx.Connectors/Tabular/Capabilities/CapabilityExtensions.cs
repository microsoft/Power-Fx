// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors.Tabular.Capabilities
{
    internal static class CapabilityExtensions
    {
        internal static int GetInt(this IDictionary<string, IOpenApiAny> obj, string propName, int defaultValue = 0)
            => obj.TryGetValue(propName, out OpenApiInteger i)
                ? i.Value
                : defaultValue;

        internal static bool GetBool(this IDictionary<string, IOpenApiAny> obj, string propName, string exceptionMessage = null)
            => obj.GetNullableBool(propName)
                ?? (string.IsNullOrEmpty(exceptionMessage)
                    ? false
                    : throw new PowerFxConnectorException(exceptionMessage));

        internal static bool? GetNullableBool(this IDictionary<string, IOpenApiAny> obj, string propName)
            => obj.TryGetValue(propName, out OpenApiBoolean b)
                ? b.Value
                : null;

        internal static string GetStr(this IDictionary<string, IOpenApiAny> obj, string propName)
            => obj.TryGetValue(propName, out OpenApiString s)
                ? s.Value
                : null;

        internal static List<string> GetList(this IDictionary<string, IOpenApiAny> obj, string propName)
            => obj.GetEnumerable(propName)?.ToList();

        internal static string[] GetArray(this IDictionary<string, IOpenApiAny> obj, string propName)
            => obj.GetEnumerable(propName)?.ToArray();

        internal static IEnumerable<string> GetEnumerable(this IDictionary<string, IOpenApiAny> obj, string propName)
            => obj.TryGetArray(propName, out IList<IOpenApiAny> array)
                ? array.Select(item => item is OpenApiString str ? str.Value : null)
                : null;

        internal static IDictionary<string, IOpenApiAny> GetObject(this IDictionary<string, IOpenApiAny> obj, string propName)
            => obj.TryGetObject(propName, out IDictionary<string, IOpenApiAny> o)
                ? o
                : null;

        private static bool TryGetValue<T>(this IDictionary<string, IOpenApiAny> obj, string propName, out T result)
            where T : class, IOpenApiAny
        {
            if (obj.TryGetValue(propName, out IOpenApiAny prop) && prop is T propValue)
            {
                result = propValue;
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryGetArray(this IDictionary<string, IOpenApiAny> obj, string propName, out IList<IOpenApiAny> result)            
        {            
            if (obj.TryGetValue(propName, out IOpenApiAny prop) && prop is IList<IOpenApiAny> propValue)
            {
                result = propValue;
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryGetObject(this IDictionary<string, IOpenApiAny> obj, string propName, out IDictionary<string, IOpenApiAny> result)
        {
            if (obj.TryGetValue(propName, out IOpenApiAny prop) && prop is IDictionary<string, IOpenApiAny> propValue)
            {
                result = propValue;
                return true;
            }

            result = null;
            return false;
        }
    }
}
