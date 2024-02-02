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
            Assert.Equal("ResourceManager is required. (Parameter 'resourceManager')", ex.Message);
        }

        [Fact]
        public void BlobTest_ConstructorNullResourceManager()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new BlobValue(null, new ResourceHandle() { Handle = 0 }));
            Assert.Equal("ResourceManager is required. (Parameter 'resourceManager')", ex.Message);
        }

        [Fact]
        public void BlobTest_ConstructorInvalidElement()
        {
            IResourceManager resourceManager = new DefaultResourceManager();
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new BlobValue(resourceManager, new ResourceHandle() { Handle = -1 }));
            Assert.Equal("ResourceManager does not contain element with Id -1. (Parameter 'handle')", ex.Message);
        }

        [Fact]
        public void BlobTest_InvalidCoercions()
        {
            IResourceManager resourceManager = new DefaultResourceManager();
            ResourceHandle id = new StringResourceElement(resourceManager, null).Handle;
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
            DefaultResourceManager resourceManager = new DefaultResourceManager();
            ResourceHandle id = new StringResourceElement(resourceManager, null).Handle;
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Null(blob.ResourceElement.GetAsStringAsync().Result);
            Assert.Null(blob.ResourceElement.GetAsBase64StringAsync().Result);
            Assert.Equal(id.Handle, blob.Handle.Handle);            
            Assert.Equal("appres://blobmanager/0", blob.ToString());
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(new ResourceHandle() { Handle = 0 }));

            resourceManager.RemoveResource(id);
            Assert.Null(resourceManager.GetResource(id));
        }

        [Fact]
        public void BlobTest_EmptyValue()
        {
            DefaultResourceManager resourceManager = new DefaultResourceManager();
            ResourceHandle id = new StringResourceElement(resourceManager, string.Empty).Handle;
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Equal(string.Empty, blob.ResourceElement.GetAsStringAsync().Result);
            Assert.Equal(string.Empty, blob.ResourceElement.GetAsBase64StringAsync().Result);
            Assert.Equal("appres://blobmanager/0", blob.ToString());
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(new ResourceHandle() { Handle = 0 }));
        }

        [Fact]
        public void BlobTest_SomeValue()
        {
            IResourceManager resourceManager = new DefaultResourceManager();
            ResourceHandle id = new StringResourceElement(resourceManager, "Hello world!").Handle;
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Equal("Hello world!", blob.ResourceElement.GetAsStringAsync().Result);
            Assert.Equal("SGVsbG8gd29ybGQh", blob.ResourceElement.GetAsBase64StringAsync().Result);
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(new ResourceHandle() { Handle = 0 }));
        }

        [Fact]
        public void BlobTest_SomeBase64Value()
        {
            IResourceManager resourceManager = new DefaultResourceManager();            
            ResourceHandle id = new Base64StringResourceElement(resourceManager, "SGVsbG8gd29ybGQh").Handle;
            BlobValue blob = new BlobValue(resourceManager, id);

            Assert.NotNull(blob);
            Assert.Equal("Hello world!", blob.ResourceElement.GetAsStringAsync().Result);
            Assert.Equal("SGVsbG8gd29ybGQh", blob.ResourceElement.GetAsBase64StringAsync().Result);
            Assert.Same(blob.ResourceElement, resourceManager.GetResource(new ResourceHandle() { Handle = 0 }));
        }

        [Fact]
        public void BlobTest_InvalidBase64()
        {
            IResourceManager resourceManager = new DefaultResourceManager();
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new Base64StringResourceElement(resourceManager, "This_is_not_a_base64_string!"));
            Assert.Equal("Invalid Base64 string (Parameter 'str')", ex.Message);
        }

        [Fact]
        public void BlobTest_MultipleObjects()
        {
            IResourceManager resourceManager = new DefaultResourceManager();
            BlobValue[] blobs = Enumerable.Range(0, 10).Select(i =>
            {
                ResourceHandle id = new StringResourceElement(resourceManager, $"Blob {i}").Handle;
                return new BlobValue(resourceManager, id);
            }).ToArray();

            foreach (BlobValue blob in blobs)
            {
                Assert.NotNull(blob);
                Assert.Equal($"Blob {blob.Handle.Handle}", blob.ResourceElement.GetAsStringAsync().Result);
                Assert.Equal($"appres://blobmanager/{blob.Handle.Handle}", blob.ToString());                
            }

            Assert.Equal(45, blobs.Sum(b => b.Handle.Handle));
        }        
    }
}
