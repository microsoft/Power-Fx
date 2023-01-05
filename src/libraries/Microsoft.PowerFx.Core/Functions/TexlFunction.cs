// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Functions.Publish;
using Microsoft.PowerFx.Core.Functions.TransportSchemas;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.IR.IRTranslator;
using CallNode = Microsoft.PowerFx.Syntax.CallNode;
using IRCallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;

namespace Microsoft.PowerFx.Core.Functions
{
    [ThreadSafeImmutable]
    internal abstract class TexlFunction : IFunction
    {
        // Column name when Features.ConsistentOneColumnTableResult is enabled.
        public const string ColumnName_ValueStr = "Value";

        // A default "no-op" error container that does not post document errors.
        public static IErrorContainer DefaultErrorContainer => new DefaultNoOpErrorContainer();

        // The information for scope if there is one.
        private FunctionScopeInfo _scopeInfo;

        // A description associated with this function.
        private readonly TexlStrings.StringGetter _description;

        // Convenience mask that indicates which parameters are to be treated as lambdas.
        // Bit at position K refers to argument of rank K. A bit of 1 denotes a lambda, 0 denotes non-lambda.
        // Overloads may choose to ignore this mask, and override the HasLambdas/IsLambdaParam APIs instead.
        protected readonly BigInteger _maskLambdas;

        // The parent namespace for this function. DPath.Root indicates the global namespace.
        public DPath Namespace { get; }

        // A DType.Unknown return type means that this function can return any type
        // and the specific return type will depend on the argument types.
        // If the function can return some shape of record, which depends on the argument types,
        // DType.EmptyRecord should be used. Similarly for tables and DType.EmptyTable.
        // CheckTypes can be used to infer the exact return type of a specific invocation.
        public DType ReturnType { get; }

        // Function arity (expected min/max number of arguments).
        public int MinArity { get; }

        public int MaxArity { get; }

        // Parameter types.
        public readonly DType[] ParamTypes;

        private SignatureConstraint _signatureConstraint;

        private TransportSchemas.FunctionInfo _cachedFunctionInfo;

        private string _cachedLocaleName;

        // Return true if the function should be hidden from the formular bar, false otherwise.
        public virtual bool IsHidden => false;

        // Return true if the function expects lambda arguments, false otherwise.
        public virtual bool HasLambdas => !_maskLambdas.IsZero;

        /// <summary>
        /// Returns true if the function expect identifiers, false otherwise.
        /// Needs to be overloaded for functions having identifier parameters.
        /// Also overload IsIdentifierParam method. 
        /// </summary>
        public virtual bool HasColumnIdentifiers => false;

        // Return true if lambda args should affect ECS, false otherwise.
        public virtual bool HasEcsExcemptLambdas => false;

        // Return true if the function is asynchronous, false otherwise.
        public virtual bool IsAsync => false;

        // Return true if the function is tracked in telemetry.
        public virtual bool IsTrackedInTelemetry => true;

        // Return true if the function is declared as variadic.
        public bool IsVariadicFunction => MaxArity == int.MaxValue;

        // Return true if the function's return value only depends on the global variable
        // e.g. Today(), Now() depend on the system time.
        public virtual bool IsGlobalReliant => false;

        // Return true if the function is self-contained (no side effects), or false otherwise.
        // This is a decision that developers will need to do for new functions, so making it
        // abstract will force them to do so.
        public abstract bool IsSelfContained { get; }

        // Return true if the function is stateless (same result for same input), or false otherwise.
        public virtual bool IsStateless => true;

        // Return true if the function supports inlining during codegen.
        public virtual bool SupportsInlining => false;

        // Returns false if we want to block the function within FunctionWithScope calls
        // that have a nondeterministic operation order (due to multiple async calls).
        public virtual bool AllowedWithinNondeterministicOperationOrder => true;

        // Returns true if the function creates an implicit dependency on the control's parent screen.
        public virtual bool CreatesImplicitScreenDependency => false;

        // Returns true if the function can be used in test cases; all "global" functions should
        // work, functionst that create screen dependencies don't by default (but can be overriden
        // if the function is ready for that)
        public virtual bool CanBeUsedInTests => !CreatesImplicitScreenDependency;

        /// <summary>
        /// Whether the function always produces a visible error if CheckTypes returns invalid.
        /// This can be used to prevent the overall "Function has invalid arguments" error.
        /// </summary>
        public virtual bool HasPreciseErrors => false;

        // Returns true if the function is disabled for component.
        public virtual bool DisableForComponent => false;

        // Returns true if the function is disabled for data component.
        public virtual bool DisableForDataComponent => false;

        // Returns true if the function is disabled for Commmanding
        public virtual bool DisableForCommanding => false;

        // Returns true if the function should be suppressed in Intellisense for component.
        public virtual bool SuppressIntellisenseForComponent => DisableForComponent;

        public virtual RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.None;

        // Return true if the function is pure (stateless with no side effects), or false otherwise.
        public bool IsPure => IsSelfContained && IsStateless;

        // Return true if the function is strict (in all of its parameters), or false otherwise.
        // A strict function is a function that always evaluates all of its arguments (the parameters
        // have to be computed before the function can run). A non-strict function is a function
        // that does not always evaluate all of its arguments. In terms of dependencies, a strict
        // function means that a dependence on the function result implies dependencies on all of its args,
        // whereas a non-strict function means that a dependence on the result implies dependencies
        // on only some of the args.
        public virtual bool IsStrict => true;

        // Return true if the function can only be used in behavior rules, i.e. rules that run in
        // response to user feedback. Only certain functions fall into this category, e.g. functions
        // with side effects, such as Collect.
        public virtual bool IsBehaviorOnly => !IsSelfContained;

