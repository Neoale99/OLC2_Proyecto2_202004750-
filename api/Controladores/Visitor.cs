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

    public GeneradorARM codigo = new GeneradorARM();
    private StringBuilder _dataSection = new StringBuilder();
    private StringBuilder _textSection = new StringBuilder();
    private StringBuilder _entrySection = new StringBuilder();
    private string tipo = "";
    private string cadena = "";
    private bool _isdeclaracion = false;
    private bool _condicionCumplida = false; 
    private bool _comesfromasign = false;
    public int etiquetaCount = 0;
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
        //File.WriteAllText(path, tablaHTML);
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
        foreach (var exp in context.expresion()) //Implementacion temporal unicamente para operaciones basicas con enteros.
        {

            Visit(exp);
            codigo.comentario("Imprimiendo");
            if (tipo != null && tipo.StartsWith("[]"))
                {
                    
                    if (exp is GolightParser.IdContext idContext)
                    {
                        string id = idContext.GetText();
                        var simbolo = symbolTable.GetSymbol(id);
                        
                        if (simbolo != null && simbolo.SliceValues != null)
                        {
                            string sliceContent = "[";
                            string tipoElemento = simbolo.Type.Substring(2); 
                            
                            for (int i = 0; i < simbolo.SliceValues.Count; i++)
                            {
                                var elemento = simbolo.SliceValues[i];
                                
                                if (i > 0) sliceContent += " ";
                                
                                if (tipoElemento == "string")
                                    sliceContent += $"\"{elemento}\"";
                                else
                                    sliceContent += elemento?.ToString() ?? "null";
                            }
                            
                            sliceContent += "]";
                            codigo.Printstr(sliceContent);
                            continue;
                        }
                    }
                    
                    codigo.Printstr("Error: No se puede imprimir el slice");
                    continue;
                }
            switch (tipo)
            {
                case "int":
                    codigo.comentario($"Imprimiendo entero: {Registers.x0}");
                    codigo.pop(Registers.x0); // Sacamos el valor de la pila
                    codigo.Printint(Registers.x0); // Imprimimos el valor
                    tipo = ""; // Reiniciamos el tipo
                    break;
                case "string":
                    codigo.comentario($"Imprimiendo cadena: {cadena}");
                    Console.WriteLine(cadena);
                    if (cadena.StartsWith("\"") && cadena.EndsWith("\""))
                        {
                            cadena = cadena.Substring(1, cadena.Length - 2);
                        }
                    codigo.Printstr(cadena); // Imprimimos la cadena
                    cadena = ""; // Reiniciamos la cadena
                    tipo = ""; // Reiniciamos el tipo
                    break;
                case "float":
                    codigo.comentario("Imprimiendo flotante");
                    codigo.PrintFloat();
                    break;
                case "bool":
                    codigo.comentario($"Imprimiendo bool: {cadena}");
                    codigo.Printstr(cadena); 
                    cadena = ""; 
                    tipo = ""; 
                    break;
                case "rune":
                    codigo.comentario($"Imprimiendo rune: {cadena}");
                    codigo.Printstr(cadena); 
                    cadena = ""; 
                    tipo = ""; 
                    break;
                default:
                    throw new Exception($"Tipo no soportado para impresión: {tipo}");
            }
        }
        codigo.comentario("Salto de linea");
        codigo.PrintNewLine();
        return null;
    }

    public override Object VisitId(GolightParser.IdContext context)
    {
        string id = context.GetText();
        var symbol = symbolTable.GetSymbol(id);
        var tmp = symbol.Value;
        tipo = symbol.Type;
        // Manejar tipos de slices
        if (tipo != null && tipo.StartsWith("[]")) {
            cadena = id; 
        }


        switch (tipo)
        {
            case "int":
                codigo.comentario($"Entero: {tmp}");
                codigo.mov(Registers.x0, int.Parse(tmp));
                codigo.push(Registers.x0);
                cadena = symbol.Value;
                break;
            case "float":
                codigo.comentario($"ID flotante: {id}");
                codigo.comentario($"Flotante: {tmp}");
                codigo.LoadFloatBits(double.Parse(tmp));
                cadena = symbol.Value;
                break;
            case "string":
                cadena = symbol.Value;
                cadena = symbol.Value.Substring(1, symbol.Value.Length - 2); // Eliminar comillas
                break;
            case "bool":
                cadena = symbol.Value;
                break;
            case "rune":
                cadena = symbol.Value;
                break;
            case "[]int":
                cadena = id;
                break;
            case "[]string":
                cadena = id;
                break;
            default:
                throw new Exception($"Tipo no soportado: {tipo}");
        }
        cadena = symbol.Value;
        if (tipo != null && tipo.StartsWith("[]")) {
            cadena = id; 
        }
        return null;
    }

    public override Object VisitInt(GolightParser.IntContext context)
    {
        var entero = context.GetText();
        codigo.comentario($"Entero: {entero}");
        codigo.mov(Registers.x0, int.Parse(entero));
        codigo.push(Registers.x0);
        cadena = entero;
        tipo = "int";
        return null;
    }

    public override Object VisitFloat64(GolightParser.Float64Context context)
    {
        var flotante = context.GetText();
        double valflotante = double.Parse(flotante);
        codigo.comentario($"Flotante: {flotante}");
        codigo.LoadFloatBits(valflotante);
        tipo = "float";
        cadena = flotante;
        return null;
    }
    public override Object VisitString(GolightParser.StringContext context)
    {
        cadena = context.GetText();
        cadena = cadena.Substring(1, cadena.Length - 2); 


        tipo = "string";

        return null;
    }
    public override Object VisitBool(GolightParser.BoolContext context)
    {
        cadena = context.GetText();
        tipo = "bool";
        return null;
    }

    public override Object VisitRune(GolightParser.RuneContext context)
    {
        cadena = context.GetText();
        cadena = cadena.Substring(1, cadena.Length - 2); // Eliminar comillas
        tipo = "rune";
        return null;
    }
    public override Object VisitNull(GolightParser.NullContext context)
    {
        tipo = "nil";
        return null;
    }

