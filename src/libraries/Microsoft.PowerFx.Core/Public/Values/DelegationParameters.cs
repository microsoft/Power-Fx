// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// For use with <see cref="TableValue"/> and delegation.
    /// Describe delegation parameters such as filters, column selection, and sorting. 
    /// </summary>
    public abstract class DelegationParameters
    {
        /// <summary>
        /// Which features does this use - so we can determine if we support it. 
        /// </summary>
        public abstract DelegationParameterFeatures Features { get; }

        /// <summary>
        /// Throw if the parameters use features outside the feature list. 
        /// </summary>
        /// <param name="allowedFeatures"></param>
        /// <exception cref="InvalidOperationException">Thrown if this has uses features outside the allowedFeatures parameter.</exception>
        public void EnsureOnlyFeatures(DelegationParameterFeatures allowedFeatures)
        {
            var flags = this.Features;

            // Fail if params have extra features not supported in Odata. 
            if ((allowedFeatures & flags) != allowedFeatures)
            {
                throw new InvalidOperationException($"Delegations needs ({flags}), but only supports ({allowedFeatures}).");
            }
        }

        public abstract string GetOdataFilter();

        // 0 columns means return all columns.
        public virtual IReadOnlyCollection<string> GetColumns()
        {
            return new string[0];
        }

        public int? Top { get; set; }

        // Other odata fetchers?         
    }

    /// <summary>
    /// Let <see cref="DelegationParameters"/> describe which virtuals it implements. 
    /// This allows consumers to check for unsupported features. 
    /// </summary>
    [Flags]
    public enum DelegationParameterFeatures
    {
        Filter,
        Top,
        Sort,
        Columns
    }
}
