# Changes for 1.3.0 [RC]

## Breaking Language changes:


## New API features
  - Summarize (https://github.com/microsoft/Power-Fx/pull/2317): A new Power Fx function that combines grouping and aggregation in one call.

`Summarize( Table, GroupByColumn1 [, GroupByColumn2 …], AggregateExpr1 As Name [, AggregateExpr1 As Name …] )`

  - Suggestions (https://github.com/microsoft/Power-Fx/pull/2365): A new Power Fx REPL function that prints suggestions based on input. Use `|` char to determine the cursor position

`Suggestions("Abs|(")`


## Other:
