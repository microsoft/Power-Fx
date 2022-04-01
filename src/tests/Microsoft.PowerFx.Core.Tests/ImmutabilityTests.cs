// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// TODO.
    /// </summary>
    internal class ImmutabilityTests
    {
        public static void CheckImmutability(Assembly asm)
        {
            var errors = new StringBuilder();
            var checkedCount = 0;

            // TODO: Checking public types for now because some internal ones fail (e.g., DType)
            foreach (var t in asm.GetTypes()
                                 .Where(t => t.IsPublic && t.GetCustomAttribute<ThreadSafeImmutableAttribute>() != null))
            {
                // Check properties
                foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    var propName = $"{t.FullName}.{prop.Name}";
                    checkedCount++;

                    if (prop.CanWrite)
                    {
                        errors.AppendLine($"{propName} has setter");
                    }

                    if (!AnalyzeThreadSafety.IsTypeImmutable(prop.PropertyType))
                    {
                        errors.AppendLine($"{propName} type ({prop.PropertyType.FullName}) is mutable");
                    }
                }

                // Check fields
                foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    var fieldName = $"{t.FullName}.{field.Name}";
                    checkedCount++;

                    if (!field.IsInitOnly)
                    {
                        errors.AppendLine($"{fieldName} is not readonly");
                    }

                    if (!AnalyzeThreadSafety.IsTypeImmutable(field.FieldType))
                    {
                        errors.AppendLine($"{fieldName} type ({field.FieldType}) is mutable");
                    }
                }

                // TODO: Probably does not make sense to check methods? They could return mutable stuff
            }

            // Sanity check
            Assert.True(checkedCount > 10);

            Assert.True(errors.Length == 0, $"Mutability errors: {errors}");
        }
    }
}
