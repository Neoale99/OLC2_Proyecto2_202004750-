using analyzer;
using Antlr4.Runtime.Misc;
using System;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using errores;
using System.IO;
using System.Xml.Serialization;

public class Visitor : GolightBaseVisitor<Object>
{
    private Utils.SymbolTable symbolTable = new Utils.SymbolTable();
    //            symbolTable.AddSymbol(new Utils.Symbol(id, type, "local", line, column));
    public GeneradorARM codigo = new GeneradorARM();
    private StringBuilder _dataSection = new StringBuilder();
    private StringBuilder _textSection = new StringBuilder();
    private StringBuilder _entrySection = new StringBuilder();
    private bool _hasStart = false;

    private bool _condicionCumplida = false; 
    private bool _TieneStart = false;
    public Visitor()
    {

    }

    public override Object VisitProgram(GolightParser.ProgramContext context)
    {
        Console.WriteLine("Iniciando programa");

        foreach (var contenido in context.contenido())
        {
            try
            {
                _condicionCumplida = false;
   
                Visit(contenido);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante la ejecución: {ex.Message}");
            }
        }

        Console.WriteLine("Programa finalizado");
        var symbolTable = GetSymbolTable();
        var symbols = symbolTable.GetAllSymbols();
        var tmp = symbols.Select(s => (s.Name, s.Type, s.Scope, s.Line.ToString(), s.Column.ToString())).ToList();
        //Console.WriteLine("TablaSimbolos:");
        foreach (var symbol in symbols)
        {
            Console.WriteLine($"Nombre: {symbol.Name}, Tipo: {symbol.Type}, Ámbito: {symbol.Scope}, Fila: {symbol.Line}, Columna: {symbol.Column}");
        }
        string tablaHTML = GenerarTablaSimbolos(tmp);
        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "TablaSimbolos.html");
        File.WriteAllText(path, tablaHTML);
        return null;
    }

    public override Object VisitContenido(GolightParser.ContenidoContext context)
    {   
        
        //Console.WriteLine("Visitando contenido " + context.GetChild(0).GetText());
        
            return Visit(context.GetChild(0));

        

        
    }
public override Object VisitPrintln(GolightParser.PrintlnContext context)
    {
        codigo.comentario("Print");
        foreach (var exp in context.expresion()) //Implementacion temporal unicamente para sumas y restas
        {
            Visit(exp);
            codigo.comentario("Imprimiendo");
            codigo.pop(Registers.x0); // Sacamos el valor de la pila
            codigo.Printint(Registers.x0); // Imprimimos el valor

        }
        return null;
    }

    public override Object VisitId(GolightParser.IdContext context)
    {
        return null;
    }


    public override Object VisitInt(GolightParser.IntContext context)
    {
        var entero = context.GetText();
        codigo.comentario($"Entero: {entero}");
        codigo.mov(Registers.x0, int.Parse(entero));
        codigo.push(Registers.x0);
        return null;
    }

    public override Object VisitFloat64(GolightParser.Float64Context context)
    {
        return null;
    }
    public override Object VisitString(GolightParser.StringContext context)
    {
        return null;
    }
    public override Object VisitBool(GolightParser.BoolContext context)
    {
        return null;
    }

    public override Object VisitRune(GolightParser.RuneContext context)
    {
        return null;
    }
    public override Object VisitNull(GolightParser.NullContext context)
    {
        return null;
    }

public override Object VisitDeclaexplicitavalor(GolightParser.DeclaexplicitavalorContext context)
{
        return null;
}

public override Object VisitDeclaexplicitanovalor(GolightParser.DeclaexplicitanovalorContext context)
    {
        return null;
    }

