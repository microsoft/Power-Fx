// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public abstract class TabularRecordType : RecordType
    {
        private readonly IEnumerable<string> _fieldNames; 

        public TabularRecordType(DisplayNameProvider displayNameProvider, TableParameters tableParameters)
            : base(displayNameProvider, tableParameters)
        {
            _type = DType.AttachDataSourceInfo(_type, new InternalTableParameters(this, displayNameProvider, tableParameters));            
            _fieldNames = displayNameProvider.LogicalToDisplayPairs.Select(pair => pair.Key.Value).ToList();
        }

        public override IEnumerable<string> FieldNames => _fieldNames;

        public sealed override bool TryGetFieldType(string name, out FormulaType type) => TryGetFieldType(name, false, out type);

        public abstract bool TryGetFieldType(string name, bool ignorelationship, out FormulaType type);

        public abstract ColumnCapabilitiesDefinition GetColumnCapability(string fieldName);       
    }
}
