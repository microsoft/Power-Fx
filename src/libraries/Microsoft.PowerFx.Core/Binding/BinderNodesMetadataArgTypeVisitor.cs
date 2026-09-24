// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    internal sealed partial class TexlBinding
    {
        private sealed class BinderNodesMetadataArgTypeVisitor : Visitor
        {
            private readonly TexlBinding _txb;

            public BinderNodesMetadataArgTypeVisitor(TexlBinding binding, INameResolver resolver, DType topScope, bool useThisRecordForRuleScope, Features features)
                : base(binding, resolver, topScope, useThisRecordForRuleScope, features)
            {
                Contracts.AssertValue(binding);

                _txb = binding;
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
                    SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                    return;
                }

                nameRhs = GetLogicalNodeNameAndUpdateDisplayNames(lhsType, node.Right);

                if (!lhsType.TryGetType(nameRhs, out typeRhs))
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
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
                    var tableDisplayName = parentDataSource.TableMetadata?.DisplayName;
                    var metadata = new DataTableMetadata(
                        parentDataSource.Name,
                        string.IsNullOrEmpty(tableDisplayName) ? parentDataSource.Name : tableDisplayName);
                    nodeType = DType.CreateMetadataType(new DataColumnMetadata(typeRhs.ExpandInfo.Name, expandedEntityType, metadata));
                }
                else if ((firstNameNode = node.Left.AsFirstName()) != null && (firstNameInfo = _txb.GetInfo(firstNameNode)) != null)
                {
                    var tabularDataSourceInfo = firstNameInfo.Data as IExternalTabularDataSource;
                    tableMetadata = tabularDataSourceInfo?.TableMetadata;
                    if (tableMetadata == null || !tableMetadata.TryGetColumn(nameRhs.Value, out var columnMetadata))
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                        return;
                    }

                    var metadata = new DataTableMetadata(tableMetadata.Name, tableMetadata.DisplayName);
                    nodeType = DType.CreateMetadataType(new DataColumnMetadata(columnMetadata, metadata), tabularDataSourceInfo);
                }
                else
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                    return;
                }

                Contracts.AssertValid(nodeType);

                _txb.SetType(node, nodeType);
                _txb.SetInfo(node, new DottedNameInfo(node));
            }
        }
    }
}
