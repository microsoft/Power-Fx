# Changes for 1.3.0 [RC]

## Breaking Language changes:


## New API features
  - Void accept behavior (https://github.com/microsoft/Power-Fx/pull/2096): There are three main changes related to `Void` type:
    - Void will accept all types, including Void.
    - Void will not be accepted by any other types (except Void). Trying to use the output from Set will result in an error.
    - The following functions will return Void: Set, Clear, Remove, Notify (Repl), Help (Repl), Exit (Repl). Future functions that have no return value will return Void. 
  - Summarize (https://github.com/microsoft/Power-Fx/pull/2317): A new Power Fx function that combines grouping and aggregation in one call.

`Summarize( Table, GroupByColumn1 [, GroupByColumn2 …], AggregateExpr1 As Name [, AggregateExpr1 As Name …] )`

  - Suggestions (https://github.com/microsoft/Power-Fx/pull/2365): A new Power Fx REPL function that prints suggestions based on input. Use `|` char to determine the cursor position

`Suggestions("Abs|(")`


## Other:
