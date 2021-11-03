// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalEntityScope
    {
        bool TryGetNamedEnum(DName identName, out DType enumType);
        bool TryGetCdsDataSourceWithLogicalName(string datasetName, string expandInfoIdentity, out IExternalCdsDataSource dataSource);
        IExternalTabularDataSource GetTabularDataSource(string identName);
        bool TryGetEntity<T>(DName currentEntityEntityName, out T externalEntity) where T : class,IExternalEntity;
        IEnumerable<IExternalEntity> DynamicTypes { get; }
    }

    internal static class IExternalEntityScopeExtensions
    {
        // from FunctionUtils.TryGetDataSource
        public static bool TryGetDataSource(this IExternalEntityScope entityScope, TexlNode node, out IExternalDataSource dataSourceInfo)
        {
            Contracts.AssertValue(entityScope);
            Contracts.AssertValue(node);

            FirstNameNode firstNameNode;
            if ((firstNameNode = node.AsFirstName()) == null)
            {
                dataSourceInfo = null;
                return false;
            }

            return entityScope.TryGetEntity<IExternalDataSource>(firstNameNode.Ident.Name, out dataSourceInfo);
        }
    }
}