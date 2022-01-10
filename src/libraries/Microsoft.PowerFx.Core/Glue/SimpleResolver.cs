// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Glue
{
    /// <summary>
    /// Basic implementation of INameResolver. 
    /// Host can override Lookup to provide additional symbols to the expression. 
    /// </summary>
    internal class SimpleResolver : INameResolver
    {
        protected TexlFunction[] _library;
        protected EnumSymbol[] _enums = new EnumSymbol[] { };
        protected IExternalDocument _document;

        public IExternalDocument Document => _document;

        // public EntityScope EntityScope => (EntityScope)Document.GlobalScope;
        //IExternalEntityScope INameResolver.EntityScope => EntityScope;
        IExternalEntityScope INameResolver.EntityScope => throw new NotImplementedException();

        public DName CurrentProperty => default;
        public DPath CurrentEntityPath => default;

        // Allow behavior properties, like calls to POST.
        // $$$ this may need to be under a flag so host can enforce read-only properties.
        public bool CurrentPropertyIsBehavior => true;

        public bool CurrentPropertyIsConstantData => false;
        public bool CurrentPropertyAllowsNavigation => false;

        public IEnumerable<TexlFunction> Functions => _library;

        public IExternalEntity CurrentEntity => null;

        public SimpleResolver(IEnumerable<EnumSymbol> enumSymbols, params TexlFunction[] extraFunctions) : this(extraFunctions)
        {
            _enums = enumSymbols.ToArray();
        }

        public SimpleResolver(params TexlFunction[] extraFunctions)
        {
            var list = new List<TexlFunction>();
            list.AddRange(BuiltinFunctionsCore.BuiltinFunctionsLibrary);
            list.AddRange(extraFunctions);
            _library = list.ToArray();
        }

        public virtual bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
        {
            var enumValue = _enums.FirstOrDefault(symbol => symbol.InvariantName == name);
            if (enumValue != null)
            {
                nameInfo = new NameLookupInfo(BindKind.Enum, enumValue.EnumType, DPath.Root, 0, enumValue);
                return true;
            }

            nameInfo = default;
            return false;
        }

        public IEnumerable<TexlFunction> LookupFunctions(DPath theNamespace, string name, bool localeInvariant = false)
        {
            Contracts.Check(theNamespace.IsValid, "The namespace is invalid.");
            Contracts.CheckNonEmpty(name, "name");

            // See TexlFunctionsLibrary.Lookup
            // return _functionLibrary.Lookup(theNamespace, name, localeInvariant, null);            
            var functionLibrary = _library.Where(func => func.Namespace == theNamespace && name == (localeInvariant ? func.LocaleInvariantName : func.Name)); // Base filter
            return functionLibrary;
        }

        public IEnumerable<TexlFunction> LookupFunctionsInNamespace(DPath nameSpace)
        {
            Contracts.Check(nameSpace.IsValid, "The namespace is invalid.");

            return _library.Where(function => function.Namespace.Equals(nameSpace));
        }

        internal bool TryGetNamedEnumValueByLocName(DName locName, out DName enumName, out DType enumType, out object value)
        {
            foreach (var info in _enums)
            {
                if (info.TryLookupValueByLocName(locName, out _, out value))
                {
                    enumName = new DName(info.Name);
                    enumType = info.EnumType;
                    return true;
                }
            }

            // Not found
            enumName = default;
            enumType = null;
            value = null;

            return false;
        }

        public bool LookupDataControl(DName name, out NameLookupInfo lookupInfo, out DName dataControlName)
        {
            dataControlName = default;
            lookupInfo = default;
            return false;
        }

        public virtual bool LookupEnumValueByInfoAndLocName(object enumInfo, DName locName, out object value)
        {
            value = null;
            var castEnumInfo = enumInfo as EnumSymbol;
            return castEnumInfo?.TryLookupValueByLocName(locName.Value, out _, out value) ?? false;
        }

        public virtual bool LookupEnumValueByTypeAndLocName(DType enumType, DName locName, out object value)
        {
            // Slower O(n) lookup involving a walk over the registered enums...
            foreach (var info in _enums)
            {
                if (info.EnumType == enumType)
                    return info.TryLookupValueByLocName(locName.Value, out _, out value);
            }

            value = null;
            return false;
        }

        public virtual bool LookupGlobalEntity(DName name, out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        public bool LookupParent(out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        public bool LookupSelf(out NameLookupInfo lookupInfo)
        {
            lookupInfo = default;
            return false;
        }

        public bool TryGetInnermostThisItemScope(out NameLookupInfo nameInfo)
        {
            nameInfo = default;
            return false;
        }

        public bool TryLookupEnum(DName name, out NameLookupInfo lookupInfo)
        {
            throw new System.NotImplementedException();
        }
    }
}
