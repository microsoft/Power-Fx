// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#undef INVARIANT_CHECKS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// Implement this interface to add AssertValid/CheckValid validation capabilities to your class.
    /// </summary>
    public interface ICheckable
    {
        bool IsValid { get; }
    }

    internal static class Contracts
    {
        #region Check contracts for public APIs

        [Conditional("DEBUG")]
        public static void Check(bool f, string sid)
        {
            if (!f)
            {
                throw Except(sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNonEmpty(string s, string paramName)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (ReferenceEquals(s, null))
                {
                    throw ExceptValue(paramName);
                }

                throw ExceptEmpty(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNonEmpty(string s, string paramName, string sid)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (ReferenceEquals(s, null))
                {
                    throw ExceptValue(paramName, sid);
                }

                throw ExceptEmpty(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNonEmpty(Guid g, string paramName)
        {
            if (g == Guid.Empty)
            {
                throw ExceptEmpty(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNonEmpty(Guid? g, string paramName)
        {
            CheckValue(g, paramName);
            CheckNonEmpty(g.Value, paramName);
        }

        [Conditional("DEBUG")]
        public static void CheckNonEmpty<T>(IList<T> args, string paramName)
        {
            if (Size(args) == 0)
            {
                throw ExceptEmpty(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNonEmptyOrNull(string s, string paramName)
        {
            if (s != null && s.Length == 0)
            {
                throw ExceptEmpty(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNonEmptyOrNull(Guid? g, string paramName)
        {
            if (g.HasValue && g.Value == Guid.Empty)
            {
                throw ExceptEmpty(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckRange(bool f, string paramName)
        {
            if (!f)
            {
                throw ExceptRange(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckRange(bool f, string paramName, string sid)
        {
            if (!f)
            {
                throw ExceptRange(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexRange(int index, int count, int available, string paramName)
        {
            if (!IsValid(index, count, available))
            {
                throw ExceptRange(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexRange(int index, int count, int available, string paramName, string sid)
        {
            if (!IsValid(index, count, available))
            {
                throw ExceptRange(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexRange(long index, long count, long available, string paramName)
        {
            if (!IsValid(index, count, available))
            {
                throw ExceptRange(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexRange(long index, long count, long available, string paramName, string sid)
        {
            if (!IsValid(index, count, available))
            {
                throw ExceptRange(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndex(int index, int available, string paramName)
        {
            if (!IsValidIndex(index, available))
            {
                throw ExceptRange(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndex(int index, int available, string paramName, string sid)
        {
            if (!IsValidIndex(index, available))
            {
                throw ExceptRange(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndex(long index, long available, string paramName)
        {
            if (!IsValidIndex(index, available))
            {
                throw ExceptRange(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndex(long index, long available, string paramName, string sid)
        {
            if (!IsValidIndex(index, available))
            {
                throw ExceptRange(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexInclusive(int index, int available, string paramName)
        {
            if (!IsValidIndexInclusive(index, available))
            {
                throw ExceptRange(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexInclusive(int index, int available, string paramName, string sid)
        {
            if (!IsValidIndexInclusive(index, available))
            {
                throw ExceptRange(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexInclusive(long index, long available, string paramName)
        {
            if (!IsValidIndexInclusive(index, available))
            {
                throw ExceptRange(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckIndexInclusive(long index, long available, string paramName, string sid)
        {
            if (!IsValidIndexInclusive(index, available))
            {
                throw ExceptRange(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckParam(bool f, string paramName)
        {
            if (!f)
            {
                throw ExceptParam(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckParam(bool f, string paramName, string sid)
        {
            if (!f)
            {
                throw ExceptParam(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckValue<T>(T val, string paramName)
            where T : class
        {
            if (ReferenceEquals(val, null))
            {
                throw ExceptValue(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckValue<T>(T val, string paramName, string sid)
            where T : class
        {
            if (ReferenceEquals(val, null))
            {
                throw ExceptValue(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckValue<T>(T? val, string paramName)
            where T : struct
        {
            if (!val.HasValue)
            {
                throw ExceptValue(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNull<T>(T val, string paramName)
            where T : class
        {
            if (!ReferenceEquals(val, null))
            {
                throw ExceptNull(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNull<T>(T val, string paramName, string sid)
            where T : class
        {
            if (!ReferenceEquals(val, null))
            {
                throw ExceptNull(paramName, sid);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNull<T>(T? val, string paramName)
            where T : struct
        {
            if (val != null)
            {
                throw ExceptNull(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckNull<T>(T? val, string paramName, string sid)
            where T : struct
        {
            if (val != null)
            {
                throw ExceptNull(paramName, sid);
            }
        }
        
        public static void CheckXmlDocumentString(string text, string paramName, out XDocument parsedXDocument)
        {
            CheckNonEmpty(text, paramName);
            try
            {
                parsedXDocument = XDocument.Parse(text, LoadOptions.None);
            }
            catch
            {
                throw ExceptParam(paramName);
            }
        }

        public static void CheckXmlDocumentStringOrNull(string text, string paramName, out XDocument parsedXDocument)
        {
            if (text == null)
            {
                parsedXDocument = null;
            }
            else
            {
                CheckXmlDocumentString(text, paramName, out parsedXDocument);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckXmlDocumentString(string text, string paramName)
        {
            CheckXmlDocumentString(text, paramName, out var parsedXDocument);
        }

        [Conditional("DEBUG")]
        public static void CheckXmlDocumentStringOrNull(string text, string paramName)
        {
            if (text != null)
            {
                CheckXmlDocumentString(text, paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckAllNonEmpty(IList<string> args, string paramName)
        {
            for (var i = 0; i < Size(args); i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                {
                    throw ExceptEmpty(paramName);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void CheckAllValues<T>(IList<T> args, string paramName)
            where T : class
        {
            for (var i = 0; i < Size(args); i++)
            {
                if (ReferenceEquals(args[i], null))
                {
                    throw ExceptParam(paramName);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void CheckAllValues<T>(IEnumerable<T> args, string paramName)
            where T : class
        {
            if (!ReferenceEquals(args, null))
            {
                foreach (var arg in args)
                {
                    if (ReferenceEquals(arg, null))
                    {
                        throw ExceptParam(paramName);
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        public static void CheckAllValues<TKey, TValue>(IDictionary<TKey, TValue> args, string paramName)
            where TValue : class
        {
            if (!ReferenceEquals(args, null))
            {
                foreach (var arg in args.Values)
                {
                    if (ReferenceEquals(arg, null))
                    {
                        throw ExceptParam(paramName);
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        public static void CheckAll<T>(IList<T> args, string paramName)
            where T : struct, ICheckable
        {
            for (var i = 0; i < Size(args); i++)
            {
                if (!args[i].IsValid)
                {
                    throw ExceptValid(paramName);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void CheckValid<T>(T val, string paramName)
            where T : struct, ICheckable
        {
            if (!val.IsValid)
            {
                throw ExceptValid(paramName);
            }
        }

        [Conditional("DEBUG")]
        public static void CheckValid<T>(T val, string paramName, string sid)
            where T : struct, ICheckable
        {
            if (!val.IsValid)
            {
                throw ExceptValid(paramName, sid);
            }
        }

        [Conditional("INVARIANT_CHECKS")]        
        public static void CheckValueOrNull<T>(T val)
            where T : class
        {
        }

        [Conditional("INVARIANT_CHECKS")]        
        public static void CheckValueOrNull<T>(T val, string paramName)
            where T : class
        {
        }

        [Conditional("INVARIANT_CHECKS")]        
        public static void CheckValueOrNull<T>(T val, string name, string sid)
            where T : class
        {
        }

        [Conditional("INVARIANT_CHECKS")]        
        public static void CheckValueOrNull<T>(T? val, string paramName)
            where T : struct
        {
        }

        #endregion

        #region Assert contracts for internal validation

        [Conditional("DEBUG")]
        public static void Assert(bool f)
        {
#if DEBUG
            if (!f)
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void Assert(bool f, string msg)
        {
#if DEBUG
            if (!f)
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexRange(int index, int count, int available)
        {
#if DEBUG
            if (!IsValid(index, count, available))
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexRange(int index, int count, int available, string msg)
        {
#if DEBUG
            if (!IsValid(index, count, available))
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexRange(long index, long count, long available)
        {
#if DEBUG
            if (!IsValid(index, count, available))
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexRange(long index, long count, long available, string msg)
        {
#if DEBUG
            if (!IsValid(index, count, available))
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndex(int index, int available)
        {
#if DEBUG
            if (!IsValidIndex(index, available))
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndex(int index, int available, string msg)
        {
#if DEBUG
            if (!IsValidIndex(index, available))
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndex(long index, long available)
        {
#if DEBUG
            if (!IsValidIndex(index, available))
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndex(long index, long available, string msg)
        {
#if DEBUG
            if (!IsValidIndex(index, available))
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexInclusive(int index, int available)
        {
#if DEBUG
            if (!IsValidIndexInclusive(index, available))
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexInclusive(int index, int available, string msg)
        {
#if DEBUG
            if (!IsValidIndexInclusive(index, available))
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexInclusive(long index, long available)
        {
#if DEBUG
            if (!IsValidIndexInclusive(index, available))
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIndexInclusive(long index, long available, string msg)
        {
#if DEBUG
            if (!IsValidIndexInclusive(index, available))
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty(string s)
        {
#if DEBUG
            if (string.IsNullOrEmpty(s))
            {
                DbgFailEmpty();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty(string s, string msg)
        {
#if DEBUG
            if (string.IsNullOrEmpty(s))
            {
                DbgFailEmpty(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty(Guid g)
        {
#if DEBUG
            if (g == Guid.Empty)
            {
                DbgFailEmpty();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty(Guid? g)
        {
#if DEBUG
            AssertValue(g);
            AssertNonEmpty(g.Value);
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmptyOrNull(Guid? g)
        {
#if DEBUG
            if (g.HasValue && g.Value == Guid.Empty)
            {
                DbgFailEmpty();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty<T>(IEnumerable<T> args)
        {
#if DEBUG
            if (Size(args) == 0)
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty<T>(IEnumerable<T> args, string msg)
        {
#if DEBUG
            if (Size(args) == 0)
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty<T>(IList<T> args)
        {
#if DEBUG
            if (Size(args) == 0)
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmpty<T>(IList<T> args, string msg)
        {
#if DEBUG
            if (Size(args) == 0)
            {
                DbgFail(msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmptyOrNull(string s)
        {
#if DEBUG
            if (s != null)
            {
                AssertNonEmpty(s);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmptyOrNull(string s, string msg)
        {
#if DEBUG
            if (s != null)
            {
                AssertNonEmpty(s, msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmptyOrNull<T>(IEnumerable<T> args)
        {
#if DEBUG
            if (args != null)
            {
                AssertNonEmpty(args);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmptyOrNull<T>(IEnumerable<T> args, string msg)
        {
#if DEBUG
            if (args != null)
            {
                AssertNonEmpty(args, msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmptyOrNull<T>(IList<T> args)
        {
#if DEBUG
            if (args != null)
            {
                AssertNonEmpty(args);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNonEmptyOrNull<T>(IList<T> args, string msg)
        {
#if DEBUG
            if (args != null)
            {
                AssertNonEmpty(args, msg);
            }
#endif
        }

        /// <summary>
        /// Asserts the value is a value type (i.e. a struct, enum).
        /// Usage of this contract allows us to detect when parameter types change and should use the AssertValue method
        /// instead.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertValueType<T>(T val)
            where T : struct
        {
#if DEBUG
            // No-op: This provides compile-time check.
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertValue<T>(T? val)
            where T : struct
        {
#if DEBUG
            if (!val.HasValue)
            {
                DbgFailValue();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertValue<T>(T val)
            where T : class
        {
#if DEBUG
            if (ReferenceEquals(val, null))
            {
                DbgFailValue();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertValue<T>(T val, string name)
            where T : class
        {
#if DEBUG
            if (ReferenceEquals(val, null))
            {
                DbgFailValue(name);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertValue<T>(T val, string name, string msg)
            where T : class
        {
#if DEBUG
            if (ReferenceEquals(val, null))
            {
                DbgFailValue(name, msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNull<T>(T val)
            where T : class
        {
#if DEBUG
            if (!ReferenceEquals(val, null))
            {
                DbgFailNull();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNull<T>(T val, string name)
            where T : class
        {
#if DEBUG
            if (!ReferenceEquals(val, null))
            {
                DbgFailNull(name);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNull<T>(T val, string name, string msg)
            where T : class
        {
#if DEBUG
            if (!ReferenceEquals(val, null))
            {
                DbgFailNull(name, msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNull<T>(T? val)
            where T : struct
        {
#if DEBUG
            if (val != null)
            {
                DbgFailNull();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNull<T>(T? val, string name)
            where T : struct
        {
#if DEBUG
            if (val != null)
            {
                DbgFailNull(name);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertNull<T>(T? val, string name, string msg)
            where T : struct
        {
#if DEBUG
            if (val != null)
            {
                DbgFailNull(name, msg);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertXmlDocumentString(string text)
        {
#if DEBUG
            AssertNonEmpty(text);
            try
            {
                XDocument.Parse(text, LoadOptions.None);
            }
            catch
            {
                DbgFail();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertXmlDocumentStringOrNull(string text)
        {
#if DEBUG
            if (text != null)
            {
                AssertXmlDocumentString(text);
            }
#endif
        }

        /// <summary>
        /// Asserts that <paramref name="val"/> is not null and one of the <paramref name="expectedPossibilities"/>.
        /// This uses the default equality operator for the type.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertOneOf<T>(T val, params T[] expectedPossibilities)
            where T : class
        {
#if DEBUG
            AssertOneOf(val, (IEnumerable<T>)expectedPossibilities);
#endif
        }

        /// <summary>
        /// Asserts that <paramref name="val"/> is not null and one of the <paramref name="expectedPossibilities"/>.
        /// This uses the default equality operator for the type.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertOneOf<T>(T val, IEnumerable<T> expectedPossibilities)
            where T : class
        {
#if DEBUG
            AssertValue(val);
            AssertValue(expectedPossibilities);
            AssertAllValues(expectedPossibilities);

            if (!expectedPossibilities.Contains(val))
            {
                DbgFail(string.Concat("The value is not one of the allowed possibilities: ", val));
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertOneOfOrNull<T>(T val, params T[] expectedPossibilities)
            where T : class
        {
#if DEBUG
            if (!ReferenceEquals(val, null))
            {
                AssertOneOf(val, expectedPossibilities);
            }
#endif
        }

        /// <summary>
        /// Asserts that <paramref name="val"/> is not null and one of the <paramref name="expectedPossibilities"/>.
        /// This uses the default equality operator for the type.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertOneOfOrNull<T>(T val, IEnumerable<T> expectedPossibilities)
            where T : class
        {
#if DEBUG
            if (!ReferenceEquals(val, null))
            {
                AssertOneOf(val, expectedPossibilities);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertOneOfValueType<T>(T val, params T[] expectedPossibilities)
            where T : struct
        {
#if DEBUG
            AssertOneOfValueType(val, (IEnumerable<T>)expectedPossibilities);
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertOneOfValueType<T>(T val, IEnumerable<T> expectedPossibilities)
            where T : struct
        {
#if DEBUG
            AssertValue(expectedPossibilities);

            if (!expectedPossibilities.Contains(val))
            {
                DbgFail(string.Concat("The value is not one of the allowed possibilities: ", val));
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertOneOfValueTypeOrNull<T>(T? val, params T[] expectedPossibilities)
            where T : struct
        {
#if DEBUG
            if (val.HasValue)
            {
                AssertOneOfValueType(val.Value, (IEnumerable<T>)expectedPossibilities);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertOneOfValueTypeOrNull<T>(T? val, IEnumerable<T> expectedPossibilities)
            where T : struct
        {
#if DEBUG
            if (val.HasValue)
            {
                AssertOneOfValueType(val.Value, expectedPossibilities);
            }
#endif
        }

        /// <summary>
        /// Asserts that each value in <paramref name="values"/> is not null and one of the <paramref name="expectedPossibilities"/>.
        /// This uses the default equality operator for the type.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertAllOneOf<T>(IEnumerable<T> values, IEnumerable<T> expectedPossibilities)
            where T : class
        {
#if DEBUG
            AssertValue(expectedPossibilities);
            AssertAllValues(expectedPossibilities);

            if (values != null)
            {
                foreach (var val in values)
                {
                    AssertOneOf(val, expectedPossibilities);
                }
            }
#endif
        }

        /// <summary>
        /// Asserts that <paramref name="val"/> is null OR one of the <paramref name="expectedPossibilities"/>.
        /// This uses the default equality operator for the type.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertNullOrOneOf<T>(T val, IEnumerable<T> expectedPossibilities)
            where T : class
        {
#if DEBUG
            AssertValue(expectedPossibilities);
            AssertAllValues(expectedPossibilities);

            if (!ReferenceEquals(val, null))
            {
                AssertOneOf<T>(val, expectedPossibilities);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllNonEmpty(IList<string> args)
        {
#if DEBUG
            for (var i = 0; i < Size(args); i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                {
                    DbgFail();
                }
            }
#endif
        }

        /// <param name="args">Warning: this IEnumerable should not be read-once or it will cause side effects.</param>
        /// <param name="msg"></param>
        [Conditional("DEBUG")]
        public static void AssertAllNonEmpty(IEnumerable<string> args, string msg)
        {
#if DEBUG
            if (!ReferenceEquals(args, null))
            {
                foreach (var arg in args)
                {
                    if (string.IsNullOrEmpty(arg))
                    {
                        DbgFail(msg);
                    }
                }
            }
#endif
        }

        /// <param name="args">Warning: this IEnumerable should not be read-once or it will cause side effects.</param>
        [Conditional("DEBUG")]
        public static void AssertAllNonEmpty(IEnumerable<string> args)
        {
#if DEBUG
            if (!ReferenceEquals(args, null))
            {
                foreach (var arg in args)
                {
                    if (string.IsNullOrEmpty(arg))
                    {
                        DbgFail();
                    }
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllNonEmpty(IList<string> args, string msg)
        {
#if DEBUG
            for (var i = 0; i < Size(args); i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                {
                    DbgFail(msg);
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllValues<T>(IEnumerable<T> args)
            where T : class
        {
#if DEBUG
            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (ReferenceEquals(arg, null))
                    {
                        DbgFail();
                    }
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllValues<T>(IList<T> args)
            where T : class
        {
#if DEBUG
            for (var i = 0; i < Size(args); i++)
            {
                if (ReferenceEquals(args[i], null))
                {
                    DbgFail();
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllValues<T>(IList<T> args, string msg)
            where T : class
        {
#if DEBUG
            for (var i = 0; i < Size(args); i++)
            {
                if (ReferenceEquals(args[i], null))
                {
                    DbgFail(msg);
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllValues<TKey, TValue>(IDictionary<TKey, TValue> args)
            where TValue : class
        {
#if DEBUG
            if (!ReferenceEquals(args, null))
            {
                foreach (var arg in args.Values)
                {
                    if (ReferenceEquals(arg, null))
                    {
                        DbgFail();
                    }
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAll<T>(IList<T> args)
            where T : ICheckable
        {
#if DEBUG
            for (var i = 0; i < Size(args); i++)
            {
                if (ReferenceEquals(args[i], null) || !args[i].IsValid)
                {
                    DbgFail();
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAll<TKey, TValue>(IDictionary<TKey, TValue> args)
            where TValue : ICheckable
        {
#if DEBUG
            if (!ReferenceEquals(args, null))
            {
                foreach (var arg in args.Values)
                {
                    if (ReferenceEquals(arg, null) || !arg.IsValid)
                    {
                        DbgFail();
                    }
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertValid<T>(T val)
            where T : ICheckable
        {
#if DEBUG
            if (ReferenceEquals(val, null) || !val.IsValid)
            {
                DbgFailValid();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertValid<T>(T val, string name)
            where T : ICheckable
        {
#if DEBUG
            if (ReferenceEquals(val, null) || !val.IsValid)
            {
                DbgFailValid(name);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllValid<T>(IEnumerable<T> args)
            where T : ICheckable
        {
#if DEBUG
            foreach (var arg in args)
            {
                if (ReferenceEquals(arg, null) || !arg.IsValid)
                {
                    DbgFailValid();
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllValid<T>(IList<T> args)
            where T : ICheckable
        {
#if DEBUG
            for (var i = 0; i < Size(args); i++)
            {
                if (ReferenceEquals(args[i], null) || !args[i].IsValid)
                {
                    DbgFailValid();
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertAllValid<T>(IList<T> args, string msg)
            where T : ICheckable
        {
#if DEBUG
            for (var i = 0; i < Size(args); i++)
            {
                if (ReferenceEquals(args[i], null) || !args[i].IsValid)
                {
                    DbgFailValid(msg);
                }
            }
#endif
        }

        [Conditional("INVARIANT_CHECKS")]
        public static void AssertValueOrNull<T>(T val)
            where T : class
        {
        }

        [Conditional("INVARIANT_CHECKS")]
        public static void AssertValueOrNull<T>(T? val)
            where T : struct
        {
        }

        [Conditional("INVARIANT_CHECKS")]
        public static void AssertValueOrNull<T>(T val, string msg)
            where T : class
        {
        }

#if DEBUG
        #region Assert helpers

        // If we're running in Unit Test environment, throw an exception that can be caught by the test harness.
        private static ConstructorInfo _assertFailExCtor;

        private static void DbgFailCore(string msg)
        {
            // Only try to get a new assertFailExCtor if we do not already have one.
            if (_assertFailExCtor == null)
            {
                // Try first to get the VS UnitTestFramework constructor
                _assertFailExCtor = GetTestExceptionConstructor(
                    "Microsoft.VisualStudio.TestPlatform.UnitTestFramework",
                    "Microsoft.VisualStudio.TestPlatform.UnitTestFramework.AssertFailedException");

                // Otherwise, check for...
                if (_assertFailExCtor == null)
                {
                    _assertFailExCtor = GetTestExceptionConstructor(
                        "xunit.assert",
                        "Xunit.Sdk.XunitException");
                }

                if (_assertFailExCtor == null)
                {
                    _assertFailExCtor = GetTestExceptionConstructor(
                        "Microsoft.VisualStudio.TestPlatform.TestFramework",
                        "Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException");
                }
            }

            // Log the failure, so we get some information on server failures during JS test execution.
            // Must be done before the Debug.Assert for the tests to get it.
            Console.Error.WriteLine($"Server debug failure: {msg}. Callstack: {Environment.StackTrace}");

            if (_assertFailExCtor == null)
            {
                Debug.Assert(false, msg);
            }

            if (_assertFailExCtor != null)
            {
                var ex = (Exception)_assertFailExCtor.Invoke(new object[] { msg });
                throw ex;
            }
        }

        private static ConstructorInfo GetTestExceptionConstructor(string assemblyName, string testExceptionFullName)
        {
            ConstructorInfo testExceptionCtor = null;
            try
            {
                // Try to load the test assembly
                var uTFwkAssemblyName = new AssemblyName(assemblyName);
                var uTFwkAssembly = Assembly.Load(uTFwkAssemblyName);
                var assertFailedExClassType = uTFwkAssembly.ExportedTypes.Single(t => t.FullName == testExceptionFullName);
                var assertFailedExClassTypeInfo = IntrospectionExtensions.GetTypeInfo(assertFailedExClassType);
                testExceptionCtor = assertFailedExClassTypeInfo.DeclaredConstructors.Single(ctor => ctor.GetParameters().Length == 1);
            }

            // We are only catching exceptions as a result of not running within a unit test.
            // This will allow us to bubble up other potential problems (eg. signature issues) that would otherwise break our safety net.
            catch (IOException)
            {
            }
            catch (BadImageFormatException)
            {
            }

            return testExceptionCtor;
        }

        private static void DbgFail()
        {
            DbgFailCore("Assertion Failed");
        }
        
        private static void DbgFail(string msg)
        {
            DbgFailCore(msg);
        }

        private static void DbgFailValue()
        {
            DbgFailCore("Non-null assertion failure");
        }

        private static void DbgFailValue(string name)
        {
            DbgFailCore(string.Format(CultureInfo.CurrentCulture, "Non-null assertion failure: {0}", name));
        }

        private static void DbgFailValue(string name, string msg)
        {
            DbgFailCore(string.Format(CultureInfo.CurrentCulture, "Non-null assertion failure: {0}: {1}", name, msg));
        }

        private static void DbgFailNull()
        {
            DbgFailCore("Null assertion failure");
        }

        private static void DbgFailNull(string name)
        {
            DbgFailCore(string.Format(CultureInfo.CurrentCulture, "Null assertion failure: {0}", name));
        }

        private static void DbgFailNull(string name, string msg)
        {
            DbgFailCore(string.Format(CultureInfo.CurrentCulture, "Null assertion failure: {0}: {1}", name, msg));
        }

        private static void DbgFailEmpty()
        {
            DbgFailCore("Non-empty assertion failure");
        }

        private static void DbgFailEmpty(string msg)
        {
            DbgFailCore(string.Format(CultureInfo.CurrentCulture, "Non-empty assertion failure: {0}", msg));
        }

        private static void DbgFailValid()
        {
            DbgFailCore("Validity assertion failure");
        }

        private static void DbgFailValid(string name)
        {
            DbgFailCore(string.Format(CultureInfo.CurrentCulture, "Validity assertion failure: {0}", name));
        }

        #endregion
#endif

        #endregion

        #region Verify contracts

        // Verify contracts are used to assert a value in debug, and act as a pass through in retail.        
        public static bool Verify(this bool f, string message = "")
        {
#if DEBUG
            if (string.IsNullOrWhiteSpace(message))
            {
                Assert(f);
            }
            else
            {
                Assert(f, message);
            }
#endif
            return f;
        }
        
        public static T VerifyValue<T>(this T val)
            where T : class
        {
            AssertValue(val);
            return val;
        }
        
        public static string VerifyNonEmpty(this string val)
        {
            AssertNonEmpty(val);
            return val;
        }

        #endregion

        #region Helpers

        private static int Size<T>(IList<T> list)
        {
            return list == null ? 0 : list.Count;
        }

        /// <summary>Note: This will actualize the IEnumerable. Care should be taken to only use this when the list is not read-once.</summary>
        private static int Size<T>(IEnumerable<T> list)
        {
            return list == null ? 0 : list.Count();
        }
        
        // Internal for unit tests
        internal static bool IsValid(int index, int count, int available)
        {
            // This code explicitly allows the case of index == available, but only when
            // count == 0. This degenerate case is permitted in order to avoid problems
            // for developers consuming APIs that are range-checked using this routine.
            // Particularly interesting is the fact {index == 0, count == 0, available == 0} is
            // considered valid.

            Assert(available >= 0);

            unchecked
            {
                // Equivalent to
                // return index >= 0 && index <= available && count >= 0 && count <= available - index;
                return ((uint)index <= (uint)available) && ((uint)count <= (uint)(available - index));
            }
        }
        
        internal static bool IsValid(long index, long count, long available)
        {
            // This code explicitly allows the case of index == available, but only when
            // count == 0. This degenerate case is permitted in order to avoid problems
            // for developers consuming APIs that are range-checked using this routine.
            // Particularly interesting is the fact {index == 0, count == 0, available == 0} is
            // considered valid.

            Assert(available >= 0);

            unchecked
            {
                // Equivalent to
                // return index >= 0 && index <= available && count >= 0 && count <= available - index;
                return ((ulong)index <= (ulong)available) && ((ulong)count <= (ulong)(available - index));
            }
        }

        internal static bool IsValidIndex(int index, int available)
        {
            Assert(available >= 0);

            unchecked
            {
                // Equivalent to
                // return index >= 0 && index < available;
                return (uint)index < (uint)available;
            }
        }

        internal static bool IsValidIndex(long index, long available)
        {
            Assert(available >= 0);

            unchecked
            {
                // Equivalent to
                // return index >= 0 && index < available;
                return (ulong)index < (ulong)available;
            }
        }

        internal static bool IsValidIndexInclusive(int index, int existing)
        {
            Assert(existing >= 0);

            unchecked
            {
                // Equivalent to
                // return index >= 0 && index <= existing;
                return (uint)index <= (uint)existing;
            }
        }

        internal static bool IsValidIndexInclusive(long index, long existing)
        {
            Assert(existing >= 0);

            unchecked
            {
                // Equivalent to
                // return index >= 0 && index <= existing;
                return (ulong)index <= (ulong)existing;
            }
        }

        private static Exception Process(Exception ex)
        {
            // TASK: 69493 - Support exceptions in logging.
            // This is also a convenient point to catch validation exceptions during development.
            return ex;
        }

        public static Exception Except()
        {
            return Process(new InvalidOperationException());
        }

        public static Exception Except(string sid)
        {
            return Process(new InvalidOperationException(sid));
        }

        public static Exception Except<T>(string sid, T arg)
        {
            return Process(new InvalidOperationException(FormatMessage(sid, arg)));
        }

        public static Exception Except(string sid, params object[] args)
        {
            return Process(new InvalidOperationException(FormatMessage(sid, args)));
        }

        public static Exception ExceptRange(string paramName)
        {
            return Process(new ArgumentOutOfRangeException(paramName));
        }

        public static Exception ExceptRange(string paramName, string sid)
        {
            return Process(new ArgumentOutOfRangeException(paramName, sid));
        }

        public static Exception ExceptParam(string paramName)
        {
            return Process(new ArgumentException(paramName));
        }

        public static Exception ExceptParam(string paramName, string sid)
        {
            return Process(new ArgumentException(sid, paramName));
        }

        public static Exception ExceptParam<T>(string paramName, string sid, T arg)
        {
            return Process(new ArgumentException(FormatMessage(sid, arg), paramName));
        }

        public static Exception ExceptValue(string paramName)
        {
            return Process(new ArgumentNullException(paramName));
        }

        public static Exception ExceptValue(string paramName, string sid)
        {
            return Process(new ArgumentNullException(paramName, sid));
        }

        public static Exception ExceptNull(string paramName)
        {
            return Process(new ArgumentException(paramName));
        }

        public static Exception ExceptNull(string paramName, string sid)
        {
            return Process(new ArgumentException(paramName, sid));
        }

        public static Exception ExceptEmpty(string paramName)
        {
            return Process(new ArgumentException(paramName));
        }

        public static Exception ExceptEmpty(string paramName, string sid)
        {
            return Process(new ArgumentException(sid, paramName));
        }

        public static Exception ExceptValid(string paramName)
        {
            return Process(new ArgumentException(paramName));
        }

        public static Exception ExceptValid(string paramName, string sid)
        {
            return Process(new ArgumentException(sid, paramName));
        }
        #endregion

        private static string FormatMessage(string msg, params object[] args)
        {
            AssertValue(msg);
            AssertValue(args);
            return string.Format(CultureInfo.CurrentCulture, msg, args);
        }
    }
}
