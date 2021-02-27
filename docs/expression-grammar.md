---
title: Power Fx Expression Grammar | Microsoft Docs
description: Annotated grammar for the Power Fx language
author: gregli-msft
manager: kvivek
ms.reviewer: nabuthuk
ms.service: powerapps
ms.topic: reference
ms.custom: canvas
ms.date: 02/24/2021
ms.author: gregli
search.audienceType: 
  - maker
search.app: 
  - PowerApps
---
# Power Fx Expression Grammar

Power Fx is based on formulas that binds a name to an expression.  Just like a spreadsheet, as inbound dependencies to the expression change, the expression is recalculated and the value of the name changes, possibly cascading the recalculation into other formulas.  

This grammar covers the expression part of the formula. Binding to a name to create a formula is dependent on how Power Fx is integrated. In spreadsheets, the binding syntax is not exposed and is implied by where the expression is written, for example `typing =B1` in the A1 cell. In some cases, no binding is required at all and Power Fx is used as an expression evaluator, for example in supporting calculated columns of a database table. For Power Apps, the binding is implied when in Power Apps Studio with [a serialization format based on YAML for use outside Power Apps Studio](expression-grammar.md).

## Grammar conventions

The lexical and syntactic grammars are presented using grammar productions. Each grammar production defines a non-terminal symbol and the possible expansions of that non-terminal symbol into sequences of non-terminal or terminal symbols. In grammar productions, non-terminal symbols are shown in italic type, and terminal symbols are shown in a fixed-width font.

The first line of a grammar production is the name of the non-terminal symbol being defined, followed by a colon. Each successive indented line contains a possible expansion of the non-terminal given as a sequence of non-terminal or terminal symbols. For example, the production:

&emsp;&emsp;*GlobalIdentifier* **:**
&emsp;&emsp;&emsp;&emsp;`[@`&emsp;*Identifier*&emsp;`]`
defines a *GlobalIdentifier* to consist of the token `[@`, followed by an *Identifier*, followed by the token `]`

When there is more than one possible expansion of a non-terminal symbol, the alternatives are listed on separate lines. A subscripted suffix "opt" is used to indicate an optional symbol. For example,the production:

&emsp;&emsp;*FunctionCall* **:**
&emsp;&emsp;&emsp;&emsp;*FunctionIdentifier*&emsp;`(`&emsp;*FunctionArguments*<sub>opt</sub>&emsp;`)`

is shorthand for:

&emsp;&emsp;*FunctionCall* **:**
&emsp;&emsp;&emsp;&emsp;*FunctionIdentifier*&emsp;`(`&emsp;`)`
&emsp;&emsp;&emsp;&emsp;*FunctionIdentifier*&emsp;`(`&emsp;*FunctionArguments*&emsp;`)`

Alternatives are normally listed on separate lines, though in cases where there are many alternatives, the phrase "one of" may precede a list of expansions given on a single line. This is simply shorthand for listing each of the alternatives on a separate line. 

For example, the production:

&emsp;&emsp;*DecimalDigit* **:** **one of**
&emsp;&emsp;&emsp;&emsp;`0`&emsp;`1`&emsp;`2`&emsp;`3`&emsp;`4`&emsp;`5`&emsp;`6`&emsp;`7`&emsp;`8`&emsp;`9`

is shorthand for:

&emsp;&emsp;*DecimalDigit* **:** 
&emsp;&emsp;&emsp;&emsp;`0`
&emsp;&emsp;&emsp;&emsp;`1`
&emsp;&emsp;&emsp;&emsp;`2`
&emsp;&emsp;&emsp;&emsp;`3`
&emsp;&emsp;&emsp;&emsp;`4`
&emsp;&emsp;&emsp;&emsp;`5`
&emsp;&emsp;&emsp;&emsp;`6`
&emsp;&emsp;&emsp;&emsp;`7`
&emsp;&emsp;&emsp;&emsp;`8`
&emsp;&emsp;&emsp;&emsp;`9`

### Lexical analysis 
The lexical-unit production defines the lexical grammar for a Power Fx expression. Every valid Power Fx expression conforms to this grammar.

