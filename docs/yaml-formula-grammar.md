<!-- 
Copyright (c) Microsoft Corporation.
Licensed under the MIT license.
-->

# YAML formula grammar

*NOTE: Microsoft Power Fx is the new name for canvas apps formula language.  These articles are work in progress as we extract the language from canvas apps, integrate it with other products of the Power Platform, and make available as open source.  Start with the [Power Fx Overview](overview.md) for an introduction to the language.*

Microsoft Power Fx has a [well-established grammar for expressions](expression-grammar.md) based on Excel. However, when used in Power Apps and other hosts where UI provides the name-to-expression binding for a formula, there is no standard way of editing the formula bindings as text.  

We have selected the industry standard [YAML](https://yaml.org/spec/1.2/spec.html) as our language for this binding. There are already a large number of editors, tools, and libraries for working with YAML.  This article describes how we represent formulas in YAML.

At this time, we support only a restricted subset of YAML. Only the constructs described in this article are supported.  

This is very much a work in progress and may change. Not everything that defines a canvas app is represented here, additional information flows through other files that the tool produces and consume.

## Leading =

First and foremost, all expressions must begin with a leading equals sign `=`:

```
Visible: =true
X: =34
Text: |
	="Hello, " &
	"World"
```

We use the `=` in this manner for three reasons:

- It is consistent with Excel's usage of a leading `=` to bind an expression to a cell.
- It effectively escapes the formula language's syntax so that YAML does not attempt to parse it.  Normally, YAML would treat `text: 1:00` as minutes and seconds, converting to a number.  By inserting an `=`, YAML will not use its implicit typing rules and formulas will not be unharmed.  Using `=` covers most cases, but not all, and those exceptions are described below under *Single-line formulas*.
- In the future, we can support both formulas (starts with `=`) and non-formulas (no `=`) in the same file, just as Excel does.  We can do this in YAML and non-YAML files alike across in source files of the Power Platform.  Anywhere a formula is supported, the leading `=` differentiates a Power Apps formula expression from a static scalar value.

## Single-line formulas

Single-line formulas are written in the form:

*Name* `:` `SPACE` `=` *Expression*

The space between the colon and the equals sign is required to be YAML-compliant.  The equals sign disrupts YAML's normal interpretation of the expression, allowing the rest of the line to be interrupted as the formula language. 

For example:

```
Text1: ="Hello, World"
Text2: ="Hello " & ", " & "World"
Number1: =34
Boolean1: =true
Time1: =1:34
```

The pound sign `#` and colon `:` are not allowed anywhere in single-line formulas, even if they are in a quoted text string or identifier name. To use a pound sign or colon, the formula must be expressed as a multi-line formula.  The pound sign is interpreted as a comment in YAML and the colon is interpreted as a new name map in YAML.  To add a comment to a single-line comment, use the formula language's line comment starting with `//`.

Using normal YAML escaping with single quotes and C-like backslashes is not supported.  If these would be required, us a multi-line formula instead.  We do this for consistency and to facilitate cut/paste between the formula bar in Power Apps Studio and YAML source files.

See the canvas apps [operators and identifiers](https://docs.microsoft.com/powerapps/maker/canvas-apps/functions/operators) documentation for details on allowed names and the structure of an expression.

## Multi-line formulas

Formulas can span multiple lines using YAML's block scalar indicators:  

*Name* `:` `SPACE` ( `|` or `|+` or `|-` )
&emsp;`=` *Expression-Line*
&emsp;*Expression-Line*
&emsp;...

All lines that are a part of the block must be indented at least one space in from the level of the first line.

For example:

```
Text1: |
    ="Hello, World"
Text2: |
    ="Hello" &
    "," &
    "World"
```

We accept all forms of YAML multi-line scalar notation on import, including for example `>+`.  However, we will only produce `|`, `|+`, or `|-` to ensure that whitespace is properly preserved.

## Component instance

Components are instanced using YAML object notation. The type of the object is established with the `As` operator as a part of the left-hand side YAML tag.  For container controls, objects can be nested.

*Name*&emsp;`As`&emsp;*Component-Type*&emsp;[ `.`&emsp;*Component-Template* ]&emsp;`:`
&emsp;( *Single-Line-Formula* or *Multi-Line-Formula* or *Object-instance* )
&emsp;...

All lines that are a part of the block must be indented at least one space in from the level of the first line.

For example:

```
Gallery1 As Gallery.horizontalGallery:
    Fill: = Color.White
    Label1 As Label:
        Text: ="Hello, World"
        X: =20
        Y: =40
        Fill: |
            =If( Lower( Left( Self.Text, 6 ) ) = "error:",
                Color.Red,
                Color.Black
            ) 
```

*Component-Type* can be any canvas component or control.  Base types, such as *Number* is not supported.

*Component-Template* is an optional specifier for components that have different templates such as the Gallery.  Not all components will have templates.

If *Name* contains special characters and is wrapped with single quotes, then the entire phrase to the left of the colon will need to be escaped.  This can be done in two ways: 

- Use single quotes to wrap the entire left-hand side, requiring the existing single quotes to be doubled:
    ```
    '''A name with a space'' As Gallery':
    ```
- Use double quotes to wrap the entire left-hand side, but be careful that there are no double quotes in the name:
    ```
    "'A name with a space' As Gallery":
    ```

## Component definition

Similarly, components are defined by creating an instance of one of the supported base types. The base types cannot be instanced directly.  Within an object definition, properties can be added to what the base type provides.

The supported base types are: CanvasComponent

### Simple property definition

Components use properties to communicate with the app in which they are hosted.

*Name* `:` ( *Single-Line-Expression* or *Multi-Line-Expression* )

The type of the formula is implied by the type of the expression.  

For input properties, the expression provides the default to be inserted into the app when the component is instanced.  The app maker can modify this expression as they see fit, but cannot change the type.

For output properties, the expression provides the calculation to be performed.  The app maker cannot modify this expression, it is encapsulated in the component. 

At this time, all properties are data flow only and cannot contain side effects.

At this time, additional metadata about the property is not defined here but is instead in the other files of the `.msapp` file, for example the property's description. 

For example:

```
DateRangePicker As CanvasComponent:
    DefaultStart: |-
		=// input property, customizable default for the component instance
		Now()                      
    DefaultEnd: |-
		=// input property, customizable default for the component instance
		DateAdd( Now(), 1, Days )    
    SelectedStart: =DatePicker1.SelectedDate   // output property
    SelectedEnd: =DatePicker2.SelectedDate     // output property
```

## YAML compatibility 

### YAML comments

YAML's `#` line comments are not preserved anywhere in the source format.  Instead, within a formula, use the formula language's line comments that start with `//` or block comments that are delimited with `/*` and `*/`.  

### Errors for common pitfalls

There are a few places where the formula language and YAML grammars are incompatible or could be confusing for a user.  In these cases, we will throw an error.  

For example, in:

```
Text: ="Hello #PowerApps"
Record: ={ a: 1, b: 2 }
```

the `#` is treated as a comment by YAML even though it is embedded in what Excel would consider a double quoted text string.  In the record case, YAML considers `a:` and `b:` to be another name map binding.  To avoid confusion, we will error on these cases during import.  In these cases, a YAML multi-line form can be used instead.

YAML allows the same name map to be reused, the last silently overriding any previous definitions.  As this can be confusing for a low-code maker and can result in the loss of a property formula, we will error if we see the same name twice.
