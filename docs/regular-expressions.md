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

Regular expressions are very powerful and are used for a wide variety of purposes. They also often look like a random sequence of punctuation marks. This article doesn't describe all aspects of regular expressions, but a wealth of information, tutorials, and tools are available online.

Regular expressions have a long history and are available in many programming languages. Every programming language has its own dialect of regular expressions and there are few standards. As much as possible, we would like the same regular expression to give the same result across all Power Fx implementations. That isn't easy to accomplish as Power Fx runs on top of JavaScript and .NET which have significant differences. To accommodate running on different platforms, Power Fx regular expressions are limited to a subset of features that are widely supported across the industry.

As a result, some regular expressions that may work in other environments will be blocked in Power Fx. Power Fx will produce an authoring time error when unsupported features are encountered. This is one of the reasons that the regular expression and options must be an authoring time constant and not dynamic (for example, provided in a variable).

## Power Apps

Note that Power Apps uses a pre-V1 regular expression engine and enums, more closely tied to what JavaScript supports. The information in this article will be similar, but different, than Power Apps behavior in some ways. With Power Fx V1 enabled, regular expressions in Power Apps will match the behavior described here.

## Supported features

Power Fx supports the following regular expression features, with notes on how Power Fx behavior may differ from other systems.

The regular expression must be a constant and not calculated or stored in a variable. Using the `&` operator, string interpolation `$"{...}"`, and the `Concatenate`, `Char`, and `UniChar` functions with constant arguments is supported.

### Literal characters

| Feature | Description |
|---------|---------|
| Literal characters | Any Unicode character can be inserted directly, except `\`, `[`, `]`, `^`, `$`, `.`, `|`, `?`, `*`, `+`, `(`, `)`, `{`, and `}`. When using **MatchOptions.FreeSpacing**, `#`, ` `, and other `\s` space characters must be escaped as they have a different meaning. |
| Escaped literal characters | `\` (backslash) followed by one of the direct literal characters, such as `\?` to insert a question mark. `\#` and `\ ` may also be used even when **MatchOptions.FreeSpacing** is disabled for consistency. | 
| Hexadecimal and Unicode character codes | `\x20` with exactly two hexadecimal digits, `\u2028` with exactly four hexadecimal digits and can be used for high and low surrogates. |
| Unicode code point | `\u{01F47B}` with up to 8 hexadecimal digits. Must be in the range 0 to U+10FFFF and cannot be used for either a high or low surrogate. May result in a surrogate pair (two characters) if greater than U+FFFF. |
| Carriage return | `\r`, the same as `Char(13)`. |
| Newline character | `\n`, the same as `Char(10)`. |
| Form feed | `\f`, the same as `Char(12)`. |
| Horizontal Tab | `\t`, the same as `Char(9)`. |

Octal codes for characters, such as `\044` or `\o{044}` are disallowed, as they can be ambiguous with numbered back references. Use `\x` or `\u` instead. 

`\v` is not supported as it ambiguous across regular expression languages. Use `\x0b` for a vertical tab or `[\x0b\f\r\n\x85\u2028\u2029]` for vertical whitespace.

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

Lookahead and lookbehind assertions cannot contain submatches or unlimited quantifiers within, and cannot be used with a quantifier outside. 

### Character classes

| Feature | Description |
|---------|---------|
| Dot | `.`, matches everything except `\r` and `\n` unless **MatchOptions.DotAll** is used. |
| Character class | `[abc]` list of characters, `[a-fA-f0-9]` range of characters, `[^a-z]` everything but these characters. Character classes cannot be nested, subtracted, or intersected, and many punctuation marks may not appear twice in a row (`@@`, `%%`, `!!`, etc). |  
| Word characters | `\w` and `\W` using the Unicode definition of letters `[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}\p{Lm}]`. `\W` cannot be used in a negative character class.|
| Digit characters | `\d` includes the digits `0` to`9` and `\p{Nd}`, `\D` matches everything except characters matched by `\d`. `\D` cannot be used in a negative character class.|
| Space characters | `\s` includes spacing characters `[ \t\n\x0b\f\r\x85\u2028\u2029\p{Z}]`, `\S` which matches everything except characters matched by `\s`. `\S` cannot be used in a negative character class.|
| Unicode character category | `\p{Ll}` matches all Unicode lowercase letters, while `\P{Ll}` matches everything that is not a Unicode lowercase letter. `\P{}` cannot be used in a negative character class. |

