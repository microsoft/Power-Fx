// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class BlobTests : PowerFxTest
    {
        [Fact]
        public void BlobTest_IRConstructor()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new BlobValue(IR.IRContext.NotInSource(FormulaType.String)));
            Assert.Equal("Invalid fileType (Parameter 'fileType')", ex.Message);
        }

        [Fact]
        public void BlobTest_ConstructorNullResourceManager()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new BlobValue(null, "xxx", false));
            Assert.Equal("ResourceManager is required. (Parameter 'resourceManager')", ex.Message);
        }

        [Fact]
        public void BlobTest_InvalidCoercions()
        {
            ResourceManager resourceManager = new ResourceManager();
            BlobValue blob = new BlobValue(resourceManager, null, false);

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
            ResourceManager resourceManager = new ResourceManager();
            BlobValue blob = new BlobValue(resourceManager, null, false);

            Assert.NotNull(blob);
            Assert.Null(blob.String);
            Assert.Null(blob.Base64String);
            Assert.Equal(0, blob.Id);
            Assert.Equal(FileType.Any, blob.FileType);
            Assert.Equal("appres://blobmanager/0", blob.ToString());
            Assert.Same(blob, resourceManager.GetResource(0));

            resourceManager.RemoveResource(0);
            Assert.Null(resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_EmptyValue()
        {
            ResourceManager resourceManager = new ResourceManager();
            BlobValue blob = new BlobValue(resourceManager, string.Empty, false);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.String);
            Assert.Equal(string.Empty, blob.Base64String);
            Assert.Equal("appres://blobmanager/0", blob.ToString());
            Assert.Same(blob, resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_SomeValue()
        {
            ResourceManager resourceManager = new ResourceManager();
            BlobValue blob = new BlobValue(resourceManager, "Hello world!", false);

            Assert.NotNull(blob);
            Assert.Equal("Hello world!", blob.String);
            Assert.Equal("SGVsbG8gd29ybGQh", blob.Base64String);            
            Assert.Same(blob, resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_SomeBase64Value()
        {
            ResourceManager resourceManager = new ResourceManager();
            BlobValue blob = new BlobValue(resourceManager, "SGVsbG8gd29ybGQh", true);

            Assert.NotNull(blob);
            Assert.Equal("Hello world!", blob.String);
            Assert.Equal("SGVsbG8gd29ybGQh", blob.Base64String);                        
            Assert.Same(blob, resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_InvalidBase64()
        {
            ResourceManager resourceManager = new ResourceManager();

            ArgumentException ex = Assert.Throws<ArgumentException>(() => new BlobValue(resourceManager, "This_is_not_a_base64_string!", true));
            Assert.Equal("Invalid Base64 string (Parameter 'str')", ex.Message);
        }

        [Fact]
        public void BlobTest_MultipleObjects()
        {
            ResourceManager resourceManager = new ResourceManager();
            BlobValue[] blobs = Enumerable.Range(0, 10).Select(i => new BlobValue(resourceManager, $"Blob {i}", false)).ToArray();

            foreach (BlobValue blob in blobs)
            {
                Assert.NotNull(blob);
                Assert.Equal($"Blob {blob.Id}", blob.String);                
                Assert.Equal($"appres://blobmanager/{blob.Id}", blob.ToString());
                Assert.Same(blob, resourceManager.GetResource(blob.Id));
            }

            Assert.Equal(45, blobs.Sum(b => b.Id));
        }        
    }
}
