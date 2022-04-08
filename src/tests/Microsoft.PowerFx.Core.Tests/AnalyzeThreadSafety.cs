// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// Analyze assemblies for thread safety issues. 
    /// </summary>
    public class AnalyzeThreadSafety
    {
        // Verify there are no "unsafe" static fields that could be threading issues.
        // Bugs - list of field types types that don't work. This should be driven to 0. 
        // BugNames - list of "Type.Field" that don't work. This should be driven to 0. 
        public static void CheckStatics(Assembly asm, HashSet<Type> bugsFieldType, HashSet<string> bugNames)
        {
            // Being immutable is the easiest way to be thread safe. Tips:
            // `const` fields are safest - but that can only apply to literals. 
            //
            // readonly fields are safe if the field type is also immutable. So either:
            // - change to immutable interface, like `readonly Dict<string,int> _keys` --> `readonly IReadOnlyDict<string,int> _keys`
            // - mark type as immutable via [ImmutableObject] attribute. 
            //
            // Compiler properties will generate a backing field - we still catch the field via reflection.
            // `int Prop {get;set; } = 123`  // mutable backing field! 
            // `int Prop {get; } = 123`      // readonly backing field! 
            var errors = new List<string>();

            var total = 0;
            foreach (var type in asm.GetTypes())
            {
                // Reflecting over fields will also find compiler-generated "backing fields" from static properties. 
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var name = $"{type.Name}.{field.Name}";
                    total++;

                    if (field.Attributes.HasFlag(FieldAttributes.Literal))
                    {
                        continue;  // 'const' keyword, 
                    }

                    if (type.Name.Contains("<>") ||
                        type.Name.Contains("<PrivateImplementationDetails>"))
                    {
                        continue; // exclude compiler generated closures. 
                    }

                    // Field has is protected by a lock.
                    if (field.GetCustomAttributes<ThreadSafeProtectedByLockAttribute>().Any())
                    {
                        continue;
                    }

                    if (bugsFieldType != null && bugsFieldType.Contains(field.FieldType))
                    {
                        continue;
                    }

                    if (bugNames != null && bugNames.Contains(name))
                    {
                        continue;
                    }

                    if (field.GetCustomAttribute<ThreadStaticAttribute>() != null)
                    {
                        // If field is marked [ThreadStatic], then each thread gets its own copy.
                        // It also implies the author thought about threading. 
                        continue;
                    }

                    if (IsFieldVolatile(field))
                    {
                        // If a field was marked volatile, then assume the author thought through the threading.
                        continue;
                    }

                    // Is it readonly? Const?
                    if (!field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    {
                        // Mutable static field! That's bad.  
                        errors.Add($"{name} is not readonly");
                        continue;
                    }

                    // Is it a
                    if (!IsTypeImmutable(field.FieldType))
                    {
                        errors.Add($"{name} readonly, but still a mutable type {field.FieldType}");
                    }

                    // Safe! The static field is readonly and set to an immutable object. 
                }
            }

            // Sanity check that we actually ran the test. 
            Assert.True(total > 10, "failed to find fields");

            // Batch up errors so we can see all at once. 
            Assert.Empty(errors);
        }

        private static bool IsFieldVolatile(FieldInfo field)
        {
            var isVolatile = field
                .GetRequiredCustomModifiers()
                .Any(x => x == typeof(IsVolatile));
            return isVolatile;
        }

        // For other custom types, mark with [ThreadSafeImmutable] attribute.
        private static readonly HashSet<Type> _knownImmutableTypes = new HashSet<Type>
        {
            // Primitives
            typeof(object),
            typeof(string),
            typeof(Random),
            typeof(DateTime),
            typeof(System.Text.RegularExpressions.Regex),
            typeof(System.Numerics.BigInteger),
        };

        // If the instance is readonly, is the type itself immutable ?
        internal static bool IsTypeImmutable(Type t)
        {
            if (t.IsArray)
            {
                // Arrays are definitely not safe - their elements can be mutated.
                return false;
            }

            if (t.IsPrimitive || t.IsEnum)
            {
                return true;
            }

            var attr = t.GetCustomAttribute<ThreadSafeImmutableAttribute>();
            if (attr != null)
            {
                return true;
            }

            // Collection classes should be a IReadOnly<T>. Verify their T is also safe.
            if (t.IsGenericType)
            {
                var genericDef = t.GetGenericTypeDefinition();
                if (genericDef == typeof(IReadOnlyDictionary<,>))
                {
                    // For a Dict<key,value>, need to make sure the values are also safe. 
                    var valueArg = t.GetGenericArguments()[1];
                    var isValueArgSafe = IsTypeImmutable(valueArg);
                    return isValueArgSafe;
                }

                if (genericDef == typeof(IReadOnlyList<>))
                {
                    var valueArg = t.GetGenericArguments()[0];
                    return IsTypeImmutable(valueArg);
                }

                if (genericDef == typeof(IEnumerable<>))
                {
                    var valueArg = t.GetGenericArguments()[0];
                    var isValueArgSafe = IsTypeImmutable(valueArg);
                    return isValueArgSafe;
                }
            }

            if (_knownImmutableTypes.Contains(t))
            {
                return true;
            }

            // Treat delegates as immutable. They're just static functions. 
            // If the delegate is closed over mutable state, those arugments would show up as fields and be caught.
            if (t.BaseType == typeof(MulticastDelegate))
            {
                return true;
            }

            return false;
        }
    }
}
