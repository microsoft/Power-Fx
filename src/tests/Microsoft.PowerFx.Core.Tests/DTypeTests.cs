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

            _ = DType.TryParse("*[A:n, B:s, C:b]", out var dType1);
            Assert.Equal(1, dType1.MaxDepth);

            var metaFieldName = "'meta-6de62757-ecb6-4be6-bb85-349b3c7938a9'";
            _ = DType.TryParse("*[" + metaFieldName + ":![A:n, B:s, C:b]", out var dType2);
            Assert.Equal(0, dType2.MaxDepth);

            _ = DType.TryParse("*[A:![A:n]]", out var dType3);
            Assert.Equal(2, dType3.MaxDepth);

            _ = DType.TryParse("*[A:![B:*[C:n]]]", out var dType4);
            Assert.Equal(3, dType4.MaxDepth);

            _ = DType.TryParse("*[X:*[Y:n], A:![B:*[C:n]]]", out var dType5);
            Assert.Equal(3, dType5.MaxDepth);
        }

        [Fact]
        public void DType_TextRole()
        {
            var type = @"%s[Default:""default"", Heading1:""heading1"", Heading2:""heading2"", Heading3:""heading3"", Heading4:""heading4""]";

            var b = DType.TryParse(type, out var dType);
            Assert.True(b);

            var str = dType.ToString();
            Assert.Equal(type, str);
        }
    }
}
