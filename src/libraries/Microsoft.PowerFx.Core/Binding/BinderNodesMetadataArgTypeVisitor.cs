// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding
{
    internal sealed partial class TexlBinding
    {
        private sealed class BinderNodesMetadataArgTypeVisitor : Visitor
        {
            private readonly TexlBinding _txb;

            public BinderNodesMetadataArgTypeVisitor(TexlBinding binding, INameResolver resolver, DType topScope, bool useThisRecordForRuleScope)
                : base(binding, resolver, topScope, useThisRecordForRuleScope)
            {
                Contracts.AssertValue(binding);

                _txb = binding;
            }

            private bool IsColumnMultiChoice(IExternalColumnMetadata columnMetadata)
            {
                Contracts.AssertValue(columnMetadata);

                return columnMetadata?.DataFormat == DataFormat.Lookup;
            }

            public override void PostVisit(DottedNameNode node)
            {
                Contracts.AssertValue(node);

                var lhsType = _txb.GetType(node.Left);
                var typeRhs = DType.Invalid;
                var nameRhs = node.Right.Name;
                FirstNameInfo firstNameInfo;
                FirstNameNode firstNameNode;
                IExternalTableMetadata tableMetadata;
                var nodeType = DType.Unknown;

                if (node.Left.Kind != NodeKind.FirstName &&
                    node.Left.Kind != NodeKind.DottedName)
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName);
                    return;
                }

                nameRhs = GetLogicalNodeNameAndUpdateDisplayNames(lhsType, node.Right);

                if (!lhsType.TryGetType(nameRhs, out typeRhs))
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName);
                    return;
                }

                // There are two cases:
                // 1. RHS could be an option set.
                // 2. RHS could be a data entity.
                // 3. RHS could be a column name and LHS would be a datasource.
                if (typeRhs.IsOptionSet)
                {
                    nodeType = typeRhs;
                }
                else if (typeRhs.IsExpandEntity)
                {
                    var entityInfo = typeRhs.ExpandInfo;
                    Contracts.AssertValue(entityInfo);

                    var entityPath = string.Empty;
                    if (lhsType.HasExpandInfo)
                    {
                        entityPath = lhsType.ExpandInfo.ExpandPath.ToString();
                    }

                    var expandedEntityType = GetExpandedEntityType(typeRhs, entityPath);

                    var parentDataSource = entityInfo.ParentDataSource;
                    var metadata = new DataTableMetadata(parentDataSource.Name, parentDataSource.Name);
                    nodeType = DType.CreateMetadataType(new DataColumnMetadata(typeRhs.ExpandInfo.Name, expandedEntityType, metadata));
                }
                else if ((firstNameNode = node.Left.AsFirstName()) != null && (firstNameInfo = _txb.GetInfo(firstNameNode)) != null)
                {
                    var tabularDataSourceInfo = firstNameInfo.Data as IExternalTabularDataSource;
                    tableMetadata = tabularDataSourceInfo?.TableMetadata;
                    if (tableMetadata == null || !tableMetadata.TryGetColumn(nameRhs.Value, out var columnMetadata) || !IsColumnMultiChoice(columnMetadata))
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidName);
                        return;
                    }

                    var metadata = new DataTableMetadata(tabularDataSourceInfo.Name, tableMetadata.DisplayName);
                    nodeType = DType.CreateMetadataType(new DataColumnMetadata(columnMetadata, metadata));
                }
                else
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName);
                    return;
                }

                Contracts.AssertValid(nodeType);

                _txb.SetType(node, nodeType);
                _txb.SetInfo(node, new DottedNameInfo(node));
            }
        }
    }
}
