// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Repl;
using Microsoft.PowerFx.Repl.Functions;
using Microsoft.PowerFx.Repl.Services;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Microsoft.PowerFx
{
    /// <summary>
    /// A REPL (Read-Eval-Print Loop) for Power Fx. 
    /// This accepts input, evaluates it, and prints the result.
    /// </summary>
    [Obsolete("Preview")]
    public class PowerFxREPL
    {
        public RecalcEngine Engine { get; set; }

        public ValueFormatter ValueFormatter { get; set; } = new StandardFormatter();

        public HelpProvider HelpProvider { get; set; } = new HelpProvider();

        public IReplOutput Output { get; set; } = new ConsoleReplOutput();

        // Allow repl to create new definitions, such as Set(). 
        public bool AllowSetDefinitions { get; set; }

        // Do we print each command before evaluation?
        // Useful if we're running a file and are debugging, or if input UI is separated from output UI. 
        public bool Echo { get; set; } = false;

        // Do we print each result after evaluation?
        // Useful if we're running in interactive mode, or with Echo enabled.
        public bool PrintResult { get; set; } = true;

        // Has Exit() been called?
        // Host can check this flag after each command is executed to see if the repl should close.
        public bool ExitRequested { get; set; } = false;

        /// <summary>
        /// Optional - provide the current user. 
        /// Must call <see cref="EnableUserObject(string[])"/> first to declare the schema.
        /// </summary>
        public UserInfo UserInfo { get; set; }

        /// <summary>
        /// Optional set of Services provided to Repl at eval time. 
        /// </summary>
        public IServiceProvider InnerServices { get; set; }

        // Policy for handling multiple lines. 
        public MultilineProcessor MultilineProcessor { get; set; } = new MultilineProcessor();

        public ParserOptions ParserOptions { get; set; } = new ParserOptions() { AllowsSideEffects = true };

        // example override, switching to [1], [2] etc.
        public virtual string Prompt => ">> ";

        // prompt for multiline continuation
        public virtual string PromptContinuation => ".. ";

        /// <summary>
        /// Optional - if set, additional symbol values that are fed into the repl . 
        /// </summary>
        public ReadOnlySymbolValues ExtraSymbolValues { get; set; }

        /// <summary>
        /// Get sorted names of all functions. This includes functions from the <see cref="Engine"/> as well as <see cref="MetaFunctions"/>.
        /// </summary>
        public IEnumerable<string> FunctionNames
        {
            get
            {
                // Can't use Engine.SupportedFunctions as that doesn't include Engine.AddFunction functions
                var set = new HashSet<string>();
                set.UnionWith(this.Engine.GetAllFunctionNames());
                set.UnionWith(this.MetaFunctions.FunctionNames);
                var list = set.ToArray();
                Array.Sort(list);
                
                return list;
            }
        }

        // Interpreter should normally not throw.
        // Exceptions should be caught and converted to ErrorResult.
        // Not called for OperationCanceledException since those are expected.
        // This can rethrow
        public virtual async Task OnEvalExceptionAsync(Exception e, CancellationToken cancel)
        {
            var msg = e.ToString();

            await this.Output.WriteLineAsync(msg, OutputKind.Error, cancel);
        }

        private readonly IDictionary<string, IPseudoFunction> _pseudoFunctions = new Dictionary<string, IPseudoFunction>();

        public void AddPseudoFunction(IPseudoFunction func)
        {
            _pseudoFunctions.Add(func.Name, func);
        }

        public PowerFxREPL()
        {
            this.MetaFunctions.AddFunction(new NotifyFunction());

            this.MetaFunctions.AddFunction(new ExitFunction(this));

            // Hook through the HelpProvider, don't override these Help functions
            this.MetaFunctions.AddFunction(new Help0Function(this));
            this.MetaFunctions.AddFunction(new Help1Function(this));
        }

        private bool _userEnabled = false;

        public void EnableSampleUserObject()
        {
            var sampleUserInfo = new BasicUserInfo
            {
                FullName = "First Last",
                Email = "SampleUser@contoso.com",
                DataverseUserId = new Guid("88888888-044f-4928-a95f-30d4c8ebf118"),
                TeamsMemberId = "29:1DUjC5z4ttsBQa0fX2O7B0IDu30R",
                EntraObjectId = new Guid("99999999-044f-4928-a95f-30d4c8ebf118"),
            };

            UserInfo = sampleUserInfo.UserInfo;

            this.EnableUserObject(UserInfo.AllKeys);
        }

        // Either inherited symbols must define User (and it's schema), 
        // or we can define it now. 
        public void EnableUserObject(params string[] keys)
        {
            if (_userEnabled)
            {
                throw new InvalidOperationException($"Can't enable User object twice.");
            }

            _userEnabled = true;
            if (keys.Length == 0)
            {
                // Assume inherited has defined it.
                var check = this.Engine.Check("User");
                if (!check.IsSuccess)
                {
                    throw new InvalidOperationException($"Must either specify User properties or engine must have it enabled already.");
                }
            }
            else
            {
                this.MetaFunctions.AddUserInfoObject(keys);
            }
        }

        // Separate symbol table so that Repl doesn't interfere with engine.
        public SymbolTable MetaFunctions { get; } = new SymbolTable { DebugName = "Repl Functions" };

        /// <summary>
        /// Print the prompt - call this before input. The prompt can change based on whether this is the first
        /// line of input or a contination within a multiline. 
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public virtual async Task WritePromptAsync(CancellationToken cancel = default)
        {
            string prompt;

            if (this.MultilineProcessor.IsFirstLine)
            {
                prompt = this.Prompt;
            }
            else
            {
                // in the middle of a multiline
                prompt = this.PromptContinuation;
            }

            // Start of new command 
            await this.Output.WriteAsync(prompt, OutputKind.Control, cancel);
        }

        /// <summary>
        /// Accept a single line of input and evaluate it.
        /// This allows continuations via the policy set in <see cref="MultilineProcessor"/>.
        /// </summary>
        /// <param name="line">A line of input.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="suggest">Provide suggestions instead of evaluating the expression.</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If cancelled.</exception>
        public async Task<ReplResult> HandleLineAsync(string line, bool suggest = false, uint? lineNumber = null, CancellationToken cancel = default)
        {
            if (this.Engine == null)
            {
                throw new InvalidOperationException($"Engine is not set.");
            }

            string expression = this.MultilineProcessor.HandleLine(line);

            if (expression != null)
            {
                return await this.HandleCommandAsync(expression, suggest, lineNumber, cancel);
            }

            return null;
        }

        /// <summary>
        /// Directly invoke a command. This skips multiline handling. 
        /// </summary>
        /// <param name="expression">expression to run.</param>
        /// <param name="suggest">Provide suggestions instead of evaluating the expression.</param>
        /// <param name="cancel">cancellation token.</param>
        /// <param name="lineNumber">line number to attribute errors to.</param>
        /// <returns>status object with details.</returns>
        /// <exception cref="InvalidOperationException">invalid.</exception>
        public virtual async Task<ReplResult> HandleCommandAsync(string expression, bool suggest = false, uint? lineNumber = null, CancellationToken cancel = default)
        {
            string lineError = lineNumber == null ? string.Empty : $"Line {lineNumber}: ";

            if (this.Engine == null)
            {
                throw new InvalidOperationException($"Engine is not set.");
            }

            if (string.IsNullOrWhiteSpace(expression))
            {
                return new ReplResult();
            }

            if (this.Echo)
            {
                await this.WritePromptAsync(cancel);

                // better to strip any newline and add one ourselves (with WriteLine), in case the expression didn't come in with one
                await this.Output.WriteLineAsync(expression.TrimEnd(), OutputKind.Repl, cancel);
            }

            ReadOnlySymbolTable extraSymbolTable = this.ExtraSymbolValues?.SymbolTable;
            RuntimeConfig runtimeConfig = new RuntimeConfig(this.ExtraSymbolValues) { ServiceProvider = new BasicServiceProvider(this.InnerServices) };

            if (this.UserInfo != null)
            {
                if (!_userEnabled)
                {
                    throw new InvalidOperationException($"Must call {nameof(EnableUserObject)} before setting {nameof(UserInfo)}.");
                }

                runtimeConfig.SetUserInfo(this.UserInfo);
            }

            runtimeConfig.AddService(this.Output);
            ReadOnlySymbolTable currentSymbolTable = ReadOnlySymbolTable.Compose(this.MetaFunctions, extraSymbolTable);            

            CheckResult check = new CheckResult(this.Engine).SetText(expression, ParserOptions).SetBindingInfo(currentSymbolTable);            
            check.ApplyParse();

            if (suggest)
            {
                IIntellisenseResult intellisenseResults = this.Engine.Suggest(check, expression.Length, runtimeConfig.ServiceProvider);
                return new ReplResult { CheckResult = check, IntellisenseResult = intellisenseResults };
            }

            HashSet<string> varsToDisplay = new HashSet<string>();

            if (check.Parse.IsSuccess)
            {
                // comments, pseudo functions, and named formula assignments, handled outside of the interpreter
                // for our purposes, we don't need the engine's features or the parser options

                // comment only
                if (check.Parse.Root is BlankNode bn)
                {
                    return new ReplResult();
                }

                // pseudo function call
                if (check.Parse.Root is CallNode cn && _pseudoFunctions.TryGetValue(cn.Head.Name, out var psuedoFunction))
                {
                    // Foo(expr)
                    // where Foo() is a peudo function, get CheckResult for just 'expr' and pass in. 
                    // Inner expr doesn't get access to meta functions. 
                    var innerExpr = cn.Args.ToString();
                    CheckResult psuedoCheck = this.Engine.Check(innerExpr, options: this.ParserOptions, symbolTable: extraSymbolTable);

                    await psuedoFunction.ExecuteAsync(psuedoCheck, this, cancel).ConfigureAwait(false);

                    return new ReplResult();
                }

                // named formula assignment
                if (check.Parse.Root is BinaryOpNode bo && bo.Op == BinaryOp.Equal && bo.Left.Kind == NodeKind.FirstName)
                {
                    var formula = bo.Right.ToString();
                    CheckResult formulaCheck = this.Engine.Check(formula, options: this.ParserOptions, symbolTable: extraSymbolTable);

                    if (formulaCheck.IsSuccess)
                    {
                        try
                        {
                            Engine.SetFormula(bo.Left.ToString(), formula, OnFormulaUpdate);
                        }
                        catch (Exception ex)
                        {
                            await this.Output.WriteLineAsync(lineError + "Error: " + ex.Message, OutputKind.Error, cancel)
                                .ConfigureAwait(false);
                        }

                        return new ReplResult();
                    }
                    else
                    {
                        foreach (var error in formulaCheck.Errors)
                        {
                            var kind = error.IsWarning ? OutputKind.Warning : OutputKind.Error;
                            await this.Output.WriteLineAsync(lineError + error.ToString(), kind, cancel)
                                .ConfigureAwait(false);
                        }

                        return new ReplResult { CheckResult = formulaCheck };
                    }
                }
            }

            // Pre-scan expression for declarations, like Set(x, y)
            // If found, declare 'x'. And the proceed with eval like normal. 
            if (check.IsSuccess && this.AllowSetDefinitions)
            {
                var vis = new FindDeclarationVisitor();
                check.Parse.Root.Accept(vis);

                bool createdDeclarations = false;

                foreach (var declare in vis._declarations)
                {
                    var name = declare._variableName;
                    varsToDisplay.Add(name);
                    if (!Engine.TryGetVariableType(name, out var existingType))
                    {
                        // Deosn't exist yet!

                        // Get the type. 
                        var rhsExpr = declare._rhs.GetCompleteSpan().GetFragment(expression);

                        var setCheck = this.Engine.Check(rhsExpr, ParserOptions, this.ExtraSymbolValues?.SymbolTable);
                        if (!setCheck.IsSuccess)
                        {
                            await this.Output.WriteLineAsync($"Failed to initialize '{name}'.", OutputKind.Error, cancel).ConfigureAwait(false);
                            return new ReplResult { CheckResult = setCheck };
                        }

                        // Start as blank. Will execute expression below to actually assign. 
                        var setValue = FormulaValue.NewBlank(setCheck.ReturnType);

                        this.Engine.UpdateVariable(name, setValue);
                        
                        createdDeclarations = true;
                    }
                    else
                    {
                        // Already exists, don't need to declare. 
                    }
                }

                if (createdDeclarations)
                {
                    // Rebind expression with new declarations. 
                    check = new CheckResult(this.Engine)
                        .SetText(expression, new ParserOptions { AllowsSideEffects = true })
                        .SetBindingInfo(currentSymbolTable);

                    check.ApplyParse();
                }

                // Now, all variables are defined, just execute expression like normal.
            }

            // Show parse/bind errors 
            var errors = check.ApplyErrors();
            if (!check.IsSuccess)
            {
                foreach (var error in check.Errors)
                {
                    var kind = error.IsWarning ? OutputKind.Warning : OutputKind.Error;
                    var msg = error.ToString();

                    // If case is wrong or parens are missing for Help() or Exit(), suggest the correct form
                    if (error.MessageKey == "ErrInvalidName" || error.MessageKey == "ErrUnknownFunction" || error.MessageKey == "ErrUnimplementedFunction")
                    {
                        var invalid = error.MessageArgs.First().ToString().ToLowerInvariant();
                        if (invalid == "help" || invalid == "exit")
                        {
                            msg = msg + $" Did you mean \"{invalid.Substring(0, 1).ToUpperInvariant()}{invalid.Substring(1)}()\"?";
                        }
                    }

                    await this.Output.WriteLineAsync(lineError + msg, kind, cancel)
                        .ConfigureAwait(false);
                }

                return new ReplResult { CheckResult = check };
            }

            // Now eval

            var runner = check.GetEvaluator();

            FormulaValue result;

            try
            {
                result = await runner.EvalAsync(cancel, runtimeConfig).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // $$$ cancelled task, or a normal abort? 
                // Signal to caller that we're done. 
                throw;
            }
            catch (Exception e)
            {
                await this.OnEvalExceptionAsync(e, cancel);

                // $$$ Should be an error. 
                return new ReplResult { CheckResult = check };
            }

            var replResult = new ReplResult
            {
                CheckResult = check,
                EvalResult = result,
            };

            if (PrintResult)
            {
                // Print results for updated named formulas
                foreach (var varName in varsToDisplay)
                {
                    var varValue = this.Engine.GetValue(varName);
                    replResult.DeclaredVars[varName] = varValue;

                    await this.WriteLineVarAsync(varValue, cancel, $"{varName}: ");
                }

                // Now print the result for this expression
                await this.WriteLineVarAsync(result, cancel);
            }

            return replResult;
        }

        public virtual async void OnFormulaUpdate(string name, FormulaValue newValue)
        {
            if (PrintResult)
            {
                await this.WriteLineVarAsync(newValue, CancellationToken.None, $"{name}: ");
            }
        }

        private async Task WriteLineVarAsync(FormulaValue result, CancellationToken cancel, string prefix = null)
        {
            if (prefix == null)
            {
                prefix = string.Empty;
            }

            if (result == null)
            {
                await this.Output.WriteLineAsync(string.Empty, OutputKind.Repl, cancel);
                return;
            }

            var output = this.ValueFormatter.Format(result);
            var kind = (result is ErrorValue) ? OutputKind.Error : OutputKind.Repl;

            await this.Output.WriteLineAsync(prefix + output, kind, cancel);
        }
    }

    /// <summary>
    /// Result from <see cref="PowerFxREPL.HandleCommandAsync(string, bool, uint?, CancellationToken)"/>.
    /// </summary>
    public class ReplResult
    {
        /// <summary>
        /// Check Result, after ApplyErrors() is called. Could have be failures.
        /// Could be null on a nop. 
        /// </summary>
        public CheckResult CheckResult { get; set; }

        public bool IsSuccess => this.CheckResult == null ? true : this.CheckResult.IsSuccess;

        /// <summary>
        /// Result after evaluation. Null if didn't eval. 
        /// </summary>
        public FormulaValue EvalResult { get; set; }

        /// <summary>
        /// Updates to variables via Set(). 
        /// key is the name, value is the final value at the end of the expression. 
        /// </summary>
        public IDictionary<string, FormulaValue> DeclaredVars { get; set; } = new Dictionary<string, FormulaValue>();

        /// <summary>
        /// Intellisense results, if any.
        /// </summary>
        public IIntellisenseResult IntellisenseResult { get; set; }
    }
}
