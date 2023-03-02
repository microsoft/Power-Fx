// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public CheckResult SetText(ParseResult parse)
        {
            if (_engine == null)
            {
                throw new InvalidOperationException($"Can't call {nameof(SetText)} without an engine.");
            }

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
            if (_engine == null)
            {
                throw new InvalidOperationException($"Can't call {nameof(SetText)} without an engine.");
            }

            expression = expression ?? throw new ArgumentNullException(nameof(expression));

            if (_expression != null)
            {
                throw new InvalidOperationException($"Can only call {nameof(SetText)} once.");
            }

            _expression = expression;
            _parserOptions = parserOptions ?? Engine.GetDefaultParserOptionsCopy();

            return this;
        }

        // Symbols could be null if no additional symbols are provided. 
        public CheckResult SetBindingInfo(ReadOnlySymbolTable symbols)
        {
            if (_engine == null)
            {
                throw new InvalidOperationException($"Can't call {nameof(SetText)} without an engine.");
            }

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
            get => _errors;

            [Obsolete("use constructor to set errors")]
            set => _errors.AddRange(value);
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

        internal bool HasDeferredArgsWarning => _errors.Any(x => x.IsWarning && x.MessageKey.Equals(TexlStrings.WarnDeferredType.Key));

        /// <summary>
        /// Full set of Symbols passed to this binding. 
        /// Can include symbols from Config, Engine, and Parameters, 
        /// May be null. 
        /// </summary>
        internal ReadOnlySymbolTable AllSymbols { get; private set; }

        /// <summary>
        /// Parameters are the subset of symbols that must be passed in Eval() for each evaluation. 
        /// This lets us associated the type in Check()  with the values in Eval().
        /// </summary>
        internal ReadOnlySymbolTable Parameters => _symbols;

        /// <summary>
        /// Culture info passed to this binding. May be null. 
        /// </summary>
        internal CultureInfo CultureInfo => this.Engine.Config.CultureInfo;

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
                this.AllSymbols = combinedSymbols;

                // Add the errors
                IEnumerable<ExpressionError> bindingErrors = ExpressionError.New(binding.ErrorContainer.GetErrors(), CultureInfo);
                _errors.AddRange(bindingErrors);

                if (this.IsSuccess && !this.HasDeferredArgsWarning)
                {
                    // TODO: Fix FormulaType.Build to not throw exceptions for Enum types then remove this check
                    if (binding.ResultType.Kind != DKind.Enum)
                    {
                        this.ReturnType = FormulaType.Build(binding.ResultType);
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

        public string GetIR()
        {
            return this.ApplyIR().TopNode.ToString();
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
    }

    // Internal interface to ensure that Result objects have a common contract
    // for error reporting. 
    internal interface IOperationStatus
    {
        public IEnumerable<ExpressionError> Errors { get; }

        public bool IsSuccess { get; }
    }
}
