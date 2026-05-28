// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// A simple test implementation of <see cref="IAttributeDefinition"/> that delegates
    /// validation to an optional callback.
    /// </summary>
    internal class MockAttributeDefinition : IAttributeDefinition
    {
        public string Name { get; }

        public int MinArgCount { get; }

        public int MaxArgCount { get; }

        private readonly Func<AttributeValidationContext, IEnumerable<TexlError>> _validate;

        public MockAttributeDefinition(string name, int minArgCount = 0, int maxArgCount = 0, Func<AttributeValidationContext, IEnumerable<TexlError>> validate = null)
        {
            Name = name;
            MinArgCount = minArgCount;
            MaxArgCount = maxArgCount;
            _validate = validate;
        }

        public IEnumerable<TexlError> Validate(AttributeValidationContext context)
        {
            return _validate?.Invoke(context);
        }
    }
}