        // Return true if the function can only be used as part of test cases. Functions that
        // emulate user interaction fall into this category, such as SetProperty.
        public virtual bool IsTestOnly => false;

        // Return true if the function manipulates collections.
        public virtual bool ManipulatesCollections => false;

        // Return true if the function uses an input's column names to inform Intellisense's suggestions.
        public virtual bool CanSuggestInputColumns => false;

        // Return true if the function expects a screen's context variables to be suggested within a record argument.
        public virtual bool CanSuggestContextVariables => false;

        // Returns true if it's valid to suggest ThisItem for this function as an argument.
        public virtual bool CanSuggestThisItem => false;

        // Return true if this function affects collection schemas.
        public virtual bool AffectsCollectionSchemas => false;

        // Return true if this function affects screen aliases ("context variables").
        public virtual bool AffectsAliases => false;

        // Return true if this function affects scope variable ("app scope variable or component scope variable").
        public virtual bool AffectsScopeVariable => false;

        // Return true if this function affects datasource query options.
        public virtual bool AffectsDataSourceQueryOptions => false;

        // Return true if this function can return a type with ExpandInfo.
        public virtual bool CanReturnExpandInfo => false;

        // Return true if this function requires binding context info.
        public virtual bool RequiresBindingContext => false;

        // Return true if this function can generate new data on its own without re-evaluating a rule.
        public virtual bool IsAutoRefreshable => false;

        // Return true if this function returns dynamic metadata
        public virtual bool IsDynamic => false;

        // Return the index to be used to provide type recommendations for later arguments
        public virtual int SuggestionTypeReferenceParamIndex => 0;

        // Return true if the function uses the parent scope to provide suggestions
        public virtual bool UseParentScopeForArgumentSuggestions => false;

        // Return true if the function uses the enum namespace for type suggestions
        public virtual bool UsesEnumNamespace => false;

        // Return true if the function supports parameter coercion.
        public virtual bool SupportsParamCoercion => true;

        /// <summary>Indicates whether table and record param types require all columns to be specified in the input argument.</summary>
        public virtual bool RequireAllParamColumns => false;

        /// <summary>
        /// Indicates whether the function sets a value.
        /// </summary>
        public virtual bool ModifiesValues => false;

        /// <summary>
        /// The function's name as surfaced in / accessible from the language.
        /// Using properties instead of fields here, to account for the fact that subclasses may override LocaleSpecificName.
        /// </summary>
        public string Name { get; }

        // The localized version of the namespace for this function.
        public DPath LocaleSpecificNamespace { get; }

        /// <summary>
        /// The function's locale-specific name.
        /// These should all be defined in the string resources, e.g. Abs_Name, Filter_Name, etc.
        /// The derived classes can pass in the value if needed and in that case, the passed in value is directly used.
        /// </summary>
        public string LocaleSpecificName { get; }

        // The function's English / locale-invariant name.
        public string LocaleInvariantName { get; }

        // A description associated with this function.
        public string Description => _description(null);

        // A forward link to the function help.
        public virtual string HelpLink =>

                // The invariant name is used to form a URL. It cannot contain spaces and other
                // funky characters. We have tests that enforce this constraint. If we ever need
                // such characters (#, &, %, ?), they need to be encoded here, e.g. %20, etc.
                StringResources.Get("FunctionReference_Link") + char.ToLower(LocaleInvariantName.First());

        /// <summary>
        /// Might need to reset if Function is variadic function.
        /// </summary>
        public SignatureConstraint SignatureConstraint
        {
            get
            {
                if (MaxArity == int.MaxValue && _signatureConstraint == null)
                {
                    _signatureConstraint = new SignatureConstraint(MinArity + 1, 1, 0, MinArity + 3);
                }

                return _signatureConstraint;
            }
            protected set => _signatureConstraint = value;
        }

        /// <summary>
        /// Gives information for scope if the function has scope. If this is null,
        /// the function does not involve row scope.
        /// </summary>
        public FunctionScopeInfo ScopeInfo
        {
            get => _scopeInfo;

            protected set
            {
                if (_scopeInfo != null)
                {
                    Contracts.Assert(false, "The ScopeInfo should only be set once in the constructor, if at all.");
                }

                _scopeInfo = value;
            }
        }

        // Mask indicating the function categories the function belongs to.
        public FunctionCategories FunctionCategoriesMask { get; }

        // Mask indicating the function delegation capabilities.
        public virtual DelegationCapability FunctionDelegationCapability => DelegationCapability.None;

        // Mask indicating the function capabilities.
        public virtual Capabilities Capabilities => Capabilities.None;

        // The function's fully qualified locale-specific name, including the namespace.
        // If the function is in the global namespace, this.QualifiedName is the same as this.Name.
        public string QualifiedName => Namespace.IsRoot ? Name : Namespace.ToDottedSyntax() + TexlLexer.PunctuatorDot + TexlLexer.EscapeName(Name);