public override Object VisitDeclaexplicitavalor(GolightParser.DeclaexplicitavalorContext context)
    {
        string id = context.ID().GetText();
        string tipoVariable = context.TIPO().GetText();
        if (tipoVariable == "float64") {
            tipoVariable = "float";
        }
        _comesfromasign = true;
        Visit(context.expresion());
        _comesfromasign = false;


        string valor = tipo switch
        {
            "int" => context.expresion().GetText(),
            "float" => context.expresion().GetText(),
            "string" => cadena,
            "bool" => cadena,
            "rune" => cadena,
            _ => throw new Exception($"Tipo no soportado: {tipoVariable}")
        };

        symbolTable.AddSymbol(new Utils.Symbol(id, tipoVariable, "local", context.Start.Line, context.Start.Column, valor));
        tipo = "";
        return null;
    }
public override Object VisitDeclaexplicitanovalor(GolightParser.DeclaexplicitanovalorContext context)
    {
        string id = context.ID().GetText();
        string tipoVariable = context.TIPO().GetText();
        if (tipoVariable == "float64") {
            tipoVariable = "float";
        }
        symbolTable.AddSymbol(new Utils.Symbol(id, tipoVariable, "local", context.Start.Line, context.Start.Column));
        return null;
    }
    public override Object VisitDeclaracionimplicita(GolightParser.DeclaracionimplicitaContext context)
    {
        string id = context.ID().GetText();
        
        _comesfromasign = true;
        Visit(context.expresion());
        _comesfromasign = false;

        // Obtener el valor según el tipo inferido
        string valor = tipo switch
        {
            "int" => cadena,
            "float" => cadena,
            "string" => cadena,
            "bool" => cadena,
            "rune" => cadena,
            _ => throw new Exception($"Tipo no soportado: {tipo}")
        };

        symbolTable.AddSymbol(new Utils.Symbol(id, tipo, "local", context.Start.Line, context.Start.Column, valor));
        return null;
    }

    public override Object VisitDeclaracionslicevalor(GolightParser.DeclaracionslicevalorContext ctx)
    {
        string id = ctx.ID().GetText();
        string sliceType = ctx.TIPO().GetText();
        
        // Crear el símbolo del slice con tipo []tipoElemento y asegurar que SliceValues esté inicializado
        var sliceSymbol = new Utils.Symbol(id, sliceType, "local", ctx.Start.Line, ctx.Start.Column, null);
        
        // Asegurar que SliceValues está correctamente inicializado
        if (sliceSymbol.SliceValues == null)
        {
            sliceSymbol.SliceValues = new List<object>();
        }
        
        // Agregar el símbolo a la tabla de símbolos
        symbolTable.AddSymbol(sliceSymbol);
        
        // Procesar valores iniciales si existen
        foreach (var valoresContext in ctx.valores())
        {
            Visit(valoresContext);
            string tipoElemento = sliceType.Substring(2); // Quitamos "[]" del tipo
            
            object valorConvertido;
            try {
                valorConvertido = ConvertirSegunTipo(cadena, tipoElemento);
            }
            catch (Exception ex) {
                throw new Exception($"Error al convertir valor '{cadena}' al tipo '{tipoElemento}': {ex.Message}");
            }
            
            symbolTable.Append(id, valorConvertido);
        }
        
        return null;
    }

    private object ConvertirSegunTipo(string valor, string tipo)
    {
        switch (tipo)
        {
            case "int":
                return int.Parse(valor);
            case "float":
                return double.Parse(valor);
            case "string":
                // Si es una cadena con comillas, quitar las comillas
                if (valor.StartsWith("\"") && valor.EndsWith("\""))
                    return valor.Substring(1, valor.Length - 2);
                return valor;
            case "bool":
                return bool.Parse(valor);
            default:
                return valor;
        }
    }
    public override Object VisitDeclaracionslicenovalor(GolightParser.DeclaracionslicenovalorContext context)
        {
            string id = context.ID().GetText();
            string sliceType = context.TIPO().GetText();

            return null;
        }
    public override Object VisitAsignacionslicesimple(GolightParser.AsignacionslicesimpleContext ctx)
    {   
        string id = ctx.ID().GetText();
        
        Visit(ctx.valor()[0]);
        int indice = int.Parse(cadena);
        
        Visit(ctx.expresion());
        string valorAsignar = cadena;
        string tipoValor = tipo;
        string tipoSlice = symbolTable.GetSymbolType(id);
        string tipoElemento = tipoSlice.Substring(2); 
        
        object valorConvertido = ConvertirSegunTipo(valorAsignar, tipoElemento);
        
        symbolTable.UpdateSliceElement(id, indice, valorConvertido);
        
        return null;
    }
    public override Object VisitSlicesdesc(GolightParser.SlicesdescContext context)
    {
        string id = context.ID().GetText();
        
        // Obtener el índice al que queremos acceder
        Visit(context.valor()[0]);
        int indice = int.Parse(cadena);
        
        // Verificar que el slice existe
        var simbolo = symbolTable.GetSymbol(id);
        if (simbolo == null)
        {
            throw new Exception($"El slice '{id}' no está definido");
        }
        
        // Asegurarnos que SliceValues esté inicializado
        if (simbolo.SliceValues == null)
        {
            throw new Exception($"{id} no es un slice o no está inicializado");
        }
        
        // Verificar que el índice es válido
        if (indice < 0 || indice >= simbolo.SliceValues.Count)
        {
            throw new Exception($"Índice fuera de rango: {indice}");
        }
        
        // Obtener el valor del slice en el índice
        var valor = simbolo.SliceValues[indice];
        
        // Establecer el tipo y cadena según el tipo del elemento
        string tipoElemento = simbolo.Type.Substring(2); // Quitar "[]"
        tipo = tipoElemento;
        
        // Convertir el valor a cadena según su tipo
        switch (tipoElemento)
        {
            case "int":
                cadena = valor.ToString();
                break;
            case "float":
                cadena = valor.ToString();
                break;
            case "string":
                cadena = $"\"{valor}\""; // Agregar comillas para strings
                break;
            case "bool":
                cadena = valor.ToString().ToLower();
                break;
            default:
                cadena = valor?.ToString() ?? "null";
                break;
        }
        
        return null;
    }

    public override Object VisitSlices(GolightParser.SlicesContext context)
    {
        foreach (var valoresContext in context.valores())
        {
            Visit(valoresContext); // Esto establecerá tipo y cadena
        }
        return null;
    }
    public override Object VisitDeclaracionimplicitaslice(GolightParser.DeclaracionimplicitasliceContext context)
    {
        string id = context.ID().GetText();
        
        string sliceType = "[]" + InferirTipoDeElementos(context);

        // Crear el símbolo del slice
        var sliceSymbol = new Utils.Symbol(id, sliceType, "local", context.Start.Line, context.Start.Column, null);
        
        // Agregar el símbolo a la tabla de símbolos
        symbolTable.AddSymbol(sliceSymbol);
        
        // Procesar los valores iniciales del slice
        foreach (var slicesContext in context.slices())
        {
            foreach (var valorContext in slicesContext.valores())
            {
                Visit(valorContext);
                string tipoElemento = sliceType.Substring(2); // Quitamos "[]" del tipo
                object valorConvertido;
                try {
                    valorConvertido = ConvertirSegunTipo(cadena, tipoElemento);
                }
                catch (Exception ex) {
                    throw new Exception($"Error al convertir valor '{cadena}' al tipo '{tipoElemento}': {ex.Message}");
                }
                
                symbolTable.Append(id, valorConvertido);
            }
        }
        
        return null;
    }
    private string InferirTipoDeElementos(GolightParser.DeclaracionimplicitasliceContext context)
    {
        if (context.slices().Length > 0 && context.slices()[0].valores().Length > 0)
        {
            var primerValor = context.slices()[0].valores()[0];
            Console.WriteLine("Primer valor: " + primerValor.GetText());
            Visit(primerValor);
            return tipo; 
        }

        return "any";
    }
    public override Object VisitAsignacionmetodos(GolightParser.AsignacionmetodosContext context)
    {

        return null;
    }
    public override Object VisitAsignacion(GolightParser.AsignacionContext context)
    {
        _comesfromasign = true;
        string id = context.expresion(0).GetText();
        //Console.WriteLine("ID: " + id);
        var symbol = symbolTable.GetSymbol(id);
        string tipoVariable = symbol.Type;
        Console.WriteLine("Variable: " + id + " con valor " + symbol.Value);
        Visit(context.expresion(1));


        if (tipoVariable == "string" && !cadena.StartsWith("\""))
        {   
            Console.WriteLine("Cadena sin comillas: " + cadena);
            cadena = $"\"{cadena}\"";
            Console.WriteLine("Cadena con comillas: " + cadena);
        }
        Console.WriteLine("Actualizando: "+id + "con valor" + cadena);
        symbolTable.UpdateSymbol(id, cadena);
        _comesfromasign = false;
        return null;
    }

    public override Object VisitAsignaciondesdefuncion(GolightParser.AsignaciondesdefuncionContext context)
    {

        return null;
    }


    public override Object VisitSumres(GolightParser.SumresContext context) 
        {
    var operacion = context.op.Text;
        
        if (_comesfromasign) {
            // Primera expresión
            Visit(context.expresion(0));
            string tipo1 = tipo;
            string val1 = cadena;

            // Segunda expresión
            Visit(context.expresion(1));
            string tipo2 = tipo;
            string val2 = cadena;
            cadena = "";
            // Si alguna expresión es ID, obtener su valor
            if (context.expresion(0) is GolightParser.IdContext id1)
            {
                val1 = symbolTable.GetSymbol(id1.GetText()).Value;
                val1 = val1.ToString();
            }
            if (context.expresion(1) is GolightParser.IdContext id2)
            {
                val2 = symbolTable.GetSymbol(id2.GetText()).Value;
                val2 = val2.ToString();
            }

            // Manejo de strings
            if (tipo1 == "string" && tipo2 == "string")
            {
                if (operacion == "+")
                {   
                    val1 = val1.Substring(1, val1.Length - 2); 
                    val2 = val2.Substring(1, val2.Length - 2); 
                    cadena = $"\"{val1}{val2}\"";
                    tipo = "string";
                    return null;
                }
                throw new Exception("Operación no válida para strings");
            }   

            // Operaciones numéricas
            if (tipo1 == "float" || tipo2 == "float") 
            {
                double num1 = double.Parse(val1);
                double num2 = double.Parse(val2);
                Console.WriteLine(num1 + num2);
                double resultado = operacion == "+" ? num1 + num2 : num1 - num2;
                tipo = "float";
                cadena = resultado.ToString();
            }
            else 
            {
                int num1 = int.Parse(val1);
                int num2 = int.Parse(val2);
                Console.WriteLine(num1 + num2);
                int resultado = operacion == "+" ? num1 + num2 : num1 - num2;
                tipo = "int";
                cadena = resultado.ToString();
            }
            return null;
        }else {

        Visit(context.expresion(0));
        string tmp1 = tipo;

        Visit(context.expresion(1));
        string tmp2 = tipo;

            if (tmp1 == "float" || tmp2 == "float") {
                codigo.comentario($"Operación flotante: {tmp1} {operacion} {tmp2}");
                if (tmp1 == "int") {
                    codigo.popFloat();            // Pop del float a d1
                    codigo.pop(Registers.x0);     // Pop del int
                    codigo.IntToFloat();          // Convierte int en d0
                } 
                else if (tmp2 == "int") {
                    codigo.pop(Registers.x0);     // Pop del int
                    codigo.LoadIntToFloat(Registers.x0); // Convierte int a float en d1
                    codigo.popFloat();            // Pop del primer float a d0
                }
                else {
                    // Ambos son float
                    codigo.popFloat();  // Pop segundo float a d1
                    codigo.popFloat();  // Pop primer float a d0
                }

                if (operacion == "+") {
                    codigo.addFloat(Registers.d0, Registers.d0, Registers.d1);
                }
                else if (operacion == "-") {
                    codigo.subFloat(Registers.d0, Registers.d0, Registers.d1);
                }
                codigo.pushFloat();
                tipo = "float";
            }
            else {
                codigo.pop(Registers.x1);
                codigo.pop(Registers.x0);
                if (operacion == "+") {
                    codigo.add(Registers.x0, Registers.x0, Registers.x1);
                }
                else if (operacion == "-") {
                    codigo.sub(Registers.x0, Registers.x0, Registers.x1);
                }
                codigo.push(Registers.x0);
                tipo = "int";
            }
        }
        return null;
    } 
    public override Object VisitMultdivmod(GolightParser.MultdivmodContext context)
    {
        var operacion = context.op.Text;
        if (_comesfromasign == true)     {
        // Evaluar primera expresión
        Visit(context.expresion(0));
        string tipo1 = tipo;
        string val1;
        
        // Si es un ID, obtener el valor de la tabla de símbolos
        if (context.expresion(0) is GolightParser.IdContext id1)
        {
            val1 = symbolTable.GetSymbol(id1.GetText()).Value;
        }
        else
        {
            val1 = context.expresion(0).GetText();
        }

        // Evaluar segunda expresión
        Visit(context.expresion(1));
        string tipo2 = tipo;
        string val2;
        
        if (context.expresion(1) is GolightParser.IdContext id2)
        {
            val2 = symbolTable.GetSymbol(id2.GetText()).Value;
        }
        else
        {
            val2 = context.expresion(1).GetText();
        }

        // Realizar operación según tipos
        if (tipo1 == "float" || tipo2 == "float") 
        {
            double num1 = double.Parse(val1);
            double num2 = double.Parse(val2);
            double resultado;

            switch (operacion)
            {
                case "*":
                    resultado = num1 * num2;
                    break;
                case "/":
                    resultado = num1 / num2;
                    break;
                default:
                    throw new Exception($"Operación no válida para flotantes: {operacion}");
            }

            tipo = "float";
            cadena = resultado.ToString();
        }
        else 
        {
            int num1 = int.Parse(val1);
            int num2 = int.Parse(val2);
            int resultado;

            switch (operacion)
            {
                case "*":
                    resultado = num1 * num2;
                    break;
                case "/":
                    if (num1 % num2 == 0)
                    {
                        resultado = num1 / num2;
                        tipo = "int";
                    }
                    else
                    {
                        double resultadoFloat = (double)num1 / num2;
                        tipo = "float";
                        cadena = resultadoFloat.ToString();
                        return null;
                    }
                    break;
                case "%":
                    resultado = num1 % num2;
                    break;
                default:
                    throw new Exception($"Operación no válida: {operacion}");
            }

            tipo = "int";
            cadena = resultado.ToString();
        }     
        } else {
        Visit(context.expresion(0));
        string tmp1 = tipo;
        Visit(context.expresion(1));
        string tmp2 = tipo;
        string tmpval1 = context.expresion(0).GetText();
        string tmpval2 = context.expresion(1).GetText();
        if (tmp1 == "float" || tmp2 == "float") {
            codigo.comentario($"Operación flotante: {tmp1} {operacion} {tmp2}");
            if (tmp1 == "int") {
                codigo.popFloat();            // Pop del float a d1
                codigo.pop(Registers.x0);     // Pop del int
                codigo.IntToFloat();          // Convierte int en d0
                
                if (operacion == "/") {
                    // Para división, no intercambiamos los operandos
                    codigo.divFloat(Registers.d0, Registers.d0, Registers.d1);
                } else {
                    codigo.SwapFloats();          // Intercambia d0 y d1 para otras operaciones
                    if (operacion == "*") {
                        codigo.mulFloat(Registers.d0, Registers.d0, Registers.d1);
                    }
                }
            } 
            else if (tmp2 == "int") {
                codigo.pop(Registers.x0);     // Pop del int
                codigo.LoadIntToFloat(Registers.x0); // Convierte int a float en d1
                codigo.popFloat();            // Pop del primer float a d0
                
                if (operacion != "/") {
                    // Para multiplicación y suma, el orden no importa
                    if (operacion == "*") {
                        codigo.mulFloat(Registers.d0, Registers.d0, Registers.d1);
                    }
                } else {
                    // Para división, mantener el orden correcto
                    codigo.divFloat(Registers.d0, Registers.d0, Registers.d1);
                }
            }
            else {
                codigo.popFloat();  // Pop segundo float a d1
                codigo.popFloat();  // Pop primer float a d0
                if (operacion == "*") {
                    codigo.mulFloat(Registers.d0, Registers.d0, Registers.d1);
                }
                else if (operacion == "/") {
                    codigo.divFloat(Registers.d0, Registers.d0, Registers.d1);
                }
            }
            
            codigo.pushFloat();
            tipo = "float";
        }
        else {
            codigo.pop(Registers.x1);
            codigo.pop(Registers.x0);
            if (operacion == "*") {
                codigo.mul(Registers.x0, Registers.x0, Registers.x1);
            }
            else if (operacion == "/") {
                if (int.TryParse(tmpval1, out int val1) && int.TryParse(tmpval2, out int val2)) {
                    if (val1 % val2 != 0) {
                        // La división resultará en flotante
                        codigo.IntToFloat();  // Convierte dividendo a float
                        codigo.mov(Registers.x0, val2);
                        codigo.LoadIntToFloat(Registers.x0);  // Convierte divisor a float
                        codigo.divFloat(Registers.d0, Registers.d0, Registers.d1);
                        codigo.pushFloat();
                        tipo = "float";
                    } else {
                        // División entera normal
                        codigo.div(Registers.x0, Registers.x0, Registers.x1);
                        codigo.push(Registers.x0);
                        tipo = "int";
                    }
                } else {
                    // Si no podemos determinar los valores, hacer división entera
                    codigo.div(Registers.x0, Registers.x0, Registers.x1);
                    codigo.push(Registers.x0);
                    tipo = "int";
                }
            }
            else if (operacion == "%") {
                codigo.mod(Registers.x0, Registers.x0, Registers.x1);
            }
            codigo.push(Registers.x0);
            tipo = "int";
        }
        }
        return null;
    }

