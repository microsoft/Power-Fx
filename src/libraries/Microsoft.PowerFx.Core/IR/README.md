# Power Fx IR

## Goals

This aims to be a more refined Intermediate Representation (IR) that makes it easier for other backends to consume.  For example, we don’t want every backend to need to reimplement our coercion matrix.

For example:
-	Coercion matrix
-	Overload operator+  … is it for numbers, dates, etc?
-	And() could be either a operator (A && B) or a function call ( And(A,B)). This should get normalized.
-	‘As’ keyword – this is purely a symbol binding thing and shouldn’t have any impact on codegen.    ‘ThisRecord.Value’, ‘Value’,  and ‘… as X, X.Value’ should all be the same.

The most immediate consumer here is the new backends (notably SQL for CDS).  We’d like Canvas's JSTranslator to be able to consume it too; but that’s a more advanced goal.

A good IR should make it easy for other backends to consume and get correctly. For example, we now make coercion an explicit node and flatten the coercion matrix to a single enum so that backends can easily implement the various patterns (or throw not-impl), rather than require them to implement a complex coercion matrix.

Large portions of Canvas-Specific support is not present in this initial draft, but will be added over time as more features are added to Power Fx

## Specific Mappings of interest


- The AST's UnaryOpNode maps to the IR's UnaryOpNode
- Coercion marked by the binding maps to UnaryOpNode
- Enums are translated to their backing Literal values
- Operators that are also Functions, like And, Or, Power, Concatenate are represented by the functions
- Any function parameters that are not always evaluated are represented by Lambda nodes
- Scope access is handled by a ScopeSymbol associated with the function that creates the scope. It is referenced from a ScopeAccessNode that has a ScopeAccessSymbol pointing to the original ScopeSymbol



