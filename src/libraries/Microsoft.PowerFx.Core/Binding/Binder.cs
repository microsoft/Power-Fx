// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.App.Components;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Binding.BinderUtils;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Microsoft.PowerFx.Core.Binding
{
    internal sealed partial class TexlBinding
    {
        private readonly IBinderGlue _glue;

        // The parse tree for this binding.
        public readonly TexlNode Top;

        // Path of entity where this formula was bound.
        public readonly DPath EntityPath;

        // Name of entity where this formula was bound.
        public readonly DName EntityName;

        // The name resolver associated with this binding.
        public readonly INameResolver NameResolver;

        // The local scope resolver associated with this binding.
        public readonly IExternalRuleScopeResolver LocalRuleScopeResolver;

        // Maps IDs to nodes
        private readonly TexlNode[] _nodeMap;

        // Maps Ids to Types, where the Id is an index in the array.
        private readonly DType[] _typeMap;
        private readonly DType[] _coerceMap;

        // Maps Ids to whether the node/subtree is async or not. A subtree
        // that has async components is itself async, so the async aspect of an expression
        // propagates up the parse tree all the way to the root.
        private readonly bool[] _asyncMap;

        // Used to mark nodes as delegatable or not.
        private readonly BitArray _isDelegatable;

        // Used to mark node as pageable or not.
        private readonly BitArray _isPageable;

        // Extra information. We have a slot for each node.
        // Maps Ids to Info, where the Id is an index in the array.
        private readonly object[] _infoMap;

        // Call nodes which do not appear in the source code
        // For example, $"" (interpolated string) maps to a call to Concatenate
        private readonly CallNode[] _compilerGeneratedCallNodes;

        private readonly IDictionary<int, IList<FirstNameInfo>> _lambdaParams;

        // Whether a node is stateful or has side effects or is contextual or is constant.
        private readonly BitArray _isStateful;
        private readonly BitArray _hasSideEffects;
        private readonly BitArray _isContextual;
        private readonly BitArray _isConstant;
        private readonly BitArray _isSelfContainedConstant;

        // Whether a node supports its rowscoped param exempted from delegation check. e.g. The 3rd argument in AddColumns function
        private readonly BitArray _supportsRowScopedParamDelegationExempted;

        // Whether a node is an ECS excempt lambda. e.g. filter lambdas
        private readonly BitArray _isEcsExcemptLambda;

        // Whether a node is inside delegable function but its value only depends on the outer scope(higher than current scope)
        private readonly BitArray _isBlockScopedConstant;

        // Property to which current rule is being bound to. It could be null in the absence of NameResolver.
        private readonly IExternalControlProperty _property;
        private readonly IExternalControl _control;

        // Whether a node is scoped to app or not. Used by translator for component scoped variable references.
        private readonly BitArray _isAppScopedVariable;

        // The scope use sets associated with all the nodes.
        private readonly ScopeUseSet[] _lambdaScopingMap;

        private List<DType> _typesNeedingMetadata;
        private bool _hasThisItemReference;
        private readonly bool _forceUpdateDisplayNames;

        // If As is used at the toplevel, contains the rhs value of the As operand;
        private DName _renamedOutputAccessor;

        // A mapping of node ids to lists of variable identifiers that are to have been altered in runtime prior
        // to the node of the id, e.g. Set(x, 1); Set(y, x + 1);
        // All child nodes of the chaining operator that come after Set(x, 1); will have a variable weight that
        // contains x
        private readonly ImmutableHashSet<string>[] _volatileVariables;

        // This is set when a First Name node or child First Name node contains itself in its variable weight
        // and can be read by the back end to determine whether it may generate code that lifts or caches an
        // expression
        private readonly BitArray _isUnliftable;

        public bool HasLocalScopeReferences { get; private set; }

        public ErrorContainer ErrorContainer { get; } = new ErrorContainer();

        /// <summary>
        /// The maximum number of selects in a table that will be included in data call.
        /// </summary>
        public const int MaxSelectsToInclude = 100;

        /// <summary>
        /// Default name used to access a Lambda scope.
        /// </summary>
        internal static DName ThisRecordDefaultName => new DName("ThisRecord");

        public Features Features { get; }

        // Property to which current rule is being bound to. It could be null in the absence of NameResolver.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0025:Use expression body for properties", Justification = "n/a")]
        public IExternalControlProperty Property
        {
            get
            {
#if DEBUG
                if (NameResolver != null && NameResolver.CurrentEntity is IExternalControl && NameResolver.CurrentProperty.IsValid && NameResolver.TryGetCurrentControlProperty(out var currentProperty))
                {
                    Contracts.Assert(_property == currentProperty);
                }
#endif
                return _property;
            }
        }

        // Control to which current rule is being bound to. It could be null in the absence of NameResolver.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0025:Use expression body for properties", Justification = "n/a")]
        public IExternalControl Control
        {
            get
            {
#if DEBUG
                if (NameResolver != null && NameResolver.CurrentEntity != null && NameResolver.CurrentEntity is IExternalControl)
                {
                    Contracts.Assert(NameResolver.CurrentEntity == _control);
                }
#endif
                return _control;
            }
        }

        // We store this information here instead of on TabularDataSourceInfo is that this information should change as the rules gets edited
        // and we shouldn't store information about the fields user tried but didn't end up in final rule.
        public DataSourceToQueryOptionsMap QueryOptions { get; }

        public bool UsesGlobals { get; private set; }

        public bool UsesAliases { get; private set; }

        public bool UsesScopeVariables { get; private set; }

        public bool UsesScopeCollections { get; private set; }

        public bool UsesThisItem { get; private set; }

        public bool UsesResources { get; private set; }

        public bool UsesOptionSets { get; private set; }

        public bool UsesViews { get; private set; }

        public bool TransitionsFromAsyncToSync { get; private set; }

        public int IdLim => _infoMap == null ? 0 : _infoMap.Length;

        public DType ResultType => GetType(Top);

        // The coerced type of the rule after name-mapping.
        public DType CoercedToplevelType { get; internal set; }

        public bool HasThisItemReference => _hasThisItemReference || UsesThisItem;

        public bool HasParentItemReference { get; private set; }

        public bool HasSelfReference { get; private set; }

        public BindingConfig BindingConfig { get; }

        public CheckTypesContext CheckTypesContext { get; }

        public IExternalDocument Document => NameResolver?.Document;

        public bool AffectsAliases { get; private set; }

        public bool AffectsScopeVariable { get; private set; }

        public bool AffectsScopeVariableName { get; private set; }

        public bool AffectsTabularDataSources { get; private set; } = false;

        public bool HasControlReferences { get; private set; }

        /// <summary>
        /// UsedControlProperties  is for processing edges required for indirect control property references.
        /// </summary>
        public HashSet<DName> UsedControlProperties { get; } = new HashSet<DName>();

        public bool HasSelectFunc { get; private set; }

        public bool HasReferenceToAttachment { get; private set; }

        public bool IsGloballyPure => !(UsesGlobals || UsesThisItem || UsesAliases || UsesScopeVariables || UsesResources || UsesScopeCollections) && IsPure(Top);

        public bool IsCurrentPropertyPageable => Property != null && Property.SupportsPaging;

        public bool CurrentPropertyRequiresDefaultableReferences => Property != null && Property.RequiresDefaultablePropertyReferences;

        public bool ContainsAnyPageableNode => _isPageable.Cast<bool>().Any(isPageable => isPageable);

        public IExternalEntityScope EntityScope => NameResolver?.EntityScope;

        public string TopParentUniqueId => EntityPath.IsRoot ? string.Empty : EntityPath[0].Value;

        // Stores tokens that need replacement (Display Name -> Logical Name) for serialization
        // Replace Nodes (Display Name -> Logical Name) for serialization
        public IList<KeyValuePair<Token, string>> NodesToReplace { get; }

        public bool UpdateDisplayNames { get; }

        /// <summary>
        /// The fields of this type are defined as valid keywords for this binding.
        /// </summary>
        public DType ContextScope { get; }

        private TexlBinding(
            IBinderGlue glue,
            IExternalRuleScopeResolver scopeResolver,
            DataSourceToQueryOptionsMap queryOptions,
            TexlNode node,
            INameResolver resolver,
            BindingConfig bindingConfig,
            DType ruleScope,
            bool updateDisplayNames = false,
            bool forceUpdateDisplayNames = false,
            IExternalRule rule = null,
            Features features = Features.None)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(bindingConfig);
            Contracts.AssertValueOrNull(resolver);
            Contracts.AssertValueOrNull(scopeResolver);

            BindingConfig = bindingConfig;
            QueryOptions = queryOptions;
            Features = features;
            _glue = glue;
            Top = node;
            NameResolver = resolver;
            LocalRuleScopeResolver = scopeResolver;

            var idLim = node.Id + 1;

            _typeMap = new DType[idLim];
            _coerceMap = new DType[idLim];
            for (var i = 0; i < idLim; ++i)
            {
                _typeMap[i] = DType.Invalid;
                _coerceMap[i] = DType.Invalid;
            }

            CoercedToplevelType = DType.Invalid;
            _nodeMap = new TexlNode[idLim];
            _infoMap = new object[idLim];
            _compilerGeneratedCallNodes = new CallNode[idLim];
            _asyncMap = new bool[idLim];
            _lambdaParams = new Dictionary<int, IList<FirstNameInfo>>(idLim);
            _isStateful = new BitArray(idLim);
            _hasSideEffects = new BitArray(idLim);
            _isAppScopedVariable = new BitArray(idLim);
            _isContextual = new BitArray(idLim);
            _isConstant = new BitArray(idLim);
            _isSelfContainedConstant = new BitArray(idLim);
            _lambdaScopingMap = new ScopeUseSet[idLim];
            _isDelegatable = new BitArray(idLim);
            _isPageable = new BitArray(idLim);
            _isEcsExcemptLambda = new BitArray(idLim);
            _supportsRowScopedParamDelegationExempted = new BitArray(idLim);
            _isBlockScopedConstant = new BitArray(idLim);
            _hasThisItemReference = false;
            _renamedOutputAccessor = default;

            _volatileVariables = new ImmutableHashSet<string>[idLim];
            _isUnliftable = new BitArray(idLim);

            HasParentItemReference = false;

            ContextScope = ruleScope;
            BinderNodeMetadataArgTypeVisitor = new BinderNodesMetadataArgTypeVisitor(this, resolver, ruleScope, BindingConfig.UseThisRecordForRuleScope, features);
            HasReferenceToAttachment = false;
            NodesToReplace = new List<KeyValuePair<Token, string>>();
            UpdateDisplayNames = updateDisplayNames;
            _forceUpdateDisplayNames = forceUpdateDisplayNames;
            HasLocalScopeReferences = false;
            TransitionsFromAsyncToSync = false;
            Rule = rule;
            if (resolver != null)
            {
                EntityPath = resolver.CurrentEntityPath;
                EntityName = resolver.CurrentEntity == null ? default : resolver.CurrentEntity.EntityName;
            }

            resolver?.TryGetCurrentControlProperty(out _property);
            _control = resolver?.CurrentEntity as IExternalControl;

            CheckTypesContext = new CheckTypesContext(
                features,
                resolver,
                entityName: EntityName,
                propertyName: Property?.InvariantName ?? string.Empty,
                isEnhancedDelegationEnabled: Document?.Properties?.EnabledFeatures?.IsEnhancedDelegationEnabled ?? false,
                allowsSideEffects: bindingConfig.AllowsSideEffects);
        }

        // Binds a Texl parse tree.
        // * resolver provides the name context used to bind names to globals, resources, etc. This may be null.
        public static TexlBinding Run(
            IBinderGlue glue,
            IExternalRuleScopeResolver scopeResolver,
            DataSourceToQueryOptionsMap queryOptionsMap,
            TexlNode node,
            INameResolver resolver,
            BindingConfig bindingConfig,
            bool updateDisplayNames = false,
            DType ruleScope = null,
            bool forceUpdateDisplayNames = false,
            IExternalRule rule = null,
            Features features = Features.None)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValueOrNull(resolver);

            var txb = new TexlBinding(glue, scopeResolver, queryOptionsMap, node, resolver, bindingConfig, ruleScope, updateDisplayNames, forceUpdateDisplayNames, rule: rule, features: features);
            var vis = new Visitor(txb, resolver, ruleScope, bindingConfig.UseThisRecordForRuleScope, features);
            vis.Run();

            // Determine if a rename has occured at the top level
            if (txb.Top is AsNode asNode)
            {
                txb._renamedOutputAccessor = txb.GetInfo(asNode).AsIdentifier;
            }

            return txb;
        }

        public static TexlBinding Run(
            IBinderGlue glue,
            TexlNode node,
            INameResolver resolver,
            BindingConfig bindingConfig,
            bool updateDisplayNames = false,
            DType ruleScope = null,
            bool forceUpdateDisplayNames = false,
            IExternalRule rule = null,
            Features features = Features.None)
        {
            return Run(glue, null, new DataSourceToQueryOptionsMap(), node, resolver, bindingConfig, updateDisplayNames, ruleScope, forceUpdateDisplayNames, rule, features);
        }

        public static TexlBinding Run(
            IBinderGlue glue,
            TexlNode node,
            INameResolver resolver,
            BindingConfig bindingConfig,
            DType ruleScope,
            Features features = Features.None)
        {
            return Run(glue, null, new DataSourceToQueryOptionsMap(), node, resolver, bindingConfig, false, ruleScope, false, null, features);
        }

        public void WidenResultType()
        {
            SetType(Top, DType.Error);
            ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, Top, TexlStrings.ErrTypeError);
        }

        public DType GetType(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(_typeMap[node.Id].IsValid);

            return _typeMap[node.Id];
        }

        /// <summary>
        /// Checks that the node is associated with this binding. This is critical so that node IDs are valid.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool IsNodeValid(TexlNode node)
        {
            Contracts.AssertValue(node);

            if (node.Id >= _nodeMap.Length)
            {
                return false;
            }

            var nodeById = _nodeMap[node.Id];
            return ReferenceEquals(node, nodeById);
        }

        private void SetType(TexlNode node, DType type)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(type.IsValid);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(_typeMap[node.Id] == null || !_typeMap[node.Id].IsValid || type.IsError);

            _nodeMap[node.Id] = node;
            _typeMap[node.Id] = type;
        }

        private void SetContextual(TexlNode node, bool isContextual)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(isContextual || !_isContextual.Get(node.Id));

            _isContextual.Set(node.Id, isContextual);
        }

        private void SetConstant(TexlNode node, bool isConstant)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(isConstant || !_isConstant.Get(node.Id));

            _isConstant.Set(node.Id, isConstant);
        }

        private void SetSelfContainedConstant(TexlNode node, bool isConstant)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(isConstant || !_isSelfContainedConstant.Get(node.Id));

            _isSelfContainedConstant.Set(node.Id, isConstant);
        }

        private void SetSideEffects(TexlNode node, bool hasSideEffects)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(hasSideEffects || !_hasSideEffects.Get(node.Id));

            _hasSideEffects.Set(node.Id, hasSideEffects);
        }

        private void SetStateful(TexlNode node, bool isStateful)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(isStateful || !_isStateful.Get(node.Id));

            _isStateful.Set(node.Id, isStateful);
        }

        private void SetAppScopedVariable(FirstNameNode node, bool isAppScopedVariable)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.Assert(isAppScopedVariable || !_isAppScopedVariable.Get(node.Id));

            _isAppScopedVariable.Set(node.Id, isAppScopedVariable);
        }

        public bool IsAppScopedVariable(FirstNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            return _isAppScopedVariable.Get(node.Id);
        }

        /// <summary>
        /// See documentation for <see cref="GetVolatileVariables"/> for more information.
        /// </summary>
        /// <param name="node">
        /// Node to which volatile variables are being added.
        /// </param>
        /// <param name="variables">
        /// The variables that are to be added to the list associated with <paramref name="node"/>.
        /// </param>
        private void AddVolatileVariables(TexlNode node, ImmutableHashSet<string> variables)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _volatileVariables.Length);

            var volatileVariables = _volatileVariables[node.Id] ?? ImmutableHashSet<string>.Empty;
            _volatileVariables[node.Id] = volatileVariables.Union(variables);
        }

        /// <summary>
        /// See documentation for <see cref="GetVolatileVariables"/> for more information.
        /// </summary>
        /// <param name="node">
        /// Node whose liftability will be altered by this invocation.
        /// </param>
        /// <param name="value">
        /// The value that the node's liftability should assume by the invocation of this method.
        /// </param>
        private void SetIsUnliftable(TexlNode node, bool value)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isUnliftable.Length);

            _isUnliftable[node.Id] = value;
        }

        private bool SupportsServerDelegation(CallNode node)
        {
            Contracts.AssertValue(node);

            var info = GetInfo(node).VerifyValue();
            var function = info.Function;
            if (function == null)
            {
                return false;
            }

            var isServerDelegatable = function.IsServerDelegatable(node, this);
            LogTelemetryForFunction(function, node, this, isServerDelegatable);
            return isServerDelegatable;
        }

        private bool SupportsPaging(FirstNameNode node)
        {
            Contracts.AssertValue(node);

            var info = GetInfo(node).VerifyValue();
            if (info.Data is IExternalPageableSymbol pageableSymbol && pageableSymbol.IsPageable)
            {
                return true;
            }

            // One To N Relationships are pagable using nextlinks
            if (info.Kind == BindKind.DeprecatedImplicitThisItem && (GetType(node).ExpandInfo?.IsTable ?? false))
            {
                return true;
            }

            return false;
        }

        private bool SupportsDelegation(FirstNameNode node)
        {
            Contracts.AssertValue(node);

            var info = GetInfo(node).VerifyValue();
            return info.Data is IExternalDelegatableSymbol delegableSymbol && delegableSymbol.IsDelegatable;
        }

        private bool SupportsPaging(TexlNode node)
        {
            Contracts.AssertValue(node);

            switch (node.Kind)
            {
                case NodeKind.FirstName:
                    return SupportsPaging(node.AsFirstName());
                case NodeKind.DottedName:
                    return SupportsPaging(node.AsDottedName());
                case NodeKind.Call:
                    return SupportsPaging(node.AsCall());
                default:
                    Contracts.Assert(false, "This should only be called for FirstNameNode, DottedNameNode and CallNode.");
                    return false;
            }
        }

        private bool SupportsPaging(CallNode node)
        {
            Contracts.AssertValue(node);

            var info = GetInfo(node);
            return info?.Function?.SupportsPaging(node, this) ?? false;
        }

        private bool TryGetEntityInfo(DottedNameNode node, out IExpandInfo info)
        {
            Contracts.AssertValue(node);

            info = null;
            var dottedNameNode = node.AsDottedName();
            if (dottedNameNode == null)
            {
                return false;
            }

            info = GetInfo(dottedNameNode)?.Data as IExpandInfo;
            return info != null;
        }

        private bool TryGetEntityInfo(FirstNameNode node, out IExpandInfo info)
        {
            Contracts.AssertValue(node);

            info = null;
            var firstNameNode = node.AsFirstName();
            if (firstNameNode == null)
            {
                return false;
            }

            info = GetInfo(firstNameNode)?.Data as IExpandInfo;
            return info != null;
        }

        private bool TryGetEntityInfo(CallNode node, out IExpandInfo info)
        {
            Contracts.AssertValue(node);

            info = null;
            var callNode = node.AsCall();
            if (callNode == null)
            {
                return false;
            }

            // It is possible for function to be null here if it referred to
            // a service function from a service we are in the process of
            // deregistering.
            return GetInfo(callNode).VerifyValue().Function?.TryGetEntityInfo(node, this, out info) ?? false;
        }

        internal IExternalRule Rule { get; }

        // When getting projections from a chain rule, ensure that the projection belongs to the same DS as the one we're operating on (using match param)
        internal bool TryGetDataQueryOptions(TexlNode node, bool forCodegen, out DataSourceToQueryOptionsMap tabularDataQueryOptionsMap)
        {
            Contracts.AssertValue(node);

            if (node.Kind == NodeKind.As)
            {
                node = node.AsAsNode().Left;
            }

            if (node.Kind == NodeKind.Call)
            {
                if (node.AsCall().Args.Children.Length == 0)
                {
                    tabularDataQueryOptionsMap = null;
                    return false;
                }

                node = node.AsCall().Args.Children[0];

                // Call nodes may have As nodes as the lhs, make sure query options are pulled from the lhs of the as node
                if (node.Kind == NodeKind.As)
                {
                    node = node.AsAsNode().Left;
                }
            }

            if (!Rule.TexlNodeQueryOptions.ContainsKey(node.Id))
            {
                tabularDataQueryOptionsMap = null;
                return false;
            }

            TexlNode topNode = null;
            foreach (var top in TopChain)
            {
                if (!node.InTree(top))
                {
                    continue;
                }

                topNode = top;
                break;
            }

            Contracts.AssertValue(topNode);

            if (node.Kind == NodeKind.FirstName
                && Rule.TexlNodeQueryOptions.Count > 1)
            {
                if (!(Rule.Document.GlobalScope.GetTabularDataSource(node.AsFirstName().Ident.Name) is IExternalTabularDataSource tabularDs))
                {
                    tabularDataQueryOptionsMap = Rule.TexlNodeQueryOptions[node.Id];
                    return true;
                }

                tabularDataQueryOptionsMap = new DataSourceToQueryOptionsMap();
                tabularDataQueryOptionsMap.AddDataSource(tabularDs);

                foreach (var x in Rule.TexlNodeQueryOptions)
                {
                    if (topNode.MinChildID > x.Key || x.Key > topNode.Id)
                    {
                        continue;
                    }

                    var qo = x.Value.GetQueryOptions(tabularDs);

                    if (qo == null)
                    {
                        continue;
                    }

                    tabularDataQueryOptionsMap.GetQueryOptions(tabularDs).Merge(qo);
                }

                return true;
            }
            else
            {
                tabularDataQueryOptionsMap = Rule.TexlNodeQueryOptions[node.Id];
                return true;
            }
        }

        private static IExternalControl GetParentControl(ParentNode parent, INameResolver nameResolver)
        {
            Contracts.AssertValue(parent);
            Contracts.AssertValueOrNull(nameResolver);

            if (nameResolver == null || nameResolver.CurrentEntity == null)
            {
                return null;
            }

            if (!(nameResolver.CurrentEntity is IExternalControl) || !nameResolver.LookupParent(out var lookupInfo))
            {
                return null;
            }

            return lookupInfo.Data as IExternalControl;
        }

        private static IExternalControl GetSelfControl(SelfNode self, INameResolver nameResolver)
        {
            Contracts.AssertValue(self);
            Contracts.AssertValueOrNull(nameResolver);

            if (nameResolver == null || nameResolver.CurrentEntity == null)
            {
                return null;
            }

            if (!nameResolver.LookupSelf(out var lookupInfo))
            {
                return null;
            }

            return lookupInfo.Data as IExternalControl;
        }

        private bool IsDataComponentDataSource(NameLookupInfo lookupInfo)
        {
            return lookupInfo.Kind == BindKind.Data &&
                _glue.IsComponentDataSource(lookupInfo.Data);
        }

        private bool IsDataComponentDefinition(NameLookupInfo lookupInfo)
        {
            return lookupInfo.Kind == BindKind.Control &&
                   _glue.IsDataComponentDefinition(lookupInfo.Data);
        }

        private bool IsDataComponentInstance(NameLookupInfo lookupInfo)
        {
            return lookupInfo.Kind == BindKind.Control &&
                   _glue.IsDataComponentInstance(lookupInfo.Data);
        }

        private IExternalControl GetDataComponentControl(DottedNameNode dottedNameNode, INameResolver nameResolver, TexlVisitor visitor)
        {
            Contracts.AssertValue(dottedNameNode);
            Contracts.AssertValueOrNull(nameResolver);
            Contracts.AssertValueOrNull(visitor);

            if (nameResolver == null || !(dottedNameNode.Left is FirstNameNode lhsNode))
            {
                return null;
            }

            if (!nameResolver.LookupGlobalEntity(lhsNode.Ident.Name, out var lookupInfo) ||
                (!IsDataComponentDataSource(lookupInfo) &&
                !IsDataComponentDefinition(lookupInfo) &&
                !IsDataComponentInstance(lookupInfo)))
            {
                return null;
            }

            if (GetInfo(lhsNode) == null)
            {
                lhsNode.Accept(visitor);
            }

            var lhsInfo = GetInfo(lhsNode);
            if (lhsInfo?.Data is IExternalControl dataCtrlInfo)
            {
                return dataCtrlInfo;
            }

            if (lhsInfo?.Kind == BindKind.Data &&
                _glue.TryGetCdsDataSourceByBind(lhsInfo.Data, out var info))
            {
                return info;
            }

            return null;
        }

        private DPath GetFunctionNamespace(CallNode node, TexlVisitor visitor)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(visitor);

            var leftNode = (node.HeadNode as DottedNameNode)?.Left;
            var ctrlInfo = leftNode switch
            {
                ParentNode parentNode => GetParentControl(parentNode, NameResolver),
                SelfNode selfNode => GetSelfControl(selfNode, NameResolver),
                FirstNameNode firstNameNode => GetDataComponentControl(node.HeadNode.AsDottedName(), NameResolver, visitor),
                _ => null,
            };

            return ctrlInfo != null
                ? DPath.Root.Append(new DName(ctrlInfo.DisplayName))
                : node.Head.Namespace;
        }

        internal bool TryGetDataQueryOptions(out DataSourceToQueryOptionsMap tabularDataQueryOptionsMap)
        {
            return TryGetDataQueryOptions(Top, false, out tabularDataQueryOptionsMap);
        }

        internal IEnumerable<string> GetDataQuerySelects(TexlNode node)
        {
            if (!Document.Properties.EnabledFeatures.IsProjectionMappingEnabled)
            {
                return Enumerable.Empty<string>();
            }

            if (!TryGetDataQueryOptions(node, true, out var tabularDataQueryOptionsMap))
            {
                return Enumerable.Empty<string>();
            }

            var currNodeQueryOptions = tabularDataQueryOptionsMap.GetQueryOptions();

            if (currNodeQueryOptions.Count() == 0)
            {
                return Enumerable.Empty<string>();
            }

            if (currNodeQueryOptions.Count() == 1)
            {
                var ds = currNodeQueryOptions.First().TabularDataSourceInfo;

                if (!ds.IsSelectable)
                {
                    return Enumerable.Empty<string>();
                }

                var ruleQueryOptions = Rule.Binding.QueryOptions.GetQueryOptions(ds);
                if (ruleQueryOptions != null)
                {
                    foreach (var nodeQO in Rule.TexlNodeQueryOptions)
                    {
                        var nodeQOSelects = nodeQO.Value.GetQueryOptions(ds)?.Selects;
                        ruleQueryOptions.AddSelectMultiple(nodeQOSelects);
                    }

                    ruleQueryOptions.AddRelatedColumns();

                    if (ruleQueryOptions.HasNonKeySelects(Document?.Properties?.UserFlags?.EnforceSelectPropagationLimit ?? false))
                    {
                        return ruleQueryOptions.Selects;
                    }
                }
                else
                {
                    if (ds.QueryOptions.HasNonKeySelects(Document?.Properties?.UserFlags?.EnforceSelectPropagationLimit ?? false))
                    {
                        ds.QueryOptions.AddRelatedColumns();
                        return ds.QueryOptions.Selects;
                    }
                }
            }

            return Enumerable.Empty<string>();
        }

        internal IEnumerable<string> GetExpandQuerySelects(TexlNode node, string expandEntityLogicalName)
        {
            if (Document.Properties.EnabledFeatures.IsProjectionMappingEnabled
                && TryGetDataQueryOptions(node, true, out var tabularDataQueryOptionsMap))
            {
                var currNodeQueryOptions = tabularDataQueryOptionsMap.GetQueryOptions();

                foreach (var qoItem in currNodeQueryOptions)
                {
                    foreach (var expandQueryOptions in qoItem.Expands)
                    {
                        if (expandQueryOptions.Value.ExpandInfo.Identity == expandEntityLogicalName)
                        {
                            if (!expandQueryOptions.Value.SelectsEqualKeyColumns() &&
                                (!(Document?.Properties?.UserFlags?.EnforceSelectPropagationLimit ?? false) || expandQueryOptions.Value.Selects.Count() <= MaxSelectsToInclude))
                            {
                                return expandQueryOptions.Value.Selects;
                            }
                            else
                            {
                                return Enumerable.Empty<string>();
                            }
                        }
                    }
                }
            }

            return Enumerable.Empty<string>();
        }

        public bool TryGetEntityInfo(TexlNode node, out IExpandInfo info)
        {
            Contracts.AssertValue(node);

            switch (node.Kind)
            {
                case NodeKind.DottedName:
                    return TryGetEntityInfo(node.AsDottedName(), out info);
                case NodeKind.FirstName:
                    return TryGetEntityInfo(node.AsFirstName(), out info);
                case NodeKind.Call:
                    return TryGetEntityInfo(node.AsCall(), out info);
                default:
                    info = null;
                    return false;
            }
        }

        public bool HasExpandInfo(TexlNode node)
        {
            Contracts.AssertValue(node);

            object data;
            switch (node.Kind)
            {
                case NodeKind.DottedName:
                    data = GetInfo(node.AsDottedName())?.Data;
                    break;
                case NodeKind.FirstName:
                    data = GetInfo(node.AsFirstName())?.Data;
                    break;
                default:
                    data = null;
                    break;
            }

            return (data != null) && (data is IExpandInfo);
        }

        internal bool TryGetDataSourceInfo(TexlNode node, out IExternalDataSource dataSourceInfo)
        {
            Contracts.AssertValue(node);

            var kind = node.Kind;

            switch (kind)
            {
                case NodeKind.Call:
                    var callNode = node.AsCall().VerifyValue();
                    var callFunction = GetInfo(callNode)?.Function;
                    if (callFunction != null)
                    {
                        return callFunction.TryGetDataSource(callNode, this, out dataSourceInfo);
                    }

                    break;
                case NodeKind.FirstName:
                    var firstNameNode = node.AsFirstName().VerifyValue();
                    dataSourceInfo = GetInfo(firstNameNode)?.Data as IExternalDataSource;
                    return dataSourceInfo != null;
                case NodeKind.DottedName:
                    IExpandInfo info;
                    if (TryGetEntityInfo(node.AsDottedName(), out info))
                    {
                        dataSourceInfo = info.ParentDataSource;
                        return dataSourceInfo != null;
                    }

                    break;
                case NodeKind.As:
                    return TryGetDataSourceInfo(node.AsAsNode().Left, out dataSourceInfo);
                default:
                    break;
            }

            dataSourceInfo = null;
            return false;
        }

        private bool SupportsPaging(DottedNameNode node)
        {
            Contracts.AssertValue(node);

            if (HasExpandInfo(node) && SupportsPaging(node.Left))
            {
                return true;
            }

            return TryGetEntityInfo(node, out var entityInfo) && entityInfo.IsTable;
        }

        public void CheckAndMarkAsDelegatable(CallNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            if (SupportsServerDelegation(node))
            {
                _isDelegatable.Set(node.Id, true);

                // Delegatable calls are async as well.
                FlagPathAsAsync(node);
            }
        }

        public void CheckAndMarkAsDelegatable(AsNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            if (_isDelegatable[node.Left.Id])
            {
                _isDelegatable.Set(node.Id, true);

                // Mark this as async, as this may result in async invocation.
                FlagPathAsAsync(node);
            }
        }

        public void CheckAndMarkAsPageable(CallNode node, TexlFunction func)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);
            Contracts.AssertValue(func);

            // Server delegatable call always returns a pageable object.
            if (func.SupportsPaging(node, this))
            {
                _isPageable.Set(node.Id, true);
            }
            else
            {
                // If we are transitioning from pageable call node to non-pageable node then it results in an
                // async call. So mark the path as async if current node is non-pageable with pageable child.
                // This also means that we will need an error context
                var args = node.Args.Children;
                if (args.Any(cnode => IsPageable(cnode)))
                {
                    FlagPathAsAsync(node);
                    TransitionsFromAsyncToSync = true;
                }
            }
        }

        public void CheckAndMarkAsPageable(FirstNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            if (SupportsPaging(node))
            {
                _isPageable.Set(node.Id, true);

                // Mark this as async, as this may result in async invocation.
                FlagPathAsAsync(node);

                // Pageable nodes are also stateful as data is always pulled from outside.
                SetStateful(node, isStateful: true);
            }
        }

        public void CheckAndMarkAsPageable(AsNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            if (_isPageable[node.Left.Id])
            {
                _isPageable.Set(node.Id, true);

                // Mark this as async, as this may result in async invocation.
                FlagPathAsAsync(node);

                // Pageable nodes are also stateful as data is always pulled from outside.
                SetStateful(node, isStateful: true);
            }
        }

        public void CheckAndMarkAsPageable(DottedNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            if (SupportsPaging(node))
            {
                _isPageable.Set(node.Id, true);

                // Mark this as async, as this may result in async invocation.
                FlagPathAsAsync(node);

                // Pageable nodes are also stateful as data is always pulled from outside.
                SetStateful(node, isStateful: true);
            }
        }

        public void CheckAndMarkAsDelegatable(FirstNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            if (SupportsDelegation(node))
            {
                _isDelegatable.Set(node.Id, true);

                // Mark this as async, as this may result in async invocation.
                FlagPathAsAsync(node);

                // Pageable nodes are also stateful as data is always pulled from outside.
                SetStateful(node, isStateful: true);
            }
        }

        public void CheckAndMarkAsDelegatable(DottedNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _typeMap.Length);

            if (SupportsPaging(node))
            {
                _isDelegatable.Set(node.Id, true);

                // Mark this as async, as this may result in async invocation.
                FlagPathAsAsync(node);

                // Pageable nodes are also stateful as data is always pulled from outside.
                SetStateful(node, isStateful: true);
            }
        }

        public bool IsDelegatable(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isDelegatable.Length);

            return _isDelegatable.Get(node.Id);
        }

        public bool IsPageable(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isPageable.Length);

            return _isPageable.Get(node.Id);
        }

        public bool HasSideEffects(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _hasSideEffects.Length);

            return _hasSideEffects.Get(node.Id);
        }

        public bool IsContextual(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isContextual.Length);

            return _isContextual.Get(node.Id);
        }

        public bool IsConstant(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isConstant.Length);

            return _isConstant.Get(node.Id);
        }

        public bool IsSelfContainedConstant(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isSelfContainedConstant.Length);

            return _isSelfContainedConstant.Get(node.Id);
        }

        /// <summary>
        /// A node's "volatile variables" are the names whose values may at runtime have be modified at some
        /// point before the node to which these variables pertain is executed.
        ///
        /// e.g. <code>Set(w, 1); Set(x, w); Set(y, x); Set(z, y);</code>
        /// The call node Set(x, w); will have an entry in volatile variables containing just "w", Set(y, x); will
        /// have [w, x], and Set(z, y); will have [w, x, y].
        ///
        /// <see cref="TexlFunction.GetIdentifierOfModifiedValue"/> reports which variables may be
        /// changed by a call node, and they are recorded when the call node is analyzed and a reference to
        /// its TexlFunction is acquired. They are propagated to subsequent nodes in the variadic operator as
        /// the children of the variadic node are being accepted by the visitor.
        ///
        /// When the children of the variadic expression are visited, the volatile variables are transferred to the
        /// children's children, and so on and so forth, in a manner obeying that which is being commented.
        /// As the tree is descended, the visitor may encounter a first name node that will receive itself among
        /// the volatile variables of its parent. In such a case, neither this node nor any of its ancestors up to
        /// the root of the chained node may be lifted during code generation.
        ///
        /// The unliftability propagates back to the ancestors during the post visit traversal of the tree, and is
        /// ultimately read by the code generator when it visits these nodes and may attempt to lift their
        /// expressions.
        /// </summary>
        /// <param name="node">
        /// The node of which volatile variables are being requested.
        /// </param>
        /// <returns>
        /// A list containing the volatile variables of <paramref name="node"/>.
        /// </returns>
        private ImmutableHashSet<string> GetVolatileVariables(TexlNode node)
        {
            Contracts.AssertValue(node);

            return _volatileVariables[node.Id] ?? ImmutableHashSet<string>.Empty;
        }

        public bool IsFullRecordRowScopeAccess(TexlNode node)
        {
            return TryGetFullRecordRowScopeAccessInfo(node, out _);
        }

        public bool TryGetFullRecordRowScopeAccessInfo(TexlNode node, out FirstNameInfo firstNameInfo)
        {
            Contracts.CheckValue(node, nameof(node));
            firstNameInfo = null;

            if (!(node is DottedNameNode dottedNameNode))
            {
                return false;
            }

            if (!(dottedNameNode.Left is FirstNameNode fullRecordAccess))
            {
                return false;
            }

            var info = GetInfo(fullRecordAccess);
            if (info?.Kind != BindKind.LambdaFullRecord)
            {
                return false;
            }

            firstNameInfo = info;
            return true;
        }

        /// <summary>
        /// Gets the renamed ident and returns true if the node is an AsNode
        /// Otherwise returns false and sets scopeIdent to the default.
        /// </summary>
        /// <returns></returns>
        private bool GetScopeIdent(TexlNode node, DType rowType, out DName scopeIdent)
        {
            scopeIdent = ThisRecordDefaultName;
            if (node is AsNode asNode)
            {
                scopeIdent = GetInfo(asNode).AsIdentifier;
                return true;
            }

            return false;
        }

        public bool IsRowScope(TexlNode node)
        {
            Contracts.AssertValue(node);

            return GetScopeUseSet(node).IsLambdaScope;
        }

        private void SetEcsExcemptLambdaNode(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isEcsExcemptLambda.Length);

            _isEcsExcemptLambda.Set(node.Id, true);
        }

        // Some lambdas don't need to be propagated to ECS (for example when used as filter predicates within Filter or LookUp)
        public bool IsInECSExcemptLambda(TexlNode node)
        {
            Contracts.AssertValue(node);

            if (node == null)
            {
                return false;
            }

            // No need to go further if node is outside row scope.
            if (!IsRowScope(node))
            {
                return false;
            }

            var parentNode = node;
            while ((parentNode = parentNode.Parent) != null)
            {
                if (_isEcsExcemptLambda.Get(parentNode.Id))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsStateful(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isStateful.Length);

            return _isStateful.Get(node.Id);
        }

        public bool IsPure(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isStateful.Length);
            Contracts.AssertIndex(node.Id, _hasSideEffects.Length);

            return !_isStateful.Get(node.Id) && !_hasSideEffects.Get(node.Id);
        }

        public bool IsGlobal(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _lambdaScopingMap.Length);

            return _lambdaScopingMap[node.Id].IsGlobalOnlyScope;
        }

        public bool IsLambdaScoped(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _lambdaScopingMap.Length);

            return _lambdaScopingMap[node.Id].IsLambdaScope;
        }

        public int GetInnermostLambdaScopeLevel(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _lambdaScopingMap.Length);

            return _lambdaScopingMap[node.Id].GetInnermost();
        }

        private void SetLambdaScopeLevel(TexlNode node, int upCount)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, IdLim);
            Contracts.Assert(IsGlobal(node) || upCount >= 0);

            // Ensure we don't exceed the supported up-count limit.
            if (upCount > ScopeUseSet.MaxUpCount)
            {
                ErrorContainer.Error(node, TexlStrings.ErrTooManyUps);
            }

            SetScopeUseSet(node, new ScopeUseSet(upCount));
        }

        private ScopeUseSet GetScopeUseSet(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, IdLim);

            return _lambdaScopingMap[node.Id];
        }

        private void SetScopeUseSet(TexlNode node, ScopeUseSet set)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, IdLim);
            Contracts.Assert(IsGlobal(node) || set.IsLambdaScope);

            _lambdaScopingMap[node.Id] = set;
        }

        private void SetSupportingRowScopedDelegationExemptionNode(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _supportsRowScopedParamDelegationExempted.Length);

            _supportsRowScopedParamDelegationExempted.Set(node.Id, true);
        }

        internal bool IsDelegationExempted(FirstNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _lambdaScopingMap.Length);

            if (node == null)
            {
                return false;
            }

            // No need to go further if the lambda scope is global.
            if (!IsLambdaScoped(node))
            {
                return false;
            }

            TryGetFirstNameInfo(node.Id, out var info);
            var upCount = info.UpCount;
            TexlNode parentNode = node;
            while ((parentNode = parentNode.Parent) != null)
            {
                if (TryGetCall(parentNode.Id, out var callInfo) && callInfo.Function != null && callInfo.Function.HasLambdas)
                {
                    upCount--;
                }

                if (upCount < 0)
                {
                    return false;
                }

                if (_supportsRowScopedParamDelegationExempted.Get(parentNode.Id) && upCount == 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal void SetBlockScopedConstantNode(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, IdLim);

            _isBlockScopedConstant.Set(node.Id, true);
        }

        public bool IsBlockScopedConstant(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isBlockScopedConstant.Length);

            return _isBlockScopedConstant.Get(node.Id);
        }

        public bool CanCoerce(TexlNode node)
        {
            Contracts.AssertValue(node);

            if (!TryGetCoercedType(node, out var toType))
            {
                return false;
            }

            var fromType = GetType(node);
            Contracts.Assert(fromType.IsValid);
            Contracts.Assert(!toType.IsError);

            if (fromType.IsUniversal)
            {
                return false;
            }

            return true;
        }

        public bool TryGetCoercedType(TexlNode node, out DType coercedType)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _coerceMap.Length);

            coercedType = _coerceMap[node.Id];
            return coercedType.IsValid;
        }

        public void SetCoercedType(TexlNode node, DType type)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(type.IsValid);
            Contracts.AssertIndex(node.Id, _coerceMap.Length);
            Contracts.Assert(!_coerceMap[node.Id].IsValid);

            _coerceMap[node.Id] = type;
        }

        public void SetCoercedToplevelType(DType type)
        {
            Contracts.Assert(type.IsValid);
            Contracts.Assert(!CoercedToplevelType.IsValid);

            CoercedToplevelType = type;
        }

        public FirstNameInfo GetInfo(FirstNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null || _infoMap[node.Id] is FirstNameInfo);

            return _infoMap[node.Id] as FirstNameInfo;
        }

        private BinderNodesVisitor _lazyInitializedBinderNodesVisitor = null;

        private BinderNodesVisitor BinderNodesVisitor
        {
            get
            {
                if (_lazyInitializedBinderNodesVisitor == null)
                {
                    _lazyInitializedBinderNodesVisitor = BinderNodesVisitor.Run(Top);
                }

                return _lazyInitializedBinderNodesVisitor;
            }
        }

        private BinderNodesMetadataArgTypeVisitor BinderNodeMetadataArgTypeVisitor { get; }

        public IEnumerable<BinaryOpNode> GetBinaryOperators()
        {
            return BinderNodesVisitor.BinaryOperators;
        }

        public IEnumerable<VariadicOpNode> GetVariadicOperators()
        {
            return BinderNodesVisitor.VariadicOperators;
        }

        public IEnumerable<NodeKind> GetKeywords()
        {
            return BinderNodesVisitor.Keywords;
        }

        public IEnumerable<BoolLitNode> GetBooleanLiterals()
        {
            return BinderNodesVisitor.BooleanLiterals;
        }

        public IEnumerable<NumLitNode> GetNumericLiterals()
        {
            return BinderNodesVisitor.NumericLiterals;
        }

        public IEnumerable<StrLitNode> GetStringLiterals()
        {
            return BinderNodesVisitor.StringLiterals;
        }

        public IEnumerable<StrInterpNode> GetStringInterpolations()
        {
            return BinderNodesVisitor.StringInterpolations;
        }

        public IEnumerable<UnaryOpNode> GetUnaryOperators()
        {
            return BinderNodesVisitor.UnaryOperators;
        }

        public bool IsEmpty => !_infoMap.Any(info => info != null);

        public IEnumerable<TexlNode> TopChain
        {
            get
            {
                if (IsEmpty)
                {
                    return Enumerable.Empty<TexlNode>();
                }

                if (Top is VariadicBase)
                {
                    return (Top as VariadicBase).Children;
                }

                return new TexlNode[] { Top as TexlNode };
            }
        }

        public IEnumerable<FirstNameInfo> GetFirstNamesInTree(TexlNode node)
        {
            for (var id = 0; id < IdLim; id++)
            {
                if (_infoMap[id] is FirstNameInfo info
                     && info.Node.InTree(node))
                {
                    yield return info;
                }
            }
        }

        public IEnumerable<FirstNameInfo> GetFirstNames()
        {
            return _infoMap.OfType<FirstNameInfo>();
        }

        public IEnumerable<FirstNameInfo> GetGlobalNames()
        {
            if (!UsesGlobals && !UsesResources)
            {
                return Enumerable.Empty<FirstNameInfo>();
            }

            return _infoMap
                .OfType<FirstNameInfo>()
                .Where(
                    info => info.Kind == BindKind.Control ||
                    info.Kind == BindKind.Data ||
                    info.Kind == BindKind.Resource ||
                    info.Kind == BindKind.NamedValue ||
                    info.Kind == BindKind.ComponentNameSpace ||
                    info.Kind == BindKind.WebResource ||
                    info.Kind == BindKind.QualifiedValue);
        }

        public IEnumerable<FirstNameInfo> GetGlobalControlNames()
        {
            if (!UsesGlobals)
            {
                return Enumerable.Empty<FirstNameInfo>();
            }

            return _infoMap
                .OfType<FirstNameInfo>()
                .Where(info => info.Kind == BindKind.Control);
        }

        public IEnumerable<ControlKeywordInfo> GetControlKeywordInfos()
        {
            if (!UsesGlobals)
            {
                return Enumerable.Empty<ControlKeywordInfo>();
            }

            return _infoMap.OfType<ControlKeywordInfo>();
        }

        public bool TryGetGlobalNameNode(string globalName, out TexlNode firstName)
        {
            Contracts.AssertNonEmpty(globalName);

            firstName = null;
            if (!UsesGlobals && !UsesResources)
            {
                return false;
            }

            foreach (var info in _infoMap.OfType<FirstNameInfo>())
            {
                var kind = info.Kind;
                if (info.Name.Value.Equals(globalName) &&
                    (kind == BindKind.Control || kind == BindKind.Data || kind == BindKind.Resource || kind == BindKind.QualifiedValue || kind == BindKind.WebResource))
                {
                    firstName = info.Node;
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<FirstNameInfo> GetAliasNames()
        {
            if (!UsesAliases)
            {
                return Enumerable.Empty<FirstNameInfo>();
            }

            return _infoMap
                .OfType<FirstNameInfo>()
                .Where(info => info.Kind == BindKind.Alias);
        }

        public IEnumerable<FirstNameInfo> GetScopeVariableNames()
        {
            if (!UsesScopeVariables)
            {
                return Enumerable.Empty<FirstNameInfo>();
            }

            return _infoMap
                .OfType<FirstNameInfo>()
                .Where(info => info.Kind == BindKind.ScopeVariable);
        }

        public IEnumerable<FirstNameInfo> GetScopeCollectionNames()
        {
            if (!UsesScopeCollections)
            {
                return Enumerable.Empty<FirstNameInfo>();
            }

            return _infoMap
                .OfType<FirstNameInfo>()
                .Where(info => info.Kind == BindKind.ScopeCollection);
        }

        public IEnumerable<FirstNameInfo> GetThisItemFirstNames()
        {
            if (!HasThisItemReference)
            {
                return Enumerable.Empty<FirstNameInfo>();
            }

            return _infoMap.OfType<FirstNameInfo>().Where(info => info.Kind == BindKind.ThisItem);
        }

        public IEnumerable<FirstNameInfo> GetImplicitThisItemFirstNames()
        {
            if (!HasThisItemReference)
            {
                return Enumerable.Empty<FirstNameInfo>();
            }

            return _infoMap.OfType<FirstNameInfo>().Where(info => info.Kind == BindKind.DeprecatedImplicitThisItem);
        }

        public IEnumerable<FirstNameInfo> GetLambdaParamNames(int nest)
        {
            Contracts.Assert(nest >= 0);
            if (_lambdaParams.TryGetValue(nest, out var infos))
            {
                return infos;
            }

            return Enumerable.Empty<FirstNameInfo>();
        }

        internal IEnumerable<DottedNameInfo> GetDottedNamesInTree(TexlNode node)
        {
            for (var id = 0; id < IdLim; id++)
            {
                if (_infoMap[id] is DottedNameInfo info
                    && info.Node.InTree(node))
                {
                    yield return info;
                }
            }
        }

        public IEnumerable<DottedNameInfo> GetDottedNames()
        {
            for (var id = 0; id < IdLim; id++)
            {
                if (_infoMap[id] is DottedNameInfo info)
                {
                    yield return info;
                }
            }
        }

        internal IEnumerable<CallInfo> GetCallsInTree(TexlNode node)
        {
            for (var id = 0; id < IdLim; id++)
            {
                if (_infoMap[id] is CallInfo info
                    && info.Node.InTree(node))
                {
                    yield return info;
                }
            }
        }

        public IEnumerable<CallInfo> GetCalls()
        {
            for (var id = 0; id < IdLim; id++)
            {
                if (_infoMap[id] is CallInfo info)
                {
                    yield return info;
                }
            }
        }

        public IEnumerable<CallInfo> GetCalls(TexlFunction function)
        {
            Contracts.AssertValue(function);

            for (var id = 0; id < IdLim; id++)
            {
                if (_infoMap[id] is CallInfo info && info.Function == function)
                {
                    yield return info;
                }
            }
        }

        public IEnumerable<TableNode> GetTableNodes()
        {
            for (var id = 0; id < IdLim; id++)
            {
                if (_nodeMap[id] is TableNode tableNode)
                {
                    yield return tableNode;
                }
            }
        }

        public bool TryGetCall(int nodeId, out CallInfo callInfo)
        {
            Contracts.AssertIndex(nodeId, IdLim);

            callInfo = _infoMap[nodeId] as CallInfo;
            return callInfo != null;
        }

        // Try to get the text span from a give nodeId
        // The node could be CallInfo, FirstNameInfo or DottedNameInfo
        public bool TryGetTextSpan(int nodeId, out Span span)
        {
            Contracts.AssertIndex(nodeId, IdLim);

            var node = _infoMap[nodeId];
            if (node is CallInfo callInfo)
            {
                span = callInfo.Node.GetTextSpan();
                return true;
            }

            if (node is FirstNameInfo firstNameInfo)
            {
                span = firstNameInfo.Node.GetTextSpan();
                return true;
            }

            if (node is DottedNameInfo dottedNameInfo)
            {
                span = dottedNameInfo.Node.GetTextSpan();
                return true;
            }

            span = null;
            return false;
        }

        public bool TryGetFirstNameInfo(int nodeId, out FirstNameInfo info)
        {
            if (nodeId < 0)
            {
                info = null;
                return false;
            }

            Contracts.AssertIndex(nodeId, IdLim);

            info = _infoMap[nodeId] as FirstNameInfo;
            return info != null;
        }

        public bool TryGetInfo<T>(int nodeId, out T info)
            where T : class
        {
            if (nodeId < 0 || nodeId > IdLim)
            {
                info = null;
                return false;
            }

            info = _infoMap[nodeId] as T;
            return info != null;
        }

        // Returns all scope fields consumed by this rule that match the given scope type.
        // This is always a subset of the scope type.
        // Returns DType.EmptyRecord if no scope fields are consumed by the rule.
        public DType GetTopUsedScopeFields(DName sourceControlName, DName outputTablePropertyName)
        {
            Contracts.AssertValid(sourceControlName);
            Contracts.AssertValid(outputTablePropertyName);

            // Begin with an empty record until we find an access to the specified output table.
            var accumulatedType = DType.EmptyRecord;

            // Identify all accesses to the specified output table in this rule.
            var sourceTableAccesses = GetDottedNames().Where(d => d.Node.Matches(sourceControlName, outputTablePropertyName));

            foreach (var sourceTableAccess in sourceTableAccesses)
            {
                // Start with the type of the table access.
                var currentRecordType = GetType(sourceTableAccess.Node).ToRecord();

                TexlNode node = sourceTableAccess.Node;

                // Reduce the type if the table is being sliced.
                if (node.Parent != null && node.Parent.Kind == NodeKind.DottedName)
                {
                    currentRecordType = GetType(node.Parent).ToRecord();
                }

                // Walk up the parse tree to find the first CallNode, then determine if the
                // required type can be reduced to scope fields.
                for (; node.Parent != null && node.Parent.Parent != null; node = node.Parent)
                {
                    if (node.Parent.Parent.Kind == NodeKind.Call)
                    {
                        var callInfo = GetInfo(node.Parent.Parent as CallNode);

                        if (callInfo.Function.ScopeInfo != null)
                        {
                            var scopeFunction = callInfo.Function;

                            Contracts.Assert(callInfo.Node.Args.Children.Length > 0);
                            var firstArg = callInfo.Node.Args.Children[0];

                            // Determine if we arrived as the first (scope) argument of the function call
                            // and whether we can reduce the type to contain only the used scope fields
                            // for the call.
                            if (firstArg == node && !scopeFunction.ScopeInfo.UsesAllFieldsInScope)
                            {
                                // The cursor type must be the same as the current type.
                                Contracts.Assert(currentRecordType.Accepts(callInfo.CursorType));
                                currentRecordType = GetUsedScopeFields(callInfo);
                            }
                        }

                        // Always break if we have reached a CallNode.
                        break;
                    }
                }

                // Accumulate the current type.
                accumulatedType = DType.Union(accumulatedType, currentRecordType);
            }

            return accumulatedType;
        }

        // Returns the scope fields used by the lambda parameters in the given invocation.
        // This is always a subset of the scope type (call.CursorType).
        // Returns DType.Error for anything other than invocations of functions with scope.
        public DType GetUsedScopeFields(CallInfo call)
        {
            Contracts.AssertValue(call);

            if (ErrorContainer.HasErrors() ||
                call.Function == null ||
                call.Function.ScopeInfo == null ||
                !call.CursorType.IsAggregate ||
                call.Node.Args.Count < 1)
            {
                return DType.Error;
            }

            var fields = DType.EmptyRecord;
            var arg0 = call.Node.Args.Children[0].VerifyValue();

            foreach (var name in GetLambdaParamNames(call.ScopeNest + 1))
            {
                var fError = false;
                if (!name.Node.InTree(arg0) &&
                    name.Node.InTree(call.Node) &&
                    call.CursorType.TryGetType(name.Name, out var lambdaParamType))
                {
                    if (name.Node.Parent is DottedNameNode dotted)
                    {
                        // Get the param type accumulated so far
                        if (!fields.TryGetType(name.Name, out var accParamType))
                        {
                            accParamType = DType.EmptyRecord;
                        }

                        // Get the RHS property type reported by the scope
                        var tempRhsType = lambdaParamType.IsControl ? lambdaParamType.ToRecord() : lambdaParamType;
                        if (!tempRhsType.TryGetType(dotted.Right.Name, out var propertyType))
                        {
                            propertyType = DType.Unknown;
                        }

                        // Accumulate into the param type
                        accParamType = accParamType.Add(ref fError, DPath.Root, dotted.Right.Name, propertyType);
                        lambdaParamType = accParamType;
                    }

                    fields = DType.Union(fields, DType.EmptyRecord.Add(ref fError, DPath.Root, name.Name, lambdaParamType));
                }
            }

            Contracts.Assert(fields.IsRecord);
            return fields;
        }

        private void SetInfo(FirstNameNode node, FirstNameInfo info)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(info);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null);

            if (info.Kind == BindKind.LambdaField || info.Kind == BindKind.LambdaFullRecord)
            {
                if (!_lambdaParams.ContainsKey(info.NestDst))
                {
                    _lambdaParams[info.NestDst] = new List<FirstNameInfo>();
                }

                _lambdaParams[info.NestDst].Add(info);
            }

            _infoMap[node.Id] = info;
        }

        public DottedNameInfo GetInfo(DottedNameNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null || _infoMap[node.Id] is DottedNameInfo);

            return _infoMap[node.Id] as DottedNameInfo;
        }

        private void SetInfo(DottedNameNode node, DottedNameInfo info)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(info);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null);

            _infoMap[node.Id] = info;
        }

        public AsInfo GetInfo(AsNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null || _infoMap[node.Id] is AsInfo);

            return _infoMap[node.Id] as AsInfo;
        }

        private void SetInfo(AsNode node, AsInfo info)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(info);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null);

            _infoMap[node.Id] = info;
        }

        public CallInfo GetInfo(CallNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null || _infoMap[node.Id] is CallInfo);

            return _infoMap[node.Id] as CallInfo;
        }

        public CallNode GetCompilerGeneratedCallNode(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _compilerGeneratedCallNodes.Length);

            return _compilerGeneratedCallNodes[node.Id];
        }

        private void SetInfo(CallNode node, CallInfo info, bool markIfAsync = true)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(info);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null);

            _infoMap[node.Id] = info;

            var function = info.Function;
            if (function != null)
            {
                // If the invocation is async then the whole call path is async.
                if (markIfAsync && function.IsAsyncInvocation(node, this))
                {
                    FlagPathAsAsync(node);
                }

                // If the invocation affects aliases, cache that info.
                if (function.AffectsAliases)
                {
                    AffectsAliases = true;
                }

                // If the invocation affects scope varialbe, cache that info.
                if (function.AffectsScopeVariable)
                {
                    AffectsScopeVariable = true;
                }

                if (function.AffectsDataSourceQueryOptions)
                {
                    AffectsTabularDataSources = true;
                }
            }
        }

        private CallNode GenerateCallNode(StrInterpNode node)
        {
            // We generate a transient CallNode (with no arguments) to the Concatenate function
            var func = BuiltinFunctionsCore.Concatenate;
            var ident = new IdentToken(func.Name, node.Token.Span);
            var id = node.Id;
            var listNodeId = 0;
            var minChildId = node.MinChildID;
            var callNode = new CallNode(
                ref id,
                primaryToken: ident,
                sourceList: node.SourceList,
                head: new Identifier(ident),
                headNode: null,
                new ListNode(ref listNodeId, tok: node.Token, args: new TexlNode[0], delimiters: null, sourceList: node.SourceList),
                node.StrInterpEnd);
            _compilerGeneratedCallNodes[node.Id] = callNode;
            SetInfo(callNode, new CallInfo(func, callNode));
            return callNode;
        }

        internal bool AddFieldToQuerySelects(DType type, string fieldName)
        {
            Contracts.AssertValid(type);
            Contracts.AssertNonEmpty(fieldName);
            Contracts.AssertValue(QueryOptions);

            var retVal = false;

            if (type.AssociatedDataSources == null)
            {
                return retVal;
            }

            foreach (var associatedDataSource in type.AssociatedDataSources)
            {
                if (!associatedDataSource.IsSelectable)
                {
                    continue;
                }

                // If this is accessing datasource itself then we don't need to capture this.
                if (associatedDataSource.Name == fieldName)
                {
                    continue;
                }

                retVal |= QueryOptions.AddSelect(associatedDataSource, new DName(fieldName));

                AffectsTabularDataSources = true;
            }

            return retVal;
        }

        internal DName GetFieldLogicalName(Identifier ident)
        {
            var rhsName = ident.Name;
            if (!UpdateDisplayNames && TryGetReplacedIdentName(ident, out var rhsLogicalName))
            {
                rhsName = new DName(rhsLogicalName);
            }

            return rhsName;
        }

        internal bool TryGetReplacedIdentName(Identifier ident, out string replacedIdent)
        {
            replacedIdent = string.Empty;

            // Check if the access was renamed:
            if (NodesToReplace != null)
            {
                // Token equality doesn't work here, compare the spans to be certain
                var newName = NodesToReplace.Where(kvp => kvp.Key.Span.Min == ident.Token.Span.Min && kvp.Key.Span.Lim == ident.Token.Span.Lim).FirstOrDefault();
                if (newName.Value != null && newName.Key != null)
                {
                    replacedIdent = newName.Value;
                    return true;
                }
            }

            return false;
        }

        public ParentInfo GetInfo(ParentNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null || _infoMap[node.Id] is ParentInfo);

            return _infoMap[node.Id] as ParentInfo;
        }

        public SelfInfo GetInfo(SelfNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null || _infoMap[node.Id] is SelfInfo);

            return _infoMap[node.Id] as SelfInfo;
        }

        private void SetInfo(ParentNode node, ParentInfo info)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(info);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null);

            _infoMap[node.Id] = info;
        }

        private void SetInfo(SelfNode node, SelfInfo info)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(info);
            Contracts.AssertIndex(node.Id, _infoMap.Length);
            Contracts.Assert(_infoMap[node.Id] == null);

            _infoMap[node.Id] = info;
        }

        private void FlagPathAsAsync(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _asyncMap.Length);

            while (node != null && !_asyncMap[node.Id])
            {
                _asyncMap[node.Id] = true;
                node = node.Parent;
            }
        }

        public bool IsAsync(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _asyncMap.Length);

            return _asyncMap[node.Id];
        }

        /// <summary>
        /// See documentation for <see cref="GetVolatileVariables"/> for more information.
        /// </summary>
        /// <param name="node">
        /// Node whose liftability is questioned.
        /// </param>
        /// <returns>
        /// Whether the current node is liftable.
        /// </returns>
        public bool IsUnliftable(TexlNode node)
        {
            Contracts.AssertValue(node);
            Contracts.AssertIndex(node.Id, _isUnliftable.Length);

            return _isUnliftable[node.Id];
        }

        public bool IsInfoKindDataSource(NameInfo info)
        {
            return info.Kind == BindKind.Data || info.Kind == BindKind.ScopeCollection;
        }

        public bool TryCastToFirstName(TexlNode node, out FirstNameInfo firstNameInfo)
        {
            Contracts.AssertValue(node);

            firstNameInfo = null;

            FirstNameNode firstNameNode;
            return (firstNameNode = node.AsFirstName()) != null &&
                (firstNameInfo = GetInfo(firstNameNode)) != null;
        }

        internal void DeclareMetadataNeeded(DType type)
        {
            Contracts.AssertValid(type);

            if (_typesNeedingMetadata == null)
            {
                _typesNeedingMetadata = new List<DType>();
            }

            if (!_typesNeedingMetadata.Contains(type))
            {
                _typesNeedingMetadata.Add(type);
            }
        }

        internal List<DType> GetExpandEntitiesMissingMetadata()
        {
            return _typesNeedingMetadata;
        }

        internal bool TryGetRenamedOutput(out DName outputName)
        {
            outputName = _renamedOutputAccessor;
            return outputName != default;
        }

        public bool IsAsyncWithNoSideEffects(TexlNode node)
        {
            return IsAsync(node) && !HasSideEffects(node);
        }

        private class Visitor : TexlVisitor
        {
            private sealed class Scope
            {
                public readonly CallNode Call;
                public readonly int Nest;
                public readonly Scope Parent;
                public readonly DType Type;
                public readonly bool CreatesRowScope;
                public readonly bool SkipForInlineRecords;
                public readonly DName ScopeIdentifier;
                public readonly bool RequireScopeIdentifier;

                // Optional data associated with scope. May be null.
                public readonly object Data;

                public Scope(DType type)
                {
                    Contracts.Assert(type.IsValid);
                    Type = type;
                }

                public Scope(CallNode call, Scope parent, DType type, DName scopeIdentifier = default, bool requireScopeIdentifier = false, object data = null, bool createsRowScope = true, bool skipForInlineRecords = false)
                {
                    Contracts.Assert(type.IsValid);
                    Contracts.AssertValueOrNull(data);

                    Call = call;
                    Parent = parent;
                    Type = type;
                    Data = data;
                    CreatesRowScope = createsRowScope;
                    SkipForInlineRecords = skipForInlineRecords;
                    ScopeIdentifier = scopeIdentifier;
                    RequireScopeIdentifier = requireScopeIdentifier;

                    Nest = parent?.Nest ?? 0;

                    // Scopes created for record scope only do not increase lambda param nesting
                    if (createsRowScope)
                    {
                        Nest += 1;
                    }
                }

                public Scope Up(int upCount)
                {
                    Contracts.AssertIndex(upCount, Nest);

                    var scope = this;
                    while (upCount-- > 0)
                    {
                        scope = scope.Parent;
                        Contracts.AssertValue(scope);
                    }

                    return scope;
                }
            }

            private readonly INameResolver _nameResolver;
            private readonly Scope _topScope;
            private readonly TexlBinding _txb;
            private Scope _currentScope;
            private int _currentScopeDsNodeId;
            private readonly Features _features;

            public Visitor(TexlBinding txb, INameResolver resolver, DType topScope, bool useThisRecordForRuleScope, Features features)
            {
                Contracts.AssertValue(txb);
                Contracts.AssertValueOrNull(resolver);

                _txb = txb;
                _nameResolver = resolver;
                _features = features;

                _topScope = new Scope(null, null, topScope ?? DType.Error, useThisRecordForRuleScope ? TexlBinding.ThisRecordDefaultName : default);
                _currentScope = _topScope;
                _currentScopeDsNodeId = -1;
            }

            [Conditional("DEBUG")]
            private void AssertValid()
            {
#if DEBUG
                Contracts.AssertValueOrNull(_nameResolver);
                Contracts.AssertValue(_topScope);
                Contracts.AssertValue(_currentScope);

                var scope = _currentScope;
                while (scope != null && scope != _topScope)
                {
                    scope = scope.Parent;
                }

                Contracts.Assert(scope == _topScope, "_topScope should be in the parent chain of _currentScope.");
#endif
            }

            public void Run()
            {
                _txb.Top.Accept(this);
                Contracts.Assert(_currentScope == _topScope);
            }

            private ScopeUseSet JoinScopeUseSets(params TexlNode[] nodes)
            {
                Contracts.AssertValue(nodes);
                Contracts.AssertAllValues(nodes);

                var set = ScopeUseSet.GlobalsOnly;
                foreach (var node in nodes)
                {
                    set = set.Union(_txb.GetScopeUseSet(node));
                }

                return set;
            }

            public override void Visit(ErrorNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                _txb.SetType(node, DType.Error);

                // Note that there is no need to log a binding error for this node. The fact that
                // an ErrorNode exists in the parse tree ensures that a parse/syntax error was
                // logged for it, and there is no need to duplicate it.
            }

            public override void Visit(BlankNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                _txb.SetConstant(node, true);
                _txb.SetSelfContainedConstant(node, true);
                _txb.SetType(node, DType.ObjNull);
            }

            public override void Visit(BoolLitNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                _txb.SetConstant(node, true);
                _txb.SetSelfContainedConstant(node, true);
                _txb.SetType(node, DType.Boolean);
            }

            public override void Visit(StrLitNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                _txb.SetConstant(node, true);
                _txb.SetSelfContainedConstant(node, true);
                _txb.SetType(node, DType.String);

                // For Data Table Scenario Only
                if (_txb.Property != null && _txb.Property.UseForDataQuerySelects)
                {
                    // Lookup ThisItem info
                    if (_nameResolver == null || !_nameResolver.TryGetInnermostThisItemScope(out var lookupInfo))
                    {
                        return;
                    }

                    _txb.AddFieldToQuerySelects(lookupInfo.Type, node.Value);
                }
            }

            public override void Visit(NumLitNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                _txb.SetConstant(node, true);
                _txb.SetSelfContainedConstant(node, true);
                _txb.SetType(node, DType.Number);
            }

            public DName GetLogicalNodeNameAndUpdateDisplayNames(DType type, Identifier ident, bool isThisItem = false)
            {
                return GetLogicalNodeNameAndUpdateDisplayNames(type, ident, out var unused, isThisItem);
            }

            public DName GetLogicalNodeNameAndUpdateDisplayNames(DType type, Identifier ident, out string newDisplayName, bool isThisItem = false)
            {
                Contracts.AssertValid(type);
                Contracts.AssertValue(ident);

                var logicalNodeName = ident.Name;
                newDisplayName = logicalNodeName.Value;

                if (type == DType.Invalid || (!type.IsOptionSet && !type.IsView && type.AssociatedDataSources == default))
                {
                    return logicalNodeName;
                }

                // Skip trying to match display names if the type isn't associated with a data source, an option set or view, or other display name source
                if (!type.AssociatedDataSources.Any() && !type.IsOptionSet && !type.IsView && !type.HasExpandInfo && type.DisplayNameProvider == null)
                {
                    return logicalNodeName;
                }

                var useUpdatedDisplayNames = (type.AssociatedDataSources.FirstOrDefault()?.IsConvertingDisplayNameMapping ?? false) || (type.OptionSetInfo?.IsConvertingDisplayNameMapping ?? false) || (type.ViewInfo?.IsConvertingDisplayNameMapping ?? false) || _txb._forceUpdateDisplayNames;
                var updatedDisplayNamesType = type;

                if (!useUpdatedDisplayNames && type.HasExpandInfo && type.ExpandInfo.ParentDataSource.Kind == DataSourceKind.CdsNative)
                {
                    if (_txb.Document != null && _txb.Document.GlobalScope.TryGetCdsDataSourceWithLogicalName(((IExternalCdsDataSource)type.ExpandInfo.ParentDataSource).DatasetName, type.ExpandInfo.Identity, out var relatedDataSource) &&
                        relatedDataSource.IsConvertingDisplayNameMapping)
                    {
                        useUpdatedDisplayNames = true;
                        updatedDisplayNamesType = relatedDataSource.Type;
                    }
                }

                if (_txb.UpdateDisplayNames && useUpdatedDisplayNames)
                {
                    // Either we need to go Display Name -> Display Name here
                    // Or we need to go Logical Name -> Display Name
                    if (DType.TryGetConvertedDisplayNameAndLogicalNameForColumn(updatedDisplayNamesType, ident.Name.Value, out var maybeLogicalName, out var maybeDisplayName))
                    {
                        logicalNodeName = new DName(maybeLogicalName);
                        _txb.NodesToReplace.Add(new KeyValuePair<Token, string>(ident.Token, maybeDisplayName));
                    }
                    else if (DType.TryGetDisplayNameForColumn(updatedDisplayNamesType, ident.Name.Value, out maybeDisplayName))
                    {
                        _txb.NodesToReplace.Add(new KeyValuePair<Token, string>(ident.Token, maybeDisplayName));
                    }

                    if (maybeDisplayName != null)
                    {
                        newDisplayName = new DName(maybeDisplayName);
                    }
                }
                else
                {
                    if (DType.TryGetLogicalNameForColumn(updatedDisplayNamesType, ident.Name.Value, out var maybeLogicalName, isThisItem))
                    {
                        logicalNodeName = new DName(maybeLogicalName);

                        // If we're updating display names, we don't want to accidentally rewrite something that hasn't changed to it's logical name. 
                        if (!_txb.UpdateDisplayNames)
                        {
                            _txb.NodesToReplace.Add(new KeyValuePair<Token, string>(ident.Token, maybeLogicalName));
                        }
                    }
                }

                return logicalNodeName;
            }

            public override void Visit(FirstNameNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                FirstNameInfo info;
                var haveNameResolver = _nameResolver != null;

                // Reset name lookup preferences.
                var lookupPrefs = NameLookupPreferences.None;
                var nodeName = node.Ident.Name;
                var fError = false;

                // If node is a global variable but it appears in its own weight table, we know its state has changed
                // in a "younger" sibling or cousin node, vis. some predecessor statement in a chained operation
                // changed the value of this variable, and we must ensure that it is not lifted by the back end.
                // e.g. With({}, Set(x, 1); Set(y, x + 1)) -- we need to indicate that "x + 1" cannot be cached and
                // expect to retain the same value throughout the chained operator's scope.
                if (_txb.GetVolatileVariables(node).Contains(node.Ident.Name))
                {
                    _txb.SetIsUnliftable(node, true);
                }

                // [@name]
                if (node.Ident.AtToken != null)
                {
                    if (haveNameResolver)
                    {
                        lookupPrefs |= NameLookupPreferences.GlobalsOnly;
                    }
                }

                // name[@field]
                else if (IsRowScopeAlias(node, out var scope))
                {
                    Contracts.Assert(scope.Type.IsRecord);

                    info = FirstNameInfo.Create(BindKind.LambdaFullRecord, node, scope.Nest, _currentScope.Nest, scope.Data);
                    Contracts.Assert(info.Kind == BindKind.LambdaFullRecord);

                    nodeName = GetLogicalNodeNameAndUpdateDisplayNames(scope.Type, node.Ident);

                    if (scope.Nest < _currentScope.Nest)
                    {
                        _txb.SetBlockScopedConstantNode(node);
                    }

                    _txb.SetType(node, scope.Type);
                    _txb.SetInfo(node, info);
                    _txb.SetLambdaScopeLevel(node, info.UpCount);
                    _txb.AddFieldToQuerySelects(scope.Type, nodeName);
                    return;
                }

                // fieldName (unqualified)
                else if (IsRowScopeField(node, out scope, out fError, out var isWholeScope))
                {
                    Contracts.Assert(scope.Type.IsRecord || scope.Type.IsUntypedObject);

                    // Detected access to a pageable dataEntity in row scope, error was set
                    if (fError)
                    {
                        return;
                    }

                    var nodeType = scope.Type;

                    if (!isWholeScope)
                    {
                        info = FirstNameInfo.Create(BindKind.LambdaField, node, scope.Nest, _currentScope.Nest, scope.Data);
                        nodeName = GetLogicalNodeNameAndUpdateDisplayNames(scope.Type, node.Ident);
                        nodeType = scope.Type.GetType(nodeName);
                    }
                    else
                    {
                        info = FirstNameInfo.Create(BindKind.LambdaFullRecord, node, scope.Nest, _currentScope.Nest, scope.Data);
                        if (scope.Nest < _currentScope.Nest)
                        {
                            _txb.SetBlockScopedConstantNode(node);
                        }
                    }

                    Contracts.Assert(info.UpCount >= 0);

                    _txb.SetType(node, nodeType);
                    _txb.SetInfo(node, info);
                    _txb.SetLambdaScopeLevel(node, info.UpCount);
                    _txb.AddFieldToQuerySelects(nodeType, nodeName);
                    return;
                }

                // Look up a global variable with this name.
                NameLookupInfo lookupInfo = default;
                if (_txb.AffectsScopeVariableName)
                {
                    if (haveNameResolver && _nameResolver.CurrentEntity != null)
                    {
                        var scopedControl = _txb._glue.GetVariableScopedControlFromTexlBinding(_txb);

                        // App variable name cannot conflict with any existing global entity name, eg. control/data/table/enum.
                        if (scopedControl.IsAppInfoControl && _nameResolver.LookupGlobalEntity(node.Ident.Name, out lookupInfo))
                        {
                            _txb.ErrorContainer.Error(node, TexlStrings.ErrExpectedFound_Ex_Fnd, lookupInfo.Kind, TokKind.Ident);
                        }

                        _txb.SetAppScopedVariable(node, scopedControl.IsAppInfoControl);
                    }

                    // Set the variable name node as DType.String.
                    _txb.SetType(node, DType.String);
                    _txb.SetInfo(node, FirstNameInfo.Create(node, default(NameLookupInfo)));
                    return;
                }

                if (node.Parent is DottedNameNode)
                {
                    lookupPrefs |= NameLookupPreferences.HasDottedNameParent;
                }

                // Check if this control property has local scope name resolver.
                var localScopeNameResolver = _txb.LocalRuleScopeResolver;
                if (localScopeNameResolver != null && localScopeNameResolver.Lookup(node.Ident.Name, out var scopedInfo))
                {
                    _txb.SetType(node, scopedInfo.Type);
                    _txb.SetInfo(node, FirstNameInfo.Create(node, scopedInfo));
                    _txb.SetStateful(node, scopedInfo.IsStateful);
                    _txb.HasLocalScopeReferences = true;
                    return;
                }

                if (!haveNameResolver || !_nameResolver.Lookup(node.Ident.Name, out lookupInfo, preferences: lookupPrefs))
                {
                    _txb.ErrorContainer.Error(node, TexlStrings.ErrInvalidName, node.Ident.Name.Value);
                    _txb.SetType(node, DType.Error);
                    _txb.SetInfo(node, FirstNameInfo.Create(node, default(NameLookupInfo)));
                    return;
                }

                Contracts.Assert(lookupInfo.Kind != BindKind.LambdaField);
                Contracts.Assert(lookupInfo.Kind != BindKind.LambdaFullRecord);
                Contracts.Assert(lookupInfo.Kind != BindKind.Unknown);

                var fnInfo = FirstNameInfo.Create(node, lookupInfo);
                var lookupType = lookupInfo.Type;

                if (lookupInfo.DisplayName != default)
                {
                    if (_txb.UpdateDisplayNames)
                    {
                        _txb.NodesToReplace.Add(new KeyValuePair<Token, string>(node.Token, lookupInfo.DisplayName));
                    }
                    else if (lookupInfo.Data is IExternalEntity entity)
                    {
                        _txb.NodesToReplace.Add(new KeyValuePair<Token, string>(node.Token, entity.EntityName));
                    }
                }

                // Internal control references are not allowed in component input properties.
                if (CheckComponentProperty(lookupInfo.Data as IExternalControl))
                {
                    _txb.ErrorContainer.Error(node, TexlStrings.ErrInternalControlInInputProperty);
                    _txb.SetType(node, DType.Error);
                    _txb.SetInfo(node, fnInfo ?? FirstNameInfo.Create(node, default(NameLookupInfo)));
                    return;
                }

                if (lookupInfo.Kind == BindKind.ThisItem)
                {
                    _txb._hasThisItemReference = true;
                    if (!TryProcessFirstNameNodeForThisItemAccess(node, lookupInfo, out lookupType, out fnInfo) || lookupType.IsError)
                    {
                        // Property should not include ThisItem, return an error
                        _txb.ErrorContainer.Error(node, TexlStrings.ErrInvalidName, node.Ident.Name.Value);
                        _txb.SetType(node, DType.Error);
                        _txb.SetInfo(node, fnInfo ?? FirstNameInfo.Create(node, default(NameLookupInfo)));
                        return;
                    }

                    _txb.SetContextual(node, true);
                }
                else if (lookupInfo.Kind == BindKind.DeprecatedImplicitThisItem)
                {
                    _txb._hasThisItemReference = true;

                    // Even though lookupInfo.Type isn't the full data source type, it still is tagged with the full datasource info if this is a thisitem node
                    nodeName = GetLogicalNodeNameAndUpdateDisplayNames(lookupType, node.Ident, /* isThisItem */ true);

                    // If the ThisItem reference is an entity, the type should be expanded.
                    if (lookupType.IsExpandEntity)
                    {
                        var parentEntityPath = string.Empty;

                        var thisItemType = default(DType);
                        if (lookupInfo.Data is IExternalControl outerControl)
                        {
                            thisItemType = outerControl.ThisItemType;
                        }

                        if (thisItemType != default && thisItemType.HasExpandInfo)
                        {
                            parentEntityPath = thisItemType.ExpandInfo.ExpandPath.ToString();
                        }

                        lookupType = GetExpandedEntityType(lookupType, parentEntityPath);
                        fnInfo = FirstNameInfo.Create(node, lookupInfo, lookupInfo.Type.ExpandInfo);
                    }
                }

                // Make a note of this global's type, as identifier by the resolver.
                _txb.SetType(node, lookupType);

                // If this is a reference to an Enum, it is constant.
                _txb.SetConstant(node, lookupInfo.Kind == BindKind.Enum);
                _txb.SetSelfContainedConstant(node, lookupInfo.Kind == BindKind.Enum);

                // Create a name info with an appropriate binding, defaulting to global binding in error cases.
                _txb.SetInfo(node, fnInfo);

                // If the firstName is a standalone global control reference (i.e. not a LHS for a property access)
                // make sure to record this, as it's something that is needed later during codegen.
                if (lookupType.IsControl && (node.Parent == null || node.Parent.AsDottedName() == null))
                {
                    _txb.HasControlReferences = true;

                    // If the current property doesn't support global control references, set an error
                    if (_txb.CurrentPropertyRequiresDefaultableReferences)
                    {
                        _txb.ErrorContainer.EnsureError(node, TexlStrings.ErrInvalidControlReference);
                    }
                }

                // Update _usesGlobals, _usesResources, etc.
                UpdateBindKindUseFlags(lookupInfo.Kind);

                // Update statefulness of global datasources excluding dynamic datasources.
                if (lookupInfo.Kind == BindKind.Data && !_txb._glue.IsDynamicDataSourceInfo(lookupInfo.Data))
                {
                    _txb.SetStateful(node, true);
                }

                if (lookupInfo.Kind == BindKind.WebResource || (lookupInfo.Kind == BindKind.QualifiedValue && ((lookupInfo.Data as IQualifiedValuesInfo)?.IsAsyncAccess ?? false)))
                {
                    _txb.FlagPathAsAsync(node);
                    _txb.SetStateful(node, true);
                }

                _txb.CheckAndMarkAsPageable(node);
                _txb.CheckAndMarkAsDelegatable(node);

                if ((lookupInfo.Kind == BindKind.WebResource || lookupInfo.Kind == BindKind.QualifiedValue) && !(node.Parent is DottedNameNode))
                {
                    _txb.ErrorContainer.EnsureError(node, TexlStrings.ErrValueMustBeFullyQualified);
                }

                if (lookupInfo.IsAsync)
                {
                    _txb.FlagPathAsAsync(node);
                }
            }

            private bool TryProcessFirstNameNodeForThisItemAccess(FirstNameNode node, NameLookupInfo lookupInfo, out DType nodeType, out FirstNameInfo info)
            {
                if (_nameResolver.CurrentEntity is IExternalControl)
                {
                    // Check to see if we only want to include ThisItem in specific
                    // properties of this Control
                    if (_nameResolver.EntityScope.TryGetEntity(_nameResolver.CurrentEntity.EntityName, out IExternalControl nodeAssociatedControl) &&
                        nodeAssociatedControl.Template.IncludesThisItemInSpecificProperty)
                    {
                        if (nodeAssociatedControl.Template.TryGetProperty(_nameResolver.CurrentProperty, out var nodeAssociatedProperty) && !nodeAssociatedProperty.ShouldIncludeThisItemInFormula)
                        {
                            nodeType = null;
                            info = null;
                            return false;
                        }
                    }
                }

                // Check to see if ThisItem is used in a DottedNameNode and if there is a data control
                // accessible from this rule.
                DName dataControlName = default;
                if (node.Parent is DottedNameNode node1 && _nameResolver.LookupDataControl(node.Ident.Name, out var dataControlLookupInfo, out dataControlName))
                {
                    // Get the property name being accessed by the parent dotted name.
                    var rightName = node1.Right.Name;

                    Contracts.AssertValid(rightName);
                    Contracts.Assert(dataControlLookupInfo.Type.IsControl);

                    // Check to see if the dotted name is accessing a property of the data control.
                    if (((IExternalControlType)dataControlLookupInfo.Type).ControlTemplate.HasOutput(rightName))
                    {
                        // Set the result type to the data control type.
                        nodeType = dataControlLookupInfo.Type;
                        info = FirstNameInfo.Create(node, lookupInfo, dataControlName, true);
                        return true;
                    }
                }

                nodeType = lookupInfo.Type;
                info = FirstNameInfo.Create(node, lookupInfo, dataControlName, false);
                return true;
            }

            private bool IsRowScopeField(FirstNameNode node, out Scope scope, out bool fError, out bool isWholeScope)
            {
                Contracts.AssertValue(node);

                fError = false;
                isWholeScope = false;

                // [@foo] cannot be a scope field.
                if (node.Ident.AtToken != null)
                {
                    scope = default;
                    return false;
                }

                var nodeName = node.Ident.Name;

                // Look up the name in the current scopes, innermost to outermost.
                // The logic here is as follows:
                // We need to find the innermost row scope where the FirstName we're searching for is present in the scope
                // Either as a field in the type, or as the scope identifier itself
                // We check the non-reqired identifier case first to preserve existing behavior when the field name is 'ThisRecord'
                for (scope = _currentScope; scope != null; scope = scope.Parent)
                {
                    Contracts.AssertValue(scope);

                    if (!scope.CreatesRowScope)
                    {
                        continue;
                    }

                    // If the scope identifier isn't required, look up implicit accesses
                    if (!scope.RequireScopeIdentifier)
                    {
                        // If scope type is a data source, the node may be a display name instead of logical.
                        // Attempt to get the logical name to use for type checking.
                        // If this is executed amidst a metadata refresh then the reference may refer to an old
                        // display name, so we need to check the old mapping as well as the current mapping.
                        var usesDisplayName =
                            DType.TryGetConvertedDisplayNameAndLogicalNameForColumn(scope.Type, nodeName.Value, out var maybeLogicalName, out _) ||
                            DType.TryGetLogicalNameForColumn(scope.Type, nodeName.Value, out maybeLogicalName);
                        if (usesDisplayName)
                        {
                            nodeName = new DName(maybeLogicalName);
                        }

                        if (scope.Type.TryGetType(nodeName, out var typeTmp))
                        {
                            // Expand the entity type here.
                            if (typeTmp.IsExpandEntity)
                            {
                                var parentEntityPath = string.Empty;
                                if (scope.Type.HasExpandInfo)
                                {
                                    parentEntityPath = scope.Type.ExpandInfo.ExpandPath.ToString();
                                }

                                // We cannot access pageable entities in row-scope, as it will generate too many calls to the connector
                                // Set an error and skip it.
                                if (typeTmp.ExpandInfo.IsTable)
                                {
                                    if (_txb.Document != null && _txb.Document.Properties.EnabledFeatures.IsEnableRowScopeOneToNExpandEnabled)
                                    {
                                        _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, node, TexlStrings.WrnRowScopeOneToNExpandNumberOfCalls);
                                    }
                                    else
                                    {
                                        _txb.ErrorContainer.Error(node, TexlStrings.ErrColumnNotAccessibleInCurrentContext);
                                        _txb.SetType(node, DType.Error);
                                        fError = true;
                                        return true;
                                    }
                                }

                                var expandedEntityType = GetExpandedEntityType(typeTmp, parentEntityPath);
                                var type = scope.Type.SetType(ref fError, DPath.Root.Append(nodeName), expandedEntityType);
                                scope = new Scope(scope.Call, scope.Parent, type, scope.ScopeIdentifier, scope.RequireScopeIdentifier, expandedEntityType.ExpandInfo);
                            }

                            return true;
                        }
                    }

                    if (scope.ScopeIdentifier == nodeName)
                    {
                        isWholeScope = true;
                        return true;
                    }
                }

                scope = default;
                return false;
            }

            private bool IsRowScopeAlias(FirstNameNode node, out Scope scope)
            {
                Contracts.AssertValue(node);

                scope = default;

                if (!node.IsLhs)
                {
                    return false;
                }

                var dotted = node.Parent.AsDottedName().VerifyValue();
                if (!dotted.UsesBracket)
                {
                    return false;
                }

                // Look up the name as a scope alias.
                for (scope = _currentScope; scope != null; scope = scope.Parent)
                {
                    Contracts.AssertValue(scope);

                    if (!scope.CreatesRowScope || scope.Call == null)
                    {
                        continue;
                    }

                    // There is no row scope alias, so we have to rely on a heuristic here.
                    // Look for the first scope whose parent call specifies a matching FirstName arg0.
                    FirstNameNode arg0;
                    if (scope.Call.Args.Count > 0 &&
                        (arg0 = scope.Call.Args.Children[0].AsFirstName()) != null &&
                        arg0.Ident.Name == node.Ident.Name &&
                        arg0.Ident.Namespace == node.Ident.Namespace)
                    {
                        return true;
                    }
                }

                scope = default;
                return false;
            }

            public override void Visit(ParentNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                if (_nameResolver == null || _nameResolver.CurrentEntity == null)
                {
                    _txb.ErrorContainer.Error(node, TexlStrings.ErrInvalidIdentifier);
                    _txb.SetType(node, DType.Error);
                    return;
                }

                if (!(_nameResolver.CurrentEntity is IExternalControl) || !_nameResolver.LookupParent(out var lookupInfo))
                {
                    _txb.ErrorContainer.Error(node, TexlStrings.ErrArgNotAValidIdentifier_Name, node.Kind);
                    _txb.SetType(node, DType.Error);
                    return;
                }

                // Treat this as a standard access to the parent control ("v" type).
                _txb.SetType(node, lookupInfo.Type);
                _txb.SetInfo(node, new ParentInfo(node, lookupInfo.Path, lookupInfo.Data as IExternalControl));
                _txb.HasParentItemReference = true;

                UpdateBindKindUseFlags(lookupInfo.Kind);
            }

            public override void Visit(SelfNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                if (_nameResolver == null || _nameResolver.CurrentEntity == null)
                {
                    _txb.ErrorContainer.Error(node, TexlStrings.ErrInvalidIdentifier);
                    _txb.SetType(node, DType.Error);
                    return;
                }

                if (!_nameResolver.LookupSelf(out var lookupInfo))
                {
                    _txb.ErrorContainer.Error(node, TexlStrings.ErrArgNotAValidIdentifier_Name, node.Kind);
                    _txb.SetType(node, DType.Error);
                    return;
                }

                // Treat this as a standard access to the current control ("v" type).
                _txb.SetType(node, lookupInfo.Type);
                _txb.SetInfo(node, new SelfInfo(node, lookupInfo.Path, lookupInfo.Data as IExternalControl));
                _txb.HasSelfReference = true;

                UpdateBindKindUseFlags(lookupInfo.Kind);
            }

            private void UpdateBindKindUseFlags(BindKind bindKind)
            {
                Contracts.Assert(bindKind >= BindKind.Min && bindKind < BindKind.Lim);

                switch (bindKind)
                {
                    case BindKind.Condition:
                    case BindKind.Control:
                    case BindKind.Data:
                    case BindKind.PowerFxResolvedObject:
                    case BindKind.NamedValue:
                    case BindKind.QualifiedValue:
                    case BindKind.WebResource:
                        _txb.UsesGlobals = true;
                        break;
                    case BindKind.Alias:
                        _txb.UsesAliases = true;
                        break;
                    case BindKind.ScopeCollection:
                        _txb.UsesScopeCollections = true;
                        break;
                    case BindKind.ScopeVariable:
                        _txb.UsesScopeVariables = true;
                        break;
                    case BindKind.DeprecatedImplicitThisItem:
                    case BindKind.ThisItem:
                        _txb.UsesThisItem = true;
                        break;
                    case BindKind.Resource:
                        _txb.UsesResources = true;
                        _txb.UsesGlobals = true;
                        break;
                    case BindKind.OptionSet:
                        _txb.UsesGlobals = true;
                        _txb.UsesOptionSets = true;
                        break;
                    case BindKind.View:
                        _txb.UsesGlobals = true;
                        _txb.UsesViews = true;
                        break;
                    default:
                        Contracts.Assert(bindKind == BindKind.LambdaField || bindKind == BindKind.LambdaFullRecord || bindKind == BindKind.Enum || bindKind == BindKind.Unknown);
                        break;
                }
            }

            public override bool PreVisit(RecordNode node) => PreVisitVariadicBase(node);

            public override bool PreVisit(TableNode node) => PreVisitVariadicBase(node);

            private bool PreVisitVariadicBase(VariadicBase node)
            {
                Contracts.AssertValue(node);

                var volatileVariables = _txb.GetVolatileVariables(node);
                foreach (var child in node.Children)
                {
                    _txb.AddVolatileVariables(child, volatileVariables);
                }

                return true;
            }

            public override bool PreVisit(DottedNameNode node)
            {
                Contracts.AssertValue(node);

                _txb.AddVolatileVariables(node.Left, _txb.GetVolatileVariables(node));
                return true;
            }

            public override void PostVisit(DottedNameNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                var leftType = _txb.GetType(node.Left);

                if (!leftType.IsControl && !leftType.IsAggregate && !leftType.IsEnum && !leftType.IsOptionSet && !leftType.IsView && !leftType.IsUntypedObject)
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidDot);
                    return;
                }

                object value = null;
                var typeRhs = DType.Invalid;
                var nameRhs = node.Right.Name;

                nameRhs = GetLogicalNodeNameAndUpdateDisplayNames(leftType, node.Right);

                // In order for the node to be constant, it must be a member of an enum,
                // a member of a constant aggregate,
                // or a reference to a constant rule (checked later).
                var isConstant = leftType.IsEnum || (leftType.IsAggregate && _txb.IsConstant(node.Left));

                // Some nodes are never pageable, use this to
                // skip the check for pageability and default to non-pageable;
                var canBePageable = true;

                if (leftType.IsEnum)
                {
                    if (_nameResolver == null)
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidIdentifier);
                        return;
                    }

                    // The RHS is a locale-specific name (straight from the parse tree), so we need
                    // to look things up accordingly. If the LHS is a FirstName, fetch its embedded
                    // EnumInfo and look in it for a value with the given locale-specific name.
                    // This should be a fast O(1) lookup that covers 99% of all cases, such as
                    // Couleur!Rouge, Align.Droit, etc.
                    var firstNodeLhs = node.Left.AsFirstName();
                    var firstInfoLhs = firstNodeLhs == null ? null : _txb.GetInfo(firstNodeLhs).VerifyValue();
                    if (firstInfoLhs != null && _nameResolver.LookupEnumValueByInfoAndLocName(firstInfoLhs.Data, nameRhs, out value))
                    {
                        typeRhs = leftType.GetEnumSupertype();
                    }

                    // ..otherwise do a slower lookup by type for the remaining 1% of cases,
                    // such as text1!Fill!Rouge, etc.
                    // This is O(n) in the number of registered enums.
                    else if (_nameResolver.LookupEnumValueByTypeAndLocName(leftType, nameRhs, out value))
                    {
                        typeRhs = leftType.GetEnumSupertype();
                    }
                    else
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                        return;
                    }
                }
                else if (leftType.IsOptionSet || leftType.IsView)
                {
                    if (!leftType.TryGetType(nameRhs, out typeRhs))
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                        return;
                    }
                }
                else if (leftType.IsAttachment)
                {
                    // Error: Attachment Type should never be the left hand side of dotted name node
                    SetDottedNameError(node, TexlStrings.ErrInvalidIdentifier);
                    return;
                }
                else if (leftType is IExternalControlType leftControl)
                {
                    var (controlInfo, isIndirectPropertyUsage) = GetLHSControlInfo(node);

                    if (isIndirectPropertyUsage)
                    {
                        _txb.UsedControlProperties.Add(nameRhs);
                    }

                    // Explicitly block accesses to the parent's nested-aware property.
                    if (controlInfo != null && UsesParentsNestedAwareProperty(controlInfo, nameRhs))
                    {
                        SetDottedNameError(node, TexlStrings.ErrNotAccessibleInCurrentContext);
                        return;
                    }

                    // The RHS is a control property name (locale-specific).
                    var template = leftControl.ControlTemplate.VerifyValue();
                    if (!template.TryGetOutputProperty(nameRhs, out var property))
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                        return;
                    }

                    // We block the property access usage for behavior component properties.
                    if (template.IsComponent && property.PropertyCategory.IsBehavioral())
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidPropertyReference);
                        return;
                    }

                    // We block the property access usage for scoped component properties.
                    if (template.IsComponent && property.IsScopeVariable)
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidPropertyReference);
                        return;
                    }

                    // We block the property access usage for datasource of the command component.
                    if (template.IsCommandComponent &&
                        _txb._glue.IsPrimaryCommandComponentProperty(property))
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidPropertyReference);
                        return;
                    }

                    var lhsControlInfo = controlInfo;
                    var currentControl = _txb.Control;

                    // We block the property access usage for context property of the command component instance unless it's the same command control.
                    if (lhsControlInfo != null &&
                        lhsControlInfo.IsCommandComponentInstance &&
                        _txb._glue.IsContextProperty(property) &&
                        currentControl != null && currentControl != lhsControlInfo)
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidPropertyReference);
                        return;
                    }

                    // Explicitly block access to design properties referenced via Selected/AllItems.
                    if (leftControl.IsDataLimitedControl && property.PropertyCategory != PropertyRuleCategory.Data)
                    {
                        SetDottedNameError(node, TexlStrings.ErrNotAccessibleInCurrentContext);
                        return;
                    }

                    // For properties requiring default references, block non-defaultable properties
                    if (_txb.CurrentPropertyRequiresDefaultableReferences && property.UnloadedDefault == null)
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidControlReference);
                        return;
                    }

                    // If the property has pass-through input (e.g. AllItems, Selected, etc), the correct RHS (property)
                    // expando type is not available in the "v" type. We try delay calculating this until we need it as this is
                    // an expensive operation especially for form control which generally has tons of nested controls. So we calculate the type here.
                    // There might be cases where we are getting the schema from imported data that once belonged to a control and now,
                    // we don't have a pass-through input associated with it. Therefore, we need to get the opaqueType to avoid localizing the schema.
                    if (property.PassThroughInput == null)
                    {
                        typeRhs = property.GetOpaqueType();
                    }
                    else
                    {
                        var firstNodeLhs = node.Left.AsFirstName();
                        if (template.HasExpandoProperties &&
                            template.ExpandoProperties.Any(p => p.InvariantName == property.InvariantName) &&
                            controlInfo != null && (firstNodeLhs == null || _txb.GetInfo(firstNodeLhs).Kind != BindKind.ScopeVariable))
                        {
                            // If visiting an expando type property of control type variable, we cannot calculate the type here because
                            // The LHS associated ControlInfo is App/Component.
                            // e.g. Set(controlVariable1, DropDown1), Label1.Text = controlVariable1.Selected.Value.
                            leftType = (DType)controlInfo.GetControlDType(calculateAugmentedExpandoType: true, isDataLimited: false);
                        }

                        if (!leftType.ToRecord().TryGetType(property.InvariantName, out typeRhs))
                        {
                            SetDottedNameError(node, TexlStrings.ErrInvalidName, property.InvariantName);
                            return;
                        }
                    }

                    // If the reference is to Control.Property and the rule for that Property is a constant,
                    // we need to mark the node as constant, and save the control info so we may look up the
                    // rule later.
                    if (controlInfo?.GetRule(property.InvariantName) is { HasErrors: false } rule && rule.Binding.IsConstant(rule.Binding.Top))
                    {
                        value = controlInfo;
                        isConstant = true;
                    }

                    // Check access to custom scoped input properties. Such properties can only be accessed from within a component or output property of a component.
                    if (property.IsScopedProperty &&
                        _txb.Control != null && _txb.Property != null &&
                        controlInfo != null &&
                        !IsValidAccessToScopedProperty(controlInfo, property, _txb.Control, _txb.Property))
                    {
                        SetDottedNameError(node, TexlStrings.ErrUnSupportedComponentDataPropertyAccess);
                        return;
                    }

                    // Check for scoped property access with required scoped variable.
                    if (property.IsScopedProperty && property.ScopeFunctionPrototype.MinArity > 0)
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidPropertyAccess);
                        return;
                    }

                    if (property.IsScopedProperty && property.ScopeFunctionPrototype.IsAsync)
                    {
                        _txb.FlagPathAsAsync(node);
                    }
                }
                else if (!leftType.TryGetType(nameRhs, out typeRhs) && !leftType.IsUntypedObject)
                {
                    // We may be in the case of dropDown!Selected!RHS
                    // In this case, Selected embeds a meta field whose v-type encapsulates localization info
                    // for the sub-properties of "Selected". The localized sub-properties are NOT present in
                    // the Selected DType directly.
                    Contracts.Assert(leftType.IsAggregate);
                    if (leftType.TryGetMetaField(out var vType))
                    {
                        if (!vType.ControlTemplate.TryGetOutputProperty(nameRhs, out var property))
                        {
                            SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                            return;
                        }

                        typeRhs = property.Type;
                    }
                    else
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                        return;
                    }
                }
                else if (typeRhs is IExternalControlType controlType && controlType.IsMetaField)
                {
                    // Meta fields are not directly accessible. E.g. dropdown!Selected!meta is an invalid access.
                    SetDottedNameError(node, TexlStrings.ErrInvalidName, node.Right.Name.Value);
                    return;
                }
                else if (typeRhs.IsExpandEntity)
                {
                    typeRhs = GetEntitySchema(typeRhs, node);
                    value = typeRhs.ExpandInfo;
                    Contracts.Assert(typeRhs == DType.Error || typeRhs.ExpandInfo != null);

                    if (_txb.IsRowScope(node.Left) && (typeRhs.ExpandInfo != null && typeRhs.ExpandInfo.IsTable))
                    {
                        if (_txb.Document != null && _txb.Document.Properties.EnabledFeatures.IsEnableRowScopeOneToNExpandEnabled)
                        {
                            _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, node, TexlStrings.WrnRowScopeOneToNExpandNumberOfCalls);
                        }
                        else
                        {
                            SetDottedNameError(node, TexlStrings.ErrColumnNotAccessibleInCurrentContext);
                            return;
                        }
                    }
                }

                // Consider the attachmentType as the type of the node for binding purposes
                // if it is being accessed from a record
                if (typeRhs.IsAttachment)
                {
                    // Disable accessing the attachment in RowScope or single column table
                    // to prevent a large number of calls to the service
                    if (_txb.IsRowScope(node.Left) || leftType.IsTable)
                    {
                        SetDottedNameError(node, TexlStrings.ErrColumnNotAccessibleInCurrentContext);
                        return;
                    }

                    var attachmentType = typeRhs.AttachmentType;
                    Contracts.AssertValid(attachmentType);
                    Contracts.Assert(leftType.IsRecord);

                    typeRhs = attachmentType;
                    _txb.HasReferenceToAttachment = true;
                    _txb.FlagPathAsAsync(node);
                }

                // Set the type for the dotted node itself.
                if (leftType.IsEnum)
                {
                    // #T[id:val, ...] . id --> T
                    Contracts.Assert(typeRhs == leftType.GetEnumSupertype());
                    _txb.SetType(node, typeRhs);
                }
                else if (leftType.IsOptionSet || leftType.IsView)
                {
                    _txb.SetType(node, typeRhs);
                }
                else if (leftType.IsRecord)
                {
                    // ![id:type, ...] . id --> type
                    _txb.SetType(node, typeRhs);
                }
                else if (leftType.IsUntypedObject)
                {
                    _txb.SetType(node, DType.UntypedObject);
                }
                else if (leftType.IsTable)
                {
                    // *[id:type, ...] . id  --> *[id:type]
                    // We don't support scenario when lhs is table and rhs is entity of table type (1-n)
                    if (value is IExpandInfo && typeRhs.IsTable)
                    {
                        SetDottedNameError(node, TexlStrings.ErrColumnNotAccessibleInCurrentContext);
                        return;
                    }
                    else if (value is IExpandInfo)
                    {
                        var resultType = DType.CreateTable(new TypedName(typeRhs, nameRhs));
                        foreach (var cds in leftType.AssociatedDataSources)
                        {
                            resultType = DType.AttachDataSourceInfo(resultType, cds, attachToNestedType: false);
                        }

                        _txb.SetType(node, resultType);
                        canBePageable = false;
                    }
                    else
                    {
                        _txb.SetType(node, DType.CreateDTypeWithConnectedDataSourceInfoMetadata(DType.CreateTable(new TypedName(typeRhs, nameRhs)), typeRhs.AssociatedDataSources, typeRhs.DisplayNameProvider));
                    }
                }
                else
                {
                    // v[prop:type, ...] . prop --> type
                    Contracts.Assert(leftType.IsControl || leftType.IsExpandEntity || leftType.IsAttachment);
                    _txb.SetType(node, typeRhs);
                }

                // Set the remaining bits -- name info, side effect info, etc.
                _txb.SetInfo(node, new DottedNameInfo(node, value));
                _txb.SetSideEffects(node, _txb.HasSideEffects(node.Left));
                _txb.SetStateful(node, _txb.IsStateful(node.Left));
                _txb.SetContextual(node, _txb.IsContextual(node.Left));

                _txb.SetConstant(node, isConstant);
                _txb.SetSelfContainedConstant(node, leftType.IsEnum || (leftType.IsAggregate && _txb.IsSelfContainedConstant(node.Left)));
                if (_txb.IsBlockScopedConstant(node.Left))
                {
                    _txb.SetBlockScopedConstantNode(node);
                }

                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Left));

                if (canBePageable)
                {
                    _txb.CheckAndMarkAsDelegatable(node);
                    _txb.CheckAndMarkAsPageable(node);
                }

                _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(node.Left));
                _txb.SetIsUnliftable(node, _txb.IsUnliftable(node.Left));
            }

            private (IExternalControl controlInfo, bool isIndirectPropertyUsage) GetLHSControlInfo(DottedNameNode node)
            {
                var isIndirectPropertyUsage = false;
                if (!TryGetControlInfoLHS(node.Left, out var info))
                {
                    // App Global references need not be tracked for control references
                    // here as Global control edges are is already handled in analysis.
                    // Doing this here for global control reference can cause more than required aggressive edges
                    // and creating cross screen dependencies that are not required.
                    isIndirectPropertyUsage = !(node.Left.Kind == NodeKind.DottedName
                        && TryGetControlInfoLHS(node.Left.AsDottedName().Left, out var outerInfo)
                        && outerInfo.IsAppGlobalControl);
                }

                return (info, isIndirectPropertyUsage);
            }

            // Check if the control can be used in current component property
            private bool CheckComponentProperty(IExternalControl control)
            {
                return control != null && !_txb._glue.CanControlBeUsedInComponentProperty(_txb, control);
            }

            private DType GetEntitySchema(DType entityType, DottedNameNode node)
            {
                Contracts.AssertValid(entityType);
                Contracts.AssertValue(node);

                var entityPath = string.Empty;
                var lhsType = _txb.GetType(node.Left);

                if (lhsType.HasExpandInfo)
                {
                    entityPath = lhsType.ExpandInfo.ExpandPath.ToString();
                }

                return GetExpandedEntityType(entityType, entityPath);
            }

            protected DType GetExpandedEntityType(DType expandEntityType, string relatedEntityPath)
            {
                Contracts.AssertValid(expandEntityType);
                Contracts.Assert(expandEntityType.HasExpandInfo);
                Contracts.AssertValue(relatedEntityPath);

                var expandEntityInfo = expandEntityType.ExpandInfo;

                if (expandEntityInfo.ParentDataSource is not IExternalTabularDataSource dsInfo)
                {
                    return expandEntityType;
                }

                // This will cache expandend types of entities in QueryOptions
                var entityTypes = _txb.QueryOptions.GetExpandDTypes(dsInfo);

                if (!entityTypes.TryGetValue(expandEntityInfo.ExpandPath, out var type))
                {
                    if (!expandEntityType.TryGetEntityDelegationMetadata(out var metadata))
                    {
                        // We need more metadata to bind this fully
                        _txb.DeclareMetadataNeeded(expandEntityType);
                        return DType.Error;
                    }

                    type = expandEntityType.ExpandEntityType(metadata.Schema, metadata.Schema.AssociatedDataSources);
                    Contracts.Assert(type.HasExpandInfo);

                    // Update the datasource and relatedEntity path.
                    type.ExpandInfo.UpdateEntityInfo(expandEntityInfo.ParentDataSource, relatedEntityPath);
                    entityTypes.Add(expandEntityInfo.ExpandPath, type);
                }

                return type;
            }

            private bool TryGetControlInfoLHS(TexlNode node, out IExternalControl info)
            {
                Contracts.AssertValue(node);

                info = node switch
                {
                    ParentNode parentNode => _txb.GetInfo(parentNode)?.Data as IExternalControl,
                    SelfNode selfNode => _txb.GetInfo(selfNode)?.Data as IExternalControl,
                    FirstNameNode firstNameNode => _txb.GetInfo(firstNameNode)?.Data as IExternalControl,
                    _ => null,
                };

                return info != null;
            }

            protected void SetDottedNameError(DottedNameNode node, ErrorResourceKey errKey, params object[] args)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(errKey.Key);
                Contracts.AssertValue(args);

                _txb.SetInfo(node, new DottedNameInfo(node));
                _txb.ErrorContainer.Error(node, errKey, args);
                _txb.SetType(node, DType.Error);
            }

            // Returns true if the currentControl is a replicating child of the controlName being passed and the propertyName passed is
            // a nestedAware out property of the parent and currentProperty is not a behaviour property.
            private bool UsesParentsNestedAwareProperty(IExternalControl controlInfo, DName propertyName)
            {
                Contracts.AssertValue(controlInfo);
                Contracts.Assert(propertyName.IsValid);

                if (_nameResolver == null || _nameResolver.CurrentEntity is not IExternalControl currentControlInfo)
                {
                    return false;
                }

                return currentControlInfo.IsReplicable &&
                        !currentControlInfo.Template.HasProperty(_nameResolver.CurrentProperty.Value, PropertyRuleCategory.Behavior) &&
                        controlInfo.Template.ReplicatesNestedControls &&
                        currentControlInfo.IsDescendentOf(controlInfo) &&
                        controlInfo.Template.NestedAwareTableOutputs.Contains(propertyName);
            }

            public override void PostVisit(UnaryOpNode node)
            {
                AssertValid();

                var childType = _txb.GetType(node.Child);

                var res = CheckUnaryOpCore(_txb.ErrorContainer, node, childType);

                foreach (var coercion in res.Coercions)
                {
                    _txb.SetCoercedType(coercion.Node, coercion.CoercedType);
                }

                _txb.SetType(res.Node, res.NodeType);

                _txb.SetSideEffects(node, _txb.HasSideEffects(node.Child));
                _txb.SetStateful(node, _txb.IsStateful(node.Child));
                _txb.SetContextual(node, _txb.IsContextual(node.Child));
                _txb.SetConstant(node, _txb.IsConstant(node.Child));
                _txb.SetSelfContainedConstant(node, _txb.IsSelfContainedConstant(node.Child));
                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Child));
                _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(node.Child));
                _txb.SetIsUnliftable(node, _txb.IsUnliftable(node.Child));
            }

            public override void PostVisit(BinaryOpNode node)
            {
                AssertValid();

                var leftType = _txb.GetType(node.Left);
                var rightType = _txb.GetType(node.Right);

                var res = CheckBinaryOpCore(_txb.ErrorContainer, node, leftType, rightType, _txb.Document != null && _txb.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled);

                foreach (var coercion in res.Coercions)
                {
                    _txb.SetCoercedType(coercion.Node, coercion.CoercedType);
                }

                _txb.SetType(res.Node, res.NodeType);

                _txb.SetSideEffects(node, _txb.HasSideEffects(node.Left) || _txb.HasSideEffects(node.Right));
                _txb.SetStateful(node, _txb.IsStateful(node.Left) || _txb.IsStateful(node.Right));
                _txb.SetContextual(node, _txb.IsContextual(node.Left) || _txb.IsContextual(node.Right));
                _txb.SetConstant(node, _txb.IsConstant(node.Left) && _txb.IsConstant(node.Right));
                _txb.SetSelfContainedConstant(node, _txb.IsSelfContainedConstant(node.Left) && _txb.IsSelfContainedConstant(node.Right));
                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Left, node.Right));
                _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(node.Left));
                _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(node.Right));
                _txb.SetIsUnliftable(node, _txb.IsUnliftable(node.Left) || _txb.IsUnliftable(node.Right));
            }

            public override void PostVisit(AsNode node)
            {
                Contracts.AssertValue(node);

                // As must be either the top node, or an immediate child of a call node
                if (node.Id != _txb.Top.Id &&
                    (node.Parent?.Kind != NodeKind.List || node.Parent?.Parent?.Kind != NodeKind.Call))
                {
                    _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrAsNotInContext);
                }
                else if (node.Id == _txb.Top.Id &&
                    (_nameResolver == null || !(_nameResolver.CurrentEntity is IExternalControl currentControl) ||
                    !currentControl.Template.ReplicatesNestedControls ||
                    !(currentControl.Template.ThisItemInputInvariantName == _nameResolver.CurrentProperty)))
                {
                    _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrAsNotInContext);
                }

                _txb.SetInfo(node, new AsInfo(node, node.Right.Name));

                var left = node.Left;
                _txb.CheckAndMarkAsPageable(node);
                _txb.CheckAndMarkAsDelegatable(node);
                _txb.SetType(node, _txb.GetType(left));
                _txb.SetSideEffects(node, _txb.HasSideEffects(left));
                _txb.SetStateful(node, _txb.IsStateful(left));
                _txb.SetContextual(node, _txb.IsContextual(left));
                _txb.SetConstant(node, _txb.IsConstant(left));
                _txb.SetSelfContainedConstant(node, _txb.IsSelfContainedConstant(left));
                _txb.SetScopeUseSet(node, _txb.GetScopeUseSet(left));
                _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(left));
                _txb.SetIsUnliftable(node, _txb.IsUnliftable(node.Left));
            }

            private void SetVariadicNodePurity(VariadicBase node)
            {
                Contracts.AssertValue(node);
                Contracts.AssertIndex(node.Id, _txb.IdLim);
                Contracts.AssertValue(node.Children);

                // Check for side-effects and statefulness of operation
                var hasSideEffects = false;
                var isStateful = false;
                var isContextual = false;
                var isConstant = true;
                var isSelfContainedConstant = true;
                var isBlockScopedConstant = true;
                var isUnliftable = false;

                foreach (var child in node.Children)
                {
                    hasSideEffects |= _txb.HasSideEffects(child);
                    isStateful |= _txb.IsStateful(child);
                    isContextual |= _txb.IsContextual(child);
                    isConstant &= _txb.IsConstant(child);
                    isSelfContainedConstant &= _txb.IsSelfContainedConstant(child);
                    isBlockScopedConstant &= _txb.IsBlockScopedConstant(child) || _txb.IsPure(child);
                    isUnliftable |= _txb.IsUnliftable(child);
                }

                // If any child is unliftable then the full expression is unliftable
                _txb.SetIsUnliftable(node, isUnliftable);

                _txb.SetSideEffects(node, hasSideEffects);
                _txb.SetStateful(node, isStateful);
                _txb.SetContextual(node, isContextual);
                _txb.SetConstant(node, isConstant);
                _txb.SetSelfContainedConstant(node, isSelfContainedConstant);

                if (isBlockScopedConstant)
                {
                    _txb.SetBlockScopedConstantNode(node);
                }
            }

            public override void PostVisit(VariadicOpNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                switch (node.Op)
                {
                    case VariadicOp.Chain:
                        _txb.SetType(node, _txb.GetType(node.Children.Last()));
                        break;

                    default:
                        Contracts.Assert(false);
                        _txb.SetType(node, DType.Error);
                        break;
                }

                // Determine constancy.
                var isConstant = true;
                var isSelfContainedConstant = true;

                foreach (var child in node.Children)
                {
                    isConstant &= _txb.IsConstant(child);
                    isSelfContainedConstant &= _txb.IsSelfContainedConstant(child);
                    if (!isConstant && !isSelfContainedConstant)
                    {
                        break;
                    }
                }

                _txb.SetConstant(node, isConstant);
                _txb.SetSelfContainedConstant(node, isSelfContainedConstant);

                SetVariadicNodePurity(node);
                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Children));
            }

            private static bool IsValidAccessToScopedProperty(IExternalControl lhsControl, IExternalControlProperty rhsProperty, IExternalControl currentControl, IExternalControlProperty currentProperty)
            {
                Contracts.AssertValue(lhsControl);
                Contracts.AssertValue(rhsProperty);
                Contracts.AssertValue(currentControl);
                Contracts.AssertValue(currentProperty);

                if (lhsControl.IsComponentControl &&
                   lhsControl.Template.ComponentType == ComponentType.CanvasComponent &&
                   (currentControl.IsComponentControl ||
                   (currentControl.TopParentOrSelf is IExternalControl { IsComponentControl: false })))
                {
                    // If current property is output property of the component then access is allowed.
                    // Or if the rhs property is out put property then it's allowed which could only be possible if the current control is component definition.
                    return currentProperty.IsImmutableOnInstance || rhsProperty.IsImmutableOnInstance;
                }

                return true;
            }

            private bool IsValidScopedPropertyFunction(CallNode node, CallInfo info)
            {
                Contracts.AssertValue(node);
                Contracts.AssertIndex(node.Id, _txb.IdLim);
                Contracts.AssertValue(info);
                Contracts.AssertValue(_txb.Control);

                var currentControl = _txb.Control;
                var currentProperty = _txb.Property;
                if (currentControl.IsComponentControl && currentControl.Template.ComponentType != ComponentType.CanvasComponent)
                {
                    return true;
                }

                var infoTexlFunction = info.Function;
                if (_txb._glue.IsComponentScopedPropertyFunction(infoTexlFunction))
                {
                    // Component custom behavior properties can only be accessed by controls within a component.
                    if (_txb.Document != null && _txb.Document.TryGetControlByUniqueId(infoTexlFunction.Namespace.Name.Value, out var lhsControl) &&
                        lhsControl.Template.TryGetProperty(infoTexlFunction.Name, out var rhsProperty))
                    {
                        return IsValidAccessToScopedProperty(lhsControl, rhsProperty, currentControl, currentProperty);
                    }
                }

                return true;
            }

            private void SetCallNodePurity(CallNode node, CallInfo info)
            {
                Contracts.AssertValue(node);
                Contracts.AssertIndex(node.Id, _txb.IdLim);
                Contracts.AssertValue(node.Args);

                var hasSideEffects = _txb.HasSideEffects(node.Args);
                var isStateFul = _txb.IsStateful(node.Args);

                if (info?.Function != null)
                {
                    var infoTexlFunction = info.Function;

                    if (_txb._glue.IsComponentScopedPropertyFunction(infoTexlFunction))
                    {
                        // We only have to check the property's rule and the calling arguments for purity as scoped variables
                        // (default values) are by definition data rules and therefore always pure.
                        if (_txb.Document != null && _txb.Document.TryGetControlByUniqueId(infoTexlFunction.Namespace.Name.Value, out var ctrl) &&
                            ctrl.TryGetRule(new DName(infoTexlFunction.Name), out var rule))
                        {
                            hasSideEffects |= rule.Binding.HasSideEffects(rule.Binding.Top);
                            isStateFul |= rule.Binding.IsStateful(rule.Binding.Top);
                        }
                    }
                    else
                    {
                        hasSideEffects |= !infoTexlFunction.IsSelfContained;
                        isStateFul |= !infoTexlFunction.IsStateless;
                    }
                }

                _txb.SetSideEffects(node, hasSideEffects);
                _txb.SetStateful(node, isStateFul);
                _txb.SetContextual(node, _txb.IsContextual(node.Args)); // The head of a function cannot be contextual at the moment

                // Nonempty variable weight containing variable "x" implies this node or a node that is to be
                // evaluated before this node is non pure and modifies "x"
                _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(node.Args));

                // True if this node or one of its children contains any element of this node's variable weight
                _txb.SetIsUnliftable(node, _txb.IsUnliftable(node.Args));
            }

            private ScopeUseSet GetCallNodeScopeUseSet(CallNode node, CallInfo info)
            {
                Contracts.AssertValue(node);

                // If there are lambda params, find their scopes
                if (info?.Function == null)
                {
                    return ScopeUseSet.GlobalsOnly;
                }
                else if (!info.Function.HasLambdas)
                {
                    return JoinScopeUseSets(node.Args);
                }
                else
                {
                    var args = node.Args.Children;
                    var set = ScopeUseSet.GlobalsOnly;

                    for (var i = 0; i < args.Length; i++)
                    {
                        var argScopeUseSet = _txb.GetScopeUseSet(args[i]);

                        // Translate the set to the parent (invocation) scope, to indicate that we are moving outside the lambda.
                        if (i <= info.Function.MaxArity && info.Function.IsLambdaParam(i))
                        {
                            argScopeUseSet = argScopeUseSet.TranslateToParentScope();
                        }

                        set = set.Union(argScopeUseSet);
                    }

                    return set;
                }
            }

            private bool TryGetFunctionNameLookupInfo(CallNode node, DPath functionNamespace, out NameLookupInfo lookupInfo)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValid(functionNamespace);

                lookupInfo = default;
                if (!(node.HeadNode is DottedNameNode dottedNameNode))
                {
                    return false;
                }

                if (!(dottedNameNode.Left is FirstNameNode) &&
                    !(dottedNameNode.Left is ParentNode) &&
                    !(dottedNameNode.Left is SelfNode))
                {
                    return false;
                }

                if (!_nameResolver.LookupGlobalEntity(functionNamespace.Name, out lookupInfo) ||
                    lookupInfo.Data == null ||
                    !(lookupInfo.Data is IExternalControl))
                {
                    return false;
                }

                return true;
            }

            public override bool PreVisit(BinaryOpNode node)
            {
                Contracts.AssertValue(node);

                var volatileVariables = _txb.GetVolatileVariables(node);
                _txb.AddVolatileVariables(node.Left, volatileVariables);
                _txb.AddVolatileVariables(node.Right, volatileVariables);

                return true;
            }

            public override bool PreVisit(UnaryOpNode node)
            {
                Contracts.AssertValue(node);

                var volatileVariables = _txb.GetVolatileVariables(node);
                _txb.AddVolatileVariables(node.Child, volatileVariables);

                return true;
            }

            /// <summary>
            /// Accepts each child, records which identifiers are affected by each child and sets the binding
            /// appropriately.
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public override bool PreVisit(VariadicOpNode node)
            {
                var runningWeight = _txb.GetVolatileVariables(node);
                var isUnliftable = false;

                foreach (var child in node.Children)
                {
                    _txb.AddVolatileVariables(child, runningWeight);
                    child.Accept(this);
                    runningWeight = runningWeight.Union(_txb.GetVolatileVariables(child));
                    isUnliftable |= _txb.IsUnliftable(child);
                }

                _txb.AddVolatileVariables(node, runningWeight);
                _txb.SetIsUnliftable(node, isUnliftable);

                PostVisit(node);
                return false;
            }

            private void PreVisitHeadNode(CallNode node)
            {
                Contracts.AssertValue(node);

                // We want to set the correct error type. This is important for component instance rule replacement logic.
                if (_nameResolver == null && (node.HeadNode is DottedNameNode))
                {
                    node.HeadNode.Accept(this);
                }
            }

            private static void ArityError(int minArity, int maxArity, TexlNode node, int actual, IErrorContainer errors)
            {
                if (maxArity == int.MaxValue)
                {
                    errors.Error(node, TexlStrings.ErrBadArityMinimum, actual, minArity);
                }
                else if (minArity != maxArity)
                {
                    errors.Error(node, TexlStrings.ErrBadArityRange, actual, minArity, maxArity);
                }
                else
                {
                    errors.Error(node, TexlStrings.ErrBadArity, actual, minArity);
                }
            }

            public override bool PreVisit(CallNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                var funcNamespace = _txb.GetFunctionNamespace(node, this);
                var overloads = LookupFunctions(funcNamespace, node.Head.Name.Value);
                if (!overloads.Any())
                {
                    if (funcNamespace.ToString() != string.Empty)
                    {
                        _txb.ErrorContainer.Error(node, TexlStrings.ErrUnknownNamespaceFunction, node.Head.Name.Value, funcNamespace.ToString());
                    }
                    else
                    {
                        _txb.ErrorContainer.Error(node, TexlStrings.ErrUnknownFunction, node.Head.Name.Value);
                    }

                    _txb.SetInfo(node, new CallInfo(node));
                    _txb.SetType(node, DType.Error);

                    PreVisitHeadNode(node);
                    PreVisitBottomUp(node, 0);
                    FinalizeCall(node);

                    return false;
                }

                var overloadsWithMetadataTypeSupportedArgs = overloads.Where(func => func.SupportsMetadataTypeArg && !func.HasLambdas);
                if (overloadsWithMetadataTypeSupportedArgs.Any())
                {
                    // Overloads are not supported for such functions yet.
                    Contracts.Assert(overloadsWithMetadataTypeSupportedArgs.Count() == 1);

                    PreVisitMetadataArg(node, overloadsWithMetadataTypeSupportedArgs.FirstOrDefault());
                    FinalizeCall(node);
                    return false;
                }

                // If there are no overloads with lambdas or identifiers, we can continue the visitation and
                // yield to the normal overload resolution.
                var overloadsWithLambdasOrIdentifiers = overloads.Where(func => func.HasLambdas || func.HasColumnIdentifiers);
                if (!overloadsWithLambdasOrIdentifiers.Any())
                {
                    // We may still need a scope to determine inline-record types
                    Scope maybeScope = null;
                    var startArg = 0;

                    // Construct a scope if display names are enabled and this function requires a data source scope for inline records
                    if ((_txb.Document?.Properties?.EnabledFeatures?.IsUseDisplayNameMetadataEnabled ?? true) &&
                        overloads.Where(func => func.RequiresDataSourceScope).Any() && node.Args.Count > 0)
                    {
                        // Visit the first arg if it exists. This will give us the scope type for any subsequent lambda/predicate args.
                        var nodeInp = node.Args.Children[0];
                        nodeInp.Accept(this);

                        if (nodeInp.Kind == NodeKind.As)
                        {
                            _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrAsNotInContext);
                        }

                        // Only if there is a projection map associated with this will we need to set a scope
                        var typescope = _txb.GetType(nodeInp);

                        if ((typescope.AssociatedDataSources.Any() || typescope.DisplayNameProvider != null) && typescope.IsTable)
                        {
                            maybeScope = new Scope(node, _currentScope, typescope.ToRecord(), createsRowScope: false);
                        }

                        startArg++;
                    }

                    PreVisitHeadNode(node);
                    PreVisitBottomUp(node, startArg, maybeScope);
                    FinalizeCall(node);
                    return false;
                }

                var numOverloads = overloads.Count();

                var overloadsWithUntypedObjectLambdas = overloadsWithLambdasOrIdentifiers.Where(func => func.ParamTypes.Any() && func.ParamTypes[0] == DType.UntypedObject);
                TexlFunction overloadWithUntypedObjectLambda = null;
                if (overloadsWithUntypedObjectLambdas.Any())
                {
                    Contracts.Assert(overloadsWithUntypedObjectLambdas.Count() == 1, "Incorrect multiple overloads with both UntypedObject and lambdas.");
                    overloadWithUntypedObjectLambda = overloadsWithUntypedObjectLambdas.Single();

                    // As an extraordinarily special case, we ignore untype object lambdas for now, and type check as normal
                    // using the function without untyped object params. This only works if both functions have exactly
                    // the same arity (this is enforced below). We can't simply check the type of the first argument
                    // because the argument list might be empty. Arity checks below require that we already picked an override.
                    overloadsWithLambdasOrIdentifiers = overloadsWithLambdasOrIdentifiers.Where(func => func.ParamTypes.Any() && func.ParamTypes[0] != DType.UntypedObject);
                    numOverloads -= 1;
                }

                // We support a single overload with lambdas. Otherwise we have a conceptual chicken-and-egg
                // problem, whereby in order to bind the lambda args we need the precise overload (for
                // its lambda mask), which in turn requires binding the args (for their types).
                Contracts.Assert(overloadsWithLambdasOrIdentifiers.Count() == 1, "Incorrect multiple overloads with lambdas.");
                var maybeFunc = overloadsWithLambdasOrIdentifiers.Single();

                if (overloadWithUntypedObjectLambda != null)
                {
                    // Both overrides must have exactly the same arity.
                    Contracts.Assert(maybeFunc.MaxArity == overloadWithUntypedObjectLambda.MaxArity);
                    Contracts.Assert(maybeFunc.MinArity == overloadWithUntypedObjectLambda.MinArity);

                    // There also cannot be optional parameters
                    Contracts.Assert(maybeFunc.MinArity == maybeFunc.MaxArity);
                }

                var scopeInfo = maybeFunc.ScopeInfo;
                IDelegationMetadata metadata = null;

                Scope scopeNew = null;
                IExpandInfo expandInfo;

                // Check for matching arities.
                var carg = node.Args.Count;
                if (carg < maybeFunc.MinArity || carg > maybeFunc.MaxArity)
                {
                    var argCountVisited = 0;
                    if (numOverloads == 1)
                    {
                        var scope = DType.Invalid;
                        var required = false;
                        DName scopeIdentifier = default;
                        if (scopeInfo.ScopeType != null)
                        {
                            scopeNew = new Scope(node, _currentScope, scopeInfo.ScopeType, skipForInlineRecords: maybeFunc.SkipScopeForInlineRecords);
                        }
                        else if (carg > 0)
                        {
                            // Visit the first arg. This will give us the scope type for any subsequent lambda/predicate args.
                            var nodeInp = node.Args.Children[0];
                            nodeInp.Accept(this);

                            // At this point we know the type of the first argument, so we can check for untyped objects
                            if (overloadWithUntypedObjectLambda != null && _txb.GetType(nodeInp) == DType.UntypedObject)
                            {
                                maybeFunc = overloadWithUntypedObjectLambda;
                                scopeInfo = maybeFunc.ScopeInfo;
                            }

                            // Determine the Scope Identifier using the 1st arg
                            required = _txb.GetScopeIdent(nodeInp, _txb.GetType(nodeInp), out scopeIdentifier);

                            if (scopeInfo.CheckInput(nodeInp, _txb.GetType(nodeInp), out scope))
                            {
                                if (_txb.TryGetEntityInfo(nodeInp, out expandInfo))
                                {
                                    scopeNew = new Scope(node, _currentScope, scope, scopeIdentifier, required, expandInfo, skipForInlineRecords: maybeFunc.SkipScopeForInlineRecords);
                                }
                                else
                                {
                                    maybeFunc.TryGetDelegationMetadata(node, _txb, out metadata);
                                    scopeNew = new Scope(node, _currentScope, scope, scopeIdentifier, required, metadata, skipForInlineRecords: maybeFunc.SkipScopeForInlineRecords);
                                }
                            }

                            argCountVisited = 1;
                        }

                        // If there is only one function with this name and its arity doesn't match,
                        // that means the invocation is erroneous.
                        ArityError(maybeFunc.MinArity, maybeFunc.MaxArity, node, carg, _txb.ErrorContainer);
                        _txb.SetInfo(node, new CallInfo(maybeFunc, node, scope, scopeIdentifier, required, _currentScope.Nest));
                        _txb.SetType(node, maybeFunc.ReturnType);
                    }

                    // Either way continue the visitation. If we do have overloads,
                    // a different overload with no lambdas may match (including the arity).
                    PreVisitBottomUp(node, argCountVisited, scopeNew);
                    FinalizeCall(node);

                    return false;
                }

                // All functions with lambdas have at least one arg.
                Contracts.Assert(carg > 0);

                // The zeroth arg should not be a lambda. Instead it defines the context type for the lambdas.
                Contracts.Assert(!maybeFunc.IsLambdaParam(0));

                var args = node.Args.Children;
                var argTypes = new DType[args.Length];

                // We need to know which variables are volatile in case the first argument is or contains a
                // reference to a volatile variable and we need to control its liftability
                var volatileVariables = _txb.GetVolatileVariables(node);

                // Visit the first arg. This will give us the scope type for the subsequent lambda args.
                var nodeInput = args[0];
                _txb.AddVolatileVariables(nodeInput, volatileVariables);
                nodeInput.Accept(this);

                // At this point we know the type of the first argument, so we can check for untyped objects
                if (overloadWithUntypedObjectLambda != null && _txb.GetType(nodeInput) == DType.UntypedObject)
                {
                    maybeFunc = overloadWithUntypedObjectLambda;
                    scopeInfo = maybeFunc.ScopeInfo;
                }

                FirstNameNode dsNode;
                if (maybeFunc.TryGetDataSourceNodes(node, _txb, out var dsNodes) && ((dsNode = dsNodes.FirstOrDefault()) != default(FirstNameNode)))
                {
                    _currentScopeDsNodeId = dsNode.Id;
                }

                var typeInput = argTypes[0] = _txb.GetType(nodeInput);

                // Get the cursor type for this arg. Note we're not adding document errors at this point.
                DType typeScope;
                DName scopeIdent = default;
                var identRequired = false;
                var fArgsValid = true;
                if (scopeInfo.ScopeType != null)
                {
                    typeScope = scopeInfo.ScopeType;

                    // For functions with a Scope Type, there is no ScopeIdent needed
                }
                else
                {
                    fArgsValid = scopeInfo.CheckInput(nodeInput, typeInput, out typeScope);

                    // Determine the scope identifier using the first node for lambda params
                    identRequired = _txb.GetScopeIdent(nodeInput, typeScope, out scopeIdent);
                }

                if (!fArgsValid)
                {
                    if (numOverloads == 1)
                    {
                        // If there is a single function with this name, and the first arg is not
                        // a good match, then we have an erroneous invocation.
                        _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, nodeInput, TexlStrings.ErrBadType);
                        _txb.ErrorContainer.Error(node, TexlStrings.ErrInvalidArgs_Func, maybeFunc.Name);
                        _txb.SetInfo(node, new CallInfo(maybeFunc, node, typeScope, scopeIdent, identRequired, _currentScope.Nest));
                        _txb.SetType(node, maybeFunc.ReturnType);
                    }

                    // Yield to the normal overload resolution either way. We already visited and
                    // bound the first argument, hence the 1'.
                    PreVisitBottomUp(node, 1);
                    FinalizeCall(node);

                    return false;
                }

                // At this point we know we have an invocation of our function with lambdas (as opposed
                // to an invocation of a different overload). Pin that, and make a best effort to match
                // the rest of the args. Binding failures along the way become proper document errors.

                // We don't want to check and mark this function as async for now as IsAsyncInvocation function calls into IsServerDelegatable which
                // requires more contexts about the args which is only available after we visit all the children. So delay this after visiting
                // children.
                _txb.SetInfo(node, new CallInfo(maybeFunc, node, typeScope, scopeIdent, identRequired, _currentScope.Nest), markIfAsync: false);

                if (_txb.TryGetEntityInfo(nodeInput, out expandInfo))
                {
                    scopeNew = new Scope(node, _currentScope, typeScope, scopeIdent, identRequired, expandInfo, skipForInlineRecords: maybeFunc.SkipScopeForInlineRecords);
                }
                else
                {
                    maybeFunc.TryGetDelegationMetadata(node, _txb, out metadata);
                    scopeNew = new Scope(node, _currentScope, typeScope, scopeIdent, identRequired, metadata, skipForInlineRecords: maybeFunc.SkipScopeForInlineRecords);
                }

                // Process the rest of the args.
                for (var i = 1; i < carg; i++)
                {
                    Contracts.Assert(_currentScope == scopeNew || _currentScope == scopeNew.Parent);

                    if (maybeFunc.AllowsRowScopedParamDelegationExempted(i))
                    {
                        _txb.SetSupportingRowScopedDelegationExemptionNode(args[i]);
                    }

                    if (maybeFunc.IsEcsExcemptedLambda(i))
                    {
                        _txb.SetEcsExcemptLambdaNode(args[i]);
                    }

                    if (volatileVariables != null)
                    {
                        _txb.AddVolatileVariables(args[i], volatileVariables);
                    }

                    var isIdentifier = args[i] is FirstNameNode &&
                        _features.HasFlag(Features.SupportColumnNamesAsIdentifiers) &&
                        maybeFunc.IsIdentifierParam(i);

                    // Use the new scope only for lambda args.
                    _currentScope = (maybeFunc.IsLambdaParam(i) && scopeInfo.AppliesToArgument(i)) ? scopeNew : scopeNew.Parent;

                    if (!isIdentifier)
                    {
                        args[i].Accept(this);
                        _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(args[i]));
                        argTypes[i] = _txb.GetType(args[i]);

                        Contracts.Assert(argTypes[i].IsValid);
                    }
                    else
                    {
                        // This is an identifier, no associated type, let's make it invalid
                        argTypes[i] = DType.Invalid;
                    }

                    // Async lambdas are not (yet) supported for this function. Flag these with errors.
                    if (_txb.IsAsync(args[i]) && !scopeInfo.SupportsAsyncLambdas)
                    {
                        fArgsValid = false;
                        _txb.ErrorContainer.Error(DocumentErrorSeverity.Severe, node, TexlStrings.ErrAsyncLambda);
                    }

                    // Accept should leave the scope as it found it.
                    Contracts.Assert(_currentScope == ((maybeFunc.IsLambdaParam(i) && scopeInfo.AppliesToArgument(i)) ? scopeNew : scopeNew.Parent));
                }

                // Now check and mark the path as async.
                if (maybeFunc.IsAsyncInvocation(node, _txb))
                {
                    _txb.FlagPathAsAsync(node);
                }

                _currentScope = scopeNew.Parent;
                PostVisit(node.Args);

                if (maybeFunc.HasColumnIdentifiers && _features.HasFlag(Features.SupportColumnNamesAsIdentifiers))
                {
                    var i = 0;

                    foreach (var arg in args)
                    {
                        if (arg is FirstNameNode firstNameNode && maybeFunc.IsIdentifierParam(i))
                        {
                            _ = GetLogicalNodeNameAndUpdateDisplayNames(argTypes[0], firstNameNode.Ident, out _);

                        }

                        i++;
                    }
                }

                // Typecheck the invocation and infer the return type.
                fArgsValid &= maybeFunc.HandleCheckInvocation(_txb, args, argTypes, _txb.ErrorContainer, out var returnType, out var nodeToCoercedTypeMap);

                // This is done because later on, if a CallNode has a return type of Error, you can assert HasErrors on it.
                // This was not done for UnaryOpNodes, BinaryOpNodes, CompareNodes.
                // This doesn't need to be done on the other nodes (but can) because their return type doesn't depend
                // on their argument types.
                if (!fArgsValid)
                {
                    _txb.ErrorContainer.Error(DocumentErrorSeverity.Severe, node, TexlStrings.ErrInvalidArgs_Func, maybeFunc.Name);
                }

                // Set the inferred return type for the node.
                _txb.SetType(node, returnType);

                if (fArgsValid && nodeToCoercedTypeMap != null)
                {
                    foreach (var nodeToCoercedTypeKvp in nodeToCoercedTypeMap)
                    {
                        _txb.SetCoercedType(nodeToCoercedTypeKvp.Key, nodeToCoercedTypeKvp.Value);
                    }
                }

                FinalizeCall(node);

                // We fully processed the call, so don't visit children or call PostVisit.
                return false;
            }

            private void FinalizeCall(CallNode node)
            {
                Contracts.AssertValue(node);

                var callInfo = _txb.GetInfo(node);

                // Set the node purity and context
                SetCallNodePurity(node, callInfo);
                _txb.SetScopeUseSet(node, GetCallNodeScopeUseSet(node, callInfo));

                var func = callInfo?.Function;
                if (func == null)
                {
                    return;
                }

                // Invalid datasources always result in error
                if (func.IsBehaviorOnly && !_txb.BindingConfig.AllowsSideEffects)
                {
                    _txb.ErrorContainer.EnsureError(node, TexlStrings.ErrBehaviorPropertyExpected);
                }

                // Test-only functions can only be used within test cases.
                else if (func.IsTestOnly && _txb.Property != null && !_txb.Property.IsTestCaseProperty)
                {
                    _txb.ErrorContainer.EnsureError(node, TexlStrings.ErrTestPropertyExpected);
                }

                // Auto-refreshable functions cannot be used in behavior rules.
                else if (func.IsAutoRefreshable && _txb.BindingConfig.AllowsSideEffects)
                {
                    _txb.ErrorContainer.EnsureError(node, TexlStrings.ErrAutoRefreshNotAllowed);
                }

                // Give warning if returning dynamic metadata without a known dynamic type
                else if (func.IsDynamic && _nameResolver.Document.Properties.EnabledFeatures.IsDynamicSchemaEnabled)
                {
                    if (!func.CheckForDynamicReturnType(_txb, node.Args.Children))
                    {
                        _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, node, TexlStrings.WarnDynamicMetadata);
                    }
                }
                else if (_txb.Control != null && _txb.Property != null && !IsValidScopedPropertyFunction(node, callInfo))
                {
                    var errorMessage = callInfo.Function.IsBehaviorOnly ? TexlStrings.ErrUnSupportedComponentBehaviorInvocation : TexlStrings.ErrUnSupportedComponentDataPropertyAccess;
                    _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Critical, node, errorMessage);
                }

                // Apply custom function validation last
                else if (!func.PostVisitValidation(_txb, node))
                {
                    // Check to see if we are a side-effectful function operating on on invalid datasource.
                    if (IsIncorrectlySideEffectful(node, out var errorKey, out var badAncestor))
                    {
                        _txb.ErrorContainer.EnsureError(node, errorKey, badAncestor.Head.Name);
                    }
                }

                _txb.CheckAndMarkAsDelegatable(node);
                _txb.CheckAndMarkAsPageable(node, func);

                // A function will produce a constant output (and have no side-effects, which is important for
                // caching/precomputing the result) iff the function is pure and its arguments are constant.
                _txb.SetConstant(node, func.IsPure && _txb.IsConstant(node.Args));
                _txb.SetSelfContainedConstant(node, func.IsPure && _txb.IsSelfContainedConstant(node.Args));

                // Mark node as blockscoped constant if the function's return value only depends on the global variable
                // This node will skip delegation check, be codegened as constant and be simply passed into the delegation query.
                // e.g. Today() in formula Filter(CDS, CreatedDate < Today())
                if (func.IsGlobalReliant || (func.IsPure && _txb.IsBlockScopedConstant(node.Args)))
                {
                    _txb.SetBlockScopedConstantNode(node);
                }

                // Update field projection info
                if (_txb.QueryOptions != null)
                {
                    func.UpdateDataQuerySelects(node, _txb, _txb.QueryOptions);
                }
            }

            private bool IsIncorrectlySideEffectful(CallNode node, out ErrorResourceKey errorKey, out CallNode badAncestor)
            {
                Contracts.AssertValue(node);

                badAncestor = null;
                errorKey = new ErrorResourceKey();

                var call = _txb.GetInfo(node).VerifyValue();
                var func = call.Function;
                if (func == null || func.IsSelfContained)
                {
                    return false;
                }

                if (!func.TryGetDataSource(node, _txb, out var ds))
                {
                    ds = null;
                }

                var ancestorScope = _currentScope;
                while (ancestorScope != null)
                {
                    if (ancestorScope.Call != null)
                    {
                        var ancestorCall = _txb.GetInfo(ancestorScope.Call);

                        // For record-scoped rules, if we are processing a nested call node, it's possible the node info may not be set yet
                        // In that case, verify that the node has overloads that support record scoping.
                        if (ancestorCall == null && LookupFunctions(ancestorScope.Call.Head.Namespace, ancestorScope.Call.Head.Name.Value).Any(overload => overload.RequiresDataSourceScope))
                        {
                            ancestorScope = ancestorScope.Parent;
                            continue;
                        }

                        var ancestorFunc = ancestorCall.Function;
                        var ancestorScopeInfo = ancestorCall.Function?.ScopeInfo;

                        // Check for bad scope modification
                        if (ancestorFunc != null && ancestorScopeInfo != null && ds != null && ancestorScopeInfo.IteratesOverScope)
                        {
                            if (ancestorFunc.TryGetDataSource(ancestorScope.Call, _txb, out var ancestorDs) && ancestorDs == ds)
                            {
                                errorKey = TexlStrings.ErrScopeModificationLambda;
                                badAncestor = ancestorScope.Call;
                                return true;
                            }
                        }

                        // Check for completely blocked functions.
                        if (ancestorFunc != null &&
                            ancestorScopeInfo != null &&
                            ancestorScopeInfo.HasNondeterministicOperationOrder &&
                            !func.AllowedWithinNondeterministicOperationOrder)
                        {
                            errorKey = TexlStrings.ErrFunctionDisallowedWithinNondeterministicOperationOrder;
                            badAncestor = ancestorScope.Call;
                            return true;
                        }
                    }

                    // Pop up to the next scope.
                    ancestorScope = ancestorScope.Parent;
                }

                return false;
            }

            public override void PostVisit(CallNode node)
            {
                Contracts.Assert(false, "Should never get here");
            }

            public override bool PreVisit(StrInterpNode node)
            {
                var runningWeight = _txb.GetVolatileVariables(node);
                var isUnliftable = false;

                // Make a binder-aware call to Concatenate
                _txb.GenerateCallNode(node);

                var args = node.Children;
                var argTypes = new DType[args.Length];

                // Process arguments
                for (var i = 0; i < args.Length; i++)
                {
                    var child = args[i];
                    _txb.AddVolatileVariables(child, runningWeight);
                    child.Accept(this);
                    argTypes[i] = _txb.GetType(args[i]);
                    runningWeight = runningWeight.Union(_txb.GetVolatileVariables(child));
                    isUnliftable |= _txb.IsUnliftable(child);
                }

                // Typecheck the node's children against the built-in Concatenate function
                var fArgsValid = BuiltinFunctionsCore.Concatenate.HandleCheckInvocation(_txb, args, argTypes, _txb.ErrorContainer, out var returnType, out var nodeToCoercedTypeMap);

                if (!fArgsValid)
                {
                    _txb.ErrorContainer.Error(DocumentErrorSeverity.Severe, node, TexlStrings.ErrInvalidStringInterpolation);
                }

                if (fArgsValid && nodeToCoercedTypeMap != null)
                {
                    foreach (var nodeToCoercedTypeKvp in nodeToCoercedTypeMap)
                    {
                        _txb.SetCoercedType(nodeToCoercedTypeKvp.Key, nodeToCoercedTypeKvp.Value);
                    }
                }

                _txb.SetType(node, returnType);

                _txb.AddVolatileVariables(node, runningWeight);
                _txb.SetIsUnliftable(node, isUnliftable);

                PostVisit(node);
                return false;
            }

            public override void PostVisit(StrInterpNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                // Determine constancy.
                var isConstant = true;
                var isSelfContainedConstant = true;

                foreach (var child in node.Children)
                {
                    isConstant &= _txb.IsConstant(child);
                    isSelfContainedConstant &= _txb.IsSelfContainedConstant(child);
                    if (!isConstant && !isSelfContainedConstant)
                    {
                        break;
                    }
                }

                _txb.SetConstant(node, isConstant);
                _txb.SetSelfContainedConstant(node, isSelfContainedConstant);

                SetVariadicNodePurity(node);
                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Children));
            }

            private bool TryGetAffectScopeVariableFunc(CallNode node, out TexlFunction func)
            {
                Contracts.AssertValue(node);

                var funcNamespace = _txb.GetFunctionNamespace(node, this);
                var overloads = LookupFunctions(funcNamespace, node.Head.Name.Value).Where(fnc => fnc.AffectsScopeVariable).ToArray();

                Contracts.Assert(overloads.Length == 1 || overloads.Length == 0, "Lookup Affect scopeVariable Function by CallNode should be 0 or 1");

                func = overloads.Length == 1 ? overloads[0].VerifyValue() : null;
                return func != null;
            }

            private void PreVisitMetadataArg(CallNode node, TexlFunction func)
            {
                AssertValid();
                Contracts.AssertValue(node);
                Contracts.AssertValue(func);
                Contracts.Assert(func.SupportsMetadataTypeArg);
                Contracts.Assert(!func.HasLambdas);

                var args = node.Args.Children;
                var argCount = args.Length;

                var returnType = func.ReturnType;
                for (var i = 0; i < argCount; i++)
                {
                    if (func.IsMetadataTypeArg(i))
                    {
                        args[i].Accept(_txb.BinderNodeMetadataArgTypeVisitor);
                    }
                    else
                    {
                        args[i].Accept(this);
                    }

                    if (args[i].Kind == NodeKind.As)
                    {
                        _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrAsNotInContext);
                    }
                }

                PostVisit(node.Args);

                var info = _txb.GetInfo(node);

                // If PreVisit resulted in errors for the node (and a non-null CallInfo),
                // we're done -- we have a match and appropriate errors logged already.
                if (_txb.ErrorContainer.HasErrors(node))
                {
                    Contracts.Assert(info != null);

                    return;
                }

                Contracts.AssertNull(info);

                _txb.SetInfo(node, new CallInfo(func, node));
                if (argCount < func.MinArity || argCount > func.MaxArity)
                {
                    ArityError(func.MinArity, func.MaxArity, node, argCount, _txb.ErrorContainer);
                    _txb.SetType(node, returnType);
                    return;
                }

                var argTypes = args.Select(_txb.GetType).ToArray();
                bool fArgsValid;

                // Typecheck the invocation and infer the return type.
                fArgsValid = func.HandleCheckInvocation(_txb, args, argTypes, _txb.ErrorContainer, out returnType, out _);
                if (!fArgsValid)
                {
                    _txb.ErrorContainer.Error(DocumentErrorSeverity.Severe, node, TexlStrings.ErrInvalidArgs_Func, func.Name);
                }

                _txb.SetType(node, returnType);
            }

            private void PreVisitBottomUp(CallNode node, int argCountVisited, Scope scopeNew = null)
            {
                AssertValid();
                Contracts.AssertValue(node);
                Contracts.AssertIndexInclusive(argCountVisited, node.Args.Count);
                Contracts.AssertValueOrNull(scopeNew);

                var args = node.Args.Children;
                var argCount = args.Length;

                var info = _txb.GetInfo(node);
                Contracts.AssertValueOrNull(info);
                Contracts.Assert(info == null || _txb.ErrorContainer.HasErrors(node));

                // Attempt to get the overloads, so we can determine the scope to use for datasource name matching
                // We're only interested in the overloads without lambdas, since those were
                // already processed in PreVisit.
                var funcNamespace = _txb.GetFunctionNamespace(node, this);
                var overloads = LookupFunctions(funcNamespace, node.Head.Name.Value)
                    .Where(fnc => !fnc.HasLambdas && !fnc.HasColumnIdentifiers)
                    .ToArray();

                TexlFunction funcWithScope = null;
                if (info != null && info.Function != null && scopeNew != null)
                {
                    funcWithScope = info.Function;
                }

                Contracts.Assert(scopeNew == null || funcWithScope != null || overloads.Any(fnc => fnc.RequiresDataSourceScope));

                var affectScopeVariable = TryGetAffectScopeVariableFunc(node, out var affectScopeVariablefunc);

                Contracts.Assert(affectScopeVariable ^ affectScopeVariablefunc == null);

                var volatileVariables = _txb.GetVolatileVariables(node);
                for (var i = argCountVisited; i < argCount; i++)
                {
                    Contracts.AssertValue(args[i]);

                    if (affectScopeVariable)
                    {
                        // If the function affects app/component variable, update the cache info if it is the arg affects scopeVariableName.
                        _txb.AffectsScopeVariableName = affectScopeVariablefunc.ScopeVariableNameAffectingArg() == i;
                    }

                    // Use the new scope only for lambda args and args with datasource scope for display name matching.
                    if (scopeNew != null)
                    {
                        if (overloads.Any(fnc => fnc.ArgMatchesDatasourceType(i)) || (i <= funcWithScope.MaxArity && funcWithScope.IsLambdaParam(i)))
                        {
                            _currentScope = scopeNew;
                        }
                        else
                        {
                            _currentScope = scopeNew.Parent;
                        }
                    }

                    if (volatileVariables != null)
                    {
                        _txb.AddVolatileVariables(args[i], volatileVariables);
                    }

                    args[i].Accept(this);

                    // In case weight was added during visitation
                    _txb.AddVolatileVariables(node, _txb.GetVolatileVariables(args[i]));

                    if (args[i].Kind == NodeKind.As)
                    {
                        _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrAsNotInContext);
                    }
                }

                if (scopeNew != null)
                {
                    _currentScope = scopeNew.Parent;
                }

                // Since variable weight may have changed as we accepted the children, we need to propagate
                // this value to the args
                var adjustedVolatileVariables = _txb.GetVolatileVariables(node);
                if (adjustedVolatileVariables != null)
                {
                    _txb.AddVolatileVariables(node.Args, adjustedVolatileVariables);
                }

                PostVisit(node.Args);

                // If PreVisit resulted in errors for the node (and a non-null CallInfo),
                // we're done -- we have a match and appropriate errors logged already.
                if (_txb.ErrorContainer.HasErrors(node))
                {
                    Contracts.Assert(info != null);

                    return;
                }

                Contracts.AssertNull(info);

                // There should be at least one possible match at this point.
                Contracts.Assert(overloads.Length > 0);

                if (overloads.Length > 1)
                {
                    PreVisitWithOverloadResolution(node, overloads);
                    return;
                }

                // We have a single possible match. Bind as usual, which will generate appropriate
                // document errors for incorrect arguments, etc.
                var func = overloads[0].VerifyValue();

                if (_txb._glue.IsComponentScopedPropertyFunction(func))
                {
                    if (TryGetFunctionNameLookupInfo(node, funcNamespace, out var lookupInfo))
                    {
                        var headNode = node.HeadNode as DottedNameNode;
                        Contracts.AssertValue(headNode);

                        UpdateBindKindUseFlags(BindKind.Control);
                        _txb.SetInfo(node, new CallInfo(func, node, lookupInfo.Data));
                    }
                    else
                    {
                        _txb.ErrorContainer.Error(node, TexlStrings.ErrInvalidName, node.Head.Name.Value);
                        _txb.SetInfo(node, new CallInfo(node));
                        _txb.SetType(node, DType.Error);
                        return;
                    }
                }
                else
                {
                    _txb.SetInfo(node, new CallInfo(func, node));
                }

                Contracts.Assert(!func.HasLambdas);

                var returnType = func.ReturnType;
                if (argCount < func.MinArity || argCount > func.MaxArity)
                {
                    ArityError(func.MinArity, func.MaxArity, node, argCount, _txb.ErrorContainer);
                    _txb.SetType(node, returnType);
                    return;
                }

                var modifiedIdentifiers = func.GetIdentifierOfModifiedValue(args, out _);
                if (modifiedIdentifiers != null)
                {
                    _txb.AddVolatileVariables(node, modifiedIdentifiers.Select(identifier => identifier.Name.ToString()).ToImmutableHashSet());
                }

                // Typecheck the invocation and infer the return type.
                var argTypes = args.Select(_txb.GetType).ToArray();
                bool fArgsValid;

                // Typecheck the invocation and infer the return type.
                fArgsValid = func.HandleCheckInvocation(_txb, args, argTypes, _txb.ErrorContainer, out returnType, out var nodeToCoercedTypeMap);

                if (!fArgsValid && !func.HasPreciseErrors)
                {
                    _txb.ErrorContainer.Error(DocumentErrorSeverity.Severe, node, TexlStrings.ErrInvalidArgs_Func, func.Name);
                }

                _txb.SetType(node, returnType);

                if (fArgsValid && nodeToCoercedTypeMap != null)
                {
                    foreach (var nodeToCoercedTypeKvp in nodeToCoercedTypeMap)
                    {
                        _txb.SetCoercedType(nodeToCoercedTypeKvp.Key, nodeToCoercedTypeKvp.Value);
                    }
                }
            }

            private IEnumerable<TexlFunction> LookupFunctions(DPath theNamespace, string name)
            {
                Contracts.Assert(theNamespace.IsValid);
                Contracts.AssertNonEmpty(name);

                if (_nameResolver != null)
                {
                    return _nameResolver.LookupFunctions(theNamespace, name);
                }
                else
                {
                    return Enumerable.Empty<TexlFunction>();
                }
            }

            private void PreVisitWithOverloadResolution(CallNode node, TexlFunction[] overloads)
            {
                Contracts.AssertValue(node);
                Contracts.AssertNull(_txb.GetInfo(node));
                Contracts.AssertValue(overloads);
                Contracts.Assert(overloads.Length > 1);
                Contracts.AssertAllValues(overloads);

                var args = node.Args.Children;
                var carg = args.Length;
                var argTypes = args.Select(_txb.GetType).ToArray();

                if (TryGetBestOverload(_txb.CheckTypesContext, _txb.ErrorContainer, node, argTypes, overloads, out var function, out var nodeToCoercedTypeMap, out var returnType))
                {
                    function.CheckSemantics(_txb, args, argTypes, _txb.ErrorContainer);

                    _txb.SetInfo(node, new CallInfo(function, node));
                    _txb.SetType(node, returnType);

                    // If we found an overload and this value is set then we require parameter conversion
                    if (nodeToCoercedTypeMap != null)
                    {
                        foreach (var nodeToCoercedTypeKvp in nodeToCoercedTypeMap)
                        {
                            _txb.SetCoercedType(nodeToCoercedTypeKvp.Key, nodeToCoercedTypeKvp.Value);
                        }
                    }

                    return;
                }

                var someFunc = FindBestErrorOverload(overloads, argTypes, carg);

                // If nothing matches even the arity, we're done.
                if (someFunc == null)
                {
                    var minArity = overloads.Min(func => func.MinArity);
                    var maxArity = overloads.Max(func => func.MaxArity);
                    ArityError(minArity, maxArity, node, carg, _txb.ErrorContainer);

                    _txb.SetInfo(node, new CallInfo(overloads.First(), node));
                    _txb.SetType(node, DType.Error);
                    return;
                }

                // We exhausted the overloads without finding an exact match, so post a document error.
                if (!someFunc.HasPreciseErrors)
                {
                    _txb.ErrorContainer.Error(node, TexlStrings.ErrInvalidArgs_Func, someFunc.Name);
                }

                // The final CheckInvocation call will post all the necessary document errors.
                someFunc.HandleCheckInvocation(_txb, args, argTypes, _txb.ErrorContainer, out returnType, out _);

                _txb.SetInfo(node, new CallInfo(someFunc, node));
                _txb.SetType(node, returnType);
            }

            public override void PostVisit(ListNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);
                SetVariadicNodePurity(node);
                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Children));
            }

            private bool IsRecordScopeFieldName(DName name, out Scope scope)
            {
                Contracts.AssertValid(name);

                if (!(_txb.Document?.Properties?.EnabledFeatures?.IsUseDisplayNameMetadataEnabled ?? true))
                {
                    scope = default;
                    return false;
                }

                // Look up the name in the current scopes, innermost to outermost.
                for (scope = _currentScope; scope != null; scope = scope.Parent)
                {
                    Contracts.AssertValue(scope);

                    // If scope type is a data source, the node may be a display name instead of logical.
                    // Attempt to get the logical name to use for type checking
                    if (!scope.SkipForInlineRecords && (DType.TryGetConvertedDisplayNameAndLogicalNameForColumn(scope.Type, name.Value, out var maybeLogicalName, out var tmp) ||
                        DType.TryGetLogicalNameForColumn(scope.Type, name.Value, out maybeLogicalName)))
                    {
                        name = new DName(maybeLogicalName);
                    }

                    if (scope.Type.TryGetType(name, out var tmpType))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override void PostVisit(RecordNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);

                var nodeType = DType.EmptyRecord;

                var dataSourceBoundType = DType.Invalid;
                if (node.SourceRestriction != null && node.SourceRestriction.Kind == NodeKind.FirstName)
                {
                    var sourceRestrictionNode = node.SourceRestriction.AsFirstName().VerifyValue();

                    var info = _txb.GetInfo(sourceRestrictionNode);
                    if (info?.Data is not IExternalDataSource dataSourceInfo)
                    {
                        _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, sourceRestrictionNode, TexlStrings.ErrExpectedDataSourceRestriction);
                        nodeType = DType.Error;
                    }
                    else
                    {
                        dataSourceBoundType = dataSourceInfo.Type;
                        nodeType = DType.CreateDTypeWithConnectedDataSourceInfoMetadata(nodeType, dataSourceBoundType.AssociatedDataSources, dataSourceBoundType.DisplayNameProvider);
                    }
                }

                var isSelfContainedConstant = true;
                for (var i = 0; i < node.Count; i++)
                {
                    var displayName = node.Ids[i].Name.Value;
                    var fieldName = node.Ids[i].Name;
                    DType fieldType;

                    isSelfContainedConstant &= _txb.IsSelfContainedConstant(node.Children[i]);

                    if (dataSourceBoundType != DType.Invalid)
                    {
                        fieldName = GetLogicalNodeNameAndUpdateDisplayNames(dataSourceBoundType, node.Ids[i], out displayName);

                        if (!dataSourceBoundType.TryGetType(fieldName, out fieldType))
                        {
                            dataSourceBoundType.ReportNonExistingName(FieldNameKind.Display, _txb.ErrorContainer, fieldName, node.Children[i]);
                            nodeType = DType.Error;
                        }
                        else if (!fieldType.Accepts(_txb.GetType(node.Children[i])))
                        {
                            _txb.ErrorContainer.EnsureError(
                                DocumentErrorSeverity.Severe,
                                node.Children[i],
                                TexlStrings.ErrColumnTypeMismatch_ColName_ExpectedType_ActualType,
                                displayName,
                                fieldType.GetKindString(),
                                _txb.GetType(node.Children[i]).GetKindString());
                            nodeType = DType.Error;
                        }
                    }
                    else
                    {
                        // For local records, check name/type match with scope
                        if (IsRecordScopeFieldName(fieldName, out var maybeScope))
                        {
                            fieldName = GetLogicalNodeNameAndUpdateDisplayNames(maybeScope.Type, node.Ids[i], out displayName);
                        }
                    }

                    if (nodeType != DType.Error)
                    {
                        if (nodeType.TryGetType(fieldName, out fieldType))
                        {
                            _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, node.Children[i], TexlStrings.ErrMultipleValuesForField_Name, displayName);
                        }
                        else
                        {
                            nodeType = nodeType.Add(fieldName, _txb.GetType(node.Children[i]));
                        }
                    }
                }

                _txb.SetType(node, nodeType);
                SetVariadicNodePurity(node);
                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Children));
                _txb.SetSelfContainedConstant(node, isSelfContainedConstant);
            }

            public override void PostVisit(TableNode node)
            {
                AssertValid();
                Contracts.AssertValue(node);
                var exprType = DType.Invalid;
                var isSelfContainedConstant = true;

                foreach (var child in node.Children)
                {
                    var childType = _txb.GetType(child);
                    isSelfContainedConstant &= _txb.IsSelfContainedConstant(child);

                    if (!exprType.IsValid)
                    {
                        exprType = childType;
                    }
                    else if (exprType.CanUnionWith(childType))
                    {
                        exprType = DType.Union(exprType, childType);
                    }
                    else if (childType.CoercesTo(exprType))
                    {
                        _txb.SetCoercedType(child, exprType);
                    }
                    else
                    {
                        _txb.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, child, TexlStrings.ErrTableDoesNotAcceptThisType);
                    }
                }

                DType tableType = exprType.IsValid
                    ? (_features.HasTableSyntaxDoesntWrapRecords() && exprType.IsRecord
                        ? DType.CreateTable(exprType.GetNames(DPath.Root))
                        : DType.CreateTable(new TypedName(exprType, TableValue.ValueDName)))
                    : DType.EmptyTable;

                _txb.SetType(node, tableType);
                SetVariadicNodePurity(node);
                _txb.SetScopeUseSet(node, JoinScopeUseSets(node.Children));
                _txb.SetSelfContainedConstant(node, isSelfContainedConstant);
            }
        }
    }
}
