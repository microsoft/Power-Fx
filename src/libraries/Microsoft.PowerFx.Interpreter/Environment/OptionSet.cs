// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    public class OptionSet : IExternalOptionSet
    {
        private readonly SingleSourceDisplayNameProvider _displayNameProvider;
        private readonly DType _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="options">The members of the option set. Dictionary of logical name to display name.</param>
        public OptionSet(string name, IDictionary<string, string> options)
        {
            EntityName = new DName(name);
            Options = ImmutableDictionary.CreateRange(options.Select(kvp => new KeyValuePair<DName, DName>(new DName(kvp.Key), new DName(kvp.Value))));

            _displayNameProvider = new SingleSourceDisplayNameProvider(Options);
            FormulaType = new OptionSetValueType(this);
            _type = DType.CreateOptionSetType(this);
        }
        
        /// <summary>
        /// Name of the option set, referenceable from expressions.
        /// </summary>
        public DName EntityName { get; }

        /// <summary>
        /// Contains the members of the option set.
        /// Key is logical/invariant name, value is display name.
        /// </summary>
        public ImmutableDictionary<DName, DName> Options { get; }

        /// <summary>
        /// Formula Type corresponding to this option set.
        /// Use in record/table contexts to define the type of fields using this option set.
        /// </summary>
        public FormulaType FormulaType { get; }

        public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
        {
            if (!Options.ContainsKey(fieldName)) 
            {
                optionSetValue = null;
                return false;
            }

            optionSetValue = new OptionSetValue(fieldName, IRContext.NotInSource(FormulaType));
            return true;
        }

        IEnumerable<DName> IExternalOptionSet.OptionNames => Options.Keys;

        DisplayNameProvider IExternalOptionSet.DisplayNameProvider => _displayNameProvider;
        
        bool IExternalOptionSet.IsBooleanValued => false;

        bool IExternalOptionSet.IsConvertingDisplayNameMapping => false;

        DType IExternalOptionSet.Type => _type;
    }
}
