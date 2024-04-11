// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Holds a set of Power Fx variables and formulas. Formulas are recalculated when their dependent variables change.
    /// </summary>
    public sealed class RecalcEngine : Engine
    {
        // Map SlotIndex --> Value
        internal Dictionary<int, RecalcFormulaInfo> Formulas { get; } = new Dictionary<int, RecalcFormulaInfo>();

        internal readonly SymbolTable _symbolTable;
        internal readonly SymbolValues _symbolValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecalcEngine"/> class.
        /// Create a new power fx engine. 
        /// </summary>
        public RecalcEngine()
            : this(new PowerFxConfig())
        {
        }

        public RecalcEngine(PowerFxConfig powerFxConfig)
            : base(powerFxConfig)
        {
            _symbolTable = new SymbolTable { DebugName = "Globals" };
            _symbolValues = new SymbolValues(_symbolTable);
            _symbolValues.OnUpdate += OnSymbolValuesOnUpdate;

            base.EngineSymbols = _symbolTable;

            // Add Builtin functions that aren't yet in the shared library. 
            SupportedFunctions = _interpreterSupportedFunctions;
        }

        // Expose publicly. 
        public new ReadOnlySymbolTable EngineSymbols => base.EngineSymbols;

        // Set of default functions supported by the interpreter. 
        private static readonly ReadOnlySymbolTable _interpreterSupportedFunctions = ReadOnlySymbolTable.NewDefault(Library.FunctionList);

        // For internal testing
        internal INameResolver TestCreateResolver()
        {
            return CreateResolverInternal();
        }

        // Event handler fired when we update symbol values. 
        private void OnSymbolValuesOnUpdate(ISymbolSlot slot, FormulaValue arg2)
        {
            if (Formulas.TryGetValue(slot.SlotIndex, out var info))
            {
                if (!info.IsFormula)
                {
                    // IF we've updated a non-formula (variable), then trigger the recalc chain.
                    // Cascading formula recalc will be triggered by Recalc chain. 
                    Recalc(info.Name);
                }
            }
        }

        public void UpdateVariable(string name, double value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, decimal value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, int value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, long value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, string value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, bool value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, Guid value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, DateTime value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        public void UpdateVariable(string name, TimeSpan value)
        {
            UpdateVariable(name, FormulaValue.New(value));
        }

        /// <summary>
        /// Create or update a named variable to a value. 
        /// </summary>
        /// <param name="name">variable name. This can be used in other formulas.</param>
        /// <param name="value">constant value.</param>
        public void UpdateVariable(string name, FormulaValue value)
        {
            var x = value;

            if (value is TableValue && !value.CanShallowCopy)
            {
                throw new InvalidOperationException($"Can't set '{name}' to a table value that cannot be copied.");
            }

            if (TryGetByName(name, out var fi))
            {
                // Set() will validate type compatibility
                _symbolValues.Set(fi.Slot, value);

                // Be sure to preserve used-by set. 
            }
            else
            {
                // New
                var slot = _symbolTable.AddVariable(name, value.Type, mutable: true);

                Formulas[slot.SlotIndex] = RecalcFormulaInfo.NewVariable(slot, name, x.IRContext.ResultType);
                _symbolValues.Set(slot, value);
            }

            // Recalc was triggered by SymbolValue Set's OnUpdate handler. 
        }

        /// <summary>
        /// Evaluate an expression as text and return the result.
        /// </summary>
        /// <param name="expressionText">textual representation of the formula.</param>
        /// <param name="parameters">parameters for formula. The fields in the parameter record can 
        /// be acecssed as top-level identifiers in the formula.</param>
        /// <param name="options"></param>        
        /// <returns>The formula's result.</returns>
        public FormulaValue Eval(string expressionText, RecordValue parameters = null, ParserOptions options = null)
        {
            return EvalAsync(expressionText, CancellationToken.None, parameters, options).Result;
        }

        public async Task<FormulaValue> EvalAsync(string expressionText, CancellationToken cancellationToken, RecordValue parameters, ParserOptions options = null)
        {
            if (parameters == null)
            {
                parameters = RecordValue.Empty();
            }

            var symbolValues = ReadOnlySymbolValues.NewFromRecord(parameters);
            var runtimeConfig = new RuntimeConfig(symbolValues);

            return await EvalAsync(expressionText, cancellationToken, options, null, runtimeConfig).ConfigureAwait(false);
        }

        public async Task<FormulaValue> EvalAsync(string expressionText, CancellationToken cancellationToken, ReadOnlySymbolValues runtimeConfig)
        {
            var runtimeConfig2 = new RuntimeConfig(runtimeConfig);
            return await EvalAsync(expressionText, cancellationToken, runtimeConfig: runtimeConfig2).ConfigureAwait(false);
        }

        public async Task<FormulaValue> EvalAsync(string expressionText, CancellationToken cancellationToken, ParserOptions options = null, ReadOnlySymbolTable symbolTable = null, RuntimeConfig runtimeConfig = null)
        {
            // We could have any combination of symbols and runtime values. 
            // - RuntimeConfig may be null if we don't need it. 
            // - Some Symbols are metadata-only (like option sets, UDFs, constants, etc)
            // and hence don't require a corresponnding runtime Symbol Value. 
            var parameterSymbols = runtimeConfig?.Values?.SymbolTable;
            var symbolsAll = ReadOnlySymbolTable.Compose(parameterSymbols, symbolTable);

            options ??= this.GetDefaultParserOptionsCopy();

            var check = Check(expressionText, options, symbolsAll);
            check.ThrowOnErrors();

            var stackMarker = new StackDepthCounter(Config.MaxCallDepth);
            var eval = check.GetEvaluator(stackMarker);

            var result = await eval.EvalAsync(cancellationToken, runtimeConfig).ConfigureAwait(false);
            return result;
        }

        // Invoke onUpdate() each time this formula is changed, passing in the new value. 
        public void SetFormula(string name, string expr, Action<string, FormulaValue> onUpdate)
        {
            SetFormula(name, new FormulaWithParameters(expr), onUpdate);
        }

        /// <summary>
        /// Create a formula that will be recalculated when its dependent values change.
        /// </summary>
        /// <param name="name">name of formula. This can be used in other formulas.</param>
        /// <param name="expr">expression.</param>
        /// <param name="onUpdate">Callback to fire when this value is updated.</param>
        public void SetFormula(string name, FormulaWithParameters expr, Action<string, FormulaValue> onUpdate)
        {
            var check = Check(expr._expression, expr._schema);
            check.ThrowOnErrors();
            var binding = check.Binding;

            // This will fail if it already exists 
            var slot = _symbolTable.AddVariable(name, check.ReturnType, mutable: false);

            // We can't have cycles because:
            // - formulas can only refer to already-defined values
            // - formulas can't be redefined.  
            var dependsOn = check.TopLevelIdentifiers;

            var type = FormulaType.Build(binding.ResultType);
            var info = RecalcFormulaInfo.NewFormula(slot, name, type, dependsOn, binding, onUpdate);

            Formulas[slot.SlotIndex] = info;

            foreach (var x in dependsOn)
            {
                GetByName(x)._usedBy.Add(name);
            }

            Recalc(name);
        }

        // Trigger a recalc on name and anything that depends on it. 
        // Invoke on Update callbacks. 
        private void Recalc(string name)
        {
            var r = new RecalcEngineWorker(this);
            r.Recalc(name);
        }

        /// <summary>
        /// Delete formula that was previously created.
        /// </summary>
        /// <param name="name">Formula name.</param>
        public void DeleteFormula(string name)
        {
            if (TryGetByName(name, out var fi))
            {
                if (fi._usedBy.Count == 0)
                {
                    if (fi._dependsOn != null)
                    {
                        foreach (var dependsOnName in fi._dependsOn)
                        {
                            if (TryGetByName(dependsOnName, out var info))
                            {
                                info._usedBy.Remove(name);
                            }
                        }
                    }

                    Formulas.Remove(fi.Slot.SlotIndex);
                    _symbolTable.RemoveVariable(name);
                }
                else
                {
                    throw new InvalidOperationException($"Formula {name} cannot be deleted due to the following dependencies: {string.Join(", ", fi._usedBy)}");
                }
            }
            else
            {
                throw new InvalidOperationException($"Formula {name} does not exist");
            }
        }

        /// <summary>
        /// Get the current value of a formula. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FormulaValue GetValue(string name)
        {
            TryGetByName(name, out var fi);
            var value = _symbolValues.Get(fi.Slot);
            return value;
        }

        /// <summary>
        /// Try to get the current value of a formula.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string name, out FormulaValue value)
        {
            value = null;
            if (!TryGetByName(name, out var fi))
            {
                return false;
            }

            value = _symbolValues.Get(fi.Slot);
            return true;
        }

        internal RecalcFormulaInfo GetByName(string name)
        {
            if (TryGetByName(name, out var fi))
            {
                return fi;
            }

            throw new InvalidOperationException($"{name} not found");
        }

        internal bool TryGetByName(string name, out RecalcFormulaInfo info)
        {
            if (_symbolTable.TryLookupSlot(name, out var slot))
            {
                if (slot.Owner != _symbolTable)
                {
                    throw _symbolTable.NewBadSlotException(slot);
                }

                info = Formulas[slot.SlotIndex];
                return true;
            }

            info = null;
            return false;
        }

        public bool TryGetVariableType(string name, out FormulaType type)
        {
            type = default;

            if (_symbolTable.TryGetVariable(new DName(name), out var nameLookupInfo, out _))
            {
                type = FormulaType.Build(nameLookupInfo.Type);
                return true;
            }

            return false;
        }

        public void UpdateSupportedFunctions(SymbolTable s)
        {
            SupportedFunctions = s;
        }

        /// <summary>
        /// Add a set of user-defined formulas and functions to the engine.
        /// </summary>
        /// <param name="script">Script containing user defined functions and/or named formulas.</param>
        /// <param name="parseCulture">Locale to parse user defined script.</param>
        /// <param name="onUpdate">Function to be called when update is triggered.</param>
        public void AddUserDefinitions(string script, CultureInfo parseCulture = null, Action<string, FormulaValue> onUpdate = null)
        {
            // Compose will handle null symbols
            var composedSymbols = SymbolTable.Compose(Config.SymbolTable, SupportedFunctions, _symbolTable, PrimitiveTypes);

            var userDefinitionResult = UserDefinitions.Process(script, parseCulture, features: Config.Features, globalNameResolver: composedSymbols);

            if (userDefinitionResult.DefinedTypes.Any()) 
            {
                Config.SymbolTable.AddTypes(userDefinitionResult.DefinedTypes);
            }

            var sb = new StringBuilder();

            foreach (var udf in userDefinitionResult.UDFs)
            {
                Config.SymbolTable.AddFunction(udf);
                var binding = udf.BindBody(composedSymbols, new Glue2DocumentBinderGlue(), BindingConfig.Default, features: Config.Features);

                List<TexlError> errors = new List<TexlError>();

                if (binding.ErrorContainer.GetErrors(ref errors))
                {
                    sb.AppendLine(string.Join(", ", errors.Select(err => err.ToString())));
                }
            }

            if (sb.Length > 0)
            {
                throw new InvalidOperationException(sb.ToString());
            }

            foreach (var namedFormula in userDefinitionResult.NamedFormulas)
            {
                SetFormula(namedFormula.Ident.Name, namedFormula.Formula.ToString(), onUpdate);
            }
        }
    } // end class RecalcEngine
}
