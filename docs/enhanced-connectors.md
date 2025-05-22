
# Overview
Enhanced connectors allow Power Platform to connect to tabular data in external datasources. Whereas action connectors allow invoking a fixed list of operations calls described via OpenApi("swagger"), Enhanced connectors need to describe a dynamic set of tables, schemas and capabilities. 

This describes the Enhanced connector Protocol. This protocol is a RESTful protocol primarily based on OData and augmented with metadata to be self-describing. 

The protocol is stateless and "pass through" - no indices are created. Clients (such as Power Apps) call into the connector via Odata, and then the connector transforms that to an outgoing request to the underlying datasource.  When used with Power Platform, the Connector infrastructure will stamp the auth token that was provided when the connection was created. 

So essentially, an enhanced connector has several key parts:
1. Metadata: ability to describe the metadata of the target datasources 
2. The Transpiler: ability to accept an OData request and execute it against the target datasource. This is for Read operations. 
3. Create, Update, Delete operations. 

## References

- An overview of Power Platform connectors: https://learn.microsoft.com/en-us/connectors/overview
- There is a sample connector implementation at: https://github.com/microsoft/power-fx-enhanced-connector 
- There is a connector client for invoking the connectors in the Power Fx repo, at: https://github.com/microsoft/Power-Fx/tree/main/src/libraries/Microsoft.PowerFx.Connectors 



## Concepts 
Tabular data follows the following resource hierarchy: 

- A `dataset` exposes a collection of tables 
- A `table` has `rows` and `columns` that contain data 
- An `item` represents a row of a table 

This means the tables are exposed in a 2-tier namespace (dataset / table). 

Examples:
| Connector | Dataset | Table |
|--|--|--|
| SQL | Database | Table |
| SharePoint | Site | List |

Tables are queried using a subset of `OData queries`, notably $filter, $select, $orderby, $sort, and $top. A key principle of Power Platform is to enable good design-time experiences. 
This means that the protocol is self-describing with rich metadata endpoints. The connector must describe which queries it can support at design time - prior to executing the query. 

`Capabilities` refers to metadata describing what query operations are supported. 

For example, a UI may only allow filtering on columns that are marked filterable; or it may only show sort icons on columns that can be sorted. 

`Transpiler` refers to converting from the incoming odata query to the datasource's native format.  For example, the SQL connector takes in odata queries and converts them to outgoing SQL statements. Connectors effectively must implement a query tree transformation. 

Tables and Columns may have both:
- `Logical Names` which are used in the API. This could be a guid or mangled form of the display name. Logical names are api-friendly and just alpahnumeric characeters.
- `Display Names` which are shown to the user and can include any characters (including spaces, and special characters).


A connector is primarily  means having a `transpiler` that takes in an incoming OData request and converts it to the outgoing 

## Power Fx and Delegation 

Power Fx expressions provide an easy way to access Enhanced connectors.  `Delegation` refers to translating a Power Fx function into an efficieint server-side operation. The Power Fx engine will look at the connector capabilities and produce a query. 

Power Fx will also present Display Names in the expressions but translate to logical names for the queries. 

For example, 
- if `MyTable` is a connection with dataset="default" and tableName="myTable512", 
- and it has columns with display names Score and Age, and logical names new_score, new_age respectively
- then that table capabilities suports filter and sort, then this expression:

`Sort(Filter(MyTable, Score > 100), Age)`

Would get executed as a single query to the connector:

`GET /datasets/default/tables/myTable512/items?$filter=new_score+gt+100&$sort=new_age`

Power Fx may also do column analysis and add a $select clause. 

For operations that can't be delegated, Power Fx can  detect this statically via the metadata nd issue a warning. The host then has the option to fallback to downloading the rows and doing client-side execution. This is effective for small tables. 

See more: https://learn.microsoft.com/en-us/power-apps/maker/canvas-apps/delegation-overview 

# REST API 
This section describes the REST endpoints the connector needs to implement. 
APIs are restful and return JSON results.

The endpoints should use these status codes:
- 200: success
- 401: unauthorized request
- 404: item not found


Errors should follow a standard payload:
```
{
    "message" : "here's a message",
    "RequestUri" : "/"
}
```

## Auth 
An enhanced connector is "pass through". The connector will receive an auth token that was setup when the connection was created.  This could be an api key or auth token to the target datasource.   It could also be a property bag (like a base64 encoded JSON object) that contains multiple properties like an API Endpoint and API key for accessing the target data. 


## Table Metadata

