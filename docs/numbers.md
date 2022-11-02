<!-- 
Copyright (c) Microsoft Corporation.
Licensed under the MIT license.
-->

# Numbers

Microsoft Power Fx supports two kinds of numbers:
- **Decimal** is used for high precision and financial calculations.  It is the type of numeric literals and the **Value** function.
- **Float** is used for high performance calculations and for very large and very small numbers.

In most situations, you do need to know which one to use.  Values will freely coerce between these types as needed.  By having numeric literals and the Value function use **Decimal**, calculations will be done in a high precision manner that most people are accustomed to and is great for calculations involving money.  But the use of controls, objects, and database fields will automatically promotes to the higher performance **Float** when that is more appropriate. 

## Decimal

**Decimal** is best for high precision and financial calculations.  It is the type of numbers you are already most familiar with.  When you write down a number such as "1.234", you are expressing that number in a base 10 "decimal" notation.

**Decimal** performs operations in base 10, much as a person would, and is appropriate for calculations on money.  `0.1` can be exactly represented, and `0.1 + 0.1 + 0.1` exactly equals `0.3`.  In addition, with 28 significant digits, it offers nearly twice the precision of **Float**.

These benefits come at a cost.  **Decimal**'s range is lower than **Float** but for most calculations it is still much more than is needed for most applications.  The real cost is performance: **Decimal** operations can be many times slower than their **Float** counterparts.  In practice, the number of calculations on **Decimal** is relatively small due to promotion to **Float** for many situations that are covered next, and so this performance penalty is usually not a concern. 

