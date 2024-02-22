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
            Assert.Equal("Value cannot be null. (Parameter 'content')", ex.Message);
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
        public void BlobTest_NullValue()
        {
            BlobContent beb = new StringBlob(null);
            BlobValue blob = new BlobValue(beb);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal(string.Empty, blob.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Empty(blob.GetAsByteArrayAsync(CancellationToken.None).Result);
            Assert.Same(blob.Content, beb);
        }

        [Fact]
        public void BlobTest_EmptyValue()
        {
            BlobContent beb = new StringBlob(string.Empty);
            BlobValue blob = new BlobValue(beb);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.Content.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal(string.Empty, blob.Content.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Empty(blob.GetAsByteArrayAsync(CancellationToken.None).Result);
            Assert.Same(blob.Content, beb);
        }

        [Fact]
        public void BlobTest_SomeValue()
        {            
            BlobValue blob = FormulaValue.NewBlob("Hello World!", false);

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", blob.Content.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal("SGVsbG8gV29ybGQh", blob.Content.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, blob.GetAsByteArrayAsync(CancellationToken.None).Result);            
        }

        [Fact]
        public void BlobTest_SomeUTF32Value()
        {
            BlobValue blob = FormulaValue.NewBlob("Hello World!", false, Encoding.UTF32);

            Assert.NotNull(blob);            
            Assert.Equal("SAAAAGUAAABsAAAAbAAAAG8AAAAgAAAAVwAAAG8AAAByAAAAbAAAAGQAAAAhAAAA", blob.Content.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[] { 72, 0, 0, 0, 101, 0, 0, 0, 108, 0, 0, 0, 108, 0, 0, 0, 111, 0, 0, 0, 32, 0, 0, 0, 87, 0, 0, 0, 111, 0, 0, 0, 114, 0, 0, 0, 108, 0, 0, 0, 100, 0, 0, 0, 33, 0, 0, 0 }, blob.GetAsByteArrayAsync(CancellationToken.None).Result);
        }

        [Fact]
        public void BlobTest_SomeBase64Value()
        {            
            BlobValue blob = FormulaValue.NewBlob("SGVsbG8gV29ybGQh", true);

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", blob.Content.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal("SGVsbG8gV29ybGQh", blob.Content.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, blob.GetAsByteArrayAsync(CancellationToken.None).Result);            
        }

        [Fact]
        public void BlobTest_SomeByteArrayValue()
        {            
            BlobValue blob = FormulaValue.NewBlob(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 });

            Assert.NotNull(blob);
            Assert.Equal("Hello World!", blob.Content.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal("SGVsbG8gV29ybGQh", blob.Content.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 33 }, blob.GetAsByteArrayAsync(CancellationToken.None).Result);            
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
                BlobContent sre = new StringBlob($"Blob {i}");
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
        public void BlobTest_SomeCustomValue()
        {
            string internalContent = "Test Value";
            BlobValue blob = FormulaValue.NewBlob(new CustomBlobContent(internalContent));

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.Content.GetAsStringAsync(null, CancellationToken.None).Result);
            Assert.Equal(string.Empty, blob.Content.GetAsBase64Async(CancellationToken.None).Result);
            Assert.Equal(new byte[0], blob.GetAsByteArrayAsync(CancellationToken.None).Result);
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
