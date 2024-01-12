// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Connectors
{
    public class SupportsConnectorErrors
    {
        public bool HasErrors => _errors != null && _errors.Count != 0;

        public IReadOnlyCollection<string> Errors => _errors;

        private HashSet<string> _errors = null;

        protected SupportsConnectorErrors()
        {
        }

        protected SupportsConnectorErrors(string error)
        {
            AddError(error);
        }

        internal void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                InitializeList();
                _errors.Add(error);
            }
        }

        internal T AggregateErrors<T>(T externalObject)
            where T : SupportsConnectorErrors
        {
            if (externalObject?.HasErrors == true)
            {
                InitializeList();
                foreach (string error in externalObject.Errors)
                {
                    _errors.Add(error);
                }
            }

            return externalObject;
        }

        private void InitializeList()
        {
            _errors ??= new HashSet<string>();
        }
    }
}
