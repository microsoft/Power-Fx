# Expression Tests

This directory contains Power Fx expression tests in a simple .txt file format.

Tests are in the form:
```
>> test expression
test result or #SKIP directive
blank line
```

Test expression can be multiple lines using significant white space at the beginning of continuation lines (like Python and YAML).

## Configuration testing

These tests are run with different configurations of features and parser options.
Most tests are not impacted by these differences and should be placed in a top level file
with no suffix and few #SEUTP directives for features that are not
directly being tested.

There may be some tests that are sensitive to the configuration.  These should be broken
out into their own file and the suffix reflects roughly the configuration that is needed.

For example:
- **MinMax.txt**: Tests common Min and Max function scenarios that are not sensitive to the configuration.  
  For example, `Max(1,2)` is always `2`.
- **MinMax_NumberIsFloat.txt**: Tests that are specific to floating point operation and would not work properly with
  Decimal numbers.  For example, `Max(1e300,1.1e300)`.  This file contains a `#SETUP: NumberIsFloat` directive.
- **MinMax_V1Compat.txt**: Tests that have the old behavior before PowerFxV1CompatibilityRules was introduced.
  This file contains a `#SETUP: PowerFxV1CompaitilibyRules` to prevent it from being run if that switch is disabled.
- **MinMax_V1CompatDisabled.txt**: Tests for the new PowerFxV1CompatibilityRules behavior.
  This file contains a `#SETUP: disable:PowerFxV1CompaitilibyRules` to prevent it from being run if that switch is disabled.

## File Directives

File directives appear at the top of the file.

Each direcive may be followed by an end of line comment.

Multiple directives are allowed in a file, except for #OVERRIDE.

### SETUP

```
#SETUP: handler | [disable:]flag
```

Specifies the setup handler, engine Features, and ParserOption flags required by this test.

If the test environment cannot satisfy the needs of #SETUP, the test file is skipped.

It is OK to turn on flags that are not directly being tested.  For example, `#SETUP: TableSyntaxDoesntWrapRecords` is common 
as many tests are written using [ ] notation for defining tables.

### OVERRIDE

```
#OVERRIDE: filename
```

Test results or #SKIP in this file override the same test in the named file.

### DISABLE

```
#DISABLE: filename
```

Disables a test file.  All #DISABLE directives are handled before any tests are run.

## Test Directives

### SKIP

```
#SKIP: comment
```

Use this directive in place of a result to skip a test.
Comment should include why the test was skipped, ideally with a link an issue filed for repair and the expected result after the repair.

For example
```
>> Abs(Table({a:1/0},{a:Power(-3,2)}))
#SKIP: waiting on https://github.com/microsoft/Power-Fx/issues/1204 expected: Table({Value:Error({Kind:ErrorKind.Div0})},{Value:9})
```

## Numbers

Since Power Fx works with both Decimal and Float numbers, when not directly testing the limits of these,
it is best to pick numbers in the range -1e28 to 1e28 and to not have infinitely repeating results.  
For example, instead of testing with 1/3 which has a different precision in decimal and floating point, stick to
1/4, 1/8, or 1/2 that have an exact representation within 12 decimal places.

Do not use the Float or Decimal functions at this time.  Not all hosts have these functions yet.
If writing a test specifically for NumberIsFloat, use the Value function instead.  
All the tests are run with both Decimal and Float configurations, so there is no need to have tests
that vary on just this one difference.
