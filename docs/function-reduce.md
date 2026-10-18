---
title: Reduce function
description: Reference information including syntax and examples for the Reduce function.
author: gregli-msft

ms.topic: reference
ms.custom: canvas
ms.reviewer: mkaur
ms.date: 6/10/2024
ms.subservice: power-fx
ms.author: gregli
search.audienceType:
  - maker
contributors:
  - gregli-msft
  - mduelae
  - gregli
no-loc: ["ForAll"]
---

# Reduce function

Aggregates values for all the [records](/power-apps/maker/canvas-apps/working-with-tables#records) in a [table](/power-apps/maker/canvas-apps/working-with-tables).

## Description

The **Reduce** function reduces a table of values to a single value by evaluating a formula for all the records of the table and aggregating the results together into a single value.

Simple reductions can be done with the **Sum**, **Max** and other aggregate functions for numbers and the **Concat** function for strings, while the **Reduce** function provides a general purpose way to reduce a table. Use the **Map** and **Filter** functions to shape the table before using **Reduce**.  Use the [**Sequence** function](function-sequence.md) with the **Reduce** function to reduce based on a count.

## Formula

The **Reduce** function iteratively evalues a formula. 

This formula uses two values to determine the reduction:
- **ThisRecord** - The current record of the table.
- **ThisReduce** - The accumulated value to this point. This is the return value after the last iteration. This initial value is *blank* unless an _InitialValue_ is supplied.

For example, `Reduce( Sequence(3), ThisReduce + ThisRecord.Value ^ 2 )` performs the following evaluations:

| Iteration | ThisReduce before evaluation                   | ThisRecord  | ThisReduce after evaluation |
|-----------|------------------------------------------------|-------------|-----------------------------|
| 1         | *blank* (since no *InitialValue* was supplied) | {Value: 1}  | 1                           |
| 2         | 1                                              | {Value: 2}  | 5                           |
| 3         | 5                                              | {Value: 3}  | 14                          |

And the return value is the last value of ThisReduce, in this case `14`.

Evaluation is always sequential, from the first record in the table to the last.

[!INCLUDE [record-scope](includes/record-scope.md)]

Likewise, the name of **ThisReduce** can be changed with the `As` operator.

### Return value

The return value is the last value of ThisReduce. If there was no iteration and no *InitialValue* was supplied, the return value is *blank*.

If *InitialValue* is provided, the type of **ThisReduce** and the return type for the function will match. An error will result if the return type of the formula is incompatible.

Without *InitialValue*, the return type is inferred from the type of the formula.

### Delegation

The **Reduce** function is not delegable.

[!INCLUDE [delegation-no-one](includes/delegation-no-one.md)]

## Syntax

**Reduce**(_Table_, _Formula_, [ _InitialValue_ ])

- _Table_ - Required. Table to iterate over.
- _Formula_ - Required. The formula to evaluate for all records of the _Table_ and aggregate together.
- _InitialValue_ - Optional. The initial value for **ThisReduce**.

## Simple Examples

| Formula                                                                                       | Description                                                                                                                                                                                                                      | Result                                                 |
| --------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ |
| `Reduce( Sequence(5), ThisReduce + Value )`<br><br>`Sum(Sequence(5))` | For all the records of the input table, adds together the **Value** column. The **Sum** function can also be used with a single-column table, making it possible to perform this example without using **Reduce**. | ![Example of Sqrt.](media/function-forall/sqrt.png)    |
| `Reduce( Split( "Hello World", Blank() ), ThisReduce & Value )`**<br><br>**`Concat( Split( "Hello World", Blank() ), Value )` | For all the records of the input table, concatenates together the **Value** column. The **Concat** function can also be used with a single-column table, making it possible to perform this example without using **Reduce**.          | ![Example of Power.](media/function-forall/power3.png) |
| `Reduce( Sequence(5), ThisReduce )` | The return type cannot be inferred as there are no operators or functions used. | *Error: Type cannot be inferred* |
| `Reduce( Sequence(5), ThisReduce + Value )` | The return value is inferred from the formula that uses the numeric `+` operator. The result must be a number. | |
| `Reduce( Sequence(5), ThisReduce & Value )` | The return value is inferred from the formula that uses the text `&` operator. The result must be text. | "12345" |
| `Reduce( Sequence(5), ThisReduce + Value, GUID())` | The return type of the function is GUID based on the *InitialValue*. Sine GUIDs cannot be added, this results in an authoring error. | *Error: type mismatch* |

[!INCLUDE[footer-include](includes/footer-banner.md)]

