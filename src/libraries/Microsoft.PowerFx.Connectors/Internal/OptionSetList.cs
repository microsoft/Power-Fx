// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;

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
        // if the option set exists with same name but different options, we rename it and return that new optionSet
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
            
            int i = 1;
            SingleSourceDisplayNameProvider dnp = new SingleSourceDisplayNameProvider(optionSet.Options);

            while (oss == OptionSetStatus.Conflict && i < 20)
            {
                string name = $"{optionSet.EntityName}_{i++}";

                OptionSet newOptionSet = new OptionSet(name, dnp);
                oss = GetStatus(newOptionSet);

                // no conflict now, let's add it
                if (oss == OptionSetStatus.New)
                {
                    Add(newOptionSet);
                    return newOptionSet;
                }

                // we found an existing one that matches
                if (oss == OptionSetStatus.Same)
                {
                    return Get(name);
                }
            }

            throw new InvalidOperationException("Too many option set conflicts");           
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
