// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        public RecalcEngine Engine { get; set; }

        public ValueFormatter ValueFormatter { get; set; } = new ValueFormatter();

        // $$$ Print logo header
        public IReplOutput Output { get; set; } = new ConsoleWriter();

        // Allow repl to create new definitions, such as Set(). 
        public bool AllowSetDefinitions { get; set; }

        // Metata function "IR" that dumps the IR. 
        public bool AllowIRFunction { get; set; } = true;

        // Optional. If set, then 
        public UserInfo UserInfo { get; set; }

        // Policy for handling multiple lines. 
        public MultilineProcessor MultilineProcessor { get; set; } = new MultilineProcessor();

        // example override, switching to [1], [2] etc.
        public virtual string Prompt => ">> ";        

        public PowerFxReplBase()
        {
            // $$$ Make it easy for host to hook Notify? 
            this.MetaFunctions.AddFunction(new NotifyFunction());
            this.MetaFunctions.AddFunction(new HelpFunction(this));
        }

        // $$$ Should we just put on engine? 
        // useful to keep separate if engine is passed in.
        public SymbolTable MetaFunctions { get; } = new SymbolTable { DebugName = "Repl Functions" };

        // Can we get direct Reader/Writer?
        // - writer can't do colorizing...
        // public async Task RunAsync(TextReader input, TextWriter output, CancellationToken cancel)        

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
                prompt = "  "; // standard indent
            }

            // Start of new command 
            await this.Output.WriteAsync(prompt, OutputKind.Control, cancel);
        }

        // Accept a line of input.  This supports multi-line handling. 
        // Caller invokes this in a loop.  $$$ How to signal an exit?
        public async Task HandleLineAsync(string line, CancellationToken cancel = default)
        {
            string cmd = this.MultilineProcessor.HandleLine(line);

            if (cmd != null)
            {
                await this.HandleCommand(cmd, cancel);
            }            
        }

        // $$$ pre-post hook? Capture result. 
        // Handle a command - does not allow multi-line processing.
        public async Task HandleCommand(string line, CancellationToken cancel = default)
        {
            var parserOpts = new ParserOptions { AllowsSideEffects = true };

            if (this.AllowIRFunction)
            {
                var match = Regex.Match(line, @"^\s*IR\((?<expr>.*)\)\s*$", RegexOptions.Singleline);
                if (match.Success)
                {
                    var inner = match.Groups["expr"].Value;
                    var cr = this.Engine.Check(inner, options: parserOpts);
                    var irText = cr.PrintIR();
                    await this.Output.WriteLineAsync(irText, OutputKind.Repl, cancel)
                        .ConfigureAwait(false);
                    return;
                }
            }

            var runtimeConfig = new RuntimeConfig();

            if (this.UserInfo != null)
            {
                runtimeConfig.SetUserInfo(this.UserInfo);
            }

            runtimeConfig.AddService<IReplOutput>(this.Output);

            var check = new CheckResult(this.Engine)
                .SetText(line, parserOpts)
                .SetBindingInfo(this.MetaFunctions);

            check.ApplyParse();

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
                    if (!Engine.TryGetVariableType(name, out var existingType))
                    {
                        // Deosn't exist yet!

                        // Get the type. 
                        var rhsExpr = declare._rhs.GetCompleteSpan().GetFragment(line);

                        // $$$ better eval, handle errors?
                        var setValue = this.Engine.Eval(rhsExpr);

                        // $$$ separate symbol table?
                        this.Engine.UpdateVariable(name, setValue);

                        var output2 = this.ValueFormatter.Format(setValue);

                        await this.Output.WriteLineAsync($"{name}: {output2}", OutputKind.Repl, cancel)
                            .ConfigureAwait(false);

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
                        .SetText(line, new ParserOptions { AllowsSideEffects = true })
                        .SetBindingInfo(this.MetaFunctions);

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

                return;
            }

            // Now eval

            var runner = check.GetEvaluator();
            var result = await runner.EvalAsync(cancel, runtimeConfig)
                .ConfigureAwait(false);
                       
            // Now print the result
            var output = this.ValueFormatter.Format(result);

            await this.Output.WriteLineAsync(output, OutputKind.Repl, cancel);
        }
    }

    // $$$ delete
    public class ReplResult
    {
        // middle of a multiline operation. 
        public bool NeedMoreInput { get; set; }
    }
}
