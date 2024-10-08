// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Core.Tests
{
    public static class Extensions
    {
        public static string GetCompactIRString(this CheckResult check)
        {
            return PrettyPrintIRVisitor.ToString(check);
        }

        public static void AddBehaviorFunction(this PowerFxConfig config)
        {
            config.AddFunction(new BehaviorFunction());
        }

        public static string GetErrBehaviorPropertyExpectedMessage()
        {
            return StringResources.GetErrorResource(TexlStrings.ErrBehaviorPropertyExpected).GetSingleValue(ErrorResource.ShortMessageTag);
        }

        public static string ToStringWithDisplayNames(this FormulaType ftype)
        {
            var sb = new StringBuilder();
            sb.AppendToWithDisplayNames(ftype._type, ftype);
            return sb.ToString();
        }

        internal static string AppendToWithDisplayNames(this StringBuilder sb, DType dtype, FormulaType ftype)
        {
            sb.Append(DType.MapKindToStr(dtype.Kind));

            switch (dtype.Kind)
            {
                case DKind.Record:
                case DKind.Table:
                    AppendAggregateType(sb, dtype.TypeTree, dtype.DisplayNameProvider);
                    break;
                case DKind.LazyRecord:
                case DKind.LazyTable:
                    AppendAggregateLazyType(sb, ftype as AggregateType ?? throw new InvalidOperationException("Not a valid type"));
                    break;
                case DKind.OptionSet:
                case DKind.View:
                    AppendOptionSetOrViewType(sb, dtype.TypeTree, dtype.DisplayNameProvider);
                    break;
                case DKind.Enum:
                    AppendEnumType(sb, dtype.ValueTree, dtype.EnumSuperkind, dtype.DisplayNameProvider);
                    break;
                case DKind.OptionSetValue:
                    AppendOptionSetValue(sb, dtype.OptionSetInfo);
                    break;
            }

            return sb.ToString();
        }

        private static void AppendOptionSetValue(StringBuilder sb, IExternalOptionSet optionSet)
        {
            if (optionSet is EnumSymbol es)
            {
                sb.Append('(');
                bool first = true;

                foreach (DName name in es.OptionNames.OrderBy(dn => dn.Value, StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;

                    sb.Append(TexlLexer.EscapeName(name.Value));
                    if (es.TryLookupValueByName(name.Value, out object value))
                    {
                        sb.Append('=');
                        sb.Append(value.ToString());
                    }
                }

                sb.Append(')');
            }
        }

        private static void AppendAggregateType(StringBuilder sb, TypeTree tree, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append("[");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs().OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value) && TexlLexer.EscapeName(kvp.Key) != TexlLexer.EscapeName(kvp2.Value.Value))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(kvp2.Value.Value));
                }

                sb.Append(":");
                sb.AppendToWithDisplayNames(kvp.Value, null);
                strPre = ", ";
            }

            sb.Append("]");
        }

        private static void AppendAggregateLazyType(StringBuilder sb, AggregateType fType)
        {
            Contracts.AssertValue(sb);

            sb.Append("[");

            var strPre = string.Empty;
            foreach (string fieldName in fType.FieldNames.OrderBy(f => f, StringComparer.Ordinal))
            {
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(fieldName));

                string display = fType._type.DisplayNameProvider.LogicalToDisplayPairs.FirstOrDefault(kvp => kvp.Key.Value == fieldName).Value.Value;

                if (!string.IsNullOrEmpty(display) && TexlLexer.EscapeName(display) != TexlLexer.EscapeName(fieldName))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(display));
                }

                sb.Append(":");

                IExternalTabularDataSource ads = fType._type.AssociatedDataSources.FirstOrDefault();
                InternalTableParameters internalTableParameters = ads as InternalTableParameters;

                if (internalTableParameters == null && fType._type.TryGetType(new DName(fieldName), out DType type))
                {
                    sb.Append(type.ToString());
                }
                else if (internalTableParameters != null)
                {
                    if (internalTableParameters.ColumnsWithRelationships.TryGetValue(fieldName, out string remoteTable))
                    {
                        sb.Append('~');
                        sb.Append(remoteTable);
                        sb.Append(':');

                        if (!internalTableParameters.RecordType.TryGetUnderlyingFieldType(fieldName, out FormulaType backingFieldType))
                        {
                            throw new InvalidOperationException();
                        }

                        sb.Append(backingFieldType._type.ToString());
                    }
                    else if (ads.Type.TryGetType(new DName(fieldName), out DType type2))
                    {
                        sb.Append(type2.ToString());
                    }
                    else
                    {
                        sb.Append('§');
                    }
                }
                else
                {
                    sb.Append("§§");
                }

                strPre = ", ";
            }

            sb.Append("]");
        }

        private static void AppendOptionSetOrViewType(StringBuilder sb, TypeTree tree, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append("{");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs().OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value) && TexlLexer.EscapeName(kvp.Key) != TexlLexer.EscapeName(kvp2.Value.Value))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(kvp2.Value.Value));
                }

                sb.Append(":");
                sb.AppendToWithDisplayNames(kvp.Value, null);
                strPre = ", ";
            }

            sb.Append("}");
        }

        private static void AppendEnumType(StringBuilder sb, ValueTree tree, DKind enumSuperkind, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append(DType.MapKindToStr(enumSuperkind));
            sb.Append("[");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs().OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                Contracts.AssertNonEmpty(kvp.Key);
                Contracts.AssertValue(kvp.Value.Object);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value) && TexlLexer.EscapeName(kvp.Key) != TexlLexer.EscapeName(kvp2.Value.Value))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(kvp2.Value.Value));
                }

                sb.Append(":");
                kvp.Value.AppendTo(sb);
                strPre = ", ";
            }

            sb.Append("]");
        }

        // Run on an isolated thread.
        // Useful for testing per-thread properties
        internal static void RunOnIsolatedThread(this CultureInfo culture, Action<CultureInfo> worker)
        {
            Exception exception = null;

            var t = new Thread(() =>
            {
                try
                {
                    worker(culture);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            t.Start();
            t.Join();

            if (exception != null)
            {
                throw exception;
            }
        }
    }
}