### GET /datasets
Enumerates the data sets in the connector. 
Often, this just returns a single dataset named `default`. 


### GET /$metadata.json/datasets
Get connector-wide  metadata for the dataset.

Sample 200 response:
```
{
  "tabular": {
    "source": "mru",
    "displayName": "site",
    "urlEncoding": "double",
    "tableDisplayName": "list",
    "tablePluralName": "lists"
  },
  "blob": {
    "source": "mru",
    "displayName": "site",
    "urlEncoding": "double"
  }
}
```

### GET /datasets/{datasetName}/tables
Enumerate the tables in the dataset. 

Returns an array of tableName and DisplayName pairs. The tableName is the logical name that can be used elsewhere in this API.  The DisplayName is suitable to show the user.

Sample 200 response:
```
{
  "value": [
    {
      "Name": "4bd37916-0026-4726-94e8-5a0cbc8e476a",
      "DisplayName": "Documents"
    },
    {
      "Name": "5266fcd9-45ef-4b8f-8014-5d5c397db6f0",
      "DisplayName": "MyTestList"
    }
  ]
}
```


### GET /$metadata.json/datasets/{datasetName}/tables/{tableName}
Get metdata for a specific table. This includes:
- table capabilities 
- column information 




| Field | Description |
|--|--|
| `name` | Logical name of the table |
| `title` | Display name of the table.<br>This is optional and when missing, display name will be `name` |
| `x-ms-permission` | Table level permission <br> Only 3 possible values <br><br>- `read-write` for non-primary keys, that support Create and Update <br> - `read-only` for primary keys, calculated columns <br> - `null` otherwise |
| `x-ms-capabilities` | Table level capabilities (See Capabilities chapter below) |
| `schema` | Table description (swagger/OpenAPI format) <br> - `items/properties` define the list of columns, each having `x-ms-capabilities` extension to define column capabilities <br> - `items/x-ms-relationships` Relationships, in relation/combination with `referencedEntities` <br> - `items/x-ms-displayFormat` Ordering of columns <br> - `items/required` is a string array defining the list of required properties<br><br> Other extensions exist (*) |
| `referencedEntities` | [INTERNAL] Relationships, in relation/combination with `schema\items\x-ms-relationships`<br>&#x26A0; Unclear why those are separated and not included in `x-ms-capabilities`| 
| `webUrl` | [INTERNAL] &#x26A0; URL link, Unknown usage | 

(*) Schema extensions (Column level)

