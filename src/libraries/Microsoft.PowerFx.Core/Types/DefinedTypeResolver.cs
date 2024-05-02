// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
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
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    internal class DefinedTypeResolver
    {
        private readonly Dictionary<string, DefinedType> _typesDict;
        private readonly ReadOnlySymbolTable _globalSymbols;

        private readonly IEnumerable<string> _nodes;
        private readonly List<TopologicalSortEdge<string>> _edges;

        private readonly List<TexlError> _errors;

        // TODO: Add more future type names
        private static readonly ISet<string> _restrictedTypeNames = new HashSet<string> { "Record", "Table", "Currency" };

        private DefinedTypeResolver(IEnumerable<DefinedType> definedTypes, ReadOnlySymbolTable globalSymbols)
        {
            Contracts.AssertValue(globalSymbols);
            Contracts.AssertValue(definedTypes);
            Contracts.AssertAllValues(definedTypes);

            _errors = new List<TexlError>();
            _typesDict = new Dictionary<string, DefinedType>();
            _globalSymbols = globalSymbols;

            foreach (var dt in definedTypes)
            {
                var typeName = dt.Ident.Name.Value;

                // Have only valid type names to resolve.
                if (CheckTypeName(dt))
                {
                    _typesDict.Add(typeName, dt);
                }
            }

            // Build graph
            _nodes = _typesDict.Keys;
            _edges = new List<TopologicalSortEdge<string>>();

            foreach (var definedType in definedTypes)
            {
                // Get unresolved dependency names.
                var dependencies = DefinedTypeDependencyVisitor.FindDependencies(definedType.Type.TypeRoot, globalSymbols);

                foreach (var processFirst in dependencies)
                {
                    // Ensure no edges from non-existent node.
                    if (_typesDict.ContainsKey(processFirst))
                    {
                        _edges.Add(new TopologicalSortEdge<string>(processFirst, definedType.Ident.Name.Value));
                    }
                }
            }
        }

        private bool CheckTypeName(DefinedType dt)
        {
            Contracts.AssertValue(dt);

            var typeName = dt.Ident.Name;

            if (_restrictedTypeNames.Contains(typeName))
            {
                _errors.Add(new TexlError(dt.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrNamedType_InvalidTypeName, typeName.Value));
                return false;
            }

            if (_typesDict.ContainsKey(typeName) || ((INameResolver)_globalSymbols).LookupType(typeName, out var _))
            {
                _errors.Add(new TexlError(dt.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrNamedType_TypeAlreadyDefined, typeName.Value));
                return false;
            }

            return true;
        }

        private TypeResolverResult ResolveTypes()
        {
            var containsCycles = !TopologicalSort.TrySort(_nodes, _edges, out var resolveOrder, out var cycles);

            Contracts.AssertValue(resolveOrder);

            var definedTypeSymbolTable = new SymbolTable();

            var composedSymbols = ReadOnlySymbolTable.Compose(definedTypeSymbolTable, _globalSymbols);

            foreach (var typeName in resolveOrder)
            {
                PopType(typeName, out var currentType);

                var resolvedType = DTypeVisitor.Run(currentType.Type.TypeRoot, composedSymbols);

                if (resolvedType == DType.Invalid)
                {
                    _errors.Add(new TexlError(currentType.Type.TypeRoot, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, currentType.Ident.Name));
                    continue;
                }

                // To allow missing fields/columns
                if (resolvedType.IsTable || resolvedType.IsRecord)
                {
                    resolvedType.AreFieldsOptional = true;
                }

                var name = currentType.Ident.Name;
                definedTypeSymbolTable.AddType(name, FormulaType.Build(resolvedType));
            }

            // Add error for types with cycles and unreachable edges. 
            if (containsCycles)
            {
                foreach (var cyclicType in cycles)
                {
                    PopType(cyclicType, out var ct);
                    _errors.Add(new TexlError(ct.Type.TypeRoot, DocumentErrorSeverity.Severe, TexlStrings.ErrTypeLiteral_InvalidTypeDefinition, ct.Ident.Name));
                }
            }

            Contracts.Assert(_typesDict.Count == 0);

            return new TypeResolverResult(((INameResolver)definedTypeSymbolTable).NamedTypes, _errors);
        }

        // Safely get and remove a defined type.
        private void PopType(string typeName, out DefinedType type)
        {
            Contracts.AssertValue(typeName);
            Contracts.AssertNonEmpty(typeName);
            Contracts.Assert(_typesDict.ContainsKey(typeName));

            _typesDict.TryGetValue(typeName, out type);
            _typesDict.Remove(typeName);
        }

        // Resolve a given set of DefinedType ASTs to FormulaType.
        public static TypeResolverResult ResolveTypes(IEnumerable<DefinedType> definedTypes, ReadOnlySymbolTable typeNameResolver)
        {
            Contracts.AssertValue(typeNameResolver);
            Contracts.AssertValue(definedTypes);
            Contracts.AssertAllValues(definedTypes);

            var typeGraph = new DefinedTypeResolver(definedTypes, typeNameResolver);
            return typeGraph.ResolveTypes();
        }

        public static TypeResolverResult ResolveTypes(IEnumerable<DefinedType> definedTypes, INameResolver typeNameResolver)
        {
            Contracts.AssertValue(typeNameResolver);
            Contracts.AssertValue(definedTypes);
            Contracts.AssertAllValues(definedTypes);

            return ResolveTypes(definedTypes, ReadOnlySymbolTable.NewDefaultTypes(typeNameResolver.NamedTypes));
        }
    }
}