        public TexlFunction(
            DPath theNamespace,
            string name,
            string localeSpecificName,
            TexlStrings.StringGetter description,
            FunctionCategories functionCategories,
            DType returnType,
            BigInteger maskLambdas,
            int arityMin,
            int arityMax,
            params DType[] paramTypes)
        {
            Contracts.Assert(theNamespace.IsValid);
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(localeSpecificName);
            Contracts.Assert((uint)functionCategories > 0);
            Contracts.Assert(returnType.IsValid);
            Contracts.AssertValue(paramTypes);
            Contracts.AssertAllValid(paramTypes);
            Contracts.Assert(maskLambdas.Sign >= 0 || arityMax == int.MaxValue);
            Contracts.Assert(arityMax >= 0 && paramTypes.Length <= arityMax);
            Contracts.AssertIndexInclusive(arityMin, arityMax);

            Namespace = theNamespace;
            LocaleInvariantName = name;
            FunctionCategoriesMask = functionCategories;
            _description = description;
            ReturnType = returnType;
            _maskLambdas = maskLambdas;
            MinArity = arityMin;
            MaxArity = arityMax;
            ParamTypes = paramTypes;

            // Locale Specific Name is a legacy piece of code only used by ServiceFunctions.
            // For all other instances, the name is the same as the En-Us name
            if (!string.IsNullOrEmpty(localeSpecificName))
            {
                LocaleSpecificNamespace = new DPath().Append(new DName(localeSpecificName));
                LocaleSpecificName = localeSpecificName;
            }
            else
            {
                LocaleSpecificName = LocaleInvariantName;
            }

            Name = LocaleSpecificName;
        }

        // Return all signatures for this function.
        // Functions with optional parameters have more than one signature.
        public abstract IEnumerable<TexlStrings.StringGetter[]> GetSignatures();

        // Return all enums that are required by this function.
        // This can be used to generate a list of enums required for a function library.
        public virtual IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>();
        }

        // Return all signatures with at most 'arity' parameters.
        public virtual IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            Contracts.Assert(arity >= 0);