To increase clarity and avoid ambiguity, square bracket character classes are more restrictive than in other regular expression languages:
- Literal hyphen characters must be escaped. Use `[\-a]` instead of `[-a]` to match `-` or `a`.
- Beginning square brackets must be escaped. Use `[\[a]` instead of `[[]` to match `[` or `a`.
- Unless it is the first character and indicating negation, the character must be escaped. Use `[a\^]` instead of `[a^]` to match `^` or `a`.
- Curly braces must be escaped. Use `[\{\}]` to match `{` or `}`.
- Empty character class `[]` is not supported. To include a closing square bracket in a character class, escape it.

Unicode character categories supported by `\p{}` and `\P{}`:
- Letters: `L`, `Lu`, `Ll`, `Lt`, `Lm`, `Lo`
- Marks: `M`, `Mn`, `Mc`, `Me`
- Numbers: `N`, `Nd`, `Nl`, `No`
- Punctuation: `P`, `Pc`, `Pd`, `Ps`, `Pe`, `Pi`, `Pf`, `Po`
- Symbols: `S`, `Sm`, `Sc`, `Sk`, `So`
- Separators: `Z`, `Zs`, `Zl`, `Zp`
- Control and Format: `Cc`, `Cf`, while other `C` prefix categories are not supported.

`\W`, `\D`, `\S`, and `\P{}` cannot be used within a negated character class `[^...]`. In order to be implemented on some platforms, these are translated to their Unicode equivalents which could be difficult to do if also negated.

Unicode characters at or above U+10000, requiring surrogate pairs, are not supported in character classes.

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
| Lazy at least n | `{n,}?` matches at least *n* times, with as *small* a match as possible. For example, `a{3,}?` will match only the first three characters in `aaaaa`. |
| Lazy between n and m | `{n,m}?` matches between *n* and *m* times, with as *small* a match as possible. For example, `a{1,3}?` will match only the first character of `aaaaa`. |
| Exact n | `{n}` matches *n* times, exactly. For example, `a{3}` will match exactly 3 characters of `aaaaa`. |

Possessive quantifiers are not supported.

