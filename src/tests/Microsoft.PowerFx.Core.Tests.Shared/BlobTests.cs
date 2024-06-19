// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
#if !NET462
            Assert.Equal("Value cannot be null. (Parameter 'content')", ex.Message);
#else
#pragma warning disable SA1116 // Split parameters should start on line after open parenthesis
            Assert.Equal(@"Value cannot be null.
Parameter name: content", ex.Message);
#pragma warning restore SA1116 // Split parameters should start on line after open parenthesis
#endif
        }

        [Fact]
        public void BlobTest_InvalidCoercions()
        {
            BlobContent beb = new StringBlob(null);
            BlobValue blob = new BlobValue(beb);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => blob.AsDouble());
            Assert.Equal("Can't coerce to double from Blob", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => blob.AsBoolean());
            Assert.Equal("Can't coerce to boolean from Blob", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => blob.AsDecimal());
            Assert.Equal("Can't coerce to decimal from Blob", ex.Message);
        }

        [Fact]
        public async Task BlobTest_NullValue()
        {
            BlobContent beb = new StringBlob(null);
            BlobValue blob = new BlobValue(beb);

            Assert.NotNull(blob);            
            Assert.Equal(string.Empty, await blob.GetAsStringAsync(null, CancellationToken.None));
            Assert.Equal(string.Empty, await blob.GetAsBase64Async(CancellationToken.None));
            Assert.Empty(await blob.GetAsByteArrayAsync(CancellationToken.None));
            Assert.Same(blob.Content, beb);
        }

        [Fact]
        public async Task BlobTest_EmptyValue()
        {
            BlobContent beb = new StringBlob(string.Empty);
            BlobValue blob = new BlobValue(beb);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, await blob.Content.GetAsStringAsync(null, CancellationToken.None));
            Assert.Equal(string.Empty, await blob.Content.GetAsBase64Async(CancellationToken.None));
            Assert.Empty(await blob.GetAsByteArrayAsync(CancellationToken.None));
            Assert.Same(blob.Content, beb);
        }

        [Fact]
        public async Task BlobTest_SomeValue()
        {            
            BlobValue blob = FormulaValue.NewBlob("Hello World!", false);

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", await blob.Content.GetAsStringAsync(null, CancellationToken.None));
            Assert.Equal("SGVsbG8gV29ybGQh", await blob.Content.GetAsBase64Async(CancellationToken.None));
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, await blob.GetAsByteArrayAsync(CancellationToken.None));            
        }

        [Fact]
        public async Task BlobTest_SomeUTF32Value()
        {
            BlobValue blob = FormulaValue.NewBlob("Hello World!", false, Encoding.UTF32);

            Assert.NotNull(blob);            
            Assert.Equal("SAAAAGUAAABsAAAAbAAAAG8AAAAgAAAAVwAAAG8AAAByAAAAbAAAAGQAAAAhAAAA", await blob.Content.GetAsBase64Async(CancellationToken.None));
            Assert.Equal(new byte[] { 72, 0, 0, 0, 101, 0, 0, 0, 108, 0, 0, 0, 108, 0, 0, 0, 111, 0, 0, 0, 32, 0, 0, 0, 87, 0, 0, 0, 111, 0, 0, 0, 114, 0, 0, 0, 108, 0, 0, 0, 100, 0, 0, 0, 33, 0, 0, 0 }, await blob.GetAsByteArrayAsync(CancellationToken.None));
        }

        [Fact]
        public async Task BlobTest_SomeBase64Value()
        {            
            BlobValue blob = FormulaValue.NewBlob("SGVsbG8gV29ybGQh", true);

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", await blob.Content.GetAsStringAsync(null, CancellationToken.None));
            Assert.Equal("SGVsbG8gV29ybGQh", await blob.Content.GetAsBase64Async(CancellationToken.None));
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, await blob.GetAsByteArrayAsync(CancellationToken.None));            
        }

        [Fact]
        public async Task BlobTest_SomeByteArrayValue()
        {            
            BlobValue blob = FormulaValue.NewBlob(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 });

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", await blob.Content.GetAsStringAsync(null, CancellationToken.None));
            Assert.Equal("SGVsbG8gV29ybGQh", await blob.Content.GetAsBase64Async(CancellationToken.None));
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, await blob.GetAsByteArrayAsync(CancellationToken.None));            
        }   

        [Fact]
        public void BlobTest_InvalidBase64()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new Base64Blob("This_is_not_a_base64_string!"));
#if !NET462
            Assert.Equal("Invalid base64 string (Parameter 'base64Str')", ex.Message);
#else
#pragma warning disable SA1116 // Split parameters should start on line after open parenthesis
            Assert.Equal(@"Invalid base64 string
Parameter name: base64Str", ex.Message);
#pragma warning restore SA1116 // Split parameters should start on line after open parenthesis
#endif
        }

        [Fact]
        public async Task BlobTest_MultipleObjects()
        {
            BlobValue[] blobs = Enumerable.Range(0, 10).Select(i =>
            {
                BlobContent sre = new StringBlob($"Blob {i}");
                return new BlobValue(sre);
            }).ToArray();

            int i = 0;
            foreach (BlobValue blob in blobs)
            {
                Assert.NotNull(blob);
                Assert.Equal($"Blob {i++}", await blob.GetAsStringAsync(null, CancellationToken.None));                
            }          
        }

        [Fact]
        public async Task BlobTest_SomeCustomValue()
        {
            string internalContent = "Test Value";
            BlobValue blob = FormulaValue.NewBlob(new CustomBlobContent(internalContent));

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, await blob.Content.GetAsStringAsync(null, CancellationToken.None));
            Assert.Equal(string.Empty, await blob.Content.GetAsBase64Async(CancellationToken.None));
            Assert.Equal(new byte[0], await blob.GetAsByteArrayAsync(CancellationToken.None));
            var customContent = Assert.IsType<CustomBlobContent>(blob.Content);
            Assert.Equal(internalContent, customContent.InternalContent);
        }

        private class CustomBlobContent : BlobContent
        {
            public CustomBlobContent(string internalContent)
            {
                InternalContent = internalContent;
            }

            public string InternalContent { get; }

            public override Task<byte[]> GetAsByteArrayAsync(CancellationToken token)
            {
                return Task.FromResult(new byte[0]);
            }
        }
    }
}
