// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    // This can cache the results of ReadOnlySymbolTable.Compose - and includes a safe 
    // invalidation strategy. 
    // Compose() is fast because it defers all work. But it will cache the results of that work,
    // (notably compuing TexlFunctionSets), so caching Compose is important to reuse those
    // other caches. 
    internal class ComposedSymbolTableCache
    {
        // A cached value from ReadOnlySymbolTable.Compose
        private ReadOnlySymbolTable _result;

        // The symbol tables that were composed for _result.
        private ReadOnlySymbolTable[] _list;

        // Determine if the cache should be invalidated. 
        // this will set _result to null to force a recompute.
        private void MaybeInvalidateCache(ReadOnlySymbolTable[] tables)
        {
            // Check cache invalidation.
            if (_list != null)
            {
                if (_list.Length != tables.Length)
                {
                    // Developer error - list that we cache on should be statically fixed. 
                    throw new InvalidOperationException($"List should be fixed");
                }
                else
                {
                    // Check if any instances changed.
                    // If it's the same symbol table instance, but the table has mutated, 
                    // then ROST.compose will already that via the VersionHash.              
                    for (int i = 0; i < tables.Length; i++)
                    {
                        if (!object.ReferenceEquals(_list[i], tables[i]))
                        {
                            _result = null;
                            break;
                        }
                    }
                }
            }
        }

        public ReadOnlySymbolTable GetComposedCached(params ReadOnlySymbolTable[] tables)
        {
            // This is a conceptually read-only operation, so it could be called on multiple threads. 
            // CA2002 - Lock(this) is safe since it's a private object that doesn't cross AD boundaries.
#pragma warning disable CA2002 // Do not lock on objects with weak identity
            lock (this)
            {
                MaybeInvalidateCache(tables);

                if (_result == null)
                {
                    var functionList = ReadOnlySymbolTable.Compose(tables);

                    _list = tables;
                    _result = functionList;
                }

                return _result;
            }
#pragma warning restore CA2002 // Do not lock on objects with weak identity
        }
    }
}
