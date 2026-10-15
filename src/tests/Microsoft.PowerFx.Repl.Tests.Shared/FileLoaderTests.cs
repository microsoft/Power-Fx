// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerFx.Repl.Tests
{
    // Resolving module names against the module directory.
    public sealed class FileLoaderTests : IDisposable
    {
        private readonly TempDirectoryHolder _dir = new TempDirectoryHolder();
        private readonly string _root;
        private readonly FileLoader _loader;

        public FileLoaderTests()
        {
            _root = Path.Combine(_dir.FullPath, "modules");

            WriteModule("modules/main.fx.yml");
            WriteModule("modules/sub/child.fx.yml");
            WriteModule("shared.fx.yml");
            WriteModule("modules2/extra.fx.yml");

            _loader = new FileLoader(_root);
        }

        public void Dispose()
        {
            _dir.Dispose();
            GC.SuppressFinalize(this);
        }

        private void WriteModule(string path)
        {
            _dir.WriteFile(path, "Formulas: |", "  Func(x: Number) : Number = x;");
        }

        [Theory]
        [InlineData("main.fx.yml", "main.fx.yml")]
        [InlineData("  main.fx.yml  ", "main.fx.yml")]
        [InlineData("sub/child.fx.yml", "sub/child.fx.yml")]
        [InlineData("./sub/child.fx.yml", "sub/child.fx.yml")]
        [InlineData("sub/../main.fx.yml", "main.fx.yml")]
        public async Task LoadsRelativeName(string name, string expected)
        {
            var (poco, _) = await _loader.LoadAsync(name);

            var expectedPath = Path.Combine(_root, expected.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(expectedPath, poco.Src_Filename);
            Assert.Equal("Func(x: Number) : Number = x;", poco.Formulas.Value.Trim());
        }

        [Theory]
        [InlineData("../shared.fx.yml")]
        [InlineData("sub/../../shared.fx.yml")]
        [InlineData("../modules2/extra.fx.yml")]
        public async Task RejectsNameOutsideDirectory(string name)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync(name));
            Assert.Equal($"Module '{name}' is outside the module directory.", ex.Message);
        }

        [Fact]
        public async Task LoadsFullPathInsideDirectory()
        {
            var name = Path.Combine(_root, "sub", "child.fx.yml");

            var (poco, _) = await _loader.LoadAsync(name);

            Assert.Equal(name, poco.Src_Filename);
        }

        [Fact]
        public async Task RejectsFullPathOutsideDirectory()
        {
            var name = Path.Combine(_dir.FullPath, "shared.fx.yml");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync(name));
            Assert.Contains("is outside the module directory", ex.Message);
        }

        [Fact]
        public async Task RejectsNameFromFileSystemRoot()
        {
            var name = Path.DirectorySeparatorChar + "shared.fx.yml";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.LoadAsync(name));
            Assert.Contains("is outside the module directory", ex.Message);
        }

        [Fact]
        public async Task DirectoryCaseIgnoredOnWindows()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var (poco, _) = await _loader.LoadAsync("../MODULES/main.fx.yml");

            Assert.NotNull(poco.Formulas);
        }
    }
}
