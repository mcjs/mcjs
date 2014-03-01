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
using System.Text;

using mdr;
using mjr.IR;

namespace mjr
{
    public class JSSourceLocation : m.Util.ISourceLocation
    {
        private const int beforeContext  = 50;
        private const int afterContext   = 50;

        private JSFunctionMetadata functionMetadata;
        private int line;
        private int character;
        private string prettified;  // Serves dual roles: holds prettified string, and null-ness indicates if lazy properties have been evaluated

        public JSSourceLocation(JSFunctionMetadata metadata, Node node) : this(metadata, node.SourceOffset) { }

        public JSSourceLocation(JSFunctionMetadata metadata, int offset)
        {
            functionMetadata = metadata;
            Offset = offset;
            prettified = null;
        }

        public DFunctionMetadata FunctionMetadata { get { return functionMetadata; } }

        public string Function { get { return FunctionMetadata.Declaration; } }

        public int Offset { get; private set; }

        public int Line
        {
            get
            {
                if (prettified == null)
                    ComputeLocationInfo();
                return line;
            }
        }

        public int Character
        {
            get
            {
                if (prettified == null)
                    ComputeLocationInfo();
                return character;
            }
        }

        public string Prettified
        {
            get
            {
                if (prettified == null)
                    ComputeLocationInfo();
                return prettified;
            }
        }

        private void ComputeLocationInfo()
        {
            string input = JSRuntime.Instance.Scripts.GetScriptString(functionMetadata);
            // Exit early if offset is invalid
            if (input == null || Offset == Runtime.InvalidOffset || Offset < 0 || Offset >= input.Length)
            {
                line = character = -1;
                prettified = "<UNKNOWN SOURCE LOCATION>";
                return;
            }

            int i;

            // Walk through the input until we reach the offset
            line = character = 0;
            for (i = 0 ; i < Offset ; ++i)
            {
                if (input[i] == '\n')
                {
                    ++line;
                    character = 0;
                }
                else
                    ++character;
            }

            // Record start and end of line
            int lineStart =  i - character;

            while (i < input.Length && input[i] != '\n')
                ++i;

            int lineEnd = i;

            // Make line and character 1-based instead of 0-based
            ++line;
            ++character;

            // Prettify
            if (lineEnd - lineStart > beforeContext + afterContext + 1)
            {
              // The line is too large. Clamp it to beforeContext and afterContext.
              lineStart = Math.Max(Offset - beforeContext, lineStart);
              lineEnd = Math.Min(Offset + afterContext, lineEnd);
            }

            var lineIndicator = String.Format("{0} ({1},{2}): ", functionMetadata.Declaration, line, character);
            var sb = new StringBuilder();
            sb.Append(lineIndicator);
            sb.Append(input.Substring(lineStart, lineEnd - lineStart));

            if (sb[sb.Length - 1] != '\n')
                sb.AppendLine();

            sb.Append(new String(' ', (Offset - lineStart) + lineIndicator.Length));
            sb.Append('^');

            prettified = sb.ToString();
        }
    }
}