&emsp;&emsp;<a name="ExpressionUnit"></a>*ExpressionUnit* **:**
&emsp;&emsp;&emsp;&emsp;*[ExpressionElements](#ExpressionElements)*<sub>opt</sub>

&emsp;&emsp;<a name="ExpressionElements"></a>*ExpressionElements* **:**
&emsp;&emsp;&emsp;&emsp;*[ExpressionElement](#ExpressionElement)*
&emsp;&emsp;&emsp;&emsp;*[ExpressionElement](#ExpressionElement)*&emsp;*[ExpressionElements](#ExpressionElements)*<sub>opt</sub>

&emsp;&emsp;<a name="ExpressionElement"></a>*ExpressionElement* **:**
&emsp;&emsp;&emsp;&emsp;*[Whitespace](#Whitespace)*
&emsp;&emsp;&emsp;&emsp;*[Comment](#Comment)*
&emsp;&emsp;&emsp;&emsp;*[Token](#Token)*

At the lexical level, a Power Fx expression consists of a stream of *Whitespace*, *Comment*, and *Token* elements. Each of these productions is covered in the following sections. Only *Token* elements are significant in the syntactic grammar.

### Whitespace 

Whitespace is used to separate comments and tokens within a PowerApps document. 

&emsp;&emsp;<a name="Whitespace"></a>*Whitespace* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode Space separator (class Zs)
&emsp;&emsp;&emsp;&emsp;any Unicode Line separator (class Zl)
&emsp;&emsp;&emsp;&emsp;any Unicode Paragraph separator (class Zp)
&emsp;&emsp;&emsp;&emsp;Horizontal tab character (U+0009)
&emsp;&emsp;&emsp;&emsp;Line feed character (U+000A)
&emsp;&emsp;&emsp;&emsp;Vertical tab character (U+000B)
&emsp;&emsp;&emsp;&emsp;Form feed character (U+000C)
&emsp;&emsp;&emsp;&emsp;Carriage return character (U+000D)
&emsp;&emsp;&emsp;&emsp;Next line character (U+0085)

### Comments 

Two forms of comments are supported: 
- Single-line comments that start with the characters // and extend to the end of the source line. 
- Delimited comments that start with the characters /* and end with the characters */. Delimited comments may span multiple lines.

&emsp;&emsp;<a name="Comment"></a>*Comment* **:**
&emsp;&emsp;&emsp;&emsp;*[DelimitedComment](#DelimitedComment)*
&emsp;&emsp;&emsp;&emsp;*[SingleLineComment](#SingleLineComment)*

&emsp;&emsp;<a name="SingleLineComment"></a>*SingleLineComment* **:**
&emsp;&emsp;&emsp;&emsp;`//`&emsp;*[SingleLineCommentCharacters](#SingleLineCommentCharacters)*<sub>opt</sub>

&emsp;&emsp;<a name="SingleLineCommentCharacters"></a>*SingleLineCommentCharacters* **:**
&emsp;&emsp;&emsp;&emsp;*[SingleLineCommentCharacter](#SingleLineCommentCharacter)*
&emsp;&emsp;&emsp;&emsp;*[SingleLineCommentCharacter](#SingleLineCommentCharacter)*&emsp;*[SingleLineCommentCharacters](#SingleLineCommentCharacters)*<sub>opt</sub>

&emsp;&emsp;<a name="SingleLineCommentCharacter"></a>*SingleLineCommentCharacter* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode characters except a NewLineCharacter

&emsp;&emsp;<a name="DelimitedComment"></a>*DelimitedComment* **:**
&emsp;&emsp;&emsp;&emsp;`/*`&emsp;*[DelimitedCommentCharacters](#DelimitedCommentCharacters)*<sub>opt</sub>&emsp;`*/`

&emsp;&emsp;<a name="DelimitedCommentCharacters"></a>*DelimitedCommentCharacters* **:**
&emsp;&emsp;&emsp;&emsp;*[DelimitedCommentCharactersNoAsterisk](#DelimitedCommentCharactersNoAsterisk)*&emsp;*[DelimitedCommentCharacters](#DelimitedCommentCharacters)*<sub>opt</sub>
&emsp;&emsp;&emsp;&emsp;`*`&emsp;*[DelimitedCommentAfterAsteriskCharacters](#DelimitedCommentAfterAsteriskCharacters)*

&emsp;&emsp;<a name="DelimitedCommentAfterAsteriskCharacters"></a>*DelimitedCommentAfterAsteriskCharacters* **:**
&emsp;&emsp;&emsp;&emsp;*[DelimitedCommentNoSlashAsteriskCharacter](#DelimitedCommentNoSlashAsteriskCharacter)*&emsp;*[DelimitedCommentCharacters](#DelimitedCommentCharacters)*<sub>opt</sub>
&emsp;&emsp;&emsp;&emsp;`*`&emsp;*[DelimitedCommentAfterAsteriskCharacters](#DelimitedCommentAfterAsteriskCharacters)*

&emsp;&emsp;<a name="DelimitedCommentCharactersNoAsterisk"></a>*DelimitedCommentCharactersNoAsterisk* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character except * (asterisk)

&emsp;&emsp;<a name="DelimitedCommentNoSlashAsteriskCharacter"></a>*DelimitedCommentNoSlashAsteriskCharacter* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character except a / (slash) or * (asterisk)

Comments do not nest. The character sequences `/*` and `*/` have no special meaning within a *single-line-comment*, and the character sequences `//` and `/*` have no special meaning within a *delimited-comment*.

Comments are not processed within *text-literal*.

The following example includes two delimited comments:

```powerapps-dot
/* Hello, world
*/
"Hello, world"    /* This is an example of a text literal */
```

The following examples include three single-line comments:

```powerapps-dot
// Hello, world
//
"Hello, world"    // This is an example of a text literal
```

### Literals 

A literal is a source code representation of a value.

&emsp;&emsp;<a name="Literal"></a>*Literal* **:**
&emsp;&emsp;&emsp;&emsp;*[LogicalLiteral](#LogicalLiteral)*
&emsp;&emsp;&emsp;&emsp;*[NumberLiteral](#NumberLiteral)*
&emsp;&emsp;&emsp;&emsp;*[TextLiteral](#TextLiteral)*

#### Logical literals 

A logical literal is used to write the values true and false and produces a logical value.

&emsp;&emsp;<a name="LogicalLiteral"></a>*LogicalLiteral* **:** **one of**
&emsp;&emsp;&emsp;&emsp;`true`&emsp;`false`

#### Number literals 

A number literal is used to write a numeric value and produces a number value.

&emsp;&emsp;<a name="NumberLiteral"></a>*NumberLiteral* **:**
&emsp;&emsp;&emsp;&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[ExponentPart](#ExponentPart)*<sub>opt</sub>
&emsp;&emsp;&emsp;&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[DecimalSeparator](#DecimalSeparator)*&emsp;*[DecimalDigits](#DecimalDigits)*<sub>opt</sub>&emsp;*[ExponentPart](#ExponentPart)*<sub>opt</sub>
&emsp;&emsp;&emsp;&emsp;*[DecimalSeparator](#DecimalSeparator)*&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[ExponentPart](#ExponentPart)*<sub>opt</sub>

&emsp;&emsp;<a name="DecimalDigits"></a>*DecimalDigits* **:**
&emsp;&emsp;&emsp;&emsp;*[DecimalDigit](#DecimalDigit)*
&emsp;&emsp;&emsp;&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[DecimalDigit](#DecimalDigit)*

&emsp;&emsp;<a name="DecimalDigit"></a>*DecimalDigit* **:** **one of**
&emsp;&emsp;&emsp;&emsp;`0`&emsp;`1`&emsp;`2`&emsp;`3`&emsp;`4`&emsp;`5`&emsp;`6`&emsp;`7`&emsp;`8`&emsp;`9`

&emsp;&emsp;<a name="ExponentPart"></a>*ExponentPart* **:**
&emsp;&emsp;&emsp;&emsp;*[ExponentIndicator](#ExponentIndicator)*&emsp;*[Sign](#Sign)*<sub>opt</sub>&emsp;*[DecimalDigits](#DecimalDigits)*

&emsp;&emsp;<a name="ExponentIndicator"></a>*ExponentIndicator* **:** **one of**
&emsp;&emsp;&emsp;&emsp;`e`&emsp;`E`

&emsp;&emsp;<a name="Sign"></a>*Sign* **:** **one of**
&emsp;&emsp;&emsp;&emsp;`+`&emsp;`-`

#### Text literals 

A text literal is used to write a sequence of Unicode characters and produces a text value.  Text literals are enclosed in double quotes.  To include double quotes in the text value, the double quote mark is repeated:

```powerapps-dot
"The ""quoted"" text" // The "quoted" text
```

&emsp;&emsp;<a name="TextLiteral"></a>*TextLiteral* **:**
&emsp;&emsp;&emsp;&emsp;`"`&emsp;*[TextLiteralCharacters](#TextLiteralCharacters)*<sub>opt</sub>&emsp;`"`

&emsp;&emsp;<a name="TextLiteralCharacters"></a>*TextLiteralCharacters* **:**
&emsp;&emsp;&emsp;&emsp;*[TextLiteralCharacter](#TextLiteralCharacter)*&emsp;*[TextLiteralCharacters](#TextLiteralCharacters)*<sub>opt</sub>

&emsp;&emsp;<a name="TextLiteralCharacter"></a>*TextLiteralCharacter* **:**
&emsp;&emsp;&emsp;&emsp;*[TextCharacterNoDoubleQuote](#TextCharacterNoDoubleQuote)*
&emsp;&emsp;&emsp;&emsp;*[DoubleQuoteEscapeSequence](#DoubleQuoteEscapeSequence)*

&emsp;&emsp;<a name="TextCharacterNoDoubleQuote"></a>*TextCharacterNoDoubleQuote* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode code point except double quote

&emsp;&emsp;<a name="DoubleQuoteEscapeSequence"></a>*DoubleQuoteEscapeSequence* **:**
&emsp;&emsp;&emsp;&emsp;`"`&emsp;`"`

## Identifiers 

An identifier is a name used to refer to a value. Identifiers can either be regular identifiers or single quoted identifiers.

&emsp;&emsp;<a name="Identifier"></a>*Identifier* **:**
&emsp;&emsp;&emsp;&emsp;*[IdentifierName](#IdentifierName)* **but** **not** *[Operator](#Operator)* **or** *[ContextKeyword](#ContextKeyword)*

&emsp;&emsp;<a name="IdentifierName"></a>*IdentifierName* **:**
&emsp;&emsp;&emsp;&emsp;*[IdentifierStartCharacter](#IdentifierStartCharacter)*&emsp;*[IdentifierContinueCharacters](#IdentifierContinueCharacters)*<sub>opt</sub>
&emsp;&emsp;&emsp;&emsp;*[SingleQuotedIdentifier](#SingleQuotedIdentifier)*

&emsp;&emsp;<a name="IdentifierStartCharacter"></a>*IdentifierStartCharacter* **:**
&emsp;&emsp;&emsp;&emsp;*[LetterCharacter](#LetterCharacter)*
&emsp;&emsp;&emsp;&emsp;`_`

&emsp;&emsp;<a name="IdentifierContinueCharacter"></a>*IdentifierContinueCharacter* **:**
&emsp;&emsp;&emsp;&emsp;*[IdentifierStartCharacter](#IdentifierStartCharacter)*
&emsp;&emsp;&emsp;&emsp;*[DecimalDigitCharacter](#DecimalDigitCharacter)*
&emsp;&emsp;&emsp;&emsp;*[ConnectingCharacter](#ConnectingCharacter)*
&emsp;&emsp;&emsp;&emsp;*[CombiningCharacter](#CombiningCharacter)*
&emsp;&emsp;&emsp;&emsp;*[FormattingCharacter](#FormattingCharacter)*

&emsp;&emsp;<a name="IdentifierContinueCharacters"></a>*IdentifierContinueCharacters* **:**
&emsp;&emsp;&emsp;&emsp;*[IdentifierContinueCharacter](#IdentifierContinueCharacter)*&emsp;*[IdentifierContinueCharacters](#IdentifierContinueCharacters)*<sub>opt</sub>

&emsp;&emsp;<a name="LetterCharacter"></a>*LetterCharacter* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character of the classes Uppercase letter (Lu), Lowercase letter (Ll)
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Titlecase letter (Lt)
&emsp;&emsp;&emsp;&emsp;any Unicode character of the classes Letter modifier (Lm), Letter other (Lo)
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Number letter (Nl)

&emsp;&emsp;<a name="CombiningCharacter"></a>*CombiningCharacter* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character of the classes Non-spacing mark (Mn), Spacing combining mark (Mc)

&emsp;&emsp;<a name="DecimalDigitCharacter"></a>*DecimalDigitCharacter* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Decimal digit (Nd)

&emsp;&emsp;<a name="ConnectingCharacter"></a>*ConnectingCharacter* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Connector punctuation (Pc)

&emsp;&emsp;<a name="FormattingCharacter"></a>*FormattingCharacter* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Format (Cf)

#### Single quoted identifiers 

A *SingleQuotedIdentifier* can contain any sequence of Unicode characters to be used as an identifier, including keywords, whitespace, comments, and operators.  Single quote characters are supported with a double single quote escape sequence.

&emsp;&emsp;<a name="SingleQuotedIdentifier"></a>*SingleQuotedIdentifier* **:**
&emsp;&emsp;&emsp;&emsp;*[SingleQuotedIdentifierCharacters](#SingleQuotedIdentifierCharacters)*

&emsp;&emsp;<a name="SingleQuotedIdentifierCharacters"></a>*SingleQuotedIdentifierCharacters* **:**
&emsp;&emsp;&emsp;&emsp;*[SingleQuotedIdentifierCharacter](#SingleQuotedIdentifierCharacter)*&emsp;*[SingleQuotedIdentifierCharacters](#SingleQuotedIdentifierCharacters)*<sub>opt</sub>

&emsp;&emsp;<a name="SingleQuotedIdentifierCharacter"></a>*SingleQuotedIdentifierCharacter* **:**
&emsp;&emsp;&emsp;&emsp;*[TextCharactersNoSingleQuote](#TextCharactersNoSingleQuote)*
&emsp;&emsp;&emsp;&emsp;*[SingleQuoteEscapeSequence](#SingleQuoteEscapeSequence)*

&emsp;&emsp;<a name="TextCharactersNoSingleQuote"></a>*TextCharactersNoSingleQuote* **:**
&emsp;&emsp;&emsp;&emsp;any Unicode character except ' (U+0027)

&emsp;&emsp;<a name="SingleQuoteEscapeSequence"></a>*SingleQuoteEscapeSequence* **:**
&emsp;&emsp;&emsp;&emsp;`'`&emsp;`'`

#### Disambiguated identifier 

&emsp;&emsp;<a name="DisambiguatedIdentifier"></a>*DisambiguatedIdentifier* **:**
&emsp;&emsp;&emsp;&emsp;*[TableColumnIdentifier](#TableColumnIdentifier)*
&emsp;&emsp;&emsp;&emsp;*[GlobalIdentifier](#GlobalIdentifier)*

&emsp;&emsp;<a name="TableColumnIdentifier"></a>*TableColumnIdentifier* **:**
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`[@`&emsp;*[Identifier](#Identifier)*&emsp;`]`

&emsp;&emsp;<a name="GlobalIdentifier"></a>*GlobalIdentifier* **:**
&emsp;&emsp;&emsp;&emsp;`[@`&emsp;*[Identifier](#Identifier)*&emsp;`]`

#### Context keywords 

&emsp;&emsp;<a name="ContextKeyword"></a>*ContextKeyword* **:**
&emsp;&emsp;&emsp;&emsp;`Parent`
&emsp;&emsp;&emsp;&emsp;`Self`
&emsp;&emsp;&emsp;&emsp;`ThisItem`
&emsp;&emsp;&emsp;&emsp;`ThisRecord`

#### Case sensitivity 

Power Apps identifiers are case-sensitive.  The authoring tool will auto correct to the correct case when a formula is being written.

## Separators 

&emsp;&emsp;<a name="DecimalSeparator"></a>*DecimalSeparator* **:**
&emsp;&emsp;&emsp;&emsp;`.` (dot) for language that uses a dot as the separator for decimal numbers, for example `1.23`
&emsp;&emsp;&emsp;&emsp;`,` (comma) for languages that use a comma as the separator for decimal numbers, for example `1,23`

&emsp;&emsp;<a name="ListSeparator"></a>*ListSeparator* **:**
&emsp;&emsp;&emsp;&emsp;`,` (comma) if *[DecimalSeparator](#DecimalSeparator)* is `.` (dot)
&emsp;&emsp;&emsp;&emsp;`;` (semi-colon) if *[DecimalSeparator](#DecimalSeparator)* is `,` (comma)

&emsp;&emsp;<a name="ChainingSeparator"></a>*ChainingSeparator* **:**
&emsp;&emsp;&emsp;&emsp;`;` (semi-colon) if *[DecimalSeparator](#DecimalSeparator)* is `.` (dot)
&emsp;&emsp;&emsp;&emsp;`;;` (double semi-colon) if *[DecimalSeparator](#DecimalSeparator)* is `,` (comma)

## Operators 

Operators are used in formulas to describe operations involving one or more operands. For example, the expression `a + b` uses the `+` operator to add the two operands `a` and `b`. 

&emsp;&emsp;<a name="Operator"></a>*Operator* **:**
&emsp;&emsp;&emsp;&emsp;*[BinaryOperator](#BinaryOperator)*
&emsp;&emsp;&emsp;&emsp;*[BinaryOperatorRequiresWhitespace](#BinaryOperatorRequiresWhitespace)*
&emsp;&emsp;&emsp;&emsp;*[PrefixOperator](#PrefixOperator)*
&emsp;&emsp;&emsp;&emsp;*[PrefixOperatorRequiresWhitespace](#PrefixOperatorRequiresWhitespace)*
&emsp;&emsp;&emsp;&emsp;*[PostfixOperator](#PostfixOperator)*

&emsp;&emsp;<a name="BinaryOperator"></a>*BinaryOperator* **:** **one of**
&emsp;&emsp;&emsp;&emsp;`=`&emsp;`<`&emsp;`<=`&emsp;`>`&emsp;`>=`&emsp;`<>`
&emsp;&emsp;&emsp;&emsp;`+`&emsp;`-`&emsp;`*`&emsp;`/`&emsp;`^`
&emsp;&emsp;&emsp;&emsp;`&`
&emsp;&emsp;&emsp;&emsp;`&&`&emsp;`||`
&emsp;&emsp;&emsp;&emsp;`in`&emsp;`exactin`

&emsp;&emsp;<a name="BinaryOperatorRequiresWhitespace"></a>*BinaryOperatorRequiresWhitespace* **:**
&emsp;&emsp;&emsp;&emsp;`And`&emsp;*[Whitespace](#Whitespace)*
&emsp;&emsp;&emsp;&emsp;`Or`&emsp;*[Whitespace](#Whitespace)*

&emsp;&emsp;<a name="PrefixOperator"></a>*PrefixOperator* **:**
&emsp;&emsp;&emsp;&emsp;`!`

&emsp;&emsp;<a name="PrefixOperatorRequiresWhitespace"></a>*PrefixOperatorRequiresWhitespace* **:**
&emsp;&emsp;&emsp;&emsp;`Not`&emsp;*[Whitespace](#Whitespace)*

&emsp;&emsp;<a name="PostfixOperator"></a>*PostfixOperator* **:**
&emsp;&emsp;&emsp;&emsp;`%`

### Reference operator 

&emsp;&emsp;<a name="ReferenceOperator"></a>*ReferenceOperator* **:** **one of**
&emsp;&emsp;&emsp;&emsp;`.`&emsp;`!`

### Object reference 

&emsp;&emsp;<a name="Reference"></a>*Reference* **:**
&emsp;&emsp;&emsp;&emsp;*[BaseReference](#BaseReference)*
&emsp;&emsp;&emsp;&emsp;*[BaseReference](#BaseReference)*&emsp;*[ReferenceOperator](#ReferenceOperator)*&emsp;*[ReferenceList](#ReferenceList)*

&emsp;&emsp;<a name="BaseReference"></a>*BaseReference* **:**
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*
&emsp;&emsp;&emsp;&emsp;*[DisambiguatedIdentifier](#DisambiguatedIdentifier)*
&emsp;&emsp;&emsp;&emsp;*[ContextKeyword](#ContextKeyword)*

&emsp;&emsp;<a name="ReferenceList"></a>*ReferenceList* **:**
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;*[ReferenceOperator](#ReferenceOperator)*&emsp;*[ReferenceList](#ReferenceList)*

### Inline record 

&emsp;&emsp;<a name="InlineRecord"></a>*InlineRecord* **:**
&emsp;&emsp;&emsp;&emsp;`{`&emsp;*[InlineRecordList](#InlineRecordList)*<sub>opt</sub>&emsp;`}`

&emsp;&emsp;<a name="InlineRecordList"></a>*InlineRecordList* **:**
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`:`&emsp;*[Expression](#Expression)*
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`:`&emsp;*[Expression](#Expression)*&emsp;*[ListSeparator](#ListSeparator)*&emsp;*[InlineRecordList](#InlineRecordList)*

### Inline table 

&emsp;&emsp;<a name="InlineTable"></a>*InlineTable* **:**
&emsp;&emsp;&emsp;&emsp;`[`&emsp;*[InlineTableList](#InlineTableList)*<sub>opt</sub>&emsp;`]`

&emsp;&emsp;<a name="InlineTableList"></a>*InlineTableList* **:**
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[ListSeparator](#ListSeparator)*&emsp;*[InlineTableList](#InlineTableList)*

## Expression 

&emsp;&emsp;<a name="Expression"></a>*Expression* **:**
&emsp;&emsp;&emsp;&emsp;*[Literal](#Literal)*
&emsp;&emsp;&emsp;&emsp;*[Reference](#Reference)*
&emsp;&emsp;&emsp;&emsp;*[InlineRecord](#InlineRecord)*
&emsp;&emsp;&emsp;&emsp;*[InlineTable](#InlineTable)*
&emsp;&emsp;&emsp;&emsp;*[FunctionCall](#FunctionCall)*
&emsp;&emsp;&emsp;&emsp;`(`&emsp;*[Expression](#Expression)*&emsp;`)`
&emsp;&emsp;&emsp;&emsp;*[PrefixOperator](#PrefixOperator)*&emsp;*[Expression](#Expression)*
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[PostfixOperator](#PostfixOperator)*
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[BinaryOperator](#BinaryOperator)*&emsp;*[Expression](#Expression)*

### Chained expressions 

&emsp;&emsp;<a name="ChainedExpression"></a>*ChainedExpression* **:**
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[ChainingSeparator](#ChainingSeparator)*&emsp;*[ChainedExpression](#ChainedExpression)*<sub>opt</sub>

### Function call

&emsp;&emsp;<a name="FunctionCall"></a>*FunctionCall* **:**
&emsp;&emsp;&emsp;&emsp;*[FunctionIdentifier](#FunctionIdentifier)*&emsp;`(`&emsp;*[FunctionArguments](#FunctionArguments)*<sub>opt</sub>&emsp;`)`

&emsp;&emsp;<a name="FunctionIdentifier"></a>*FunctionIdentifier* **:**
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`.`&emsp;*[FunctionIdentifier](#FunctionIdentifier)*

&emsp;&emsp;<a name="FunctionArguments"></a>*FunctionArguments* **:**
&emsp;&emsp;&emsp;&emsp;*[ChainedExpression](#ChainedExpression)*
&emsp;&emsp;&emsp;&emsp;*[ChainedExpression](#ChainedExpression)*&emsp;*[ListSeparator](#ListSeparator)*&emsp;*[FunctionArguments](#FunctionArguments)*


