using System;
using Antlr4.Runtime;
using analyzer;
using Antlr4.Runtime.Tree;
using System.Diagnostics;
using System.Text;
using errores;

public static class Utils
{

    public static string dot(IParseTree tree, Parser parser)
    {
        var dotBuilder = new StringBuilder();
        dotBuilder.AppendLine("digraph ParseTree {");
        dotBuilder.AppendLine("  node [shape=egg,  style=filled color = cyan];");

        void BuildTree(IParseTree node, int parentId, ref int nodeId)
        {
            var currentNodeId = nodeId++;
            var nodeText = EscapeLabel(GetNodeText(node, parser));
            dotBuilder.AppendLine($"  node{currentNodeId} [label=\"{nodeText}\"];");

            if (parentId != -1)
            {
                dotBuilder.AppendLine($"  node{parentId} -> node{currentNodeId};");
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                BuildTree(node.GetChild(i), currentNodeId, ref nodeId);
            }
        }

        int nodeId = 0;
        BuildTree(tree, -1, ref nodeId);

        dotBuilder.AppendLine("}");
        return dotBuilder.ToString();
    }

    private static string GetNodeText(IParseTree node, Parser parser)
    {
        if (node is IRuleNode ruleNode)
        {
            var ruleIndex = ruleNode.RuleContext.RuleIndex;
            return parser.RuleNames[ruleIndex];
        }
        else if (node is ITerminalNode terminalNode)
        {
            return terminalNode.GetText();
        }
        return string.Empty;
    }

private static string EscapeLabel(string label)
{
    return label
        .Replace("\\", "\\\\")  
        .Replace("\"", "\\\"")  
        .Replace("\n", "\\n")   
        .Replace("\r", "\\r")   
        .Replace("\t", "\\t");  
}

    public static void Svgdot(string dotContent, string outputFilePath)
{
    
    string tempDotFile = Path.GetTempFileName();
    File.WriteAllText(tempDotFile, dotContent);

   
    var processStartInfo = new ProcessStartInfo
    {
        FileName = "dot", 
        Arguments = $"-Tsvg \"{tempDotFile}\" -o \"{outputFilePath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    
    using (var process = Process.Start(processStartInfo))
    {
        if (process != null)
        {
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new Exception($"Error al generar el SVG: {error}");
            }
        }
    }

    
    File.Delete(tempDotFile);
}


public static string GenerarTablaErrores(List<(string Tipo, string Linea, string Columna, string Descripcion)> errores)
{
    
    StringBuilder html = new StringBuilder();
    html.Append("<html><head> <meta charset=\"UTF-8\"> <style>");
    html.Append(@"
        h1 {
            text-align: center;
        }
        table {
            width: 80%;
            margin: 0 auto;
            border-collapse: collapse;
            border: 3px solid black; 
        }
        th, td {
            border: 2px solid black; 
            padding: 10px;
            text-align: left;
        }
        th {
            background-color: cyan;
        }
        td {
            background-color: lightblue;
        }
    ");
    html.Append("</style></head><body> \n");
    html.Append("<h1>Tabla de errores</h1> \n");
    html.Append("<table> \n"); 
    html.Append("<tr><th>No.</th><th>Descripción</th><th>Línea</th><th>Columna</th><th>Tipo</th></tr> \n");

    int contador = 1;
    foreach (var error in errores)
    {
        html.Append($"<tr><td>{contador}</td><td>{error.Descripcion}</td><td>{error.Linea}</td><td>{error.Columna}</td><td>{error.Tipo}</td></tr> \n");
        contador++;
    }

    html.Append("</table> \n");
    html.Append("</body></html> \n");

    return html.ToString();
}





    public class RawErrorListener : IAntlrErrorListener<IToken>
    {
        public List<(string Tipo, string Linea, string Columna, string Descripcion)> Errors { get; } = new List<(string, string, string, string)>();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string tipo = "Sintáctico";
            Errors.Add((tipo, line.ToString(), charPositionInLine.ToString(), $"Token no esperado: {offendingSymbol.Text}"));
        }
    }
    public class LexerErrorListener : IAntlrErrorListener<int>
    {
        public List<(string Tipo, string Linea, string Columna, string Descripcion)> Errors { get; } = new List<(string, string, string, string)>();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string tipo = "Léxico";
            string tokenIncorrecto = ExtraerTokenIncorrecto(msg); 
            Errors.Add((tipo, line.ToString(), charPositionInLine.ToString(), $"Token incorrecto: {tokenIncorrecto}"));
        }

        private string ExtraerTokenIncorrecto(string mensajeError)
        {
        
            int inicio = mensajeError.IndexOf("'") + 1;
            int fin = mensajeError.LastIndexOf("'");
            if (inicio >= 0 && fin >= 0 && fin > inicio)
            {
                return mensajeError.Substring(inicio, fin - inicio);
            }
            return "Desconocido"; 
        }
    }

    public class Symbol
    {
        public string Name { get; } 
        public string Type { get; } 
        public string Scope { get; } 
        public int Line { get; } 
        public int Column { get; } 

        public Symbol(string name, string type, string scope, int line, int column)
        {
            Name = name;
            Type = type;
            Scope = scope;
            Line = line;
            Column = column;
        }
    }

    public class SymbolTable
    {
        private Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();

        public void AddSymbol(Symbol symbol)
        {
            if (symbols.ContainsKey(symbol.Name))
            {
                throw new Exception($"El símbolo '{symbol.Name}' ya está definido.");
            }
            symbols[symbol.Name] = symbol;
        }

        public Symbol GetSymbol(string name)
        {
            if (symbols.TryGetValue(name, out var symbol))
            {
                return symbol;
            }
            throw new Exception($"El símbolo '{name}' no está definido.");
        }

        public bool ContainsSymbol(string name)
        {
            return symbols.ContainsKey(name);
        }

        public List<Symbol> GetAllSymbols()
        {
            return new List<Symbol>(symbols.Values);
        }
    }

    public class ScopeManager
    {
        private Stack<SymbolTable> scopeStack = new Stack<SymbolTable>();

        public ScopeManager()
        {

            EnterScope();
        }


        public void EnterScope()
        {
            scopeStack.Push(new SymbolTable());
        }

        public void ExitScope()
        {
            if (scopeStack.Count > 1) 
            {
                scopeStack.Pop();
            }
        }

        public SymbolTable CurrentScope()
        {
            return scopeStack.Peek();
        }

        public Symbol GetSymbol(string name)
        {
            foreach (var scope in scopeStack)
            {
                if (scope.ContainsSymbol(name))
                {
                    return scope.GetSymbol(name);
                }
            }
            throw new Exception($"El símbolo '{name}' no está definido.");
        }
    }
}


