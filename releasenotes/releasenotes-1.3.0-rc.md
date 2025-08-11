# Changes for 1.3.0 [RC]

## Breaking Language changes:


## New API features
  - Void accept behavior (https://github.com/microsoft/Power-Fx/pull/2096): There are three main changes related to `Void` type:
    - Void will accept all types, including Void.
    - Void will not be accepted by any other types (except Void). Trying to use the output from Set will result in an error.
    - The following functions will return Void: Set, Clear, Remove, Notify (Repl), Help (Repl), Exit (Repl). Future functions that have no return value will return Void. 
  - Summarize (https://github.com/microsoft/Power-Fx/pull/2317): A new Power Fx function that combines grouping and aggregation in one call.\
`Summarize( Table, GroupByColumn1 [, GroupByColumn2 …], AggregateExpr1 As Name [, AggregateExpr1 As Name …] )`

  - Suggestions (https://github.com/microsoft/Power-Fx/pull/2365): A new Power Fx REPL function that prints suggestions based on input. Use `|` char to determine the cursor position.\
`Suggestions("Abs|(")`

  - Collect / ClearCollect (https://github.com/microsoft/Power-Fx/pull/2156): The Collect and ClearCollect functions now support multiple arguments, including primitive ones.\
`Collect(collection:*[...], item1:![...]|*[...], ...)`
`ClearCollect(collection:*[...], item1:![...]|*[...], ...)`

  - Patch (https://github.com/microsoft/Power-Fx/pull/2269): The Patch function now supports different overloads.\
`Patch(dataSource:*[], Record, Updates1, Updates2,…)`\
`Patch(DS, record_with_keys_and_updates)`\
`Patch(DS, table_of_rows, table_of_updates)`\
`Patch(DS, table_of_rows_with_updates)`\
`Patch(Record, Updates1, Updates2,…)`

  - ParseJSON, IsType, AsType (https://github.com/microsoft/Power-Fx/pull/2569): ParseJSON, IsType, AsType functions now supports in-lined and user-defined types as argument.\
  `ParseJSON(Text, Type)`\
  `IsType(UntypedObject, Type)`\
  `AsType(UntypedObject, Type)` 

## Updated function behaviors:
  - TimeValue function (https://github.com/microsoft/Power-Fx/pull/2731)
    - Support for am/pm designators: `TimeValue("6:00pm")` now works (used to return an error)
    - Better validation: `TimeValue("1")` would return a time value (equivalent to `Time(0,0,0)`), now returns an error
    - Better support for wrapping times around the 24-hour mark: `TimeValue("27:00:00")` now returns the same as `Time(3,0,0)`, consistent with Excel's behavior.

## Other:  
  - Untyped object
    - Read a field from an untyped object by index (https://github.com/microsoft/Power-Fx/pull/2555):  
      `Index(untypedObject, 1) // Read the first field from an untyped record`  
      `Index(Index(untypedObject, 1), 1) // Read the record from an untyped array, then reads the first field.`
      
    - Setting an untyped object via deep mutation is now supported (https://github.com/microsoft/Power-Fx/pull/2548):  
      `Set(untypedObject.Field, 99)`      
      `Set(Index(untypedObject, 1).Field, 99)  // Reference field by name`  
      `Set(Index(Index(untypedObject, 1), 1), 99) // Reference field by index`
