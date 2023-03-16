// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Holds work such as parsing, binding, error checking done on a single expression. 
    /// Different options require different work. 
    /// Tracks which work is done so that it is not double repeated.
    /// </summary>
    public class CheckResult : IOperationStatus
    {
        #region User Inputs
        // Captured from user, set once via a Set*() function. 

        /// <summary>
        /// The source engine this was created against. 
        /// This is critical for calling back to populate the rest of the results. 
        /// </summary>
        private readonly Engine _engine;

        // The raw expression test. 
        private string _expression;

        private ParserOptions _parserOptions;

        private CultureInfo _defaultErrorCulture;

        // We must call all Set() operations before calling Apply(). 
        // This is because Apply() methods can be called in lazy ways, and so we need a gaurantee
        // that the Set() conditions are fixed. 
        private bool _beginApply;

        // Information for binding. 
        private bool _setBindingCalled;
        internal ReadOnlySymbolTable _symbols; // can be null
        private VersionHash _symbolHash; // hash of _symbols at time of assignment

        #endregion

        [Obsolete("Use public constructor")]
        internal CheckResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckResult"/> class.
        /// </summary>
        /// <param name="source">Engine used to handle Apply operations.</param>
        public CheckResult(Engine source)
        {
            this._engine = source ?? throw new ArgumentNullException(nameof(source));            
        }

        internal Engine Engine => _engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckResult"/> class.
        /// Create a "failed" check around a set of errors. 
        /// Can't do any other operations. 
        /// </summary>
        /// <param name="extraErrors"></param>
        public CheckResult(IEnumerable<ExpressionError> extraErrors)
        {
            this._errors.AddRange(extraErrors);
        }

        #region Set info from User. 
        private void VerifyEngine([CallerMemberName] string memberName = "")
        {
            if (_engine == null)
            {
                throw new InvalidOperationException($"Can't call {memberName} without an engine.");
            }

            // Can't call Set*() methods after the init phase.
            if (_beginApply)
            {
                throw new InvalidOperationException($"Can't call {memberName} after calling an Apply*() method.");
            }
        }

        public CheckResult SetText(ParseResult parse)
        {
            VerifyEngine();

            if (parse == null)
            {
                throw new ArgumentNullException(nameof(parse));
            }

            if (_expression != null)
            {
                throw new InvalidOperationException($"Can only call {nameof(SetText)} once.");
            }

            _expression = parse.Text;
            _parserOptions = parse.Options;
            this.Parse = parse;

            return this;
        }

        public CheckResult SetText(string expression, ParserOptions parserOptions = null)
        {
            VerifyEngine();

            expression = expression ?? throw new ArgumentNullException(nameof(expression));

            if (_expression != null)
            {
                throw new InvalidOperationException($"Can only call {nameof(SetText)} once.");
            }

            _expression = expression;
            _parserOptions = parserOptions ?? Engine.GetDefaultParserOptionsCopy();
            this.ParserCultureInfo = _parserOptions.Culture;

            return this;
        }

        // Set the default culture for localizing error messages. 
        public CheckResult SetDefaultErrorCulture(CultureInfo culture)
        {
            VerifyEngine();

            this._defaultErrorCulture = culture;
            return this;
        }

        // Symbols could be null if no additional symbols are provided. 
        public CheckResult SetBindingInfo(ReadOnlySymbolTable symbols)
        {
            VerifyEngine();

            if (_setBindingCalled)
            {
                throw new InvalidOperationException($"Can only call {nameof(SetBindingInfo)} once.");
            }

            _symbols = symbols;

            if (_symbols != null)
            {
                _symbolHash = _symbols.VersionHash;
            }

            _setBindingCalled = true;
            return this;
        }

        public CheckResult SetBindingInfo(RecordType parameterType)
        {
            ReadOnlySymbolTable symbolTable = null;
            if (parameterType != null)
            {
                symbolTable = SymbolTable.NewFromRecord(parameterType);
            }

            return this.SetBindingInfo(symbolTable);
        }

        private FormulaType _expectedReturnType;
        private bool _allowCoerceToType = false;

        public CheckResult SetExpectedReturnValue(FormulaType type, bool allowCoerceTo = false)
        {
            VerifyEngine();

            _expectedReturnType = type;
            _allowCoerceToType = allowCoerceTo;
            return this;
        }

        // No additional binding is required
        public CheckResult SetBindingInfo()
        {
            return SetBindingInfo((RecordType)null);
        }
        #endregion

        #region Results from Parsing 

        /// <summary>
        /// Results from parsing. <see cref="ApplyParse"/>.
        /// </summary>
        public ParseResult Parse { get; private set; }
        #endregion

        #region Results from Binding

        /// <summary>
        /// Binding, computed from <see cref="ApplyBindingInternal"/>.
        /// </summary>        
        internal TexlBinding _binding;

        /// <summary> 
        /// Return type of the expression. Null if type can't be determined. 
        /// </summary>
        public FormulaType ReturnType { get; set; }

        #endregion

        #region Results from Dependencies

        private HashSet<string> _topLevelIdentifiers;

        /// <summary>
        /// Names of fields that this formula uses. 
        /// null if unavailable.  
        /// This is only valid when <see cref="IsSuccess"/> is true.
        /// </summary>
        public HashSet<string> TopLevelIdentifiers
        {
            get
            {
                if (_topLevelIdentifiers == null)
                {
                    throw new InvalidOperationException($"Call {nameof(ApplyDependencyAnalysis)} first.");
                }

                return _topLevelIdentifiers;
            }
        }

        #endregion 

        #region Results from Errors

        // All errors accumulated. 
        private readonly List<ExpressionError> _errors = new List<ExpressionError>();
        #endregion

        #region Results from IR 
        private IRResult _irresult;

        #endregion 

        internal TexlBinding Binding
        {
            get
            {
                if (_binding == null)
                {
                    throw new InvalidOperationException($"Must call {nameof(ApplyBindingInternal)} before accessing binding.");
                }

                return _binding;
            }
        }

        /// <summary>
        /// List of all errors and warnings. Check <see cref="ExpressionError.IsWarning"/>.
        /// This can include Parse, Bind, and per-engine custom errors (see <see cref="Engine.PostCheck(CheckResult)"/>, 
        /// or any custom errors passes explicit to the ctor.
        /// Not null, but empty on success.
        /// </summary>
        public IEnumerable<ExpressionError> Errors
        {
            get => GetErrorsInLocale(null);

            [Obsolete("use constructor to set errors")]
            set => _errors.AddRange(value);
        }

        /// <summary>
        /// Get errors localized with the given culture. 
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public IEnumerable<ExpressionError> GetErrorsInLocale(CultureInfo culture)
        {
            culture ??= _defaultErrorCulture ?? this.ParserCultureInfo;

            foreach (var error in this._errors)
            {
                yield return error.GetInLocale(culture);
            }
        }

        /// <summary>
        /// True if no errors for stages run so far. 
        /// </summary>
        public bool IsSuccess => !_errors.Any(x => !x.IsWarning);

        /// <summary>
        /// Helper to throw if <see cref="IsSuccess"/> is false.
        /// </summary>
        public void ThrowOnErrors()
        {
            if (!IsSuccess)
            {
                var msg = string.Join("\r\n", Errors.Select(err => err.ToString()).ToArray());
                throw new InvalidOperationException($"Errors: " + msg);
            }
        }

        internal bool HasDeferredArgsWarning => _errors.Any(x => x.IsWarning && x.MessageKey.Equals(TexlStrings.WarnDeferredType.Key, StringComparison.Ordinal));

        private ReadOnlySymbolTable _allSymbols;

        /// <summary>
        /// Full set of Symbols passed to this binding. 
        /// Can include symbols from Config, Engine, and Parameters, 
        /// May be null. 
        /// Set after binding. 
        /// </summary>
        public ReadOnlySymbolTable Symbols
        {
            get
            {
                if (_binding == null)
                {
                    throw new InvalidOperationException($"Must call {nameof(ApplyBinding)} before accessing combined Sybmols.");
                }

                return this._allSymbols;
            }
        }

        /// <summary>
        /// Parameters are the subset of symbols that must be passed in Eval() for each evaluation. 
        /// This lets us associated the type in Check()  with the values in Eval().
        /// </summary>
        internal ReadOnlySymbolTable Parameters 
        {
            get
            {
                if (!this._setBindingCalled)
                {
                    throw new InvalidOperationException($"Must call {nameof(SetBindingInfo)} first.");
                }

                return _symbols;
            }
        }

        /// <summary>
        /// Culture info used for parsing. 
        /// By default, this is also used for error messages. 
        /// </summary>
        internal CultureInfo ParserCultureInfo { get; private set; }

        internal void ThrowIfSymbolsChanged()
        {
            if (_symbols != null)
            {
                var endHash = _symbols.VersionHash;
                if (_symbolHash != endHash)
                {
                    throw new InvalidOperationException($"SymbolTable was mutated during binding of {_expression}");
                }
            }
        }

        public ParseResult ApplyParse()
        {
            if (_expression == null)
            {
                throw new InvalidOperationException($"Must call {nameof(SetText)} before calling ApplyParse().");
            }

            _beginApply = true;
            if (this.Parse == null)
            {
                var result = Engine.Parse(_expression, Engine.Config.Features, _parserOptions);
                this.Parse = result;

                _errors.AddRange(this.Parse.Errors);
            }

            return this.Parse;
        }

        internal Formula GetParseFormula()
        {
            var parseResult = this.ApplyParse();

            var expression = parseResult.Text;
            var culture = parseResult.Options.Culture;
            var formula = new Formula(expression, culture);
            formula.ApplyParse(parseResult);

            return formula;
        }

        /// <summary>
        /// Call to run binding. 
        /// This will compute types on each node, enabling calling <see cref="GetNodeType(TexlNode)"/>.
        /// </summary>
        public void ApplyBinding()
        {
            ApplyBindingInternal();
        }

        // Apply and return the binding. 
        internal TexlBinding ApplyBindingInternal()
        {
            if (_binding == null)
            {
                if (!this._setBindingCalled)
                {
                    throw new InvalidOperationException($"Must call {nameof(SetBindingInfo)} before calling {nameof(ApplyBinding)}.");
                }

                (var binding, var combinedSymbols) = Engine.ComputeBinding(this);

                this.ThrowIfSymbolsChanged();

                // Don't modify any fields until after we've verified the symbols haven't change.

                this._binding = binding;
                this._allSymbols = combinedSymbols;

                // Add the errors
                IEnumerable<ExpressionError> bindingErrors = ExpressionError.New(binding.ErrorContainer.GetErrors(), ParserCultureInfo);
                _errors.AddRange(bindingErrors);

                if (this.IsSuccess)
                {
                    // TODO: Fix FormulaType.Build to not throw exceptions for Enum types then remove this check
                    if (binding.ResultType.Kind != DKind.Enum)
                    {
                        this.ReturnType = FormulaType.Build(binding.ResultType);
                    }
                }

                if (this.ReturnType != null && this._expectedReturnType != null)
                {
                    bool notCoerceToType = false;
                    if (_allowCoerceToType)
                    {
                        if (this._expectedReturnType != FormulaType.String)
                        {
                            throw new NotImplementedException();
                        }

                        if (!StringValue.AllowedListConvertToString.Contains(this.ReturnType))
                        {
                            notCoerceToType = true;
                        }
                    }

                    var sameType = this._expectedReturnType == this.ReturnType;
                    if (notCoerceToType || !sameType)
                    {
                        _errors.Add(new ExpressionError
                        {
                            Kind = ErrorKind.Validation,
                            Severity = ErrorSeverity.Critical,
                            Span = new Span(0, this._expression.Length),
                            MessageKey = TexlStrings.ErrTypeError_WrongType.Key,
                            _messageArgs = new object[]
                            {
                                this._expectedReturnType._type.GetKindString(),
                                this.ReturnType._type.GetKindString()
                            }
                        });
                    }
                }
            }

            return _binding;
        }

        /// <summary>
        /// Compute the dependencies. Called after binding. 
        /// </summary>
        public void ApplyDependencyAnalysis()
        {
            var binding = this.Binding; // will throw if binding wasn't run
            this._topLevelIdentifiers = DependencyFinder.FindDependencies(binding.Top, binding);
        }

        // Flag to ensure Post Checks are only invoked once. 
        private bool _invokingPostCheck;

        /// <summary>
        /// Calculate all errors. 
        /// Invoke Binding and any engine-specific errors via <see cref="Engine.PostCheck(CheckResult)"/>. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ExpressionError> ApplyErrors()
        {
            if (!_invokingPostCheck)
            {
                _invokingPostCheck = true;

                // Errors require Binding, Parse 
                var binding = ApplyBindingInternal();

                // Plus engine's may have additional constaints. 
                // PostCheck may refer to binding. 
                var extraErrors = Engine.InvokePostCheck(this);

                _errors.AddRange(extraErrors);
            }

            return this.Errors;
        }

        internal IRResult ApplyIR()
        {
            if (_irresult == null)
            {
                // IR should not create any new errors. 
                var binding = this.ApplyBindingInternal();
                this.ThrowOnErrors();
                (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(binding);

                _irresult = new IRResult
                {
                    TopNode = irnode,
                    RuleScopeSymbol = ruleScopeSymbol
                };
            }

            return _irresult;
        }

        /// <summary>
        /// Gets the type of a syntax node. Must call <see cref="ApplyBinding"/> first. 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public FormulaType GetNodeType(TexlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var type = this.Binding.GetType(node);
            return FormulaType.Build(type);
        }

        internal IReadOnlyDictionary<string, TokenResultType> GetTokens(GetTokensFlags flags) => GetTokensUtils.GetTokens(this.Binding, flags);

        private string _expressionInvariant;

        // form of expression with personal info removed,
        // suitable for logging the structure of a formula.
        private string _expressionAnonymous;

        /// <summary>
        /// Get the invariant form of the expression.  
        /// </summary>
        /// <returns></returns>
        public string ApplyGetInvariant()
        {
            if (_expressionInvariant == null)
            {
                this.GetParseFormula(); // will verify 
                var symbols = this.Parameters; // will throw

                _expressionInvariant = _engine.GetInvariantExpressionWorker(this._expression, symbols, parseCulture: _parserOptions.Culture);
            }

            return _expressionInvariant;
        }

        /// <summary>
        /// Get anonymous form of expression with all PII removed. Suitable for logging to 
        /// capture the structure of the expression.
        /// </summary>
        public string ApplyGetLogging()
        {
            if (_expressionAnonymous == null)
            {
                var parse = ApplyParse();
                
                _expressionAnonymous = parse.GetAnonymizedFormula();
            }

            return _expressionAnonymous;
        }
    }

    // Internal interface to ensure that Result objects have a common contract
    // for error reporting. 
    internal interface IOperationStatus
    {
        public IEnumerable<ExpressionError> Errors { get; }

        public bool IsSuccess { get; }
    }
}
