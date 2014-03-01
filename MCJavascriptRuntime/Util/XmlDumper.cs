// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Collections.Generic;
using System.IO;
using mjr.Expressions;
using System.Reflection;
using System.Xml.Serialization;

namespace mjr.Util
{
    public class XmlDumper
    {
        public static T Load<T>(string filename)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            var fileStream = new FileStream(filename, FileMode.Open);
            var obj = (T)xmlSerializer.Deserialize(fileStream);
            return obj;
        }
        public static void Save<T>(string filename, T obj)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            var fileStream = new FileStream(filename, FileMode.OpenOrCreate);
            xmlSerializer.Serialize(fileStream, obj);
            fileStream.Close();
        }
        public static void Run(string filename, JSFunctionMetadata func)
        {
            var dumper = new XmlDumper(new System.IO.StreamWriter(filename));
            dumper.Execute(func);
        }
        System.Xml.XmlWriter output;        

        public XmlDumper(TextWriter outputTarget)
        {
            output = System.Xml.XmlWriter.Create(outputTarget, new System.Xml.XmlWriterSettings { Indent = true });
        }

        public void Execute(JSFunctionMetadata func)
        {
            output.WriteStartElement("Program");
            Dump(func, 0, 0);
            output.WriteEndElement();
            output.Flush();
        }

        void DumpFields(object o, int recurseLevel, int maxRecurseLevel)
        {
            var type = o.GetType();
            foreach (var field in type.GetFields())
            {
                output.WriteStartElement(field.Name);
                if (field.Name == "Readers"
                    || field.Name == "Writers")
                    maxRecurseLevel = 3;
                var value = field.GetValue(o);
                Dump(value, recurseLevel, maxRecurseLevel);
                output.WriteEndElement();
            }
        }

        void DumpProperties(object o, int recurseLevel, int maxRecurseLevel)
        {
            var type = o.GetType();
            foreach (var prop in type.GetProperties())
            {
                output.WriteStartElement(prop.Name);
                if (prop.Name == "User")
                    maxRecurseLevel = 2;
                var value = prop.GetValue(o, null);
                Dump(value, recurseLevel, maxRecurseLevel);
                output.WriteEndElement();
            }
        }

        void Dump(object o, int recurseLevel, int maxRecurseLevel)
        {
            if (o == null)
                return;

            if (maxRecurseLevel != 0)
            {
                if (recurseLevel > maxRecurseLevel)
                    return;
                else
                    recurseLevel++;
            }

            if (o is Statement)
            {
                var type = o.GetType();
                if (o is MethodCall && (o as MethodCall).InlinedAst != null)
                    Dump((o as MethodCall).InlinedAst, recurseLevel, maxRecurseLevel);
                else
                {
                    output.WriteStartElement(type.Name);
                    DumpFields(o, recurseLevel, maxRecurseLevel);
                    DumpProperties(o, recurseLevel, maxRecurseLevel);
                    output.WriteEndElement();
                }
            }
            else if (o is JSSymbol)
            {
                var type = o.GetType();
                output.WriteStartElement(type.Name);
                DumpFields(o, recurseLevel, maxRecurseLevel);
                DumpProperties(o, recurseLevel, maxRecurseLevel);
                output.WriteEndElement();
            }
            else if (o is JSFunctionMetadata)
            {
                output.WriteStartElement("Symbols");
                Dump((o as JSFunctionMetadata).Symbols, recurseLevel, maxRecurseLevel);
                output.WriteEndElement();

                Dump((o as JSFunctionMetadata).AST, recurseLevel, maxRecurseLevel);
            }
            else if (o is IEnumerable<Identifier>)
            {
                foreach (var i in o as IEnumerable<Identifier>)
                    Dump(i, recurseLevel, maxRecurseLevel);
            }
            else if (o is IEnumerable<Expression>)
            {
                foreach (var i in o as IEnumerable<Expression>)
                    Dump(i, recurseLevel, maxRecurseLevel);
            }
            else if (o is IEnumerable<Statement>)
            {
                foreach (var i in o as IEnumerable<Statement>)
                    Dump(i, recurseLevel, maxRecurseLevel);
            }
            else if (o is IEnumerable<string>)
            {
                foreach (var i in o as IEnumerable<string>)
                    Dump(i, recurseLevel, maxRecurseLevel);
            }
            else if (o is ICollection<JSSymbol>)
            {
                foreach (var i in o as ICollection<JSSymbol>)
                    Dump(i, recurseLevel, maxRecurseLevel);
            }
            else if (o is IDictionary<string, Expression>)
            {
                foreach (var i in o as IDictionary<string, Expression>)
                {
                    output.WriteStartElement(i.Key);
                    Dump(i.Value, recurseLevel, maxRecurseLevel);
                    output.WriteEndElement();
                }
            }
            else
                output.WriteString(o.ToString());
        }
        #region IAstVisitor Members
