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
using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  class FunctionDeclarationHoister
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<Implementation>("JS/Hoist/", () => new Implementation(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public static void Execute(JSFunctionMetadata funcMetada) { _pool.Execute(funcMetada); }

    class Implementation : AlgorithmImplementation
    {
      public void Execute(CodeGenerationInfo cgInfo) { throw new NotImplementedException(); }
      public void Execute(JSFunctionMetadata functionMetadata)
      {
        var subFunctions = functionMetadata.SubFunctions;
        var declarations = new List<Statement>();
        for (var i = 0; i < subFunctions.Count; ++i)
        {
          var func = subFunctions[i];
          if (func.Scope.IsFunctionDeclaration)
          {
            HoistDeclaration(functionMetadata, func, declarations);
          }
        }

        if (declarations.Count > 0)
        {
          var funcIR = functionMetadata.FunctionIR;
          var currStatements = funcIR.Statement;
          currStatements.RemoveUser(funcIR);
          var declarationBlock = new BlockStatement(declarations);
          var newStatements = new BlockStatement(new List<Statement>()
        {
          declarationBlock,
          currStatements,
        });
          funcIR.Replace(currStatements, newStatements);
        }
      }

      void HoistDeclaration(JSFunctionMetadata func, JSFunctionMetadata declaredFunc, List<Statement> declarations)
      {
        var writeId = declaredFunc.FunctionIR.User as WriteIdentifierExpression;
        Debug.Assert(writeId != null, "Invalid situation, user of the FunctionIR must be a WriteIdentifier");
        var funcDeclStatement = writeId.User as FunctionDeclarationStatement;
        Debug.Assert(funcDeclStatement != null, "Invalid situation, user of the WriteIdentifier must be a FunctionDeclarationStatement");
        var declarationUser = funcDeclStatement.User;
        Debug.Assert(declarationUser != null, "Invalid situation, user of FunctionDeclarationStatement must not be null");

        var emptyStatement = new EmptyStatement(); //TODO: we can either remove the statement or use some other marker
        declarationUser.Replace(funcDeclStatement, emptyStatement);
        funcDeclStatement.RemoveUser(declarationUser);

        declarations.Add(funcDeclStatement);
      }
    }
  }
}
