// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Base class for supporting connector errors and warnings.
    /// </summary>
    public class SupportsConnectorErrors
    {
        /// <summary>
        /// Gets a value indicating whether there are errors.
        /// </summary>
        public bool HasErrors => _errors != null && _errors.Count != 0;

        /// <summary>
        /// Gets a value indicating whether there are warnings.
        /// </summary>
        public bool HasWarnings => _warnings != null && _warnings.Count != 0;

        /// <summary>
        /// Gets the collection of errors.
        /// </summary>
        public IReadOnlyCollection<string> Errors => _errors;

        /// <summary>
        /// Gets the collection of warnings.
        /// </summary>
        public IReadOnlyCollection<ExpressionError> Warnings => _warnings;

        protected HashSet<string> _errors = null;

        protected HashSet<ExpressionError> _warnings = null;

        protected SupportsConnectorErrors()
        {
        }

        protected SupportsConnectorErrors(string error, ExpressionError warning = default)
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

        internal void AddWarning(ExpressionError warning)
        {
            if (warning != null)
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
                foreach (ExpressionError warning in externalObject.Warnings)
                {
                    _warnings.Add(warning);
                }
            }

            return externalObject;
        }

        private void InitializeLists()
        {
            _errors ??= new HashSet<string>();
            _warnings ??= new HashSet<ExpressionError>();
        }
    }
}