Mixing submatches and quantifiers has limitations. See [Possibly empty submatches](#possibly-empty-submatches) for more information.

### Groups

| Feature | Description |
|---------|---------|
| Group | `(` and `)` are used to group elements for quantifiers to be applied. For example `(abc)+` matches `abcabc`. |
| Alternation | `a|b` matches "a" or "b", often used in a group. |
| Named submatch and back reference | `(?<name>chars)` captures a submatch with the name `name`, referenced with `\k<name>`. Cannot be used if **MatchOptions.NumberedSubMatches** is enabled. |
| Numbered submatch and back reference | When **MatchOptions.NumberedSubMatches** is enabled, `(a)` captures a submatch referenced with `\1`. |
| Non-capture group | `(?:a)`, creates group without capturing the result as a named or numbered submatch. All groups are non-capturing unless **MatchOptions.NumberedSubMatches** is enabled. |

Named and numbered submatches cannot be used together. By default, named submatches are enabled and are preferred for clarity and maintainability, while standard capture groups become non capture groups with improved performance. This can be changed with **MatchOptions.NumberedSubMatches** which provides for traditional capture groups but disables named capture groups. Some implementations treat a mix of numbered and named capture groups differently which is why Power Fx disallows it. 

Self referencing capture groups are not supported, for example the regular expression `(a\1)`.

Two capture groups cannot share the same name, for example the regular expression `(?<id>\w+)|(?<id>\d+)` is not supported.

The name of a named submatch must begin with a `\p{L}` character or `_`, and can continue with those characters plus `\p{Nd}`. Names are limited in length to 62 UTF-16 code units.

Backreferences to possibly empty submatches and to submatches within a look behind or look ahead are also not supported. 

Some implementations offer an "explicit capture" option to improve performance which is unnecessary in Power Fx as it is effectively the default. **MatchOptions.NumberedSubMatches** disables it and enables implicitly numbered captures.

Mixing submatches and quantifiers has limitations. See [Possibly empty submatches](#possibly-empty-submatches) for more information.

### Comments

| Feature | Description |
|---------|---------|
| Inline comments | `(?# comment here)`, which is ignored as a comment. Comment ends with the next close parenthesis, even if an opening parenthesis is in the comment.|

See **MatchOptions.FreeSpacing** for an alternative for formatting and commenting regular expressions.

### Inline options

| Feature | Description |
|---------|---------|
| Inline options | `(?im)` is the same as using **MatchOptions.IgnoreCase** and **MatchOptions.Multiline**. Must be set at the beginning of the regular expression. |

Supported inline modes are `[imsx]`, corresponding to **MatchOptions.IgnoreCase**, **MatchOptions.Multiline**, **MatchOptions.DotAll**, and **MatchOptions.FreeSpacing**, respectively. `n` is also accepted for compatibility but has no effect as it is the default and is incompatible with **MatchOptions.NumberedSubMatches**.

Inline options cannot be used to disable an option or set an option for a sub-expression.

## Options

Match options change the behavior of regular expression matching. There are two ways to enable options, which can be mixed so long as there is no conflict:
- **MatchOptions** enum value passed as the third argument to **Match**, **MatchAll**, and **IsMatch**.  Options can be combined with the `&` operator or `Concatenate` function, for example `MatchOptions.DotAll & MatchOptions.FreeSpacing`. All of the regular expression functions require that **MatchOptions** is a constant value, it cannot be calculated or stored in a variable. 
- `(?...)` prefix at the very beginning of the regular expression.  Options can be combined with multiple letters in the `(?...)` construct, for example `(?sx)`.  Some options do not have a `(?...)` equivalent but may have other ways to get the same effect, for example **MatchOptions.BeginsWith** is the equivalent of `^` at the beginning of the regular expression.

### Contains

Enabled with **MatchOptions.Contains** without a regular expression text equivalent. This is the default.

### Complete

Enabled with **MatchOptions.Complete** or use `^` and `$` at the beginning and of the regular expression, respectively.

### BeginsWith

Enabled with **MatchOptions.BeginsWith** or use `^` at the beginning and of the regular expression.

### EndsWith

Enabled with **MatchOptions.EndsWith** or use `$` at the end of the regular expression.

### DotAll

Enabled with **MatchOptions.DotAll** or `(?s)` at the start of the regular expression.

Normally the dot `.` operator will match all characters except newline characters `[\n\x0b\f\r\x85\u2028\u2029]`. With the **DotAll** modifier, all characters are matched, including newlines.

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
- Space characters are ignored in the regular expression, including all characters that would match `\s`. If matching a space is desired, use `\s`, `\ `, `\t`, `\r`, or `\n`.
- `#` begins a comment which runs until the end of the line. It and all characters that follow up to the next newline character (characters not matched by `.` without **MatchOptions.DotAll**) are ignored.
- Characters classes are not included in these changes. Space characters and `#` act as they normally do. For example, `IsMatch( "a#b c", "(?x)a[ #]b[ #]c" )` returns *true*. Some regular expression languages include character classes in free spacing, or provide an option to include them, but Power Fx does not.

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

## Predefined patterns

Predefined patterns provide a simple way to match either one of a set of characters or a sequence of multiple characters. Use the [string-concatenation operator **&**](operators.md) to combine your own text strings with members of the **Match** enum:

| Match enum            | Description                                                                                              | Regular expression    |
| --------------------- | -------------------------------------------------------------------------------------------------------- | --------------------- |
| **Any**               | Matches any character.                                                                                   | `.`                   |
| **Comma**             | Matches a comma.                                                                                         | `,`                   |
| **Digit**             | Matches a single digit ("0" through "9").                                                                | `\d`                  |
| **Email**             | Matches an email address that contains an "at" symbol ("\@") and a domain name that contains a dot (".") | *see note*            |
| **Hyphen**            | Matches a hyphen.                                                                                        | `-` *see note*        |
| **LeftParen**         | Matches a left parenthesis "(".                                                                          | `\(`                  |
| **Letter**            | Matches a letter.                                                                                        | `\p{L}`               |
| **MultipleDigits**    | Matches one or more digits.                                                                              | `\d+`                 |
| **MultipleLetters**   | Matches one or more letters.                                                                             | `\p{L}+`              |
| **MultipleNonSpaces** | Matches one or more characters that don't add whitespace (not space, tab, or newline).                   | `\S+`                 |
| **MultipleSpaces**    | Matches one or more characters that add whitespace (space, tab, or newline).                             | `\s+`                 |
| **NonSpace**          | Matches a single character that doesn't add whitespace.                                                  | `\S`                  |
| **OptionalDigits**    | Matches zero, one, or more digits.                                                                       | `\d*`                 |
| **OptionalLetters**   | Matches zero, one, or more letters.                                                                      | `\p{L}*`              |
| **OptionalNonSpaces** | Matches zero, one, or more characters that don't add whitespace.                                         | `\S*`                 |
| **OptionalSpaces**    | Matches zero, one, or more characters that add whitespace.                                               | `\s*`                 |
| **Period**            | Matches a period or dot (".").                                                                           | `\.`                  |
| **RightParen**        | Matches a right parenthesis ")".                                                                         | `\)`                  |
| **Space**             | Matches a character that adds whitespace.                                                                | `\s`                  |
| **Tab**               | Matches a tab character.                                                                                 | `\t`                  |

For example, the pattern **"A" & Match.MultipleDigits** will match the letter "A" followed by one or more digits.

The **Match.Email** is more complicated than the rest. It is designed to detect and extract common email address of the form **local@hostname.tld**, including support for international characters. Use it to validate a form that takes an email address as an input, as a quick test that the input is in an email form. If you want to see the regular expression, evaluate the formula `Text( Match.Email )`.

However, **Match.Email** does not validate that the email address conforms to all of the many evolving standards for email addresses, hostnames, and top-level-domains. That would require a very complicated regular expression that would need to be updated from time to time. Although the vast majority of email address will be treated as one would expect, **Match.Email** will match some things which are not legal, for example an underscore in the hostname, and not match some that are legal, for example quoted email addresses or IP addresses. If needed, there are many regular expressions on the web for detecting a truly legal email address.

In Power Apps, when not using Power Fx 1.0, **Match.Email** and **Match.Hyphen** have slightly different definitions. **Match.Email** is simpler, but is not suitable for extracting an email address as it will capture spaces while **Match.Hyphen** was escaped outside of a character class which is now illegal.

### NumberedSubMatches

Enabled with **MatchOptions.NumberedSubMatches** with no inline option. `(?n)` is supported as the opposite of this option for compatibility and is the default.

By default, `(...)` does not capture, the equivalent of what most systems call "explicit capture". To capture, use a named capture with `(?<name>...)` with backreference `\k<name>`. This improves performance of the regular expression by not capturing groups that do not need to be captures and improving clarity by using names instead of numbers that can change.

If you have an existing regular expression, it may depend on groups being captured automatically and numbered, including numbered back references. This is available by using the **MatchOptions.NumberedSubMatches** option.

Named and numbered submatches cannot be used together. Some implementations treat a mix of numbered and named capture groups differently which is why Power Fx disallows it. 

## Possibly empty submatches

As stated in the introduction, Power Fx's regular expressions are intentionally limited to features that can be consistently implemented on top of .NET, JavaScript, and other programming language regular expression engines. Authoring time errors prevent the use of features that are not a part of this set. 

One area that can be significantly different between implementations is how empty submatches are handled. For example, consider the regular expression `(?<submatch>a*)+` asked to match the text `a`. On .NET, the submatch will result in an empty text string, while on JavaScript it will result in `a`. Both can be argued as correct implementations, as the `+` quantifier can be satisfied with an empty string since the contents of the group has a `*` quantifier.

To avoid different results across Power Fx implementations, submatches that could be empty cannot be used with a quantifier. Here are examples of how a submatch could be empty:

| Examples | Description |
|----------|-------------|
| `(?<submatch>a{0,}b*)+` | All of the contents of the submatch are optional and so the entire submatch may be empty. |
| `((<submatch>a)?b)+` | Due to the `?` outside the submatch, the submatch as a whole is optional. |
| `(?<submatch>a|b*)+` | Alternation within the submatch with something that could be empty could result in the entire submatch being empty. |
| `((?<submatch>a)|b)+` | Alternation outside the submatch could match `b` in which case the submatch would be empty.|

Note that the submatch in `(?<submatch>a+)+` cannot be empty, as there must be at least one `a` in he submatch, and is supported.

## Unicode

Power Fx regular expressions use Unicode categories for the definitions of `\w`, `\d`, and `\s`, with specific categories available through `\p{..}`.

There can be some variation in these definitions across platforms. For example, the Unicode standard is updated from time to time with new characters which will later be implemented by platforms at their own pace. Variations of results between platforms on these character changes can be expected until all platforms are updated.

Power Fx regular expressions will ensure that category information is always available for the Basic Multilingual Plane (characters `\u0` to `\uffff`). Some platforms do not implement categories for characters in the Supplementary Multilingual Plane and above (U+10000 through U+10ffff). This is not usually a concern as characters in the Basic Multilingual Plane (`\u0` to `\uffff`) are the most commonly used. Consider using character values directly instead of categories if your scenario involves characters at or above U+10000 and test your formulas on the platforms you intend to use.

There can also be small edge case differences between platforms. For example, some platforms may not see `ſ` as matching `s` when **MatchOptions.IgnoreCase** is invoked. If these characters are important for your scenario, use a character class such as `[ſsS]` to match in a case insensitive manner that explicitly includes the characters desired.
