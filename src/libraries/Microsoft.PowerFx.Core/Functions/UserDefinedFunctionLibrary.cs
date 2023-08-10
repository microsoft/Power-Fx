// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class UserDefinedFunctionLibrary
    {
        private TexlFunctionSet _library;

        private readonly Dictionary<string, TexlBinding> _bindings;

        public Dictionary<string, List<TexlFunction>>.KeyCollection FunctionNames => _library.FunctionNames;

        public int Count => FunctionNames.Count();

        public UserDefinedFunctionLibrary()
        {
            _library = new TexlFunctionSet();
            _bindings = new Dictionary<string, TexlBinding>();
        }

        public bool TryAdd(UserDefinedFunction function, TexlBinding binding)
        {
            Contracts.AssertValue(function);

            if (_library.AnyWithName(function.Name))
            {
                return false;
            }

            _bindings.Add(function.Name, binding);

            return _library.Add(function) != null;
        }

        public bool TryGetBinding(string functionName, out TexlBinding binding) => _bindings.TryGetValue(functionName, out binding);

        public bool Contains(string name) => _library.FunctionNames.Contains(name);

        public void RemoveFunction(string functionName)
        {
            Contracts.AssertValue(functionName);

            _bindings.Remove(functionName);
            _library.RemoveAll(functionName);
        }

        public bool TryGetFunction(string name, out UserDefinedFunction function, bool localeInvariant = false)
        {
            Contracts.AssertNonEmpty(name);

            // Overloads are not supported in phase 1, so we are taking the first one
            function = Lookup(name, localeInvariant).FirstOrDefault() as UserDefinedFunction;

            return function != null;
        }

        public IEnumerable<TexlFunction> Lookup(string name, bool localeInvariant = false)
        {
            Contracts.AssertNonEmpty(name);

            var functions = localeInvariant
                                ? _library.WithInvariantName(name, DPath.Root)
                                : _library.WithName(name, DPath.Root);

            return functions;
        }

        public void Clear()
        {
            _library = new TexlFunctionSet();
            _bindings.Clear();
        }
    }
}
