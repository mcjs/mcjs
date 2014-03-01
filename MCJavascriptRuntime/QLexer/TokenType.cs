// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿namespace mjr.QLexer
{
    // <summary>
    // An enumeration of every type of token used by the JavaScript lexer and parser.
    // </summary>
    public enum TokenType : int
    {
        Block = 258,        // Special block token; starting value is chosen based upon Bison file
        LazyBlock,          // Special "lazy" block token; currently unused

        Identifier,         // Any identifier that is not a keyword, reserved word, etc.
        NumericLiteral,
        StringLiteral,
        RegexLiteral,
        True,
        False,
        Null,

        // Unused; for easier mapping between Bison and JQuick tokens
        Dummy,

        // Start Keywords
        Break,
        Case,
        Catch,
        Const,
        Continue,
        Debugger,
        Default,
        Delete,
        Do,
        Else,
        Finally,
        For,
        Function,
        If,
        In,
        Instanceof,
        New,
        Return,
        Switch,
        This,
        Throw,
        Try,
        Typeof, 
        Var, 
        Void, 
        While,
        With,
        // End Keywords

        // Start Reserved Words
        Reserved,
        // End Reserved Words

        // Start Operators
        LBrace,
        RBrace,
        LParen,
        RParen,
        LBracket,
        RBracket,
        Dot,
        Semicolon,
        Comma,
        LT,
        GT,
        LEQ,
        GEQ,
        EQEQ,
        NEQ,
        EQEQEQ,
        NEQEQ,
        Plus,
        Minus,
        Times,
        Div,
        Mod,
        Increment,
        Decrement,
        LShift,
        RShift,
        RShiftUnsigned,
        BitAnd,
        BitOr,
        BitXor,
        Tilde,
        Not,
        And,
        Or,
        Question,
        Colon,
        EQ,
        PlusEQ,
        MinusEQ,
        TimesEQ,
        DivEq,
        ModEq,
        LShiftEq,
        RShiftEq,
        RShiftEqUnsigned,
        BitAndEq,
        BitOrEq,
        BitXorEq,
        // End Operators

        End     // EOF
    }
}