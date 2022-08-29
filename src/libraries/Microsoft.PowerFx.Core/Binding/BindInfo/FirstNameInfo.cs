// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    /// <summary>
    /// Binding information for "first" names.a.
    /// </summary>
    internal sealed class FirstNameInfo : NameInfo
    {
        public override DName Name => Node.AsFirstName().Ident.Name;

        // Nesting level of where this name is defined.
        // Negative values mean "up".
        // Positive values mean that the target is a parameter of a nested lambda.
        // In RowScope bind, NestDst specifies the upcount for the row scope alias.
        public readonly int NestDst;

        // The nesting level of where this name is being used. This is always >= 0 and >= NestDst.
        public readonly int NestSrc;

        // Qualifying path. May be DPath.Root (unqualified).
        public readonly DPath Path;

        // Optional data associated with a FirstName. May be null.
        public readonly object Data;

        // For FirstNames with BindKind.ThisItem, are we accessing a field of the control template instead of data?
        public readonly bool IsDataControlAccess;
        public readonly DName DataControlName;
        public readonly bool IsPageable;

        private readonly Lazy<Dictionary<ExpandPath, ExpandQueryOptions>> _dataQueryOptions;

        public Dictionary<ExpandPath, ExpandQueryOptions> DataQueryOptions => _dataQueryOptions.Value;

        // The number of containing scopes up where the name is defined.
        // 0 refers to the current/innermost scope, a higher number refers to a parent/ancestor scope.
        public int UpCount => NestSrc - NestDst;

        private FirstNameInfo(BindKind kind, FirstNameNode node, int nestDst, int nestCur, DPath path, object data, DName dataControlName, bool isDataControlAccess, bool isPageable = false)
            : base(kind, node)
        {
            Contracts.Assert(nestDst >= int.MinValue);
            Contracts.Assert(nestCur >= 0);
            Contracts.Assert(nestCur >= nestDst);
            Contracts.AssertValueOrNull(data);

            NestDst = nestDst;
            NestSrc = nestCur;
            Path = path;
            Data = data;
            IsDataControlAccess = isDataControlAccess;
            DataControlName = dataControlName;
            IsPageable = isPageable;

            _dataQueryOptions = new Lazy<Dictionary<ExpandPath, ExpandQueryOptions>>();
        }

        // Create a qualified name (global/alias/enum/resource), e.g. "screen1.group25.slider2", or "Color".
        // Can also be used to create fields of ThisItem (with an appropriate upCount).
        public static FirstNameInfo Create(FirstNameNode node, NameLookupInfo lookupInfo)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(lookupInfo.Kind != BindKind.LambdaField && lookupInfo.Kind != BindKind.LambdaFullRecord);
            Contracts.Assert(lookupInfo.UpCount >= 0);
            Contracts.Assert(lookupInfo.Path.IsValid);

            return new FirstNameInfo(lookupInfo.Kind, node, -lookupInfo.UpCount, 0, lookupInfo.Path, lookupInfo.Data, default, false, lookupInfo.IsPageable);
        }

        public static FirstNameInfo Create(FirstNameNode node, NameLookupInfo lookupInfo, IExpandInfo data)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(lookupInfo.Kind == BindKind.DeprecatedImplicitThisItem);
            Contracts.Assert(lookupInfo.UpCount >= 0);
            Contracts.Assert(lookupInfo.Path.IsValid);

            return new FirstNameInfo(lookupInfo.Kind, node, -lookupInfo.UpCount, 0, lookupInfo.Path, data, default, false);
        }

        public static FirstNameInfo Create(FirstNameNode node, NameLookupInfo lookupInfo, DName dataControlName, bool isDataControlAccess)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(lookupInfo.Kind == BindKind.ThisItem);
            Contracts.Assert(lookupInfo.UpCount >= 0);
            Contracts.Assert(lookupInfo.Path.IsValid);

            return new FirstNameInfo(lookupInfo.Kind, node, -lookupInfo.UpCount, 0, lookupInfo.Path, lookupInfo.Data, dataControlName, isDataControlAccess);
        }

        public static FirstNameInfo Create(FirstNameNode node, ScopedNameLookupInfo lookupInfo)
        {
            Contracts.AssertValue(node);

            return new FirstNameInfo(BindKind.ScopeArgument, node, 0, 0, DPath.Root, lookupInfo, default, false);
        }

        public static FirstNameInfo Create(FirstNameNode node, IExternalControl data)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(data);

            return new FirstNameInfo(BindKind.Control, node, 0, 0, DPath.Root, data, default, false);
        }

        // Create either an unqualified scoped field:
        //      e.g. "X" in Filter(T, X < 2)
        // ..or a row scope alias:
        //      e.g. "T1" in Filter(T1, T1[@a] < 100)
        public static FirstNameInfo Create(BindKind kind, FirstNameNode node, int nestDst, int nestSrc, object data = null)
        {
            Contracts.Assert(kind == BindKind.LambdaField || kind == BindKind.LambdaFullRecord || kind == BindKind.ComponentNameSpace);
            Contracts.AssertValue(node);
            Contracts.Assert(nestDst > int.MinValue);
            Contracts.Assert(nestSrc >= 0);
            Contracts.Assert(nestSrc >= nestDst);
            Contracts.AssertValueOrNull(data);

            return new FirstNameInfo(kind, node, nestDst, nestSrc, DPath.Root, data, default, false);
        }
    }
}
