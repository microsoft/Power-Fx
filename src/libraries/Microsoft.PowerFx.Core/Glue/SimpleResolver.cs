// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Glue
{
    /// <summary>
    /// Basic implementation of INameResolver around a <see cref="PowerFxConfig"/> object. 
    /// This aides in binding and intellisense. 
    /// Host can override Lookup to provide additional symbols to the expression. 
    /// </summary>
    internal class SimpleResolver : INameResolver2
    {
        private readonly PowerFxConfig _config;
        private readonly TexlFunction[] _library;        
        private readonly EnumSymbol[] _enums = new EnumSymbol[] { };
        private readonly IExternalDocument _document;

        protected IReadOnlyDictionary<string, IGlobalSymbol> _globalSymbols;

        IExternalDocument INameResolver.Document => _document;

        IExternalEntityScope INameResolver.EntityScope => throw new NotImplementedException();

        DName INameResolver.CurrentProperty => default;

        DPath INameResolver.CurrentEntityPath => default;

        // Expose the list to aide in intellisense suggestions. 
        public IEnumerable<TexlFunction> Functions => _library;

        public IReadOnlyDictionary<string, IGlobalSymbol> GlobalSymbols => _globalSymbols;

        IExternalEntity INameResolver.CurrentEntity => null;

        public bool SuggestUnqualifiedEnums => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleResolver"/> class.
        /// </summary>
        /// <param name="config"></param>        
        public SimpleResolver(PowerFxConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _library = config.Functions.ToArray();
            _globalSymbols = null;
            _enums = config.EnumStoreBuilder.Build().EnumSymbols.ToArray();            
        }

        public SimpleResolver(PowerFxConfig config, IReadOnlyDictionary<string, IGlobalSymbol> globalSymbols)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _library = config.Functions.ToArray();
            _globalSymbols = globalSymbols;
            _enums = config.EnumStoreBuilder.Build().EnumSymbols.ToArray();
        }

        // for derived classes that need to set INameResolver.Document. 
        protected SimpleResolver(PowerFxConfig config, IExternalDocument document)
            : this(config)
        {
            _document = document;
        }

        public virtual bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
        {
            if (_config != null)
            {
                if (_config.TryGetSymbol(name, out var symbol, out var displayName))
                {
                    // Special case symbols
                    if (symbol is IExternalOptionSet optionSet)
                    {
                        nameInfo = new NameLookupInfo(
                            BindKind.OptionSet,
                            optionSet.Type,
                            DPath.Root,
                            0,
                            optionSet,
                            displayName);

                        return true;
                    }
                    else
                    {
                        throw new NotImplementedException($"{symbol.GetType().Name} not supported.");
                    }
                }
            }

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
                {
                    return info.TryLookupValueByLocName(locName.Value, out _, out value);
                }
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