public override Object VisitDeclaracionimplicita(GolightParser.DeclaracionimplicitaContext context)
    {
        return null;
    }


    public override Object VisitAsignacionslicesimple(GolightParser.AsignacionslicesimpleContext context)
    {
 
        return null;
    }

    public override Object VisitSlices(GolightParser.SlicesContext context)
    {
        return null;
    }

    public override Object VisitAsignacionmetodos(GolightParser.AsignacionmetodosContext context)
    {

        return null;
    }
    public override Object VisitAsignacion(GolightParser.AsignacionContext context)
    {

        return null;
    }

    public override Object VisitAsignaciondesdefuncion(GolightParser.AsignaciondesdefuncionContext context)
    {

        return null;
    }


    public override Object VisitSumres(GolightParser.SumresContext context)
    {
        var operacion = context.op.Text;
        Console.WriteLine(context.GetText());
        Visit(context.expresion(0)); //Conseguimos el primer valor
        Visit(context.expresion(1)); //Conseguimos el segundo valor        
        codigo.pop(Registers.x1); //Cargamos el segundo valor en x1
        codigo.pop(Registers.x0); //Cargamos el primer valor en x0
        codigo.comentario($"Popeados ambos valores");
        if (operacion == "+")
        {
            codigo.add(Registers.x0, Registers.x0, Registers.x1); //x0 = Valor 1 + Valor 2
        }
        else if (operacion == "-")
        {
            codigo.sub(Registers.x0, Registers.x0, Registers.x1); //x0 = Valor 1 - Valor 2
        }
        else
        {
            throw new Exception($"Operación no soportada: {operacion}");
        }
        codigo.push(Registers.x0); //Guardamos el resultado en la pila
        return null;
    }

    public override Object VisitMultdivmod(GolightParser.MultdivmodContext context)
    {
        return null;
    }

    public override Object VisitOr(GolightParser.OrContext context)
    {
        return null;
    }

    public override Object VisitAnd(GolightParser.AndContext context)
    {
        return null;
    }

    public override Object VisitIgualdad(GolightParser.IgualdadContext context)
    {
        return null;
    }

    public override Object VisitRelacionales(GolightParser.RelacionalesContext context)
    {
        return null;
    }

    public override Object VisitUnario(GolightParser.UnarioContext context)
    {
        return null;
    }

    public override Object VisitParentesisexpre(GolightParser.ParentesisexpreContext context)
    {
        
        return Visit(context.expresion());
    }

    public override Object VisitCorchetesexpre(GolightParser.CorchetesexpreContext context)
    {
        
        return Visit(context.expresion());
    }
    public override Object VisitAtoi(GolightParser.AtoiContext context)
    {
        return null;
    }

    public override Object VisitParsefloat(GolightParser.ParsefloatContext context)
    {
        return null;
    }

    public override Object VisitTypeof(GolightParser.TypeofContext context)
    {
        return null;
    }

    public override Object VisitIncremento(GolightParser.IncrementoContext context)
    {
        return null;
    }

    public override Object VisitDecremento(GolightParser.DecrementoContext context)
    {
        return null;
    }

    public override Object VisitIncremento2(GolightParser.Incremento2Context context)
    {
        return null;
    }

    public override Object VisitDecremento2(GolightParser.Decremento2Context context)
    {
        return null;
    }
    public override Object VisitBloque(GolightParser.BloqueContext context)
    {
        return null;
    }

    public override Object VisitIf1(GolightParser.If1Context context)
    {
        return null;
    }



    public override Object VisitElseif(GolightParser.ElseifContext context)
    {
        return null;
    }


    public override Object VisitElse(GolightParser.ElseContext context)
    {
        return null;
    }

    public override Object VisitFor1(GolightParser.For1Context context)
    {
        return null;
    }
public override Object VisitFor2(GolightParser.For2Context context)
    {
        return null;

    }
    public override Object VisitFor3(GolightParser.For3Context context)
    {

      return null; 
    }


    public override Object VisitSwitch(GolightParser.SwitchContext context)
    {
        return null;
    }


    public override Object VisitBreak(GolightParser.BreakContext context)
    {
        return null;
    }

    public override Object VisitContinue(GolightParser.ContinueContext context)
    {
        return null;
    }

    public override Object VisitReturn(GolightParser.ReturnContext context)
    {
        return null;
    }

    public override Object VisitLen(GolightParser.LenContext context)
    {
        return null;
    }



    public override Object VisitJoin(GolightParser.JoinContext context)
    {
        return null;
    }



    public override Object VisitIndex(GolightParser.IndexContext context)
    {
        return null;
    }
    
    public override Object VisitAppend(GolightParser.AppendContext context)
    {
        return null;
    }



    public override Object VisitExpdotexp1(GolightParser.Expdotexp1Context context)
    {
        return null;
    }

    public override Object VisitExpdotexp(GolightParser.ExpdotexpContext context)
    {
        return null;
    }


    public override Object VisitFuncionvacia(GolightParser.FuncionvaciaContext context)
    {

        return null;
    }

    public override Object VisitFuncionconparametros(GolightParser.FuncionconparametrosContext context)
    {

        return null;
    }

    public override Object VisitFuncionanonima(GolightParser.FuncionanonimaContext context)
    {
        return null;
    }

    public override Object VisitLlamadafuncion1(GolightParser.Llamadafuncion1Context context)
    {
        return null;
    }

    public override Object VisitLlamadafuncion2(GolightParser.Llamadafuncion2Context context)
    {
        return null;
    }


    //Separacion entre visitors y metodos que no me dejo meter en otro archivo por alguna razon
    public string ProcesarSecuenciasEscape(string input)
    {
        return input
            .Replace("\\n", "\n") 
            .Replace("\\t", "\t")  
            .Replace("\\\"", "\"") 
            .Replace("\\\\", "\\"); 
    }

    

private string EscapeString(string str)
    {
        if (str.StartsWith("\"") && str.EndsWith("\""))
        {
            str = str.Substring(1, str.Length - 2);
        }
        return str.Replace("\n", "\\n").Replace("\t", "\\t").Replace("\"", "\\\"");
    }

    public static string GenerarTablaSimbolos(List<(string Nombre, string Tipo, string ambito, string Linea, string columna)> symbols)
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
        html.Append("<h1>Tabla de Símbolos</h1> \n");
        html.Append("<table> \n");
        html.Append("<tr><th>No.</th><th>Nombre</th><th>Tipo</th><th>Ámbito</th><th>Línea</th><th>Columna</th></tr> \n");

        int contador = 1;
        foreach (var symbol in symbols)
        {
            html.Append($"<tr><td>{contador}</td><td>{symbol.Nombre}</td><td>{symbol.Tipo}</td><td>{symbol.ambito}</td><td>{symbol.Linea}</td><td>{symbol.columna}</td></tr> \n");
            contador++;
        }

        html.Append("</table> \n");
        html.Append("</body></html> \n");

        return html.ToString();
    }
    public Utils.SymbolTable GetSymbolTable()
    {
        return symbolTable;
    }

}