#if NONE
        //public Native.JsInstance Result
        //{
        //    get { throw new NotImplementedException(); }
        //}

        public void Visit(Expressions.Program expression)
        {
            Dump(expression);
        }

        public void Visit(AssignmentExpression expression)
        {
            Dump(expression);
        }

        public void Visit(BlockStatement expression)
        {
            Dump(expression);
        }

        public void Visit(BreakStatement expression)
        {
            Dump(expression);
        }

        public void Visit(ContinueStatement expression)
        {
            Dump(expression);
        }

        public void Visit(DoWhileStatement expression)
        {
            Dump(expression);
        }

        public void Visit(EmptyStatement expression)
        {
            Dump(expression);
        }

        public void Visit(ExpressionStatement expression)
        {
            Dump(expression);
        }

        public void Visit(ForEachInStatement expression)
        {
            Dump(expression);
        }

        public void Visit(ForStatement expression)
        {
            Dump(expression);
        }

        public void Visit(FunctionDeclarationStatement expression)
        {
            Dump(expression);
        }

        public void Visit(IfStatement expression)
        {
            Dump(expression);
        }

        public void Visit(ReturnStatement expression)
        {
            Dump(expression);
        }

        public void Visit(SwitchStatement expression)
        {
            Dump(expression);
        }

        public void Visit(WithStatement expression)
        {
            Dump(expression);
        }

        public void Visit(ThrowStatement expression)
        {
            Dump(expression);
        }

        public void Visit(TryStatement expression)
        {
            Dump(expression);
        }

        public void Visit(VariableDeclarationStatement expression)
        {
            Dump(expression);
        }

        public void Visit(WhileStatement expression)
        {
            Dump(expression);
        }

        public void Visit(ArrayDeclaration expression)
        {
            Dump(expression);
        }

        public void Visit(CommaOperatorStatement expression)
        {
            Dump(expression);
        }

        public void Visit(FunctionExpression expression)
        {
            Dump(expression);
        }

        public void Visit(MethodCall expression)
        {
            Dump(expression);
        }

        public void Visit(Indexer expression)
        {
            Dump(expression);
        }

        public void Visit(PropertyExpression expression)
        {
            Dump(expression);
        }

        public void Visit(PropertyDeclarationExpression expression)
        {
            Dump(expression);
        }

        public void Visit(Identifier expression)
        {
            Dump(expression);
        }

        public void Visit(JsonExpression expression)
        {
            Dump(expression);
        }

        public void Visit(NewExpression expression)
        {
            Dump(expression);
        }

        public void Visit(BinaryExpression expression)
        {
            Dump(expression);
        }

        public void Visit(TernaryExpression expression)
        {
            Dump(expression);
        }

        public void Visit(UnaryExpression expression)
        {
            Dump(expression);
        }

        public void Visit(ValueExpression expression)
        {
            Dump(expression);
        }

        public void Visit(RegexpExpression expression)
        {
            Dump(expression);
        }

        public virtual void Visit(GotoStatement expression)
        {
            Dump(expression);
        }

        public virtual void Visit(StatementLabel expression)
        {
            Dump(expression);
        }

        public void Visit(Statement expression)
        {
            Dump(expression);
        }
#endif
        #endregion

    }
}
