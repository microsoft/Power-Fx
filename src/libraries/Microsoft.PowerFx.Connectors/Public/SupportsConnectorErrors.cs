// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Connectors
{
    public class SupportsConnectorErrors
    {
        public bool HasErrors => _errors != null && _errors.Count != 0;

        public bool HasWarnings => _warnings != null && _warnings.Count != 0;

        public IReadOnlyCollection<string> Errors => _errors;

        public IReadOnlyCollection<ErrorResourceKey> Warnings => _warnings;

        private HashSet<string> _errors = null;

        private HashSet<ErrorResourceKey> _warnings = null;

        protected SupportsConnectorErrors()
        {
        }

        protected SupportsConnectorErrors(string error, ErrorResourceKey warning = default)
        {
            AddError(error);
            AddWarning(warning);
        }

        internal void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                InitializeLists();
                _errors.Add(error);
            }
        }

        internal void AddWarning(ErrorResourceKey warning)
        {
            if (warning.Key != null)
            {
                InitializeLists();
                _warnings.Add(warning);
            }
        }

        internal T AggregateErrorsAndWarnings<T>(T externalObject)
            where T : SupportsConnectorErrors
        {
            if (externalObject?.HasErrors == true)
            {
                InitializeLists();
                foreach (string error in externalObject.Errors)
                {
                    _errors.Add(error);
                }
            }

            if (externalObject?.HasWarnings == true)
            {
                InitializeLists();
                foreach (ErrorResourceKey warning in externalObject.Warnings)
                {
                    _warnings.Add(warning);
                }
            }

            return externalObject;
        }

        private void InitializeLists()
        {
            _errors ??= new HashSet<string>();
            _warnings ??= new HashSet<ErrorResourceKey>();
        }
    }
}
