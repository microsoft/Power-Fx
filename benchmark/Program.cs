// See https://aka.ms/new-console-template for more information
using PowerFXBenchmark;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

Console.WriteLine("Hello, World!");

BenchmarkRunner.Run<Benchmark>();

//var bench = new Benchmark();
//// read
//var result = await bench.Evaluation_UntypedInput_ParseJSON().ConfigureAwait(false);
//Console.WriteLine(result);
//result = await bench.Evaluation_UntypedInput_CustomUntypedObject().ConfigureAwait(false);
//Console.WriteLine(result);
//result = await bench.Evaluation_StronglyTypedInput().ConfigureAwait(false);
//Console.WriteLine(result);
//result = await bench.Evaluation_Expression_Complexity1().ConfigureAwait(false);
//Console.WriteLine(result);
//result = await bench.Evaluation_Expression_Complexity2().ConfigureAwait(false);
//Console.WriteLine(result);
