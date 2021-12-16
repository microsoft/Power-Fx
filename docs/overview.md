<!-- 
Copyright (c) Microsoft Corporation.
Licensed under the MIT license.
-->

# Microsoft Power Fx overview

*NOTE: Microsoft Power Fx is the new name for canvas apps formula language.  This overview article and the associated articles are a work in progress as we extract the language from canvas apps, integrate it with other products of the Power Platform, and make it available as open source.  To learn more about and experience the language today, start with [get started with formulas in Power Apps](https://docs.microsoft.com/powerapps/maker/canvas-apps/working-with-formulas) and sign up for a free [Power Apps trial](https://powerapps.microsoft.com).*

Microsoft Power Fx is the low-code language that will be used across the Microsoft Power Platform. It is a general-purpose, strong typed, declarative, and functional programming language.

Power Fx is expressed in a human-friendly text. It is a low-code language that makers can work with directly in an Excel-like formula bar or Visual Studio Code text window. The "low" is due to the concise and simple nature of the language, making common programming tasks easy for both makers and developers.  It enables the full spectrum of development from "no-code" for those who have never programmed before to "pro code" for the seasoned professional, with no learning or rewriting cliffs in between, enabling diverse teams to collaborate and save time and expense. 

Power Fx binds objects together with declarative spreadsheet-like formulas. For example, think of a UI control's **Visible** property as a spreadsheet cell, with an associated formula that calculates its value based on other control's properties. The formula logic recalculates automatically similar to how a spreadsheet does which affects the control's visibility.  

Also, Power Fx offers imperative logic when needed. Spreadsheets don't typically have buttons that can submit changes to a database, but apps often do. The same expression language is used for both declarative and imperative logic.

Power Fx will be made available as an open-source. It is currently integrated into canvas apps where you can experience it today.  We are in the process of extracting it from Power Apps and using it in other Power Platform products and as an open-source.  More information: [Power Fx open-source](https://github.com/microsoft/power-fx).  

This article is an overview of the language and its design principles. To learn more about Power Fx, see the following articles:

- [Data types](data-types.md)
- [Operators and identifiers](operators.md)
- [Tables](tables.md)
- [Variables](variables.md)
- [Imperative logic](imperative.md)
- [Global support](global.md)
- [Expression grammar](expression-grammar.md)
- [YAML formula grammar](yaml-formula-grammar.md)

## Think spreadsheet

What if you could build an app as easily as you build a spreadsheet?

What if you could leverage your existing spreadsheet knowledge?

These were the questions that inspired the creation of Power Apps and Power Fx.  Hundreds of millions of people create spreadsheets with Excel every day. Let’s bring app creation to them that is easy and leverages Excel concepts that they already know. By breaking Power Fx out of Power Apps, we are going to answer these questions for building automation, or a virtual agent, or other domains.

All programming languages, including Power Fx, have expressions: a way to represent a calculation over numbers, strings, and other data types. For example, `mass * acceleration` in most languages expresses multiplication of `mass` and `acceleration`.  The result of an expression can be placed in a variable, used as an argument to a procedure, or nested in a bigger expression.

Power Fx takes this a step further. An expression by itself says nothing about what it is calculating. It is up to the maker to place it in a variable or pass it to a function. In Power Fx, instead of only writing an expression that has no specific meaning, one writes a *formula* that binds the expression to an identifier.  One writes `force = mass * acceleration` as a formula, in the mathematical sense, for calculating `force` that is always true.  As `mass` or `acceleration` changes, `force` automatically updates to a new value.  An expression describes a calculation, a formula gives that calculation a name and uses it as a recipe. This is why we refer to Power Fx as a formula language.

For example, this formula from [Stack Overflow](https://stackoverflow.com/questions/350264/how-can-i-perform-a-reverse-string-search-in-excel-without-using-vba) searches a string in reverse order.  In Excel, it looks like this:

![Excel with the formula A2=Right(A1,Len(A1)-Find("|",Substitute(A1," ","|",Len(A1-Len(Substitute(A1," ",""))))), A1 has the value "Hello World! It is great to meet you!" and A2 shows the calculated value "you!"](media/overview/reverse-search-excel.png "Reverse search")

Power Fx works with this same formula, with the cell references replaced with control property references:

![Power Fx with the formula Label.Text=Right(Input.Text,Len(Input.Text)-Find("|",Substitute(Input.Text," ","|",Len(Input.text-Len(Substitute(Input.Text," ",""))))), Input shows typing of "Hello World! It is great to meet you!" and Label shows reverse search wit the final result being the same as Excel with "you!"](media/overview/reverse-search-power-apps.gif "Power Fx reverse search")

As the `Input` control value is changed, the `Label` control automatically recalculates the formula and shows the new value. There are no `OnChange` event handlers here as would be common in other languages.

Another example that uses a formula for the `Fill` color of the screen.  As the sliders that control Red, Green, and Blue are changed, the background color automatically changes, as it is being recalculated.

![Power Apps canvas with the formula Screen.Fill=RGBA(RedSlider.Value, GreenSlider.Value, BlueSlider.Value, 100%), as the color sliders are moved the formula recalcs and the background color changes](media/overview/color-sliders-power-apps.gif "Color slider")

There are no `OnChange` events for the slider controls as would be common in other languages.  There is no way to explicitly set the `Fill` property value at all. If the color isn’t working as expected, you need to look at this one formula to understand why is it not working. You don’t need to search through the app to find a piece of code that sets the property at an unexpected time. There is no time element, the correct formula values are always maintained.

As the sliders are set to a dark color, the labels for Red, Green, and Blue change to white to compensate.  This is done through a simple formula on each label control's `Color` property.

![Power Apps canvas with the labels for the three slider controls selected, each with the formula Color=If(BlueSlider.Value+GreenSlider.Value+RedSlider.Value < 150, White, Black), and each of the labels showing as Black as the sliders together are beyond the 150 bright threshold.](media/overview/color-sliders-power-apps-labels.png "Power Fx color sliders")

What's great about this is that it is isolated from what is happening for the `Fill` color;  these are two entirely different calculations.  Instead of large monolithic procedures, Power Fx logic is typically lots of smaller formulas that are independent.  That's easier to understand and enables enhancements without disturbing existing logic.

Power Fx is a declarative language, just as Excel is. The maker defines what behavior they want, but it is up to the system to determine and optimize how and when to accomplish it. To make that practical, most work is done through pure functions without side effects, making Power Fx also a functional language, again just as Excel is.

*NOTE: In this article, we refer to makers when referring to a feature that could be used by either end of the spectrum.  We refer to the user as a developer if the feature is more advanced and is beyond the scope of an Excel user.*


## Always live

A defining aspect of spreadsheets is that they are always live and changes are reflected instantaneously.  There is no compile or run mode in a spreadsheet. When a formula is modified or a value is entered, the spreadsheet immediately recalculates to reflect the changes.  Any errors detected are surfaced immediately and don’t interfere with the rest of the spreadsheet.

The same thing is implemented with Power Fx as well. An incremental compiler is used to continuously keep the program and the data it is operating on in sync. Changes automatically propagate through the program's graph, affecting the results of dependent calculations, which may drive properties on controls such as color or position. The incremental compiler also provides a rich formula editing experience with IntelliSense, suggestions, autocomplete, and type checking.

In the animation below, the order number is displayed in a label control dependent on the slider control, even though there are two errors on the labels below it.  The app is very much alive and interactive.  The first attempt at fixing the formula with `.InvalidName` results in an immediate red line and error displayed as it should, but the app keeps running.

![In Power Apps canvas, a slider is moved that updates the order number label, even though there are errors present.  As formulas are typed in, data sources are automatically added and the app keeps running, adding the additional information.  At no time does the maker need to "run" the app, it is always running.](media/overview/always-live.gif "Always live")

When `.Employee` is typed in. This causes the `Data` pane to add the Employees table, metadata for this table is retrieved, and suggestions for columns are immediately offered.  We just walked across a relationship from one table to another and the system made the needed adjustments to the app’s references. The same thing happens when adding `.Customer`.

After each change, the slider continues with its last value and any variables retain their value.  Throughout, the order number has continued to be displayed in the top label as it should.  The app has been live, processing real data, the entire time.  We can save it, walk away, and others can open and use it just like Excel. There is no build step, no compile, there is only a publish step to determine which version of the app is ready for users.

## Low code

Power Fx describes business logic in concise, yet powerful, formulas.  Most logic can be reduced to a single line with plenty of expressiveness and control for more complex needs.  The goal is to keep the number of concepts a maker needs to understand to a minimum, ideally no more than an Excel user would already know.

For example, to look up the first name of the employee for an order, one would write the following Power Fx.  Beyond Excel concepts, the only added concept used here is **"."** notation for drilling into a data structure, in this case for `.Employee.'First Name'`.  The gif shows the mapping between the parts of the Power Fx formula and the concepts that need to be explicitly coded in the equivalent JavaScript.

![Low code JavaScript](media/overview/low-code-javascript.gif "Low code JavaScript")

Let’s look more in-depth at all the things that Power Fx is doing for us and the freedom it has to optimize because the formula was declarative:

- **Asynchronous**:  All data operations in Power Fx are asynchronous. The maker doesn’t need to specify this, nor does the maker need to synchronize after the call is over.  The maker doesn’t need to be aware of this concept at all, they don’t need to know what a promise or lambda function is.
Local and remote.  Power Fx uses the same syntax and functions for data that is local in-memory and remote in a database or service.  The user need not think about this distinction.  Power Fx automatically delegates what it can to the server to process filters and sorts there more efficiently.

- **Relational data**:  Orders and Customers are two different tables, related through a many-to-one relationship.  The OData query requires an "$expand" with knowledge of the foreign key, similar to a Join in SQL.  The formula has none of this, in fact, database keys are another concept the maker doesn’t need to know about.  The maker can use simple dot notation to access the entire graph of relationships from a record.

- **Projection**:  When writing a query, many developers write `select * from table` that brings back all the columns of data. Power Fx analyzes all the columns that are used through the entire app, even across formula dependencies.  Projection is automatically optimized and again maker needs not even know what that word means.

- **Retrieve only what is needed**:  In this example, the `LookUp` function implies that only one record should be retrieved and that is all that is returned. If more records are requested by using the `Filter` function, for which thousands of records may qualify, only a single page of data is returned at a time, on the order of 100 records.  The user must gesture through a gallery or data table to see more data, and it is automatically brought in for them.  The maker can reason about large sets of data without needing to think about limiting data requests to reasonable chunks.

- **Runs only when is needed**:  We defined formula for the `Text` property of the label control.  As the variable selected changes, the `LookUp` is automatically recalculated and the label is updated. Maker did not need to write an OnChange handler for Selection, needing to remember that this label is dependent upon it.  This is declarative programming as discussed earlier, the maker specified what they wanted to have in the label, not how or when it should be fetched.  If this label is not visible because it is on a screen that is not visible or its `Visible` property is false, we can defer this calculation until the label is visible and effectively eliminate it if that rarely happens.

- **Excel syntax translation**:  Excel is used by many users, most of which know that **&** is used for string concatenation.  JavaScript uses **+** and other languages use **.**
   
- **Display names and localization**:  `First Name` is used in the Power Fx formula while `nwind_firstname` is used in the JavaScript equivalent.  In Dataverse and SharePoint, there is a display name for columns and tables as well as a unique logical name.  The display names are often much more user-friendly, as in this case, but they have another important quality that they can be localized.  If you have a multi-lingual team, each team member can see table and field names in their own language.  In all the use cases, Power Fx makes sure that the correct logical name is sent to the database automatically.

## No code

One does not need to read and write Power Fx to start expressing logic.  There are lots of customizations and logic that can be expressed through simple switches and UI builders.  We build these “no-code” tools to read and write Power Fx to ensure there is plenty of headroom for someone to take it further, acknowledging that “no-code” tools will never offer all the expressiveness of the full language.  Even when used with “no-code” builders, we have deliberately kept the formula bar front and center in Power Apps to educate the maker on what we are doing on their behalf so they can begin to learn Power Fx.

Let’s take a look at some examples.  In Power Apps, the property panel provides “no-code” switches and knobs for the properties of the controls.  In practice most property values are static.  We’ll use the color builder to change the background color of the `Gallery`.  What you will notice is that the formula bar reflects this change, updating the formula to a different `RGBA` call.  At any time, the maker can go to the formula bar and take this a step further. In this example using `ColorFade` to adjust the color.  The color property still shows in the properties panel, but an fx appears on hover and the maker is directed to the formula bar.  This is fully two ways, removing the `ColorFade` call returns the color to something the property panel can understand and we can again use it to set a color.

![No code color](media/overview/no-code-color.gif "No code color")

Here’s a more complicated example.  The gallery is showing a list of Employees from Dataverse.  Dataverse provides views over table data.  We can select one of these and the formula is changed to use the `Filter` function with this view name.  The two dropdowns can be used to dial in the correct table and view without touching the formula bar.  But, as in this example, let’s say you want to go further and add a sort.  We can do that in the formula bar and the property panel again shows an fx icon and directs modifications to the formula bar.  And again, if we simplify the formula to something the property panel can read and write, it again can be used.

![No code sort](media/overview/no-code-sort.gif "No code sort")

These have been simple examples.  We believe Power Fx makes a great language for describing no code interactions.  It is concise, powerful, and easy to parse, and provides the headroom that is so often needed with “no cliffs” up to low-code.

## Pro code

Low-code makers build things that sometimes require the help of an expert or are taken over by a professional developer to maintain and enhance.  Professionals also appreciate that low-code development can be easier, faster, and less costly than building a professional tool.  Not every situation requires the full power of Visual Studio.

Professionals want to use professional tools to be most productive. Power Fx formulas can be stored in [YAML source files](yaml-formula-grammar.md) that are easy to edit using Visual Studio Code, Visual Studio, or any other text editor and enables Power Fx to be put under source control with GitHub, Azure DevOps, or any other source code control system.

![Pro code vscode](media/overview/pro-code-vscode.gif "Pro code vscode")

![Pro code GitHub](media/overview/pro-code-github.gif "Pro code GitHub")

Power Fx supports formula-based components for sharing and reuse.  A few weeks ago we announced support for parameters to component properties, enabling the creation of pure user-defined functions with more enhancements on the way.

Also, Power Fx is great at stitching together components and services built by professionals.  Out of the box connectors provide access to hundreds of data sources and web services, custom connectors enable Power Fx to talk to any REST web service, and code components enable Power Fx to interact with fully custom JavaScript on the screen and page.

## Design principles 

### Simple

Power Fx is designed to target *maker* audience who is not trained as developers.  Wherever possible, we use the knowledge that this audience would already know or can pick up quickly.  The number of concepts required to be successful is kept to a minimum.

Being simple is also good for developers. For the developer audience, we aim to be a low-code language that cuts down the time required to build a solution.

### Excel consistency

The Power Apps language borrows heavily from the Excel formula language.  We seek to leverage as much Excel knowledge and experience from the many makers who also use Excel. Types, operators, and function semantics are as close to Excel as possible.

If Excel does not have an answer, we next look to SQL. After Excel, SQL is the next most commonly used declarative language and can provide guidance on data operations and strong typing that Excel does not.

### Declarative

The maker describes "what" they want their logic to do, not exactly "how" or "when" it is to be done.  This allows the compiler to optimize by performing operations in parallel, deferring work until needed, pre-fetching and reusing cached data, etc.

For example, in an Excel spreadsheet, the author defines the relationships between cells but Excel decides when and in what order formulas are evaluated.  Similarly, formulas in an app can be thought of as "recalcing" as needed based on user actions, database changes, or timer events.  

### Functional

We favor pure functions without side effects. This results in logic, which is easier to understand and gives the compiler the most freedom to optimize.

Unlike Excel, apps by their nature do mutate state.  For example, apps have buttons that save changes to the record in a database.  Some functions, therefore, do have side effects, although we limit this as much as is practical. 

### Composition 

Where possible, functionality is added that composes well with existing functionality.  Powerful functions can be decomposed into smaller parts that can be more easily used independently.

For example, a **Gallery** control does not have separate `Sort` and `Filter` properties.  Instead, the `Sort` and `Filter` functions are composed together into a single `Items` property. User interface for expressing `Sort` and `Filter` behavior is layered on top of the `Items` property with a two-way editor for this property.

### Strongly typed 

The types of all the values are known at compile time.  This allows for the early detection of errors and rich suggestions while authoring. 

Polymorphic types are supported, but before being used, their type must be pinned to a static type and that type must be known at compile time.  The **IsType** and **AsType** functions are provided for testing and casting types.

### Type inference

Types are derived from their use without being declared. For example, setting a variable to a number results in that variable's type being established as a number.

Conflicting type usage results in a compile-time error.

### Locale-sensitive decimal separators

Some regions of the world use a **.** (dot or period) as the decimal separator while others use a **,** (comma).  This is what Excel does too.  This is commonly not done in other programming languages, using a canonical **.** as the decimal separator for all users worldwide.  To be as approachable as possible for makers at all levels, it is important that `3,14` is a decimal number for a person in France who has used that syntax all their lives.  

The choice of decimal separator has a cascading impact on the list separator, used for function call arguments, and the chaining operator:

| Author's language decimal separator | Power Fx decimal separator | Power Fx list separator | Power Fx chaining operator |
| --- | --- | --- | --- |
| **.** (dot or period) |**.** (dot or period) |**,** (comma) |**;** (semi-colon) |
| **,** (comma) |**,** (comma) |**;** (semi-colon) |**;;** (double semi-colon) |

See [global support](global.md) for more details.

### Not object oriented 

Excel is not object-oriented and neither is Power Fx.  

For example, in some languages, the length of a string is expressed as a property of the string such as `"Hello World".length` in JavaScript.  Excel and Power Apps instead express this in terms of a function as `Len( "Hello World" )`. 

Components with properties and methods are object-oriented and Power Fx easily works with them. But where possible we prefer a functional approach.  

### Extensible 

Makers can create their components and functions using the formula language itself. Developers can create their components and functions by writing JavaScript.

### Developer friendly 

Although makers are our primary target, we try to be developer-friendly wherever possible. If it would not conflict with the above principles, we do things in such a way that a developer would appreciate.  

For example, Excel had no answer for comments, so we use C like line and inline comments.

### Language evolution 

Evolving programming languages forward is both necessary and tricky. Everyone, rightfully so, is concerned that a change no matter how well-intentioned could break existing code and require users to learn a new pattern.  Power Fx takes backward compatibility seriously, but we also strongly believe that we won’t always get it right the first time and we will collectively learn what is best as a community.  We must evolve and Power Fx designed support for language evolution from the very beginning.

A language version stamp is included with every Power Fx document that is saved.  If we want to make an incompatible change, we write what we call a “back compat converter” that rewrites the formula automatically the next time it is edited.  If it is something major that we need to educate the user about, we’ll also display a message with a link to the docs.  Using this facility, we can still load apps built with the preview versions of Power Apps from many years ago despite all the changes that have occurred since then.

For example, we introduced the `ShowError` function to display an error banner with a red background. 

![Show error](media/overview/showerror-2018.png "Show error")

Users loved it, but they also asked us for a way to show a success banner (green background) or an informational banner (blue background).  So, we came up with a more generic `Notify` function that took a second argument for the kind of notification.  We could have just added `Notify` and kept `ShowError` the way it was.  But we didn’t, we replaced `ShowError` with `Notify`. We removed a function that had previously been in production with something else. Because there would have been two ways to do the same thing, it would have caused confusion especially for new users, and most importantly it added complexity. Nobody complained, everybody, appreciated the change, and then moved on to their next Notify feature. For all the users we may have now, there are so many more to come, let’s get it right for them.

This is how the same app looks when loaded into the latest version of Power Apps. No action was required by the user to make this transformation happen, it was automatic upon open:

![New show error](media/overview/showerror-2021.png "New show error")

With this facility, Power Fx may evolve faster and more aggressively than most programming languages.  

### No undefined value

Some language such as JavaScript uses the concept of an "undefined" value for uninitialized variables or missing property. For simplicity's purpose, we have avoided this concept, treating instances that would be undefined elsewhere as either an error or a blank value.  For example, all uninitialized variables start with a blank value.  All data types can take on the value of blank.

## Related articles

[Data types](data-types.md)<br>
[Operators and identifiers](operators.md)<br>
[Tables](tables.md)<br>
[Variables](variables.md)<br>
[Imperative logic](imperative.md)<br>
[Global support](global.md)<br>
[Expression grammar](expression-grammar.md)<br>
[YAML formula grammar](yaml-formula-grammar.md)
