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

namespace mjr.IR.Syntax
{
  #region AST node types.

  // Syntax nodes:
  public interface ISyntaxNode { }
  public interface IProgram : ISyntaxNode { }
  public interface IVariableDeclaration : ISyntaxNode { }
  public interface ICaseClause : ISyntaxNode { }
  public interface ICatch : ISyntaxNode { }
  public interface IFinally : ISyntaxNode { }
  public interface IPropertyAssignment : ISyntaxNode { }

  // Statements:
  public interface IStatement : ISyntaxNode { }

  // Expressions:
  public interface IExpression : ISyntaxNode { }
  public interface ILeftHandSideExpression : IExpression { }
  public interface IIdentifier : ILeftHandSideExpression { }
  public interface ILiteral : ILeftHandSideExpression { }

  #endregion

  #region Collection classes and extension methods.

  public class IStatementList           : List<Statement>                { }
  public class IExpressionList          : List<Expression>               { }
  public class IIdentifierList          : List<ReadIdentifierExpression> { }
  public class IVariableDeclarationList : List<VariableDeclaration>      { }
  public class IPropertyAssignmentList  : List<PropertyAssignment>       { }
  public class ICaseClauseList          : List<CaseClause>               { }

  public static class SyntaxExtensions
  {
    /// <summary>
    /// Add any syntax node type to a collection of another syntax node type, using runtime
    /// casts to avoid type-checking issues. This lets us get around difficulties with
    /// covariance and contravariance of generic collections.
    /// </summary>
    public static void Add<T>(this ICollection<T> collection, ISyntaxNode item) where T : ISyntaxNode
    {
      collection.Add((T) item);
    }
  }

  #endregion
}