| <div style="width:230px">Extension</div> | Description |
|--|--|
| `x-ms-keyType` | Can only be `primary` or `none` <br> When set to `primary` defines the primary key or a part of it <br> <br> If primary keys are readonly and not required, they are server generated.<br>If they are writable and required, they are user provided.<br> Any other combination is invalid, except for `shared_dynamicsax`connector (FnO). <br><br> All `primary` keys appear FIRST in the list of columns, ordered by `x-ms-keyOrder` value. <br> All other columns appear in the order of the schema, as they appear in `properties` list, EXCEPT if `x-ms-DisplayFormat` is defined with `propertiesDisplayOrder`. |
| `x-ms-keyOrder` | Integer starting from 0 <br> When defined, `x-ms-keyType` must be set to `primary` <br> If multiple columns use `x-ms-keyOrder`, they all have to be different. "Missing" values are possible. <br> Defines the list of columns that represent the primary key and in which order they appear. |
| `x-ms-permission` | Only 3 possible values <br><br>- `read-write` for non-primary keys, that support Create and Update <br> - `read-only` for primary keys, calculated columns <br> - `null` otherwise (equivalent to RW) |
| `x-ms-sort` | Defines if ordering is supported <br> Comma separated list <br> `asc` when ascending order is supported <br> `desc`when descending order is supported  <br> `asc,desc` when both ascending and descending are supported (`desc,asc` is not valid) <br> `none` when ordering is not supported |
| `x-ms-visibility` | Defines how columns are displayed in the UI <br> Only 3 possible values: <br>`none` for regular properties, always visible <br>`advanced` for properties in 'Advanced' view  <br>`internal` for properties not visible to the end used |
| `x-ms-dynamic-values` | Dynamic Intellisense <br> Documented [here](https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values)|
| `x-ms-media-kind` | [INTERNAL] Type of media <br>- `image` (=WADL Image, default), <br>- `video` (WADL Media),<br>- `audio` (WADL Media)<br>- or null (=WADL Blob) <br><br> Used when type=`string` and <br>- format=`byte` (`byte`=Base 64 encoding) <br> - format=`uri` |
| `x-ms-media-base-url` | [INTERNAL] A runtime url to the base path to a RESTful service implementation of the Blob Protocol (namely CDPBlob0). e.g. the base path to the same connector's Blob protocol implementation, base path to a dependent connector's Blob protocol implementation (e.g. the DropBox/OneDrive connector baseUrl <br><br> ONLY used when type=`string` and format=`uri` <br> &#x26A0; Unknown usage <br> |
| `x-ms-media-default-folder-path` | [INTERNAL] `UploadFile` or `InlineDataUri` (not case sensitive)<br>At runtime, this folder path is given to the Blob protocol endpoints to identify where to place a new uploaded Blob. <br><br> ONLY used when type=`string` and format=`uri`. <br> MANDATORY when `x-ms-media-base-url` is defined.<br> |
| `x-ms-media-upload-format` | [INTERNAL] &#x26A0; Unknown usage|
| `x-ms-displayFormat` | [INTERNAL] This extension can be defined<br>- for properties of type=object<br>- at table level for the ordering of columns. <br><br> The schema of this extension varies depending on its location. <br> <br> For table level:<br>- `propertiesDisplayOrder` Ordering used for gallery on the web in Power Apps. <br>This is a string array with a list of propertie/colums of the tables. When defined, columns/properties defined in this list appear first, then primary keys not in this list and following `x-ms-keyOrder`, then all other columns not this list, based on their order of appearance in the list of properties. <br> - `propertiesCompactDisplayOrder`Ordering used for gallery on mobile device in Power Apps. <br> - `propertiesTabularDisplayOrder` Ordering used for grid/table in Power Apps<br><br>![image.png](/.attachments/image-ebe56358-37f0-4c37-8e61-591c6e7f25c8.png) <br><br>For column level:<br>- `titleProperty` &#x26A0; Unknown usage, <br>- `subtitleProperty` &#x26A0; Unknown usage, <br>- `thumbnailProperty` &#x26A0; Unknown usage <br><br>![image.png](/.attachments/image-033ede7c-29b6-4bd2-975a-7a619f8f7afd.png)|
| `x-ms-capabilities` | Column level capabilities (See Capabilities chapter below) |
| `x-ms-navigation` | [INTERNAL] &#x26A0; Unknown usage <br> Has `targetTable` and `referentialConstraints` properties |
| `x-ms-enable-selects` | [INTERNAL] SharePoint specific header for controlling $select. <br>Boolean value. Enables SPO ECS feature.<br> &#x26A0; Unclear usage|
| `enum` | This one isn't an extension and described in [OpenAPI Specification - Version 2.0 Swagger](https://swagger.io/specification/v2/)<br> It only describes logical names |  
| `x-ms-enum` | Provides the name of an OptionSet<br> Documented in [AutoRest/Documentation/swagger-extensions.md at master · stankovski/AutoRest · GitHub](https://github.com/stankovski/AutoRest/blob/master/Documentation/swagger-extensions.md#x-ms-enum)| 
| `x-ms-enum-values` | Describes logical and display names <br><br>![image.png](/.attachments/image-c49313ce-b104-45cd-b1f8-0b99260e144c.png)| 
| `x-ms-content-sensitivityLabelInfo`                      | Metadata about a sensitivity label applied to this column. The value is a JSON object with these properties:<br>- <code>sensitivityLabelId</code> (<code>string</code>): GUID of the label<br>- <code>name</code> (<code>string</code>): Internal label name<br>- <code>displayName</code> (<code>string</code>): Friendly label name<br>- <code>tooltip</code> (<code>string\|null</code>): Optional help text<br>- <code>priority</code> (<code>string</code>): Label priority (numeric string; lower = higher importance)<br>- <code>color</code> (<code>string</code>): Hex color code for UI badge<br>- <code>isEncrypted</code> (<code>"true"/"false"</code>): Whether content is encrypted<br>- <code>isEnabled</code> (<code>"True"/"False"</code>): Whether the label is active<br>- <code>isParent</code> (<code>"True"/"False"</code>): True if it has child labels<br>- <code>parentSensitivityLabelId</code> (<code>string\|null</code>): GUID of its parent label, if any |

##### Example of `x-ms-content-sensitivityLabelInfo`

```json
"x-ms-content-sensitivityLabelInfo": {
  "sensitivityLabelId":       "8ff7c975-dd7e-422d-8675-8d2aceaf54b3",
  "name":                     "Confidential Microsoft FTE",
  "displayName":              "Microsoft FTE",
  "tooltip":                  null,
  "priority":                 "6",
  "color":                    "#F7630C",
  "isEncrypted":              "false",
  "isEnabled":                "True",
  "isParent":                 "False",
  "parentSensitivityLabelId": "c181f07c-4e5f-4dac-b7cf-207b8ca6b35b"
}
```

OptionSets have a name which is defined as `propertyName (tableName)` when no `x-ms-enum` extension is used.
Otherwise, it will have `x-ms-enum.name (tableName)` name, where `x-ms-enum.name` is the value of `name` property in `x-ms-enum` (ie. there is no dot).

[INTERNAL] Note: Excel connector defines `__PowerAppsId__` column which is hidden (by design)

![image.png](/.attachments/image-cf11340c-6e52-4d09-aaaf-1e11e853306d.png)

### Capabilities

Capabilities are used for query delegation
As an example `First(table)`, without delegation, will make the server retrieve all rows (or first page of, for example, 1000 rows), then return only 1 row
With delegation enabled, we'll be able to send "GetItems" request with "$top=1" command to have the server only return 1 row.

Capabilities are defined in `x-ms-capabilities` extension.
Capabilities are categorized in 2 parts: table level capabilities and column level capabilities.

#### Table level capabilities

Defined in `x-ms-capabilities`

| Capability | TableDelegationInfo<br><small>In parenthesis when internal</small> | Description |
|--|--|--|
| | TableName | Name of table |
| | IsReadOnly | ReadOnly table, when `x-ms-permission` is set to `read-only`<br> When a table is defining no primary column, it is also marked `read-only` and a fake primary key column is added to it ("KeyId", number)|
| `sortRestrictions` | SortRestriction | Sort Restrictions |
|  | IsSortable| When SortRestriction is not null |
| `countRestrictions` | CountRestriction | Count Restrictions |
|  | IsCountable | When `countRestrictions` is not null and `countable=true`, the table supports `$count` in OData queries |
| `filterRestrictions` | FilterRestriction | Filter Restrictions |
| `selectRestrictions`  | SelectionRestriction | Select Restrictions |
|  | IsSelectable | When SelectionRestriction is not null && SelectionRestriction.IsSelectable |
| `isDelegable`  | IsDelegable | Defines if the table is supporting some level of delegation<br> = IsSortable \|\| FilterRestriction not null \|\| FilterFunctions not null |
| `groupRestriction` | GroupRestriction | Group Restrictions |
| ~~`filterFunctions`~~ | &#8709; | This property is defined in PowerApps but isn't valid<br>It is not provided in C# implementation |
| `filterFunctionSupport` | FilterSupportedFunctions | List of supported functions by the table in a filter function<br> For CDS, defaults to `{ "eq", "ne", "gt", "ge", "lt", "le", "and", "or", "cdsin", "contains", "startswith", "endswith", "not", "null", "sum", "average", "min", "max", "count", "countdistinct", "top", "astype", "arraylookup" }`<br> Defines default column capabilities for all columns before any restriction would apply |
| `serverPagingOptions` | (PagingCapabilities.ServerPagingOptions) | String array (enum) - List of supported server-driven paging capabilities <br>Only valid values `top` or `skiptoken` <br> &#x26A0; `skiptoken` seems never used|
| `isOnlyServerPagable` | (PagingCapabilities.IsOnlyServerPagable) | Boolean, default `false` - Defines if a table is pageable even if it wouldn't be supporting delegation<br> Always `true` for CDS<br> In opposition with client paging<br>If set to `false` or server paging options contains `top`, $top OData will be used - [ref, line 934]( https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/AppMagic/js/AppMagic.Services/ConnectedData/CdpConnector.ts&_a=contents&version=GBmaster) <br>If set to `true`, `@odata.nextlink` URL is used instead of $skip and $top query parameters|
| `isPageable` | | Defines is a table supports paging <br> This can be `true` with `isDelegable` = false. |
| `odataVersion` | | Specified the OData version <br>Either 3 or 4. <br>No other version is supported. <br> See differences here [What's New in OData Version 4.0](https://docs.oasis-open.org/odata/new-in-odata/v4.0/cn01/new-in-odata-v4.0-cn01.html) |
| `supportsRecordPermission` | (SupportsRecordPermission) | For supporting record level permissions. <br> Always true in CDS. <br> Used by [RecordInfo](https://learn.microsoft.com/en-us/power-platform/power-fx/reference/function-recordinfo) function |

##### Sort restrictions

Defines what properties can be sorted or not, ascending only or asc,desc.

| Capability  | Description  |
|--|--|
| `sortRestrictions\sortable`  | Boolean value defining if sorting is supported. <br> This property is optional <br> If `false`, no other property can be defined |
| `sortRestrictions\unsortableProperties` | Enumerates the list of properties that are not sortable. |
| `sortRestrictions\ascendingOnlyProperties` | Enumerates the list of properties that only support ascending ordering. |

![image.png](/.attachments/image-32023a14-8a9d-4c7c-b528-80b2721871d2.png)

##### Filter restrictions

If not defined, there is no filter restriction in place.

| Capability  | Description  |
|--|--|
| `filterRestrictions\filterable` | Boolean value defining if filtering is supported. <br> This property is optional <br> If `false`, no other property can be defined |
| `filterRestrictions\requiredProperties` | [INTERNAL] &#x26A0; Unknown usage |
| `filterRestrictions\nonFilterableProperties` | List of properties that do not support filtering |

![image.png](/.attachments/image-d44d8853-984f-4c07-8392-af8325d88490.png)


##### Select restrictions

| Capability  | Description  |
|--|--|
| `selectRestrictions\selectable` | Boolean value, indicates whether this table has selectable columns <br> Always `true` for CDS <br> If set to `true`, `$select` will be used in OData query. All columns will be used in $select, except Attachment type ones in lazy records. |

![image.png](/.attachments/image-196cbd5f-d319-42b2-a1f7-00e33cbaf13d.png)

##### Group restriction

| Capability  | Description  |
|--|--|
| `selectRestrictions\ungroupableProperties` | Defines the list of ungroupable properties. |

#### Column level capabilities

Defined in `x-ms-capabilities`

| Capability | Description |
|--|--|
| `filterFunctions` | List of functions allowed in a filter for the given column<br> List of strings/functions allowed is defined in [DelegationMetadataOperatorConstants](https://github.com/microsoft/Power-Fx/blob/main/src/libraries/Microsoft.PowerFx.Core/Functions/Delegation/DelegationMetadataOperatorConstants.cs) <br><br> Current list: "eq", "ne", "lt", "le", "gt", "ge", "and", "or", "contains", "indexof", "substringof", "not", "year", "month", "day", "hour", "minute", "second", "tolower", "toupper", "trim", "null", "date", "length", "sum", "min", "max", "average", "count", "add", "sub", "startswith", "mul", "div", "endswith", "countdistinct", "cdsin", "top", "astype", "arraylookup" |
| `x-ms-sp` | SharePoint specific capabilities |

![image.png](/.attachments/image-eda977d9-7900-4394-bba3-e63062241eff.png)

## Runtime CRUD operations 


### Etags

Enhanced connectors use optimistic concurrency via an etag pattern. 
In PATCH, DELETE, POST requests, `If-Match` header can be used
In POST, GET requests, `If-None-Match` header can be used
Responses may contain `ETag` header

Return a 412 status code in etag mismatch. 

### POST /datasets/{datasetName}/tables/{tableName}/items
Create a new row. 

### GET /datasets/{datasetName}/tables/{tableName}/items

OData optional parameters (in query string)
- $filter
- $orderby
- $skip
- $top
- $select
- $count

The supported runtime operations here should be consistent with the capabilities described in the metadata.

#### Sample 200 Response (without `$count`)

<details>
<summary>Response</summary>


```text
HTTP/1.1 200 OK
OData-Version: 4.0
```
```json
{
  "@odata.context": "[Organization URI]/api/data/v9.2/$metadata#accounts(accountid)",
  "value": [
    {
      "@odata.etag": "W/\"81359849\"",
      "accountid": "78914942-34cb-ed11-b596-0022481d68cd"
    }
    // ... more rows
  ]
}
```
</details>

#### Sample 200 Response (with $count=true)
<details> <summary>Response</summary>
    
```text
HTTP/1.1 200 OK
OData-Version: 4.0
```
```json
{
  "@odata.context": "[Organization URI]/api/data/v9.2/$metadata#accounts(accountid)",
  "@odata.count": 9,
  "value": [
    {
      "@odata.etag": "W/\"81359849\"",
      "accountid": "78914942-34cb-ed11-b596-0022481d68cd"
    }
    // ... more rows
  ]
}
```
</details>

### DELETE /datasets/{datasetName}/tables/{tableName}/items/{id}
Delete a row

### PATCH /datasets/{datasetName}/tables/{tableName}/items/{id}
Update a row 



