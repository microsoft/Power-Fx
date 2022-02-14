// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    // A container for the symbols/scopes that are available for
    // use within a given .visit method on EvalVisitor, allowing
    // the EvalVisitor to be scope-agnostic. The implementation
    // of call stack semantics in our language can leverage the
    // existing C# call stack semantics by making new instances
    // of this class in the bodies of the various .visit methods
    internal sealed class SymbolContext
    {
        public SymbolContext(ScopeSymbol currentScope, Dictionary<int, IScope> scopeValues)
        {
            CurrentScope = currentScope;
            ScopeValues = scopeValues;
        }

        public ScopeSymbol CurrentScope { get; } = null;

        public Dictionary<int, IScope> ScopeValues { get; } = null;

        public static SymbolContext New()
        {
            return new SymbolContext(null, new Dictionary<int, IScope>());
        }

        public static SymbolContext NewTopScope(ScopeSymbol topScope, RecordValue ruleScope)
        {
            return new SymbolContext(topScope, new Dictionary<int, IScope>() { { topScope.Id, new RecordScope(ruleScope) } });
        }

        public SymbolContext WithScope(ScopeSymbol currentScope)
        {
            return new SymbolContext(currentScope, ScopeValues);
        }

        public SymbolContext WithScopeValues(IScope scopeValues)
        {
            var newScopeValues = new Dictionary<int, IScope>(ScopeValues)
            {
                [CurrentScope.Id] = scopeValues
            };
            return new SymbolContext(CurrentScope, newScopeValues);
        }

        public SymbolContext WithScopeValues(RecordValue scopeValues)
        {
            return WithScopeValues(new RecordScope(scopeValues));
        }

        public FormulaValue GetScopeVar(ScopeSymbol scope, string name)
        {
            var record = ScopeValues[scope.Id];
            return record.Resolve(name); // Binder should ensure success.
        }
    }
}
