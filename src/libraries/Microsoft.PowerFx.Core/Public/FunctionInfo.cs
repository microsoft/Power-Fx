// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;

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
    }

    /// <summary>
    /// Represent a possible signature for a <see cref="FunctionInfo"/>.
    /// </summary>
    [DebuggerDisplay("{DebugToString()}")]
    [ThreadSafeImmutable]
    public class FunctionInfoSignature
    {
        private readonly FunctionInfo _parent;
        private readonly IReadOnlyCollection<TexlStrings.StringGetter> _paramNames;

        /// <summary>
        /// Number of parameters in this signature. 
        /// </summary>
        public int Length => _paramNames.Count;

        public string[] GetParameterNames(CultureInfo culture = null)
        {
            culture ??= CultureInfo.InvariantCulture;
            var localeName = culture.Name;

            List<string> result = new List<string>();

            foreach (var param in _paramNames)
            {
                var name = param(localeName);
                result.Add(name);
            }

            return result.ToArray();
        }

        // Keep this debug-only, since there are too many possible formats to pick just one. 
        internal string DebugToString(CultureInfo culture = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_parent.Name);
            sb.Append('(');

            var sep = string.Empty;
            foreach (var param in GetParameterNames(culture))
            {
                sb.Append(sep);
                sb.Append(param);
                sep = ", ";
            }

            sb.Append(')');

            return sb.ToString();
        }
        
        internal FunctionInfoSignature(FunctionInfo parent, TexlStrings.StringGetter[] paramNames)
        {
            _parent = parent;
            _paramNames = paramNames;
        }
    }
}
