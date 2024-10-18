// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;

namespace Microsoft.PowerFx.Repl.Tests
{
    // Helper for creating temp files and cleaning up.
    internal class TempFileHolder : IDisposable
    {
        public string FullPath { get; } = Path.GetTempFileName();

        public void Dispose()
        {
            // Cleanup the file. 
            try
            {
                File.Delete(this.FullPath);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
