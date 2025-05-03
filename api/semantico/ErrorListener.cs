using Antlr4.Runtime;
using System;
using System.Collections.Generic;

namespace errores
{
    public class SemanticError2 : Exception
    {
        private string message;
        private static List<ErrorEntry> errors = new List<ErrorEntry>();

        private Antlr4.Runtime.IToken token;

        public SemanticError2(string message, Antlr4.Runtime.IToken token)
        {
            this.message = message;
            this.token = token;

            if (token == null)
            {
                
            }else{
                AddError(message, token);
            } 
            

        }

        public override string Message
        {
            get
            {
                
                return "";
            }
        }

        public void AddError(string message, IToken token, string errorType = "Semántico")
        {
            errors.Add(new ErrorEntry(message, token.Line, token.Column, errorType));
        }

        public List<(string Tipo, string Linea, string Columna, string Descripcion)> GetErrors()
        {
            var errorList = new List<(string, string, string, string)>();
            foreach (var error in errors)
            {
                errorList.Add((error.ErrorType, error.Line.ToString(), error.Column.ToString(), error.Message));
            }
            return errorList;
        }

        private class ErrorEntry
        {
            public string Message { get; }
            public int Line { get; }
            public int Column { get; }
            public string ErrorType { get; }

            public ErrorEntry(string message, int line, int column, string errorType)
            {
                Message = message;
                Line = line;
                Column = column;
                ErrorType = errorType;
            }
        }
    }
}