// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Core.Tests
{
    public static class Extensions
    {
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
            var dtype = ftype._type;
            var sb = new StringBuilder();
            sb.Append(DType.MapKindToStr(dtype.Kind));

            switch (dtype.Kind)
            {
                case DKind.Record:
                case DKind.Table:
                    AppendAggregateType(sb, dtype.TypeTree, dtype.DisplayNameProvider);
                    break;
                case DKind.OptionSet:
                case DKind.View:
                    AppendOptionSetOrViewType(sb, dtype.TypeTree, dtype.DisplayNameProvider);
                    break;
                case DKind.Enum:
                    AppendEnumType(sb, dtype.ValueTree, dtype.EnumSuperkind, dtype.DisplayNameProvider);
                    break;
            }
             
            return sb.ToString();
        }

        private static void AppendAggregateType(StringBuilder sb, TypeTree tree, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append("[");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value))
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

        private static void AppendOptionSetOrViewType(StringBuilder sb, TypeTree tree, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append("{");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(kvp2.Value.Value));
                }

                sb.Append(":");
                kvp.Value.AppendTo(sb);
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
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.AssertNonEmpty(kvp.Key);
                Contracts.AssertValue(kvp.Value.Object);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value))
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
    }
}
