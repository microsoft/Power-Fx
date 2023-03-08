﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Microsoft.PowerFx.Core.Types
{
    [ThreadSafeImmutable]
    internal class DType : ICheckable
    {
        public const char EnumPrefix = '%';
        public const string MetaFieldName = "meta-6de62757-ecb6-4be6-bb85-349b3c7938a9";

        public static readonly DType Unknown = new DType(DKind.Unknown);
        public static readonly DType Boolean = new DType(DKind.Boolean);
        public static readonly DType Number = new DType(DKind.Number);
        public static readonly DType String = new DType(DKind.String);
        public static readonly DType DateTimeNoTimeZone = new DType(DKind.DateTimeNoTimeZone);
        public static readonly DType DateTime = new DType(DKind.DateTime);
        public static readonly DType Date = new DType(DKind.Date);
        public static readonly DType Time = new DType(DKind.Time);
        public static readonly DType Hyperlink = new DType(DKind.Hyperlink);
        public static readonly DType Currency = new DType(DKind.Currency);
        public static readonly DType Image = new DType(DKind.Image);
        public static readonly DType PenImage = new DType(DKind.PenImage);
        public static readonly DType Media = new DType(DKind.Media);
        public static readonly DType Color = new DType(DKind.Color);
        public static readonly DType Blob = new DType(DKind.Blob);
        public static readonly DType Guid = new DType(DKind.Guid);
        public static readonly DType OptionSet = new DType(DKind.OptionSet);
        public static readonly DType OptionSetValue = new DType(DKind.OptionSetValue);
        public static readonly DType ObjNull = new DType(DKind.ObjNull);
        public static readonly DType Error = new DType(DKind.Error);
        public static readonly DType EmptyRecord = new DType(DKind.Record);
        public static readonly DType EmptyTable = new DType(DKind.Table);
        public static readonly DType EmptyEnum = new DType(DKind.Unknown, default(ValueTree));
        public static readonly DType Polymorphic = new DType(DKind.Polymorphic);
        public static readonly DType View = new DType(DKind.View);
        public static readonly DType ViewValue = new DType(DKind.ViewValue);
        public static readonly DType NamedValue = new DType(DKind.NamedValue);
        public static readonly DType MinimalLargeImage = CreateMinimalLargeImageType();
        public static readonly DType UntypedObject = new DType(DKind.UntypedObject);
        public static readonly DType Deferred = new DType(DKind.Deferred);

        public static readonly DType Invalid = new DType();

        public static IEnumerable<DType> GetPrimitiveTypes()
        {
            yield return Boolean;
            yield return Number;
            yield return String;
            yield return DateTimeNoTimeZone;
            yield return DateTime;
            yield return Date;
            yield return Time;
            yield return Hyperlink;
            yield return Currency;
            yield return Image;
            yield return PenImage;
            yield return Media;
            yield return Color;
            yield return Blob;
        }

        private static readonly Lazy<Dictionary<DKind, DKind>> _kindToSuperkindMapping =
            new Lazy<Dictionary<DKind, DKind>>(
                () => new Dictionary<DKind, DKind>
            {
                { DKind.DateTimeNoTimeZone, DKind.DateTime },
                { DKind.Date, DKind.DateTime },
                { DKind.Time, DKind.DateTime },
                { DKind.Image, DKind.Hyperlink },
                { DKind.Media, DKind.Hyperlink },
                { DKind.Blob, DKind.Hyperlink },
                { DKind.PenImage, DKind.Image },
                { DKind.Boolean, DKind.Error },
                { DKind.Number, DKind.Error },
                { DKind.String, DKind.Error },
                { DKind.DateTime, DKind.Error },
                { DKind.Hyperlink, DKind.String },
                { DKind.Guid, DKind.Error },
                { DKind.Currency, DKind.Number },
                { DKind.Color, DKind.Error },
                { DKind.Control, DKind.Error },
                { DKind.DataEntity, DKind.Error },
                { DKind.Metadata, DKind.Error },
                { DKind.File, DKind.Error },
                { DKind.LargeImage, DKind.Error },
                { DKind.OptionSet, DKind.Error },
                { DKind.OptionSetValue, DKind.Error },
                { DKind.Polymorphic, DKind.Error },
                { DKind.Record, DKind.Error },
                { DKind.Table, DKind.Error },
                { DKind.ObjNull, DKind.Error },
                { DKind.View, DKind.Error },
                { DKind.ViewValue, DKind.Error },
                { DKind.NamedValue, DKind.Error },
                { DKind.UntypedObject, DKind.Error },
            }, isThreadSafe: true);

        public static Dictionary<DKind, DKind> KindToSuperkindMapping => _kindToSuperkindMapping.Value;

        #region Core fields 
        public DKind Kind { get; }

        // Fields of an aggregate type (Record/table).  Just logical names. 
        // Immutable tree. 
        public TypeTree TypeTree { get; }

        // These are default values except for Enums.
        public DKind EnumSuperkind { get; }

        // Don't use this. Use option sets instead. 
        // Special case for old enums. 
        public ValueTree ValueTree { get; }

        #endregion 

        #region New Generic versions of legacy features. 

        /// <summary>
        /// Intended future home of all lazy type expansion (Control, Relationship, Other).
        /// </summary>
        internal readonly LazyTypeProvider LazyTypeProvider;

        /// <summary>
        /// Provides a logical / display name mapping. 
        /// Eventually, all display names should come from this centralized source.
        /// We should not be using individual DataSource/OptionSet/View references.
        /// </summary>
        internal DisplayNameProvider DisplayNameProvider { get; private set; }

        /// <summary>
        /// NamedValueKind is used only for values of kind NamedValue
        /// It is a restriction on what variety of named value it actually is
        /// Semantically, NamedValues of a given kind only interact with other ones of the same Kind
        /// Null for non-named value DTypes.
        /// </summary>
        internal string NamedValueKind { get; }

        /// <summary>
        /// Describes OptionSets. Includes display names and naming info. 
        /// Can create <see cref="OptionSetValue"/>s. 
        /// </summary>
        internal IExternalOptionSet OptionSetInfo { get; }

        #endregion

        #region Fields for Dataverse Support 

        // These are legacy implementation that are special cases for dataverse concepts. 
        // We're trying to move away from these to the more generic versions. 

        // External data source for a tabular connection, like a Dataverse Entity or Sharepoint.
        // Can also provide Display Names.
        internal HashSet<IExternalTabularDataSource> AssociatedDataSources { get; }

        // Describes a relationships on tabular connections. 
        // This is a "placeholder" field that can expand to another entity (ala a TypeRef). 
        // Can also provide Display Names.
        // Should eventually be subsumed by LazyTypeProvider
        public IExpandInfo ExpandInfo { get; }

        // This is very similar interface to OptionSets, could potentially unify. 
        internal IExternalViewInfo ViewInfo { get; }

        public IPolymorphicInfo PolymorphicInfo { get; }

        public IDataColumnMetadata Metadata { get; }

        #endregion

        #region Bad Fields

        // These fields are extra state for hacks that should be removed. 

        private readonly bool _isFile;

        private readonly bool _isLargeImage;

        private bool? _isActivityPointer;

        /// <summary>
        /// Hack for binding Service Function optional parameters. 
        /// The last parameter type of service functions is a record.  The fields of this argument do not have to
        /// be defined in order for an invocation to correctly type check.  The individual field types must match
        /// the expected type exactly, however, so it is necessary to set this value for a single aggregate DType
        /// and not for the individual field types within.
        /// </summary>
        public bool AreFieldsOptional { get; set; } = false;

        #endregion 

        /// <summary>
        ///  Whether this type is a subtype of all possible types, meaning that it can be placed in
        ///  any location without coercion.
        /// </summary>
        internal bool IsUniversal => Kind == DKind.Error || Kind == DKind.ObjNull;

        // Constructor for the single invalid DType sentinel value.
        private DType()
        {
        }

        internal DType(DKind kind)
        {
            Contracts.Assert(kind >= DKind._Min && kind < DKind._Lim);

            Kind = kind;
            TypeTree = default;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            AssertValid();
        }

        internal DType(DKind kind, TypeTree tree, HashSet<IExternalTabularDataSource> dataSourceInfo, DisplayNameProvider displayNameProvider = null)
            : this(kind, tree)
        {
            Contracts.AssertValueOrNull(dataSourceInfo);

            if (dataSourceInfo == null)
            {
                dataSourceInfo = new HashSet<IExternalTabularDataSource>();
            }

            AssociatedDataSources = dataSourceInfo;
            DisplayNameProvider = displayNameProvider;
        }

        public virtual DType Clone()
        {
            AssertValid();

            return new DType(
                Kind,
                TypeTree,
                EnumSuperkind,
                ValueTree,
                ExpandInfo,
                PolymorphicInfo,
                Metadata,
                IsFile,
                IsLargeImage,
                new HashSet<IExternalTabularDataSource>(AssociatedDataSources),
                OptionSetInfo,
                ViewInfo,
                NamedValueKind,
                DisplayNameProvider,
                LazyTypeProvider);
        }

        // Constructor for aggregate types (record, table)
        public DType(DKind kind, TypeTree tree, bool isFile = false, bool isLargeImage = false)
        {
            Contracts.Assert(kind >= DKind._Min && kind < DKind._Lim);
            tree.AssertValid();
            Contracts.Assert(tree.IsEmpty || kind == DKind.Table || kind == DKind.Record);

            Kind = kind;
            TypeTree = tree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            _isFile = isFile;
            _isLargeImage = isLargeImage;
            AssertValid();
        }

        // Constructor for enum types
        public DType(DKind superkind, ValueTree enumTree)
        {
            Contracts.Assert(superkind >= DKind._Min && superkind < DKind._Lim);

            Kind = DKind.Enum;
            TypeTree = default;
            EnumSuperkind = superkind;
            ValueTree = enumTree;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            AssertValid();
        }

        // Constructor for control types
        protected DType(TypeTree outputTypeTree)
        {
            outputTypeTree.AssertValid();

            Kind = DKind.Control;
            TypeTree = outputTypeTree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            AssertValid();
        }

        // Constructor for Entity types
        private DType(DKind kind, IExpandInfo info, TypeTree outputTypeTree, HashSet<IExternalTabularDataSource> associatedDataSources = null)
        {
            Contracts.AssertValue(info);
            outputTypeTree.AssertValid();

            Kind = kind;
            TypeTree = outputTypeTree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = info;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = associatedDataSources ?? new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            AssertValid();
        }

        // Constructor for Polymorphic types
        private DType(DKind kind, IPolymorphicInfo info, TypeTree outputTypeTree, HashSet<IExternalTabularDataSource> associatedDataSources = null)
        {
            Contracts.AssertValue(info);
            outputTypeTree.AssertValid();

            Kind = kind;
            TypeTree = outputTypeTree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = info;
            Metadata = null;
            AssociatedDataSources = associatedDataSources ?? new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            AssertValid();
        }

        // Constructor for Metadata type
        private DType(DKind kind, IDataColumnMetadata metadata, TypeTree outputTypeTree)
        {
            Contracts.Assert(kind == DKind.Metadata);
            Contracts.AssertValue(metadata);
            outputTypeTree.AssertValid();

            Kind = kind;
            TypeTree = outputTypeTree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = metadata.IsExpandEntity ? metadata.Type.ExpandInfo : null;
            PolymorphicInfo = null;
            Metadata = metadata;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            AssertValid();
        }

        // Constructor for File or large image type
        private DType(DKind kind, DType complexType)
        {
            Contracts.AssertValid(complexType);
            Contracts.Assert(kind == DKind.File || kind == DKind.LargeImage);

            Kind = kind;
            TypeTree = complexType.TypeTree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            _isFile = kind == DKind.File;
            _isLargeImage = kind == DKind.LargeImage;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            AssertValid();
        }

        // Constructor for OptionSet type
        private DType(DKind kind, TypeTree outputTypeTree, IExternalOptionSet info)
        {
            Contracts.Assert(kind == DKind.OptionSet);
            Contracts.AssertValue(info);
            outputTypeTree.AssertValid();

            Kind = kind;
            TypeTree = outputTypeTree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = info;
            ViewInfo = null;
            NamedValueKind = null;
            DisplayNameProvider = info.DisplayNameProvider;
            AssertValid();
        }

        // Constructor for OptionSetValue type
        private DType(DKind kind, IExternalOptionSet info)
        {
            Contracts.Assert(kind == DKind.OptionSetValue);
            Contracts.AssertValue(info);

            Kind = kind;
            TypeTree = default;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = info;
            ViewInfo = null;
            NamedValueKind = null;
            DisplayNameProvider = info.DisplayNameProvider;
            AssertValid();
        }

        // Constructor for View type
        private DType(DKind kind, TypeTree outputTypeTree, IExternalViewInfo info)
        {
            Contracts.Assert(kind == DKind.View);
            Contracts.AssertValue(info);
            outputTypeTree.AssertValid();

            Kind = kind;
            TypeTree = outputTypeTree;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = info;
            NamedValueKind = null;
            DisplayNameProvider = info.DisplayNameProvider;
            AssertValid();
        }

        // Constructor for ViewValue type
        private DType(DKind kind, IExternalViewInfo info)
        {
            Contracts.Assert(kind == DKind.ViewValue);
            Contracts.AssertValue(info);

            Kind = kind;
            TypeTree = default;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = info;
            NamedValueKind = null;
            DisplayNameProvider = info.DisplayNameProvider;
            AssertValid();
        }

        // Constructor for NamedValue type
        private DType(DKind kind, string namedValueKind)
        {
            Contracts.Assert(kind == DKind.NamedValue);
            Contracts.AssertNonEmptyOrNull(namedValueKind);

            Kind = kind;
            TypeTree = default;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = namedValueKind;
            AssertValid();
        }

        internal DType(LazyTypeProvider provider, bool isTable, DisplayNameProvider displayNameProvider = null)
        {
            Contracts.AssertValue(provider);

            LazyTypeProvider = provider;
            Kind = isTable ? DKind.LazyTable : DKind.LazyRecord;

            TypeTree = default;
            EnumSuperkind = default;
            ValueTree = default;
            ExpandInfo = null;
            PolymorphicInfo = null;
            Metadata = null;
            AssociatedDataSources = new HashSet<IExternalTabularDataSource>();
            OptionSetInfo = null;
            ViewInfo = null;
            NamedValueKind = null;
            DisplayNameProvider = displayNameProvider;

            AssertValid();
        }

        [Conditional("DEBUG")]
        internal void AssertValid()
        {
            Contracts.Assert(Kind >= DKind._Min && Kind < DKind._Lim);
#if DEBUG
            TypeTree.AssertValid();
#endif
            Contracts.Assert(TypeTree.IsEmpty || Kind == DKind.Table || Kind == DKind.Record || Kind == DKind.Control || Kind == DKind.DataEntity || Kind == DKind.File || Kind == DKind.LargeImage || Kind == DKind.OptionSet || Kind == DKind.OptionSetValue || Kind == DKind.View || Kind == DKind.ViewValue);
            Contracts.Assert(ValueTree.IsEmpty || Kind == DKind.Enum);
            Contracts.Assert(Kind != DKind.Enum || (EnumSuperkind >= DKind._Min && EnumSuperkind < DKind._Lim && EnumSuperkind != DKind.Enum));
            Contracts.Assert((Metadata != null) == (Kind == DKind.Metadata));
            Contracts.Assert((LazyTypeProvider != null) == (Kind == DKind.LazyRecord || Kind == DKind.LazyTable));

#if DEBUG
            if (ExpandInfo != null)
            {
                Contracts.Assert((Kind == DKind.Table) || (Kind == DKind.Record) || (Kind == DKind.DataEntity) || (Kind == DKind.Metadata));
            }

            if (ValueTree.Count > 1)
            {
                var pairs = ValueTree.GetPairs();
                var firstObjType = pairs.First().Value.Object.GetType();
                Contracts.Assert(pairs.All(kvp => kvp.Value.Object.GetType() == firstObjType));
            }
#endif
        }

        public bool IsValid => Kind >= DKind._Min && Kind < DKind._Lim;

        public bool IsUnknown => Kind == DKind.Unknown;

        public bool IsDeferred => Kind == DKind.Deferred;

        public bool IsError => Kind == DKind.Error;

        public bool IsRecord => Kind == DKind.Record || Kind == DKind.ObjNull || Kind == DKind.LazyRecord;

        public bool IsTable => Kind == DKind.Table || Kind == DKind.ObjNull || Kind == DKind.LazyTable;

        public bool IsEnum => Kind == DKind.Enum || Kind == DKind.ObjNull;

        public bool IsColumn => IsTable && ChildCount == 1;

        public bool IsControl => Kind == DKind.Control;

        public bool IsExpandEntity => Kind == DKind.DataEntity;

        public bool IsMetadata => Kind == DKind.Metadata;

        public bool IsAttachment => IsLazyType && LazyTypeProvider.BackingFormulaType is BuiltInLazyTypes.AttachmentType;

        public bool IsPolymorphic => Kind == DKind.Polymorphic;

        public bool IsOptionSet => Kind == DKind.OptionSet || Kind == DKind.OptionSetValue;

        public bool IsView => Kind == DKind.View || Kind == DKind.ViewValue;

        public bool IsAggregate => IsRecord || IsTable;

        public bool IsPrimitive => (Kind >= DKind._MinPrimitive && Kind < DKind._LimPrimitive) || Kind == DKind.ObjNull;

        public bool IsUntypedObject => Kind == DKind.UntypedObject;

        public bool IsFile => _isFile || Kind == DKind.File;

        public bool IsLargeImage => _isLargeImage || Kind == DKind.LargeImage;

        public bool IsActivityPointer
        {
            get
            {
                if (_isActivityPointer.HasValue)
                {
                    return _isActivityPointer.Value;
                }

                TryGetType(new DName("activity_pointer_fax"), out var activityReferenceType);
                _isActivityPointer = IsRecord && activityReferenceType != Invalid;
                return _isActivityPointer.Value;
            }
        }

        public DType AttachmentType => IsAttachment ? LazyTypeProvider.GetExpandedType(IsTable) : DType.Invalid;

        public bool HasExpandInfo => ExpandInfo != null;

        public bool IsLazyType => Kind == DKind.LazyRecord || Kind == DKind.LazyTable;

        public bool HasPolymorphicInfo => PolymorphicInfo != null;

        public int ChildCount
        {
            get
            {
                if (IsAggregate)
                {
                    return TypeTree.Count;
                }

                return 0;
            }
        }

        public bool IsNamedValue => Kind == DKind.NamedValue;

        public bool IsNamedValueOfKind(string kind)
        {
            return Kind == DKind.NamedValue && NamedValueKind == kind;
        }

        // REVIEW ragru: investigate how we can compute this on construction.
        public int MaxDepth => IsAggregate
                    ? 1 +
                      (ChildCount == 0
                          ? 0
                          : GetAllNames(DPath.Root)
                              .Max(tn => tn.Type.MaxDepth))
                    : 0;

        public bool HasErrors => IsAggregate
                    ? GetNames(DPath.Root).Any(tn => tn.Type.IsError || tn.Type.HasErrors)
                    : IsError;

        public bool Contains(DName fieldName)
        {
            Contracts.AssertValid(fieldName);

            return IsAggregate && TypeTree.Contains(fieldName);
        }

        public bool Contains(DPath fieldPath)
        {
            Contracts.AssertValid(fieldPath);

            return IsAggregate && TryGetType(fieldPath, out var _);
        }

        public static DType AttachDataSourceInfo(DType type, IExternalTabularDataSource dsInfo, bool attachToNestedType = true)
        {
            type.AssertValid();
            Contracts.AssertValue(dsInfo);

            var returnType = type.Clone();
            returnType.AssociatedDataSources.Add(dsInfo);

            if (!attachToNestedType || type.IsLazyType)
            {
                return returnType;
            }

            var fError = false;
            foreach (var typedName in returnType.GetNames(DPath.Root))
            {
                returnType = returnType.SetType(ref fError, DPath.Root.Append(typedName.Name), AttachDataSourceInfo(typedName.Type, dsInfo, false), skipCompare: true);
            }

            return returnType;
        }

        /// <summary>
        /// This should only be used when constructing DTypes from the public surface to replace an existing display name provider.
        /// </summary>
        public static DType ReplaceDisplayNameProvider(DType type, DisplayNameProvider displayNames)
        {
            type.AssertValid();
            Contracts.AssertValue(displayNames);

            var returnType = type.Clone();
            returnType.DisplayNameProvider = displayNames;
            return returnType;
        }

        /// <summary>
        /// This should be used by internal operations to update the set of display name providers associated with a type, i.e. during Union operations.
        /// Display name providers are disabled if there's a conflict with an existing provider.
        /// </summary>
        public static DType AttachOrDisableDisplayNameProvider(DType type, DisplayNameProvider displayNames)
        {
            type.AssertValid();
            Contracts.AssertValue(displayNames);

            var returnType = type.Clone();
            if (returnType.DisplayNameProvider == null)
            {
                returnType.DisplayNameProvider = displayNames;
            }
            else if (!ReferenceEquals(type.DisplayNameProvider, displayNames))
            {
                returnType.DisplayNameProvider = DisabledDisplayNameProvider.Instance;
            }

            return returnType;
        }

        public DType ExpandEntityType(DType expandedType, HashSet<IExternalTabularDataSource> associatedDatasources)
        {
            Contracts.AssertValid(expandedType);
            Contracts.Assert(HasExpandInfo);

            // expandedType is always a table as that's what runtime registers it.
            // But EntityInfo.IsTable defines whether it's a table or record.
            if (!ExpandInfo.IsTable && expandedType.IsTable)
            {
                expandedType = expandedType.ToRecord();
            }

            Contracts.AssertValid(expandedType);
            return new DType(expandedType.Kind, ExpandInfo.Clone(), expandedType.TypeTree, associatedDatasources);
        }

        public DType ExpandPolymorphic(DType expandedType, IExpandInfo expandInfo)
        {
            Contracts.AssertValid(expandedType);
            Contracts.AssertValue(expandInfo);
            Contracts.Assert(HasPolymorphicInfo);

            if (!PolymorphicInfo.IsTable && expandedType.IsTable)
            {
                expandedType = expandedType.ToRecord();
            }

            Contracts.AssertValid(expandedType);
            return new DType(expandedType.Kind, expandInfo, expandedType.TypeTree, expandedType.AssociatedDataSources);
        }

        // Use for keeping Entity Info in expando properties
        public static DType CopyExpandInfo(DType to, DType from)
        {
            Contracts.AssertValid(to);
            Contracts.AssertValid(from);
            Contracts.Assert(from.HasExpandInfo);

            return new DType(to.Kind, from.ExpandInfo.Clone(), to.TypeTree, to.AssociatedDataSources);
        }

        public static DType CreateRecord(params TypedName[] typedNames)
        {
            return CreateRecordOrTable(DKind.Record, typedNames);
        }

        public static DType CreateRecord(IEnumerable<TypedName> typedNames)
        {
            return CreateRecordOrTable(DKind.Record, typedNames);
        }

        public static DType CreateTable(params TypedName[] typedNames)
        {
            return CreateRecordOrTable(DKind.Table, typedNames);
        }

        public static DType CreateTable(IEnumerable<TypedName> typedNames)
        {
            return CreateRecordOrTable(DKind.Table, typedNames);
        }

        public static DType CreateFile(params TypedName[] typedNames)
        {
            return CreateRecordOrTable(DKind.Record, typedNames, true);
        }

        public static DType CreateLargeImage(params TypedName[] typedNames)
        {
            return CreateRecordOrTable(DKind.Record, typedNames, isLargeImage: true);
        }

        private static DType CreateRecordOrTable(DKind kind, IEnumerable<TypedName> typedNames, bool isFile = false, bool isLargeImage = false)
        {
            Contracts.Assert(kind == DKind.Record || kind == DKind.Table);
            Contracts.AssertValue(typedNames);

            return new DType(kind, TypeTree.Create(typedNames.Select(TypedNameToKVP)), isFile, isLargeImage);
        }

        public static DType CreateExpandType(IExpandInfo info)
        {
            Contracts.AssertValue(info);

            return new DType(DKind.DataEntity, info, Unknown.TypeTree);
        }

        public static DType CreatePolymorphicType(IPolymorphicInfo info)
        {
            Contracts.AssertValue(info);

            return new DType(DKind.Polymorphic, info, Unknown.TypeTree);
        }

        public static DType CreateMetadataType(IDataColumnMetadata metadata)
        {
            Contracts.AssertValue(metadata);

            return new DType(DKind.Metadata, metadata, Unknown.TypeTree);
        }

        /// <summary>
        /// Attachment types can be either tables or records, and are represented using a LazyTable/Record type.
        /// </summary>
        public static DType CreateAttachmentType(DType attachmentType)
        {
            Contracts.AssertValid(attachmentType);
            Contracts.Assert(attachmentType.IsAggregate);

            var attachmentRecord = new BuiltInLazyTypes.AttachmentType(attachmentType.ToRecord());

            return attachmentType.IsTable ? attachmentRecord.ToTable()._type : attachmentRecord._type;
        }

        public static DType CreateFileType(DType fileType)
        {
            Contracts.AssertValid(fileType);

            return new DType(DKind.File, fileType);
        }

        public static DType CreateLargeImageType(DType imageType)
        {
            Contracts.AssertValid(imageType);

            return new DType(DKind.LargeImage, imageType);
        }

        public static DType CreateOptionSetType(IExternalOptionSet info)
        {
            Contracts.AssertValue(info);
            Contracts.Assert(info.BackingKind is DKind.String or DKind.Number or DKind.Boolean or DKind.Color);

            var typedNames = new List<TypedName>();

            foreach (var name in info.OptionNames)
            {
                var type = new DType(DKind.OptionSetValue, info);
                typedNames.Add(new TypedName(type, name));
            }

            return new DType(DKind.OptionSet, TypeTree.Create(typedNames.Select(TypedNameToKVP)), info);
        }

        public static DType CreateViewType(IExternalViewInfo info)
        {
            Contracts.AssertValue(info);

            var typedNames = new List<TypedName>();

            foreach (var name in info.ViewNames)
            {
                var type = new DType(DKind.ViewValue, info);
                typedNames.Add(new TypedName(type, name));
            }

            return new DType(DKind.View, TypeTree.Create(typedNames.Select(TypedNameToKVP)), info);
        }

        public static DType CreateNamedValueType(string namedValueKind)
        {
            Contracts.AssertNonEmptyOrNull(namedValueKind);

            return new DType(DKind.NamedValue, namedValueKind);
        }

        public static DType CreateMinimalLargeImageType()
        {
            var minTypeTree = new List<KeyValuePair<string, DType>>
            {
                new KeyValuePair<string, DType>("Value", Image)
            };
            return new DType(DKind.Record, TypeTree.Create(minTypeTree));
        }

        public static DType CreateOptionSetValueType(IExternalOptionSet info)
        {
            return new DType(DKind.OptionSetValue, info);
        }

        public static DType CreateViewValue(IExternalViewInfo info)
        {
            return new DType(DKind.ViewValue, info);
        }

        private static KeyValuePair<string, DType> TypedNameToKVP(TypedName typedName)
        {
            Contracts.Assert(typedName.IsValid);
            return new KeyValuePair<string, DType>(typedName.Name, typedName.Type);
        }

        public static DType CreateEnum(DType supertype, ValueTree valueTree)
        {
            Contracts.Assert(supertype.IsValid);

            return new DType(supertype.Kind, valueTree);
        }

        public static DType CreateEnum(DType supertype, params KeyValuePair<DName, object>[] pairs)
        {
            return CreateEnum(supertype, (IEnumerable<KeyValuePair<DName, object>>)pairs);
        }

        public static DType CreateEnum(DType supertype, IEnumerable<KeyValuePair<DName, object>> pairs)
        {
            Contracts.Assert(supertype.IsValid);
            Contracts.AssertValue(pairs);

            return new DType(supertype.Kind, ValueTree.Create(pairs.Select(NamedObjectToKVP)));
        }

        private static KeyValuePair<string, EquatableObject> NamedObjectToKVP(KeyValuePair<DName, object> pair)
        {
            Contracts.Assert(pair.Key.IsValid);
            Contracts.AssertValue(pair.Value);

            // Coercing all numerics to double to avoid mismatches between 1.0 vs. 1, and such.
            var value = pair.Value;
            if (value is int || value is uint)
            {
                value = (double)((int)value);
            }
            else if (value is long || value is ulong)
            {
                value = (double)((long)value);
            }

            return new KeyValuePair<string, EquatableObject>(pair.Key.Value, new EquatableObject(value));
        }

        /// <summary>
        /// Get the string form representation for the Kind to be displayed in the UI.
        /// </summary>
        /// <returns>String representation of DType.Kind.</returns>
        public string GetKindString()
        {
            if (Kind == DKind._MinPrimitive)
            {
                return "Boolean";
            }

            if (Kind == DKind.String)
            {
                return "Text";
            }

            if (Kind == DKind._LimPrimitive)
            {
                return "Control";
            }

            if (IsLazyType)
            {
                Contracts.AssertValue(LazyTypeProvider);

                var typeSuffix = string.IsNullOrEmpty(LazyTypeProvider.UserVisibleTypeName) ? string.Empty : $" ({LazyTypeProvider.UserVisibleTypeName})";
                return (IsTable ? DKind.Table : DKind.Record) + typeSuffix;
            }

            if (IsOptionSet)
            {
                var typeSuffix = string.IsNullOrEmpty(OptionSetInfo?.EntityName) ? string.Empty : $" ({OptionSetInfo.EntityName})";
                var kind = OptionSetInfo is EnumSymbol ? DKind.Enum : Kind;
                return kind.ToString() + typeSuffix;
            }

            if (IsView)
            {
                var typeSuffix = string.IsNullOrEmpty(ViewInfo?.EntityName) ? string.Empty : $" ({ViewInfo.EntityName})";
                return Kind.ToString() + typeSuffix;
            }

            return Kind.ToString();
        }

        public IEnumerable<IExpandInfo> GetExpands()
        {
            AssertValid();

            var expands = new List<IExpandInfo>();
            foreach (var typedName in GetNames(DPath.Root))
            {
                if (typedName.Type.IsExpandEntity)
                {
                    expands.Add(typedName.Type.ExpandInfo.VerifyValue());
                }
                else if (typedName.Type.IsPolymorphic && typedName.Type.HasPolymorphicInfo)
                {
                    foreach (var expand in typedName.Type.PolymorphicInfo.Expands)
                    {
                        expands.Add(expand);
                    }
                }
            }

            return expands;
        }

        public DType ToRecord()
        {
            var fError = false;
            var type = ToRecord(ref fError);

            if (fError)
            {
                Contracts.Assert(false, "Bad source kind for ToRecord");
            }

            return type;
        }

        public DType ToRecord(ref bool fError)
        {
            AssertValid();

            switch (Kind)
            {
                case DKind.LazyRecord:
                case DKind.Record:
                    return this;
                case DKind.LazyTable:
                    return new DType(LazyTypeProvider, isTable: false, DisplayNameProvider);
                case DKind.Table:
                case DKind.DataEntity:
                case DKind.Control:
                    if (ExpandInfo != null)
                    {
                        return new DType(DKind.Record, ExpandInfo, TypeTree);
                    }
                    else
                    {
                        return new DType(DKind.Record, TypeTree, AssociatedDataSources, DisplayNameProvider);
                    }

                case DKind.ObjNull:
                    return EmptyRecord;
                default:
                    fError = true;
                    return EmptyRecord;
            }
        }

        public DType ToTable()
        {
            var fError = false;
            var type = ToTable(ref fError);

            if (fError)
            {
                Contracts.Assert(false, "Bad source kind for ToTable");
            }

            return type;
        }

        public DType ToTable(ref bool fError)
        {
            AssertValid();

            switch (Kind)
            {
                case DKind.LazyTable:
                case DKind.Table:
                    return this;
                case DKind.LazyRecord:
                    return new DType(LazyTypeProvider, isTable: true, DisplayNameProvider);
                case DKind.Record:
                case DKind.DataEntity:
                case DKind.Control:
                    if (ExpandInfo != null)
                    {
                        return new DType(DKind.Table, ExpandInfo, TypeTree);
                    }
                    else
                    {
                        return new DType(DKind.Table, TypeTree, AssociatedDataSources, DisplayNameProvider);
                    }

                case DKind.ObjNull:
                    return EmptyTable;
                default:
                    fError = true;
                    return EmptyTable;
            }
        }

        // Get the underlying value associated with the specified enum value.
        public bool TryGetEnumValue(DName name, out object value)
        {
            AssertValid();

            if (!name.IsValid || Kind != DKind.Enum)
            {
                value = default;
                return false;
            }

            var result = ValueTree.TryGetValue(name.Value, out var obj);
            value = obj.Object;
            return result;
        }

        // The type this enum derives from.
        public DType GetEnumSupertype()
        {
            AssertValid();
            Contracts.Assert(IsEnum);

            return new DType(EnumSuperkind);
        }

        public bool TryGetEntityDelegationMetadata(out IDelegationMetadata metadata)
        {
            if (!HasExpandInfo)
            {
                metadata = null;
                return false;
            }

            Contracts.CheckValue(ExpandInfo.ParentDataSource, nameof(ExpandInfo.ParentDataSource));
            Contracts.CheckValue(ExpandInfo.ParentDataSource.DataEntityMetadataProvider, nameof(ExpandInfo.ParentDataSource.DataEntityMetadataProvider));

            var metadataProvider = ExpandInfo.ParentDataSource.DataEntityMetadataProvider;
            if (!metadataProvider.TryGetEntityMetadata(ExpandInfo.Identity, out var entityMetadata))
            {
                metadata = null;
                return false;
            }

            Contracts.CheckValue(entityMetadata, nameof(entityMetadata));

            metadata = entityMetadata.DelegationMetadata;
            return true;
        }

        // Get the type of the specified member field (name).
        // name.Value can be null
        public bool TryGetType(DName name, out DType type)
        {
            AssertValid();

            if (!name.IsValid)
            {
                type = Invalid;
                return false;
            }

            return TryGetTypeCore(name, out type);
        }

        // Get the type of the specified member field (name).
        public DType GetType(DName name)
        {
            AssertValid();
            Contracts.Assert(name.IsValid);

            Contracts.Verify(TryGetTypeCore(name, out var type));
            return type;
        }

        private bool TryGetTypeCore(DName name, out DType type)
        {
            AssertValid();
            Contracts.Assert(name.IsValid);

            switch (Kind)
            {
                case DKind.Record:
                case DKind.Table:
                case DKind.OptionSet:
                case DKind.View:
                    return TypeTree.TryGetValue(name, out type);

                case DKind.LazyRecord:
                case DKind.LazyTable:
                    return LazyTypeProvider.TryGetFieldType(name, out type);
                case DKind.Enum:
                    if (ValueTree.Contains(name.Value))
                    {
                        type = GetEnumSupertype();
                        return true;
                    }

                    goto default;
                default:
                    type = Invalid;
                    return false;
            }
        }

        // Get the type of a member field specified by path.
        public bool TryGetType(DPath path, out DType type)
        {
            AssertValid();

            if (path.IsRoot)
            {
                type = this;
                return true;
            }

            var fRet = TryGetType(path.Parent, out type);

            if (type.IsEnum)
            {
                type = type.GetEnumSupertype();
                return true;
            }

            return type.TryGetTypeCore(path.Name, out type);
        }

        // Get the type of a member field specified by path.
        public DType GetType(DPath path)
        {
            AssertValid();

            Contracts.Verify(TryGetType(path, out var type));
            return type;
        }

        // Return a new type based on this, with the member field (path) of a specified type.
        public DType SetType(ref bool fError, DPath path, DType type, bool skipCompare = false)
        {
            AssertValid();
            type.AssertValid();

            var fullType = this;
            if (IsLazyType)
            {
                fullType = LazyTypeProvider.GetExpandedType(IsTable);
            }

            for (; path.Length > 0; path = path.Parent)
            {
                fError |= !fullType.TryGetType(path.Parent, out var typeCur);
                if (!typeCur.IsAggregate)
                {
                    fError = true;
                    return this;
                }

                Contracts.Assert(typeCur.IsRecord || typeCur.IsTable);
                var tree = typeCur.TypeTree.SetItem(path.Name, type, skipCompare);
                type = new DType(typeCur.Kind, tree, typeCur.AssociatedDataSources, typeCur.DisplayNameProvider);

                if (typeCur.HasExpandInfo)
                {
                    type = CopyExpandInfo(type, typeCur);
                }
            }

            // Don't lose the top level entity info either.
            if (HasExpandInfo)
            {
                type = CopyExpandInfo(type, fullType);
            }

            return type;
        }

        // Return a new type based on this, with an additional named member field of a specified type.
        public DType Add(ref bool fError, DPath path, DName name, DType type)
        {
            AssertValid();
            Contracts.Assert(name.IsValid);
            type.AssertValid();

            var fullType = this;
            if (IsLazyType)
            {
                fullType = LazyTypeProvider.GetExpandedType(IsTable);
            }

            fError |= !fullType.TryGetType(path, out var typeOuter);
            if (!typeOuter.IsAggregate)
            {
                fError = true;
                return this;
            }

            Contracts.Assert(typeOuter.IsRecord || typeOuter.IsTable);

            if (typeOuter.TypeTree.TryGetValue(name, out var typeCur))
            {
                fError = true;
            }

            var tree = typeOuter.TypeTree.SetItem(name, type);
            var updatedTypeOuter = new DType(typeOuter.Kind, tree, AssociatedDataSources, typeOuter.DisplayNameProvider);

            if (typeOuter.HasExpandInfo)
            {
                updatedTypeOuter = CopyExpandInfo(updatedTypeOuter, typeOuter);
            }

            return fullType.SetType(ref fError, path, updatedTypeOuter);
        }

        // Return a new type based on this, with an additional named member field (name) of a specified type.
        public DType Add(DName name, DType type)
        {
            AssertValid();
            Contracts.Assert(IsAggregate);
            Contracts.Assert(name.IsValid);
            type.AssertValid();

            var fullType = this;
            if (IsLazyType)
            {
                fullType = LazyTypeProvider.GetExpandedType(IsTable);
            }

            Contracts.Assert(!TypeTree.Contains(name));
            var tree = TypeTree.SetItem(name, type);
            var newType = new DType(Kind, tree, AssociatedDataSources, DisplayNameProvider);

            return newType;
        }

        // Return a new type based on this, with an additional named member field of a specified type.
        public DType Add(TypedName typedName)
        {
            AssertValid();
            Contracts.Assert(IsAggregate);
            Contracts.Assert(typedName.IsValid);

            // We don't want to allow building aggregate types around deferred type.
            if (typedName.Type.IsDeferred)
            {
                throw new NotSupportedException();
            }

            return Add(typedName.Name, typedName.Type);
        }

        // Drop the specified name/field from path's type, and return the resulting type.
        public DType Drop(ref bool fError, DPath path, DName name)
        {
            AssertValid();
            Contracts.Assert(name.IsValid);

            var fullType = this;
            if (IsLazyType)
            {
                fullType = LazyTypeProvider.GetExpandedType(IsTable);
            }

            fError |= !fullType.TryGetType(path, out var typeOuter);
            if (!typeOuter.IsAggregate)
            {
                fError = true;
                return this;
            }

            Contracts.Assert(typeOuter.IsRecord || typeOuter.IsTable);

            var tree = typeOuter.TypeTree.RemoveItem(ref fError, name);
            if (fError)
            {
                return this;
            }

            return fullType.SetType(ref fError, path, new DType(typeOuter.Kind, tree, AssociatedDataSources, DisplayNameProvider));
        }

        // Drop fields of specified kind.
        public DType DropAllOfKind(ref bool fError, DPath path, DKind kind)
        {
            return DropAllMatching(ref fError, path, type => type.Kind == kind);
        }

        public DType DropAllMatching(ref bool fError, DPath path, Func<DType, bool> matchFunc)
        {
            AssertValid();
            Contracts.AssertValue(matchFunc);

            var fullType = this;
            if (IsLazyType)
            {
                fullType = LazyTypeProvider.GetExpandedType(IsTable);
            }

            fError |= !fullType.TryGetType(path, out var typeOuter);
            if (!typeOuter.IsAggregate)
            {
                fError = true;
                return this;
            }

            var tree = typeOuter.TypeTree;
            foreach (var typedName in fullType.GetNames(path))
            {
                if (matchFunc(typedName.Type))
                {
                    tree = tree.RemoveItem(ref fError, typedName.Name);
                }
            }

            if (tree == typeOuter.TypeTree)
            {
                return this;
            }

            return fullType.SetType(ref fError, path, new DType(typeOuter.Kind, tree, AssociatedDataSources, DisplayNameProvider));
        }

        public DType DropAllOfTableRelationships(ref bool fError, DPath path)
        {
            AssertValid();

            var fullType = this;
            if (IsLazyType)
            {
                fullType = LazyTypeProvider.GetExpandedType(IsTable);
            }

            fError |= !fullType.TryGetType(path, out var typeOuter);
            if (!typeOuter.IsAggregate)
            {
                fError = true;
                return this;
            }

            var tree = typeOuter.TypeTree;
            foreach (var typedName in fullType.GetNames(path))
            {
                if (typedName.Type.Kind == DKind.DataEntity && (typedName.Type.ExpandInfo?.IsTable ?? false))
                {
                    tree = tree.RemoveItem(ref fError, typedName.Name);
                }
                else if (typedName.Type.IsAggregate)
                {
                    var typeInner = typedName.Type.DropAllOfTableRelationships(ref fError, DPath.Root);
                    if (fError)
                    {
                        return fullType;
                    }

                    if (typeInner.TypeTree != tree)
                    {
                        tree = tree.SetItem(typedName.Name.ToString(), typeInner);
                    }
                }
            }

            return fullType.SetType(ref fError, path, new DType(typeOuter.Kind, tree, AssociatedDataSources, DisplayNameProvider));
        }

        public DType DropAllOfKindNested(ref bool fError, DPath path, DKind kind)
        {
            return DropAllMatchingNested(ref fError, path, type => type.Kind == kind);
        }

        // Drop fields of specified kind from all nested types
        public DType DropAllMatchingNested(ref bool fError, DPath path, Func<DType, bool> matchFunc)
        {
            AssertValid();
            Contracts.AssertValue(matchFunc);

            var fullType = this;
            if (IsLazyType)
            {
                // This probably should throw (or be eliminated)
                // It's not safe to do an unbounded recursive operation
                // on Lazy types that expands subtypes
                return DropAllMatching(ref fError, path, matchFunc);
            }

            fError |= !fullType.TryGetType(path, out var typeOuter);
            if (!typeOuter.IsAggregate)
            {
                fError = true;
                return this;
            }

            var tree = typeOuter.TypeTree;
            foreach (var typedName in fullType.GetNames(path))
            {
                if (matchFunc(typedName.Type))
                {
                    tree = tree.RemoveItem(ref fError, typedName.Name);
                }
                else if (typedName.Type.IsAggregate)
                {
                    var typeInner = typedName.Type.DropAllMatchingNested(ref fError, DPath.Root, matchFunc);
                    if (fError)
                    {
                        return this;
                    }

                    if (typeInner.TypeTree != tree)
                    {
                        tree = tree.SetItem(typedName.Name.ToString(), typeInner);
                    }
                }
            }

            return fullType.SetType(ref fError, path, new DType(typeOuter.Kind, tree, AssociatedDataSources, DisplayNameProvider));
        }

        // Drop the specified names/fields from path's type, and return the resulting type.
        // Note that if some of the names/fields were missing, we are returning a new type with
        // as many fields removed as possible (and fError == true).
        public DType DropMulti(ref bool fError, DPath path, params DName[] rgname)
        {
            AssertValid();
            Contracts.AssertNonEmpty(rgname);
            Contracts.AssertAllValid(rgname);

            fError |= !TryGetType(path, out var typeOuter);
            if (!typeOuter.IsAggregate)
            {
                fError = true;
                return this;
            }

            var fullType = this;
            if (IsLazyType)
            {
                fullType = LazyTypeProvider.GetExpandedType(IsTable);
            }

            Contracts.Assert(typeOuter.IsRecord || typeOuter.IsTable);

            var tree = typeOuter.TypeTree.RemoveItems(ref fError, rgname);

            return SetType(ref fError, path, new DType(typeOuter.Kind, tree, AssociatedDataSources, DisplayNameProvider));
        }

        public bool ContainsKindNested(DPath path, DKind kind)
        {
            AssertValid();
            Contracts.Assert(kind >= DKind._Min && kind < DKind._Lim);

            TryGetType(path, out var typeOuter);
            if (!typeOuter.IsAggregate)
            {
                return typeOuter.Kind == kind;
            }

            var tree = typeOuter.TypeTree;
            foreach (var typedName in GetNames(path))
            {
                if (typedName.Type.Kind == kind)
                {
                    return true;
                }
                else if (typedName.Type.IsAggregate && !IsLazyType)
                {
                    var containsInner = typedName.Type.ContainsKindNested(DPath.Root, kind);
                    if (containsInner)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Get ALL the fields/names at the specified path, including hidden meta fields
        // and other special fields.
        public IEnumerable<TypedName> GetAllNames(DPath path)
        {
            AssertValid();

            var fError = false;
            return GetAllNames(ref fError, path);
        }

        public IEnumerable<TypedName> GetNames(DPath path)
        {
            return GetAllNames(path).Where(kvp => kvp.Name != MetaFieldName);
        }

        public IEnumerable<DName> GetRootFieldNames()
        {
            if (IsLazyType)
            {
                return LazyTypeProvider.FieldNames;
            }

            return GetAllNames(DPath.Root).Where(kvp => kvp.Name != MetaFieldName).Select(kvp => kvp.Name);
        }

        /// <summary>
        /// Returns true if type contains a entity type.
        /// </summary>
        public bool ContainsDataEntityType(DPath path)
        {
            AssertValid();
            Contracts.AssertValid(path);

            return GetNames(path).Any(n => n.Type.IsExpandEntity ||
                (n.Type.IsAggregate && n.Type.ContainsDataEntityType(DPath.Root)));
        }

        /// <summary>
        /// Returns true if type contains an attachment type.
        /// </summary>
        public bool ContainsAttachmentType(DPath path)
        {
            AssertValid();
            Contracts.AssertValid(path);

            return GetNames(path).Any(n => n.Type.IsAttachment ||
                (n.Type.IsAggregate && n.Type.ContainsAttachmentType(DPath.Root)));
        }

        /// <summary>
        /// Returns true if type contains an OptionSet type.
        /// </summary>
        /// <returns></returns>
        public bool IsMultiSelectOptionSet()
        {
            if (TypeTree.Count != 1)
            {
                return false;
            }

            var columnType = TypeTree.GetPairs().First();
            return columnType.Value.Kind == DKind.OptionSetValue;
        }

        // Get the fields/names at the specified path.
        private IEnumerable<TypedName> GetAllNames(ref bool fError, DPath path)
        {
            AssertValid();

            fError |= !TryGetType(path, out var type);

            if (!type.IsAggregate && !type.IsEnum)
            {
                fError = true;
            }

            switch (type.Kind)
            {
                case DKind.Record:
                case DKind.Table:
                case DKind.OptionSet:
                case DKind.View:
                case DKind.File:
                case DKind.LargeImage:
                    return type.TypeTree.GetPairs().Select(kvp => new TypedName(kvp.Value, new DName(kvp.Key)));
                case DKind.Enum:
                    var supertype = new DType(type.EnumSuperkind);
                    return type.ValueTree.GetPairs().Select(kvp => new TypedName(supertype, new DName(kvp.Key)));
                case DKind.LazyRecord:
                case DKind.LazyTable:
                    return type.GetRootFieldNames().Select(name => new TypedName(type.GetType(name), name));
                default:
                    return Enumerable.Empty<TypedName>();
            }
        }

        internal bool TryGetExpandedEntityType(out DType type)
        {
            if (!TryGetEntityDelegationMetadata(out var metadata))
            {
                type = Unknown;
                return false;
            }

            type = ExpandEntityType(metadata.Schema, metadata.Schema.AssociatedDataSources);
            return true;
        }

        // For patching entities, we expand the type and drop entities and attachments for the purpose of comparison.
        // This allows entity types to be compared against values from Set/Collect/UpdateContext,
        // As those functions drop collections/attachments from the type, but at runtime do not change the data
        // The risk here is that the user could attempt to construct a record that matches the entity type but does not exit in the other table
        // This will cause an runtime error instead of being caught by our type system
        internal bool TryGetExpandedEntityTypeWithoutDataSourceSpecificColumns(out DType type)
        {
            if (!TryGetExpandedEntityType(out type))
            {
                return false;
            }

            var fValid = true;
            if (type.ContainsDataEntityType(DPath.Root))
            {
                var fError = false;
                type = type.DropAllOfKindNested(ref fError, DPath.Root, DKind.DataEntity);
                fValid &= !fError;
            }

            if (!fValid)
            {
                type = Unknown;
            }

            return fValid;
        }

        /// <summary>
        /// Covers Lazy.Accepts(other) scenarios.
        /// </summary>
        private bool LazyTypeAccepts(DType other, bool exact)
        {
            Contracts.AssertValid(other);
            Contracts.Assert(IsLazyType);

            switch (other.Kind)
            {
                case DKind.LazyRecord:
                case DKind.LazyTable:
                    Contracts.AssertValue(LazyTypeProvider);
                    return other.LazyTypeProvider.BackingFormulaType.Equals(LazyTypeProvider.BackingFormulaType) && IsTable == other.IsTable;
                case DKind.Record:
                case DKind.Table:
                    return LazyTypeProvider.GetExpandedType(IsTable).Accepts(other, exact);
                default:
                    return other.Kind == DKind.Unknown || other.Kind == DKind.Deferred;
            }
        }

        /// <summary>
        /// Covers Known.Accepts(Lazy) scenarios.
        /// </summary>
        private bool AcceptsLazyType(DType lazy, bool exact)
        {
            Contracts.Assert(IsAggregate);
            Contracts.Assert(lazy.IsLazyType);

            if (IsTable != lazy.IsTable)
            {
                return false;
            }

            foreach (var namedType in GetNames(DPath.Root))
            {
                if (!lazy.TryGetType(namedType.Name, out var otherField) || !namedType.Type.Accepts(otherField, exact))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AcceptsEntityType(DType type)
        {
            Contracts.AssertValid(type);
            Contracts.Assert(Kind == DKind.DataEntity);

            switch (type.Kind)
            {
                case DKind.DataEntity:
                    Contracts.AssertValue(ExpandInfo);
                    return type.ExpandInfo.Identity == ExpandInfo.Identity;
                case DKind.LazyRecord:
                case DKind.LazyTable:
                case DKind.Table:
                case DKind.Record:
                    if (type.ExpandInfo != null && type.ExpandInfo.Identity != ExpandInfo.Identity)
                    {
                        return false;
                    }

                    DType expandedEntityType;
                    if (!TryGetExpandedEntityType(out expandedEntityType))
                    {
                        return false;
                    }

                    return expandedEntityType.Accepts(type, true);
                default:
                    return type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
            }
        }

        /// <summary>
        /// Returns whether this type can accept a value of "type".
        /// For example, a table type can accept a table type containing extra fields.
        /// <br/> - type1.Accepts(type2) is the same as asking whether type2==type1 or type2 is a sub-type of type1.
        /// <br/> - Error accepts any type.
        /// <br/> - Any type accepts Unknown.
        /// <br/> If not in 'exact' mode (i.e. if exact=false), we permit downcasting as well; for
        /// example a table type will accept a table with less fields.
        /// </summary>
        /// <param name="type">
        /// Type of questionable acceptance.
        /// </param>
        /// <param name="exact">
        /// Whether or not <see cref="DType"/>'s absense of columns that are defined in <paramref name="type"/>
        /// should affect acceptance.
        /// </param>
        /// <param name="useLegacyDateTimeAccepts"></param>
        /// <returns>
        /// True if <see cref="DType"/> accepts <paramref name="type"/>, false otherwise.
        /// </returns>
        public bool Accepts(DType type, bool exact = true, bool useLegacyDateTimeAccepts = false)
        {
            return Accepts(type, out _, out _, exact, useLegacyDateTimeAccepts);
        }

        /// <summary>
        /// Returns whether this type can accept a value of "type".
        /// For example, a table type can accept a table type containing extra fields.
        /// <br/> - type1.Accepts(type2) is the same as asking whether type2==type1 or type2 is a sub-type of type1.
        /// <br/> - Error accepts any type.
        /// <br/> - Any type accepts Unknown.
        /// <br/> If not in 'exact' mode (i.e. if exact=false), we permit downcasting as well; for
        /// example a table type will accept a table with less fields.
        /// </summary>
        /// <param name="type">
        /// Type of questionable acceptance.
        /// </param>
        /// <param name="schemaDifference">
        /// Holds the expected type of a type mismatch as well as a field name if the mismatch is aggregate.
        /// If the mismatch is top level, the key of this kvp will be set to null.
        /// </param>
        /// <param name="schemaDifferenceType">
        /// Holds the actual type of a type mismatch.
        /// </param>
        /// <param name="exact">
        /// Whether or not <see cref="DType"/>'s absense of columns that are defined in <paramref name="type"/>
        /// should affect acceptance.
        /// </param>
        /// <param name="useLegacyDateTimeAccepts"></param>
        /// <returns>
        /// True if <see cref="DType"/> accepts <paramref name="type"/>, false otherwise.
        /// </returns>
        public virtual bool Accepts(DType type, out KeyValuePair<string, DType> schemaDifference, out DType schemaDifferenceType, bool exact = true, bool useLegacyDateTimeAccepts = false)
        {
            AssertValid();
            type.AssertValid();

            schemaDifference = new KeyValuePair<string, DType>(null, Invalid);
            schemaDifferenceType = Invalid;

            // We accept ObjNull as any DType (but subtypes can override).
            if (type.Kind == DKind.ObjNull)
            {
                return true;
            }

            bool DefaultReturnValue(DType targetType) =>
                    targetType.Kind == Kind ||
                    targetType.Kind == DKind.Unknown ||
                    targetType.Kind == DKind.Deferred ||
                    (targetType.Kind == DKind.Enum && Accepts(targetType.GetEnumSupertype()));

            bool accepts;
            switch (Kind)
            {
                case DKind.Error:
                    accepts = true;
                    break;

                case DKind.Polymorphic:
                    accepts = type.Kind == DKind.Polymorphic || type.Kind == DKind.Record || type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;

                case DKind.Record:
                case DKind.File:
                case DKind.LargeImage:
                    if (type.IsLazyType)
                    {
                        return AcceptsLazyType(type, exact);
                    }

                    if (Kind == type.Kind)
                    {
                        return TreeAccepts(this, TypeTree, type.TypeTree, out schemaDifference, out schemaDifferenceType, exact, useLegacyDateTimeAccepts);
                    }

                    accepts = type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;

                case DKind.Table:
                    if (type.IsLazyType)
                    {
                        return AcceptsLazyType(type, exact);
                    }

                    if (Kind == type.Kind || type.IsExpandEntity)
                    {
                        return TreeAccepts(this, TypeTree, type.TypeTree, out schemaDifference, out schemaDifferenceType, exact, useLegacyDateTimeAccepts);
                    }

                    accepts = (IsMultiSelectOptionSet() && TypeTree.GetPairs().First().Value.OptionSetInfo == type.OptionSetInfo) || type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;

                case DKind.Enum:
                    accepts = (Kind != type.Kind && (type.Kind == DKind.Unknown || type.Kind == DKind.Deferred)) ||
                              (EnumSuperkind == type.EnumSuperkind && EnumTreeAccepts(ValueTree, type.ValueTree, exact));
                    break;

                case DKind.Unknown:
                    accepts = type.Kind == DKind.Unknown;
                    break;
                case DKind.Deferred:
                    accepts = type.Kind == DKind.Deferred || type.Kind == DKind.Unknown;
                    break;
                case DKind.String:
                    accepts =
                        type.Kind == Kind ||
                        type.Kind == DKind.Hyperlink ||
                        type.Kind == DKind.Image ||
                        type.Kind == DKind.PenImage ||
                        type.Kind == DKind.Media ||
                        type.Kind == DKind.Blob ||
                        type.Kind == DKind.Unknown ||
                        type.Kind == DKind.Deferred ||
                        type.Kind == DKind.Guid ||
                        (type.Kind == DKind.Enum && Accepts(type.GetEnumSupertype()));
                    break;

                case DKind.Number:
                    accepts =
                        type.Kind == Kind ||
                        type.Kind == DKind.Currency ||
                        type.Kind == DKind.Unknown ||
                        type.Kind == DKind.Deferred ||
                        (useLegacyDateTimeAccepts &&
                            (type.Kind == DKind.DateTime ||
                            type.Kind == DKind.Date ||
                            type.Kind == DKind.Time ||
                            type.Kind == DKind.DateTimeNoTimeZone)) ||
                        (type.Kind == DKind.Enum && Accepts(type.GetEnumSupertype()));
                    break;

                case DKind.Color:
                case DKind.Boolean:
                case DKind.PenImage:
                case DKind.ObjNull:
                case DKind.Guid:
                    accepts = DefaultReturnValue(type);
                    break;

                case DKind.Hyperlink:
                    accepts = (!exact && type.Kind == DKind.String) ||
                              type.Kind == DKind.Media ||
                              type.Kind == DKind.Blob ||
                              type.Kind == DKind.Image ||
                              type.Kind == DKind.PenImage ||
                              DefaultReturnValue(type);

                    break;
                case DKind.Image:
                    accepts =
                        type.Kind == DKind.PenImage || type.Kind == DKind.Blob || (!exact && (type.Kind == DKind.String || type.Kind == DKind.Hyperlink)) || DefaultReturnValue(type);
                    break;
                case DKind.Media:
                    accepts =
                        type.Kind == DKind.Blob || (!exact && (type.Kind == DKind.String || type.Kind == DKind.Hyperlink)) || DefaultReturnValue(type);
                    break;
                case DKind.Blob:
                    accepts = (!exact && (type.Kind == DKind.String || type.Kind == DKind.Hyperlink)) || DefaultReturnValue(type);
                    break;
                case DKind.Currency:
                    accepts = (!exact && type.Kind == DKind.Number) || DefaultReturnValue(type);
                    break;
                case DKind.DateTime:
                    accepts = (type.Kind == DKind.Date || type.Kind == DKind.Time || type.Kind == DKind.DateTimeNoTimeZone || (useLegacyDateTimeAccepts && !exact && type.Kind == DKind.Number)) || DefaultReturnValue(type);
                    break;
                case DKind.DateTimeNoTimeZone:
                    accepts = (type.Kind == DKind.Date || type.Kind == DKind.Time || (useLegacyDateTimeAccepts && !exact && type.Kind == DKind.Number)) ||
                              DefaultReturnValue(type);
                    break;
                case DKind.Date:
                case DKind.Time:
                    accepts = (!exact && (type.Kind == DKind.DateTime || type.Kind == DKind.DateTimeNoTimeZone || (useLegacyDateTimeAccepts && type.Kind == DKind.Number))) ||
                              DefaultReturnValue(type);
                    break;
                case DKind.Control:
                    throw new NotImplementedException("This should be overriden");
                case DKind.DataEntity:
                    accepts = AcceptsEntityType(type);
                    break;
                case DKind.Metadata:
                    accepts = (type.Kind == Kind &&
                                type.Metadata.Name == Metadata.Name &&
                                type.Metadata.Type == Metadata.Type &&
                                type.Metadata.ParentTableMetadata.Name == Metadata.ParentTableMetadata.Name) || type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;
                case DKind.OptionSet:
                case DKind.OptionSetValue:
                    accepts = (type.Kind == Kind &&
                                (OptionSetInfo == null || type.OptionSetInfo == null || type.OptionSetInfo.Equals(OptionSetInfo))) ||
                               type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;
                case DKind.View:
                case DKind.ViewValue:
                    accepts = (type.Kind == Kind &&
                                (ViewInfo == null || type.ViewInfo == null || type.ViewInfo == ViewInfo)) ||
                               type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;

                case DKind.NamedValue:
                    accepts = (type.Kind == Kind && NamedValueKind == type.NamedValueKind) ||
                              type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;
                case DKind.UntypedObject:
                    accepts = type.Kind == DKind.UntypedObject || type.Kind == DKind.Unknown || type.Kind == DKind.Deferred;
                    break;

                case DKind.LazyTable:
                case DKind.LazyRecord:
                    accepts = LazyTypeAccepts(type, exact);
                    break;
                default:
                    Contracts.Assert(false);
                    accepts = DefaultReturnValue(type);
                    break;
            }

            // If the type is accepted we consider the difference invalid.  Otherwise, the type difference is the
            // unaccepting type
            var typeDifference = accepts ? this : Invalid;
            schemaDifference = new KeyValuePair<string, DType>(null, typeDifference);

            return accepts;
        }

        // Implements Accepts for Record and Table types.
        private static bool TreeAccepts(DType parentType, TypeTree treeDst, TypeTree treeSrc, out KeyValuePair<string, DType> schemaDifference, out DType treeSrcSchemaDifferenceType, bool exact = true, bool useLegacyDateTimeAccepts = false)
        {
            treeDst.AssertValid();
            treeSrc.AssertValid();

            schemaDifference = new KeyValuePair<string, DType>(null, Invalid);
            treeSrcSchemaDifferenceType = Invalid;

            if (treeDst == treeSrc)
            {
                return true;
            }

            foreach (var pairDst in treeDst.GetPairs())
            {
                Contracts.Assert(pairDst.Value.IsValid);

                // If a field has type Error, it doesn't matter whether treeSrc
                // has the same field.
                if (pairDst.Value.IsError)
                {
                    continue;
                }

                if (!treeSrc.TryGetValue(pairDst.Key, out var type))
                {
                    if (!exact || parentType.AreFieldsOptional)
                    {
                        continue;
                    }

                    if (!TryGetDisplayNameForColumn(parentType, pairDst.Key, out var colName))
                    {
                        colName = pairDst.Key;
                    }

                    schemaDifference = new KeyValuePair<string, DType>(colName, pairDst.Value);
                    return false;
                }

                if (!pairDst.Value.Accepts(type, out var recursiveSchemaDifference, out var recursiveSchemaDifferenceType, exact, useLegacyDateTimeAccepts))
                {
                    if (!TryGetDisplayNameForColumn(parentType, pairDst.Key, out var colName))
                    {
                        colName = pairDst.Key;
                    }

                    if (!string.IsNullOrEmpty(recursiveSchemaDifference.Key))
                    {
                        schemaDifference = new KeyValuePair<string, DType>(colName + TexlLexer.PunctuatorDot + recursiveSchemaDifference.Key, recursiveSchemaDifference.Value);
                        treeSrcSchemaDifferenceType = recursiveSchemaDifferenceType;
                    }
                    else
                    {
                        schemaDifference = new KeyValuePair<string, DType>(colName, pairDst.Value);
                        treeSrcSchemaDifferenceType = type;
                    }

                    return false;
                }
            }

            return true;
        }

        // Implements Accepts for Enum types.
        private static bool EnumTreeAccepts(ValueTree treeDst, ValueTree treeSrc, bool exact = true)
        {
            treeDst.AssertValid();
            treeSrc.AssertValid();

            if (treeDst == treeSrc)
            {
                return true;
            }

            foreach (var pairSrc in treeSrc.GetPairs())
            {
                Contracts.AssertValue(pairSrc.Value.Object);

                if (!treeDst.TryGetValue(pairSrc.Key, out var obj))
                {
                    if (exact)
                    {
                        return false;
                    }

                    continue;
                }

                if (pairSrc.Value != obj)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSuperKind(DKind baseKind, DKind kind)
        {
            Contracts.Assert(baseKind >= DKind._Min && baseKind < DKind._Lim);
            Contracts.Assert(kind >= DKind._Min && kind < DKind._Lim);

            if (baseKind == kind)
            {
                return false;
            }

            if (baseKind == DKind.Error)
            {
                return true;
            }

            if (kind == DKind.Error)
            {
                return false;
            }

            DKind kind2Superkind;

            while (KindToSuperkindMapping.TryGetValue(kind, out kind2Superkind) && kind2Superkind != baseKind)
            {
                kind = kind2Superkind;
            }

            return kind2Superkind == baseKind;
        }

        // Some types require explicit conversion to their parents/children types.
        // This method returns true if this type requires such explicit conversion.
        // This method assumes that this type and the destination type are in the
        // same path in the type hierarchy (one is the ancestor of the other).
        public bool RequiresExplicitCast(DType destType)
        {
            Contracts.Assert(destType.IsValid);
            Contracts.Assert(destType.Accepts(this, exact: false));

            switch (destType.Kind)
            {
                case DKind.String:
                case DKind.Hyperlink:
                    if (Kind == DKind.Blob || Kind == DKind.Image || Kind == DKind.Media)
                    {
                        return true;
                    }

                    break;
                default:
                    break;
            }

            return false;
        }

        // Produces the least common supertype of the two specified types.
        public static DType Supertype(DType type1, DType type2, bool useLegacyDateTimeAccepts = false)
        {
            type1.AssertValid();
            type2.AssertValid();

            if (type1.IsAggregate && type2.IsAggregate && !(type1.IsLazyType || type2.IsLazyType))
            {
                if (type1.Kind != type2.Kind)
                {
                    return Error;
                }

                return SupertypeAggregateCore(type1, type2, useLegacyDateTimeAccepts);
            }

            return SupertypeCore(type1, type2, useLegacyDateTimeAccepts);
        }

        private static DType SupertypeCore(DType type1, DType type2, bool useLegacyDateTimeAccepts)
        {
            type1.AssertValid();
            type2.AssertValid();

            if (type1.Accepts(type2, useLegacyDateTimeAccepts: useLegacyDateTimeAccepts))
            {
                return CreateDTypeWithConnectedDataSourceInfoMetadata(type1, type2.AssociatedDataSources, type2.DisplayNameProvider);
            }

            if (type2.Accepts(type1, useLegacyDateTimeAccepts: useLegacyDateTimeAccepts))
            {
                return CreateDTypeWithConnectedDataSourceInfoMetadata(type2, type1.AssociatedDataSources, type1.DisplayNameProvider);
            }

            if (!KindToSuperkindMapping.TryGetValue(type1.Kind, out var type1Superkind) || type1Superkind == DKind.Error)
            {
                return Error;
            }

            while (!IsSuperKind(type1Superkind, type2.Kind))
            {
                if (!KindToSuperkindMapping.TryGetValue(type1Superkind, out type1Superkind) || type1Superkind == DKind.Error)
                {
                    return Error;
                }
            }

            Contracts.Assert(type1Superkind != DKind.Enum && type1Superkind >= DKind._MinPrimitive && type1Superkind < DKind._LimPrimitive);

            var type = new DType(type1Superkind);

            foreach (var cds in UnionDataSourceInfoMetadata(type1, type2))
            {
                type = AttachDataSourceInfo(type, cds);
            }

            return type;
        }

        private static DType SupertypeAggregateCore(DType type1, DType type2, bool useLegacyDateTimeAccepts)
        {
            type1.AssertValid();
            type2.AssertValid();
            Contracts.Assert(type1.IsAggregate);
            Contracts.Assert(type2.IsAggregate);
            Contracts.Assert(type1.Kind == type2.Kind);

            if (type1.ChildCount > type2.ChildCount)
            {
                CollectionUtils.Swap(ref type1, ref type2);
            }

            var fError = false;
            var treeRes = type1.TypeTree;
            using (var ator1 = type1.TypeTree.GetPairs().GetEnumerator())
            using (var ator2 = type2.TypeTree.GetPairs().GetEnumerator())
            {
                var fAtor1 = ator1.MoveNext();
                var fAtor2 = ator2.MoveNext();

                while (fAtor1 && fAtor2)
                {
                    var cmp = RedBlackNode<DType>.Compare(ator1.Current.Key, ator2.Current.Key);
                    if (cmp == 0)
                    {
                        var innerType = Supertype(ator1.Current.Value, ator2.Current.Value, useLegacyDateTimeAccepts);
                        if (innerType.IsError)
                        {
                            treeRes = treeRes.RemoveItem(ref fError, ator1.Current.Key);
                        }
                        else if (innerType != ator1.Current.Value)
                        {
                            treeRes = treeRes.SetItem(ator1.Current.Key, innerType);
                        }

                        fAtor1 = ator1.MoveNext();
                        fAtor2 = ator2.MoveNext();
                    }
                    else if (cmp < 0)
                    {
                        treeRes = treeRes.RemoveItem(ref fError, ator1.Current.Key);
                        fAtor1 = ator1.MoveNext();
                    }
                    else
                    {
                        fAtor2 = ator2.MoveNext();
                    }
                }

                // If we still have fields in ator1, they need to be removed from treeRes.
                while (fAtor1)
                {
                    treeRes = treeRes.RemoveItem(ref fError, ator1.Current.Key);
                    fAtor1 = ator1.MoveNext();
                }
            }

            Contracts.Assert(!fError);
            var returnType = new DType(type1.Kind, treeRes, UnionDataSourceInfoMetadata(type1, type2), type1.DisplayNameProvider);

            returnType = type2.DisplayNameProvider == null ?
                returnType :
                AttachOrDisableDisplayNameProvider(returnType, type2.DisplayNameProvider);
            return returnType;
        }

        // Produces the union of the two given types.
        // For primitive types, this is the same as the least common supertype.
        // For aggregates, the union is a common subtype that includes fields from both types, assuming no errors.
        public static DType Union(DType type1, DType type2, bool useLegacyDateTimeAccepts = false)
        {
            var fError = false;
            return Union(ref fError, type1, type2, useLegacyDateTimeAccepts);
        }

        public bool CanUnionWith(DType type, bool useLegacyDateTimeAccepts = false)
        {
            AssertValid();
            type.AssertValid();

            var fError = false;
            Union(ref fError, this, type, useLegacyDateTimeAccepts);

            return !fError;
        }

        private static HashSet<IExternalTabularDataSource> UnionDataSourceInfoMetadata(DType type1, DType type2)
        {
            type1.AssertValid();
            type2.AssertValid();

            if (type2.AssociatedDataSources == null && type1.AssociatedDataSources == null)
            {
                return new HashSet<IExternalTabularDataSource>();
            }

            if (type2.AssociatedDataSources == null)
            {
                return type1.AssociatedDataSources;
            }

            if (type1.AssociatedDataSources == null)
            {
                return type2.AssociatedDataSources;
            }

            var set = type1.AssociatedDataSources.Union(type2.AssociatedDataSources);
            return new HashSet<IExternalTabularDataSource>(set);
        }

        private static bool TryGetEntityMetadataForDisplayNames(DType type, out IDataEntityMetadata metadata)
        {
            if (!type.HasExpandInfo)
            {
                metadata = null;
                return false;
            }

            Contracts.AssertValue(type.ExpandInfo.ParentDataSource);
            Contracts.AssertValue(type.ExpandInfo.ParentDataSource.DataEntityMetadataProvider);

            var metadataProvider = type.ExpandInfo.ParentDataSource.DataEntityMetadataProvider;
            if (!metadataProvider.TryGetEntityMetadata(type.ExpandInfo.Identity, out metadata))
            {
                metadata = null;
                return false;
            }

            return true;
        }

        internal static DType CreateDTypeWithConnectedDataSourceInfoMetadata(DType type, HashSet<IExternalTabularDataSource> connectedDataSourceInfoSet, DisplayNameProvider displayNameProvider)
        {
            type.AssertValid();
            Contracts.AssertValueOrNull(connectedDataSourceInfoSet);

            var returnType = type;
            foreach (var cds in connectedDataSourceInfoSet ?? Enumerable.Empty<IExternalTabularDataSource>())
            {
                returnType = AttachDataSourceInfo(returnType, cds);
            }

            if (displayNameProvider != null)
            {
                returnType = AttachOrDisableDisplayNameProvider(returnType, displayNameProvider);
            }

            return returnType;
        }

        internal static bool TryGetDisplayNameForColumn(DType type, string logicalName, out string displayName)
        {
            // If we are accessing an entity, then the entity info contains the mapping
            if (TryGetEntityMetadataForDisplayNames(type, out var entityMetadata))
            {
                if (entityMetadata.DisplayNameMapping.TryGetFromFirst(logicalName, out displayName))
                {
                    return true;
                }

                return false;
            }

            // If there are multiple data sources associated with the type, we may have name conflicts
            // In that case, we block the use of display names from the type
            if (type != null && type.AssociatedDataSources != null && type.AssociatedDataSources.Count == 1)
            {
                var dataSourceInfo = type.AssociatedDataSources.FirstOrDefault();
                if (dataSourceInfo != null && dataSourceInfo.DisplayNameMapping.TryGetFromFirst(logicalName, out displayName))
                {
                    return true;
                }
            }

            // Use the DisplayNameProvider here
            if (type != null && type.DisplayNameProvider != null)
            {
                if (type.DisplayNameProvider.TryGetDisplayName(new DName(logicalName), out var displayDName))
                {
                    displayName = displayDName.Value;
                    return true;
                }
            }

            displayName = null;
            return false;
        }

        internal static bool TryGetLogicalNameForColumn(DType type, string displayName, out string logicalName, bool isThisItem = false)
        {
            // If we are accessing an entity, then the entity info contains the mapping
            if (TryGetEntityMetadataForDisplayNames(type, out var entityMetadata))
            {
                if (entityMetadata.DisplayNameMapping.TryGetFromSecond(displayName, out logicalName))
                {
                    return true;
                }

                return false;
            }

            // If there are multiple data sources associated with the type, we may have name conflicts
            // In that case, we block the use of display names from the type
            if (type != null && type.AssociatedDataSources != null && type.AssociatedDataSources.Count == 1)
            {
                var dataSourceInfo = type.AssociatedDataSources.FirstOrDefault();
                if (dataSourceInfo != null && dataSourceInfo.DisplayNameMapping.TryGetFromSecond(displayName, out logicalName))
                {
                    return true;
                }
            }

            // Use the DisplayNameProvider here
            if (type != null && type.DisplayNameProvider != null && !isThisItem)
            {
                if (type.DisplayNameProvider.TryGetLogicalName(new DName(displayName), out var logicalDName))
                {
                    logicalName = logicalDName.Value;
                    return true;
                }
            }

            logicalName = null;
            return false;
        }

        /// <summary>
        /// This API is very specific for Canvas. Don't call it unless you know exactly what you're doing. 
        /// Returns true iff <paramref name="displayName"/> was found within <paramref name="type"/>'s old display
        /// name mapping and sets <paramref name="logicalName"/> and <paramref name="newDisplayName"/>
        /// according to the new mapping.
        /// </summary>
        /// <param name="type">
        /// Type the mapping within which to search for old display name and from which to produce new
        /// display name.
        /// </param>
        /// <param name="displayName">
        /// Display name used to search.
        /// </param>
        /// <param name="logicalName">
        /// Will be set to <paramref name="displayName"/>'s corresponding logical name if
        /// <paramref name="displayName"/> exists within <paramref name="type"/>'s old mapping.
        /// </param>
        /// <param name="newDisplayName">
        /// Will be set to <paramref name="logicalName"/>'s new display name if
        /// <paramref name="displayName"/> exists within <paramref name="type"/>'s old mapping.
        /// </param>
        /// <returns>
        /// Whether <paramref name="displayName"/> exists within <paramref name="type"/>'s previous display name map.
        /// </returns>
        internal static bool TryGetConvertedDisplayNameAndLogicalNameForColumn(DType type, string displayName, out string logicalName, out string newDisplayName)
        {
            // If we are accessing an entity, then the entity info contains the mapping
            if (TryGetEntityMetadataForDisplayNames(type, out var entityMetadata))
            {
                if (entityMetadata.IsConvertingDisplayNameMapping &&
                    entityMetadata.PreviousDisplayNameMapping != null &&
                    entityMetadata.PreviousDisplayNameMapping.TryGetFromSecond(displayName, out logicalName))
                {
                    if (entityMetadata.DisplayNameMapping.TryGetFromFirst(logicalName, out newDisplayName))
                    {
                        return true;
                    }

                    // Converting and no new mapping exists for this column, so the display name is also the logical name
                    newDisplayName = logicalName;
                    return true;
                }

                logicalName = null;
                newDisplayName = null;
                return false;
            }

            // If there are multiple data sources associated with the type, we may have name conflicts
            // In that case, we block the use of display names from the type
            if (type != null && type.AssociatedDataSources != null && type.AssociatedDataSources.Count == 1)
            {
                var dataSourceInfo = type.AssociatedDataSources.FirstOrDefault();
                if (dataSourceInfo != null &&
                    dataSourceInfo.IsConvertingDisplayNameMapping &&
                    dataSourceInfo.PreviousDisplayNameMapping != null &&
                    dataSourceInfo.PreviousDisplayNameMapping.TryGetFromSecond(displayName, out logicalName))
                {
                    if (dataSourceInfo.DisplayNameMapping.TryGetFromFirst(logicalName, out newDisplayName))
                    {
                        return true;
                    }

                    // Converting and no new mapping exists for this column, so the display name is also the logical name
                    newDisplayName = logicalName;
                    return true;
                }
            }

            if (type != null && type.DisplayNameProvider != null)
            {
                if (type.DisplayNameProvider.TryRemapLogicalAndDisplayNames(new DName(displayName), out var logicalDName, out var newDisplayDName))
                {
                    logicalName = logicalDName.Value;
                    newDisplayName = newDisplayDName;
                    return true;
                }
            }

            logicalName = null;
            newDisplayName = null;
            return false;
        }

        public static DType Union(ref bool fError, DType type1, DType type2, bool useLegacyDateTimeAccepts = false)
        {
            type1.AssertValid();
            type2.AssertValid();

            // For Lazy Types, union operations must expand the current depth
            if (type1.IsLazyType)
            {
                if (type1 == type2)
                {
                    return type1;
                }

                type1 = type1.LazyTypeProvider.GetExpandedType(type1.IsTable);
            }

            if (type2.IsLazyType)
            {
                type2 = type2.LazyTypeProvider.GetExpandedType(type2.IsTable);
            }

            if (type1.IsAggregate && type2.IsAggregate)
            {
                if (type1 == ObjNull)
                {
                    return CreateDTypeWithConnectedDataSourceInfoMetadata(type2, type1.AssociatedDataSources, type1.DisplayNameProvider);
                }

                if (type2 == ObjNull)
                {
                    return CreateDTypeWithConnectedDataSourceInfoMetadata(type1, type2.AssociatedDataSources, type2.DisplayNameProvider);
                }

                if (type1.Kind != type2.Kind)
                {
                    fError = true;
                    return Error;
                }

                return CreateDTypeWithConnectedDataSourceInfoMetadata(UnionCore(ref fError, type1, type2, useLegacyDateTimeAccepts), type2.AssociatedDataSources, type2.DisplayNameProvider);
            }

            if (type1.Accepts(type2, useLegacyDateTimeAccepts: useLegacyDateTimeAccepts))
            {
                fError |= type1.IsError;
                return CreateDTypeWithConnectedDataSourceInfoMetadata(type1, type2.AssociatedDataSources, type2.DisplayNameProvider);
            }

            if (type2.Accepts(type1, useLegacyDateTimeAccepts: useLegacyDateTimeAccepts))
            {
                fError |= type2.IsError;
                return CreateDTypeWithConnectedDataSourceInfoMetadata(type2, type1.AssociatedDataSources, type1.DisplayNameProvider);
            }

            var result = Supertype(type1, type2, useLegacyDateTimeAccepts);
            fError = result == Error;
            return result;
        }

        private static DType UnionCore(ref bool fError, DType type1, DType type2, bool useLegacyDateTimeAccepts = false)
        {
            type1.AssertValid();
            Contracts.Assert(type1.IsAggregate);
            type2.AssertValid();
            Contracts.Assert(type2.IsAggregate);

            var result = type1;

            foreach (var pair in type2.GetNames(DPath.Root))
            {
                var field2Name = pair.Name;

                if (!type1.TryGetType(field2Name, out var field1Type))
                {
                    result = result.Add(pair);
                    continue;
                }

                var field2Type = pair.Type;
                if (field1Type == field2Type)
                {
                    continue;
                }

                DType fieldType;
                if (field1Type == ObjNull || field2Type == ObjNull)
                {
                    fieldType = field1Type == ObjNull ? field2Type : field1Type;
                }
                else if (field1Type.IsAggregate && field2Type.IsAggregate)
                {
                    fieldType = Union(ref fError, field1Type, field2Type, useLegacyDateTimeAccepts);
                }
                else if (field1Type.IsAggregate || field2Type.IsAggregate)
                {
                    var isMatchingExpandType = false;
                    var expandType = Unknown;
                    if (field1Type.HasExpandInfo && field2Type.IsAggregate)
                    {
                        isMatchingExpandType = IsMatchingExpandType(field1Type, field2Type);
                        expandType = field1Type;
                    }
                    else if (field2Type.HasExpandInfo && field1Type.IsAggregate)
                    {
                        isMatchingExpandType = IsMatchingExpandType(field2Type, field1Type);
                        expandType = field2Type;
                    }

                    if (!isMatchingExpandType)
                    {
                        fieldType = Error;
                        fError = true;
                    }
                    else
                    {
                        fieldType = expandType;
                    }
                }
                else
                {
                    fieldType = Union(ref fError, field1Type, field2Type, useLegacyDateTimeAccepts);
                }

                result = result.SetType(ref fError, DPath.Root.Append(field2Name), fieldType);
            }

            return result;
        }

        /// <summary>
        /// Checks whether actualColumnType in table is matching related entity type in expectedColumnType.
        /// E.g. Collection definition rule => Collect(newCollection, Accounts);
        /// Above rule will define new collection with schema from Accounts datasource
        /// Adding new rule that populates collection with lookup data like below
        /// Collection populatin rule => Collect(newCollection, {'Primary Contact':First(Contacts)});
        /// Above rule is collecting new data record with data in lookup fields.
        /// </summary>
        /// <param name="expectedColumnType">expected type in collection definition for 'Primary Contact' column is entity of expand type.</param>
        /// <param name="actualColumnType">actual column provide in data collection rule is record of entity matching expand type.</param>
        /// <returns>true if actual column type is Entity type matching Expand entity, false O.W.</returns>
        internal static bool IsMatchingExpandType(DType expectedColumnType, DType actualColumnType)
        {
            expectedColumnType.AssertValid();
            actualColumnType.AssertValid();

            var isExpectedType = false;
            foreach (var actualTypeAssociatedDS in actualColumnType.AssociatedDataSources)
            {
                if (expectedColumnType.ExpandInfo.ParentDataSource.DataEntityMetadataProvider.TryGetEntityMetadata(expectedColumnType.ExpandInfo.Identity, out var metadata)
                    && metadata.EntityName == actualTypeAssociatedDS.EntityName)
                {
                    isExpectedType = true;
                    break;
                }
            }

            return isExpectedType;
        }

        // Produces the intersection of the two given types.
        // For primitive types, this is only if the types match.
        // For aggregates, the union is all fields that have matching types.
        public static DType Intersection(DType type1, DType type2)
        {
            type1.AssertValid();
            type2.AssertValid();

            var fError = false;
            return Intersection(ref fError, type1, type2);
        }

        public static DType Intersection(ref bool fError, DType type1, DType type2)
        {
            type1.AssertValid();
            type2.AssertValid();

            if (type1.IsAggregate && type2.IsAggregate)
            {
                if (type1.Kind != type2.Kind)
                {
                    fError = true;
                    return Error;
                }

                return IntersectionCore(ref fError, type1, type2);
            }

            if (type1.Kind == type2.Kind)
            {
                return type1;
            }

            fError = true;
            return Error;
        }

        private static DType IntersectionCore(ref bool fError, DType type1, DType type2)
        {
            type1.AssertValid();
            Contracts.Assert(type1.IsAggregate);
            type2.AssertValid();
            Contracts.Assert(type2.IsAggregate);

            var result = CreateRecordOrTable(type1.Kind, Enumerable.Empty<TypedName>());

            foreach (var pair in type2.GetNames(DPath.Root))
            {
                var field2Name = pair.Name;

                if (!type1.TryGetType(field2Name, out var field1Type))
                {
                    continue;
                }

                var field2Type = pair.Type;
                DType fieldType;

                if (field1Type.IsAggregate && field2Type.IsAggregate)
                {
                    fieldType = Intersection(ref fError, field1Type, field2Type);
                }
                else if (field1Type.Kind == field2Type.Kind)
                {
                    fieldType = field1Type;
                }
                else
                {
                    continue;
                }

                result = result.Add(field2Name, fieldType);
            }

            return result;
        }

        public bool Intersects(DType other)
        {
            other.AssertValid();

            if (!IsAggregate || !other.IsAggregate || Kind != other.Kind)
            {
                return false;
            }

            foreach (var pair in GetNames(DPath.Root))
            {
                if (other.TryGetType(pair.Name, out var f2Type) && f2Type == pair.Type)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool operator ==(DType type1, DType type2)
        {
            var null1 = ReferenceEquals(type1, null);
            var null2 = ReferenceEquals(type2, null);
            if (null1 && null2)
            {
                return true;
            }

            if (null1 || null2)
            {
                return false;
            }

            return type1.Equals(type2);
        }

        public static bool operator !=(DType type1, DType type2)
        {
            var null1 = ReferenceEquals(type1, null);
            var null2 = ReferenceEquals(type2, null);
            if (null1 && null2)
            {
                return false;
            }

            if (null1 || null2)
            {
                return true;
            }

            return !type1.Equals(type2);
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(
                Hashing.HashInt((int)Kind),
                Hashing.HashInt((int)EnumSuperkind),
                TypeTree.GetHashCode(),
                ValueTree.GetHashCode(),
                LazyTypeProvider?.GetHashCode() ?? 0);
        }

        public override bool Equals(object obj)
        {
            return obj is DType other &&
               Kind == other.Kind &&
               TypeTree == other.TypeTree &&
               EnumSuperkind == other.EnumSuperkind &&
               ValueTree == other.ValueTree &&
               HasExpandInfo == other.HasExpandInfo &&
               NamedValueKind == other.NamedValueKind &&
               (LazyTypeProvider?.BackingFormulaType.Equals(other.LazyTypeProvider?.BackingFormulaType) ?? other.LazyTypeProvider == null);
        }

        // Viewing DType.Invalid in the debugger should be allowed
        // so this code doesn't assert if the kind is invalid.
        public override string ToString()
        {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        protected DType(
            DKind kind,
            TypeTree typeTree,
            DKind enumSuperkind,
            ValueTree valueTree,
            IExpandInfo expandInfo,
            IPolymorphicInfo polymorphicInfo,
            IDataColumnMetadata metadata,
            bool isFile,
            bool isLargeImage,
            HashSet<IExternalTabularDataSource> associatedDataSources,
            IExternalOptionSet optionSetInfo,
            IExternalViewInfo viewInfo,
            string namedValueKind,
            DisplayNameProvider displayNameProvider,
            LazyTypeProvider lazyTypeProvider)
        {
            Kind = kind;
            TypeTree = typeTree;
            EnumSuperkind = enumSuperkind;
            ValueTree = valueTree;
            ExpandInfo = expandInfo;
            PolymorphicInfo = polymorphicInfo;
            Metadata = metadata;
            _isFile = isFile;
            _isLargeImage = isLargeImage;
            AssociatedDataSources = associatedDataSources;
            OptionSetInfo = optionSetInfo;
            ViewInfo = viewInfo;
            NamedValueKind = namedValueKind;
            DisplayNameProvider = displayNameProvider;
            LazyTypeProvider = lazyTypeProvider;
        }

        public void AppendTo(StringBuilder sb)
        {
            Contracts.AssertValue(sb);
            sb.Append(MapKindToStr(Kind));

            switch (Kind)
            {
                case DKind.Record:
                case DKind.Table:
                    AppendAggregateType(sb, TypeTree);
                    break;
                case DKind.OptionSet:
                case DKind.View:
                    AppendOptionSetOrViewType(sb, TypeTree);
                    break;
                case DKind.Enum:
                    AppendEnumType(sb, ValueTree, EnumSuperkind);
                    break;
            }
        }

        /// <summary>
        /// Returns true if type contains a control type.
        /// </summary>
        public bool ContainsControlType(DPath path)
        {
            AssertValid();
            Contracts.AssertValid(path);

            return GetNames(path).Any(n => n.Type.IsControl ||
                (n.Type.IsAggregate && n.Type.ContainsControlType(DPath.Root)));
        }

        public bool CoercesTo(DType typeDest, bool aggregateCoercion = true, bool isTopLevelCoercion = false)
        {
            return CoercesTo(typeDest, out _, aggregateCoercion, isTopLevelCoercion);
        }

        public bool CoercesTo(DType typeDest, out bool isSafe, bool aggregateCoercion = true, bool isTopLevelCoercion = false)
        {
            return CoercesTo(typeDest, out isSafe, out _, out _, out _, aggregateCoercion, isTopLevelCoercion);
        }

        public bool AggregateCoercesTo(DType typeDest, out bool isSafe, out DType coercionType, out KeyValuePair<string, DType> schemaDifference, out DType schemaDifferenceType, bool aggregateCoercion = true)
        {
            Contracts.Assert(IsAggregate);

            schemaDifference = new KeyValuePair<string, DType>(null, Invalid);
            schemaDifferenceType = Invalid;

            if ((typeDest.Kind == DKind.Image && IsLargeImage) || (typeDest.IsLargeImage && this == MinimalLargeImage))
            {
                isSafe = true;
                coercionType = typeDest;
                return true;
            }

            // Can't coerce scalar->aggregate, or viceversa.
            if (!typeDest.IsAggregate)
            {
                isSafe = false;
                coercionType = this;
                return false;
            }

            if (typeDest.IsLazyType)
            {
                if (IsLazyType)
                {
                    // Coercion from lazy -> lazy is not supported
                    isSafe = false;
                    coercionType = this;
                    return false;
                }

                typeDest = typeDest.LazyTypeProvider.GetExpandedType(typeDest.IsTable);
            }

            if (Kind != typeDest.Kind && Kind == DKind.Record && aggregateCoercion)
            {
                return ToTable().CoercesTo(typeDest, out isSafe, out coercionType, out schemaDifference, out schemaDifferenceType);
            }

            if (Kind != typeDest.Kind)
            {
                isSafe = false;
                coercionType = this;
                return false;
            }

            var fieldIsSafe = true;
            var isValid = true;
            if (IsRecord)
            {
                coercionType = EmptyRecord;
            }
            else
            {
                coercionType = EmptyTable;
            }

            isSafe = true;
            foreach (var typedName in typeDest.GetNames(DPath.Root))
            {
                // If the name exists on the type, it is valid if it is coercible to the target type
                if (TryGetType(typedName.Name, out var thisFieldType))
                {
                    var isFieldValid = thisFieldType.CoercesTo(
                        typedName.Type,
                        out fieldIsSafe,
                        out var fieldCoercionType,
                        out var fieldSchemaDifference,
                        out var fieldSchemaDifferenceType,
                        aggregateCoercion);

                    // This is the attempted coercion type.  If we fail, we need to know this for error handling
                    coercionType = coercionType.Add(typedName.Name, fieldCoercionType);

                    // If this is the first field that invalidates the type, we set schema difference. We only report
                    // the first type mismatch.
                    if (!isFieldValid && isValid)
                    {
                        var fieldName = string.IsNullOrEmpty(fieldSchemaDifference.Key) ? typedName.Name : fieldSchemaDifference.Key;
                        schemaDifference = new KeyValuePair<string, DType>(fieldName, fieldSchemaDifference.Value);
                        schemaDifferenceType = fieldSchemaDifferenceType;
                    }

                    isValid &= isFieldValid;
                }
                else if (!typeDest.AreFieldsOptional)
                {
                    isValid = false; // If the name doesn't exist, it is valid only if it is optional
                }

                isSafe &= fieldIsSafe;
            }

            return isValid;
        }

        // Returns true if values of this type may be coerced to the specified type.
        // isSafe is marked false if the resultant coercion could have undesireable results
        // such as returning null or returning an unintuitive outcome.
        public virtual bool CoercesTo(DType typeDest, out bool isSafe, out DType coercionType, out KeyValuePair<string, DType> schemaDifference, out DType schemaDifferenceType, bool aggregateCoercion = true, bool isTopLevelCoercion = false)
        {
            AssertValid();
            Contracts.Assert(typeDest.IsValid);

            schemaDifference = new KeyValuePair<string, DType>(null, Invalid);
            schemaDifferenceType = Invalid;

            isSafe = true;

            if (typeDest.Accepts(this))
            {
                coercionType = typeDest;
                return true;
            }

            if (Kind == DKind.UntypedObject)
            {
                isSafe = false;
                if (typeDest.Kind == DKind.String ||
                    typeDest.Kind == DKind.Number ||
                    typeDest.Kind == DKind.Boolean ||
                    typeDest.Kind == DKind.Date ||
                    typeDest.Kind == DKind.Time ||
                    typeDest.Kind == DKind.DateTime ||
                    typeDest.Kind == DKind.Color ||
                    typeDest.Kind == DKind.Guid)
                {
                    coercionType = typeDest;
                    return true;
                }
                else
                {
                    coercionType = this;
                    return false;
                }
            }

            if (Kind == DKind.Error)
            {
                isSafe = false;
                coercionType = this;
                return false;
            }

            if (IsAggregate)
            {
                return AggregateCoercesTo(typeDest, out isSafe, out coercionType, out schemaDifference, out schemaDifferenceType, aggregateCoercion);
            }

            var subtypeCoerces = SubtypeCoercesTo(typeDest, ref isSafe, out coercionType, ref schemaDifference, ref schemaDifferenceType);
            if (subtypeCoerces.HasValue)
            {
                return subtypeCoerces.Value;
            }

            // For now, named values are never valid as a coerce target or source
            if (typeDest.Kind == DKind.NamedValue || Kind == DKind.NamedValue)
            {
                coercionType = this;
                return false;
            }

            if (Kind == DKind.Enum)
            {
                if (!typeDest.IsControl &&
                    !typeDest.IsExpandEntity &&
                    !typeDest.IsAttachment &&
                    !typeDest.IsMetadata &&
                    !typeDest.IsAggregate &&
                    typeDest.Accepts(GetEnumSupertype()))
                {
                    coercionType = typeDest;
                    return true;
                }
                else
                {
                    coercionType = this;
                    return false;
                }
            }

            if (typeDest.IsLargeImage && Kind == DKind.Image)
            {
                coercionType = typeDest;
                return true;
            }

            var doesCoerce = false;
            switch (typeDest.Kind)
            {
                case DKind.Boolean:
                    isSafe = Kind != DKind.String;
                    doesCoerce = Kind == DKind.String ||
                                 Number.Accepts(this) ||
                                 (Kind == DKind.OptionSetValue && OptionSetInfo != null && OptionSetInfo.IsBooleanValued());
                    break;
                case DKind.DateTime:
                case DKind.Date:
                case DKind.Time:
                case DKind.DateTimeNoTimeZone:
                    // String to boolean results in an unintuitive coercion
                    // (eg "Robert" -> false), unless it is "true" or "false" exactly.
                    // String to DateTime isn't safe for ill-formatted strings.
                    isSafe = Kind != DKind.String;
                    doesCoerce = Kind == DKind.String ||
                                 Number.Accepts(this) ||
                                 DateTime.Accepts(this);
                    break;
                case DKind.Number:
                    // Ill-formatted strings coerce to null; unsafe.
                    isSafe = Kind != DKind.String;
                    doesCoerce = Kind == DKind.String ||
                                 Number.Accepts(this) ||
                                 Boolean.Accepts(this) ||
                                 DateTime.Accepts(this) ||
                                 (Kind == DKind.OptionSetValue && OptionSetInfo != null && OptionSetInfo.BackingKind == DKind.Number);
                    break;
                case DKind.Currency:
                    // Ill-formatted strings coerce to null; unsafe.
                    isSafe = Kind != DKind.String;
                    doesCoerce = Kind == DKind.String ||
                                 Kind == DKind.Number ||
                                 Boolean.Accepts(this);
                    break;
                case DKind.String:
                    doesCoerce = Kind != DKind.Color && Kind != DKind.Control && Kind != DKind.DataEntity && Kind != DKind.OptionSet && Kind != DKind.View && Kind != DKind.Polymorphic && Kind != DKind.File && Kind != DKind.LargeImage;
                    break;
                case DKind.Hyperlink:
                    doesCoerce = Kind != DKind.Guid && String.Accepts(this);
                    break;
                case DKind.Image:
                    doesCoerce = Kind != DKind.Media && Kind != DKind.Blob && Kind != DKind.Guid && String.Accepts(this);
                    break;
                case DKind.Media:
                    doesCoerce = Kind != DKind.Image && Kind != DKind.PenImage && Kind != DKind.Blob && Kind != DKind.Guid && String.Accepts(this);
                    break;
                case DKind.Blob:
                    doesCoerce = Kind != DKind.Guid && String.Accepts(this);
                    break;
                case DKind.Color:
                    doesCoerce = Kind == DKind.OptionSetValue && OptionSetInfo != null && OptionSetInfo.BackingKind == DKind.Color;
                    break;
                case DKind.Enum:
                    return CoercesTo(typeDest.GetEnumSupertype(), out isSafe, out coercionType, out schemaDifference, out schemaDifferenceType);
                case DKind.OptionSetValue:
                    doesCoerce = (typeDest.OptionSetInfo?.IsBooleanValued() ?? false) && Kind == DKind.Boolean && !isTopLevelCoercion;
                    break;
            }

            if (doesCoerce)
            {
                coercionType = typeDest;
            }
            else
            {
                coercionType = this;
            }

            return doesCoerce;
        }

        protected virtual bool? SubtypeCoercesTo(DType typeDest, ref bool isSafe, out DType coercionType, ref KeyValuePair<string, DType> schemaDifference, ref DType schemaDifferenceType)
        {
            coercionType = null;
            return null;
        }

        // Gets the subtype of aggregate type expectedType that this type can coerce to.
        // Checks whether the fields of this type can be coerced to the fields of expectedType
        // and returns the type it should be coerced to in order to be compatible.
        public bool TryGetCoercionSubType(DType expectedType, out DType coercionType, out bool coercionNeeded, bool safeCoercionRequired = false, bool aggregateCoercion = true)
        {
            Contracts.Assert(expectedType.IsValid);

            // Primitive case
            if (!expectedType.IsAggregate || expectedType.IsLargeImage)
            {
                coercionType = expectedType;
                coercionNeeded = !expectedType.Accepts(this);
                if (!coercionNeeded)
                {
                    return true;
                }

                if (expectedType.TryGetExpandedEntityTypeWithoutDataSourceSpecificColumns(out var expandedType) && expandedType.Accepts(this))
                {
                    coercionNeeded = false;
                    return true;
                }

                return CoercesTo(expectedType, out var coercionIsSafe, aggregateCoercion) && (!safeCoercionRequired || coercionIsSafe);
            }

            // LazyTable/Record case
            if (expectedType.IsLazyType)
            {
                coercionType = expectedType;
                coercionNeeded = !expectedType.Accepts(this);
                if (!coercionNeeded)
                {
                    return true;
                }

                if (expectedType.LazyTypeProvider.GetExpandedType(expectedType.IsTable).Accepts(this))
                {
                    coercionNeeded = false;
                    return true;
                }

                // Lazy type coercion not supported
                return false;
            }

            coercionType = IsRecord ? EmptyRecord : EmptyTable;
            coercionNeeded = false;

            if (!IsAggregate)
            {
                return false;
            }

            if (IsTable != expectedType.IsTable && !aggregateCoercion)
            {
                return false;
            }

            foreach (var typedName in expectedType.GetNames(DPath.Root))
            {
                if (TryGetType(typedName.Name, out var thisFieldType))
                {
                    if (!thisFieldType.TryGetCoercionSubType(typedName.Type, out var thisFieldCoercionType, out var thisFieldCoercionNeeded, safeCoercionRequired, aggregateCoercion))
                    {
                        return false;
                    }

                    coercionNeeded |= thisFieldCoercionNeeded;
                    coercionType = coercionType.Add(typedName.Name, thisFieldCoercionType);
                }
            }

            return true;
        }

        public DType GetColumnTypeFromSingleColumnTable()
        {
            Contracts.Assert(IsTable);
            Contracts.Assert(IsColumn);

            return TypeTree.First().Value;
        }

        internal static bool AreCompatibleTypes(DType type1, DType type2)
        {
            Contracts.Assert(type1.IsValid);
            Contracts.Assert(type2.IsValid);

            return type1.Accepts(type2) || type2.Accepts(type1);
        }

        internal static string MapKindToStr(DKind kind)
        {
            switch (kind)
            {
                default:
                    return "x";
                case DKind.Unknown:
                    return "?";
                case DKind.Deferred:
                    return "X";
                case DKind.Error:
                    return "e";
                case DKind.Boolean:
                    return "b";
                case DKind.Number:
                    return "n";
                case DKind.String:
                    return "s";
                case DKind.Hyperlink:
                    return "h";
                case DKind.DateTime:
                    return "d";
                case DKind.Date:
                    return "D";
                case DKind.Time:
                    return "T";
                case DKind.DateTimeNoTimeZone:
                    return "Z";
                case DKind.Image:
                    return "i";
                case DKind.PenImage:
                    return "p";
                case DKind.Currency:
                    return "$";
                case DKind.Color:
                    return "c";
                case DKind.Record:
                    return "!";
                case DKind.LazyRecord:
                    return "r!";
                case DKind.Table:
                    return "*";
                case DKind.LazyTable:
                    return "r*";
                case DKind.Enum:
                    return "%";
                case DKind.Media:
                    return "m";
                case DKind.Blob:
                    return "o";
                case DKind.LegacyBlob:
                    return "a";
                case DKind.Guid:
                    return "g";
                case DKind.Control:
                    return "v";
                case DKind.DataEntity:
                    return "E";
                case DKind.Metadata:
                    return "M";
                case DKind.ObjNull:
                    return "N";
                case DKind.OptionSet:
                    return "L";
                case DKind.OptionSetValue:
                    return "l";
                case DKind.Polymorphic:
                    return "P";
                case DKind.View:
                    return "Q";
                case DKind.ViewValue:
                    return "q";
                case DKind.File:
                    return "F";
                case DKind.LargeImage:
                    return "I";
                case DKind.NamedValue:
                    return "V";
                case DKind.UntypedObject:
                    return "O";
            }
        }

        private static void AppendAggregateType(StringBuilder sb, TypeTree tree)
        {
            Contracts.AssertValue(sb);

            sb.Append("[");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));
                sb.Append(":");
                kvp.Value.AppendTo(sb);
                strPre = ", ";
            }

            sb.Append("]");
        }

        private static void AppendOptionSetOrViewType(StringBuilder sb, TypeTree tree)
        {
            Contracts.AssertValue(sb);

            sb.Append("{");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));
                sb.Append(":");
                kvp.Value.AppendTo(sb);
                strPre = ", ";
            }

            sb.Append("}");
        }

        private static void AppendEnumType(StringBuilder sb, ValueTree tree, DKind enumSuperkind)
        {
            Contracts.AssertValue(sb);

            sb.Append(MapKindToStr(enumSuperkind));
            sb.Append("[");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.AssertNonEmpty(kvp.Key);
                Contracts.AssertValue(kvp.Value.Object);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));
                sb.Append(":");
                kvp.Value.AppendTo(sb);
                strPre = ", ";
            }

            sb.Append("]");
        }

        // Produces a DType from a string representation in our reduced type algebra language.
        internal static bool TryParse(string typeSpec, out DType type)
        {
            Contracts.AssertNonEmpty(typeSpec);

            return DTypeSpecParser.TryParse(new DTypeSpecLexer(typeSpec), out type);
        }

        // Fetch the meta field for this DType, if there is one.
        public bool TryGetMetaField(out IExternalControlType metaFieldType)
        {
            if (!IsAggregate ||
                !TryGetType(new DName(MetaFieldName), out var field) ||
                !(field is IExternalControlType control) ||
                !control.ControlTemplate.IsMetaLoc)
            {
                metaFieldType = null;
                return false;
            }

            metaFieldType = control;
            return true;
        }

        public void ReportNonExistingName(FieldNameKind fieldNameKind, IErrorContainer errors, DName name, TexlNode location, DocumentErrorSeverity severity = DocumentErrorSeverity.Severe)
        {
            if (TryGetSimilarName(name, fieldNameKind, out var similar))
            {
                errors.EnsureError(severity, location, TexlStrings.ErrColumnDoesNotExist_Name_Similar, name, similar);
            }
            else
            {
                errors.EnsureError(severity, location, TexlStrings.ErrColDNE_Name, name);
            }
        }

        public bool TryGetSimilarName(DName name, FieldNameKind fieldNameKind, out string similar)
        {
            var maxLength = 2000;
            similar = null;
            if (name.Value.Length > maxLength)
            {
                return false;
            }

            var comparer = new StringDistanceComparer(name.Value, maxLength);
            similar = GetNames(DPath.Root).Select(k =>
            {
                var result = k.Name.Value;
                if (fieldNameKind == FieldNameKind.Display && TryGetDisplayNameForColumn(this, result, out var colName))
                {
                    result = colName;
                }

                return result;
            }).OrderBy(x => x, comparer).FirstOrDefault();

            // We also want to have a heuristic maximum distance so that we don't give ridiculous
            // suggestions.
            return similar != null &&
                   comparer.Distance(similar) < (name.Value.Length / 3) + 3;
        }
    }
}
