// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class BlobTests : PowerFxTest
    {
        [Fact]
        public void BlobTest_ConstructorNullResourceManager()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new BlobValue(null));
            Assert.Equal("Value cannot be null. (Parameter 'resourceElement')", ex.Message);
        }

        [Fact]
        public void BlobTest_InvalidCoercions()
        {
            BlobElementBase beb = new StringBlob(null);
            BlobValue blob = new BlobValue(beb);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => blob.AsDouble());
            Assert.Equal("Can't coerce to double from Blob", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => blob.AsBoolean());
            Assert.Equal("Can't coerce to boolean from Blob", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => blob.AsDecimal());
            Assert.Equal("Can't coerce to decimal from Blob", ex.Message);
        }

        [Fact]
        public void BlobTest_NullValue()
        {
            BlobElementBase beb = new StringBlob(null);
            BlobValue blob = new BlobValue(beb);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal(string.Empty, blob.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Empty(blob.GetAsByteArrayAsync(CancellationToken.None).Result);
            Assert.Same(blob.ResourceElement, beb);
        }

        [Fact]
        public void BlobTest_EmptyValue()
        {
            BlobElementBase beb = new StringBlob(string.Empty);
            BlobValue blob = new BlobValue(beb);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.ResourceElement.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal(string.Empty, blob.ResourceElement.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Empty(blob.GetAsByteArrayAsync(CancellationToken.None).Result);
            Assert.Same(blob.ResourceElement, beb);
        }

        [Fact]
        public void BlobTest_SomeValue()
        {
            BlobElementBase sb = new StringBlob("Hello World!");
            BlobValue blob = new BlobValue(sb);

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", blob.ResourceElement.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal("SGVsbG8gV29ybGQh", blob.ResourceElement.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, blob.GetAsByteArrayAsync(CancellationToken.None).Result);
            Assert.Same(blob.ResourceElement, sb);
        }

        [Fact]
        public void BlobTest_SomeBase64Value()
        {
            BlobElementBase b64b = new Base64Blob("SGVsbG8gV29ybGQh");
            BlobValue blob = new BlobValue(b64b);

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", blob.ResourceElement.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal("SGVsbG8gV29ybGQh", blob.ResourceElement.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, blob.GetAsByteArrayAsync(CancellationToken.None).Result);
            Assert.Same(blob.ResourceElement, b64b);
        }

        [Fact]
        public void BlobTest_SomeByteArrayValue()
        {
            BlobElementBase bab = new ByteArrayBlob(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 });
            BlobValue blob = new BlobValue(bab);

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", blob.ResourceElement.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal("SGVsbG8gV29ybGQh", blob.ResourceElement.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, blob.GetAsByteArrayAsync(CancellationToken.None).Result);
            Assert.Same(blob.ResourceElement, bab);
        }

        [Fact]
        public void BlobTest_InvalidBase64()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new Base64Blob("This_is_not_a_base64_string!"));
            Assert.Equal("Invalid base64 string (Parameter 'base64Str')", ex.Message);
        }

        [Fact]
        public void BlobTest_MultipleObjects()
        {            
            BlobValue[] blobs = Enumerable.Range(0, 10).Select(i =>
            {
                BlobElementBase sre = new StringBlob($"Blob {i}");
                return new BlobValue(sre);
            }).ToArray();

            int i = 0;
            foreach (BlobValue blob in blobs)
            {
                Assert.NotNull(blob);
                Assert.Equal($"Blob {i++}", blob.GetAsStringAsync(null, CancellationToken.None).Result);                
            }          
        }

        [Fact]
        public void BlobTest_LoadStream()
        {
            using Stream file = File.Open("icon.png", FileMode.Open, FileAccess.Read);
            StreamBlob sre = new StreamBlob(file);

            Assert.Equal(1899, sre.Length);
        }        
    }
}
