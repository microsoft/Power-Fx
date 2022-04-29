// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class DTypeTests
    {
        [Fact]
        public void MaxDepth()
        {
            Assert.Equal(0, new DType(DKind.Number).MaxDepth);
            Assert.Equal(0, new DType(DKind.String).MaxDepth);
            Assert.Equal(0, new DType(DKind.Boolean).MaxDepth);
            Assert.Equal(0, new DType(DKind.DateTime).MaxDepth);
            Assert.Equal(0, new DType(DKind.Date).MaxDepth);
            Assert.Equal(0, new DType(DKind.Time).MaxDepth);
            Assert.Equal(0, new DType(DKind.Currency).MaxDepth);
            Assert.Equal(0, new DType(DKind.Image).MaxDepth);
            Assert.Equal(0, new DType(DKind.PenImage).MaxDepth);
            Assert.Equal(0, new DType(DKind.Media).MaxDepth);
            Assert.Equal(0, new DType(DKind.Blob).MaxDepth);
            Assert.Equal(0, new DType(DKind.Hyperlink).MaxDepth);
            Assert.Equal(0, new DType(DKind.Color).MaxDepth);
            Assert.Equal(0, new DType(DKind.Guid).MaxDepth);
            Assert.Equal(0, new DType(DKind.Control).MaxDepth);
            Assert.Equal(0, new DType(DKind.DataEntity).MaxDepth);
            Assert.Equal(0, new DType(DKind.Polymorphic).MaxDepth);
            Assert.Equal(1, new DType(DKind.Record).MaxDepth);
            Assert.Equal(1, new DType(DKind.Table).MaxDepth);

            DType.TryParse("*[A:n, B:s, C:b]", out var dType1);
            Assert.Equal(1, dType1.MaxDepth);

            var metaFieldName = "'meta-6de62757-ecb6-4be6-bb85-349b3c7938a9'";
            DType.TryParse("*[" + metaFieldName + ":![A:n, B:s, C:b]", out var dType2);
            Assert.Equal(0, dType2.MaxDepth);

            DType.TryParse("*[A:![A:n]]", out var dType3);
            Assert.Equal(2, dType3.MaxDepth);
            DType.TryParse("*[A:![B:*[C:n]]]", out var dType4);
            Assert.Equal(3, dType4.MaxDepth);
            DType.TryParse("*[X:*[Y:n], A:![B:*[C:n]]]", out var dType5);
            Assert.Equal(3, dType5.MaxDepth);
        }
    }
}
