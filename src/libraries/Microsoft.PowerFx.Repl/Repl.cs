// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Repl;
using Microsoft.PowerFx.Repl.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Microsoft.PowerFx
{
    // Default repl. 
    public class PowerFxRepl : PowerFxReplBase
    {
        public PowerFxRepl()
        {
            var config = new PowerFxConfig();
            config.SymbolTable.EnableMutationFunctions();

#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableRegExFunctions();
#pragma warning restore CS0618 // Type or member is obsolete

            // config.EnableSetFunction();
            this.Engine = new RecalcEngine(config);
        }
    }

    // Base REPL class
    public class PowerFxReplBase
    {
        // $$$ Need some "Init" function 
        public RecalcEngine Engine { get; set; }

        public ValueFormatter ValueFormatter { get; set; } = new ValueFormatter();

        // $$$ Print logo header
        public IReplOutput Output { get; set; } = new ConsoleWriter();

        // Allow repl to create new definitions, such as Set(). 
        public bool AllowSetDefinitions { get; set; }

        // Do we print each command?
        // Useful if we're running a file, or ir input UI is separated from output UI. 
        public bool Echo { get; set; } = false;

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

        // Interpreter should normally not throw.
        // Exceptions should be caught and converted to ErrorResult.
        // Not called for OperationCanceledException since those are expected.
        // This can rethrow
        public virtual async Task OnEvalExceptionAsync(Exception e, CancellationToken cancel)
        {
            var msg = e.ToString();

            await this.Output.WriteLineAsync(msg, OutputKind.Error, cancel);
        }

        public IDictionary<string, IPseudoFunction> _pseudoFunctions = new Dictionary<string, IPseudoFunction>();

        public void AddPseudoFunction(IPseudoFunction func)
        {
            _pseudoFunctions.Add(func.Name(), func);
        }

        public PowerFxReplBase()
        {
            // $$$ Make it easy for host to hook Notify()? 
            this.MetaFunctions.AddFunction(new NotifyFunction());
            this.MetaFunctions.AddFunction(new HelpFunction(this));
        }

        private bool _userEnabled = false;

        public void EnableUserObject()
        {
            var sampleUserInfo = new BasicUserInfo
            {
                FullName = "Susan Burk",
                Email = "susan@contoso.com",
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

        // Can we get direct Reader/Writer?
        // - writer can't do colorizing...
        // public async Task RunAsync(TextReader input, TextWriter output, CancellationToken cancel)        

        // $$$ Move to caller?
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

        // Accept a line of input.  This supports multi-line handling. 
        // Caller invokes this in a loop.  $$$ How to signal an exit? OperationCanceledException?
        public async Task HandleLineAsync(string line, CancellationToken cancel = default)
        {
            if (this.Engine == null)
            {
                throw new InvalidOperationException($"Engine is not set.");
            }

            string cmd = this.MultilineProcessor.HandleLine(line);

            if (cmd != null)
            {
                await this.HandleCommandAsync(cmd, cancel);
            }
        }

        /// <summary>
        /// Directly invoke a command. This skips multiline handling. 
        /// </summary>
        /// <param name="cmd">expression to run.</param>
        /// <param name="cancel">cancellation token.</param>
        /// <returns>status object with details.</returns>
        /// <exception cref="InvalidOperationException">invalid.</exception>
        public virtual async Task<ReplResult> HandleCommandAsync(string cmd, CancellationToken cancel = default)
        {
            if (this.Engine == null)
            {
                throw new InvalidOperationException($"Engine is not set.");
            }

            if (string.IsNullOrWhiteSpace(cmd))
            {
                return new ReplResult();
            }

            if (this.Echo)
            {
                await this.Output.WriteLineAsync(cmd, OutputKind.Repl, cancel);
            }

            var extraSymbolTable = this.ExtraSymbolValues?.SymbolTable;

            // pseudo functions and named formula assignments, handled outside of the interpreter
            // for our purposes, we don't need the engine's features or the parser options
            var parseResult = PowerFx.Engine.Parse(cmd);

            if (parseResult.IsSuccess)
            {
                if (parseResult.Root is CallNode cn && _pseudoFunctions.TryGetValue(cn.Head.Name, out var psuedoFunction))
                {
                    psuedoFunction.Execute(cn, this, extraSymbolTable, cancel);
                    return new ReplResult();
                }

                if (parseResult.Root is BinaryOpNode bo && bo.Op == BinaryOp.Equal && bo.Left.Kind == NodeKind.FirstName)
                {
                    Engine.SetFormula(bo.Left.ToString(), bo.Right.ToString(), OnFormulaUpdate);
                    return new ReplResult();
                }
            }
            
            var runtimeConfig = new RuntimeConfig(this.ExtraSymbolValues)
            {
                 ServiceProvider = new BasicServiceProvider(this.InnerServices)
            };

            if (this.UserInfo != null)
            {
                if (!_userEnabled)
                {
                    throw new InvalidOperationException($"Must call {nameof(EnableUserObject)} before setting {nameof(UserInfo)}.");
                }

                runtimeConfig.SetUserInfo(this.UserInfo);
            }

            runtimeConfig.AddService<IReplOutput>(this.Output);

            var currentSymbolTable = ReadOnlySymbolTable.Compose(this.MetaFunctions, extraSymbolTable);

            var check = new CheckResult(this.Engine)
                .SetText(cmd, ParserOptions)
                .SetBindingInfo(currentSymbolTable);

            check.ApplyParse();

            HashSet<string> varsToDisplay = new HashSet<string>();

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
                        var rhsExpr = declare._rhs.GetCompleteSpan().GetFragment(cmd);

                        var setCheck = this.Engine.Check(rhsExpr, ParserOptions, this.ExtraSymbolValues?.SymbolTable);
                        if (!setCheck.IsSuccess)
                        {
                            await this.Output.WriteLineAsync($"Failed to initialize '{name}'.", OutputKind.Error, cancel)
                                .ConfigureAwait(false);
                            return new ReplResult { CheckResult = setCheck };
                        }

                        // Start as blank. Will execute expression below to actually assign. 
                        var setValue = FormulaValue.NewBlank(setCheck.ReturnType);

                        // $$$ separate symbol table?
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
                        .SetText(cmd, new ParserOptions { AllowsSideEffects = true })
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
                    await this.Output.WriteLineAsync(error.ToString(), kind, cancel)
                        .ConfigureAwait(false);
                }

                return new ReplResult { CheckResult = check };
            }

            // Now eval

            var runner = check.GetEvaluator();

            FormulaValue result;

            try
            {
                result = await runner.EvalAsync(cancel, runtimeConfig)
                    .ConfigureAwait(false);
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

            foreach (var varName in varsToDisplay)
            {
                var varValue = this.Engine.GetValue(varName);
                replResult.DeclaredVars[varName] = varValue;

                await this.WriteLineVarAsync(varValue, cancel, $"{varName}: ");
            }

            // Now print the result
            await this.WriteLineVarAsync(result, cancel);

            return replResult;
        }

        public virtual async void OnFormulaUpdate(string name, FormulaValue newValue)
        {
            await this.WriteLineVarAsync(newValue, CancellationToken.None, $"{name}: ");
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
    /// Result from <see cref="PowerFxReplBase.HandleCommandAsync(string, CancellationToken)"/>.
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
    }
}
