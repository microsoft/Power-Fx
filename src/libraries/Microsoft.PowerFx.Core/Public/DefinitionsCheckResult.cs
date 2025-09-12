// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Holds work such as parsing, binding, error checking done on user definitions.
    /// Tracks which work is done so that it is not repeated.
    /// </summary>
    public class DefinitionsCheckResult : IOperationStatus
    {
        private ReadOnlySymbolTable _symbols;

        private IReadOnlyDictionary<DName, FormulaType> _resolvedTypes;

        private TexlFunctionSet _userDefinedFunctions;

        private CultureInfo _defaultErrorCulture;
        private ParserOptions _parserOptions;

        internal ParserOptions UDFParserOptions => _parserOptions;

        private ParseUserDefinitionResult _parse;

        private readonly Features _features;

        // Local symboltable to store new symbols in a given script and use in binding.
        private readonly SymbolTable _localSymbolTable;

        // Power Fx expression containing definitions
        private string _definitions;

        // $$$ use this to generate binding for intellisense suggestions.
        internal string Definitions => _definitions;

        internal ReadOnlySymbolTable UDFBindingSymbols => ReadOnlySymbolTable.Compose(_localSymbolTable, _symbols);

        private BindingConfig _bindingConfig;

        internal BindingConfig UDFBindingConfig => _bindingConfig;

        // All errors accumulated. 
        private readonly List<ExpressionError> _errors = new List<ExpressionError>();

        // $$$ should we mark ctor as internal?
        public DefinitionsCheckResult() 
            : this(Features.PowerFxV1) 
        { 
        }

        public DefinitionsCheckResult(Features features = null)
        {
            _localSymbolTable = new SymbolTable { DebugName = "LocalUserDefinitions" };
            _features = features ?? Features.PowerFxV1;
        }

        internal DefinitionsCheckResult SetBindingInfo(ReadOnlySymbolTable symbols)
        {
            Contracts.AssertValue(symbols);

            if (_symbols != null)
            {
                throw new InvalidOperationException($"Can only call {nameof(SetBindingInfo)} once.");
            }

            _symbols = symbols;

            return this;
        }

        public DefinitionsCheckResult SetText(string definitions, ParserOptions parserOptions = null)
        {
            Contracts.AssertValue(definitions);

            if (_definitions != null)
            {
                throw new InvalidOperationException($"Can only call {nameof(SetText)} once.");
            }

            _definitions = definitions;
            _parserOptions = parserOptions ?? new ParserOptions();
            _bindingConfig = new BindingConfig(allowsSideEffects: _parserOptions.AllowsSideEffects, useThisRecordForRuleScope: false, numberIsFloat: false, userDefinitionsMode: true);
            _defaultErrorCulture = _parserOptions.Culture ?? CultureInfo.InvariantCulture;

            return this;
        }

        internal ParseUserDefinitionResult ApplyParse()
        {
            if (_definitions == null)
            {
                throw new InvalidOperationException($"Must call {nameof(SetText)} before calling ApplyParse().");
            }

            if (_parse == null)
            {
                _parse = UserDefinitions.Parse(_definitions, _parserOptions, _features);

                if (_parse.HasErrors)
                {
                    _errors.AddRange(ExpressionError.New(_parse.Errors, _defaultErrorCulture));
                }
            }

            return _parse;
        }

        public IReadOnlyDictionary<DName, FormulaType> ResolvedTypes => _resolvedTypes;

        public bool ContainsUDF => _parse.UDFs.Any();

        internal IReadOnlyDictionary<DName, FormulaType> ApplyResolveTypes()
        {
            if (_parse == null)
            {
                this.ApplyParse();
            }

            if (_symbols == null)
            {
                throw new InvalidOperationException($"Must call {nameof(SetBindingInfo)} before calling ApplyResolveTypes().");
            }

            if (_resolvedTypes == null)
            {
                if (_parse.DefinedTypes.Any())
                {
                    this._resolvedTypes = DefinedTypeResolver.ResolveTypes(_parse.DefinedTypes.Where(dt => dt.IsParseValid), _symbols, _parserOptions.NumberIsFloat, out var errors);
                    this._localSymbolTable.AddTypes(this._resolvedTypes);
                    _errors.AddRange(ExpressionError.New(errors, _defaultErrorCulture));
                }
                else
                {
                    this._resolvedTypes = ImmutableDictionary<DName, FormulaType>.Empty;
                }
            }

            return this._resolvedTypes;
        }

        internal TexlFunctionSet ApplyCreateUserDefinedFunctions()
        {
            if (_parse == null)
            {
                this.ApplyParse();
            }

            if (_symbols == null)
            {
                throw new InvalidOperationException($"Must call {nameof(SetBindingInfo)} before calling ApplyCreateUserDefinedFunctions().");
            }

            if (_resolvedTypes == null)
            {
                this.ApplyResolveTypes();
            }

            if (_userDefinedFunctions == null)
            {
                _userDefinedFunctions = new TexlFunctionSet();

                var partialUDFs = UserDefinedFunction.CreateFunctions(_parse.UDFs.Where(udf => udf.IsParseValid), UDFBindingSymbols, out var errors);

                if (errors.Any())
                {
                    _errors.AddRange(ExpressionError.New(errors, _defaultErrorCulture));
                }

                foreach (var udf in partialUDFs)
                {
                    var binding = udf.BindBody(UDFBindingSymbols, new Glue2DocumentBinderGlue(), UDFBindingConfig);
                    
                    List<TexlError> bindErrors = new List<TexlError>();

                    if (binding.ErrorContainer.GetErrors().Any(error => error.Severity > DocumentErrorSeverity.Warning))
                    {
                        _errors.AddRange(ExpressionError.New(binding.ErrorContainer.GetErrors(), _defaultErrorCulture));
                    }
                    else
                    {
                        _localSymbolTable.AddFunction(udf);
                        _userDefinedFunctions.Add(udf);
                    }
                }

                return this._userDefinedFunctions;
            }

            return this._userDefinedFunctions;
        }

        internal IEnumerable<ExpressionError> ApplyErrors()
        {
            if (_resolvedTypes == null)
            {
                this.ApplyCreateUserDefinedFunctions();
            }

            return this.Errors;
        }

        public IEnumerable<ExpressionError> ApplyParseErrors()
        {
            if (_parse == null)
            {
                this.ApplyParse();
            }

            return ExpressionError.New(_parse.Errors, _defaultErrorCulture);
        }

        /// <summary>
        /// List of all errors and warnings. Check <see cref="ExpressionError.IsWarning"/>.
        /// This can include Parse, ResolveType errors />,
        /// Not null, but empty on success.
        /// </summary>
        public IEnumerable<ExpressionError> Errors => GetErrorsInLocale(null);

        /// <summary>
        /// Get errors localized with the given culture. 
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public IEnumerable<ExpressionError> GetErrorsInLocale(CultureInfo culture)
        {
            culture ??= _defaultErrorCulture;

            foreach (var error in this._errors.Distinct(new ExpressionErrorComparer()))
            {
                yield return error.GetInLocale(culture);
            }
        }

        // Set the default culture for localizing error messages. 
        public DefinitionsCheckResult SetDefaultErrorCulture(CultureInfo culture)
        {
            Contracts.AssertValue(culture);

            this._defaultErrorCulture = culture;
            return this;
        }

        /// <summary>
        /// True if no errors for stages run so far. 
        /// </summary>
        public bool IsSuccess => !_errors.Any(x => !x.IsWarning);
    }
}
