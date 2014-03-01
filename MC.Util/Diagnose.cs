// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System;

namespace m.Util.Diagnose
{
  public static class Diagnostics
  {
    public static TextWriter Output        { get; set; }
    public static bool EnableExceptionDump { get; set; }
    public static bool EnableStackDump     { get; set; }
    public static bool FailOnException     { get; set; }
    public static bool RedirectAllExceptions { get; set; }

    static Diagnostics()
    {
      Output = Console.Out;
      EnableExceptionDump = true;
      EnableStackDump = true;
      FailOnException = true;
      RedirectAllExceptions = false;
    }

    /// <summary>
    /// The following API is for internal use within this namespace. External code should use Debug.* or Trace.*.
    /// </summary>
    #region Internal Implementations

    /// <summary>
    /// For debugging, one can threat Asserts as warning and continue
    /// </summary>
    static bool ContinueOnFail = false;


    #region Write ///////////////////////////////////////////////////////////////////////////////////////////
    internal static void Write(ISourceLocation location, string format, params object[] args)
    {
      if (location != null) Output.WriteLine("{0}", location.Prettified);
      Output.Write(string.Format(format, args));
      Output.Flush();
    }

    internal static void Write(string format, params object[] args) { Write(null, format, args); }

    internal static void WriteLine(ISourceLocation location, string format, params object[] args) { Write(location, format + "\n", args); }

    internal static void WriteLine(string format, params object[] args) { WriteLine(null, format, args); }

    #endregion

    public static string GetStackTrace(int skipFrames, bool needFileInfo = false)
    {
      var st = new StackTrace(skipFrames, needFileInfo);
      var sf = st.GetFrame(0);

      var sb = new System.Text.StringBuilder();
      sb.AppendFormat(
        "{0}({1})"
        , sf.GetMethod().Name
        , string.Join(", ", sf.GetMethod().GetParameters().Select(pi => string.Format("{0} {1}", pi.ParameterType.Name, pi.Name)).ToArray())
      );
      
      if (needFileInfo)
        sb.AppendFormat(
          " in {0}:line {1}"
          , sf.GetFileName()
          , sf.GetFileLineNumber()
        );
      
      return sb.ToString();
    }

    internal static void WriteStackTrace()
    {
      var st = new StackTrace(1, true);
      Output.WriteLine("  at {0}", GetStackTrace(1, true));
    }

    #region Fail ///////////////////////////////////////////////////////////////////////////////////////////
    internal static void Fail<T>(ISourceLocation location, T exception) where T : System.Exception
    {
      if (!ContinueOnFail)
      {
        if (location != null)
        {
          Output.Write("{0}", location.Prettified);
          throw new System.Exception(string.Format("FAIL ERROR (FUNCTION {0} LINE {1} CHAR {2})", location.Function, location.Line, location.Character),
                                     exception);
        }
        else
          throw exception;
      }
      else
        WriteLine(location, "FAIL ERROR: " + exception.Message);
    }
    internal static void Fail<T>(T exception) where T : System.Exception { Fail(null, exception); }
    internal static void Fail(ISourceLocation location, string message) { Fail(location, new System.Exception(message)); }
    internal static void Fail(string message) { Fail(null, message); }
    internal static void Fail(ISourceLocation location, string format, params object[] args) { Fail(location, string.Format(format, args)); }
    internal static void Fail(string format, params object[] args) { Fail(null, format, args); }
    #endregion


    #region Assert ///////////////////////////////////////////////////////////////////////////////////////////
    internal static void Assert(bool condition, ISourceLocation location = null, string message = null)
    {
      if (!condition)
        Fail(location, message);
    }
    #endregion

    #endregion

    public static void WriteException(Exception e, string message = null)
    {
      if (Diagnostics.EnableExceptionDump)
      {
        Trace.WriteLine("\n\n=================== Exception Occurred {0} =======================================", message);

        //Console.ForegroundColor = ConsoleColor.Red;
        Trace.WriteLine("");
        Trace.WriteLine("");
        Trace.WriteLine("==========================================================");
        var str = "=";
        for (var exception = e; exception != null; exception = exception.InnerException)
        {
          Trace.WriteLine("{0}{1}", str, exception.Message);
          str += "=";
        }
        if (Diagnostics.EnableStackDump)
        {
          Trace.WriteLine("==========================================================");
          Trace.WriteLine("\n" + e.StackTrace);
          Trace.WriteLine("");
        }
        //Console.ResetColor();
      }
      ///For some exceptions, we may want to ignore them 
      ///but for major ones, we want to fail no matter what. To be sure we are doing it write
      ///we use the same standard mechanism of capturing harmful exceptions
      ///the following does the trick
      if (!RedirectAllExceptions &&
          (  e is NullReferenceException
          || e is InvalidProgramException
          || e is OutOfMemoryException
          || e is MissingMethodException
          || e is MissingFieldException
         ))
      {
        throw PreserveStackTrace(e);
      }
      else
        Trace.Assert(!Diagnostics.FailOnException, ""); //This will alway fail in the debug mode for all other exceptions
    }