There is no commonly used standard for **Decimal** data types.  On most platforms, Power Fx uses the [C# and .NET decimal implementation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types) or the equivalent.  On SQL Server, the native SQL Server decimal data type used which exceeds the range of teh .NET decimal type.

Using the .NET decimal data type, **Decimal** can represent numbers with approximately 28 significant digits, exactly representing whole numbers up to 79,228,162,514,264,337,593,543,950,335.  **Decimal** uses a *scale* to determine how many of these decimal digits are to the right of the decimal point, a kind of floating point.  **Decimal** will round fractional digits using [*round half to even* also known as *banker's rounding*](https://en.wikipedia.org/wiki/Rounding#Round_half_to_even).

## Float

**Float** is best for high performance calculations.  Most computers include dedicated floating point hardware that makes operations very fast.  Power Fx's **Float** is based on the double precision version of the [IEEE 754 standard for floating point arithmetic](https://en.wikipedia.org/wiki/IEEE_754).  

**Float** can represent numbers with approximately 16 significant digits, exactly represent whole numbers up to 9,007,199,254,740,991, and has a approximation range of ±5.0 × 10<sup>−324</sup> to ±1.7 × 10<sup>308</sup>.

One of the challenges of **Float** is that it does not use base 10 math.  Computer friendly base 2 math is used instead, which cannot exactly represent common fractions such as `0.1` which results in the expression `0.1 + 0.1 + 0.1` not equaling `0.3`.  The result is very close at `0.2999999999999999889` and will normally be rounded for display, so this slight imprecision doesn't matter much, but it can lead to surprising results especially in long chains of calculations that are well documented on the web.  This behavior is particularly problematic when dealing with money - most professional developers and database schema designers do not use **Float** for monetary values.

## Numeric literals and the Value function

Numeric literals are the numbers that you put into formulas.  For example, the five arguments in the formula `Max( 4e-2, -0.4, 4, -4.01, 4.1e3 )` are each a numeric literal in Power Fx.  And each of these results in a **Decimal** value.  If you think about it, you express these numbers using Unicode characters *in decimal* within the formula, so the **Decimal** representation is the best translation that preserves the full precision of the original.

Likewise, the **Value** function also translates numbers, also entered by a user in a decimal format, into numbers that Power Fx can work with.  And for the same reason, the **Value** function returns a **Decimal** number.  For example, `Value( "5.423" )` returns `5.423` as a **Decimal** value.

**Float** literals can be created with the **Float** function.  For example, `Float( 1.234 )` will convert the **Decimal** `1.234` into a floating point value.  If the exponent required is beyond the range of **Decimal**, a string can be passed instead, as in `Float( "1.234e200" )`.  The **Float** function with a string is also how values entered by users are converted to **Float**.

Let's look at some examples:

| Formula | Type | Value | Description |
|---------|-------|------|-------------|
| `1`      | Decimal | `1`     | Common integers are easily represented by **Decimal**.
| `0.1`     | Decimal | `0.1`   | Using base 10 math, this fraction is easily represented by **Decimal**.
| `1e3`     | Decimal | `1000`  | Scientific notation is supported with **Decimal** numbers, up to the maximum range of a **Decimal**. |
| `1e200`   | Decimal | *error (ErrorKind.Numeric)* | This large exponent is beyond the range of **Decimal**. |
| `Value("1")` | Decimal | `1`  | Common integers are easily represented by **Decimal**.
| `Value("0.1")` | Decimal | `0.1` | Using base 10 math, this fraction is easily represented by **Decimal**.
| `Value("1e3")` | Decimal | `1000` | Scientific notation is supported with **Decimal** numbers, up to the maximum range of a **Decimal**. |
| `Value("1e200")` | Decimal | *error (ErrorKind.Numeric)* | This large exponent is beyond the range of **Decimal**. |
| `Float(1)` | Float | `1` | Whole numbers are exactly representable with **Float** |
| `Float(0.1)` | Float | `0.10000000000000000555` | Because **Float** is not base 10 based, this number cannot be exactly represented.  This may seem like a small error, but if this number is used in repeated calculations, the small error could bill and become noticeable. |
| `Float(1e3)` | Float | `1000` | Again, to a point, whole number are exactly representable with **Float** |
| `Float(1e200)`  | Float | *error (ErrorKind.Numeric)*| This starts out as a **Decimal** number which is converted to a **Float**.  Because the **Decimal** is out of range, an error is returned. |
| `Float("1e200")` | Float | `9.999999999999999e199` | This large exponent is representable with a **Float**, but it is beyond the safe integer limits and so has been rounded. | Float |

## Type precedence  

**Float** has higher type priority than **Decimal**.  Which means if you mix **Float** and **Decimal** in a mathematical operation, the result will be **Float**.  Operations between only **Decimal** quantities result in **Decimal**, and operations between only **Float** quantities result in **Float**.

For this reason, many operations and functions that could return either data type will return **Decimal** to favor the lower priority type and not force calculations to needlessly promote to **Float**.

Let's look at some examples.  **Title1** refers to a control in Power Apps, with a **Y** property of 44 and a **Height** property of 36, both of which are of type **Float**.  

| Formula | Type | Value | Description |
|---------|-------|------|-------------|
| `1+1`   | Decimal | `2`  | Both of these numeric literals are **Decimal**, so the result is **Decimal**. |
| `3*0.1` | Decimal | `0.3` | Both of these numeric literals are **Decimal**, so the result is **Decimal**.
| `Sum(0.1,0.1,0.1)` | Decimal | `0.3` | All of the arguments to **Sum** are **Decimal**, and it is one of the functions that can natively operate on **Decimall** values, so the result is also **Decimal**. |
| `Float("1e200") * 2` | Float | `1.9999999999999998e199` | The **Float** function returns a floating point version of the string passed to it.  Since it has higher type precedence than the literal **2** which is of type **Decimal**, the **2** is promoted to floating point and used in the calculation.  The result is **Float**.
| `Title1.Y + Title1.Height + 4` | Float | `84` | Power Apps control properties are usually **Float**, because approximations are fine and it is higher performance.  The literal **4** is promoted to **Float** for the calculation.  Since all the quantities are whole numbers, and **Float** can represent common whole numbers exactly, there is no rounding errors. |

## Object properties

As we've seen in the last examples, the type of object properties can have a large impact on how calculations are performed.  

For example, Power Apps uses formulas to calculate position, size, and other control properties.  Since these calculations do not need to be exact, and there are a great many of them in an app, it is best if these calculations are done in high performance **Float**.  

To make this happen, almost all numeric control properties in Power Apps are typed **Float**.  For example, the formula `Parent.X + Margin - (Self.Width / 2)` will be calculating using high performance **Float**, despite including a **Decimal** literal (`2`) and a possible **Decimal** variable (`Margin`) both of which would be automatically promoted to **Float**.

The only numeric control properties that make sense as **Decimal** are those that might be used in a business calculation.  For example, if a Power Apps slider control was used to determine the quantity of products for an order, it would be best if the formula `Slider1.Value * 'Unit price'` stayed in **Decimal**.  For this reason, a hand full of properties may use the lower precedence **Decimal**.

## Integer database fields

Likewise, the types of numeric database fields have a large impact on the type of calculation used.  

It is common to store whole numbers in databases using an integer data types, in various sizes from 8 to 64 bits.  Power Fx brings all of these in as **Decimal** values.  This keeps calculations on the lower precedence and higher precision representation as long as possible and consistently supports 64-bit integers which cannot be exactly handled with **Float**.

For example, consider an **Order details** table with a **Quantity** field that is a whole number in the database, and a **Unit price** that is a decimal in the database.  A common formula would be `ExtendedPrice = Quantity * 'Unit Price'`.  If **Quantity** was brought in as **Float**, then this entire formula would promote to **Float** and we would not get the answer we expect.  This undesirable promotion is avoided by bringing **Quantity** in a **Decimal**.

In general, **Decimal** is used by Power Fx to hold all numbers coming from data sources, except those that are explicitly typed as IEEE floating point for which **Float** is used.

## Form processing

Staying in **Decimal** is also true if a form control is used.  A **Decimal** value should be able to round-trip through a form control without loss of precision.

In Power Apps Canvas, editing **Unit price** is done by feeding the current value to a **TextInput** control's **Default** property, which is of type **Text**.  The output of the **TextInput** control is the **Text** property, which is used with the **Value** function to turn the number back into a **Decimal** type for storage back in the database.  Throughout this entire process, **Unit price** was only converted between different base 10 representations with no loss of precision.

## Functions and coercion

**Decimal** can automatically coerce to **Float** and vice versa, as needed.  They can also be explicitly coerced between each other with the **Value** and **Float** functions.

Common functions and operators that could lead to a loss of precision natively support either type.  These include:
- Arithmetic: `+`, `-`, `*`, `/`, **Mod**, `%`
- Comparison: `<`, `>`, `=`, `<>`, `>=`, `<=`
- Aggregates: **Sum**, **Max**, **Min**, **Average**, **StdevP**, **VarP**
- Rounding: **Round**, **RoundDown**, **RoundUp**, **Int**, **Trunc**
- Miscellaneous: **Text**, **Abs**, **JSON**, **ParseJSON**

All other functions will coerce **Decimal** to **Float** for their arguments.  If it is a mathematical function, such as Sqrt or Cos, the result will be **Float**, which is usually fine as these functions return approximations if the result is not a whole number, and whole numbers can be exactly represented by **Float**.  If it is a function that takes whole numbers as arguments, such as Mid or Char, there is no problem translating to **Float** before making the call.

**StdevP** is a hybrid function.  It will perform internal calculations using **Decimal** but will do the final square root operation in **Float**, and so this function returns **Float**.

## ParseJSON and JSON functions

JavaScript and JSON has no decimal data type.  Many users of decimal in JSON will wrap the decimal number in a string to avoid floating point rounding of the number by JSON parsers.  

When used with an untyped object, the **Value** function will accept a number in one of three formats:
- JSON number literal: `{ "Value": 1.234 }`.  Despite using a floating point notation, the precision of the original number will be honored to the degree possible, meaning that `{ "Value": 1.2345678912345678901 }` will retail all decimal places, despite being beyond the precision of a standard JavaScript floating point number.
- JSON string literal containing a number: `{ "Value": "1.234" }`.  The number must use dot as the decimal separator have no other punctuation such as thousands separators.
- JSON BigInt literal: `{ "Value": 1234n }`.

The **Float** function will only accept a JSON number literal.

When using the JSON function, **Decimal** values will be emitted as JSON string literals containing a number.  **JSONFormat.DecimalAsNumber** can be used to force JSON number literals to be used instead, and the full precision of the **Decimal** will be included in the JSON string and not rounded to floating point precision.
