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

using m.Util.Diagnose;

namespace mjr
{
  /// <summary>
  /// This class serves as the public interface for the JavaScript parser. Code outside of the
  /// mjr.JavaScriptParser namespace should only deal with this class.
  /// </summary>
  public static class JSParser
  {
    /// <summary>
    /// Parse a script, converting it to a Program.
    /// </summary>
    public static IR.Program ParseScript(string script)
    {
      #if DEBUG
        CheckEncoding(script);
      #endif

      var parser = new JavaScriptParser.SequentialParser(script);
      return (IR.Program) parser.ParseScript();
    }

    /// <summary>
    /// Parse a number. Intended to be used at runtime for string-to-number conversions.
    /// </summary>
    public static void ParseNumber(string number, ref mdr.DValue dValue)
    {
      var parser = new JavaScriptParser.SequentialParser(number);
      var numericLiteral = parser.ParseNumber();

      // Convert to a DValue and return via the ref parameter.
      Debug.Assert(numericLiteral is IR.PrimitiveLiteral, "ParseNumber() should yield a PrimitiveLiteral.");
      (numericLiteral as IR.PrimitiveLiteral).SetAsDValue(ref dValue);
    }

    /// <summary>
    /// Parse a regular expression. Intended to be used at runtime for the RegExp constructor.
    /// </summary>
    /// <exception cref="SyntaxException">Thrown when the regular expression is malformed.</exception>
    public static string ParseRegularExpression(string regex)
    {
      var parser = new JavaScriptParser.SequentialParser(regex);

      return parser.ParseRegularExpression();
    }

    private static void CheckEncoding(string script)
    {
      if (script.Length < 2)                            Debug.WriteLine("Encoding check: Script is too short to draw any conclusions.");
      else if (script[0] == '\0')                       Debug.WriteLine("Encoding check: Script looks like UTF-16BE treated as UTF-8.");
      else if (script[1] == '\0')                       Debug.WriteLine("Encoding check: Script looks like UTF-16LE treated as UTF-8.");
      else if ((Convert.ToUInt16(script[0]) >> 8) != 0) Debug.WriteLine("Encoding check: Script looks like UTF-16 with wrong byte order.");
      else                                              Debug.WriteLine("Encoding check: Everything looks OK.");
    }
  }
}
