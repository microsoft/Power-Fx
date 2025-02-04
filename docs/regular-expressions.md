---
title: Regular expressions in Microsoft Power Fx | Microsoft Docs
description: Reference information about working with regular expressions in Microsoft Power Fx
author: gregli-msft
ms.topic: conceptual
ms.reviewer: jdaly
ms.date: 1/31/2025
ms.subservice: power-fx
ms.author: gregli
search.audienceType: 
  - maker
contributors:
  - gregli-msft
  - mduelae
  - gregli
---
# Regular expressions

The [**IsMatch**, **Match**, and **MatchAll** functions](reference/function-ismatch.md) are used to extract and validate patterns in text. The pattern they use is called a [regular expression](https://en.wikipedia.org/wiki/Regular_expression). 

Regular expressions have a long history, are very powerful, are available in many programming languages, and used for a wide variety of purposes. They also often look like a random sequence of punctuation marks. This article doesn't describe all aspects of regular expressions, but a wealth of information, tutorials, and tools are available online.

Every programming language has its own dialect of regular expressions and there are few standards. As much as possible, we would like the same regular expression to give the same result across all Power Fx implementations. That isn't easy to accomplish as Power Fx runs on top of JavaScript and .NET which have significant differences. To accommodate running on different platforms, Power Fx regular expressions are limited to a subset of features that are widely supported across the industry.

Power Fx will produce an authoring time error when unsupported features are encountered. This is one of the reasons that the regular expression and options must be a authoring time constant and not dynamic (for example, provided in a variable).

## Supported features

Power Fx supports the following regular expression features, with notes on how Power Fx behavior may differ from other systems.

### Literal characters

| Feature | Description |
|---------|---------|
| Literal characters | Any character except `[` `]` `\` `^` `$` `.` `|` `?` `*` `+` `(` `)` can be inserted directly. |
| Escaped literal characters | `\` (backslash) followed by any character except a letter or underscore. Used to insert the exceptions to direct literal characters, such as `\?` to insert a question mark. | 
| Control characters | `\cA`, where the control characters is `A` through `Z`, upper or lowercase. |
| Hexadecimal and Unicode character codes | `\x20` with two hexadecimal digits, `\u2028` with four hexadecimal digits. |
| Carriage return | `\r`, the same as `Char(13)`. |
| Newline character | `\n`, the same as `Char(10)`. |
| Form feed | `\f`, the same as `Char(12)`. |
| Horizontal Tab | `\t`, the same as `Char(9)`. |

Octal codes for characters, such as `\044` or `\o{044}` are disallowed. Use `\x` or `\u` instead. Octal character codes can be ambiguous with numbered back references which is why Power Fx disallows them.

Unescaped `[`, `]`, `{`, or `}` as a literal character is disallowed, instead escape these characters with a backslash. This avoids ambiguity.

Escaped letters and underscore are reserved for character classes, even if they are not used today.

Vertical tab, `\v` in some regular expression systems, is not supported as it ambiguous across regular expression languages. Use `\x0b` instead.

### Assertions

Assertions match a particular position in the text, but do not consume any characters.

| Feature | Description |
|---------|---------|
| Start of line | `^`, matches the beginning of the text, or of a line if **MatchOptions.Multiline** is used. |
| End of line | `$`, matches the end of the text, or of a line if **MatchOptions.Multiline** is used. |
| Lookahead | `(?=a)` and `(?!a)`, matches ahead for a pattern.
| Lookbehind | `(?<=b)` and `(?<!b)`, matches behind for a pattern.
| Word breaks | `\b` and `\B`, using the Unicode definition of letters `[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}\p{Lm}]`. |

`$` will match the end of a line, including any trailing `\r\n`, `\r` or `\n`.

### Character classes

| Feature | Description |
|---------|---------|
| Dot | `.`, matches everything except `\r` and `\n` unless **MatchOptions.DotAll** is used. |
| Character class | `[abc]` list of characters, `[a-fA-f0-9]` range of characters, `[^a-z]` everything but these characters. Character classes cannot be nested, subtracted, or intersected, and the same character cannot appear twice in the character class (except for a hyphen). |  
| Word characters | `\w` and `\W` using the Unicode definition of letters `[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}\p{Lm}]`. `\W` cannot be used in a negative character class.|
| Digit characters | `\d` includes the digits `0` to`9` and `\p{Nd}`, `\D` matches everything except characters matched by `\d`. `\D` cannot be used in a negative character class.|
| Space characters | `\s` includes spacing characters `[ \r\n\t\f\x0B\x85\p{Z}]`, `\S` which matches everything except characters matched by `\s`, `\r` carriage return, `\n` newline, `\t` tab, `\f` form feed. `\S` cannot be used in a negative character class.|
| Unicode character category | `\p{Ll}` matches all Unicode lowercase letters, while `\P{Ll}` matches everything that is not a Unicode lowercase letter. `\P{}` cannot be used in a negative character class. |

To increase clarity and avoid ambiguity, square bracket character classes are more restrictive than in other regular expression languages:
- Literal hyphen characters must be escaped. Use `[\-a]` instead of `[-a]` to match `-` or `a`.
- Beginning square brackets must be escaped. Use `[\[a]` instead of `[[]` to match `[` or `a`.
- Unless it is the first character and indicating negation, the character must be escaped. Use `[a\^]` instead of `[a^]` to match `^` or `a`.
- Character classes cannot be empty and `[]` is not supported. To include a closing square bracket in a character class, escape it.

Unicode character categories supported by `\p{}` and `\P{}`:
- Letters: `L`, `Lu`, `Ll`, `Lt`, `Lm`, `Lo`
- Marks: `M`, `Mn`, `Mc`, `Me`
- Numbers: `N`, `Nd`, `Nl`, `No`
- Punctuation: `P`, `Pc`, `Pd`, `Ps`, `Pe`, `Pi`, `Pf`, `Po`
- Symbols: `S`, `Sm`, `Sc`, `Sk`, `So`
- Separators: `Z`, `Zs`, `Zl`, `Zp`
- Control and Format: `Cc`, `Cf`, while other `C` prefix categories are not supported.

`\W`, `\D`, `\S`, and `\P{}` cannot be used within a negated character class `[^...]`. In order to be implemented on some platforms, these are translated to their Unicode equivalents which could be difficult to do if also negated.

### Quantifiers

| Feature | Description |
|---------|---------|
| Greedy zero or one | `?` matches 0 or 1 times, with as *large* a match as possible. |
| Greedy zero or more | `*` matches 0 or more times, with as *large* a match as possible. |
| Greedy one or more | `+` matches 1 or more times, with as *large* a match as possible. |
| Greedy at least n | `{n,}` matches at least *n* times, with as *large* a match as possible. For example, `a{3,}` will match all the characters in `aaaaa`. |
| Greedy between n and m | `{n,m}` matches between *n* and *m* times, with as *large* a match as possible. For example, `a{1,3}` will match the first 3 characters of `aaaaa`. |
| Lazy zero or one | `??` matches 0 or 1 times, with as *small* a match as possible. |
| Lazy zero or more | `*?` matches 0 or more times, with as *small* a match as possible. |
| Lazy one or more | `+?` matches 1 or more times, with as *small* a match as possible. |
| Lazy at least n | `{n,}?` matches at least *n* times, with as *small* a match as possible. For example, `a{3,}` will match the first three characters in `aaaaa`. |
| Lazy between n and m | `{n,m}?` matches between *n* and *m* times, with as *small* a match as possible. For example, `a{1,3}` will match the first character of `aaaaa`. |
| Exact n | `{n}` matches *n* times, exactly. For example, `a{3}` will match exactly 3 characters of `aaaaa`. |

Possessive quantifiers are not supported.

### Groups

| Feature | Description |
|---------|---------|
| Group | `(` and `)` are used to group elements for quantifiers to be applied. For example `(abc)+` matches `abcabc`. |
| Alternation | `a|b` matches "a" or "b", often used in a group. |
| Named group and back reference | `(?<name>chars)` captures a sub-match with the name `name`, referenced with `\k<name>`. Cannot be used if **MatchOptions.NumberedSubMatches** is enabled. |
| Numbered group and back reference | When **MatchOptions.NumberedSubMatches** is enabled, `(a)` captures a sub-match referenced with `\1`. |
| Non-capture group | `(?:a)`, creates group without capturing the result as a named or numbered sub-match. All groups are non-capturing unless **MatchOptions.NumberedSubMatches** is enabled. |

Named and numbered sub-matches cannot be used together. By default, named sub-matches are enabled and are preferred for clarity and maintainability, while standard capture groups become non capture groups with improved performance. This can be changed with **MatchOptions.NumberedSubMatches** which provides for traditional capture groups but disables named captures groups. Some implementations treat a mix of numbered and named capture groups differently which is why Power Fx disallows it. 

Self referencing capture groups are not supported, for example the regular expression `(a\1)`.

Two capture groups cannot share the same name, for example the regular expression `(?<id>\w+)|(?<id>\d+)` is not supported.

Some implementations offer an "explicit capture" option to improve performance which is unnecessary in Power Fx as it is effectively the default. **MatchOptions.NumberedSubMatches** disables it and enables implicit numbered captures.

In some situations, in particular with 0 match allowed quantifiers, **SubMatchess** can return different results between two different Power Fx implementations. See [Differences between implementations](#differences-between-implementations) for more information.

### Comments

| Feature | Description |
|---------|---------|
| Inline comments | `(?# comment here)`, which is ignored as a comment. Comment ends with the next close parenthesis, even if an opening parenthesis is in the comment.|

See **MatchOptions.FreeSpacing** for an alternative for formatting and commenting regular expressions.

### Inline options

| Feature | Description |
|---------|---------|
| Inline options | `(?im)` is the same as using **MatchOptions.IgnoreCase** and **MatchOptions.Multiline**. Must be used at the beginning of the regular expression. |

Supported inline modes are `[imsx]`, corresponding to **MatchOptions.IgnoreCase**, **MatchOptions.Multiline**, **MatchOptions.DotAll**, and **MatchOptions.FreeSpacing**, respectively. 

Some regular expression systems support changing the options in the middle of a regular expression or apply an option to only a part of the regular expression; Power Fx does not.

## Options

Match options change the behavior of regular expression matching. There are two ways to enable each modifier:
- **MatchOptions** enum value passed as the third argument to **Match**, **MatchAll**, and **IsMatch**.  Mode modifiers can be combined with the `&` operator, for example `MatchOptions.DotAll & MatchOptions.FreeSpacing`.
- `(?...)` prefix at the very beginning of the regular expression.  Mode midfiers can be combined with multiple letters in the `(?...)` construct, for example `(?sx)`.  Some options do not have a `(?...)` equivalent but may have other ways to get the same effect.

### Contains

Enabled with **MatchOptions.Contains** without a regular expression text equivalent. This is the default.

### Complete

Enabled with **MatchOptions.Complete** or use `^` and `$` at the beginning and of the regular expression, respectively.

### BeginsWith

Enabled with **MatchOptions.BeginsWith** or use `^` at the beginning and of the regular expression.

### EndsWith

Enabled with **MatchOptions.EndsWith** or use `$` at the end of the regular expression.

### DotAll

Enabled with **MatchOptions.DotAll** or `(?s)` at the start of a regular expression.

Normally the dot `.` operator will match all characters except newline characters `Char(10)` and `Char(13)`. With the **DotAll** modifier, all characters are matched, including newlines.

In this example, only the "Hello" is matched as the newline after it will not be matched by a `.` by default:

```powerapps-dot
Trim( Match( "Hello
              World", ".*" ).FullMatch )
// returns 
// "Hello"
```

But if we add the **DotAll** modifier, then the newline and all subsequent characters will be matched:

```powerapps-dot
Trim( Match( "Hello
              World", ".*", MatchOptions.DotAll ).FullMatch )
// returns 
// "Hello
// World"
```

### FreeSpacing

Enabled with **MatchOptions.FreeSpacing** or `(?x)` at the start of a regular expression.

Free spacing makes it easier to read and maintain a complex regular expression. The rules are simple:
- Space characters are ignored in the regular expression, including tabs and newline characters. If matching a space is desired, use `\s`, `\ `, `\t`, `\r`, or `\n`.
- `#` begins a comment which runs until the end of the line. It and all characters that follow up to the next newline character are ignored.
- Characters classes are not included in these changes. Space characters and `#` act as they normally do. For example, `IsMatch( "a#b c", "(?x)a[ #]b[ #]c" )` returns *true*. Some regular expression languages include character classes in free spacing, or provide an option to include them, but Power Fx does not at this time.

For example, here is a complex regular expression for matching an ISO [8601 date time](https://en.wikipedia.org/wiki/ISO_8601):

```powerapps-dot
IsMatch( 
    "2025-01-17T19:38:49+0000",
    "^\d{4}-(0\d|1[012])-([012]\d|3[01])(T([01]\d|2[0123]):[0-5]\d(:[0-5]\d(\.\d{3})?)?(Z|[\-+]\d{4}))?$"
)
// returns true
```

And here is the identical regular expression with free spacing utilizing multiple lines, indentation for groups, and regular expression comments, making this version much easier to understand, validate, and maintain.

```powerapps-dot
IsMatch( "2025-01-17T19:38:49+0000", 
    "(?x)                 # enables free spacing, must be very first
    ^                     # matches from beginning of text
    \d{4}                 # year (0000-9999)
    -(0\d|1[012])         # month (00-12)
    -([012]\d|3[01])      # day (00-31, range not checked against month)
    (T([01]\d|2[0123])    # optional time, starting with hours (00-23)
      :[0-5]\d            # minutes (00-59)
      (:[0-5]\d           # optional seconds (00-59)
        (\.\d{3})?        # optional milliseconds (000-999)
      )?
      (Z|[\-+]\d{4})      # time zone
    )?
    $                     # matches to end of text
    "
)
// returns true
```

### IgnoreCase

Enabled with **MatchOptions.IgnoreCase** or `(?i)` at the start of a regular expression.

Matches text in a letter case insensitive: upper case letters match lower case letters and lower case letters match upper case letters. 

For example:

```powerapps-dot
IsMatch( "HELLO!", "hello", MatchOptions.IgnoreCase )
// returns true

IsMatch( "file://c:/temp/info.txt", "^FILE://", MatchOptions.IgnoreCase )
// returns true
```

Most parts or Power Fx are culture aware, but not here. Using culture invariant matching is the industry standard for regular expressions, including in JavaScript and Perl. It is particularly useful in the second example where a system resource is being matched, in for example the `tr-TR` culture where `I` is not the uppercase equivalent of `i`.

If a culture aware, case insensitive match is needed, use characters class with the matching characters instead, for example `[Hh][Ee][Ll][Ll][Oo]` for the first example.

### Multiline

Enabled with **MatchOptions.Multiline** or `(?m)` at the start of a regular expression.

Normally, `^` and `$` anchors match the beginning and of the input text. With the **Multiline** modifier, these anchors will match the beginning and end of lines in the input text, where each line ends with `\r`, `\n`, `\r\n`, or the end of the input.  For example:

```powerapps-dot
MatchAll( "Hello" & Char(13) & Char(10) & "World", "^.+$" )
// returns 
// "Hello"
```

## Differences between implementations

As stated in the introduction, Power Fx's regular expressions are intentionally limited to features that can be consistently implemented on top of .NET, JavaScript, and other programming language regular expression engines. Authoring time errors prevent use of features that are not a part of this set. 

Despite this, although a feature may be supported across all implementations of Power Fx, there may be small semantic differences in how each of the implementations behaves.

In general, **FullMatch** will be consistent across all implementations. Differences emerge with **SubMatches**, either named or numbered, in particular when used with quantifiers that includes a zero match possibility (for example, `*`, `?`, and `{0,5}`) that could be satisfied in multiple ways. If a backreference is used to one of these, then the **FullMatch** may also be different. 

To avoid these differences in your formulas:
- Avoid quantifiers outside of a **SubMatch**.
- Test your regular expressions thoroughly, especially those involving **SubMatches** and backreferences.
- Be particularly careful if your Power Fx regular expression is used in a module across products.

Let's look at some examples:

```powerapps-dot
Match( "ab", "(a*)+(b)" , MatchOptions.NumberedSubMatches )
// returns with .NET: {FullMatch:"ab", StartMatch:1, SubMatches:["", "b"]}
// returns with JavaScript: {FullMatch:"ab", StartMatch:1, SubMatches:["a", "b"]}
```

On a Power Fx implementation based on .NET, **Index( SubMatches, 2 )** will return an empty string `""`, while using JavaScript returns `"a"`. This difference is caused by how the different engines treat the `(a*)+`. In JavaScript it is satisfied by a single `a` as one iteration of the `+`, but on .NET it is satisfied by two iterations of the `+` with `a` followed by an empty string. The **FullMatch** is the same for both implementations as `"ax"` but we get there through two different paths.

Here's an example with a backreference:

```powerapps-dot
Match( "ab", "(a)*b\1" , MatchOptions.NumberedSubMatches )
// returns with .NET: Blank()
// returns with JavaScript: {FullMatch:"b", StartMatch:2, SubMatches:[Blank()]}
```

Normally the engine is greedy and will match the `a` in the sub match. But this won't satisfy the full regular expression, and so .NET returns `blank`. JavaScript decides that the sub match isn't possible, so it chooses a different path where the sub match didn't happen at all. 

Without the backreference, or with the `*` inside the sub match, we have consistency between implementations:

```powerapps-dot
>> Match( "ab", "(a)*b" , MatchOptions.NumberedSubMatches )
// returns: {FullMatch:"ab", StartMatch:1, SubMatches:["a"]}

>> Match( "ab", "(a*)b\1" , MatchOptions.NumberedSubMatches )
// returns: {FullMatch:"b", StartMatch:2, SubMatches:[""]}
```


