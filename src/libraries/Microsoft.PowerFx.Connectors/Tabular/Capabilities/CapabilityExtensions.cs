// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors.Tabular.Capabilities
{
    internal static class CapabilityExtensions
    {
        internal static int GetInt(this OpenApiObject obj, string propName, int defaultValue = 0)
            => obj.TryGet(propName, out OpenApiInteger i)
                ? i.Value
                : defaultValue;

        internal static bool GetBool(this OpenApiObject obj, string propName, string exceptionMessage = null)
            => obj.GetNullableBool(propName)
                ?? (string.IsNullOrEmpty(exceptionMessage)
                    ? false
                    : throw new PowerFxConnectorException(exceptionMessage));

        internal static bool? GetNullableBool(this OpenApiObject obj, string propName)
            => obj.TryGet(propName, out OpenApiBoolean b)
                ? b.Value
                : null;

        internal static string GetStr(this OpenApiObject obj, string propName)
            => obj.TryGet(propName, out OpenApiString s)
                ? s.Value
                : null;

        internal static List<string> GetList(this OpenApiObject obj, string propName)
            => obj.GetEnumerable(propName)?.ToList();

        internal static string[] GetArray(this OpenApiObject obj, string propName)
            => obj.GetEnumerable(propName)?.ToArray();

        internal static IEnumerable<string> GetEnumerable(this OpenApiObject obj, string propName)
            => obj.TryGet(propName, out OpenApiArray array)
                ? array.Select(item => item is OpenApiString str ? str.Value : null)
                : null;

        internal static OpenApiObject GetObject(this OpenApiObject obj, string propName)
            => obj.TryGet(propName, out OpenApiObject o)
                ? o
                : null;

        private static bool TryGet<T>(this OpenApiObject obj, string propName, out T result)
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
    }
}
