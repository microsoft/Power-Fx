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

        internal ConnectorExtensions(IOpenApiExtensible extension, bool numberIsFloat)
        {
            ConnectorDynamicValue = extension.GetDynamicValue(numberIsFloat);
            ConnectorDynamicList = extension.GetDynamicList(numberIsFloat);
            ConnectorDynamicSchema = extension.GetDynamicSchema(numberIsFloat);
            ConnectorDynamicProperty = extension.GetDynamicProperty(numberIsFloat);

            Summary = extension.GetSummary();
            ExplicitInput = extension.GetExplicitInput();
        }

        internal ConnectorExtensions(IOpenApiExtensible body, IOpenApiExtensible extension, bool numberIsFloat)
        {
            ConnectorDynamicValue = extension.GetDynamicValue(numberIsFloat);
            ConnectorDynamicList = extension.GetDynamicList(numberIsFloat);
            ConnectorDynamicSchema = extension.GetDynamicSchema(numberIsFloat);
            ConnectorDynamicProperty = extension.GetDynamicProperty(numberIsFloat);

            Summary = body.GetSummary();
            ExplicitInput = body.GetExplicitInput();
        }
    }
}