            foreach (var signature in GetSignatures())
            {
                if (arity <= signature.Length)
                {
                    yield return signature;
                }
            }
        }

        // Return a unique name for this function (useful in the presence of overloads).
        // This is used for name mangling and Texl -> runtime function mapping.
        // TASK: 68797: We need a TexlFunction name -> JS/runtime function name map.
        public virtual string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(string.Empty);
        }

        protected string GetUniqueTexlRuntimeName(string suffix, bool suppressAsync = false)
        {
            var name = Namespace.IsRoot ? LocaleInvariantName : Namespace.Name + "__" + LocaleInvariantName;
            if (name.Length <= 1)
            {
                return name.ToLowerInvariant();
            }

            return char.ToLowerInvariant(name[0]).ToString() + name.Substring(1) + suffix + (IsAsync && !suppressAsync ? "Async" : string.Empty);
        }

        #region CheckInvocation Replacement Project
        public bool HandleCheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var result = CheckTypes(binding.CheckTypesContext, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            CheckSemantics(binding, args, argTypes, errors, ref nodeToCoercedTypeMap);
            return result;
        }

        /// <summary>
        /// Perform sub-expression type checking and produce a return type.
        /// </summary>
        public virtual bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            return CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
        }

        public virtual bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            return CheckTypesCore(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
        }

        /// <summary>
        /// Perform expression-level semantics checks which require a binding. May produce coercions.
        /// </summary>
        public virtual void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            CheckSemantics(binding, args, argTypes, errors);
        }

        public virtual void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
        }
        #endregion

        public virtual bool CheckForDynamicReturnType(TexlBinding binding, TexlNode[] args)
        {
            return false;
        }

        protected static uint ComputeArgHash(TexlNode[] args)
        {
            var argHash = string.Empty;

            for (var i = 0; i < args.Length; i++)
            {
                argHash += args[i].ToString();
            }

            return Hashing.HashString(argHash);
        }

        public virtual bool SupportCoercionForArg(int argIndex)
        {
            return SupportsParamCoercion && (argIndex <= MinArity || argIndex <= MaxArity);
        }

        private bool CheckTypesCore(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            for (var i = 0; i < argTypes.Length; i++)
            {
                if (!IsIdentifierParam(i))
                {
                    Contracts.AssertValid(argTypes[i]);
                }
            }

            var fValid = true;
            var count = Math.Min(args.Length, ParamTypes.Length);

            nodeToCoercedTypeMap = null;

            // Type check the args
            for (var i = 0; i < count; i++)
            {
                var typeChecks = CheckType(args[i], argTypes[i], ParamTypes[i], errors, SupportCoercionForArg(i), out DType coercionType);
                if (typeChecks && coercionType != null)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], coercionType);
                }

                fValid &= typeChecks;
            }

            for (var i = count; i < args.Length; i++)
            {
                // Identifiers don't have a type
                if (IsIdentifierParam(i))
                {
                    continue;
                }

                var type = argTypes[i];
                if (type.IsError)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrBadType);
                    fValid = false;
                }
            }

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            ScopeInfo?.CheckLiteralPredicates(args, errors);

            // Default return type.
            returnType = ReturnType;

            return fValid;
        }

        /// <summary>
        /// True if there was any custom post-visit validation errors applied for this function.
        /// </summary>
        public virtual bool PostVisitValidation(TexlBinding binding, CallNode callNode)
        {
            return false;
        }

        // Return true if the parameter at the specified 0-based rank is a lambda parameter, false otherwise.
        public virtual bool IsLambdaParam(int index)
        {
            Contracts.AssertIndexInclusive(index, MaxArity);

            return _maskLambdas.TestBit(index);
        }

        /// <summary>
        /// True if the evaluation of the param at the 0-based index is controlled by the function in question
        /// e.g. conditionally evaluated, repeatedly evaluated, etc.., false otherwise.
        /// All lambda params are Lazy, but others may also be, including short-circuit booleans, conditionals, etc..
        /// </summary>
        /// <param name="index">Parameter index, 0-based.</param>
        public virtual bool IsLazyEvalParam(int index)
        {
            Contracts.AssertIndexInclusive(index, MaxArity);

            return IsLambdaParam(index);
        }

        public virtual bool IsEcsExcemptedLambda(int index)
        {
            Contracts.Assert(index >= 0);

            return false;
        }

        /// <summary>
        /// Returns true if the parameter is an identifier.
        /// </summary>
        /// <param name="index">Parameter's index.</param>
        /// <returns>Boolean representing if the parameter is an identifier.</returns>
        public virtual bool IsIdentifierParam(int index)
        {
            Contracts.Assert(index >= 0);

            return false;
        }

        public virtual bool AllowsRowScopedParamDelegationExempted(int index)
        {
            return false;
        }

        // Return true if this function requires global binding context info.
        public virtual bool RequiresGlobalBindingContext(TexlNode[] args, TexlBinding binding)
        {
            return false;
        }

        // Returns true if function requires actual data to be pulled for this arg. This is applicable to pagable args only like datasource object.
        // It's used in codegen in optimizing generated code where there is no data is required to be pulled from server.
        protected virtual bool RequiresPagedDataForParamCore(TexlNode[] args, int paramIndex, TexlBinding binding)
        {
            return true;
        }

        public bool RequiresPagedDataForParam(CallNode callNode, int paramIndex, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(callNode.Args);
            Contracts.Assert(paramIndex >= 0 && paramIndex < callNode.Args.Children.Count());
            Contracts.AssertValue(binding);

            var child = callNode.Args.Children[paramIndex].VerifyValue();
            if (!binding.IsPageable(child))
            {
                return false;
            }

            // If the parent call node is pagable then we don't need to pull the data.
            if (binding.IsPageable(callNode))
            {
                return false;
            }

            // Check with function if we actually need data for this param.
            return RequiresPagedDataForParamCore(callNode.Args.Children, paramIndex, binding);
        }

        /// <summary>
        /// Provides dataentitymetadata for a callnode.
        /// </summary>
        /// <returns></returns>
        public static bool TryGetEntityMetadata(CallNode callNode, TexlBinding binding, out IDataEntityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            var args = callNode.Args.Children.VerifyValue();
            if (!binding.IsPageable(args[0]) || !binding.TryGetEntityInfo(args[0], out var entityInfo))
            {
                metadata = null;
                return false;
            }

            Contracts.AssertValue(entityInfo.ParentDataSource);
            Contracts.AssertValue(entityInfo.ParentDataSource.DataEntityMetadataProvider);

            var metadataProvider = entityInfo.ParentDataSource.DataEntityMetadataProvider;

            if (!metadataProvider.TryGetEntityMetadata(entityInfo.Identity, out var entityMetadata))
            {
                metadata = null;
                return false;
            }

            metadata = entityMetadata.VerifyValue();
            return true;
        }

        /// <summary>
        /// Provides delegationmetadata for a callnode. It's used by delegable functions to get delegation metadata. For example, Filter, Sort, SortByColumns.
        /// </summary>
        /// <returns></returns>
        public static bool TryGetEntityMetadata(CallNode callNode, TexlBinding binding, out IDelegationMetadata metadata)
        {
            if (!TryGetEntityMetadata(callNode, binding, out IDataEntityMetadata entityMetadata))
            {
                metadata = null;
                return false;
            }

            metadata = entityMetadata.DelegationMetadata.VerifyValue();
            return true;
        }

        // Fetch the description associated with the specified parameter name (which must be the INVARIANT name)
        // If the param has no description, this will return false.
        public virtual bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            // Fetch it from the string resources by default. Subclasses can override this
            // and use their own dictionaries, etc.
            return StringResources.TryGet("About" + LocaleInvariantName + "_" + paramName, out paramDescription);
        }

        // Exhaustive list of parameter names, in no guaranteed order.
        // (Used by Tests only)
        public IEnumerable<string> GetParamNames()
        {
            return GetSignatures().SelectMany(args => args.Select(arg => arg(null))).Distinct();
        }

        // Allows a function to determine if a given type is valid for a given parameter index.
        public virtual bool IsSuggestionTypeValid(int paramIndex, DType type)
        {
            Contracts.Assert(paramIndex >= 0);
            Contracts.AssertValid(type);

            return paramIndex < MaxArity;
        }

        // Functions can use custom logic to determine if an invocation is inherently async, and therefore requires async codegen.
        public virtual bool IsAsyncInvocation(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return IsAsync || IsServerDelegatable(callNode, binding);
        }

        // Functions which support server delegation need to override this method to verify server delegation can be supported for this CallNode.
        public virtual bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return false;
        }

        public virtual bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return binding.IsDelegatable(callNode) || IsServerDelegatable(callNode, binding);
        }

        // Returns true if function is row scoped and supports delegation.
        // Needs to be overriden by functions (For example, IsBlank) which are not server delegatable themselves but can become one when scoped inside a delegatable function.
        public virtual bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!binding.IsRowScope(callNode))
            {
                return false;
            }

            return IsServerDelegatable(callNode, binding);
        }

        public virtual bool TryGetDataSource(CallNode callNode, TexlBinding binding, out IExternalDataSource dsInfo)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            dsInfo = null;
            if (callNode.Args.Count < 1)
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var arg0 = args[0].VerifyValue();
            return ArgValidators.DelegatableDataSourceInfoValidator.TryGetValidValue(arg0, binding, out dsInfo);
        }

        // Returns a datasource node for a function if function operates on datasource.
        public virtual bool TryGetDataSourceNodes(CallNode callNode, TexlBinding binding, out IList<FirstNameNode> dsNodes)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            dsNodes = new List<FirstNameNode>();
            if (callNode.Args.Count < 1)
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var arg0 = args[0].VerifyValue();
            return ArgValidators.DataSourceArgNodeValidator.TryGetValidValue(arg0, binding, out dsNodes);
        }

        // Returns a entityInfo for a function if function operates on entity.
        public bool TryGetEntityInfo(CallNode callNode, TexlBinding binding, out IExpandInfo entityInfo)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            entityInfo = null;
            if (callNode.Args.Count < 1)
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var arg0 = args[0].VerifyValue();
            return ArgValidators.EntityArgNodeValidator.TryGetValidValue(arg0, binding, out entityInfo);
        }

        public virtual IEnumerable<Identifier> GetIdentifierOfModifiedValue(TexlNode[] args, out TexlNode identifierNode)
        {
            identifierNode = null;

            return null;
        }

        // Override if Function.AffectsScopeVariable is true. Returns the index of the arg that contains the app/component variable names.
        public virtual int ScopeVariableNameAffectingArg()
        {
            return -1;
        }

        public virtual bool RequiresDataSourceScope => false;

        public virtual bool ArgMatchesDatasourceType(int argNum)
        {
            return false;
        }

        /// <summary>
        /// Gets TexlNodes of function argument that need to be processed for tabular datasource
        /// E.g. Filter function will have first argument node that will be associated with tabular datasource,
        /// however With function will have Record type argument that can hold multiple datasource type columns
        /// Functions that have datasource arguments in places ither than first argument need to override this.
        /// </summary>
        /// <param name="callNode">Function Texl Node.</param>
        public virtual IEnumerable<TexlNode> GetTabularDataSourceArg(CallNode callNode)
        {
            Contracts.AssertValue(callNode);

            return new[] { callNode.Args.Children[0] };
        }

        /// <summary>
        /// If true, the scope this function creates isn't used for field names of inline records.
        /// </summary>
        public virtual bool SkipScopeForInlineRecords => false;

        protected static bool Arg0RequiresAsync(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!TryGetArg0AsDsInfo(callNode, binding, out var dataSource))
            {
                return false;
            }

            return dataSource.RequiresAsync;
        }

        private static bool TryGetArg0AsDsInfo(CallNode callNode, TexlBinding binding, out IExternalDataSource dsInfo)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            dsInfo = null;
            if (callNode.Args.Count < 1)
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var arg0 = args[0].VerifyValue();

            var firstName = arg0.AsFirstName();
            if (firstName == null || !binding.GetType(firstName).IsTable)
            {
                return false;
            }

            var firstNameInfo = binding.GetInfo(firstName);
            if (firstNameInfo == null || firstNameInfo.Kind != BindKind.Data)
            {
                return false;
            }

            var result = binding.EntityScope != null &&
               binding.EntityScope.TryGetEntity(firstNameInfo.Name, out dsInfo);
            return result;
        }

        protected bool SetErrorForMismatchedColumns(DType expectedType, DType actualType, TexlNode errorArg, IErrorContainer errors)
        {
            Contracts.AssertValid(expectedType);
            Contracts.AssertValid(actualType);
            Contracts.AssertValue(errorArg);
            Contracts.AssertValue(errors);

            return SetErrorForMismatchedColumnsCore(expectedType, actualType, errorArg, errors, DPath.Root);
        }

        // This function recursively traverses the types to find the first occurence of a type mismatch.
        // DTypes are guaranteed to be finite, so there is no risk of a call stack overflow
        private bool SetErrorForMismatchedColumnsCore(DType expectedType, DType actualType, TexlNode errorArg, IErrorContainer errors, DPath columnPrefix)
        {
            Contracts.AssertValid(expectedType);
            Contracts.AssertValid(actualType);
            Contracts.AssertValue(errorArg);
            Contracts.AssertValue(errors);
            Contracts.AssertValid(columnPrefix);

            // Iterate through the expectedType until an error is found.
            foreach (var expectedColumn in expectedType.GetAllNames(DPath.Root))
            {
                // First, set type mismatch message.
                if (actualType.TryGetType(expectedColumn.Name, out var actualColumnType))
                {
                    var expectedColumnType = expectedColumn.Type;
                    if (expectedColumnType.Accepts(actualColumnType))
                    {
                        continue;
                    }

                    if (!DType.TryGetDisplayNameForColumn(expectedType, expectedColumn.Name, out var errName))
                    {
                        errName = expectedColumn.Name;
                    }

                    if ((expectedColumn.Type.IsTable && actualColumnType.IsTable) || (expectedColumn.Type.IsRecord && actualColumnType.IsRecord))
                    {
                        return SetErrorForMismatchedColumnsCore(
                            expectedColumn.Type,
                            actualColumnType,
                            errorArg,
                            errors,
                            columnPrefix.Append(new DName(errName)));
                    }

                    if (expectedColumn.Type.IsExpandEntity
                        && DType.IsMatchingExpandType(expectedColumn.Type, actualColumnType))
                    {
                        continue;
                    }

                    errors.EnsureError(
                        DocumentErrorSeverity.Severe,
                        errorArg,
                        TexlStrings.ErrColumnTypeMismatch_ColName_ExpectedType_ActualType,
                        columnPrefix.Append(new DName(errName)).ToDottedSyntax(),
                        expectedColumn.Type.GetKindString(),
                        actualColumnType.GetKindString());
                    return true;
                }

                // Second, set column missing message if applicable
                if (RequireAllParamColumns && !expectedType.AreFieldsOptional)
                {
                    errors.EnsureError(
                        DocumentErrorSeverity.Severe,
                        errorArg,
                        TexlStrings.ErrColumnMissing_ColName_ExpectedType,
                        columnPrefix.Append(expectedColumn.Name).ToDottedSyntax(),
                        expectedColumn.Type.GetKindString());
                    return true;
                }
            }

            return false;
        }

        #region Internal functionality

        public virtual bool SupportsMetadataTypeArg => false;

        public virtual bool IsMetadataTypeArg(int index)
        {
            Contracts.Assert(!SupportsMetadataTypeArg);

            return SupportsMetadataTypeArg;
        }

        // Return true if the function has special suggestions for the corresponding parameter.
        public virtual bool HasSuggestionsForParam(int index)
        {
            return false;
        }

        protected bool CheckType(TexlNode node, DType nodeType, DType expectedType, IErrorContainer errors, out bool matchedWithCoercion)
        {
            return CheckType(node, nodeType, expectedType, errors, SupportsParamCoercion, out matchedWithCoercion);
        }

        protected bool CheckType(TexlNode node, DType nodeType, DType expectedType, IErrorContainer errors, bool coerceIfSupported, out bool matchedWithCoercion)
        {
            var typeChecks = CheckType(node, nodeType, expectedType, errors, coerceIfSupported, out DType coercionType);
            matchedWithCoercion = typeChecks && coercionType != null;
            return typeChecks;
        }

        // Check the type of a specified node against an expected type and possibly emit errors
        // accordingly. Returns true if the types align, false otherwise.
        protected bool CheckType(TexlNode node, DType nodeType, DType expectedType, IErrorContainer errors, bool coerceIfSupported, out DType coercionType)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(nodeType.IsValid);
            Contracts.Assert(expectedType.IsValid);
            Contracts.AssertValue(errors);

            coercionType = null;
            if (expectedType.Accepts(nodeType, out var schemaDifference, out var schemaDifferenceType))
            {
                return true;
            }

            KeyValuePair<string, DType> coercionDifference = default;
            DType coercionDifferenceType = null;
            if (coerceIfSupported && nodeType.CoercesTo(expectedType, out _, out coercionType, out coercionDifference, out coercionDifferenceType))
            {
                return true;
            }

            // If we could coerce some but not all of it we don't want errors for the coercible fields
            var targetType = coercionType ?? nodeType;
            var targetDifference = coercionType == null ? schemaDifference : coercionDifference;
            var targetDifferenceType = coercionType == null ? schemaDifferenceType : coercionDifferenceType;

            if ((targetType.IsTable && nodeType.IsTable) || (targetType.IsRecord && nodeType.IsRecord))
            {
                if (SetErrorForMismatchedColumns(expectedType, targetType, node, errors))
                {
                    return false;
                }
            }

            if (nodeType.Kind == expectedType.Kind)
            {
                // If coercion type is non null and coercion difference is, then the node should have been coercible.
                // This likely indicates a bug in CoercesTo, called above
                errors.Errors(node, targetType, targetDifference, targetDifferenceType);
            }
            else
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrBadType_ExpectedType_ProvidedType, expectedType.GetKindString(), nodeType.GetKindString());
            }

            return false;
        }

        private bool CheckColumnType(DType type, TexlNode arg, DType expectedType, IErrorContainer errors, ErrorResourceKey errKey, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.Assert(type.IsValid);
            Contracts.AssertValue(arg);
            Contracts.Assert(expectedType.IsValid);
            Contracts.AssertValue(errors);

            IEnumerable<TypedName> columns;
            if (!type.IsTable || (columns = type.GetNames(DPath.Root)).Count() != 1)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrInvalidSchemaNeedCol);
                return false;
            }
            else
            {
                var column = columns.Single();
                if (!expectedType.Accepts(column.Type))
                {
                    if (SupportsParamCoercion && column.Type.CoercesTo(expectedType))
                    {
                        expectedType = DType.CreateTable(new TypedName(expectedType, column.Name));
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, arg, expectedType);
                    }
                    else
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, arg, errKey, column.Name.Value);
                        return false;
                    }
                }
            }

            return true;
        }

        // Check that the type of a specified node is a numeric column type, and possibly emit errors
        // accordingly. Returns true if the types align, false otherwise. matchedWithCoercion is set
        // to true if the types align only with coercion.
        public bool CheckNumericColumnType(DType type, TexlNode arg, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            return CheckColumnType(type, arg, DType.Number, errors, TexlStrings.ErrInvalidSchemaNeedNumCol_Col, ref nodeToCoercedTypeMap);
        }

        // Check that the type of a specified node is a color column type, and possibly emit errors
        // accordingly. Returns true if the types align, false otherwise.
        protected bool CheckColorColumnType(DType type, TexlNode arg, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            return CheckColumnType(type, arg, DType.Color, errors, TexlStrings.ErrInvalidSchemaNeedColorCol_Col, ref nodeToCoercedTypeMap);
        }

        // Check that the type of a specified node is a string column type, and possibly emit errors
        // accordingly. Returns true if the types align, false otherwise.
        protected bool CheckStringColumnType(DType type, TexlNode arg, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            return CheckColumnType(type, arg, DType.String, errors, TexlStrings.ErrInvalidSchemaNeedStringCol_Col, ref nodeToCoercedTypeMap);
        }

        // Check that the type of a specified node is a date column type, and possibly emit errors
        // accordingly. Returns true if the types align, false otherwise.
        protected bool CheckDateColumnType(DType type, TexlNode arg, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            return CheckColumnType(type, arg, DType.DateTime, errors, TexlStrings.ErrInvalidSchemaNeedDateCol_Col, ref nodeToCoercedTypeMap);
        }

        // Check that the type of a specified node is a boolean column type, and possibly emit errors
        // accordingly. Returns true if the types align, false otherwise.
        protected bool CheckBooleanColumnType(DType type, TexlNode arg, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            return CheckColumnType(type, arg, DType.Boolean, errors, TexlStrings.ErrInvalidSchemaNeedColorCol_Col, ref nodeToCoercedTypeMap);
        }

        // Enumerate some of the function signatures for a specified arity and known parameter descriptions.
        // The last parameter may be repeated as many times as necessary in order to satisfy the arity constraint.
        protected IEnumerable<TexlStrings.StringGetter[]> GetGenericSignatures(int arity, params TexlStrings.StringGetter[] args)
        {
            Contracts.Assert(MinArity <= arity && arity <= MaxArity);
            Contracts.AssertValue(args);
            Contracts.Assert(args.Length > 0);

            var signatureCount = 5;
            var argCount = arity;

            // Limit the signature length of params descriptions.
            if (SignatureConstraint != null && (arity + signatureCount) > SignatureConstraint.RepeatTopLength)
            {
                signatureCount = (SignatureConstraint.RepeatTopLength - arity) > 0 ? SignatureConstraint.RepeatTopLength - arity : 1;
                argCount = arity < SignatureConstraint.RepeatTopLength ? arity : SignatureConstraint.RepeatTopLength;
            }

            var signatures = new List<TexlStrings.StringGetter[]>(signatureCount);
            var lastArg = args.Last();

            for (var sigIndex = 0; sigIndex < signatureCount; sigIndex++)
            {
                var signature = new TexlStrings.StringGetter[argCount];

                // Populate from the given args (as much as possible). The last arg will be repeated.
                for (var i = 0; i < argCount; i++)
                {
                    signature[i] = i < args.Length ? args[i] : lastArg;
                }

                signatures.Add(signature);
                argCount++;
            }

            return new ReadOnlyCollection<TexlStrings.StringGetter[]>(signatures);
        }

        protected void AddSuggestionMessageToTelemetry(string telemetryMessage, TexlNode node, TexlBinding binding)
        {
            Contracts.AssertNonEmpty(telemetryMessage);
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            var message = string.Format("Function:{0}, Message:{1}", Name, telemetryMessage);
            TrackingProvider.Instance.AddSuggestionMessage(message, node, binding);
        }

        protected void SuggestDelegationHint(TexlNode node, TexlBinding binding, string telemetryMessage)
        {
            SuggestDelegationHint(node, binding);
            AddSuggestionMessageToTelemetry(telemetryMessage, node, binding);
        }

        // Helper used to provide hints when we detect non-delegable parts of the expression due to server restrictions.
        protected void SuggestDelegationHint(TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, node, TexlStrings.SuggestRemoteExecutionHint, Name);
        }

        protected bool CheckArgsCount(CallNode callNode, TexlBinding binding, DocumentErrorSeverity errorSeverity = DocumentErrorSeverity.Suggestion)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (binding.ErrorContainer.HasErrors(callNode, errorSeverity))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var cargs = args.Count();
            return !(cargs < MinArity || cargs > MaxArity);
        }

        // Helper used to validate call node and get delegatable datasource value.
        protected bool TryGetValidDataSourceForDelegation(CallNode callNode, TexlBinding binding, DelegationCapability expectedCapability, out IExternalDataSource dataSource)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            dataSource = null;

            // Only check for errors with severity more than warning.
            // Ignore warning errors as it's quite possible to have delegation warnings on a node in different call node context.
            // For example, Filter(CDS, A = B) It's possible that B as itself is delegatable but in the context of Filter it's not and could have warning on it.
            if (binding.ErrorContainer.HasErrors(callNode, DocumentErrorSeverity.Moderate))
            {
                return false;
            }

            if (!TryGetDataSource(callNode, binding, out dataSource))
            {
                return false;
            }

            if (dataSource == null)
            {
                return false;
            }

            // Check if DS is server delegatable.
            return dataSource.IsDelegatable &&
                    dataSource.DelegationMetadata.VerifyValue().TableCapabilities.HasCapability(expectedCapability.Capabilities);
        }

        // Helper to drop all of a single types from a result type
        protected bool DropAllOfKindNested(ref DType itemType, IErrorContainer errors, TexlNode node, DKind kind)
        {
            return DropAllMatchingNested(ref itemType, errors, node, type => type.Kind == kind);
        }

        protected bool DropAllMatchingNested(ref DType itemType, IErrorContainer errors, TexlNode node, Func<DType, bool> matchFunc)
        {
            Contracts.AssertValid(itemType);
            Contracts.AssertValue(errors);
            Contracts.AssertValue(node);

            var fError = false;
            itemType = itemType.DropAllMatchingNested(ref fError, DPath.Root, matchFunc);
            if (fError)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrIncompatibleTypes);

                // As DropAllOfKind doesn't set returned type to erroneous in case of failure, explicitly set it here.
                itemType = DType.Error;
                return false;
            }

            return true;
        }

        public virtual bool TryGetDelegationMetadata(CallNode node, TexlBinding binding, out IDelegationMetadata metadata)
        {
            metadata = null;
            return false;
        }

        public virtual IOpDelegationStrategy GetOpDelegationStrategy(BinaryOp op, PowerFx.Syntax.BinaryOpNode opNode)
        {
            Contracts.AssertValueOrNull(opNode);

            if (op == BinaryOp.In)
            {
                Contracts.AssertValue(opNode);
                Contracts.Assert(opNode.Op == op);

                return new InOpDelegationStrategy(opNode, this);
            }

            return new DefaultBinaryOpDelegationStrategy(op, this);
        }

        // This updates the field projection info for datasources. For most of the functions, binder takes care of it.
        // But if functions have specific semantics then this allows functions to contribute this information.
        // For example, Search function which references columns as string literals.
        public virtual bool UpdateDataQuerySelects(CallNode node, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            return false;
        }

        public IOpDelegationStrategy GetOpDelegationStrategy(UnaryOp op)
        {
            return new DefaultUnaryOpDelegationStrategy(op, this);
        }

        public ICallNodeDelegatableNodeValidationStrategy GetCallNodeDelegationStrategy()
        {
            return new DelegationValidationStrategy(this);
        }

        public IDottedNameNodeDelegatableNodeValidationStrategy GetDottedNameNodeDelegationStrategy()
        {
            return new DelegationValidationStrategy(this);
        }

        public IFirstNameNodeDelegatableNodeValidationStrategy GetFirstNameNodeDelegationStrategy()
        {
            return new DelegationValidationStrategy(this);
        }

        // Check the type of a specified node against an expected type (either desiredType or DType.Table with a desiredType column)
        // and possibly emit errors accordingly. Returns true if the types align, false otherwise.
        protected bool CheckParamIsTypeOrSingleColumnTable(DType desiredType, TexlNode node, DType nodeType, IErrorContainer errors, out bool isTable, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.Assert(desiredType.IsValid);
            Contracts.AssertValue(node);
            Contracts.Assert(nodeType.IsValid);
            Contracts.AssertValue(errors);

            isTable = false;

            if (desiredType.Accepts(nodeType))
            {
                return true;
            }

            if (SupportsParamCoercion && nodeType.CoercesTo(desiredType))
            {
                CollectionUtils.Add(ref nodeToCoercedTypeMap, node, desiredType);
                return true;
            }

            if (nodeType.IsTable)
            {
                var count = 0;
                isTable = true;
                foreach (var col in nodeType.GetNames(DPath.Root))
                {
                    count++;
                    if (!desiredType.Accepts(col.Type))
                    {
                        if (SupportsParamCoercion && col.Type.CoercesTo(desiredType))
                        {
                            desiredType = DType.CreateTable(new TypedName(desiredType, col.Name));
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, node, desiredType);
                        }
                        else
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrFunctionDoesNotAcceptThisType_Function_Expected, Name, desiredType.GetKindString());
                            return false;
                        }
                    }
                }

                if (count == 1)
                {
                    return true;
                }
            }

            errors.EnsureError(node, TexlStrings.ErrFunctionDoesNotAcceptThisType_Function_Expected, Name, desiredType.GetKindString());
            return false;
        }

        protected bool CheckAllParamsAreTypeOrSingleColumnTable(CheckTypesContext context, DType desiredType, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var fValid = true;
            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();

            returnType = DType.Invalid;

            // Type check the args
            for (var i = 0; i < args.Length; i++)
            {
                fValid &= CheckParamIsTypeOrSingleColumnTable(desiredType, args[i], argTypes[i], errors, out var isTable, ref nodeToCoercedTypeMap);

                // If there are any table args, the return type depends on the first such arg.
                if (isTable && returnType == DType.Invalid)
                {
                    if (fValid && nodeToCoercedTypeMap.Any())
                    {
                        var resultColumnName = context.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                            ? new DName(ColumnName_ValueStr)
                            : argTypes[i].GetNames(DPath.Root).Single().Name;

                        returnType = DType.CreateTable(new TypedName(desiredType, resultColumnName));
                    }
                    else
                    {
                        returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                            ? DType.CreateTable(new TypedName(desiredType, new DName(ColumnName_ValueStr)))
                            : argTypes[i];
                    }
                }
            }

            // If the returnType hasn't been set, we are working with only scalars.
            if (returnType == DType.Invalid)
            {
                returnType = desiredType;
            }

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }

        #endregion

        internal TransportSchemas.FunctionInfo Info(string locale)
        {
            // If the locale has changed, we want to reset the function info to one of the new locale
            if (CurrentLocaleInfo.CurrentUILanguageName == _cachedLocaleName && _cachedFunctionInfo != null)
            {
                return _cachedFunctionInfo;
            }

            _cachedLocaleName = CurrentLocaleInfo.CurrentUILanguageName;
            return _cachedFunctionInfo = new TransportSchemas.FunctionInfo()
            {
                Label = Name,
                Detail = Description,
                Signatures = GetSignatures().Select(signature => new FunctionSignature()
                {
                    Label = Name + (signature == null ?
                        "()" :
                        ("(" + string.Join(TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator + " ", signature.Select(getter => getter(null))) + ")")),
                    Parameters = signature?.Select(getter =>
                    {
                        TryGetParamDescription(getter(locale), out var description);

                        return new ParameterInfo()
                        {
                            Label = getter(null),
                            Documentation = description
                        };
                    }).ToArray()
                }).ToArray()
            };
        }

        /// <summary>
        /// Override this method to rewrite the CallNode that is generated.
        /// e.g. Boolean(true) would want to emit the arg true directly instead of a function call.
        /// </summary>
        internal virtual IntermediateNode CreateIRCallNode(CallNode node, IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            if (scope != null)
            {
                return new IRCallNode(context.GetIRContext(node), this, scope, args);
            }

            return new IRCallNode(context.GetIRContext(node), this, args);
        }
    }

    // A function capable of async invokes. 
    internal interface IAsyncTexlFunction
    {
        Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken);
    }
}
