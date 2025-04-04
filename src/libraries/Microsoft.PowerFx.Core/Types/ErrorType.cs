﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    internal sealed class ErrorType
    {
        public const string KindFieldName = "Kind";

        public const string MessageFieldName = "Message";

        private static IEnumerable<TypedName> ErrorDetailsSchema => new[]
        {
            new TypedName(DType.Number, new DName("HttpStatusCode")),
            new TypedName(DType.String, new DName("HttpResponse")),
        };

        /// <returns>
        /// The schema for an error value.
        /// </returns>
        private static IEnumerable<TypedName> ReifiedErrorSchema => new[]
        {
            new TypedName(DType.Number, new DName(KindFieldName)),
            new TypedName(DType.String, new DName(MessageFieldName)),
            new TypedName(DType.String, new DName("Source")),
            new TypedName(DType.String, new DName("Observed")),
            new TypedName(DType.CreateRecord(ErrorDetailsSchema), new DName("Details"))
        };

        /// <returns>
        /// The <see cref="DType"/> of an error value.
        /// </returns>
        public static DType ReifiedError() => DType.CreateRecord(ReifiedErrorSchema, isSealed: true);

        /// <returns>
        /// The <see cref="DType"/> of a collection of error values.
        /// </returns>
        public static DType ReifiedErrorTable() => DType.CreateTable(ReifiedErrorSchema, isSealed: true);
    }
}
