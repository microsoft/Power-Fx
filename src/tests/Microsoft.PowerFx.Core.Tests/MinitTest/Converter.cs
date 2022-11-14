// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Minit
{
    public class Converter
    {
        // Builtin Identifiers
        // PowerFx can have identifiers that represent filters. 
        // Refer to all events in the process 
        private const string AllEvents = "ProcessEvents";

        // Refer to all events in a given case. 
        private const string CaseEvents = "CaseEvents";

        // By default, all events have 3 fields builtin. 
        // They can have additional custom fields too ("Attributes")

        internal readonly RecordType _eventType = RecordType.Empty()            
            .Add("Duration", FormulaType.Number) // Logically, (Start-End)
            .Add("Start", FormulaType.DateTime)
            .Add("End", FormulaType.DateTime)
            .Add("Activity", FormulaType.String);
        
        internal readonly ReadOnlySymbolTable _builtinSymbols;
        internal readonly ISymbolSlot _slotAllEvents;

        public Converter()
        {
            var symTable = new SymbolTable();
            _slotAllEvents = symTable.AddVariable(AllEvents, _eventType.ToTable());

            _builtinSymbols = symTable;
        }

        public string Convert(string inputFx)
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            // Ensure we can parse and bind the expression. 
            var check = engine.Check(inputFx, symbolTable: _builtinSymbols);
            check.ThrowOnErrors();

            (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(check._binding);

            var visitor = new MinitVisitor();
            var ctx = new VisitorContext
            {
                _parent = this
            };
            var result = irnode.Accept(visitor, ctx);

            var output = result._miniExpr;
            return output;
        }

        // Add SumGroup, SumIf functions 
    }

    // Context mpassed between each visitor node. 
    public class VisitorContext
    {
        public Converter _parent;        
    }

    // return result from visitor method. 
    public class VisitorResult
    {
        public string _miniExpr;
    }

    // Walk an Power Fx tree and write out in target language 
    internal class MinitVisitor : IRNodeVisitor<VisitorResult, VisitorContext>
    {
        private static bool Equals(ISymbolSlot slotA, ISymbolSlot slotB)
        {
            if (slotA == null && slotB == null)
            {
                return true;
            }

            return slotA != null && !slotA.IsDisposed() &&
                slotB != null && !slotB.IsDisposed() &&
                slotA.SlotIndex == slotB.SlotIndex &&
                slotA.Owner == slotB.Owner;
        }

        public override VisitorResult Visit(TextLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(NumberLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(BooleanLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ColorLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(RecordNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ErrorNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(LazyEvalNode node, VisitorContext context)
        {
            // Common for all predicates, like arg1 in Sum:
            //   Sum(ProcessEvents, Duration)
            var ret = node.Child.Accept(this, context);
            return ret;
        }

        public override VisitorResult Visit(CallNode node, VisitorContext context)
        {
            var func = node.Function;
            
            // Sum(ProcessEvents, Duration())
            if (func.Name != "Sum")
            {
                throw new NotImplementedException($"Function {func.Name} is not implemented.");
            }

            string arg0 = null;
            if (node.Args[0] is ResolvedObjectNode node2)
            {
                if (node2.Value is ISymbolSlot slot)
                {
                    if (Equals(slot, context._parent._slotAllEvents))
                    {
                        arg0 = "ProcessEvents"; // Minit identifier. 
                    }
                }
            } 

            if (arg0 == null)
            {
                throw new NotImplementedException($"Unrecognized arg {node.Args[0].IRContext.SourceContext}");
            }

            var arg1 = node.Args[1].Accept(this, context);

            var ret = new VisitorResult
            {
                 _miniExpr = $"Sum({arg0}, {arg1._miniExpr})"
            };

            return ret;
        }

        public override VisitorResult Visit(BinaryOpNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(UnaryOpNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ScopeAccessNode node, VisitorContext context)
        {
            if (node.Value is ScopeAccessSymbol sym)
            {
                // Refers to a field on Arg1. 
                var name = sym.Name.Value;
                if (name == "Duration")
                {
                    // In minit, duration is a function. 
                    var ret = new VisitorResult
                    {
                        _miniExpr = "Duration()"
                    };
                    return ret;
                }
            }

            throw new NotImplementedException();
        }

        public override VisitorResult Visit(RecordFieldAccessNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ResolvedObjectNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(SingleColumnTableAccessNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ChainingNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(AggregateCoercionNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }
    }
}
