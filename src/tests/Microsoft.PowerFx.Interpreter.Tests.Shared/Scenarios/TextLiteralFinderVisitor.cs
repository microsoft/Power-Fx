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
    public class TextLiteralFinderTests
    {
        [Fact]
        public void Test()
        {
            string formula = @"
                {
                    type: ""AdaptiveCard"",
                    body: [ //comment
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
$/body/1/text, first
$/footer/text, second
";

            var finder = new TextLiteralFinder();

            // Extract
            Dictionary<PropertyPath, string> locs = finder.Extract(formula);

            string actual = ToString(locs).Trim();

            Assert.Equal(expected.Trim(), actual);

            // Merge 
            var locs2 = new Dictionary<PropertyPath, string>
            {
                { PropertyPath.Parse("$/body/1/text"), "LOC1" },
                { PropertyPath.Parse("$/footer/text"), "LO\"C2" },
            };
            var newFormula = finder.Merge(formula, locs2);

            string expected2 = @"
                {
                    type: ""AdaptiveCard"",
                    body: [ //comment
                        {},
                        {
                            type: ""TextBlock"",
                            size: ""Medium"",
                            weight: ""Bolder"",
                            text: ""LOC1""
                        }
	                ],
                    footer : {
                        text : ""LO""""C2""
                    }
                }                 
";
            Assert.Equal(NormExpr(expected2), NormExpr(newFormula));
        }        

        // Normalize whitespace in an expression so we can compare them.
        private static string NormExpr(string expr)
        {
            var parse = Engine.Parse(expr);
            
            var normalized = parse.Root.ToString();
            return normalized;
        }

        private static string ToString(IReadOnlyDictionary<PropertyPath, string> locs)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kv in locs.OrderBy(x => x.Key.ToString()))
            {
                sb.Append(kv.Key.ToString());
                sb.Append(", ");
                sb.Append(kv.Value);
                sb.AppendLine();
            }

            string actual = sb.ToString();
            return actual;
        }
    }

    /// <summary>
    /// Helper for <see cref="TextLiteralFinder"/>.
    /// </summary>
    internal class TextLiteralFinderVisitor : TexlFunctionalVisitor<object, PropertyPath>
    {
        internal readonly Dictionary<PropertyPath, string> _localizations = new Dictionary<PropertyPath, string>();

        // Which propertyNames should get localized?
        private readonly IReadOnlySet<string> _keywords;

        public TextLiteralFinderVisitor(IReadOnlySet<string> keywords)
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
            // Expression can have calls - but we won't visit them.
            // So records within a call's arguments won't get translated. 
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

                        if (Merged != null && Merged.TryGetValue(childPath, out var newText))
                        {
                            var span = str.GetCompleteSpan();

                            var newText2 = '"' + StrLitToken.EscapeString(newText) + '"';
                            _replacements.Add(new KeyValuePair<Span, string>(span, newText2));
                        }

                        if (shouldLocalize)
                        {
                            var valueStr = str.Value;                            
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

        internal readonly List<KeyValuePair<Span, string>> _replacements = new List<KeyValuePair<Span, string>>();

        internal IReadOnlyDictionary<PropertyPath, string> Merged { get; init; }

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

    // Given an expression that *represents an object (records/tables)*,
    // extract the text literals for fields of known names. 
    // And allow merging back new (localized) text literals.
    public class TextLiteralFinder
    {
        public Dictionary<PropertyPath, string> Extract(string formula)
        {
            var parse = Engine.Parse(formula);

            var vis = new TextLiteralFinderVisitor(_propertiesToLocalize);            

            parse.Root.Accept(vis, PropertyPath.Root);

            return vis._localizations;
        }

        public string Merge(string formula, Dictionary<PropertyPath, string> localizations)
        {
            var parse = Engine.Parse(formula);

            var vis = new TextLiteralFinderVisitor(_propertiesToLocalize)
            {
                Merged = localizations
            };

            parse.Root.Accept(vis, PropertyPath.Root);

            var newFormula = ReplaceSpans(formula, vis._replacements);

            return newFormula;            
        }

        // Get schema from: https://adaptivecards.io/explorer/AdaptiveCard.html 
        private static readonly IReadOnlySet<string> _propertiesToLocalize = new HashSet<string>
        {
            "text"
        };

        // Get from Fx.Core: https://github.com/microsoft/Power-Fx/issues/1874
        private static string ReplaceSpans(string script, IEnumerable<KeyValuePair<Span, string>> worklist)
        {
            StringBuilder sb = new StringBuilder(script.Length);

            int index = 0;
            int lastLim = -1;

            foreach (KeyValuePair<Span, string> pair in worklist.OrderBy(kvp => kvp.Key.Min))
            {
                if (pair.Key.Min < lastLim)
                {
                    // Avoid corrupting the replacement.
                    throw new InvalidOperationException($"Post-processing failed: replacement span overlap");
                }

                sb.Append(script, index, pair.Key.Min - index);
                sb.Append(pair.Value);
                index = pair.Key.Lim;

                lastLim = pair.Key.Lim;
            }

            if (index < script.Length)
            {
                sb.Append(script, index, script.Length - index);
            }

            return sb.ToString();
        }
    }

    // Represent a path into an object. 
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
                sb.Append($"/{fieldName}");
            }
            else if (_idx is int idx)
            {
                sb.Append($"/{idx}");
            }
            else
            {
                sb.Append("/???");
            }
        }

        // parse from ToString
        public static PropertyPath Parse(string path)
        {
            var x = PropertyPath.Root;

            // Starts with "$"
            var parts = path.Split('/');

            if (parts[0] != "$")
            {
                throw new InvalidOperationException($"Path should be rooted with '$'");
            }

            foreach (var part in parts.Skip(1))
            {
                if (int.TryParse(part, out var idx))
                {
                    x = x.Index(idx);
                }
                else
                {
                    x = x.Field(part);
                }
            }

            return x;
        }
    }
}
