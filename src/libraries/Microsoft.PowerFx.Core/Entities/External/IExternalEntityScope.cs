﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Entities
{
    [ThreadSafeImmutable]
    internal interface IExternalEntityScope
    {
        bool TryGetNamedEnum(DName identName, out DType enumType);

        bool TryGetCdsDataSourceWithLogicalName(string datasetName, string expandInfoIdentity, out IExternalCdsDataSource dataSource);

        IExternalTabularDataSource GetTabularDataSource(string identName);

        bool TryGetEntity<T>(DName currentEntityEntityName, out T externalEntity)
            where T : class, IExternalEntity;

        /// <summary>
        /// Checks if the given name is available or not.
        /// <para>This is used in Tokenization to determine if left-hand side of the dotted name can be hidden or not.</para>
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="ignoreNamedFormulas">Flag indicating whether to ignore named formulas or not when checking for the availability of the given name.</param>
        /// <returns>True if name is available and not used or false otherwise.</returns>
        bool IsNameAvailable(string name, bool ignoreNamedFormulas = false);
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
