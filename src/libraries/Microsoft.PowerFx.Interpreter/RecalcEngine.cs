// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Interpreter.UDF;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Holds a set of Power Fx variables and formulas. Formulas are recalculated when their dependent variables change.
    /// </summary>
    public sealed class RecalcEngine : Engine, IPowerFxEngine
    {
        internal Dictionary<string, RecalcFormulaInfo> Formulas { get; } = new Dictionary<string, RecalcFormulaInfo>();

        internal readonly RecalcEngineResolver _symbolTable;
        internal readonly RecalcSymbolValues _symbolValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecalcEngine"/> class.
        /// Create a new power fx engine. 
        /// </summary>
        public RecalcEngine()
            : this(new PowerFxConfig(null))
        {
        }

        public RecalcEngine(PowerFxConfig powerFxConfig)
            : base(powerFxConfig)
        {
            _symbolTable = new RecalcEngineResolver(this);
            _symbolValues = new RecalcSymbolValues(this);

            EngineSymbols = _symbolTable;

            // Add Builtin functions that aren't yet in the shared library. 
            SupportedFunctions = _interpreterSupportedFunctions;
        }

        // Set of default functions supported by the interpreter. 
        private static readonly ReadOnlySymbolTable _interpreterSupportedFunctions = ReadOnlySymbolTable.NewDefault(Library.FunctionList);

        // For internal testing
        internal INameResolver TestCreateResolver()
        {
            return CreateResolverInternal();
        }

        /// <inheritdoc/>
        protected override IExpression CreateEvaluator(CheckResult result)
        {
            return CreateEvaluatorDirect(result, new StackDepthCounter(Config.MaxCallDepth));
        }

        internal static IExpression CreateEvaluatorDirect(CheckResult result, StackDepthCounter stackMarker)
        {
            if (result._binding == null)
            {
                throw new InvalidOperationException($"Requires successful binding");
            }

            result.ThrowOnErrors();

            (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(result._binding);
            return new ParsedExpression(irnode, ruleScopeSymbol, stackMarker);
        }

        /// <summary>
        /// Create an evaluator over the existing binding.
        /// </summary>
        /// <param name = "result" >A successful binding from a previous call to.<see cref="Engine.Check(string, RecordType, ParserOptions)"/>. </param>        
        /// <returns></returns>
        public static IExpression CreateEvaluatorDirect(CheckResult result)
        {
            return CreateEvaluatorDirect(result, new StackDepthCounter(PowerFxConfig.DefaultMaxCallDepth));
        }

        public void UpdateVariable(string name, double value)
        {
            UpdateVariable(name, new NumberValue(IRContext.NotInSource(FormulaType.Number), value));
        }

        /// <summary>
        /// Create or update a named variable to a value. 
        /// </summary>
        /// <param name="name">variable name. This can be used in other formulas.</param>
        /// <param name="value">constant value.</param>
        public void UpdateVariable(string name, FormulaValue value)
        {
            var x = value;

            if (Formulas.TryGetValue(name, out var fi))
            {
                // Type should match?
                if (fi._type != x.Type)
                {
                    throw new NotSupportedException($"Can't change '{name}''s type from {fi._type} to {x.Type}.");
                }

                fi.Value = x;

                // Be sure to preserve used-by set. 
            }
            else
            {
                Formulas[name] = new RecalcFormulaInfo { Value = x, _type = x.IRContext.ResultType };
            }

            // Could trigger recalcs?
            Recalc(name);
        }

        /// <summary>
        /// Evaluate an expression as text and return the result.
        /// </summary>
        /// <param name="expressionText">textual representation of the formula.</param>
        /// <param name="parameters">parameters for formula. The fields in the parameter record can 
        /// be acecssed as top-level identifiers in the formula.</param>
        /// <param name="options"></param>
        /// <param name="symbols"></param>
        /// <returns>The formula's result.</returns>
        public FormulaValue Eval(string expressionText, RecordValue parameters = null, ParserOptions options = null, ReadOnlySymbolValues symbols = null)
        {
            return EvalAsync(expressionText, CancellationToken.None, parameters, options, symbols).Result;
        }

        public async Task<FormulaValue> EvalAsync(string expressionText, CancellationToken cancel, RecordValue parameters, ParserOptions options = null, ReadOnlySymbolValues symbols = null)
        {
            if (parameters == null)
            {
                parameters = RecordValue.Empty();
            }

            var check = Check(expressionText, (RecordType)parameters.IRContext.ResultType, options);
            check.ThrowOnErrors();

            var stackMarker = new StackDepthCounter(Config.MaxCallDepth);
            var run = check.GetEvaluator(stackMarker);

            var result = await run.EvalAsync(cancel, parameters);
            return result;
        }

        public async Task<FormulaValue> EvalAsync(
            string expressionText,
            CancellationToken cancel,
            ParserOptions options = null,
            ReadOnlySymbolTable symbolTable = null,
            ReadOnlySymbolValues runtimeConfig = null)
        {
            // We could have any combination of symbols and runtime values. 
            // - RuntimeConfig may be null if we don't need it. 
            // - Some Symbols are metadata-only (like option sets, UDFs, constants, etc)
            // and hence don't require a corresponnding runtime Symbol Value. 
            var symbolsAll = ReadOnlySymbolTable.Compose(runtimeConfig?.GetSymbolTableSnapshot(), symbolTable);

            var check = Check(expressionText, options, symbolsAll);
            check.ThrowOnErrors();

            var eval = (ParsedExpression)check.Expression;

            return await eval.EvalAsync(cancel, runtimeConfig);
        }

        public DefineFunctionsResult DefineFunctions(string script)
        {
            var parsedUDFS = new Core.Syntax.ParsedUDFs(script);
            var result = parsedUDFS.GetParsed();

            var udfDefinitions = result.UDFs.Select(udf => new UDFDefinition(
                udf.Ident.ToString(),
                udf.Body.ToString(),
                FormulaType.GetFromStringOrNull(udf.ReturnType.ToString()),
                udf.IsImperative,
                udf.Args.Select(arg => new NamedFormulaType(arg.VarIdent.ToString(), FormulaType.GetFromStringOrNull(arg.VarType.ToString()))).ToArray())).ToArray();
            return DefineFunctions(udfDefinitions);
        }

        /// <summary>
        /// For private use because we don't want anyone defining a function without binding it.
        /// </summary>
        /// <returns></returns>
        private UDFLazyBinder DefineFunction(UDFDefinition definition)
        {
            // $$$ Would be a good helper function 
            var record = RecordType.Empty();
            foreach (var p in definition.Parameters)
            {
                record = record.Add(p);
            }

            var check = new CheckWrapper(this, definition.Body, record, definition.IsImperative);

            var func = new UserDefinedTexlFunction(definition.Name, definition.ReturnType, definition.Parameters, check);

            var exists = _symbolTable.Functions.Any(x => x.Name == definition.Name);
            if (exists)
            {
                throw new InvalidOperationException($"Function {definition.Name} is already defined");
            }

            _symbolTable.AddFunction(func);
            return new UDFLazyBinder(func, definition.Name);
        }

        private void RemoveFunction(string name)
        {
            _symbolTable.RemoveFunction(name);
        }

        /// <summary>
        /// Tries to define and bind all the functions here. If any function names conflict returns an expression error. 
        /// Also returns any errors from binding failing. All functions defined here are removed if any of them contain errors.
        /// </summary>
        /// <param name="udfDefinitions"></param>
        /// <returns></returns>
        internal DefineFunctionsResult DefineFunctions(IEnumerable<UDFDefinition> udfDefinitions)
        {
            var expressionErrors = new List<ExpressionError>();

            var binders = new List<UDFLazyBinder>();
            foreach (UDFDefinition definition in udfDefinitions)
            {
                binders.Add(DefineFunction(definition));
            }

            foreach (UDFLazyBinder lazyBinder in binders)
            {
                var possibleErrors = lazyBinder.Bind();
                if (possibleErrors.Any())
                {
                    expressionErrors.AddRange(possibleErrors);
                }
            }

            if (expressionErrors.Any())
            {
                foreach (UDFLazyBinder lazyBinder in binders)
                {
                    RemoveFunction(lazyBinder.Name);
                }
            }

            return new DefineFunctionsResult(expressionErrors, binders.Select(binder => new FunctionInfo(binder.Function)));
        }

        internal DefineFunctionsResult DefineFunctions(params UDFDefinition[] udfDefinitions)
        {
            return DefineFunctions(udfDefinitions.AsEnumerable());
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
            if (Formulas.ContainsKey(name))
            {
                throw new InvalidOperationException($"Can't change existing formula: {name}");
            }

            var check = Check(expr._expression, expr._schema);
            check.ThrowOnErrors();
            var binding = check._binding;

            // We can't have cycles because:
            // - formulas can only refer to already-defined values
            // - formulas can't be redefined.  
            var dependsOn = check.TopLevelIdentifiers;

            var type = FormulaType.Build(binding.ResultType);
            var info = new RecalcFormulaInfo
            {
                _dependsOn = dependsOn,
                _type = type,
                _binding = binding,
                _onUpdate = onUpdate
            };

            Formulas[name] = info;

            foreach (var x in dependsOn)
            {
                Formulas[x]._usedBy.Add(name);
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
            if (Formulas.TryGetValue(name, out var fi))
            {
                if (fi._usedBy.Count == 0)
                {
                    foreach (var dependsOnName in fi._dependsOn)
                    {
                        if (Formulas.TryGetValue(dependsOnName, out var info))
                        {
                            info._usedBy.Remove(name);
                        }
                    }

                    Formulas.Remove(name);
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
            var fi = Formulas[name];
            return fi.Value;
        }
    } // end class RecalcEngine
}
