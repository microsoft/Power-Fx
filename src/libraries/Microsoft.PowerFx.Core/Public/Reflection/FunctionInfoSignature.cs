// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx
{
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
            var parameters = GetParameters(culture);
            return Array.ConvertAll(parameters, p => p.Name);
        }

        // The invariant parameter name is used to lookup the parameter help in the resource. 
        // TexlFunction will point to a Resource for the parameter name:
        //  <data name="SequenceArg1"><value>records</></>
        //
        // And then that name (in en-us) is used as another resource key to get parameter help description:
        //  <data name="AboutSequence_records" >
        //     <value>Number of records in the single column table with name "Value". Maximum 50,000.</value>
        //  </data>
        internal static string GetInvariantParameterName(TexlStrings.StringGetter p)
        {
            string invariantParamName = p("en-US");
            return invariantParamName;
        }

        public ParameterInfoSignature[] GetParameters(CultureInfo culture = null)
        {
            culture ??= CultureInfo.InvariantCulture;
            var localeName = culture.Name;

            List<ParameterInfoSignature> result = new List<ParameterInfoSignature>();

            foreach (var p in _paramNames)
            {
                string unalterableName = p(localeName);

                string invariantParamName = GetInvariantParameterName(p);

                // We should allow passing in culture to get the help text. 
                // https://github.com/microsoft/Power-Fx/issues/2216
                _parent._fnc.TryGetParamDescription(invariantParamName, out var description);

                result.Add(new ParameterInfoSignature
                {
                    Name = unalterableName,
                    Description = description
                });
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