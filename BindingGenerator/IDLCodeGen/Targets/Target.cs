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
using System.Linq;
using System.Text;
using System.IO;

using IDLCodeGen.IDL;
using IDLCodeGen.Util;

namespace IDLCodeGen.Targets
{
  abstract class Target
  {
    protected List<string> metadata;

    public Target()
    {
      metadata = new List<string>();
      AddMetadata("This is an auto generate file, please don't change manually!");
    }

    public void AddMetadata(string text)
    {
      metadata.Add("/* {0} */{1}".Formatted(text, Environment.NewLine));
    }

    public void Generate(TextWriter output)
    {
      var Write = MakeWriter(output);

      foreach (var md in metadata)
        Write(md);

      Generate(Write);
    }

    public abstract string Filename { get; }
    protected abstract void Generate(Writer Write);


    protected delegate void Writer(string output);
    private Writer MakeWriter(TextWriter output)
    {
      return (string text) =>
      {
        // Normalize newlines to those expected by our environment. This is necessary when using @-strings.
        var normalized = text.Replace("\r\n", Environment.NewLine);
        if (Environment.OSVersion.Platform == PlatformID.Unix)
          normalized = normalized.Replace("\n", Environment.NewLine);

        // Strip the inline newline from the text. This makes it much easier to get the alignment of
        // the generated code right.
        if (normalized.StartsWith(Environment.NewLine))
          normalized = normalized.Remove(0, Environment.NewLine.Length);

        output.Write(normalized);
      };
    }
  }
}
