// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol;

namespace Microsoft.PowerFx
{
    public static class Lsp
    {
        public static void Main()
        {
            const string logFile = "c:\\temp\\lsplog.txt";

            //       var rpc = new JsonRpc(Console.OpenStandardOutput(), Console.OpenStandardInput());
            // rpc.StartListening();

            ConcurrentBag<Exception> exList = new ConcurrentBag<Exception>();

            TextWriter outputWriter = new StreamWriter(logFile);

            var config = new PowerFxConfig();

            var engine = new Engine(config);

            var scopeFactory = new PowerFxScopeFactory(
                (string documentUri) => engine.CreateEditorScope());

            var sendToClientData = new List<string>();

#nullable enable
            string? input;
#nullable disable

            var lsp = new LanguageServer(sendToClientData.Add, scopeFactory);
            lsp.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            int contentLength = 0;

            Debugger.Launch();

            while ((input = Console.ReadLine()) != null)
            {
                Match match;

                outputWriter.WriteLine(">> READ: " + input);
                outputWriter.Flush();

                if ((match = Regex.Match(input, @"^\s*Content-Length:\s*(?<len>\d+)\s*$", RegexOptions.Singleline)).Success &&
                    int.TryParse(match.Groups["len"].Value, out contentLength))
                {
                    outputWriter.WriteLine(">> LENGTH: " + input + ":" + contentLength.ToString(CultureInfo.InvariantCulture));
                    outputWriter.Flush();
                }
                else if (input == string.Empty && contentLength > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < contentLength; i++)
                    {
                        sb.Append(Convert.ToChar(Console.Read()));
                    }

                    outputWriter.WriteLine(">> BLOCK: " + sb.ToString());
                    outputWriter.Flush();

                    lsp.OnDataReceived(sb.ToString());
                    foreach (var e in exList)
                    {
                        outputWriter.WriteLine(">> EXCEPTION: " + e.Message);
                    }

                    exList.Clear();
                    foreach (var x in sendToClientData)
                    {
                        outputWriter.WriteLine(">> REPLY: " + x);
                        Console.WriteLine(x);
                    }

                    outputWriter.Flush();
                    sendToClientData.Clear();
                }
                else
                {
                    outputWriter.WriteLine(">> ILLELGAL: " + input);
                    outputWriter.Flush();
                }
            }

            outputWriter.WriteLine(">> CLOSED");
            outputWriter.Flush();

            outputWriter.Close();
        }
    }

    public class PowerFxScopeFactory : IPowerFxScopeFactory
    {
        public delegate IPowerFxScope GetOrCreateInstanceDelegate(string documentUri);

        private readonly GetOrCreateInstanceDelegate _getOrCreateInstance;

        public PowerFxScopeFactory(GetOrCreateInstanceDelegate getOrCreateInstance)
        {
            _getOrCreateInstance = getOrCreateInstance;
        }

        public IPowerFxScope GetOrCreateInstance(string documentUri)
        {
            return _getOrCreateInstance(documentUri);
        }
    }
}
