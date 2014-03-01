// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace mjr.QLexer
{
    // <summary>
    // This class implements a "smart lexer" that can identify nested function and method bodies hierarchically and separate their token streams to
    // allow lazy or parallel parsing. It can also figure out the names of those functions and method bodies and record which function or method
    // is their parent; in addition, it handles semicolon insertion as required by JavaScript, and it notes which functions and methods contain
    // "eval". It could easily also record all identifiers or assign functions and methods their child functions and methods, if either of
    // these were algorithmically useful.
    // </summary>
    public static class Lexer
    {
        #region Types and Constants

        // As we're lexing, we're searching for function and method bodies to separate their token streams out so they can be parsed lazily.
        // We also look for the names of these functions and methods so that they can be quickly found later for speculative parsing.
        // LazyLexState is used to indicate what type of token we're looking for right now.
        private enum LazyLexState
        {
            None,
            FindLBrace,
            FindIdentifier
        }

        // Constant representing invalid values
        private const int Invalid = -1;

        // <summary>
        // This class represents a stream of tokens that make up the body of a function or method.
        // </summary>
        private sealed class TokenStream
        {
            public List<int> Tokens { get; private set; }               // The tokens themselves.
            public int BraceCount { get; private set; }                 // How many open braces were there when this body began?
            public int RegexLegalIndex { get; set; }                    // Stores an index in Tokens where a regular expression is legal even though it ordinarily wouldn't be.
            public IdentifierRecord Name { get; private set; }          // (Possibly) the name of this function or method.
            public bool HasEval { get; set; }                           // Does this function or method contain any uses of eval?
            public JSFunctionMetadata Implementation { get; private set; }   // The implementation of this function or method.

            public TokenStream(int braceCount, IdentifierRecord name, LexInfo lexInfo) : this(braceCount, name, lexInfo, null) { }

            public TokenStream(int braceCount, IdentifierRecord name, LexInfo lexInfo, JSFunctionMetadata parent)
            {
                Tokens = new List<int>(Config.DefaultSizeOfTokenStream);
                BraceCount = braceCount;
                RegexLegalIndex = Invalid;
                Name = name;
                HasEval = false;
                Implementation = new JSFunctionMetadata(Tokens, lexInfo, parent);
            }
        }

        // <summary>
        // This struct (the value semantics are important to the correct operation of the code) represents a function or method name
        // that may or may not be correct. If IsSafe is true, then we are reasonably sure that this is the name of the upcoming function
        // or method definition.
        // </summary>
        private struct IdentifierRecord
        {
            public int Start;
            public int Length;
            public bool IsSafe;

            public IdentifierRecord(int start, int length, bool isSafe)
            {
                Start = start;
                Length = length;
                IsSafe = isSafe;
            }
        }

        // Literal regex patterns
        private static readonly Regex RegexPat =
		        new Regex(@"\G        # Anchor match to beginning of string.
				                ( \\.     # Match an escaped character,
				                | \[      # or a bracket expression...
					                ( \\.   # containing an escaped character...
					                | [^\\\]]*  # or a regular character...
					                )*      # zero or more times...
				                  \]			# terminated by an unescaped ],
				                | [^\\[/]*    # or a sequence of regular characters,
				                )*        # zero or more times.
			                  /         # After this must come a forward slash...
			                  [a-zA-Z0-9$_]* # and options (using identifier chars).
		                   ", RegexOptions.Singleline | RegexOptions.ExplicitCapture
		                    | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex DQStrPat = new Regex(@"\G([^""\\]|\\.)*""",
            RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static readonly Regex SQStrPat = new Regex(@"\G([^'\\]|\\.)*'",
            RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        #endregion

        private struct LexerImpl
        {
            #region Public State

            public List<JSFunctionMetadata> FunctionImplementations;     // All of the functions and methods we've lexed. (The last one will be the root or main function.)
            public LexInfo LexInfo;                                 // Meta-data about the entire input that we've collected.

            #endregion

            #region Internal State

            Stack<TokenStream> tokenStack;                          // The stack of token streams; the topmost one is the current function or method we're lexing.
            LazyLexState state;                                     // See the definition of LazyLexState for some discussion.
            int i;                                                  // The current character in the input.
            int end;                                                // The end of the input.
            int tokenLength;                                        // The length of the last token, for variable-length tokens like strings and identifiers.
            int braceCount;                                         // The current number of open braces.
            int parenCount;                                         // The current number of open parens.
            int parenCountAtKeyword;                                // The number of open parens at a keyword of interest to RegexLegalAfter.            
            string input;                                           // The input itself.
            bool newlineSinceLastToken;                             // The number of newlines since the last token; used for semicolon insertion.
            TokenType lastToken;                                    // The type of the last token.
            IdentifierRecord lastIdentifier;                        // The last identifier we encountered.
            IdentifierRecord callableNameByAssignment;              // The identifier on the LHS of the most recent assignment statement (i.e., for "o.x = y;", this'd be "x")
            IdentifierRecord callableNameByDefinition;              // The name on the most recent lambda (i.e., for "o.x = function f(x)", this'd be "f")

            #endregion

            #region Constructor

            public LexerImpl(string input_, int start, int length)
            {
                input = input_;
                i = start;
                end = i + length;
                tokenLength = 0;
                braceCount = 0;
                parenCount = 0;
                parenCountAtKeyword = Invalid;
                state = LazyLexState.None;
                newlineSinceLastToken = false;
                lastToken = 0;
                lastIdentifier = new IdentifierRecord(0, 0, false);
                callableNameByAssignment = new IdentifierRecord(0, 0, false);
                callableNameByDefinition = new IdentifierRecord(0, 0, false);
                FunctionImplementations = new List<JSFunctionMetadata>(10);
                LexInfo = new LexInfo(input_, FunctionImplementations, new Dictionary<string, List<JSFunctionMetadata>>());
                tokenStack = new Stack<TokenStream>();
                tokenStack.Push(new TokenStream(0, callableNameByAssignment, LexInfo));
            }

            #endregion

            #region Matching

            bool Match(string str, int len)
            {
                int cur = i + 1;

                if (len > (end - cur))
                    return false;

                for (int j = 0; j < len; ++j)
                    if (str[j] != input[cur + j])
                        return false;

                return true;
            }

            bool RegexMatchAt(int o, Regex r)
            {
                Match m = r.Match(input, i + o);

                if (m.Success)
                {
                    tokenLength = m.Length + o;
                    return true;
                }
                else
                    return false;
            }

            bool RegexMatch(Regex r) { return RegexMatchAt(1, r); }

            bool IsValidIdentifierCharacter(char c)
            {
                switch (c)
                {
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '$':
                    case '_':
                    case '@':   // Not part of ECMAScript / JavaScript, but we use this for intrinsics. TODO: Add a way to disable these.
                        return true;

                    default:
                        return false;
                }
            }

            bool KeywordMatch(string str, int len)
            {
                int cur = i + 1;
                int j = 0;

                if (len > (end - cur))
                    return false;

                for (; j < len; ++j)
                    if (str[j] != input[cur + j])
                        return false;

                if ((cur + j) < end && IsValidIdentifierCharacter(input[cur + j]))
                    return false;

                return true;
            }

            bool IdentifierMatchAt(int offset)
            {
                int cur = i + offset;

                while (cur < end && IsValidIdentifierCharacter(input[cur]))
                    cur++;

                tokenLength = cur - i;

                return (tokenLength > 0);
            }

            bool IdentifierMatch()
            {
                return IdentifierMatchAt(1);
            }

            // This is nonsense that shouldn't exist; it violates the standard.
            // However, the major browsers do allow embedded HTML comments at the
            // end of JavaScript within a script tag without hiding the comments
            // using //, as the standard requires you to do. Thus we need to be
            // bug-compatible.
            bool HTMLCommentMatch()
            {
                if ((end - i) < 3 || input[i + 1] != '-' || input[i + 2] != '>')
                    return false;

                int cur = i + 3;

                while (cur < end)
                {
                    if (input[cur] != ' ' && input[cur] != '\n' &&
                        input[cur] != '\r' && input[cur] != '\v')
                        return false;
                    cur++;
                }

                tokenLength = cur - i;

                return true;
            }

            // The below numerical matching functions are meant to implement the following regexes. These regexes actually have some bugs which
            // the matching functions do not, so they are not 100% identical. Unfortunately, the matching functions are pretty difficult to read;
            // this is why regexes are such a nice DSL. (But unfortunately too slow for our uses.)

            //private static Regex HexPat = new Regex(@"\G[xX][0-9a-fA-F]*", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
            //private static Regex OctPat = new Regex(@"\G[0-7]*", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
            //private static Regex DotDecPat = new Regex(@"\G[0-9]+([eE][+-]?[0-9]+)?", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
            //private static Regex DecPat = new Regex(@"\G(0|[1-9][0-9]*)?(\.[0-9]*)?([eE][+-]?[0-9]+)?", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

            public bool NumericalMatchAtPos(int pos)
            {
                return NumericalMatchAtPos(pos, false);
            }

            public bool NumericalMatchAtPos(int pos, bool startMatchingAfterDot)
            {
                var cur = pos + 1;

                if (!startMatchingAfterDot)
                {
                    while (cur < end)
                    {
                        var curchar = input[cur];

                        if ((curchar >= '0' && curchar <= '9'))
                            cur += 1;
                        else
                            break;
                    }

                    if (cur < end && input[cur] == '.')
                    {
                        cur += 1;
                        startMatchingAfterDot = true;   // We have a dot now!
                    }
                }

                if (startMatchingAfterDot)
                {
                    while (cur < end)
                    {
                        var curchar = input[cur];

                        if ((curchar >= '0' && curchar <= '9'))
                            cur += 1;
                        else
                            break;
                    }

                    if (cur - pos == 1)
                        return false;   // Exit quickly if we definitely don't have a number.
                }

                // This nasty code checks for an optional exponent.
                if (cur < end)
                {
                    var cur2 = cur;     // Switch variables so we can roll back if this doesn't work out.
                    var curchar = input[cur2];

                    if (curchar == 'e' || curchar == 'E')
                    {
                        cur2 += 1;

                        if (cur2 < end)
                        {
                            curchar = input[cur2];

                            if (curchar == '+' || curchar == '-')
                                cur2 += 1;

                            var cur3 = cur2;    // Switch variables again so we can ensure the exponent has at least one digit.

                            while (cur3 < end)
                            {
                                var curchar2 = input[cur3];

                                if ((curchar2 >= '0' && curchar2 <= '9'))
                                    cur3 += 1;
                                else
                                    break;
                            }

                            if (cur3 - cur2 > 0)    // If the exponent has at least one digit, update our position.
                                cur = cur3;
                        }

                    }
                }

                tokenLength = cur - pos;
                return true;
            }

            public bool NumericalMatch() { return NumericalMatch(false); }
            public bool NumericalMatch(bool startMatchingAfterDot)
            {
                return NumericalMatchAtPos(i, startMatchingAfterDot);
            }

            public bool ZeroPrefixNumericalMatch()
            {
                var cur = i + 1;

                if (cur < end)
                {
                    switch (input[cur])
                    {
                        case 'x':
                        case 'X':
                            // Do hex matching
                            cur += 1;

                            while (cur < end)
                            {
                                var curchar = input[cur];

                                if ((curchar >= '0' && curchar <= '9') || (curchar >= 'a' && curchar <= 'f') || (curchar >= 'A' && curchar <= 'F'))
                                    cur += 1;
                                else
                                    break;
                            }

                            if (cur - i < 3)
                                return false;   // Need at least 0x followed by one digit
                            else
                            {
                                tokenLength = cur - i;
                                return true;
                            }

                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            // Do octal matching
                            cur += 1;

                            while (cur < end)
                            {
                                var curchar = input[cur];

                                if ((curchar >= '0' && curchar <= '7'))
                                    cur += 1;
                                else
                                    break;
                            }

                            tokenLength = cur - i;
                            return true;

                        case '.':
                            // Do dec matching
                            if (NumericalMatchAtPos(cur, true))
                            {
                                tokenLength += 1;
                                return true;
                            }
                            else
                                return false;

                        case 'e':
                        case 'E':
                            // Just an exponent on 0; useless, but legal.
                            cur += 1;


                            if (cur < end)
                            {
                                var curchar = input[cur];

                                if (curchar == '+' || curchar == '-')
                                    cur += 1;

                                var cur2 = cur;    // Switch variables again so we can ensure the exponent has at least one digit.

                                while (cur2 < end)
                                {
                                    var curchar2 = input[cur2];

                                    if ((curchar2 >= '0' && curchar2 <= '9'))
                                        cur2 += 1;
                                    else
                                        break;
                                }

                                if (cur2 - cur > 0)    // If the exponent has at least one digit, then it's valid.
                                {
                                    tokenLength = cur2 - i;
                                    return true;
                                }
                            }

                            // If a complete exponent wasn't present, just treat the initial zero as a number by itself.
                            tokenLength = 1;
                            return true;

                        default:
                            tokenLength = 1;
                            return true;
                    }
                }
                else
                {
                    tokenLength = 1;
                    return true;
                }
            }

            #endregion

            #region Movement

            void Skip()
            {
                ++i;
            }

            void SkipFor(int len)
            {
                i += len;
            }

            void SkipUntil(string s, int len)
            {
                while (i < end)
                {
                    if (Match(s, len))
                    {
                        i += len + 1;
                        break;
                    }
                    else
                        ++i;
                }
            }

            void CheckForNewlinesAndSkipUntil(string s, int len)
            {
                while (i < end)
                {
                    if (input[i] == '\n')
                        newlineSinceLastToken = true;

                    if (Match(s, len))
                    {
                        i += len + 1;
                        break;
                    }
                    else
                        ++i;
                }
            }

            #endregion

            #region Token Stream Manipulation

            // Push a token with no argument (i.e. an operator, but not say an identifier) onto the token stack. The length len
            // just determines how far to advance.
            void Push(TokenType t, int len)
            {
                HandleRestrictedProductions(t);

                var block = tokenStack.Peek().Tokens;
                block.Add(unchecked((int)t));

#if DIAGNOSE || DEBUG
                block.Add(i);       // Record input offset so we can produce better error messages
#endif

                i += len;

                if (state == LazyLexState.FindIdentifier)
                    state = LazyLexState.None;

                lastToken = t;
            }

            // Push a token with an argument onto the token stack. The length len determines how far to advance, and the contents
            // of the input within len form the argument to the token.
            void PushData(TokenType t, int len)
            {
                HandleRestrictedProductions(t);

                var block = tokenStack.Peek().Tokens;
                block.Add(unchecked((int)t));
                block.Add(i);
                block.Add(len);

                if (t == TokenType.StringLiteral)
                {
                    lastIdentifier.Start = i + 1;
                    lastIdentifier.Length = len - 2;
                    lastIdentifier.IsSafe = false;
                }

                i += len;

                if (state == LazyLexState.FindIdentifier)
                    state = LazyLexState.None;

                lastToken = t;
            }

            // Push a function block onto the stack.
            void PushFunction(TokenStream ts)
            {
                HandleRestrictedProductions(TokenType.Block);

                // Finish up the block
                ts.Tokens.Add(unchecked((int) TokenType.End));

                // Map the block's name to its implementation (if we know what its name is!)
                var funcMetadata = ts.Implementation;

                if (ts.Name.IsSafe)
                {
                    List<JSFunctionMetadata> blocksForName = null;
                    var name = input.Substring(ts.Name.Start, ts.Name.Length);

                    funcMetadata.Name = name;

                    if (!LexInfo.CallableMap.TryGetValue(name, out blocksForName))
                    {
                        blocksForName = new List<JSFunctionMetadata>();
                        LexInfo.CallableMap[name] = blocksForName;
                    }

                    blocksForName.Add(funcMetadata);
                }

                // Add block to global block list
                FunctionImplementations.Add(funcMetadata);

                var stackTop = tokenStack.Peek();

                //TODO: Confirm this with Seth, is this the best place?
                stackTop.Implementation.AddSubFunction(funcMetadata);

                // Add Block token to current block
                var block = stackTop.Tokens;
                block.Add(unchecked((int)TokenType.Block));
                block.Add(FunctionImplementations.Count - 1);

                if (state == LazyLexState.FindIdentifier)
                    state = LazyLexState.None;

                lastToken = TokenType.Block;
            }

            // Push an identifier onto the stack (we handle it specially because we do various bookkeeping with identifiers.)
            void PushIdentifier(int len)
            {
                HandleRestrictedProductions(TokenType.Identifier);

                var block = tokenStack.Peek().Tokens;
                block.Add(unchecked((int)TokenType.Identifier));
                block.Add(i);
                block.Add(len);

                lastIdentifier.Start = i;
                lastIdentifier.Length = len;
                lastIdentifier.IsSafe = false;

                if (lastToken == TokenType.Function)
                {
                    callableNameByDefinition = lastIdentifier;
                    callableNameByDefinition.IsSafe = true;
                }

                i += len;

                if (state == LazyLexState.FindIdentifier)
                    state = LazyLexState.FindLBrace;

                lastToken = TokenType.Identifier;
            }

            // Automatically insert semicolons where required for correctness by the "restricted productions" rules.
            void HandleRestrictedProductions(TokenType t)
            {
                var block = tokenStack.Peek().Tokens;

                if (newlineSinceLastToken)
                {
                    if (block.Count > 0)
                    {
                        switch (t)
                        {
                            case TokenType.Increment:
                            case TokenType.Decrement:
                                if (lastToken != TokenType.Semicolon)
                                {
                                    block.Add(unchecked((int)TokenType.Semicolon));
#if DIAGNOSE || DEBUG
                                    block.Add(i);       // Record input offset so we can produce better error messages
#endif
                                }

                                break;

                            default:
                                switch (lastToken)
                                {
                                    case TokenType.Break:
                                    case TokenType.Continue:
                                    case TokenType.Return:
                                    case TokenType.Throw:
                                        block.Add(unchecked((int)TokenType.Semicolon));
#if DIAGNOSE || DEBUG
                                        block.Add(i);       // Record input offset so we can produce better error messages
#endif
                                        break;
                                }
                                break;
                        }
                    }

                    newlineSinceLastToken = false;
                }
            }

            // Are regexes legal in the current context? Lets us distinguish between / as a delimiter of regexes and / as a division operator.
            bool RegexLegalAfter(TokenType t)
            {
                switch (t)
                {
                    case TokenType.Identifier:
                    case TokenType.Null:
                    case TokenType.True:
                    case TokenType.False:
                    case TokenType.This:
                    case TokenType.NumericLiteral:
                    case TokenType.StringLiteral:
                    case TokenType.RegexLiteral:
                    case TokenType.RBracket:
                        return false;

                    case TokenType.RParen:
                        // Ah, the awful case of TokenType.RParen - a source of many woes.
                        // Regexes are illegal after an ')'. Except, that is, for ')'s that
                        // correspond to the '(' associated with 'while', 'for', 'with', and
                        // 'if' constructions. We check for these cases here (by taking
                        // advantage of bookkeeping happening in the main lexing code).
                        var tokenStream = tokenStack.Peek();

                        if (tokenStream.Tokens.Count == tokenStream.RegexLegalIndex)
                            return true;
                        else
                            return false;

                    default:
                        return true;
                }
            }

            bool RegexLegal()
            {
                int topCount = tokenStack.Peek().Tokens.Count;

                if (topCount == 0)
                    return true;
                else
                    return RegexLegalAfter(lastToken);
            }

            #endregion

            #region Lexer

            private void LexInitialHTMLComment()
            {
                while (i < end)
                {
                    switch (input[i])
                    {
                        // Handle whitespace
                        case ' ':
                        case '\t':
                        case '\v':
                        case '\r':
                        case '\n':
                            Skip();
                            break;

                        // Handle the HTML comment itself
                        case '<':
                            if (Match("!--", 3))
                                SkipUntil("\n", 1);

                            // We're done regardless; we found either the
                            // comment or actual JavaScript content
                            return;

                        default:
                            // Must be some actual JavaScript; we're done
                            return;
                    }
                }
            }

            public void Lex()
            {
                // Preprocess to remove any initial HTML comment
                LexInitialHTMLComment();

                // Now, do the actual lexing!
                while (i < end)
                {
                    switch (input[i])
                    {
                        // Handle whitespace
                        case ' ':
                        case '\t':
                        case '\v':
                        case '\r':
                            Skip();
                            break;

                        case '\n':
                            newlineSinceLastToken = true;
                            Skip();
                            break;

                        // Handle operators and punctuation
                        case '/':
                            if (Match("/", 1))
                            {
                                SkipUntil("\n", 1);
                                newlineSinceLastToken = true;
                            }
                            else if (Match("*", 1)) CheckForNewlinesAndSkipUntil("*/", 2);
                            else if (RegexLegal() && RegexMatch(RegexPat)) PushData(TokenType.RegexLiteral, tokenLength);
                            else if (Match("=", 1)) Push(TokenType.DivEq, 2);
                            else Push(TokenType.Div, 1);
                            break;

                        case '<':
                            if (Match("<=", 2)) Push(TokenType.LShiftEq, 3);
                            else if (Match("<", 1)) Push(TokenType.LShift, 2);
                            else if (Match("=", 1)) Push(TokenType.LEQ, 2);
                            else Push(TokenType.LT, 1);
                            break;

                        case '>':
                            if (Match(">>=", 3)) Push(TokenType.RShiftEqUnsigned, 4);
                            else if (Match(">>", 2)) Push(TokenType.RShiftUnsigned, 3);
                            else if (Match(">=", 2)) Push(TokenType.RShiftEq, 3);
                            else if (Match(">", 1)) Push(TokenType.RShift, 2);
                            else if (Match("=", 1)) Push(TokenType.GEQ, 2);
                            else Push(TokenType.GT, 1);
                            break;

                        case '=':
                            if (Match("==", 2)) Push(TokenType.EQEQEQ, 3);
                            else if (Match("=", 1)) Push(TokenType.EQEQ, 2);
                            else
                            {
                                Push(TokenType.EQ, 1);
                                lastIdentifier.IsSafe = true;
                            }
                            break;

                        case '!':
                            if (Match("==", 2)) Push(TokenType.NEQEQ, 3);
                            else if (Match("=", 1)) Push(TokenType.NEQ, 2);
                            else Push(TokenType.Not, 1);
                            break;

                        case '+':
                            if (Match("+", 1)) Push(TokenType.Increment, 2);
                            else if (Match("=", 1)) Push(TokenType.PlusEQ, 2);
                            else Push(TokenType.Plus, 1);
                            break;

                        case '-':
                            if (HTMLCommentMatch()) SkipFor(tokenLength);
                            else if (Match("-", 1)) Push(TokenType.Decrement, 2);
                            else if (Match("=", 1)) Push(TokenType.MinusEQ, 2);
                            else Push(TokenType.Minus, 1);
                            break;

                        case '|':
                            if (Match("|", 1)) Push(TokenType.Or, 2);
                            else if (Match("=", 1)) Push(TokenType.BitOrEq, 2);
                            else Push(TokenType.BitOr, 1);
                            break;

                        case '&':
                            if (Match("&", 1)) Push(TokenType.And, 2);
                            else if (Match("=", 1)) Push(TokenType.BitAndEq, 2);
                            else Push(TokenType.BitAnd, 1);
                            break;

                        case '*':
                            if (Match("=", 1)) Push(TokenType.TimesEQ, 2);
                            else Push(TokenType.Times, 1);
                            break;

                        case '%':
                            if (Match("=", 1)) Push(TokenType.ModEq, 2);
                            else Push(TokenType.Mod, 1);
                            break;

                        case '^':
                            if (Match("=", 1)) Push(TokenType.BitXorEq, 2);
                            else Push(TokenType.BitXor, 1);
                            break;

                        case '.':
                            if (NumericalMatch(true)) PushData(TokenType.NumericLiteral, tokenLength);
                            else Push(TokenType.Dot, 1);
                            break;

                        case '{':
                            Push(TokenType.LBrace, 1);

                            braceCount++;

                            if (state == LazyLexState.FindLBrace)
                            {
                                tokenStack.Push(callableNameByAssignment.IsSafe
                                                    ? new TokenStream(braceCount, callableNameByAssignment, LexInfo, tokenStack.Peek().Implementation)
                                                    : new TokenStream(braceCount, callableNameByDefinition, LexInfo, tokenStack.Peek().Implementation));

                                callableNameByDefinition.IsSafe = false;    // Note that we just passed this to the new TokenStream; we rely on value semantics here!
                                state = LazyLexState.None;
                            }

                            break;

                        case '}':
                            if (tokenStack.Peek().BraceCount == braceCount)
                            {
                                try
                                {
                                    PushFunction(tokenStack.Pop());
                                }
                                catch (InvalidOperationException)
                                {
                                    throw new LexException("Unbalanced braces", input, i);
                                }
                            }

                            braceCount--;

                            Push(TokenType.RBrace, 1);

                            break;

                        case '(':
                            switch (lastToken)
                            {
                                case TokenType.If:
                                case TokenType.For:
                                case TokenType.While:
                                case TokenType.With:
                                    parenCountAtKeyword = parenCount; break;

                                default: break;
                            }

                            parenCount++;

                            Push(TokenType.LParen, 1);

                            break;

                        case ')':
                            parenCount--;

                            Push(TokenType.RParen, 1);

                            if (parenCount == parenCountAtKeyword)
                            {
                                var tokenStream = tokenStack.Peek();
                                tokenStream.RegexLegalIndex = tokenStream.Tokens.Count;
                                parenCountAtKeyword = Invalid;
                            }

                            break;

                        case '[': Push(TokenType.LBracket, 1); break;
                        case ']': Push(TokenType.RBracket, 1); break;
                        case ';': Push(TokenType.Semicolon, 1); break;
                        case ',': Push(TokenType.Comma, 1); break;
                        case '~': Push(TokenType.Tilde, 1); break;
                        case '?': Push(TokenType.Question, 1); break;

                        case ':':
                            Push(TokenType.Colon, 1);
                            lastIdentifier.IsSafe = true;
                            break;

                        // Handle numeric literals
                        case '0':
                            if (ZeroPrefixNumericalMatch()) PushData(TokenType.NumericLiteral, tokenLength);
                            else throw new LexException("Bad numeric literal", input, i);
                            break;

                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            if (NumericalMatch()) PushData(TokenType.NumericLiteral, tokenLength);
                            else throw new LexException("Bad numeric literal", input, i);
                            break;

                        // Handle string literals
                        case '"':
                            if (RegexMatch(DQStrPat)) PushData(TokenType.StringLiteral, tokenLength);
                            else throw new LexException("Unterminated string literal", input, i);
                            break;

                        case '\'':
                            if (RegexMatch(SQStrPat)) PushData(TokenType.StringLiteral, tokenLength);
                            else throw new LexException("Unterminated string literal", input, i);
                            break;

                        // Handle keywords, identifiers, reserved words (TODO), etc.
                        case 'b':
                            if (KeywordMatch("reak", 4)) Push(TokenType.Break, 5);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'c':
                            if (KeywordMatch("ase", 3)) Push(TokenType.Case, 4);
                            else if (KeywordMatch("atch", 4)) Push(TokenType.Catch, 5);
                            else if (KeywordMatch("ontinue", 7)) Push(TokenType.Continue, 8);
                            else if (KeywordMatch("onst", 4)) Push(TokenType.Const, 5);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'd':
                            if (KeywordMatch("ebugger", 7)) Push(TokenType.Debugger, 8);
                            else if (KeywordMatch("efault", 6)) Push(TokenType.Default, 7);
                            else if (KeywordMatch("elete", 5)) Push(TokenType.Delete, 6);
                            else if (KeywordMatch("o", 1)) Push(TokenType.Do, 2);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'e':
                            if (KeywordMatch("lse", 3)) Push(TokenType.Else, 4);
                            else if (KeywordMatch("val", 3))
                            {
                                PushIdentifier(4);

                                tokenStack.Peek().HasEval = true;
                            }
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'f':
                            if (KeywordMatch("alse", 4)) Push(TokenType.False, 5);
                            else if (KeywordMatch("inally", 6)) Push(TokenType.Finally, 7);
                            else if (KeywordMatch("or", 2)) Push(TokenType.For, 3);
                            else if (KeywordMatch("unction", 7))
                            {
                                Push(TokenType.Function, 8);
                                state = LazyLexState.FindLBrace;

                                if (lastIdentifier.IsSafe)
                                    callableNameByAssignment = lastIdentifier;
                                else
                                    callableNameByAssignment.IsSafe = false;
                            }
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'g':
                            if (KeywordMatch("et", 2))
                            {
                                PushIdentifier(3);

                                if (state == LazyLexState.None)
                                    state = LazyLexState.FindIdentifier;
                            }
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'i':
                            if (KeywordMatch("nstanceof", 9)) Push(TokenType.Instanceof, 10);
                            else if (KeywordMatch("n", 1)) Push(TokenType.In, 2);
                            else if (KeywordMatch("f", 1)) Push(TokenType.If, 2);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'n':
                            if (KeywordMatch("ew", 2)) Push(TokenType.New, 3);
                            else if (KeywordMatch("ull", 3)) Push(TokenType.Null, 4);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'r':
                            if (KeywordMatch("eturn", 5)) Push(TokenType.Return, 6);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 's':
                            if (KeywordMatch("witch", 5)) Push(TokenType.Switch, 6);
                            else if (KeywordMatch("et", 2))
                            {
                                PushIdentifier(3);

                                if (state == LazyLexState.None)
                                    state = LazyLexState.FindIdentifier;
                            }
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 't':
                            if (KeywordMatch("his", 3)) Push(TokenType.This, 4);
                            else if (KeywordMatch("hrow", 4)) Push(TokenType.Throw, 5);
                            else if (KeywordMatch("rue", 3)) Push(TokenType.True, 4);
                            else if (KeywordMatch("ry", 2)) Push(TokenType.Try, 3);
                            else if (KeywordMatch("ypeof", 5)) Push(TokenType.Typeof, 6);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'v':
                            if (KeywordMatch("ar", 2)) Push(TokenType.Var, 3);
                            else if (KeywordMatch("oid", 3)) Push(TokenType.Void, 4);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        case 'w':
                            if (KeywordMatch("hile", 4)) Push(TokenType.While, 5);
                            else if (KeywordMatch("ith", 3)) Push(TokenType.With, 4);
                            else if (IdentifierMatch()) PushIdentifier(tokenLength);
                            else throw new LexException("Bad identifier", input, i);
                            break;

                        default:
                            if (IdentifierMatchAt(0)) PushIdentifier(tokenLength);
                            else if (Char.IsWhiteSpace(input[i])) Skip();
                            else
                            {
                                //tokens.ForEach(new Action<IToken>(s => { Console.ForegroundColor = ConsoleColor.Blue; Console.WriteLine(s.ToString(input)); Console.ResetColor(); }));
                                throw new LexException("Lex error", input, i);
                            }
                            break;
                    }
                }

                if (tokenStack.Count == 1)
                {
                    var block = tokenStack.Peek();
                    block.Tokens.Add(unchecked((int)TokenType.End));
                    block.Implementation.Name = "£";// "Σ";
                    FunctionImplementations.Add(block.Implementation);
                    return;
                }
                else
                    throw new LexException("Unbalanced braces", input, i);
            }

            #endregion
        }

        #region Public API

        public static JSFunctionMetadata lex(string input, int start, int length)
        {
            var lexer = new LexerImpl(input, start, length);

            lexer.Lex();

            return lexer.FunctionImplementations[lexer.FunctionImplementations.Count - 1];
        }

        public static JSFunctionMetadata lex(string input)
        {
            return lex(input, 0, input.Length);
        }

        #endregion
    }
}
