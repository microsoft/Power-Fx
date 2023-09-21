// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Interfaces;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorExtensions
    {
        internal ConnectorDynamicValue ConnectorDynamicValue;
        internal ConnectorDynamicList ConnectorDynamicList;
        internal ConnectorDynamicSchema ConnectorDynamicSchema;
        internal ConnectorDynamicProperty ConnectorDynamicProperty;
        internal string Summary;
        internal bool ExplicitInput;

        internal ConnectorExtensions(IOpenApiExtensible extension, IOpenApiExtensible body, bool numberIsFloat)
        {
            ConnectorDynamicValue = extension.GetDynamicValue();
            ConnectorDynamicList = extension.GetDynamicList();
            ConnectorDynamicSchema = extension.GetDynamicSchema();
            ConnectorDynamicProperty = extension.GetDynamicProperty();

            Summary = (body ?? extension).GetSummary();
            ExplicitInput = (body ?? extension).GetExplicitInput();
        }
    }
}
