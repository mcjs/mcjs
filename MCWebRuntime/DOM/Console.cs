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
using System.Linq;
using System.Text;

using PropAttr = mdr.PropertyDescriptor.Attributes;

namespace mwr.DOM
{
    public partial class Console
    {
        public static void log(ref mdr.CallFrame callFrame)
        {
          ((Console)callFrame.This).Log(FormatArgs(ref callFrame));
        }

        public static void debug(ref mdr.CallFrame callFrame)
        {
          ((Console)callFrame.This).Debug(FormatArgs(ref callFrame));
        }

        public static void info(ref mdr.CallFrame callFrame)
        {
          ((Console)callFrame.This).Info(FormatArgs(ref callFrame));
        }

        public static void warn(ref mdr.CallFrame callFrame)
        {
          ((Console)callFrame.This).Warn(FormatArgs(ref callFrame));
        }

        public static void error(ref mdr.CallFrame callFrame)
        {
          ((Console)callFrame.This).Error(FormatArgs(ref callFrame));
        }

        static string FormatArgs(ref mdr.CallFrame callFrame)
        {
          var formatString = callFrame.Arg0.AsString();

          StringBuilder str = new StringBuilder();
          int escapeIdx = 0, nextEscapeIdx = 0;
          var nextArg = 1;
          while ((nextEscapeIdx = formatString.IndexOfAny(FormatChars, escapeIdx)) != -1)
          {
            // Append non-escape characters
            str.Append(formatString.Substring(escapeIdx, nextEscapeIdx - escapeIdx));

            // If we're at the last character in the string, just break out of the loop
            if (nextEscapeIdx == formatString.Length - 1)
            {
              escapeIdx = nextEscapeIdx;
              break;
            }

            // Interpret escape character
            if (formatString[nextEscapeIdx] == '\\')
            {
              if (formatString[nextEscapeIdx + 1] == '%')
                str.Append('%');
              else
                str.AppendFormat("\\{0}", formatString[nextEscapeIdx + 1]);
            }
            else if (formatString[nextEscapeIdx] == '%')
            {
              var arg = callFrame.Arg(nextArg++);
              if (formatString[nextEscapeIdx + 1] == 's')
                str.Append(mjr.Operations.Convert.ToString.Run(ref arg));
              else if (formatString[nextEscapeIdx + 1] == 'd')
                str.Append(mjr.Operations.Convert.ToInt64.Run(ref arg));
              else if (formatString[nextEscapeIdx + 1] == 'i')
                str.Append(mjr.Operations.Convert.ToInt64.Run(ref arg));
              else if (formatString[nextEscapeIdx + 1] == 'f')
                str.Append(mjr.Operations.Convert.ToDouble.Run(ref arg));
              else
                str.AppendFormat("[%{0}]", formatString[nextEscapeIdx + 1]);
            }

            // Advance past the escaped character
            escapeIdx = nextEscapeIdx + 2;
          }

          // Append any remaining characters
          str.Append(formatString.Substring(escapeIdx));

          // Return the finished formatted string
          return str.ToString();
        }

        static char[] FormatChars = new char[] {'\\', '%'};
    }
}
