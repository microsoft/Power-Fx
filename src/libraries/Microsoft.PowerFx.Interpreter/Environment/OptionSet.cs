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
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Interpreter
{
    public class OptionSet : IExternalOptionSet<string>
    {
        // Staying
        public DName EntityName { get; }

        public ImmutableDictionary<DName, DName> Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet"/> class.
        /// </summary>
        /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
        /// <param name="options">The members of the option set. Dictionary of Display name to Logical name.</param>
        public OptionSet(string name, IDictionary<string, string> options)
        {
            EntityName = new DName(name);
            Options = ImmutableDictionary.CreateRange(options.Select(kvp => new KeyValuePair<DName, DName>(new DName(kvp.Key), new DName(kvp.Value))));
        }

        internal DisplayNameProvider GetDisplayNameProvider()
        {
            return new SingleSourceDisplayNameProvider(Options);
        }

        
        // Refactor the interface to separate out all this Canvas stuff from Power Fx. 
        public string Name => EntityName;

        public bool IsBooleanValued => false;

        public string RelatedEntityName => throw new NotImplementedException();

        public bool IsControl => throw new NotImplementedException();

        public IExternalEntityScope EntityScope => throw new NotImplementedException();

        public IEnumerable<IDocumentError> Errors => throw new NotImplementedException();

        public bool IsConvertingDisplayNameMapping => throw new NotImplementedException();

        public BidirectionalDictionary<string, string> DisplayNameMapping => throw new NotImplementedException();

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping => throw new NotImplementedException();

        public bool TryGetRule(DName propertyName, out IExternalRule rule)
        {
            throw new NotImplementedException();
        }
    }
}
