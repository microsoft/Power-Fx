// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    internal class DefinedTypeDependencyGraph
    {
        private readonly IEnumerable<DefinedType> _definedTypes;
        private readonly ReadOnlySymbolTable _globalTypes;

        // Stores unresolvedType -> its dependencies
        private readonly Dictionary<DefinedType, HashSet<string>> _typeWithDependency;

        // Stores unresolvedType -> its dependendents
        private readonly Dictionary<string, HashSet<DefinedType>> _invertedDependency;

        // Queue with types ready to be resolved
        private readonly Queue<DefinedType> _tsQueue;

        private readonly SymbolTable _definedTypeSymbolTable;

        // TODO: Add more future type names
        private static readonly ISet<string> _restrictedTypeNames = new HashSet<string> { "Record", "Table", "Currency" };

        internal Dictionary<DefinedType, HashSet<string>> UnresolvedTypes => _typeWithDependency;

        internal INameResolver DefinedTypeSymbols => ReadOnlySymbolTable.Compose(_definedTypeSymbolTable, _globalTypes);

        public DefinedTypeDependencyGraph(IEnumerable<DefinedType> definedTypes, INameResolver globalSymbols) 
        {
            _definedTypes = definedTypes;
            _globalTypes = ReadOnlySymbolTable.NewDefaultTypes(globalSymbols?.DefinedTypes);

            _typeWithDependency = new Dictionary<DefinedType, HashSet<string>>();
            _invertedDependency = new Dictionary<string, HashSet<DefinedType>>();
            _tsQueue = new Queue<DefinedType>();

            _definedTypeSymbolTable = new SymbolTable();

            Build();
        }

        // Build type dependency graph to perform topological sort and resolve types
        private void Build()
        {
            foreach (var defType in _definedTypes)
            {
                var name = defType.Ident.Name.Value;
                var dependencies = DefinedTypeDependencyVisitor.FindDependencies(defType.Type.TypeRoot, _globalTypes);

                // Establish unresolvedType -> its dependencies
                _typeWithDependency.Add(defType, dependencies);

                // Establish unresolvedType -> its dependendents
                foreach (var typeSource in dependencies)
                {
                    if (_invertedDependency.TryGetValue(typeSource, out var typeDependents))
                    {
                        typeDependents.Add(defType);
                    }
                    else
                    {
                        var deps = new HashSet<DefinedType> { defType };
                        _invertedDependency.Add(typeSource, deps);
                    }
                }

                // Enqueue if no unresolved dependencies
                if (!dependencies.Any())
                {
                    _tsQueue.Enqueue(defType);
                }
            }
        }

        // Topological sort to resolve types
        internal IEnumerable<UserDefinedType> ResolveTypes(List<TexlError> errors)
        {
            var composedSymbols = ReadOnlySymbolTable.Compose(_definedTypeSymbolTable, _globalTypes);

            var userDefinedTypes = new List<UserDefinedType>();

            while (_tsQueue.Any())
            {
                var currentType = _tsQueue.Dequeue();
                _typeWithDependency.Remove(currentType);

                // Check if typename is restricted or already defined
                if (!CheckTypeName(currentType, composedSymbols, errors))
                {
                    continue;
                }

                var resolvedType = DTypeVisitor.Run(currentType.Type.TypeRoot, composedSymbols);
                if (resolvedType == DType.Invalid)
                {
                    errors.Add(new TexlError(currentType.Type.TypeRoot, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, currentType.Ident.Name));
                    continue;
                }

                // To allow missing fields/columns
                if (resolvedType.IsTable || resolvedType.IsRecord)
                {
                    resolvedType.AreFieldsOptional = true;
                }

                var name = currentType.Ident.Name;
                _definedTypeSymbolTable.AddType(name, FormulaType.Build(resolvedType));
                userDefinedTypes.Add(new UserDefinedType(name, FormulaType.Build(resolvedType), currentType.Type));

                AdjustResolveDependencies(name);
            }

            return userDefinedTypes;
        }

        // Removes resolved type name from dependencies and queue any name that is ready to be resolved.
        private void AdjustResolveDependencies(DName name)
        {
            if (_invertedDependency.TryGetValue(name, out var typeDependents))
            {
                foreach (var typeDependent in typeDependents)
                {
                    if (_typeWithDependency.TryGetValue(typeDependent, out var unresolvedTypes))
                    {
                        // Remove depenpendency since the type name is resolved
                        unresolvedTypes.Remove(name.Value);

                        // Enqueue if no unresolved dependencies
                        if (!unresolvedTypes.Any())
                        {
                            _tsQueue.Enqueue(typeDependent);
                        }
                    }
                }
            }
        }

        private bool CheckTypeName(DefinedType dt, INameResolver symbols,  List<TexlError> errors)
        {
            var typeName = dt.Ident.Name;

            if (_restrictedTypeNames.Contains(typeName))
            {
                errors.Add(new TexlError(dt.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrNamedType_InvalidTypeName, typeName.Value));
                return false;
            }

            if (symbols.LookupType(typeName, out var _))
            {
                errors.Add(new TexlError(dt.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrNamedType_TypeAlreadyDefined, typeName.Value));
                return false;
            }

            return true;
        }
    }
}
