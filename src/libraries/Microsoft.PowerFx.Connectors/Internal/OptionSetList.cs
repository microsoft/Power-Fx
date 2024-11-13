// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.Connectors
{
    internal class OptionSetList
    {
        private readonly Dictionary<string, OptionSet> _optionSets;        

        public OptionSetList()
        {
            _optionSets = new Dictionary<string, OptionSet>();            
        }

        public IEnumerable<OptionSet> OptionSets => _optionSets.Values;

        // if the option set doesn't exist, it will be added to the list and we return optionSet
        // if the option set exists with same name and options, it is not added, we return the existing optionSet
        // if the option set exists with same name but different options, we throw an exception (conflicts not allowed)
        public OptionSet TryAdd(OptionSet optionSet)
        {
            if (optionSet == null)
            {
                throw new ArgumentNullException("optionSet");
            }

            OptionSetStatus oss = GetStatus(optionSet);

            // new option set
            if (oss == OptionSetStatus.New)
            {
                Add(optionSet);
                return optionSet;
            }
            
            // same optionset, no need to add anything
            if (oss == OptionSetStatus.Same)
            {
                return Get(optionSet.EntityName);
            }
                        
            throw new InvalidOperationException($"Optionset name conflict ({optionSet.EntityName})");           
        }

        private void Add(OptionSet optionSet)
        {
            _optionSets.Add(optionSet.EntityName, optionSet);            
        }

        private OptionSet Get(string name)
        {
            return _optionSets[name];
        }

        private enum OptionSetStatus
        {
            New,
            Same,
            Conflict
        }

        private OptionSetStatus GetStatus(OptionSet optionSet)
        {
            if (optionSet == null)
            {
                return OptionSetStatus.New;
            }

            string name = optionSet.EntityName;

            if (!_optionSets.ContainsKey(name))
            {
                return OptionSetStatus.New;
            }

            OptionSet existingOptionSet = Get(name);

            if (existingOptionSet.Equals(optionSet))
            {
                return OptionSetStatus.Same;
            }

            return OptionSetStatus.Conflict;
        }
    }
}
