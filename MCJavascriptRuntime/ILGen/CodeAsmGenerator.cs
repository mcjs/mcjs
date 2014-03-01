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

using m.Util.Diagnose;

namespace mjr.ILGen
{
    class CodeAsmGenerator : DynamicAsmGenerator// DllAsmGenerator
    {
        protected string InputFilename { get; private set; }
        protected string OutputDir { get; private set; }
        protected string Filename { get; private set; }
        protected CodeAsmGenerator(string inputFilename, string outputDir)
            : base()
        {
            InputFilename = inputFilename;
            OutputDir = outputDir;
            Filename = System.IO.Path.GetFullPath(System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(inputFilename)));
        }

        public CodeAsmGenerator()
            : base()
        {
            _output = Console.Out;
        }


        #region Output
        System.IO.TextWriter _output;
        protected void OpenOutput(string outputFilename) { _output = new System.IO.StreamWriter(outputFilename); }
        public void WriteOutput(string value)
        {
            if(JSRuntime.Instance.Configuration.EnableDiagIL)
                Debug.WriteLine("{0}",value);

            lock (this)
                _output.WriteLine("{0}",value);
        }
        public void WriteOutput(string format, params object[] arg)
        {
            WriteOutput(string.Format(format, arg));
        }
        #endregion

        public override void EndAssembly()
        {
            base.EndAssembly();
            if (_output != Console.Out)
                _output.Close();
        }

        public override BaseILGenerator GetILGenerator()
        {
            return new CodeILGenerator(this, Console.Out);
        }
    }
}
