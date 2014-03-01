// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System;
using System.Collections.Generic;
using MCJavascript.Expressions;

namespace MCJavascript
{
    /// <summary>
    /// Information added to each Jint.Expression.Statement node for holding JIT information
    /// </summary>
    public class JitInfo
    {
        /// <summary>
        /// The expression that uses the result of this
        /// </summary>
        public Statement User { get; set; }

        /// <summary>
        /// Symbol that receives the result of this
        /// </summary>
        public JSFunctionImp.Symbol WritesTo { get; set; }

        /// <summary>
        /// Code generation mode for a particular AST node.
        /// </summary>
        public enum CGModes { Read, Write, ReadWrite };
        public CGModes CGMode { get; set; }

        public int AlgPassNumber { get; set; }
        public Type Type { get; set; }
        //public HashSet<Type> Types {get; private set;}

        //public LinkedList<Jint.Expressions.Expression> TypeDependsOn;// = new LinkedList<Jint.Expressions.Expression>();
        //public bool IsFixedType { get { return TypeDependsOn == null; } }

        /// <summary>
        /// Only IFunctionDeclaration will set this. //TODO: better to move the IFunctionDeclaration class after merging with parser.
        /// </summary>
        //public int FuncImpIndex { get; set; }

        public JitInfo()
        {
            //Types = new HashSet<Type>();
        }
    }
    public static class JintExtensions
    {
        public static JitInfo GetJitInfo(this Statement statement)
        {
            var jitInfo = statement.Extension as JitInfo;
            if (jitInfo == null)
            {
                jitInfo = new JitInfo();
                statement.Extension = jitInfo;
            }
            return jitInfo;
        }
    }
}
