# Microsoft.PowerFx.Connectors

`Microsoft.PowerFx.Connectors` bridges [Power Fx](https://learn.microsoft.com/power-platform/power-fx/) and REST APIs described by an [OpenAPI/Swagger](https://swagger.io/specification/) document. It converts each swagger operation (and CDP dataset/table) into first-class Power Fx symbols so authors can write formulas like:

```powerfx
MyConnector.SendEmail({ to: "a@b.com", subject: "Hi" })
First(Customers).Name
```

At a high level the library does three things:

1. **Parse** an `OpenApiDocument` and generate `ConnectorFunction` / `TexlFunction` objects (action connectors) or `CdpTable` objects (tabular connectors).
2. **Bind** those functions/tables into a `PowerFxConfig` or a `SymbolValues` bag so the Power Fx engine can resolve them at check time.
3. **Execute** them at runtime by translating Power Fx `FormulaValue` arguments into an `HttpRequestMessage`, sending it via an `HttpMessageInvoker`, and parsing the HTTP response back into a `FormulaValue`.

The two main flavors of connector this library supports are covered in detail below.

---

## Table of Contents

- [High level architecture](#high-level-architecture)
- [Action Connectors](#action-connectors)
  - [How an action connector is registered](#how-an-action-connector-is-registered)
  - [Runtime invocation pipeline](#runtime-invocation-pipeline)
  - [FormulaValue serialization (request body)](#formulavalue-serialization-request-body)
  - [FormulaValue deserialization (response)](#formulavalue-deserialization-response)
  - [End-to-end example](#end-to-end-example-action-connector)
- [Tabular Connectors (CDP)](#tabular-connectors-cdp)
  - [Endpoint sequence](#endpoint-sequence)
  - [Metadata → FormulaType conversion](#metadata--formulatype-conversion)
  - [Row deserialization](#row-deserialization)
  - [End-to-end example](#end-to-end-example-tabular-connector)
- [Shared infrastructure](#shared-infrastructure)

---

## High level architecture

```
                +------------------------------------+
                |         OpenApiDocument             |
                +------------------+-------------------+
                                    |
             +----------------------+----------------------+
             |                                             |
    ACTION CONNECTOR                             TABULAR CONNECTOR (CDP)
             |                                             |
             v                                             v
    OpenApiParser.GetFunctions                  CdpDataSource.GetTablesAsync
             |                                             |
             v                                             v
    ConnectorFunction (+ ConnectorTexlFunction)     CdpTable.InitAsync
             |                                             |
             v                                             v
    PowerFxConfig.AddActionConnector           CdpTable.GetTableValue -> CdpTableValue
             |                                             |
             v                                             v
    Engine.EvalAsync -> HttpFunctionInvoker    Engine.EvalAsync -> CdpService.GetItemsAsync
             |                                             |
             v                                             v
                       HTTP call (HttpMessageInvoker / PowerPlatformConnectorClient)
```

Two extension points wire the two worlds together:

- `PowerFxConfig.AddActionConnector(...)` — [`ConfigExtensions.AddActionConnector`](Environment/PowerFxConfigExtensions.cs) registers the generated functions with the config so they show up in `Engine.Check`.
- `RuntimeConfig.AddRuntimeContext(...)` — [`RuntimeConfigExtensions.AddRuntimeContext`](Environment/RuntimeConfigExtensions.cs) supplies the `BaseRuntimeConnectorContext` (which owns the `HttpMessageInvoker`, `TimeZoneInfo`, logger, etc.) at eval time.

The `BaseRuntimeConnectorContext` abstract class ([`BaseRuntimeConnectorContext`](Public/BaseRuntimeConnectorContext.cs)) is the seam between Power Fx execution and HTTP:

```csharp
public abstract class BaseRuntimeConnectorContext
{
    public abstract HttpMessageInvoker GetInvoker(string @namespace);
    public abstract TimeZoneInfo TimeZoneInfo { get; }
    public virtual ConnectorLogger ExecutionLogger { get; } = null;
    ...
}
```

The invoker returned here is typically a [`PowerPlatformConnectorClient`](PowerPlatformConnectorClient.cs) (obsolete but still in use) or [`PowerPlatformConnectorClient2`](PowerPlatformConnectorClient2.cs) which knows how to translate a swagger-relative request into the APIM `/invoke` protocol used by Power Platform (adding `x-ms-request-method`, `x-ms-request-url`, `Authorization`, `x-ms-client-environment-id`, etc. — see [`PowerPlatformConnectorClient.Transform`](PowerPlatformConnectorClient.cs)).

---

## Action Connectors

An **action connector** exposes every OpenAPI operation as a Power Fx function. Calling the function in a formula translates to an HTTP request and its JSON/text/form response is converted back to a `FormulaValue`.

Key types:

| Type | Purpose |
| --- | --- |
| [`OpenApiParser`](OpenApiParser.cs) | Walks the swagger and produces `ConnectorFunction` objects. |
| [`ConnectorFunction`](ConnectorFunction.cs) | Public wrapper over an `OpenApiOperation`. Exposes `Name`, `RequiredParameters`, `OptionalParameters`, `ReturnType`, `InvokeAsync(...)`. |
| [`ConnectorTexlFunction`](Texl/ConnectorTexlFunction.cs) | Adapter that lets a `ConnectorFunction` participate in Power Fx binding/eval as a `TexlFunction` implementing `IFunctionInvoker`. |
| [`HttpFunctionInvoker`](Execution/HttpFunctionInvoker.cs) | Builds `HttpRequestMessage` from `FormulaValue` args, sends it, and decodes the response. |
| [`FormulaValueSerializer`](Execution/FormulaValueSerializer.cs) and subclasses (`OpenApiJsonSerializer`, `OpenApiFormUrlEncoder`, `OpenApiTextSerializer`, `OpenApiMultipart`) | Content-type–specific serializers that turn `FormulaValue` into the HTTP body. |

### How an action connector is registered

The public entry point is `PowerFxConfig.AddActionConnector`, defined in [`ConfigExtensions.AddActionConnector`](Environment/PowerFxConfigExtensions.cs):

```csharp
public static IReadOnlyList<ConnectorFunction> AddActionConnector(
    this PowerFxConfig config,
    ConnectorSettings connectorSettings,
    OpenApiDocument openApiDocument,
    ConnectorLogger configurationLogger = null)
```

Internally it:

1. Calls [`OpenApiParser.ParseInternal`](OpenApiParser.cs) which produces two parallel lists:
   - `List<ConnectorFunction>` — the public/discoverable functions.
   - `List<ConnectorTexlFunction>` — the `TexlFunction` wrappers.
2. Adds each `ConnectorTexlFunction` to the config via `config.AddFunction(function)` so binder/intellisense can see them ([`ConfigExtensions.AddActionConnectorInternal`](Environment/PowerFxConfigExtensions.cs)).

The `ConnectorTexlFunction` derives from `TexlFunction` and implements `IFunctionInvoker.InvokeAsync` — this is the hook the interpreter calls at runtime ([`ConnectorTexlFunction.InvokeAsync`](Texl/ConnectorTexlFunction.cs) lines 89-108):

```csharp
public async Task<FormulaValue> InvokeAsync(FunctionInvokeInfo invokeInfo, CancellationToken cancellationToken)
{
    var serviceProvider = invokeInfo.FunctionServices;
    BaseRuntimeConnectorContext runtimeContext =
        serviceProvider.GetService(typeof(BaseRuntimeConnectorContext)) as BaseRuntimeConnectorContext
        ?? throw new InvalidOperationException("RuntimeConnectorContext is missing from service provider");

    return await ConnectorFunction.InvokeInternalAsync(invokeInfo.Args, runtimeContext, cancellationToken)
                                  .ConfigureAwait(false);
}
```

Note the required indirection: the runtime context is fetched from the `IServiceProvider` that was seeded by `RuntimeConfig.AddRuntimeContext`.

### Runtime invocation pipeline

`ConnectorFunction.InvokeAsync` is also the public API when calling functions outside the engine (see [`ConnectorFunction.InvokeAsync`](ConnectorFunction.cs) line 819) and delegates to `InvokeInternalAsync` (line 854).

The pipeline is:

1. **Bind FormulaValues to parameters** — [`HttpFunctionInvoker.ConvertToNamedParameters`](Execution/HttpFunctionInvoker.cs) (line 256) turns positional `FormulaValue[]` into a `Dictionary<string, FormulaValue>`, flattening records for optional args and applying default/hidden values.
2. **Build the request** — [`HttpFunctionInvoker.BuildRequest`](Execution/HttpFunctionInvoker.cs) (line 44):
   - Iterates the swagger's `Operation.Parameters` and, based on `param.In` (`Path`, `Query`, `Header`, `Cookie`), places each formatted value in the right slot. Path values get URI-escaped via `Uri.EscapeDataString`, query values get appended to a `StringBuilder`, and headers go into a case-insensitive dictionary.
   - Formats primitive values via [`HttpFunctionInvoker.FormatParameterValue`](Execution/HttpFunctionInvoker.cs) (line 217) — which converts `DateTimeValue` to ISO 8601 UTC (`yyyy-MM-ddTHH:mm:ss.fffZ`) and `DateValue` to `yyyy-MM-dd`.
   - Builds the request body by calling [`HttpFunctionInvoker.GetBodyAsync`](Execution/HttpFunctionInvoker.cs) (line 398) which picks a `FormulaValueSerializer` based on the operation's `Content-Type`.
   - Concatenates `server + path + query`, then does a second pass to substitute `{connectionId}`–style placeholders using values from `GlobalContext.ConnectorValues` (line 149).
3. **Send** — [`HttpFunctionInvoker.ExecuteHttpRequest`](Execution/HttpFunctionInvoker.cs) (line 566) uses `_httpClient.SendAsync(request, cancellationToken)`.
4. **Decode** — [`HttpFunctionInvoker.DecodeResponseAsync`](Execution/HttpFunctionInvoker.cs) (line 449) inspects the status code and calls `FormulaValueJSON.FromJson` (see [`FormulaValueJSON.FromJson`](../Microsoft.PowerFx.Json/FormulaValueJSON.cs) line 45) with the function's known `ReturnType` so the JSON is materialized against the correct schema. On error it returns an `ErrorValue`/`HttpExpressionError`.

### FormulaValue serialization (request body)

Serialization is driven by an abstract [`FormulaValueSerializer`](Execution/FormulaValueSerializer.cs):

```csharp
internal abstract class FormulaValueSerializer
{
    internal const string UtcDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    internal const string DateTimeFormat    = "yyyy-MM-ddTHH:mm:ss.fff";

    internal abstract void StartSerialization(string referenceId);
    internal abstract void EndSerialization();
    protected abstract void StartObject(string name = null);
    protected abstract void EndObject(string name = null);
    protected abstract void StartArray(string name = null);
    protected abstract void EndArray();
    protected abstract void WritePropertyName(string name);
    protected abstract void WriteNumberValue(double numberValue);
    protected abstract void WriteDecimalValue(decimal decimalValue);
    protected abstract void WriteStringValue(string stringValue);
    protected abstract void WriteBooleanValue(bool booleanValue);
    protected abstract void WriteDateTimeValue(DateTime dateTimeValue);
    protected abstract Task WriteBlobValueAsync(BlobValue blobValue);
    ...
}
```

Concrete implementations:

- [`OpenApiJsonSerializer`](Execution/OpenApiJsonSerializer.cs) — writes JSON via `Utf8JsonWriter`. Handles the "schema-less body" case (when the swagger declares the body itself as a primitive) by suppressing the outer `{ ... }` (see the `_schemaLessBody` / `_topPropertyWritten` logic in lines 39-160). `DateTimeValue` is written using `UtcDateTimeFormat`; `DateValue` as `o` format truncated to 10 characters; `BlobValue` is base64-encoded either as-is (if it already stores `Base64Blob`) or via `WriteBase64StringValue`.
- `OpenApiFormUrlEncoder` — `application/x-www-form-urlencoded`.
- `OpenApiTextSerializer` — `text/plain`.
- `OpenApiMultipart` — `multipart/form-data`.

Selection happens here ([`HttpFunctionInvoker.GetBodyAsync`](Execution/HttpFunctionInvoker.cs) lines 414-420):

```csharp
serializer = ct switch
{
    OpenApiExtensions.ContentType_XWwwFormUrlEncoded => new OpenApiFormUrlEncoder(...),
    OpenApiExtensions.ContentType_TextPlain          => new OpenApiTextSerializer(...),
    OpenApiExtensions.ContentType_Multipart          => new OpenApiMultipart(...),
    _                                                => new OpenApiJsonSerializer(...)
};
```

Traversal is schema-driven: [`FormulaValueSerializer.WriteObjectAsync`](Execution/FormulaValueSerializer.cs) (line 81) walks `schema.Properties` (not the record fields) so unknown/extra `FormulaValue` fields are dropped and missing required fields throw a `PowerFxConnectorException`.

### FormulaValue deserialization (response)

The response side is much simpler because it reuses the existing JSON→FormulaValue infrastructure from `Microsoft.PowerFx.Json`.

[`HttpFunctionInvoker.DecodeResponseAsync`](Execution/HttpFunctionInvoker.cs) (lines 503-520):

```csharp
if (statusCode < 300)
{
    bool returnUnknownRecordFieldAsUO =
        _function.ConnectorSettings.Compatibility.IncludeUntypedObjects() &&
        _function.ConnectorSettings.ReturnUnknownRecordFieldsAsUntypedObjects;

    var typeToUse = _function.ReturnType;
    if (returnTypeOverride != null) typeToUse = returnTypeOverride;

    return string.IsNullOrWhiteSpace(text)
        ? FormulaValue.NewBlank(typeToUse)
        : _returnRawResults
            ? FormulaValue.New(text)
            : FormulaValueJSON.FromJson(
                text,
                new FormulaValueJsonSerializerSettings { ReturnUnknownRecordFieldsAsUntypedObjects = returnUnknownRecordFieldAsUO },
                typeToUse);
}
```

- The **expected type** is `ConnectorFunction.ReturnType` — this is precomputed from the swagger response schema (see `Operation.GetReturnType` inside `ConnectorFunction`).
- `FormulaValueJSON.FromJson` will:
  - Try to match each JSON element against the target `FormulaType`.
  - Convert `"yyyy-MM-ddTHH:mm:ss..."` strings to `DateTimeValue` when the schema calls for it.
  - Return an `ErrorValue` with `ErrorKind.InvalidJSON` if parsing fails ([`FormulaValueJSON.FromJson`](../Microsoft.PowerFx.Json/FormulaValueJSON.cs) lines 45-72).
- If the return type is `BlobType`, the raw bytes are returned via `FormulaValue.NewBlob(bytes)` (lines 457-462) instead of going through JSON.
- On HTTP errors, `DecodeResponseAsync` returns a `FormulaValue.NewError(new HttpExpressionError(statusCode) { Kind = ErrorKind.Network, ... })` unless `throwOnError` is set.

### End-to-end example (action connector)

The pattern below is exercised end-to-end by [`BaseConnectorTest.GetElements`](../../tests/Microsoft.PowerFx.Connectors.Tests.Shared/BaseConnectorTest.cs) (line 76) and every subclass in `tests/Microsoft.PowerFx.Connectors.Tests.Shared`.

```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Readers;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Types;

public static class ActionConnectorSample
{
    public static async Task RunAsync()
    {
        // 1. Load the swagger.
        using var stream = File.OpenRead("MyConnector.swagger.json");
        var openApiDoc = new OpenApiStreamReader().Read(stream, out _);

        // 2. Create a PowerFxConfig and register the connector.
        var config = new PowerFxConfig();
        var settings = new ConnectorSettings("MyConnector")
        {
            IncludeInternalFunctions = false,
            AllowUnsupportedFunctions = false,
        };

        // OpenApiParser.GetFunctions is called under the hood by AddActionConnector.
        IReadOnlyList<ConnectorFunction> functions =
            config.AddActionConnector(settings, openApiDoc, new ConsoleLogger());

        // 3. Build an HttpMessageInvoker that will actually make the network call.
        //    In Power Platform hosts, this is typically PowerPlatformConnectorClient(2).
        using var httpClient = new HttpClient(); // or PowerPlatformConnectorClient(...)

        // 4. Create a RuntimeConfig with the connector runtime context. This is the
        //    seam through which HttpFunctionInvoker discovers the HttpMessageInvoker.
        var runtimeCtx = new MyConnectorRuntimeContext("MyConnector", httpClient);
        var runtimeConfig = new RuntimeConfig().AddRuntimeContext(runtimeCtx);

        // 5. Evaluate a Power Fx expression that uses the generated function.
        var engine = new RecalcEngine(config);
        var result = await engine.EvalAsync(
            @"MyConnector.SendEmail({ to: ""a@b.com"", subject: ""Hi"" })",
            CancellationToken.None,
            options: new ParserOptions { AllowsSideEffects = true },
            runtimeConfig: runtimeConfig);

        Console.WriteLine(result.ToObject());
    }

    private sealed class MyConnectorRuntimeContext : BaseRuntimeConnectorContext
    {
        private readonly string _ns;
        private readonly HttpMessageInvoker _invoker;
        public MyConnectorRuntimeContext(string ns, HttpMessageInvoker invoker) { _ns = ns; _invoker = invoker; }
        public override HttpMessageInvoker GetInvoker(string @namespace) => _invoker;
        public override TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;
    }
}
```

Alternatively, when NOT going through the Power Fx engine, `ConnectorFunction.InvokeAsync` can be called directly:

```csharp
ConnectorFunction sendEmail = functions.First(f => f.Name == "SendEmail");

FormulaValue result = await sendEmail.InvokeAsync(
    new FormulaValue[]
    {
        FormulaValue.NewRecordFromFields(
            new NamedValue("to",      FormulaValue.New("a@b.com")),
            new NamedValue("subject", FormulaValue.New("Hi")))
    },
    runtimeCtx,
    CancellationToken.None);
```

---

## Tabular Connectors (CDP)

**Tabular connectors** don't come from a swagger operation list. Instead they use the [**Common Data Provider (CDP)**](https://learn.microsoft.com/connectors/custom-connectors/custom-connector-tabular) protocol — a fixed set of REST endpoints exposed by the connector (SQL, SharePoint, SAP, Salesforce, etc.) that let you enumerate datasets, discover tables, retrieve schema, and read/write items.

The library models the four CDP endpoints and returns a `CdpTableValue` that plugs into the Power Fx engine as an `IDelegatableTableValue` — meaning `Filter`, `Sort`, `FirstN`, `CountRows`, `GroupBy` are translated into OData query parameters and delegated to the server rather than materialized in memory.

Key types:

| Type | Purpose |
| --- | --- |
| [`CdpDataSource`](Tabular/Services/CdpDataSource.cs) | Entry point. Represents one dataset. Fetches dataset metadata, enumerates tables, and creates `CdpTable` instances. |
| [`CdpTable`](Tabular/Services/CdpTable.cs) | One table within the dataset. Initializes schema via `InitAsync`, queries items via `GetItemsInternalAsync`. |
| [`CdpService`](Tabular/Services/CdpService.cs) | Abstract base defining `GetItemsAsync` / `ExecuteQueryAsync`. |
| [`CdpServiceBase`](Tabular/Services/CdpServiceBase.cs) | Low-level HTTP helper. `GetObject<T>` deserializes CDP responses with `System.Text.Json`. |
| [`CdpTableResolver`](Tabular/CdpTableResolver.cs) | Fetches per-table metadata and turns the CDP schema JSON into a `ConnectorType` / `RecordType`. Includes a bounded async-deduplicating cache. |
| [`CdpTableValue`](Public/CdpTableValue.cs) | Public Power Fx `TableValue` that implements `IDelegatableTableValue` and lazily fetches rows. |
| [`DatasetMetadata`, `RawTable`, `GetTables`](Tabular/Services/InternalObjects.cs) | POCOs mapped from the JSON responses. |
| [`ServiceCapabilities`, `ColumnCapabilities`, ...](Tabular/Capabilities/) | Delegation capabilities parsed from `x-ms-capabilities`. |

### Endpoint sequence

The library only exercises the CDP endpoints needed for a **read** scenario. Create / update / delete / get-single-item are documented by the CDP protocol but are not called by any code path in `Microsoft.PowerFx.Connectors` today. The four HTTP calls that the code actually issues, in the order it issues them, are:

1. **Dataset metadata** —
   `GET {uriPrefix}[/v2]/$metadata.json/datasets`
   ([`CdpDataSource.GetDatasetsMetadataAsync`](Tabular/Services/CdpDataSource.cs) lines 57-64).
   Deserializes to [`DatasetMetadata`](Tabular/Services/InternalObjects.cs) — tells us whether the dataset uses single or double URL encoding, whether it's tabular/blob, etc.

2. **Table list** —
   `GET {uriPrefix}[/v2]/datasets/{dataset}/tables` (or `/alltables` for SharePoint)
   ([`CdpDataSource.GetTablesAsync`](Tabular/Services/CdpDataSource.cs) lines 66-83).
   Deserializes to `GetTables` (`InternalObjects.cs` lines 12-21), yielding `RawTable` entries with logical + display names. Each becomes a lazy `CdpTable`.

3. **Table schema** —
   `GET {uriPrefix}[/v2]/$metadata.json/datasets/{dataset}/tables/{table}?api-version=2015-09-01[&extractSensitivityLabel=True][&purviewAccountName=...]`
   ([`CdpTableResolver.BuildTableMetadataUri`](Tabular/CdpTableResolver.cs) lines 128-143; call site [`CdpTable.InitAsync`](Tabular/Services/CdpTable.cs) lines 81-112).
   The response is a Swagger fragment describing the table's row schema, which is turned into a `ConnectorType` via `CdpTableResolver`. From that we derive:
   - `RecordType` / `TableType` used for type checking Power Fx expressions.
   - `TableDelegationInfo` (filter/sort/select restrictions) — surfaced via `CdpTable.DelegationInfo`.
   - `OptionSets` (enum columns).
   - `Relationships` (foreign key–style references to other tables).

   `CdpTableResolver` uses an async-deduplicating cache keyed by the full metadata URI ([`CdpTableResolver`](Tabular/CdpTableResolver.cs) lines 34-97) so multiple concurrent lookups of the same table share a single HTTP round trip.

4. **List items** —
   `GET {uriPrefix}[/v2]/datasets/{dataset}/tables/{table}/items?api-version=2015-09-01[&$filter=...&$top=...&$orderby=...&$select=...]`
   ([`CdpTable.Query`](Tabular/Services/CdpTable.cs) lines 145-163). The OData query string is produced by `DelegationParameters.GetODataQueryString`, which is fed the delegation info the engine builds during binding. Both `CdpTable.GetItemsInternalAsync` (paged rows) and `CdpTable.GetItemInternalAsync` (single-record / aggregation results) go through this same endpoint.

`CdpServiceBase.GetObject` is the workhorse for all four calls ([`CdpServiceBase.GetObject`](Tabular/Services/CdpServiceBase.cs) lines 21-68). It:
- Uses `HttpMethod.Get` unless a `content` payload is provided (in which case `POST` with `application/json`).
- Reads the response body as a UTF-8 string.
- Throws a `PowerFxConnectorException` with `StatusCode` when status ≥ 300.
- Deserializes the JSON payload via `System.Text.Json.JsonSerializer.Deserialize<T>` for the generic overload.
- If the deserialized type implements `ISupportsPostProcessing`, calls `PostProcess()` (e.g. `GetTables.PostProcess` strips `[...]` decorations from display names — [`GetTables.PostProcess`](Tabular/Services/InternalObjects.cs) lines 17-20).

### Metadata → FormulaType conversion

The schema JSON returned by step 3 is essentially a Swagger `Schema` object. `CdpTableResolver` uses the existing swagger→FormulaType path in [`OpenApiExtensions`](OpenApiExtensions.cs) so tabular column types are produced by the same code that produces action-connector types.

The public `ConnectorType` ([`ConnectorType`](Public/ConnectorType.cs)) is the intermediary — it holds both the Power Fx `FormulaType` (for the engine) and the OpenAPI/`ISwaggerSchema` details (for x-ms extensions such as `x-ms-keyType`, `x-ms-capabilities`, `x-ms-relationships`, `x-ms-enum-values`).

At the end of `CdpTable.InitAsync` (line 111):

```csharp
RecordType = (RecordType)TabularTableDescriptor.FormulaType;
```

`CdpTableValue.Type` therefore contains a full Power Fx `TableType` with display names, so `Engine.Check` can perform proper name resolution and delegation analysis against expressions like `First(Customers).Address`.

### Row deserialization

There are two read code paths on `CdpTable`:

- `GetItemsInternalAsync` (line 130) — returns a paged list. It calls `Query`, then passes the returned JSON to `FormulaValueJSON.FromJson(text, RecordType.Empty().Add("value", TableType))` inside [`CdpTable.GetResult`](Tabular/Services/CdpTable.cs) (line 165), extracts the `"value"` field (an OData collection) and returns its rows.
- `GetItemInternalAsync` (line 137) — used for aggregations/single-record queries. Calls `FormulaValueJSON.FromJson(text)` without a schema hint and lets the engine coerce to `parameters.ExpectedReturnType`.

Both paths ultimately delegate JSON deserialization to `FormulaValueJSON.FromJson` (see [`FormulaValueJSON.FromJson`](../Microsoft.PowerFx.Json/FormulaValueJSON.cs) line 45), which is the same deserialization used by action connectors. This guarantees that the type mapping (JSON string ↔ `StringValue`, JSON number ↔ `DecimalValue`/`NumberValue`, ISO 8601 ↔ `DateTimeValue`, arrays ↔ `TableValue`, objects ↔ `RecordValue`) stays consistent across both connector styles.

`CdpTableValue.Rows` (line 50) caches the last-fetched page so that reading the same table twice in one expression doesn't re-hit the server. When the engine performs delegation it goes through `IDelegatableTableValue.GetRowsAsync` / `ExecuteQueryAsync` (lines 77-118) instead — those bypass the cache and always forward the caller's `DelegationParameters` (which include the OData $filter, $orderby, $top, etc.).

### End-to-end example (tabular connector)

The pattern below is directly modelled on [`PowerPlatformTabularTests.SQL_CdpTabular`](../../tests/Microsoft.PowerFx.Connectors.Tests.Shared/PowerPlatformTabularTests.cs) (lines 546-600).

```csharp
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Types;

public static class TabularConnectorSample
{
    public static async Task RunAsync()
    {
        // 1. Build the HTTP transport. PowerPlatformConnectorClient handles
        //    the APIM /invoke wire protocol used by Power Platform connectors.
        using var httpClient = new HttpClient();
        using var client = new PowerPlatformConnectorClient(
            endpoint:      "firstrelease-003.azure-apihub.net",
            environmentId: "49970107-0806-e5a7-be5e-7c60e2750f01",
            connectionId:  "18992e9477684930acd2cc5dc9bb94c2",
            getAuthToken:  () => "<JWT>",
            httpInvoker:   httpClient)
        { SessionId = Guid.NewGuid().ToString() };

        var logger      = new ConsoleLogger();
        var basePath    = $"/apim/sql/{client.ConnectionId}";
        var datasetName = "pfxdev-sql.database.windows.net,connectortest";

        // 2. Fetch dataset metadata + table list.  (HTTP #1 and #2)
        var cds    = new CdpDataSource(datasetName, ConnectorSettings.NewCDPConnectorSettings());
        var tables = await cds.GetTablesAsync(client, basePath, CancellationToken.None, logger);

        // 3. Pick the table we care about and fetch its schema.  (HTTP #3)
        CdpTable customers = tables.First(t => t.DisplayName == "Customers");
        await customers.InitAsync(client, basePath, CancellationToken.None, logger);

        // 4. Wrap it as a Power Fx TableValue and register it in a SymbolTable.
        CdpTableValue customersValue = customers.GetTableValue();
        var symbolValues = new SymbolValues().Add("Customers", customersValue);

        // 5. Bind & evaluate.  The engine will delegate the Filter/Sort/First back
        //    to CdpTable.Query via CdpTableValue.GetRowsAsync.  (HTTP #4)
        var config = new PowerFxConfig(Features.PowerFxV1);
        var engine = new RecalcEngine(config);
        var rc     = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

        FormulaValue result = await engine.EvalAsync(
            @"First(Filter(Customers, Country = ""France"")).Address",
            CancellationToken.None,
            options:      new ParserOptions { AllowsSideEffects = true },
            runtimeConfig: rc);

        Console.WriteLine(((StringValue)result).Value);
    }
}
```

Traceable HTTP calls, in order:

| # | Method | URL (relative) | Producer | Consumer |
|---|--------|----------------|----------|----------|
| 1 | GET | `.../v2/$metadata.json/datasets` | Server dataset descriptor | `CdpDataSource.GetDatasetsMetadataAsync` → `DatasetMetadata` |
| 2 | GET | `.../v2/datasets/{dataset}/tables` | Table list | `CdpDataSource.GetTablesAsync` → `IEnumerable<CdpTable>` |
| 3 | GET | `.../v2/$metadata.json/datasets/{dataset}/tables/{table}?api-version=2015-09-01` | Table schema | `CdpTableResolver.ResolveTableAsync` → `ConnectorType` / `RecordType` |
| 4 | GET | `.../v2/datasets/{dataset}/tables/{table}/items?api-version=2015-09-01&$filter=...` | Rows | `CdpTable.Query` → `FormulaValueJSON.FromJson` → `RecordValue[]` |

---

## Shared infrastructure

- **`ConnectorSettings`** ([`ConnectorSettings`](Public/ConnectorSettings.cs)) — controls parser and runtime behavior: namespace, max rows, compatibility mode (`PowerAppsCompatibility` vs `SwaggerCompatibility` vs `CdpCompatibility`), whether to include internal/unsupported functions, whether to return unknown record fields as `UntypedObject`, sensitivity label extraction, and more. `ConnectorSettings.NewCDPConnectorSettings` (line 37) is the canonical factory for tabular connectors.

- **`ConnectorLogger`** ([`ConnectorLogger`](Public/ConnectorLogger.cs)) — abstract logger consumed by every entry point. Tests use `ConsoleLogger` and verify exact log output (see [`LoggerTests.ConnectorLogger_Test8`](../../tests/Microsoft.PowerFx.Connectors.Tests.Shared/LoggerTests.cs) line 117 for a full expected-log assertion).

- **`ConnectorType`** ([`ConnectorType`](Public/ConnectorType.cs)) — wraps a `FormulaType` with extra swagger metadata (dynamic values/schema/property/list, x-ms-visibility, x-ms-capabilities, x-ms-keyType, x-ms-relationships, x-ms-media-kind, MIP sensitivity labels, etc.). This is what feeds intellisense and delegation.

- **`PowerPlatformConnectorClient` / `PowerPlatformConnectorClient2`** — `HttpClient` subclasses that rewrite the request to the APIM `/invoke` protocol (adds `authority`, `scheme`, `path`, `x-ms-request-method`, `x-ms-request-url`, `Authorization: Bearer …`, `x-ms-client-environment-id`, `x-ms-user-agent`, `x-ms-client-session-id`; see [`PowerPlatformConnectorClient.Transform`](PowerPlatformConnectorClient.cs) lines 173-210). Use `PowerPlatformConnectorClient2` for new code — v1 is marked `[Obsolete]`.

- **`FormulaValueJSON`** (in `Microsoft.PowerFx.Json`) — the single JSON↔FormulaValue codec used by both connector styles for reading responses. See [`FormulaValueJSON.FromJson`](../Microsoft.PowerFx.Json/FormulaValueJSON.cs).

Together these pieces let a swagger file or CDP endpoint show up as ordinary Power Fx symbols with strong typing, delegation, and intellisense.
