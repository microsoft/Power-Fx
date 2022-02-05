// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Types.Enums;
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
        private readonly RecordType _parameters;

        public RecalcEngineResolver(
            RecalcEngine parent,
            PowerFxConfig powerFxConfig,
            RecordType parameters)
            : base(powerFxConfig.EnumStore.EnumSymbols, powerFxConfig.ExtraFunctions.Values.ToArray())
        {
            _parameters = parameters;
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

            var parameter = _parameters.MaybeGetFieldType(str);
            if (parameter != null)
            {
                var data = new ParameterData { ParameterName = str };
                var type = parameter._type;

                nameInfo = new NameLookupInfo(
                    BindKind.PowerFxResolvedObject,
                    type,
                    DPath.Root,
                    0,
                    data);
                return true;
            }

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
            else if (_powerFxConfig.EnvironmentSymbols.TryGetValue(name, out var symbol))
            {
                // Special case symbols
                if (symbol is IExternalOptionSet optionSet)
                {
                    nameInfo = new NameLookupInfo(
                        BindKind.OptionSet,
                        optionSet.Type,
                        DPath.Root,
                        0,
                        optionSet);

                    return true;
                }
                else
                {
                    throw new NotImplementedException($"{symbol.GetType().Name} not supported by {typeof(RecalcEngineResolver).Name}");
                }
            }

            return base.Lookup(name, out nameInfo, preferences);
        }

        public class ParameterData
        {
            public string ParameterName;
        }
    }
}
