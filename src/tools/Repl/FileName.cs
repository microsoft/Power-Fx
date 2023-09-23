// <copyright file="FileName.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CA1303

namespace Microsoft.PowerFx
{
    public static class ConsoleRepl2
    {
        public static void Main()
        {
            MainAsync().Wait();
        }

        public static async Task MainAsync()
        {
            var repl = new PowerFxRepl
            {
                AllowSetDefinitions = true
            };

            while (true)
            {
                await repl.WritePromptAsync();
                var line = Console.ReadLine();

                try
                {
                    await repl.HandleLineAsync(line);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"...exiting");
                    return;
                }
            }
        }
    }
}