    /// <summary>
    /// Use private APIs to freeze the stack trace of an exception before it is rethrown. We use this to avoid the
    /// appearance that exceptions are coming from WriteException, rather than the original location. May stop working
    /// with future releases of .NET / Mono, but should fail gracefully.
    ///
    /// This technique originated from and is explained at:
    /// http://stackoverflow.com/questions/57383/in-c-how-can-i-rethrow-innerexception-without-losing-stack-trace
    /// </summary>
    private static Exception PreserveStackTrace(Exception e)
    {
      // Get the remoteStackTraceString of the Exception class.
      FieldInfo remoteStackTraceString = typeof(Exception)
      .GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic); // MS.Net

      if (remoteStackTraceString == null)
        remoteStackTraceString = typeof(Exception)
        .GetField("remote_stack_trace", BindingFlags.Instance | BindingFlags.NonPublic); // Mono pre-2.6

      // Copy the current stack trace into the remoteStackTraceString.
      if (remoteStackTraceString != null)
        remoteStackTraceString.SetValue(e, e.StackTrace + Environment.NewLine);

      return e;
    }
  }
 
  /// <summary>
  /// TRACE is by default enabled in all configuration, if we really wanted to get rid of all checks (DEBUG & RELEASE) we can undefine TRACE
  /// These API will show up in the release mode and can impact performance
  /// </summary>
  public static class Trace
  {
    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Write
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Write(ISourceLocation location, string format, params object[] args)
    {
      Diagnostics.Write("-----TRACE----: ");
      Diagnostics.Write(location, format, args);
    }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Write(string format, params object[] args) { Write(null, format, args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void WriteLine(ISourceLocation location, string format, params object[] args) { Write(location, format + "\n", args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void WriteLine(string format, params object[] args) { WriteLine(null, format, args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void WriteLineIf(bool condition, ISourceLocation location, string format, params object[] args)
    {
      if (condition)
      {
        WriteLine(location, format, args);
        Diagnostics.WriteStackTrace();
      }
    }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void WriteLineIf(bool condition, string format, params object[] args) { WriteLineIf(condition, null, format, args); }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Warning
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Warning(ISourceLocation location, string format, params object[] args) { WriteLine(location, "WARNING: " + format, args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Warning(string format, params object[] args) { Warning(null, format, args); }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Fail
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Fail<T>(ISourceLocation location, T exception) where T : System.Exception { Diagnostics.Fail<T>(location, exception); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Fail<T>(T exception) where T : System.Exception { Fail(null, exception); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Fail(ISourceLocation location, string message) { Fail(location, new System.Exception(message)); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Fail(string message) { Fail(null, message); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Fail(ISourceLocation location, string format, params object[] args) { Fail(location, string.Format(format, args)); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Fail(string format, params object[] args) { Fail(null, format, args); }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Assert
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Assert(bool condition, ISourceLocation location = null, string message = null) { Diagnostics.Assert(condition, location, message); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Assert(bool condition, ISourceLocation location, string format, params object[] args) { Assert(condition, location, string.Format(format, args)); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("TRACE")]
    public static void Assert(bool condition, string format, params object[] args) { Assert(condition, null, format, args); }
    #endregion
  }

  /// <summary>
  /// DEBUG is enabled only in DEBUG configuration.
  /// We can use these API for debug only without overhead in the release mode
  /// </summary>
  public static class Debug
  {
    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Write
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Write(ISourceLocation location, string format, params object[] args)
    {
      Diagnostics.Write("----DEBUG---: ");
      Diagnostics.Write(location, format, args);
    }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Write(string format, params object[] args) { Write(null, format, args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void WriteLine(ISourceLocation location, string format, params object[] args) { Write(location, format + "\n", args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void WriteLine(string format, params object[] args) { WriteLine(null, format, args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void WriteLineIf(bool condition, ISourceLocation location, string format, params object[] args)
    {
      if (condition)
      {
        WriteLine(location, format, args);
        Diagnostics.WriteStackTrace();
      }
    }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void WriteLineIf(bool condition, string format, params object[] args) { WriteLineIf(condition, null, format, args); }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Warning
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Warning(ISourceLocation location, string format, params object[] args) { WriteLine(location, "WARNING: " + format, args); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Warning(string format, params object[] args) { Warning(null, format, args); }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Fail
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Fail<T>(ISourceLocation location, T exception) where T : System.Exception { Diagnostics.Fail<T>(location, exception); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Fail<T>(T exception) where T : System.Exception { Fail(null, exception); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Fail(ISourceLocation location, string message) { Fail(location, new System.Exception(message)); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Fail(string message) { Fail(null, message); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Fail(ISourceLocation location, string format, params object[] args) { Fail(location, string.Format(format, args)); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Fail(string format, params object[] args) { Fail(null, format, args); }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////
    #region Assert
    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Assert(bool condition, ISourceLocation location = null, string message = null) { Diagnostics.Assert(condition, location, message); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Assert(bool condition, ISourceLocation location, string format, params object[] args) { Assert(condition, location, string.Format(format, args)); }

    [ConditionalAttribute("DIAGNOSE"), ConditionalAttribute("DEBUG")]
    public static void Assert(bool condition, string format, params object[] args) { Assert(condition, null, format, args); }

    #endregion
  }
}
