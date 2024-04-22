// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Binding
{
    [Flags]
    internal enum NameLookupPreferences
    {
        None = 0,
        
        /// <summary>
        /// An identifier with [@name] notation. This means ignore all symbols in RowScope.
        /// Used to specify a global that may be overshadoweded by a local.
        /// </summary>
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

        TexlFunctionSet Functions { get; }

        // List of all valid named types in a given namespace 
        // Intellisense can use this when suggesting type options.
        IEnumerable<KeyValuePair<DName, FormulaType>> NamedTypes { get; }

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

        // Look up a type by name.
        bool LookupType(DName name, out FormulaType fType);

        /// <returns>
        /// List of functions in <paramref name="nameSpace"/>.
        /// </returns>
        IEnumerable<TexlFunction> LookupFunctionsInNamespace(DPath nameSpace);

        // Looks up the parent control for the current context.
        bool LookupParent(out NameLookupInfo lookupInfo);

        /// <summary>
        /// In Power Apps specifically, this is used to retrieve the full, derived, type
        /// of a control object where the output schema may be dependent on a control inputs and hierarchy. 
        /// All Non-Power Apps hosts should not implement this method.
        /// </summary>
        /// <param name="control">The control to get the full type of.</param>
        /// <param name="controlType">output: the expanded control type.</param>
        bool LookupExpandedControlType(IExternalControl control, out DType controlType);

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
