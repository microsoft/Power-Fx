// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// This file contains the source for AlterRegex in JavaScript.
// It is included here so that the test suite can compare results from .NET, JavaScript, and PCRE2.
// It is here in the Core library so that it can be extracted in the Canvas build and compared against the version stored there.

using System.Linq;

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_JavaScript
    {
        // This JavaScript function assumes that the regular expression has already been compiled and comforms to the Power Fx regular expression language.
        // For example, no affodance is made for nested character classes or inline options on a subexpression, as those would have already been blocked.
        // Stick to single ticks for strings to keep this easier to read and maintain here in C#.
        public const string AlterRegex_JavaScript = @"
            function AlterRegex_NeedToMapNewlines(regex, flags)
            {
                var index = 0;

                const inlineFlagsRE = /^\(\?(?<flags>[imnsx]+)\)/;
                const inlineFlags = inlineFlagsRE.exec( regex );
                if (inlineFlags != null)
                {
                    flags = flags.concat(inlineFlags.groups['flags']);
                    index = inlineFlags[0].length;
                }

                const multiline = flags.includes('m');
                const freeSpacing = flags.includes('x');

                var openCharacterClass = false; 

                for ( ; index < regex.length; index++)
                {
                    switch (regex.charAt(index) )
                    {
                        case '[':
                            openCharacterClass = true;
                            break;

                        case ']':
                            openCharacterClass = false;
                            break;

                        case '\\':
                            index++;
                            break;

                        case '^':
                            if (!openCharacterClass)
                                return true;
                            break;

                        case '$':
                            if (!openCharacterClass)
                                return true;
                            break;

                        case '(':
                            if (regex.length - index > 2 && regex.charAt(index+1) == '?' && regex.charAt(index+2) == '#')
                            {
                                // inline comment
                                for ( index++; index < regex.length && regex.charAt(index) != ')'; index++)
                                {
                                    // eat characters until a close paren, it doesn't matter if it is escaped (consistent with .NET)
                                }
                            }
                            break;

                        case '#':
                            if (freeSpacing && !openCharacterClass)
                            {
                                for ( index++; index < regex.length && regex.charAt(index) != '\r' && regex.charAt(index) != '\n'; index++)
                                {
                                    // eat characters until the end of the line
                                    // leaving dangling whitespace characters will be eaten on next iteration
                                }
                            }
                            break;
                    }
                }

                return false;
            }

            function AlterRegex_JavaScript(regex, flags, crCode, nlCode)
            {
                const otherNewLines = '';

                var index = 0;

                if (crCode > 0xffff || nlCode > 0xffff)
                    return [undefined, undefined];

                const cr = '\\u'.concat(crCode.toString(16).padStart(4,'0'));
                const nl = '\\u'.concat(nlCode.toString(16).padStart(4,'0')); 

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

                const caret = multiline ? '(?:(?<=^|' + cr + nl + '|[' + nl + otherNewLines + '])|(?<=' + cr + ')(?!' + nl + '))' : '^';
                const dollar = multiline ? '(?:(?=' + cr + nl + '|[' + cr + otherNewLines + ']|$)|(?<!' + cr + ')(?=' + nl + '))' : 
                                           '(?:(?=' + cr + nl + '$|[' + cr + otherNewLines + ']?$)|(?<!' + cr + ')(?=' + nl + '$))';

                // rebuilding from booleans avoids possible duplicate letters
                // x has been handled in this function and does not need to be passed on (and would cause an error)
                const alteredFlags = 'v'.concat((ignoreCase ? 'i' : ''), (multiline ? 'm' : ''), (dotAll ? 's' : ''));  

                var openCharacterClass = false;       // are we defining a character class?
                var altered = '';
                var spaceWaiting = false;
                var mainCharacterClass = '';
                var orCharacterClass = '';

                for ( ; index < regex.length; index++)
                {
                    var alteredToken = '';

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
                                const wordChar = '\\p{Ll}\\p{Lu}\\p{Lt}\\p{Lo}\\p{Lm}\\p{Mn}\\p{Nd}\\p{Pc}';
                                const spaceChar = '\\f\\t\\v\\x85\\p{Z}' + cr + nl + otherNewLines;
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

                                    case 'r':
                                        alteredToken = cr;
                                        break;

                                    case 'n':
                                        alteredToken = nl;
                                        break;

                                    case 'x':
                                        xCode = regex.charAt(++index) + regex.charAt(++index);
                                        if (xCode == '0d' || xCode == '0D' )
                                            alteredToken = cr;
                                        else if (xCode == '0a' || xCode == '0A' )
                                            alteredToken = nl;
                                        else 
                                            alteredToken = '\\x'.concat( xCode );
                                        break;

                                    case 'u':
                                        xCode = regex.charAt(++index) + regex.charAt(++index) + regex.charAt(++index) + regex.charAt(++index);
                                        if (xCode == '000d' || xCode == '000D' )
                                            alteredToken = cr;
                                        else if (xCode == '000a' || xCode == '000A' )
                                            alteredToken = nl;
                                        else 
                                            alteredToken = '\\u'.concat( xCode );
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
                                alteredToken = '[^' + cr + nl + otherNewLines + ']';
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

                        case ' ': case '\f': case '\n': case '\r': case '\t':
                            if (freeSpacing && !openCharacterClass)
                            {
                                spaceWaiting = true;
                            }
                            else
                            {
                                if (regex.charAt(index) == '\r' )
                                    alteredToken = cr;
                                else if ( regex.charAt(index) == '\n')
                                    alteredToken = nl;
                                else
                                    alteredToken = regex.charAt(index);
                                spaceWaiting = false;
                            }

                            break;

                        case '#':
                            if (freeSpacing && !openCharacterClass)
                            {
                                for ( index++; index < regex.length && regex.charAt(index) != '\r' && regex.charAt(index) != '\n'; index++)
                                {
                                    // eat characters until the end of the line
                                    // leaving dangling whitespace characters will be eaten on next iteration
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
