// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// <see cref="INameResolver"/> implementation for <see cref="RecalcEngine"/>.
    /// </summary>
    internal class RecalcEngineResolver : SimpleResolver
    {
        private readonly RecalcEngine _parent;
        private readonly PowerFxConfig _powerFxConfig;

        public RecalcEngineResolver(RecalcEngine parent, PowerFxConfig powerFxConfig)
            : base(powerFxConfig)
        {
            _parent = parent;
            _powerFxConfig = powerFxConfig;
        }

        public override bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
        {
            // Kinds of globals:
            // - global formula
            // - parameters 
            // - environment symbols

            var str = name.Value;

            if (_parent.Formulas.TryGetValue(str, out var fi))
            {
                var data = fi;
                var type = fi._type._type;

                nameInfo = new NameLookupInfo(
                    BindKind.PowerFxResolvedObject,
                    type,
                    DPath.Root,
                    0,
                    data);
                return true;
            }

            return base.Lookup(name, out nameInfo, preferences);
        }

        public override IDictionary<string, NameLookupInfo> VariableNames => _parent.Formulas.ToDictionary(kv => kv.Key, kv => new NameLookupInfo(BindKind.ScopeVariable, kv.Value._type._type, DPath.Root, 0, kv.Value));

        public class ParameterData
        {
            public string ParameterName;
        }
    }
}
