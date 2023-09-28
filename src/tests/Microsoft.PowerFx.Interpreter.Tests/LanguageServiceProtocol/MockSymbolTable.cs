// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

/** 
 * Just a small handy mock of symbol table to be able to customize binder to compute different token types.
 * Might not be 100% correct but it works and allows testing against different token types.
 * Meant to be used for semantic tokens related tests.
*/
internal class MockSymbolTable : ReadOnlySymbolTable
{
    public void Add(string name, NameLookupInfo info)
    {
        _variables.Add(name, info);
    }

    public void AddRecord(string name, BindKind? type = null, params TypedName[] keyValues)
    {
        type ??= BindKind.Data;
        var recordType = DType.CreateRecord(keyValues);
        Add(name, new NameLookupInfo(type.Value, recordType, DPath.Root, 0, displayName: DName.MakeValid(name, out _)));
    }

    public void AddControlAsAggregateType(string name, params TypedName[] props)
    {
        AddRecord(name, BindKind.Control, props);
    }

    public void AddControlAsControlType(string name)
    {
        var controlType = new DType(DKind.Control);
        var controlInfo = new NameLookupInfo(BindKind.Control, controlType, DPath.Root, 0);
        Add(name, controlInfo);
    }

    public TypedName GetLookupInfoAsTypedName(string name)
    {
        var validName = DName.MakeValid(name, out _);
        if (TryLookup(validName, out var lookupInfo))
        {
            return new TypedName(lookupInfo.Type, validName);
        }

        return new TypedName(DType.Unknown, validName);
    }

    internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
    {
        return _variables.TryGetValue(name.Value, out nameInfo);
    }
}
