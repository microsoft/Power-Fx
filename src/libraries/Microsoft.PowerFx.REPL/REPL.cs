// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.REPL
{
    public class RecalcEngineREPL
    {
        public readonly RecalcEngine Engine;

        public virtual BasicUserInfo UserInfo => new BasicUserInfo
        {
            FullName = "Susan Burk",
            Email = "susan@contoso.com",
            DataverseUserId = new Guid("88888888-044f-4928-a95f-30d4c8ebf118"),
            TeamsMemberId = "29:1DUjC5z4ttsBQa0fX2O7B0IDu30R",
            EntraObjectId = new Guid("99999999-044f-4928-a95f-30d4c8ebf118"),
        };

        public virtual bool StackTrace => false;

        public virtual string Prompt => "\n> ";

        public readonly bool _outputConsole;

        public virtual ParserOptions GetParserOptions()
        {
            return new ParserOptions() { AllowsSideEffects = true };
        }

        public virtual ReadOnlySymbolTable GetSymbolTable()
        {
            return Engine.EngineSymbols;
        }

        public virtual RuntimeConfig GetRuntimeConfig()
        {
            var rc = new RuntimeConfig();
            rc.SetUserInfo(UserInfo);
            return rc;
        }

        public virtual FormulaValue? Eval(string expr, TextWriter? output, bool echo)
        {
            Match match;

            // named formula definition: <ident> = <formula>
            if ((match = Regex.Match(expr, @"^\s*(?<ident>(\w+|'([^']|'')+'))\s*=(?<formula>.*)$", RegexOptions.Singleline)).Success &&
                !Regex.IsMatch(match.Groups["ident"].Value, "^\\d") &&
                match.Groups["ident"].Value != "true" && match.Groups["ident"].Value != "false" && match.Groups["ident"].Value != "blank")
            {
                var ident = match.Groups["ident"].Value;
                if (ident.StartsWith("\'", StringComparison.InvariantCulture))
                {
                    ident = ident.Substring(1, ident.Length - 2).Replace("''", "'");
                }

                Engine.SetFormula(ident, match.Groups["formula"].Value, OnUpdate);

                return null;
            }
            else
            {
                CheckResult checkResult = Engine.Check(expr, GetParserOptions(), GetSymbolTable());
                checkResult.ThrowOnErrors();

                IExpressionEvaluator evaluator = checkResult.GetEvaluator();
                return evaluator.Eval(GetRuntimeConfig());
            }
        }

        public RecalcEngineREPL(PowerFxConfig config, bool outputConsole)
        {
            Engine = new RecalcEngine(config);

            var props = new Dictionary<string, object>
            {
                { "FullName", UserInfo.FullName },
                { "Email", UserInfo.Email },
                { "DataverseUserId", UserInfo.DataverseUserId },
                { "TeamsMemberId", UserInfo.TeamsMemberId }
            };
            var allKeys = props.Keys.ToArray();
            config.SymbolTable.AddUserInfoObject(allKeys);

            _outputConsole = outputConsole;
        }

        public void REPL(TextReader input, TextWriter? output, bool echo = false)
        {
            string? expr;

            // main loop
            while ((expr = ReadFormula(input, output, echo)) != null)
            {
                try
                {
                    var result = Eval(expr, output, echo);

                    if (result != null)
                    {
                        if (_outputConsole)
                        {
                            if (result is ErrorValue errorValue1)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error: {errorValue1.Errors[0].Kind} - {errorValue1.Errors[0].Message}");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.WriteLine(PrintResult(result));
                            }
                        }

                        if (output != null)
                        {
                            if (result is ErrorValue errorValue2)
                            {
                                output.WriteLine($"Error: {errorValue2.Errors[0].Kind} - {errorValue2.Errors[0].Message}");
                            }
                            else
                            {
                                output.WriteLine(PrintResult(result));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_outputConsole)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.Message);

                        if (StackTrace)
                        {
                            Console.WriteLine(e.ToString());
                        }

                        Console.ResetColor();
                    }

                    output?.WriteLine(Regex.Replace(e.InnerException.Message, "\r\n", "|") + "\n");
                }
            }
        }

        public virtual void OnUpdate(string name, FormulaValue newValue)
        {
            if (_outputConsole)
            { 
                Console.Write($"{name}: ");
                if (newValue is ErrorValue errorValue)
                {
                    Console.WriteLine("Error: " + errorValue.Errors[0].Message);
                }
                else
                {
                    if (newValue is TableValue)
                    {
                        Console.WriteLine();
                    }

                    Console.WriteLine(PrintResult(newValue));
                }
            }
        }

        public virtual string PrintResult(FormulaValue value, bool minimal = false)
        {
            return value.ToExpression();
        }

        private string? ReadFormula(TextReader input, TextWriter? output, bool echo = false)
        {
            string? exprPartial;
            int usefulCount;

            // read
            do
            {
                string exprOne;
                int parenCount;

                exprPartial = null;

                do
                {
                    bool doubleQuote, singleQuote;
                    bool lineComment, blockComment;
                    char last;

                    if (exprPartial == null && !echo)
                    {
                        Console.Write(Prompt);
                    }

                    exprOne = input.ReadLine();

                    if (exprOne == null)
                    {
                        Console.Write("\n");
                        return exprPartial;
                    }

                    exprPartial += exprOne + "\n";

                    // determines if the parens, curly braces, and square brackets are closed
                    // taking into escaping, block, and line comments
                    // and continues reading lines if they are not, with a blank link terminating
                    parenCount = 0;
                    doubleQuote = singleQuote = lineComment = blockComment = false;
                    last = '\0';
                    usefulCount = 0;
                    foreach (var c in exprPartial)
                    {
                        // don't need to worry about escaping as it looks like two 
                        if (c == '"' && !singleQuote)
                        {
                            doubleQuote = !doubleQuote; // strings that are back to back
                        }

                        if (c == '\'' && !doubleQuote)
                        {
                            singleQuote = !singleQuote;
                        }

                        if (c == '*' && last == '/' && !blockComment)
                        {
                            blockComment = true;
                            usefulCount--;                         // compensates for the last character already being added
                        }

                        if (c == '/' && last == '*' && blockComment)
                        {
                            blockComment = false;
                            usefulCount--;
                        }

                        if (!doubleQuote && !singleQuote && !blockComment && !lineComment && c == '/' && last == '/')
                        {
                            lineComment = true;
                            usefulCount--;
                        }

                        if (c == '\n')
                        {
                            lineComment = false;
                        }

                        if (!lineComment && !blockComment && !doubleQuote && !singleQuote)
                        {
                            if (c == '(' || c == '{' || c == '[')
                            {
                                parenCount++;
                            }

                            if (c == ')' || c == '}' || c == ']')
                            {
                                parenCount--;
                            }
                        }

                        if (!char.IsWhiteSpace(c) && !lineComment && !blockComment)
                        {
                            usefulCount++;
                        }

                        last = c;
                    }
                }
                while (!Regex.IsMatch(exprOne, "^\\s*$") && (parenCount != 0 || Regex.IsMatch(exprOne, "(=|=\\>)\\s*$")));

                if (echo)
                {
                    // skip >> for comments and setup
                    if (Regex.IsMatch(exprPartial, "^\\s*//") || Regex.IsMatch(exprPartial, "^#SETUP"))
                    {
                        Console.Write(exprPartial);
                        output?.Write(exprPartial + "\n");
                        usefulCount = 0;
                    }
                    else if (!Regex.IsMatch(exprPartial, "^\\s*$"))
                    {
                        Console.Write("\n>> " + exprPartial);
                        output?.Write(">> " + exprPartial);
                    }
                }
            }
            while (usefulCount == 0);

            return exprPartial;
        }
    }
}
