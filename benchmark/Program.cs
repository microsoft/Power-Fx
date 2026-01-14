// See https://aka.ms/new-console-template for more information
using PowerFXBenchmark;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

Console.WriteLine("Hello, World!");

BenchmarkRunner.Run<Benchmark>();

//////var bench = new Benchmark();
//////// read
//////var result = await bench.EvaluateAsync().ConfigureAwait(false);
//////Console.WriteLine(result);
