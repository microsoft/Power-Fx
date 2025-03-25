// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// This file contains the source for AlterRegex in JavaScript.
// It is included here so that the test suite can compare results from .NET, JavaScript, and PCRE2.
// It is here in the Core library so that it can be extracted in the Canvas build and compared against the version stored there.

using Microsoft.PowerFx.Core.Texl.Builtins;

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_JavaScript
    {
        // This JavaScript function assumes that the regular expression has already been compiled and comforms to the Power Fx regular expression language.
        // For example, no affodance is made for nested character classes or inline options on a subexpression, as those would have already been blocked.
        // Stick to single ticks for strings to keep this easier to read and maintain here in C#.
        
        // On the mapping of Lu, Ll, and Lt with MatchOptions.IgnoreCase:
        //
        // From https://www.pcre.org/current/doc/html/pcre2pattern.html:
        // Perl originally used the name L& for the Lc property. This is still supported by Perl, but discouraged.
        // PCRE2 also still supports it. This property matches any character that has the Lu, Ll, or Lt property, in other words,
        // any letter that is not classified as a modifier or "other".
        // From release 10.45 of PCRE2 the properties Lu, Ll, and Lt are all treated as Lc when case-independent matching
        // is set by the PCRE2_CASELESS option or (?i) within the pattern. The other properties are not affected by caseless matching.
        //
        // From https://github.com/dotnet/runtime/blob/889d514aace75eac9ed88bc9c155a82eb9dac78a/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCharClass.cs#L134
        //  // Letters
        //  { "Ll", "\u0002" }, // UnicodeCategory.LowercaseLetter + 1
        //  { "Lm", "\u0004" }, // UnicodeCategory.ModifierLetter + 1
        //  { "Lo", "\u0005" }, // UnicodeCategory.OtherLetter + 1
        //  { "Lt", "\u0003" }, // UnicodeCategory.TitlecaseLetter + 1
        //  { "Lu", "\u0001" }, // UnicodeCategory.UppercaseLetter + 1
        //  { "L", "\u0000\u0002\u0004\u0005\u0003\u0001\u0000" },
        //
        //  // InternalRegexIgnoreCase = {LowercaseLetter} OR {TitlecaseLetter} OR {UppercaseLetter}
        //  // !!!This category should only ever be used in conjunction with RegexOptions.IgnoreCase code paths!!!
        //  { InternalRegexIgnoreCase, "\u0000\u0002\u0003\u0001\u0000" },

        public const string AlterRegex_JavaScript = @"
            function AlterRegex_JavaScript(regex, flags, crCode, nlCode)
            {
                const cr = '\\r';
                const nl = '\\n';
                const newLinesWithoutCRLF = '" + MatchWhiteSpace.NewLineDoubleEscapesWithoutCRLF + @"';
                const newLines = cr + nl + newLinesWithoutCRLF;
                const spaceNewLineRE = /[" + MatchWhiteSpace.SpaceNewLineEscapes + @"]/u;   // note the /u on the end
                const newLineRE = RegExp( '[' + newLines + ']' );

                var index = 0;

                const inlineFlagsRE = /^\(\?(?<flags>[imnsx]+)\)/;
                const inlineFlags = inlineFlagsRE.exec( regex );
                if (inlineFlags != null)
                {
                    flags = flags.concat(inlineFlags.groups['flags']);
                    index = inlineFlags[0].length;
                }

                const freeSpacing = flags.includes('x');
                const multiline = flags.includes('m');
                const dotAll = flags.includes('s');
                const ignoreCase = flags.includes('i');
                const numberedSubMatches = flags.includes('N');

                const caret = multiline ? '(?:(?<=^|' + cr + nl + '|[' + nl + newLinesWithoutCRLF + '])|(?<=' + cr + ')(?!' + nl + '))' : '^';
                const dollar = multiline ? '(?:(?=' + cr + nl + '|[' + cr + newLinesWithoutCRLF + ']|$)|(?<!' + cr + ')(?=' + nl + '))' : 
                                           '(?:(?=' + cr + nl + '$|[' + cr + newLinesWithoutCRLF + ']?$)|(?<!' + cr + ')(?=' + nl + '$))';

                // rebuilding from booleans avoids possible duplicate letters
                // x has been handled in this function and does not need to be passed on (and would cause an error)
                // multiline is excluded as the definitions for caret and dollar above take this into account
                const alteredFlags = 'v'.concat((ignoreCase ? 'i' : ''), (dotAll ? 's' : ''));

                var openCharacterClass = false;
                var altered = '';
                var spaceWaiting = false;
                var mainCharacterClass = '';
                var orCharacterClass = '';

                for ( ; index < regex.length; index++)
                {
                    var alteredToken = '';

                    if (freeSpacing && !openCharacterClass && spaceNewLineRE.test(regex.charAt(index)))
                    {
                        spaceWaiting = true;
                    }
                    else
                    {
                        switch (regex.charAt(index) )
                        {
                            case '[':
                                openCharacterClass = true;
                                mainCharacterClass = '';
                                orCharacterClass = '';
                                spaceWaiting = false;
                                break;

                            case ']':
                                openCharacterClass = false;
                                if (mainCharacterClass != '' && orCharacterClass != '')
                                    altered = altered.concat('(?:[', mainCharacterClass, ']', orCharacterClass, ')');
                                else if(mainCharacterClass != '')
                                    altered = altered.concat('[', mainCharacterClass, ']');
                                else
                                    altered = altered.concat(orCharacterClass.substring(1));   // strip leading '|' deliniator
                                spaceWaiting = false;
                                break;

                            case '\\':
                                if (++index < regex.length)
                                {
                                    const letterCaseChar = '\\p{Ll}\\p{Lu}\\p{Lt}';
                                    const wordChar = letterCaseChar + '\\p{Lo}\\p{Lm}\\p{Mn}\\p{Nd}\\p{Pc}';
                                    const spaceChar = '" + MatchWhiteSpace.SpaceNewLineDoubleEscapes + @"';
                                    const digitChar = '\\p{Nd}';

                                    switch (regex.charAt(index))
                                    { 
                                        case 'w':
                                            alteredToken = ''.concat(openCharacterClass ? '' : '[', wordChar, openCharacterClass ? '' : ']');
                                            break;
                                        case 'W':
                                            if (openCharacterClass)
                                                orCharacterClass = orCharacterClass.concat( '|[^', wordChar, ']' );
                                            else
                                                alteredToken = ''.concat('[^', wordChar, ']');
                                            break;

                                        case 'b':
                                            alteredToken = `(?:(?<=[${wordChar}])(?![${wordChar}])|(?<![${wordChar}])(?=[${wordChar}]))`;
                                            break;
                                        case 'B':
                                            alteredToken = `(?:(?<=[${wordChar}])(?=[${wordChar}])|(?<![${wordChar}])(?![${wordChar}]))`;
                                            break;

                                        case 's':
                                            alteredToken = ''.concat(openCharacterClass ? '' : '[', spaceChar, openCharacterClass ? '' : ']');
                                            break;
                                        case 'S':
                                            if (openCharacterClass)
                                                orCharacterClass = orCharacterClass.concat( '|[^', spaceChar, ']' );
                                            else
                                                alteredToken = ''.concat('[^', spaceChar, ']');
                                            break;

                                        case 'p':
                                            // /\p{Lt}/i.exec('a') does not match in JavaScript, but does in .NET and PCRE2
                                            // Only if using ignoreCase, replace any of Ll, Lu, and Lt with all three of them
                                            if (ignoreCase && /\{L[lut]\}/.test(regex.substring(index+1, index+6)) )
                                            {
                                                alteredToken = ''.concat(openCharacterClass ? '' : '[', letterCaseChar, openCharacterClass ? '' : ']');

                                                index += 5;
                                            }
                                            else
                                            {
                                                alteredToken = '\\'.concat(regex.charAt(index));
                                            }
                                            break;
                                        case 'P':
                                            if (ignoreCase && /\{L[lut]\}/.test(regex.substring(index+1, index+6)) )
                                            {
                                                if (openCharacterClass)
                                                    orCharacterClass = orCharacterClass.concat( '|[' + letterCaseChar + ']' );
                                                else
                                                    alteredToken = ''.concat('[^', letterCaseChar, ']');

                                                index += 5;
                                            }
                                            else
                                            {
                                                alteredToken = '\\'.concat(regex.charAt(index));
                                            }
                                            break;

                                        case 'd':
                                            alteredToken = digitChar;
                                            break;
                                        case 'D':
                                            if (openCharacterClass)
                                                orCharacterClass = orCharacterClass.concat( '|[^', digitChar, ']' );
                                            else
                                                alteredToken = ''.concat('[^', digitChar, ']');
                                            break;

                                        // needed for free spacing, needs to be unescaped to avoid /v error
                                        case '#': case ' ':
                                            alteredToken = regex.charAt(index);
                                            break;

                                        default:
                                            alteredToken = '\\'.concat(regex.charAt(index));
                                            break;
                                    }
                                }
                                else
                                {
                                    // backslash at end of regex
                                    alteredToken = '\\';
                                }
                                spaceWaiting = false;
                                break;

                            case '.':
                                if (openCharacterClass || dotAll)
                                {
                                    alteredToken = '.';
                                }
                                else
                                {
                                    alteredToken = '[^' + newLines + ']';
                                }

                                spaceWaiting = false;
                                break;

                            case '^':
                                alteredToken = openCharacterClass ? '^' : caret;
                                spaceWaiting = false;
                                break;

                            case '$':
                                alteredToken = openCharacterClass ? '$' : dollar;
                                spaceWaiting = false;
                                break;

                            case '(':
                                if (regex.length - index > 2 && regex.charAt(index+1) == '?' && regex.charAt(index+2) == '#')
                                {
                                    // inline comment
                                    for ( index++; index < regex.length && regex.charAt(index) != ')'; index++)
                                    {
                                        // eat characters until a close paren, it doesn't matter if it is escaped (consistent with .NET)
                                    }

                                    spaceWaiting = true;
                                }
                                else
                                {
                                    alteredToken = '(';
                                    spaceWaiting = false;
                                }

                                break;

                            case '#':
                                if (freeSpacing && !openCharacterClass)
                                {
                                    for ( index++; index < regex.length && !newLineRE.test(regex.charAt(index)); index++)
                                    {
                                        // eat characters until the end of the line
                                        // leaving dangling whitespace characters, for example the \n after a \r, will be eaten on next iteration as whitespace
                                    }

                                    spaceWaiting = true;
                                }
                                else
                                {
                                    alteredToken = '#';
                                    spaceWaiting = false;
                                }

                                break;

                            case '*': case '+': case '?': case '{':
                                if (spaceWaiting && altered.length > 0 && altered.charAt(altered.length-1) == '(')
                                {
                                    alteredToken = '(?:)';
                                    spaceWaiting = false;
                                }
                                alteredToken = alteredToken.concat(regex.charAt(index));
                                spaceWaiting = false;
                                break;

                            default:
                                if (spaceWaiting)
                                {
                                    alteredToken = '(?:)';
                                    spaceWaiting = false;
                                }
                                alteredToken = alteredToken.concat(regex.charAt(index));
                                break;
                        }
                    }

                    if (openCharacterClass)
                        mainCharacterClass = mainCharacterClass.concat(alteredToken);
                    else
                        altered = altered.concat(alteredToken);
                }

                if (flags.includes('^'))
                {
                    altered = caret + altered;
                }       

                if (flags.includes('$'))
                {
                    altered = altered + dollar;
                }  
                    
                return [altered, alteredFlags];
            }
        ";
    }
}
