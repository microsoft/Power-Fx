# Power Fx Expression Grammar

*NOTE: Power Fx is the new name for canvas apps formula language.  These articles are work in progress as we extract the language from canvas apps, integrate it with other products of the Power Platform, and make available as open source.  Start with the [Power Fx Overview](overview.md) for an introduction to the language.*

Power Fx is based on formulas that binds a name to an expression.  Just like in spreadsheet, as inbound dependencies to the expression change, the expression is recalculated and the value of the name changes, possibly cascading the recalculation into other formulas.  

This grammar covers the expression part of the formula. Binding to a name to create a formula is dependent on how Power Fx is integrated. In spreadsheets, the binding syntax is not exposed and is implied by where the expression is written, for example `typing =B1` in the A1 cell. In some cases, no binding is required at all and Power Fx is used as an expression evaluator, for example in supporting calculated columns of a database table. For Power Apps, the binding is implied when in Power Apps Studio with [a serialization format based on YAML for use outside Power Apps Studio](yaml-formula-grammar.md).

This article is an annotated version of the grammar.  The raw grammar, suitable for use with tools, is also [available as a .grammar file](expression-grammar.grammar).

## Grammar conventions

The lexical and syntactic grammars are presented using grammar productions. Each grammar production defines a non-terminal symbol and the possible expansions of that non-terminal symbol into sequences of non-terminal or terminal symbols. In grammar productions, non-terminal symbols are shown in italic type, and terminal symbols are shown in a fixed-width font.

The first line of a grammar production is the name of the non-terminal symbol being defined, followed by a colon. Each successive indented line contains a possible expansion of the non-terminal given as a sequence of non-terminal or terminal symbols. For example, the production:

&emsp;&emsp;*GlobalIdentifier* **:**<br>
&emsp;&emsp;&emsp;&emsp;`[@`&emsp;*Identifier*&emsp;`]`<br>

defines a *GlobalIdentifier* to consist of the token `[@`, followed by an *Identifier*, followed by the token `]`

When there is more than one possible expansion of a non-terminal symbol, the alternatives are listed on separate lines. A subscripted suffix "opt" is used to indicate an optional symbol. For example, the production:

&emsp;&emsp;*FunctionCall* **:**<br>
&emsp;&emsp;&emsp;&emsp;*FunctionIdentifier*&emsp;`(`&emsp;*FunctionArguments*<sub>opt</sub>&emsp;`)`

is shorthand for:

&emsp;&emsp;*FunctionCall***:**<br>
&emsp;&emsp;&emsp;&emsp;*FunctionIdentifier*&emsp;`(`&emsp;`)`<br>
&emsp;&emsp;&emsp;&emsp;*FunctionIdentifier*&emsp;`(`&emsp;*FunctionArguments*&emsp;`)`<br>

Alternatives are normally listed on separate lines, though in cases where there are many alternatives, the phrase "one of" may precede a list of expansions given on a single line. This is simply shorthand for listing each of the alternatives on a separate line.

For example, the production:

&emsp;&emsp;*DecimalDigit* **:** **one of**<br>
&emsp;&emsp;&emsp;&emsp;`0`&emsp;`1`&emsp;`2`&emsp;`3`&emsp;`4`&emsp;`5`&emsp;`6`&emsp;`7`&emsp;`8`&emsp;`9`

is shorthand for:

&emsp;&emsp;*DecimalDigit* **:**<br>
&emsp;&emsp;&emsp;&emsp;`0`<br>
&emsp;&emsp;&emsp;&emsp;`1`<br>
&emsp;&emsp;&emsp;&emsp;`2`<br>
&emsp;&emsp;&emsp;&emsp;`3`<br>
&emsp;&emsp;&emsp;&emsp;`4`<br>
&emsp;&emsp;&emsp;&emsp;`5`<br>
&emsp;&emsp;&emsp;&emsp;`6`<br>
&emsp;&emsp;&emsp;&emsp;`7`<br>
&emsp;&emsp;&emsp;&emsp;`8`<br>
&emsp;&emsp;&emsp;&emsp;`9`<br>

