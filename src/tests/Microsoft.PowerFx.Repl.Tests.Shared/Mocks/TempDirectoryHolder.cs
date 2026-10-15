// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;

namespace Microsoft.PowerFx.Repl.Tests
{
    // Helper for creating a temp directory of files and cleaning up.
    internal class TempDirectoryHolder : IDisposable
    {
        public string FullPath { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public TempDirectoryHolder()
        {
            Directory.CreateDirectory(FullPath);
        }

        // Write a file relative to this directory and return its full path.
        public string WriteFile(string relativePath, params string[] lines)
        {
            var path = Path.GetFullPath(Path.Combine(FullPath, relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
            return path;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(FullPath, recursive: true);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
