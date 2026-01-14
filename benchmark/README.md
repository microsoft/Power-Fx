## Benchmark

The benchmark is performed using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Scenario

In this application, we assume an Power FX expression operates on two inputs/variables -- `testObj` and `json`.

#### `testObj`
`testObj` is a strongly typed object, with its schema previously defined separately in `TestObjSchema`. 

The application can derive the `RecordType` for `testObj` by visiting the `TestObjSchema` prior to hot path.

#### `json`
`json` is a schema-less, free-form data formated in JSON.

Given the lack of schema for `json`, the application will only derive the `RecordType` for `json` when the `json` content is available to the system during hot path.

### Test Summary
The table below summarizes each test method defined in [`Benchmark.cs`](./Benchmark.cs), along with the test result.

| Method | Mean  | Explanation | Hot Path? |
| --- | --- | --- | --- |
| `Convert_TestObjSchema_To_PowerFX_RecordType`| 4 us | Create a RecordType for `testObj` by visiting the `TestObjSchema`. | No |
| `Convert_JsonElement_To_PowerFX_RecordValue`| 10 us | Create RecordValue for `json` by `FromJson()`. | Yes |
| `Convert_TestObj_To_PowerFX_RecordValue` | 64 ns | Create a custom `RecordValue` that wraps around the raw `TestObject`. | No |
| `Parse` | 40 us | `Parse()` the expression. | No |
| `TypeCheck` | 30 us | `Check()` the expression with `SymbolTable` that contains the `RecordType`s of `testObj` and `json`. | Yes |
| `EvaluateAsync` | 4 us | `EvaluateAsync()` the expression with `SymbolValues`. | Yes |
