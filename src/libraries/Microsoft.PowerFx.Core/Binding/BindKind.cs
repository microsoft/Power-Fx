// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Binding
{
    internal enum BindKind
    {
        // default(BindKind) will resolve to Unknown.
        Unknown,
        Min = Unknown,

        // Global control entity.
        Control,

        // Global data source, such as an Excel table, or a Sharepoint list.
        Data,

        // Component namespace, such as scope property functions, component1.Add(10)
        ComponentNameSpace,

        // Field of a row parameter in a lambda.
        LambdaField,

        // This is a full reference to the lambda param, e.g. "ThisRecord" in an expression like "Filter(T, ThisRecord.Price < 100)".
        LambdaFullRecord,

        // Screen attribute or alias.
        Alias,

        // Enum, such as Color or SortOrder.
        Enum,

        /// <summary>
        /// STOP. Only use this if you are 100% clear on what you're doing
        /// This bindkind is not valid in documents loaded after V1_287
        /// </summary>
        DeprecatedImplicitThisItem,

        // ThisItem bind kind.
        ThisItem,

        // Resource, such as image, video etc.
        Resource,

        // Scope variable(app variable or component variable).
        ScopeVariable,

        // Scope Collection (component scoped collection).
        ScopeCollection,

        // Global condition
        Condition,

        // Global OptionSet
        OptionSet,

        // Entity views
        View,

        // Local scope function argument
        ScopeArgument,

        // Global Name with attached value
        NamedValue,

        // Global webresource for localization, should be unified with QualifiedValue
        WebResource,

        // Corresponds to an object resolved by a PowerFx name resolver
        // The Data field of the FirstNameInfo is passed along into the IR
        PowerFxResolvedObject,

        // Global namespace, only used with fully qualified values
        QualifiedValue,

        Lim
    }
}
