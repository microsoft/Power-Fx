﻿// Copyright (c) Microsoft Corporation.
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
    internal class ImmutabilityTests : PowerFxTest
    {
        // We cannot use this code in .Net 5.0+ as it uses System.Runtime.CompilerServices.IsExternalInit hack defined in PFX.Core and which is only valid pre-Net 5.0
        // The symbol would be defined twice
#if !NET7_0_OR_GREATER
        // Include non-public types
        // Include non-public properties!
        // Are private-setters ok?
        public static void CheckImmutability(Assembly asm)
        {
            var errors = new StringBuilder();
            var checkedCount = 0;

            // TODO: Checking public types for now because some internal ones fail (e.g., DType)
            foreach (var t in asm.GetTypes()
                                 .Where(t => t.IsPublic && t.GetCustomAttribute<ThreadSafeImmutableAttribute>(inherit: false) != null))
            {
                // Check properties
                foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    var propName = $"{t.FullName}.{prop.Name}";
                    checkedCount++;

                    if (prop.SetMethod != null && prop.SetMethod.IsPublic)
                    {
                        // C# "Init" keyword is semantically immutable, but appears mutable to reflection at runtime.
                        // See https://alistairevans.co.uk/2020/11/01/detecting-init-only-properties-with-reflection-in-c-9/
                        var isInitKeyword = prop.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));
                        if (!isInitKeyword)
                        {
                            errors.AppendLine($"{propName} has setter");
                        }
                    }

                    if (!AnalyzeThreadSafety.IsTypeImmutable(prop.PropertyType))
                    {
                        // Values types are safe because we return a copy. 
                        if (!prop.PropertyType.IsValueType)
                        {
                            errors.AppendLine($"{propName} type ({prop.PropertyType.FullName}) is mutable");
                        }
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
#endif // !NET7_0_OR_GREATER
    }
}