public override Object VisitAnd(GolightParser.AndContext context)
{
    // Evaluamos la primera expresión
    Visit(context.expresion(0));
    string tipo1 = tipo;
    string val1 = cadena;

    // Verificar que sea booleano
    if (tipo1 != "bool")
    {
        throw new Exception($"El operador '&&' espera un booleano, pero recibió {tipo1}");
    }

    // Evaluamos la segunda expresión
    Visit(context.expresion(1));
    string tipo2 = tipo;
    string val2 = cadena;

    // Verificar que sea booleano
    if (tipo2 != "bool")
    {
        throw new Exception($"El operador '&&' espera un booleano, pero recibió {tipo2}");
    }

    // Realizar operación AND
    bool resultado = bool.Parse(val1) && bool.Parse(val2);
    
    tipo = "bool";
    cadena = resultado.ToString().ToLower();
    
    return null;
}

    public override Object VisitOr(GolightParser.OrContext context)
    {
        // Evaluamos la primera expresión
        Visit(context.expresion(0));
        string tipo1 = tipo;
        string val1 = cadena;

        // Verificar que sea booleano
        if (tipo1 != "bool")
        {
            throw new Exception($"El operador '||' espera un booleano, pero recibió {tipo1}");
        }

        // Evaluamos la segunda expresión
        Visit(context.expresion(1));
        string tipo2 = tipo;
        string val2 = cadena;

        // Verificar que sea booleano
        if (tipo2 != "bool")
        {
            throw new Exception($"El operador '||' espera un booleano, pero recibió {tipo2}");
        }

        // Realizar operación OR
        bool resultado = bool.Parse(val1) || bool.Parse(val2);
        
        tipo = "bool";
        cadena = resultado.ToString().ToLower();
        
        return null;
    }

    public override Object VisitRelacionales(GolightParser.RelacionalesContext context)
    {
        // Evaluar primera expresión
        Visit(context.expresion(0));
        string tipo1 = tipo;
        string val1 = cadena;

        // Evaluar segunda expresión
        Visit(context.expresion(1));
        string tipo2 = tipo;
        string val2 = cadena;

        bool resultado = false;
        
        if (tipo1 == "float" || tipo2 == "float")
        {
            double num1 = double.Parse(val1);
            double num2 = double.Parse(val2);

            switch (context.op.Text)
            {
                case ">": resultado = num1 > num2; break;
                case "<": resultado = num1 < num2; break;
                case ">=": resultado = num1 >= num2; break;
                case "<=": resultado = num1 <= num2; break;
            }
        }
        else if (tipo1 == "int" && tipo2 == "int")
        {
            int num1 = int.Parse(val1);
            int num2 = int.Parse(val2);

            switch (context.op.Text)
            {
                case ">": resultado = num1 > num2; break;
                case "<": resultado = num1 < num2; break;
                case ">=": resultado = num1 >= num2; break;
                case "<=": resultado = num1 <= num2; break;
            }
        }

        tipo = "bool";
        // Importante: guardar el resultado como 1 o 0 para ARM64
        cadena = resultado ? "true" : "false";
        Console.WriteLine($"Resultado de comparación: {cadena}");

        return null;
    }

    public override Object VisitIgualdad(GolightParser.IgualdadContext context)
    {
        // Evaluar primera expresión
        Visit(context.expresion(0));
        string tipo1 = tipo;
        string val1 = cadena;

        // Evaluar segunda expresión
        Visit(context.expresion(1));
        string tipo2 = tipo;
        string val2 = cadena;

        bool resultado = false;

        switch (tipo1)
        {
            case "int" when tipo2 == "int":
                int num1 = int.Parse(val1);
                int num2 = int.Parse(val2);
                resultado = context.op.Text == "==" ? num1 == num2 : num1 != num2;
                break;
                
            case "float" when tipo2 == "float":
                double float1 = double.Parse(val1);
                double float2 = double.Parse(val2);
                resultado = context.op.Text == "==" ? 
                    Math.Abs(float1 - float2) < 0.000001 : 
                    Math.Abs(float1 - float2) >= 0.000001;
                break;
                
            case "string" when tipo2 == "string":
                // Remover comillas
                val1 = val1.Trim('"');
                val2 = val2.Trim('"');
                resultado = context.op.Text == "==" ? val1 == val2 : val1 != val2;
                break;
                
            case "bool" when tipo2 == "bool":
                bool bool1 = bool.Parse(val1);
                bool bool2 = bool.Parse(val2);
                resultado = context.op.Text == "==" ? bool1 == bool2 : bool1 != bool2;
                break;
        }

        tipo = "bool";
        
        cadena = resultado ? "true" : "false";
        Console.WriteLine($"Resultado de comparación: {cadena}");
        return null;
    }
    public override Object VisitUnario(GolightParser.UnarioContext context)
    {
            var operador = context.op.Text;
    
    if (_comesfromasign) 
    {
        Visit(context.expresion());
        
        if (operador == "!")
        {
            if (tipo == "bool")
            {
                // Negar el valor booleano
                cadena = cadena.ToLower() == "true" ? "false" : "true";
            }
            else
            {
                throw new Exception($"No se puede aplicar el operador '!' a un tipo {tipo}");
            }
        } else {
            throw new Exception($"Operador {operador} no válido para tipo {tipo}");
        }
        
    }
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
        string cadenaNumero = context.CADENA().GetText();

        cadenaNumero = cadenaNumero.Substring(1, cadenaNumero.Length - 2);
        
        if (int.TryParse(cadenaNumero, out int resultado))
        {
            codigo.comentario($"Convirtiendo string a int: {cadenaNumero}");
            codigo.mov(Registers.x0, resultado);
            codigo.push(Registers.x0);
            tipo = "int";
            cadena = resultado.ToString();
        }
        else
        {
            throw new Exception($"Error al convertir '{cadenaNumero}' a entero");
        }
        
        return null;
    }

    public override Object VisitParsefloat(GolightParser.ParsefloatContext context)
    {
        string cadenaNumero = context.CADENA().GetText();

        cadenaNumero = cadenaNumero.Substring(1, cadenaNumero.Length - 2);
        
        if (double.TryParse(cadenaNumero, out double resultado))
        {
            codigo.comentario($"Convirtiendo string a float: {cadenaNumero}");
            codigo.LoadFloatBits(resultado);
            tipo = "float";
            cadena = resultado.ToString();
        }
        else
        {
            throw new Exception($"Error al convertir '{cadenaNumero}' a float");
        }
        
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
        string etiquetaFalse = $"L{etiquetaCount++}";
        string etiquetaFin = $"L{etiquetaCount++}";

        // Evaluar toda la condición primero
        Visit(context.expresion(0));
        if (tipo != "bool")
        {
            throw new Exception("La condición debe ser de tipo booleano");
        }

        codigo.comentario("If statement");
        codigo.pop(Registers.x0);
        codigo.cmp(Registers.x0, 0);  // Comparar con 0 (false)
        codigo.beq(etiquetaFalse);    // Si es false, saltar al else

        // Bloque if (true)
        foreach (var stmt in context.contenido())
        {
            Visit(stmt);
        }
        codigo.b(etiquetaFin);        // Saltar al final después de ejecutar el if

        // Bloque else (false)
        codigo.etiqueta(etiquetaFalse);
        if (context.contenido().Length > 1)
        {
            foreach (var stmt in context.contenido().Skip(1))
            {
                Visit(stmt);
            }
        }

        codigo.etiqueta(etiquetaFin);
        return null;
    }

    public override Object VisitElseif(GolightParser.ElseifContext context)
    {
        string etiquetaFalse = $"L{etiquetaCount++}";
        string etiquetaFin = $"L{etiquetaCount++}";

        codigo.comentario("If-else chain");
        
        // Evaluar condición del if
        Visit(context.expresion(0));
        if (tipo != "bool")
        {
            throw new Exception("La condición debe ser de tipo booleano");
        }

        codigo.pop(Registers.x0);
        codigo.cmp(Registers.x0, 0);  // Comparar con 0 (false)
        codigo.beq(etiquetaFalse);    // Si es false, ir al else

        // Bloque if (true)
        foreach (var stmt in context.contenido())
        {
            Visit(stmt);
        }
        codigo.b(etiquetaFin);        // Saltar al final después de ejecutar el if

        // Bloque else (false)
        codigo.etiqueta(etiquetaFalse);
        if (context.expresion().Length > 1)
        {
            Visit(context.expresion(1));  // Evaluar condición del else-if
            codigo.pop(Registers.x0);
            codigo.cmp(Registers.x0, 1);
            codigo.beq(etiquetaFin);      // Si es true, ejecutar el bloque
            
            foreach (var stmt in context.contenido().Skip(1))
            {
                Visit(stmt);
            }
        }

        codigo.etiqueta(etiquetaFin);
        return null;
    }
    public override Object VisitElse(GolightParser.ElseContext context)
    {
        // Ejecutar el bloque else
        foreach (var stmt in context.contenido())
        {
            Visit(stmt);
        }
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

public override Object VisitIndex(GolightParser.IndexContext ctx)
{
    // Obtener la cadena
    Visit(ctx.ID());
    string cadenaABuscar = cadena;
    
    // Obtener la letra/subcadena a buscar
    string subcadena = ctx.valor().GetText();
    subcadena = subcadena.Substring(1, subcadena.Length - 2); // Quitar comillas
    
    // Buscar el índice
    int indice = cadenaABuscar.IndexOf(subcadena);
    
    // Establecer tipo y valor de retorno
    tipo = "int";
    cadena = indice.ToString();
    
    // Manejo de ARM
    codigo.comentario($"Índice de '{subcadena}' en '{cadenaABuscar}': {indice}");
    codigo.mov(Registers.x0, indice);
    codigo.push(Registers.x0);
    
    return null;
}

    public override Object VisitJoin(GolightParser.JoinContext ctx)
    {
        // Obtener el slice a unir
        string sliceId = ctx.ID().GetText();
        var simbolo = symbolTable.GetSymbol(sliceId);
        
        if (simbolo == null || simbolo.SliceValues == null)
        {
            throw new Exception($"'{sliceId}' no es un slice válido");
        }
        
        // Obtener el separador
        string separador = ctx.CADENA().GetText();
        separador = separador.Substring(1, separador.Length - 2); // Quitar comillas
        
        // Verificar que el slice es de strings
        string tipoElemento = simbolo.Type.Substring(2); // Quitar "[]"
        if (tipoElemento != "string")
        {
            throw new Exception($"La función join espera un slice de strings, pero recibió {simbolo.Type}");
        }
        
        // Unir los elementos del slice
        string resultado = "";
        for (int i = 0; i < simbolo.SliceValues.Count; i++)
        {
            if (i > 0) resultado += separador;
            resultado += simbolo.SliceValues[i]?.ToString() ?? "";
        }
        
        // Establecer tipo y valor de retorno
        tipo = "string";
        cadena = resultado;
        
        // Manejo de ARM
        codigo.comentario($"Join de '{sliceId}' con separador '{separador}': '{resultado}'");
        codigo.Printstr(resultado);
        
        return null;
    }

    public override Object VisitLen(GolightParser.LenContext ctx)
    {
        // Obtener el identificador del slice o cadena
        string id = ctx.expresion().GetText();
        var simbolo = symbolTable.GetSymbol(id);
        
        if (simbolo == null)
        {
            throw new Exception($"La variable '{id}' no está definida");
        }
        
        int longitud;
        
        // Determinar la longitud según el tipo
        if (simbolo.Type.StartsWith("[]"))
        {
            // Es un slice
            if (simbolo.SliceValues == null)
            {
                longitud = 0;
            }
            else
            {
                longitud = simbolo.SliceValues.Count;
            }
        }
        else if (simbolo.Type == "string")
        {
            // Es una cadena
            string valor = simbolo.Value;
            if (valor.StartsWith("\"") && valor.EndsWith("\""))
            {
                valor = valor.Substring(1, valor.Length - 2); // Quitar comillas
            }
            longitud = valor.Length;
        }
        else
        {
            throw new Exception($"La función len no se puede aplicar a una variable de tipo {simbolo.Type}");
        }
        
        // Establecer tipo y valor de retorno
        tipo = "int";
        cadena = longitud.ToString();
        
        // Manejo de ARM
        codigo.comentario($"Longitud de '{id}': {longitud}");
        codigo.mov(Registers.x0, longitud);
        codigo.push(Registers.x0);
        
        return null;
    }

    public override Object VisitAppend(GolightParser.AppendContext ctx)
    {
        // Obtener el slice al que se añadirá el elemento
        string sliceId = ctx.ID().GetText();
        var simbolo = symbolTable.GetSymbol(sliceId);
        
        if (simbolo == null)
        {
            throw new Exception($"La variable '{sliceId}' no está definida");
        }
        
        if (!simbolo.Type.StartsWith("[]"))
        {
            throw new Exception($"La función append espera un slice, pero '{sliceId}' es de tipo {simbolo.Type}");
        }
        
        // Obtener el elemento a añadir
        Visit(ctx.expresion());
        string valorAAppend = cadena;
        string tipoValor = tipo;
        
        // Verificar compatibilidad de tipos
        string tipoElemento = simbolo.Type.Substring(2); // Quitar "[]"
        if (tipoElemento != tipoValor && !(tipoElemento == "float" && tipoValor == "int"))
        {
            throw new Exception($"No se puede añadir un valor de tipo {tipoValor} a un slice de tipo {simbolo.Type}");
        }
        
        // Convertir el valor al tipo correcto
        object valorConvertido = ConvertirSegunTipo(valorAAppend, tipoElemento);
        
        // Añadir el elemento al slice
        symbolTable.Append(sliceId, valorConvertido);
        
        // La función append devuelve el slice modificado
        tipo = simbolo.Type;
        cadena = sliceId;
        
        codigo.comentario($"Append a '{sliceId}': valor {valorAAppend}");
        
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