### Lexical analysis

The lexical-unit production defines the lexical grammar for a Power Fx expression. Every valid Power Fx expression conforms to this grammar.

&emsp;&emsp;<a name="ExpressionUnit"></a>*ExpressionUnit* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[ExpressionElements](#ExpressionElements)*<sub>opt</sub><br>

&emsp;&emsp;<a name="ExpressionElements"></a>*ExpressionElements* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[ExpressionElement](#ExpressionElement)*<br>
&emsp;&emsp;&emsp;&emsp;*[ExpressionElement](#ExpressionElement)*&emsp;*[ExpressionElements](#ExpressionElements)*<sub>opt</sub><br>

&emsp;&emsp;<a name="ExpressionElement"></a>*ExpressionElement* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[Whitespace](#Whitespace)*<br>
&emsp;&emsp;&emsp;&emsp;*[Comment](#Comment)*<br>
&emsp;&emsp;&emsp;&emsp;*[Token](#Token)*<br>

At the lexical level, a Power Fx expression consists of a stream of *Whitespace*, *Comment*, and *Token* elements. Each of these productions is covered in the following sections. Only *Token* elements are significant in the syntactic grammar.

### Whitespace

Whitespace is used to separate comments and tokens within a Power Apps document.

&emsp;&emsp;<a name="Whitespace"></a>*Whitespace* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode Space separator (class Zs)<br>
&emsp;&emsp;&emsp;&emsp;any Unicode Line separator (class Zl)<br>
&emsp;&emsp;&emsp;&emsp;any Unicode Paragraph separator (class Zp)<br>
&emsp;&emsp;&emsp;&emsp;Horizontal tab character (U+0009)<br>
&emsp;&emsp;&emsp;&emsp;Line feed character (U+000A)<br>
&emsp;&emsp;&emsp;&emsp;Vertical tab character (U+000B)<br>
&emsp;&emsp;&emsp;&emsp;Form feed character (U+000C)<br>
&emsp;&emsp;&emsp;&emsp;Carriage return character (U+000D)<br>
&emsp;&emsp;&emsp;&emsp;Next line character (U+0085)<br>

### Comments

Two forms of comments are supported:

- Single-line comments that start with the characters // and extend to the end of the source line.
- Delimited comments that start with the characters /\* and end with the characters \*/. Delimited comments may span multiple lines.

&emsp;&emsp;<a name="Comment"></a>*Comment* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[DelimitedComment](#DelimitedComment)*<br>
&emsp;&emsp;&emsp;&emsp;*[SingleLineComment](#SingleLineComment)*<br>

&emsp;&emsp;<a name="SingleLineComment"></a>*SingleLineComment* **:**<br>
&emsp;&emsp;&emsp;&emsp;`//`&emsp;*[SingleLineCommentCharacters](#SingleLineCommentCharacters)*<sub>opt</sub><br>

&emsp;&emsp;<a name="SingleLineCommentCharacters"></a>*SingleLineCommentCharacters* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[SingleLineCommentCharacter](#SingleLineCommentCharacter)*<br>
&emsp;&emsp;&emsp;&emsp;*[SingleLineCommentCharacter](#SingleLineCommentCharacter)*&emsp;*[SingleLineCommentCharacters](#SingleLineCommentCharacters)*<sub>opt</sub><br>

&emsp;&emsp;<a name="SingleLineCommentCharacter"></a>*SingleLineCommentCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode characters except a NewLineCharacter<br>

&emsp;&emsp;<a name="DelimitedComment"></a>*DelimitedComment* **:**<br>
&emsp;&emsp;&emsp;&emsp;`/*`&emsp;*[DelimitedCommentCharacters](#DelimitedCommentCharacters)*<sub>opt</sub>&emsp;`*/`<br>

&emsp;&emsp;<a name="DelimitedCommentCharacters"></a>*DelimitedCommentCharacters* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[DelimitedCommentCharactersNoAsterisk](#DelimitedCommentCharactersNoAsterisk)*&emsp;*[DelimitedCommentCharacters](#DelimitedCommentCharacters)*<sub>opt</sub><br>
&emsp;&emsp;&emsp;&emsp;`*`&emsp;*[DelimitedCommentAfterAsteriskCharacters](#DelimitedCommentAfterAsteriskCharacters)*<br>

&emsp;&emsp;<a name="DelimitedCommentAfterAsteriskCharacters"></a>*DelimitedCommentAfterAsteriskCharacters* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[DelimitedCommentNoSlashAsteriskCharacter](#DelimitedCommentNoSlashAsteriskCharacter)*&emsp;*[DelimitedCommentCharacters](#DelimitedCommentCharacters)*<sub>opt</sub><br>
&emsp;&emsp;&emsp;&emsp;`*`&emsp;*[DelimitedCommentAfterAsteriskCharacters](#DelimitedCommentAfterAsteriskCharacters)*<br>

&emsp;&emsp;<a name="DelimitedCommentCharactersNoAsterisk"></a>*DelimitedCommentCharactersNoAsterisk* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character except * (asterisk)<br>

&emsp;&emsp;<a name="DelimitedCommentNoSlashAsteriskCharacter"></a>*DelimitedCommentNoSlashAsteriskCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character except a / (slash) or * (asterisk)<br>

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

&emsp;&emsp;<a name="Literal"></a>*Literal* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[LogicalLiteral](#LogicalLiteral)*<br>
&emsp;&emsp;&emsp;&emsp;*[NumberLiteral](#NumberLiteral)*<br>
&emsp;&emsp;&emsp;&emsp;*[TextLiteral](#TextLiteral)*<br>

#### Logical literals

A logical literal is used to write the values true and false and produces a logical value.

&emsp;&emsp;<a name="LogicalLiteral"></a>*LogicalLiteral* **:** **one of**<br>
&emsp;&emsp;&emsp;&emsp;`true`&emsp;`false`<br>

#### Number literals

A number literal is used to write a numeric value and produces a number value.

&emsp;&emsp;<a name="NumberLiteral"></a>*NumberLiteral* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[ExponentPart](#ExponentPart)*<sub>opt</sub><br>
&emsp;&emsp;&emsp;&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[DecimalSeparator](#DecimalSeparator)*&emsp;*[DecimalDigits](#DecimalDigits)*<sub>opt</sub>&emsp;*[ExponentPart](#ExponentPart)*<sub>opt</sub><br>
&emsp;&emsp;&emsp;&emsp;*[DecimalSeparator](#DecimalSeparator)*&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[ExponentPart](#ExponentPart)*<sub>opt</sub><br>

&emsp;&emsp;<a name="DecimalDigits"></a>*DecimalDigits* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[DecimalDigit](#DecimalDigit)*<br>
&emsp;&emsp;&emsp;&emsp;*[DecimalDigits](#DecimalDigits)*&emsp;*[DecimalDigit](#DecimalDigit)*<br>

&emsp;&emsp;<a name="DecimalDigit"></a>*DecimalDigit* **:** **one of**<br>
&emsp;&emsp;&emsp;&emsp;`0`&emsp;`1`&emsp;`2`&emsp;`3`&emsp;`4`&emsp;`5`&emsp;`6`&emsp;`7`&emsp;`8`&emsp;`9`<br>

&emsp;&emsp;<a name="ExponentPart"></a>*ExponentPart* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[ExponentIndicator](#ExponentIndicator)*&emsp;*[Sign](#Sign)*<sub>opt</sub>&emsp;*[DecimalDigits](#DecimalDigits)*<br>

&emsp;&emsp;<a name="ExponentIndicator"></a>*ExponentIndicator* **:** **one of**<br>
&emsp;&emsp;&emsp;&emsp;`e`&emsp;`E`<br>

&emsp;&emsp;<a name="Sign"></a>*Sign* **:** **one of**<br>
&emsp;&emsp;&emsp;&emsp;`+`&emsp;`-`<br>

#### Text literals

A text literal is used to write a sequence of Unicode characters and produces a text value.  Text literals are enclosed in double quotes.  To include double quotes in the text value, the double quote mark is repeated:

```powerapps-dot
"The ""quoted"" text" // The "quoted" text
```

&emsp;&emsp;<a name="TextLiteral"></a>*TextLiteral* **:**<br>
&emsp;&emsp;&emsp;&emsp;`"`&emsp;*[TextLiteralCharacters](#TextLiteralCharacters)*<sub>opt</sub>&emsp;`"`<br>

&emsp;&emsp;<a name="TextLiteralCharacters"></a>*TextLiteralCharacters* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[TextLiteralCharacter](#TextLiteralCharacter)*&emsp;*[TextLiteralCharacters](#TextLiteralCharacters)*<sub>opt</sub><br>

&emsp;&emsp;<a name="TextLiteralCharacter"></a>*TextLiteralCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[TextCharacterNoDoubleQuote](#TextCharacterNoDoubleQuote)*<br>
&emsp;&emsp;&emsp;&emsp;*[DoubleQuoteEscapeSequence](#DoubleQuoteEscapeSequence)*<br>

&emsp;&emsp;<a name="TextCharacterNoDoubleQuote"></a>*TextCharacterNoDoubleQuote* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode code point except double quote<br>

&emsp;&emsp;<a name="DoubleQuoteEscapeSequence"></a>*DoubleQuoteEscapeSequence* **:**<br>
&emsp;&emsp;&emsp;&emsp;`"`&emsp;`"`<br>

## Identifiers

An identifier is a name used to refer to a value. Identifiers can either be regular identifiers or single quoted identifiers.

&emsp;&emsp;<a name="Identifier"></a>*Identifier* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[IdentifierName](#IdentifierName)* **but** **not** *[Operator](#Operator)* **or** *[ContextKeyword](#ContextKeyword)*<br>

&emsp;&emsp;<a name="IdentifierName"></a>*IdentifierName* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[IdentifierStartCharacter](#IdentifierStartCharacter)*&emsp;*[IdentifierContinueCharacters](#IdentifierContinueCharacters)*<sub>opt</sub><br>
&emsp;&emsp;&emsp;&emsp;*[SingleQuotedIdentifier](#SingleQuotedIdentifier)*<br>

&emsp;&emsp;<a name="IdentifierStartCharacter"></a>*IdentifierStartCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[LetterCharacter](#LetterCharacter)*<br>
&emsp;&emsp;&emsp;&emsp;`_`<br>

&emsp;&emsp;<a name="IdentifierContinueCharacter"></a>*IdentifierContinueCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[IdentifierStartCharacter](#IdentifierStartCharacter)*<br>
&emsp;&emsp;&emsp;&emsp;*[DecimalDigitCharacter](#DecimalDigitCharacter)*<br>
&emsp;&emsp;&emsp;&emsp;*[ConnectingCharacter](#ConnectingCharacter)*<br>
&emsp;&emsp;&emsp;&emsp;*[CombiningCharacter](#CombiningCharacter)*<br>
&emsp;&emsp;&emsp;&emsp;*[FormattingCharacter](#FormattingCharacter)*<br>

&emsp;&emsp;<a name="IdentifierContinueCharacters"></a>*IdentifierContinueCharacters* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[IdentifierContinueCharacter](#IdentifierContinueCharacter)*&emsp;*[IdentifierContinueCharacters](#IdentifierContinueCharacters)*<sub>opt</sub><br>

&emsp;&emsp;<a name="LetterCharacter"></a>*LetterCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the classes Uppercase letter (Lu), Lowercase letter (Ll)<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Titlecase letter (Lt)<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the classes Letter modifier (Lm), Letter other (Lo)<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Number letter (Nl)<br>

&emsp;&emsp;<a name="CombiningCharacter"></a>*CombiningCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the classes Non-spacing mark (Mn), Spacing combining mark (Mc)<br>

&emsp;&emsp;<a name="DecimalDigitCharacter"></a>*DecimalDigitCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Decimal digit (Nd)<br>

&emsp;&emsp;<a name="ConnectingCharacter"></a>*ConnectingCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Connector punctuation (Pc)<br>

&emsp;&emsp;<a name="FormattingCharacter"></a>*FormattingCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character of the class Format (Cf)<br>

### Single quoted identifiers

A *SingleQuotedIdentifier* can contain any sequence of Unicode characters to be used as an identifier, including keywords, whitespace, comments, and operators.  Single quote characters are supported with a double single quote escape sequence.

&emsp;&emsp;<a name="SingleQuotedIdentifier"></a>*SingleQuotedIdentifier* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[SingleQuotedIdentifierCharacters](#SingleQuotedIdentifierCharacters)*<br>

&emsp;&emsp;<a name="SingleQuotedIdentifierCharacters"></a>*SingleQuotedIdentifierCharacters* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[SingleQuotedIdentifierCharacter](#SingleQuotedIdentifierCharacter)*&emsp;*[SingleQuotedIdentifierCharacters](#SingleQuotedIdentifierCharacters)*<sub>opt</sub><br>

&emsp;&emsp;<a name="SingleQuotedIdentifierCharacter"></a>*SingleQuotedIdentifierCharacter* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[TextCharactersNoSingleQuote](#TextCharactersNoSingleQuote)*<br>
&emsp;&emsp;&emsp;&emsp;*[SingleQuoteEscapeSequence](#SingleQuoteEscapeSequence)*<br>

&emsp;&emsp;<a name="TextCharactersNoSingleQuote"></a>*TextCharactersNoSingleQuote* **:**<br>
&emsp;&emsp;&emsp;&emsp;any Unicode character except ' (U+0027)<br>

&emsp;&emsp;<a name="SingleQuoteEscapeSequence"></a>*SingleQuoteEscapeSequence* **:**<br>
&emsp;&emsp;&emsp;&emsp;`'`&emsp;`'`<br>

### Disambiguated identifier

&emsp;&emsp;<a name="DisambiguatedIdentifier"></a>*DisambiguatedIdentifier:*<br>
&emsp;&emsp;&emsp;&emsp;*[TableColumnIdentifier](#TableColumnIdentifier)*<br>
&emsp;&emsp;&emsp;&emsp;*[GlobalIdentifier](#GlobalIdentifier)*<br>

&emsp;&emsp;<a name="TableColumnIdentifier"></a>*TableColumnIdentifier* **:**<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`[@`&emsp;*[Identifier](#Identifier)*&emsp;`]`<br>

&emsp;&emsp;<a name="GlobalIdentifier"></a>*GlobalIdentifier:*<br>
&emsp;&emsp;&emsp;&emsp;`[@`&emsp;*[Identifier](#Identifier)*&emsp;`]`<br>

### Context keywords

&emsp;&emsp;<a name="ContextKeyword"></a>*ContextKeyword:*<br>
&emsp;&emsp;&emsp;&emsp;`Parent`<br>
&emsp;&emsp;&emsp;&emsp;`Self`<br>
&emsp;&emsp;&emsp;&emsp;`ThisItem`<br>
&emsp;&emsp;&emsp;&emsp;`ThisRecord`<br>

### Case sensitivity

Power Apps identifiers are case-sensitive.  The authoring tool will auto correct to the correct case when a formula is being written.

## Separators

&emsp;&emsp;<a name="DecimalSeparator"></a>*DecimalSeparator:*<br>
&emsp;&emsp;&emsp;&emsp;`.` (dot) for language that uses a dot as the separator for decimal numbers, for example `1.23`<br>
&emsp;&emsp;&emsp;&emsp;`,` (comma) for languages that use a comma as the separator for decimal numbers, for example `1,23`<br>

&emsp;&emsp;<a name="ListSeparator"></a>*ListSeparator:*<br>
&emsp;&emsp;&emsp;&emsp;`,` (comma) if *[DecimalSeparator](#DecimalSeparator)* is `.` (dot)<br>
&emsp;&emsp;&emsp;&emsp;`;` (semi-colon) if *[DecimalSeparator](#DecimalSeparator)* is `,` (comma)<br>

&emsp;&emsp;<a name="ChainingSeparator"></a>*ChainingSeparator:*<br>
&emsp;&emsp;&emsp;&emsp;`;` (semi-colon) if *[DecimalSeparator](#DecimalSeparator)* is `.` (dot)<br>
&emsp;&emsp;&emsp;&emsp;`;;` (double semi-colon) if *[DecimalSeparator](#DecimalSeparator)* is `,` (comma)<br>

## Operators

Operators are used in formulas to describe operations involving one or more operands. For example, the expression `a + b` uses the `+` operator to add the two operands `a` and `b`.

&emsp;&emsp;<a name="Operator"></a>*Operator:*<br>
&emsp;&emsp;&emsp;&emsp;*[BinaryOperator](#BinaryOperator)*<br>
&emsp;&emsp;&emsp;&emsp;*[BinaryOperatorRequiresWhitespace](#BinaryOperatorRequiresWhitespace)*<br>
&emsp;&emsp;&emsp;&emsp;*[PrefixOperator](#PrefixOperator)*<br>
&emsp;&emsp;&emsp;&emsp;*[PrefixOperatorRequiresWhitespace](#PrefixOperatorRequiresWhitespace)*<br>
&emsp;&emsp;&emsp;&emsp;*[PostfixOperator](#PostfixOperator)*<br>

&emsp;&emsp;<a name="BinaryOperator"></a>*BinaryOperator:* **one of**<br>
&emsp;&emsp;&emsp;&emsp;`=`&emsp;`<`&emsp;`<=`&emsp;`>`&emsp;`>=`&emsp;`<>`<br>
&emsp;&emsp;&emsp;&emsp;`+`&emsp;`-`&emsp;`*`&emsp;`/`&emsp;`^`<br>
&emsp;&emsp;&emsp;&emsp;`&`<br>
&emsp;&emsp;&emsp;&emsp;`&&`&emsp;`||`<br>
&emsp;&emsp;&emsp;&emsp;`in`&emsp;`exactin`<br>

&emsp;&emsp;<a name="BinaryOperatorRequiresWhitespace"></a>*BinaryOperatorRequiresWhitespace:*<br>
&emsp;&emsp;&emsp;&emsp;`And`&emsp;*[Whitespace](#Whitespace)*<br>
&emsp;&emsp;&emsp;&emsp;`Or`&emsp;*[Whitespace](#Whitespace)*<br>

&emsp;&emsp;<a name="PrefixOperator"></a>*PrefixOperator:*<br>
&emsp;&emsp;&emsp;&emsp;`!`<br>

&emsp;&emsp;<a name="PrefixOperatorRequiresWhitespace"></a>*PrefixOperatorRequiresWhitespace:*<br>
&emsp;&emsp;&emsp;&emsp;`Not`&emsp;*[Whitespace](#Whitespace)*<br>

&emsp;&emsp;<a name="PostfixOperator"></a>*PostfixOperator:*<br>
&emsp;&emsp;&emsp;&emsp;`%`<br>

### Reference operator

&emsp;&emsp;<a name="ReferenceOperator"></a>*ReferenceOperator:* **one of**<br>
&emsp;&emsp;&emsp;&emsp;`.`&emsp;`!`<br>

### Object reference

&emsp;&emsp;<a name="Reference"></a>*Reference:*<br>
&emsp;&emsp;&emsp;&emsp;*[BaseReference](#BaseReference)*<br>
&emsp;&emsp;&emsp;&emsp;*[BaseReference](#BaseReference)*&emsp;*[ReferenceOperator](#ReferenceOperator)*&emsp;*[ReferenceList](#ReferenceList)*<br>

&emsp;&emsp;<a name="BaseReference"></a>*BaseReference:*<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*<br>
&emsp;&emsp;&emsp;&emsp;*[DisambiguatedIdentifier](#DisambiguatedIdentifier)*<br>
&emsp;&emsp;&emsp;&emsp;*[ContextKeyword](#ContextKeyword)*<br>

&emsp;&emsp;<a name="ReferenceList"></a>*ReferenceList:*<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;*[ReferenceOperator](#ReferenceOperator)*&emsp;*[ReferenceList](#ReferenceList)*<br>

### Inline record

&emsp;&emsp;<a name="InlineRecord"></a>*InlineRecord:*<br>
&emsp;&emsp;&emsp;&emsp;`{`&emsp;*[InlineRecordList](#InlineRecordList)*<sub>opt</sub>&emsp;`}`<br>

&emsp;&emsp;<a name="InlineRecordList"></a>*InlineRecordList:*<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`:`&emsp;*[Expression](#Expression)*<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`:`&emsp;*[Expression](#Expression)*&emsp;*[ListSeparator](#ListSeparator)*&emsp;*[InlineRecordList](#InlineRecordList)*<br>

### Inline table

&emsp;&emsp;<a name="InlineTable"></a>*InlineTable:*<br>
&emsp;&emsp;&emsp;&emsp;`[`&emsp;*[InlineTableList](#InlineTableList)*<sub>opt</sub>&emsp;`]`<br>

&emsp;&emsp;<a name="InlineTableList"></a>*InlineTableList:*<br>
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*<br>
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[ListSeparator](#ListSeparator)*&emsp;*[InlineTableList](#InlineTableList)*<br>

## Expression

&emsp;&emsp;<a name="Expression"></a>*Expression:*<br>
&emsp;&emsp;&emsp;&emsp;*[Literal](#Literal)*<br>
&emsp;&emsp;&emsp;&emsp;*[Reference](#Reference)*<br>
&emsp;&emsp;&emsp;&emsp;*[InlineRecord](#InlineRecord)*<br>
&emsp;&emsp;&emsp;&emsp;*[InlineTable](#InlineTable)*<br>
&emsp;&emsp;&emsp;&emsp;*[FunctionCall](#FunctionCall)*<br>
&emsp;&emsp;&emsp;&emsp;`(`&emsp;*[Expression](#Expression)*&emsp;`)`<br>
&emsp;&emsp;&emsp;&emsp;*[PrefixOperator](#PrefixOperator)*&emsp;*[Expression](#Expression)*<br>
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[PostfixOperator](#PostfixOperator)*<br>
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[BinaryOperator](#BinaryOperator)*&emsp;*[Expression](#Expression)*<br>

### Chained expressions

&emsp;&emsp;<a name="ChainedExpression"></a>*ChainedExpression:*<br>
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*<br>
&emsp;&emsp;&emsp;&emsp;*[Expression](#Expression)*&emsp;*[ChainingSeparator](#ChainingSeparator)*&emsp;*[ChainedExpression](#ChainedExpression)*<sub>opt</sub><br>

### Function call

&emsp;&emsp;<a name="FunctionCall"></a>*FunctionCall:*<br>
&emsp;&emsp;&emsp;&emsp;*[FunctionIdentifier](#FunctionIdentifier)*&emsp;`(`&emsp;*[FunctionArguments](#FunctionArguments)*<sub>opt</sub>&emsp;`)`<br>

&emsp;&emsp;<a name="FunctionIdentifier"></a>*FunctionIdentifier:*<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*<br>
&emsp;&emsp;&emsp;&emsp;*[Identifier](#Identifier)*&emsp;`.`&emsp;*[FunctionIdentifier](#FunctionIdentifier)*<br>

&emsp;&emsp;<a name="FunctionArguments"></a>*FunctionArguments:*<br>
&emsp;&emsp;&emsp;&emsp;*[ChainedExpression](#ChainedExpression)*<br>
&emsp;&emsp;&emsp;&emsp;*[ChainedExpression](#ChainedExpression)*&emsp;*[ListSeparator](#ListSeparator)*&emsp;*[FunctionArguments](#FunctionArguments)*<br>
