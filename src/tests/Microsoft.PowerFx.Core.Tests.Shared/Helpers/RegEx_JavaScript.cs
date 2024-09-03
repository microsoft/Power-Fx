// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// This file contains the source for AlterRegex in JavaScript.
// It is included here so that the test suite can compare results from .NET, JavaScript, and PCRE2.
// It is here in the Core library so that it can be extracted in the Canvas build and compared against the version stored there.

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_JavaScript
    {
        // This JavaScript function assumes that the regular expression has already been compiled and comforms to the Power Fx regular expression language.
        // For example, no affodance is made for nested character classes or inline options on a subexpression, as those would have already been blocked.
        // Stick to single ticks for strings to keep this easier to read and maintain here in C#.
        public const string AlterRegex_JavaScript = @"
            function AlterRegex_JavaScript(regex, flags)
            {
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

                // rebuilding from booleans avoids possible duplicate letters
                // x has been handled in this function and does not need to be passed on (and would cause an error)
                const alteredFlags = 'u'.concat((ignoreCase ? 'i' : ''), (multiline ? 'm' : ''), (dotAll ? 's' : ''));  

                var openCharacterClass = false;       // are we defining a character class?
                var altered = '';

                for ( ; index < regex.length; index++)
                {
                    switch (regex.charAt(index) )
                    {
                        case '[':
                            openCharacterClass = true;
                            altered = altered.concat('[');
                            break;

                        case ']':
                            openCharacterClass = false;
                            altered = altered.concat(']');
                            break;

                        case '\\':
                            if (++index < regex.length)
                            {
                                const wordChar = '\\p{Ll}\\p{Lu}\\p{Lt}\\p{Lo}\\p{Lm}\\p{Nd}\\p{Pc}';
                                const spaceChar = '\\f\\n\\r\\t\\v\\x85\\p{Z}';
                                const digitChar = '\\p{Nd}';

                                switch (regex.charAt(index))
                                { 
                                    case 'w':
                                        altered = altered.concat((openCharacterClass ? '' : '['), wordChar, (openCharacterClass ? '' : ']'));
                                        break;
                                    case 'W':
                                        altered = altered.concat('[^', wordChar, ']');
                                        break;

                                    case 'b':
                                        altered = altered.concat(`(?:(?<=[${wordChar}])(?![${wordChar}])|(?<![${wordChar}])(?=[${wordChar}]))`);
                                        break;
                                    case 'B':
                                        altered = altered.concat(`(?:(?<=[${wordChar}])(?=[${wordChar}])|(?<![${wordChar}])(?![${wordChar}]))`);
                                        break;

                                    case 's':
                                        altered = altered.concat((openCharacterClass ? '' : '['), spaceChar, (openCharacterClass ? '' : ']'));
                                        break;
                                    case 'S':
                                        altered = altered.concat('[^', spaceChar, ']');
                                        break;

                                    case 'd':
                                        altered = altered.concat(digitChar);
                                        break;
                                    case 'D':
                                        altered = altered.concat('[^', digitChar, ']');
                                        break;

                                    // needed for free spacing
                                    case '#': case ' ':
                                        altered = altered.concat(regex.charAt(index));
                                        break;

                                    default:
                                        altered = altered.concat('\\', regex.charAt(index));
                                        break;
                                }
                            }
                            else
                            {
                                // backslash at end of regex
                                altered = altered.concat( '\\' );
                            }

                            break;

                        case '.':
                            altered = altered.concat(!openCharacterClass && !dotAll ? '[^\\r\\n]' : '.');
                            break;

                        case '^':
                            altered = altered.concat(!openCharacterClass && multiline ? '(?<=^|\\r\\n|\\r|\\n)' : '^');
                            break;

                        case '$':
                            altered = altered.concat(openCharacterClass ? '$' : (multiline ? '(?=$|\\r\\n|\\r|\\n)' : '(?=$|\\r\\n$|\\r$|\\n$)'));
                            break;

                        case '(':
                            if (regex.length - index > 2 && regex.charAt(index+1) == '?' && regex.charAt(index+2) == '#')
                            {
                                // inline comment
                                for ( index++; index < regex.length && regex.charAt(index) != ')'; index++)
                                {
                                    // eat characters until a close paren, it doesn't matter if it is escaped (consistent with .NET)
                                }

                                if (numberedSubMatches && /\\[\d]+$/.test(altered))
                                {
                                    // add no op to separate tokens, for the case of \1(?#comment)1 which should not be interpreted as \11
                                    altered = altered.concat('(?:)');
                                }
                            }
                            else
                            {
                                altered = altered.concat('('); 
                            }

                            break;

                        case ' ': case '\f': case '\n': case '\r': case '\t':
                            if (!freeSpacing || openCharacterClass)
                            {
                                altered = altered.concat(regex.charAt(index));
                            }
                            if (numberedSubMatches && /\\[\d]+$/.test(altered))
                            {
                                // add no op to separate tokens, for the case of '\1 1' which should not be interpreted as '\11'
                                altered = altered.concat('(?:)'); 
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

                                if (numberedSubMatches && /\\[\d]+$/.test(alteredRegex))
                                {
                                    // add no op to separate tokens, for the case of '\1# commment\n1' which should not be interpreted as '\11'
                                    altered = altered.concat('(?:)'); 
                                }
                            }
                            else
                            {
                                altered = altered.concat('#');
                            }

                            break;

                        default:
                            altered = altered.concat(regex.charAt(index));
                            break;
                    }
                }

                if (flags.includes('^') && (altered.length == 0 || altered[0] != '^'))
                {
                    altered = '^' + altered;
                }       

                if (flags.includes('$') && (altered.length == 0 || altered[altered.length-1] != '$'))
                {
                    altered = altered + '$';
                }  
                    
                return [altered, alteredFlags];
            }
        ";
    }
}
