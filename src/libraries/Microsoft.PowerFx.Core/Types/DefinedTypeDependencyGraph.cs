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
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    internal class DefinedTypeDependencyGraph
    {
        private readonly IEnumerable<DefinedType> _definedTypes;
        private readonly ReadOnlySymbolTable _globalSymbols;

        private readonly Dictionary<DefinedType, HashSet<string>> _typeWithDependency;
        private readonly Dictionary<string, HashSet<DefinedType>> _invertedDependency;
        private readonly Queue<DefinedType> _tsQueue;

        private readonly SymbolTable _definedTypeSymbolTable;

        private static readonly ISet<string> _restrictedTypeNames = new HashSet<string> { "Record" };

        internal Dictionary<DefinedType, HashSet<string>> UnresolvedTypes => _typeWithDependency;

        internal SymbolTable DefinedTypes => _definedTypeSymbolTable;

        public DefinedTypeDependencyGraph(IEnumerable<DefinedType> definedTypes, ReadOnlySymbolTable symbols) 
        {
            _definedTypes = definedTypes;
            _globalSymbols = symbols;
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
                var dependencies = DefinedTypeDependencyVisitor.Run(defType.Type.TypeRoot, _globalSymbols);

                _typeWithDependency.Add(defType, dependencies);

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

                if (!dependencies.Any())
                {
                    _tsQueue.Enqueue(defType);
                }
            }
        }

        // Topological sort to resolve types
        internal SymbolTable ResolveTypes(List<TexlError> errors)
        {
            var composedSymbols = ReadOnlySymbolTable.Compose(_definedTypeSymbolTable, _globalSymbols);

            while (_tsQueue.Any())
            {
                var currentType = _tsQueue.Dequeue();
                _typeWithDependency.Remove(currentType);

                if (!CheckTypeName(currentType, composedSymbols, errors))
                {
                    continue;
                }

                var resolvedType = DTypeVisitor.Run(currentType.Type.TypeRoot, composedSymbols);
                if (resolvedType == DType.Invalid)
                {
                    errors.Add(new TexlError(currentType.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition));
                    continue;
                }

                var name = currentType.Ident.Name.Value;
                _definedTypeSymbolTable.AddType(name, FormulaType.Build(resolvedType));

                if (_invertedDependency.TryGetValue(name, out var typeDependents))
                {
                    foreach (var typeDependent in typeDependents)
                    {
                        if (_typeWithDependency.TryGetValue(typeDependent, out var unresolvedTypes))
                        {
                            unresolvedTypes.Remove(name);

                            if (!unresolvedTypes.Any())
                            {
                                _tsQueue.Enqueue(typeDependent);
                            }
                        }
                    }
                }
            }

            return _definedTypeSymbolTable;
        }

        private bool CheckTypeName(DefinedType dt, INameResolver symbols,  List<TexlError> errors)
        {
            var typeName = dt.Ident.Name;

            if (_restrictedTypeNames.Contains(typeName))
            {
                // Todo: Change error message
                errors.Add(new TexlError(dt.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition));
                return false;
            }

            if (symbols.LookupType(typeName, out var _))
            {
                // Todo: Change error message
                errors.Add(new TexlError(dt.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition));
                return false;
            }

            return true;
        }
    }
}
