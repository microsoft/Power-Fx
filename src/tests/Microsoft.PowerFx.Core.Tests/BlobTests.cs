// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
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
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new BlobValue(IR.IRContext.NotInSource(FormulaType.String)));
            Assert.Equal("ResourceManager is required. (Id=-2) (Parameter 'resourceManager')", ex.Message);
        }

        [Fact]
        public void BlobTest_ConstructorNullResourceManager()
        {            
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new BlobValue(null, new TestResourceManager().NewElementFromString("abc")));
            Assert.Equal("ResourceManager is required. (Id=0) (Parameter 'resourceManager')", ex.Message);
        }

        [Fact]
        public void BlobTest_ConstructorInvalidElement()
        {
            IResourceManager resourceManager = new TestResourceManager();
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new BlobValue(resourceManager, -1));
            Assert.Equal("ResourceManager does not contain element with Id -1. (Parameter 'id')", ex.Message);
        }

        [Fact]
        public void BlobTest_InvalidCoercions()
        {
            IResourceManager resourceManager = new TestResourceManager();
            int id = resourceManager.NewElementFromString(null);
            BlobValue blob = new BlobValue(resourceManager, id);

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
            TestResourceManager resourceManager = new TestResourceManager();
            int id = resourceManager.NewElementFromString(null);
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Null(blob.ResourceElement.String);
            Assert.Null(blob.ResourceElement.Base64String);
            Assert.Equal(id, blob.Id);
            Assert.Equal(FileType.Any, blob.ResourceElement.FileType);
            Assert.Equal("appres://blobmanager/0", blob.ToString());
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(0));

            resourceManager.RemoveResource(0);
            Assert.Null(resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_EmptyValue()
        {
            IResourceManager resourceManager = new TestResourceManager();
            int id = resourceManager.NewElementFromString(string.Empty);
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.ResourceElement.String);
            Assert.Equal(string.Empty, blob.ResourceElement.Base64String);
            Assert.Equal("appres://blobmanager/0", blob.ToString());
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_SomeValue()
        {
            IResourceManager resourceManager = new TestResourceManager();
            int id = resourceManager.NewElementFromString("Hello world!");
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Equal("Hello world!", blob.ResourceElement.String);
            Assert.Equal("SGVsbG8gd29ybGQh", blob.ResourceElement.Base64String);            
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_SomeBase64Value()
        {
            IResourceManager resourceManager = new TestResourceManager();
            int id = resourceManager.NewElementFromBase64String("SGVsbG8gd29ybGQh");
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Equal("Hello world!", blob.ResourceElement.String);
            Assert.Equal("SGVsbG8gd29ybGQh", blob.ResourceElement.Base64String);                        
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(0));
        }

        [Fact]
        public void BlobTest_InvalidBase64()
        {
            IResourceManager resourceManager = new TestResourceManager();            

            ArgumentException ex = Assert.Throws<ArgumentException>(() => resourceManager.NewElementFromBase64String("This_is_not_a_base64_string!"));
            Assert.Equal("Invalid Base64 string (Parameter 'str')", ex.Message);
        }

        [Fact]
        public void BlobTest_MultipleObjects()
        {
            IResourceManager resourceManager = new TestResourceManager();
            BlobValue[] blobs = Enumerable.Range(0, 10).Select(i =>
            {
                int id = resourceManager.NewElementFromString($"Blob {i}");
                return new BlobValue(resourceManager, id);
            }).ToArray();

            foreach (BlobValue blob in blobs)
            {
                Assert.NotNull(blob);
                Assert.Equal($"Blob {blob.Id}", blob.ResourceElement.String);                
                Assert.Equal($"appres://blobmanager/{blob.Id}", blob.ToString());                
            }

            Assert.Equal(45, blobs.Sum(b => b.Id));
        }        
    }
}
