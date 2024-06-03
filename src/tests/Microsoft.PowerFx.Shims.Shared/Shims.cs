// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.PowerFx.Shims
{
    public static class Shims
    {
        public static T NewException<T>(string message, uint hResult, Exception inner = null)
            where T : Exception, new()
        {
            T ex = new T();

            typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ex, message);
            typeof(Exception).GetField("_HResult", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ex, unchecked((int)hResult));

            if (inner != null)
            {
                typeof(Exception).GetField("_innerException", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ex, inner);
            }

            return ex;
        }

        public static string PathJoin(string path1, string path2)
        {
#if NETCOREAPP3_1_OR_GREATER
            return Path.Combine(path1, path2);
#else
            return $@"{path1}\{path2}";
#endif
        }        

        public static string PathJoin(string path1, string path2, string path3)
        {
#if NETCOREAPP3_1_OR_GREATER
            return Path.Combine(path1, path2, path3);
#else
            return $@"{path1}\{path2}\{path3}".Replace(@"\\", @"\");
#endif
        }

        public static string GetFullPath(string path, string basePath)
        {
#if NETCOREAPP3_1_OR_GREATER
            // Can't define Shims on static classes
            return Path.GetFullPath(path, basePath);
#else            
            if (!Path.IsPathRooted(path))
            {
                return Path.Combine(basePath, path);
            }
            
            if (path.StartsWith(@"\", StringComparison.Ordinal))
            {
                return (Path.GetPathRoot(basePath) + path).Replace(@"\\", @"\");
            }

            return path;            
#endif
        }

        public static string FrameworkVersion()
        {
#if NET9_0
            return "net9.0";
#elif NET8_0
            return "net8.0";
#elif NET7_0
            return "net7.0";
#elif NET462
            return "net462";
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
