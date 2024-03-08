// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Information about a built-in function.
    /// </summary>
    [ThreadSafeImmutable]
    [DebuggerDisplay("{Name}")]
    public class FunctionInfo
    {
        internal readonly TexlFunction _fnc;

        internal FunctionInfo(TexlFunction fnc)
        {
            _fnc = fnc ?? throw new ArgumentNullException(nameof(fnc));
        }

        /// <summary>
        /// Name of the function.
        /// </summary>
        public string Name => _fnc.Name;

        /// <summary>
        /// Minimal arity of the function.
        /// </summary>
        public int MinArity => _fnc.MinArity;

        /// <summary>
        /// Maximal arity of the function.
        /// </summary>
        public int MaxArity => _fnc.MaxArity;

        /// <summary>
        /// Get a short description of the function.  
        /// </summary>
        [Obsolete("Use GetDescription() which takes a locale")]
        public string Description => _fnc.Description;

        /// <summary>
        /// Get a short description of the function.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public string GetDescription(CultureInfo culture = null)
        {
            culture ??= CultureInfo.InvariantCulture;
            return _fnc.GetDescription(culture.Name);
        }

        /// <summary>
        /// An optional URL for more help on this function. 
        /// </summary>
        public string HelpLink => _fnc.HelpLink;

        /// <summary>
        /// Get parameter names for the specific overload.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FunctionInfoSignature> Signatures 
        {
            get
            {
                var sigs = _fnc.GetSignatures();

                foreach (var paramList in sigs)
                {
                    yield return new FunctionInfoSignature(this, paramList);
                }
            }
        }

        public IEnumerable<string> GetCategoryNames(CultureInfo culture = null)
        {
            culture ??= CultureInfo.InvariantCulture;
            foreach (FunctionCategories category in Enum.GetValues(typeof(FunctionCategories)))
            {
                if ((_fnc.FunctionCategoriesMask & category) != 0)
                {
                    yield return category.GetLocalizedName(culture);
                }
            }
        }
    }
}
