// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding
{
    [Flags]
    internal enum NameLookupPreferences
    {
        None = 0,
        GlobalsOnly = 0x1,
        HasDottedNameParent = 0x2,
    }

    // A binder interface that specifies how to resolve global or otherwise unscoped names.
    internal interface INameResolver
    {
        IExternalDocument Document { get; }

        IExternalEntityScope EntityScope { get; }

        IExternalEntity CurrentEntity { get; }

        DName CurrentProperty { get; }

        DPath CurrentEntityPath { get; }

        IEnumerable<TexlFunction> Functions { get; }

        IReadOnlyDictionary<string, DType> VariableNames { get; }

        // This advertises whether the INameResolver instance will suggest unqualified enums ("Hours")
        // or only qualified enums ("TimeUnit.Hours").
        // This must be consistent with how the other Lookup functions behave.
        // Intellisense can use this when suggesting completion options.
        bool SuggestUnqualifiedEnums { get; }

        // Look up an entity, context variable, or entity part (e.g. enum value) by name.
        bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None);

        bool TryGetInnermostThisItemScope(out NameLookupInfo nameInfo);

        // Look up the data control associated with the current entity+property path, given a ThisItem identifier
        bool LookupDataControl(DName name, out NameLookupInfo lookupInfo, out DName dataControlName);

        // Look up a list of functions (and overloads) by namespace and name.
        IEnumerable<TexlFunction> LookupFunctions(DPath theNamespace, string name, bool localeInvariant = false);

        /// <returns>
        /// List of functions in <paramref name="nameSpace"/>.
        /// </returns>
        IEnumerable<TexlFunction> LookupFunctionsInNamespace(DPath nameSpace);

        // Return true if the specified boxed enum info contains a value for the given locale-specific name.
        bool LookupEnumValueByInfoAndLocName(object enumInfo, DName locName, out object value);

        // Return true if the specified enum type contains a value for the given locale-specific name.
        bool LookupEnumValueByTypeAndLocName(DType enumType, DName locName, out object value);

        // Looks up the parent control for the current context.
        bool LookupParent(out NameLookupInfo lookupInfo);

        // Looks up the current control for the current context.
        bool LookupSelf(out NameLookupInfo lookupInfo);

        // Looks up the global entity.
        bool LookupGlobalEntity(DName name, out NameLookupInfo lookupInfo);

        bool TryLookupEnum(DName name, out NameLookupInfo lookupInfo);
    }

    internal static class NameResolverExtensions
    {
        internal static bool TryGetCurrentControlProperty(this INameResolver resolver, out IExternalControlProperty currentProperty)
        {
            // If the current entity is a control and valid
            if ((resolver.CurrentEntity is IExternalControl control) && resolver.CurrentProperty.IsValid)
            {
                control.Template.TryGetInputProperty(resolver.CurrentProperty.Value, out currentProperty);
                return true;
            }

            currentProperty = null;
            return false;
        }
    }
}
