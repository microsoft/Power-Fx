// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;
using YamlDotNet.Core.Tokens;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Demonstrate mutation example using IUntypedObject
    public class Widget2Tests
    {
        [Fact]
        public void Test()
        {
            string formula = @"
                {
                    type: ""AdaptiveCard"",
                    body: [
                        {},
                        {
                            type: ""TextBlock"",
                            size: ""Medium"",
                            weight: ""Bolder"",
                            text: ""first""
                        }
	                ],
                    footer : {
                        text : ""second""
                    }
                } 
                ";
            string expected = @"
$.body[1].text, first
$.footer.text, second
";

            var finder = new Finder();
            var locs = finder.Extract(formula);

            StringBuilder sb = new StringBuilder();
            foreach (var kv in locs.OrderBy(x => x.Key.ToString()))
            {
                sb.Append(kv.Key.ToString());
                sb.Append(", ");
                sb.Append(kv.Value);
                sb.AppendLine();
            }

            string actual = sb.ToString().Trim();
            Assert.Equal(expected.Trim(), actual);
        }        
    }

    public class WidgetVisitor : TexlFunctionalVisitor<object, PropertyPath>
    {
        internal readonly Dictionary<PropertyPath, string> _localizations = new Dictionary<PropertyPath, string>();

        // Which propertyNames should get localized?
        private readonly IReadOnlySet<string> _keywords;

        public WidgetVisitor(IReadOnlySet<string> keywords)
        {
            _keywords = keywords ?? throw new ArgumentNullException(nameof(keywords));
        }

        #region nop
        public override object Visit(TypeLiteralNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(ErrorNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(BlankNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(BoolLitNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(StrLitNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(NumLitNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(DecLitNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(FirstNameNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(ParentNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(SelfNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(StrInterpNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(DottedNameNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(UnaryOpNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(BinaryOpNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(VariadicOpNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(CallNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(AsNode node, PropertyPath context)
        {
            return null;
        }
        #endregion // nop

        public override object Visit(ListNode node, PropertyPath context)
        {
            return null;
        }

        public override object Visit(RecordNode node, PropertyPath context)
        {
            var ids = node.Ids;

            for (int i = 0; i < ids.Count; i++)
            {
                var key = ids[i];
                var value = node.ChildNodes[i];

                if (key is Identifier id)
                {
                    var childPath = context.Field(id.Name);

                    if (value is StrLitNode str)
                    {
                        bool shouldLocalize = _keywords.Contains(id.Name);

                        if (shouldLocalize)
                        {
                            var valueStr = str.Value;

                            // $$$ Same thing that adds should also replace...
                            _localizations.Add(childPath, valueStr);
                        }
                    }
                    else
                    {
                        value.Accept(this, childPath);
                    }
                }
            }

            return null;
        }

        public override object Visit(TableNode node, PropertyPath context)
        {
            var children = node.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];

                var childPath = context.Index(i);
                child.Accept(this, childPath);
            }

            return null;
        }
    }

    public class Finder
    {
        public Dictionary<PropertyPath, string> Extract(string formula)
        {
            var parse = Engine.Parse(formula);

            var vis = new WidgetVisitor(_propertiesToLocalize);            

            parse.Root.Accept(vis, PropertyPath.Root);

            return vis._localizations;
        }

        public string Merge(string original, Dictionary<PropertyPath, string> localizations)
        {
            throw new NotImplementedException();
        }

        // Get schema from: https://adaptivecards.io/explorer/AdaptiveCard.html 
        private static readonly IReadOnlySet<string> _propertiesToLocalize = new HashSet<string>
        {
            "text"
        };
    }

    // a
    // a.b
    // a[0].c
    public class PropertyPath
    {
        public static PropertyPath Root = new PropertyPath();
        
        // Empty 
        private PropertyPath() 
        {
        }

        private readonly PropertyPath _parent;

        // sring or int 
        private readonly object _idx;        

        private PropertyPath(PropertyPath parent, object idx)
        {
            _parent = parent;
            _idx = idx;
        }

        public PropertyPath Field(string name)
        {
            return new PropertyPath(this, name);
        }

        public PropertyPath Index(int idx)
        {
            return new PropertyPath(this, idx);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_idx, _parent?.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj is PropertyPath path)
            {
                return this.Equals(path);
            }

            return false;
        }

        public bool Equals(PropertyPath path)
        {
            if (object.ReferenceEquals(this, path))
            {
                return true;
            }

            if (path == null) 
            {
                return false; 
            }

            return path._idx.Equals(this._idx) &&
                path._parent.Equals(this._parent);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        private void ToString(StringBuilder sb)
        {
            if (_parent == null)
            {
                // Root 
                sb.Append("$");
                return;
            }

            _parent.ToString(sb);

            if (_idx is string fieldName)
            {
                sb.Append($".{fieldName}");
            }
            else if (_idx is int idx)
            {
                sb.Append($"[{idx}]");
            }
            else
            {
                sb.Append(".???");
            }
        }
    }
}
