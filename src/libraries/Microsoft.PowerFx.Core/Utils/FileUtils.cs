using System;
using System.IO;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// Return a full path for a temporary file, and delete it at Dispose.
    /// </summary>
    internal class TempFile : IDisposable
    {
        public string FullPath { get; private set; }

        public TempFile()
        {
            FullPath = Path.GetTempFileName() + ".msapp";
        }

        public void Dispose()
        {
            if (FullPath != null && File.Exists(FullPath))
            {
                try
                {
                    File.Delete(FullPath);
                }
                catch { /* Failing to delete here isn't fatal */ }
            }
            FullPath = null;
        }
    }

    /// <summary>
    /// Return a unique temporary directory and delete it at Dispose
    /// </summary>
    internal class TempDir : IDisposable
    {
        public string Dir { get; private set; }

        public TempDir()
        {
            Dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        public void Dispose()
        {
            if (Dir != null && Directory.Exists(Dir))
            {
                try
                {
                    Directory.Delete(Dir, recursive: true);
                }
                catch { /* Failing to delete here isn't fatal */ }
            }
            Dir = null;
        }
    }

}
