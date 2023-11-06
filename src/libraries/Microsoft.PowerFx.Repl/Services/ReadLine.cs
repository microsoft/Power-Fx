// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl.Services
{
    public class ReadLine
    {
        public bool Prompt { get; set; }

        public bool NoLineFeedInPrompt { get; set; }

        public bool IntellisenseEnabled { get; set; }

        public IList<string> Expressions { get; set; }

        public PowerFxREPL PowerFxREPL { get; set; }

        public ReadLine(PowerFxREPL repl, bool prompt)
        {
            Expressions = new List<string>();
            PowerFxREPL = repl;
            Prompt = prompt;            
        }

        public void SetOptions(bool noLineFeedInPrompt, bool intellisenseEnabled)
        {
            NoLineFeedInPrompt = noLineFeedInPrompt;
            IntellisenseEnabled = intellisenseEnabled;
        }

        public string GetLine()
        {
            if (Prompt)
            {
                PowerFxREPL.WritePromptAsync(NoLineFeedInPrompt).Wait();
            }

            if (!IntellisenseEnabled)
            {
                return Console.ReadLine();
            }

            string line = string.Empty;

            // intellisense context
            int iStart = -1;
            int iEnd = -1;
            int tabCount = 0;
            string tabLine = null;
            List<IIntellisenseSuggestion> list = null;

            // history context
            int index = 0;

            while (true)
            {
                ConsoleKeyInfo cki = Console.ReadKey(true);
                ClearIntellisense(ref iStart, iEnd);

                // SHIFT-ESC = clear line
                if (cki.Key == ConsoleKey.Escape && (cki.Modifiers & ConsoleModifiers.Shift) != 0)
                {
                    ClearIntellisense(ref iStart, iEnd);
                    line = string.Empty;
                    DisplayCurrentLine(PowerFxREPL, line);
                    tabCount = 0;
                    index = 0;
                    continue;
                }

                // TAB or ESC = intellisense
                if ((cki.Key == ConsoleKey.Tab && tabCount == 0) || cki.Key == ConsoleKey.Escape)
                {
                    ReplResult rr = PowerFxREPL.HandleLineAsync(line, suggest: true, noLineFeedInPrompt: NoLineFeedInPrompt).Result;

                    if (rr?.IntellisenseResult != null && rr.IntellisenseResult.Suggestions != null && rr.IntellisenseResult.Suggestions.Any())
                    {
                        tabLine = line;
                        list = rr.IntellisenseResult.Suggestions.OrderBy(s => s.DisplayText.HighlightStart.ToString("0000", CultureInfo.InvariantCulture) + s.DisplayText.Text, StringComparer.OrdinalIgnoreCase).ToList();
                    }
                    else
                    {
                        list = null;
                    }
                }

                if (cki.Key == ConsoleKey.Tab || cki.Key == ConsoleKey.Escape)
                {
                    // TAB = complete with 1st suggestion (and next ones on subsequent TABs)
                    if (cki.Key == ConsoleKey.Tab && list != null)
                    {
                        IIntellisenseSuggestion suggestion = list.Skip(tabCount % list.Count).First();
                        int hs = suggestion.DisplayText.HighlightStart;
                        int he = suggestion.DisplayText.HighlightEnd;
                        line = tabLine.Substring(0, tabLine.Length - he + hs) + suggestion.DisplayText.Text;
                        DisplayCurrentLine(PowerFxREPL, line);
                        tabCount++;
                    }

                    // ESC = show suggestions we have found
                    if (cki.Key == ConsoleKey.Escape && list != null)
                    {
                        ClearIntellisense(ref iStart, iEnd);

                        int cl = Console.CursorLeft;
                        int ct = Console.CursorTop;

                        iStart = ct + 1;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        bool lastLine = Console.CursorTop == Console.BufferHeight - 1;

                        if (!lastLine)
                        {
                            foreach (var suggestion in list)
                            {
                                Console.WriteLine();

                                lastLine = Console.CursorTop == Console.BufferHeight - 1;
                                int n = list.Count() - Console.BufferHeight + ct + 1;
                                Console.Write(suggestion.DisplayText.Text + (lastLine && n > 0 ? $" (... {n} more)" : string.Empty));

                                if (lastLine)
                                {
                                    break;
                                }
                            }
                        }

                        iEnd = Console.CursorTop;
                        Console.ResetColor();
                        Console.SetCursorPosition(cl, ct);
                    }

                    index = 0;
                    continue;
                }

                tabCount = 0;

                // UP and DOWN, search in history
                if (cki.Key == ConsoleKey.UpArrow || cki.Key == ConsoleKey.DownArrow)
                {
                    if (Expressions.Count > 0)
                    {
                        // Keep an empty expression at the end of the list so that we can add new expressions
                        if (Expressions.Last() != string.Empty)
                        {
                            Expressions.Add(string.Empty);
                        }

                        if (cki.Key == ConsoleKey.UpArrow)
                        {
                            index++;
                        }

                        if (cki.Key == ConsoleKey.DownArrow)
                        {
                            index--;
                        }

                        if (index < 0)
                        {
                            index = Expressions.Count - 1;
                        }

                        List<string> e = new List<string>(Expressions);
                        e.Reverse();
                        line = e[index % e.Count];
                        DisplayCurrentLine(PowerFxREPL, line);
                    }

                    continue;
                }

                index = 0;

                // CTRL-Z = Exit
                if ((cki.Modifiers & ConsoleModifiers.Control) != 0 && cki.Key == ConsoleKey.Z)
                {
                    Console.WriteLine("Exiting...");
                    return null;
                }

                // backspace
                if (cki.KeyChar == 8)
                {
                    if (line.Length > 0)
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(' ');
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        line = line.Substring(0, line.Length - 1);
                    }

                    continue;
                }

                // CR / LF = end of line
                if (cki.KeyChar == 13 || cki.KeyChar == 10)
                {
                    Console.WriteLine();
                    break;
                }

                Console.Write(cki.KeyChar);
                line += cki.KeyChar;
            }

            PostLineProcessing(line);

            return line;
        }

        public void PostLineProcessing(string line)
        {
            if (Expressions.Any() && Expressions.Last() == string.Empty)
            {
                Expressions.RemoveAt(Expressions.Count - 1);
            }

            if (Expressions.Count >= 19)
            {
                Expressions.RemoveAt(0);
            }

            Expressions.Add(line);
        }

        private void DisplayCurrentLine(PowerFxREPL repl, string line)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - (NoLineFeedInPrompt ? 0 : 1));
            repl.WritePromptAsync(NoLineFeedInPrompt).Wait();
            Console.Write(line);
        }

        private static void ClearIntellisense(ref int iStart, int iEnd)
        {
            if (iStart == -1)
            {
                return;
            }

            int cl = Console.CursorLeft;
            int ct = Console.CursorTop;

            for (int i = iStart; i <= Math.Min(iEnd, Console.BufferHeight - 1); i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.BufferWidth));
            }

            Console.SetCursorPosition(cl, ct);
            iStart = -1;
        }
    }
}
