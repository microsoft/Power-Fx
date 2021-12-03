#nullable enable
namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.PowerFx.Core.Syntax.Nodes;
    using Microsoft.PowerFx.Core.Utils;

    /// <summary>
    /// Collects all the referenced state as <see cref="DPath"/> objects
    /// for a given formula without a binder. This can be used to determine
    /// the order in which to resolve formulas.
    /// </summary>
    /// <remarks>
    ///  We collect and return a list of paths for any given expression.
    ///  Certain functions create multiple valid state paths from a single node.
    ///  Example: If (A, B, C).Name references both B.Name and C.Name depending on the value of A.
    ///  
    ///  Context changing operations such as With, As, or Sum, their name are stored 
    ///  in the dictionary that is input to the visitor. As an example, 
    /// </remarks>
    internal class FindDependenciesVisitor : TexlFunctionalVisitor<DPath[], ImmutableDictionary<DName, DPath[]>>
    {
        public static HashSet<DPath> Run(TexlNode node)
        {
            var visitor = new FindDependenciesVisitor();
            var topLevelPaths = node.Accept(visitor, ImmutableDictionary<DName, DPath[]>.Empty);
            foreach (var path in topLevelPaths)
            {
                visitor.Paths.Add(path);
            }

            return visitor.Paths;
        }

        public HashSet<DPath> Paths { get; } = new HashSet<DPath>();

        public override DPath[] Visit(ErrorNode node, ImmutableDictionary<DName, DPath[]> context) => Array.Empty<DPath>();

        public override DPath[] Visit(BlankNode node, ImmutableDictionary<DName, DPath[]> context) => Array.Empty<DPath>();

        public override DPath[] Visit(BoolLitNode node, ImmutableDictionary<DName, DPath[]> context) => Array.Empty<DPath>();

        public override DPath[] Visit(StrLitNode node, ImmutableDictionary<DName, DPath[]> context) => Array.Empty<DPath>();

        public override DPath[] Visit(NumLitNode node, ImmutableDictionary<DName, DPath[]> context) => Array.Empty<DPath>();

        public override DPath[] Visit(FirstNameNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            DName name = node.Ident.Name;
            if (context.TryGetValue(name, out DPath[]? mappedPaths))
            {
                // we are inside an expression such as With({ A = Foo }, A). We should 
                // return "Foo" instead of "A".
                return mappedPaths;
            }
            else
            {
                // it's possible that due to a With clause, scope has been added to the root namespace.
                // this allows names to be ambiguous until they are bound to the type context
                // example: With(A, B). It's ambiguous whether B is a top-level identifier or a property of A
                if (!context.TryGetValue(default, out DPath[]? topLevelIdentifiers))
                {
                    topLevelIdentifiers = Array.Empty<DPath>();
                }

                var result = new DPath[topLevelIdentifiers.Length + 1];
                for (int i = 0; i < topLevelIdentifiers.Length; i++)
                {
                    result[i] = topLevelIdentifiers[i].Append(name);
                }

                result[result.Length - 1] = DPath.Root.Append(name);
                return result;
            }
        }

        public override DPath[] Visit(ParentNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            // TODO: Parent seems unavailable in the PowerFX demo environment
            // >> A.Parent.B
            // Errors: Error 2-8: Unexpected characters. The formula contains 'Ident' where 'Parent' is expected.
            // Error 2 - 8: Unexpected characters. Characters are used in the formula in an unexpected way.

            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(SelfNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            // TODO: Self seems unavailable in the PowerFX demo environment
            // >> Self.A
            // Errors: Error 0-4: Name isn't valid. This identifier isn't recognized.
            // Error 4 - 6: Invalid use of '.'
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(ReplaceableNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            // TODO: Replacement of tokens seems unavailable in the PowerFX demo environment
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(DottedNameNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            var paths = node.Left.Accept(this, context);
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = paths[i].Append(node.Right.Name);
            }

            return paths;
        }

        public override DPath[] Visit(UnaryOpNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            Collect(node.Child.Accept(this, context));
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(BinaryOpNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            Collect(node.Left.Accept(this, context));
            Collect(node.Right.Accept(this, context));
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(VariadicOpNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            CollectList(node.Children, context);
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(CallNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            var functionName = node.Head.Name;
            // add special cases
            switch (functionName.Value.ToLowerInvariant())
            {
                case "if": return VisitIfCall(node, context);
                case "with": return VisitWithCall(node, context);
                default:
                    Collect(node.Args.Accept(this, context));
                    return Array.Empty<DPath>();
            }
        }

        public override DPath[] Visit(ListNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            CollectList(node.Children, context);
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(RecordNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            CollectList(node.Children, context);
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(TableNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            CollectList(node.Children, context);
            return Array.Empty<DPath>();
        }

        public override DPath[] Visit(AsNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            // XYZ as Something
            // if the left part has anything, it now is assigned to the right side. 
            // this node is specific to certain known functions. If we directly visit As,
            // it means we have a made a mistake, or encountered a future feature, or the
            // expression contains an error.
            return Array.Empty<DPath>();
        }

        private DPath[] VisitIfCall(CallNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-if
            // If(Condition, Then)
            // If(Condition, Then, Else)
            // If(Condition, Then, Condition2, Then2)
            // If(Condition, Then, Condition2, Then2, Default)
            var argumentCount = node.Args.Count;
            if (argumentCount == 1)
            {
                // invalid count;
                return Array.Empty<DPath>();
            }

            var result = new List<DPath>();
            for (int conditionIndex = 0, thenIndex = 1; thenIndex < argumentCount; conditionIndex += 2, thenIndex += 2)
            {
                var condition = node.Args.Children[conditionIndex];
                Collect(condition.Accept(this, context));

                var then = node.Args.Children[thenIndex];
                result.AddRange(then.Accept(this, context));
            }

            if (argumentCount % 2 == 1) // odd argument count, there is a default or else case
            {
                var defaultCase = node.Args.Children[argumentCount - 1];
                result.AddRange(defaultCase.Accept(this, context));
            }

            return result.ToArray();
        }

        private DPath[] VisitWithCall(CallNode node, ImmutableDictionary<DName, DPath[]> context)
        {
            // With(Record, Formula)
            // Record – Required.The record to be acted upon. For names values, use the inline syntax
            // { name1: value1, name2: value2, ... }
            // Formula – Required.The formula to evaluate for Record.The formula can reference any of
            // the fields of Record directly as a record scope.

            if (node.Args.Count != 2)
            {
                // we need 2 arguments.
                return Array.Empty<DPath>();
            }

            var scope = node.Args.Children[0];
            var formula = node.Args.Children[1];

            var scopeContext = GetScopeContext(scope, context);
            return formula.Accept(this, scopeContext);
        }

        private ImmutableDictionary<DName, DPath[]> GetScopeContext(TexlNode scope, ImmutableDictionary<DName, DPath[]> outerContext)
            => scope switch
        {
            //  With ({ x: Foo, y: Bar }, x + 1)
            RecordNode record => GetScopeContext(record, outerContext),

            //  With (A as x, x + 1)
            AsNode asNode => GetScopeContext(asNode, outerContext),

            //  With (A as x, x + 1)
            FirstNameNode firstName => GetScopeContext(firstName, outerContext),

            // not implemented for other situations
            _ => outerContext,
        };

        private ImmutableDictionary<DName, DPath[]> GetScopeContext(FirstNameNode node, ImmutableDictionary<DName, DPath[]> outerContext)
        {
            return outerContext.Add(default, node.Accept(this, outerContext));
        }

        private ImmutableDictionary<DName, DPath[]> GetScopeContext(AsNode node, ImmutableDictionary<DName, DPath[]> outerContext)
        {
            var result = node.Left.Accept(this, outerContext);
            return outerContext.SetItem(node.Right.Name, result);
        }

        private ImmutableDictionary<DName, DPath[]> GetScopeContext(RecordNode node, ImmutableDictionary<DName, DPath[]> outerContext)
        {
            var newContext = outerContext;
            for (int i = 0; i < node.Ids.Length; i++)
            {
                var identifier = node.Ids[i];
                var value = node.Children[i];
                var paths = value.Accept(this, outerContext);
                newContext = newContext.SetItem(identifier.Name, paths);
            }

            return newContext;
        }

        private void CollectList(IEnumerable<TexlNode> nodes, ImmutableDictionary<DName, DPath[]> context)
        {
            foreach (var node in nodes)
            {
                Collect(node.Accept(this, context));
            }
        }

        private void Collect(DPath[] paths)
        {
            foreach (var path in paths)
            {
                Paths.Add(path);
            }
        }
    }
}
