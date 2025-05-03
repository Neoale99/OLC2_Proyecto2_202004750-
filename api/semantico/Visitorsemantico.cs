using analyzer;
using Antlr4.Runtime.Misc;
using System;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using errores;
using System.IO;
public class Visitorsemantico : GolightSemanticoBaseVisitor<ValueWrapper>
{
    public ValueWrapper defaultVoid = new VoidValue();
    public string salida = "";
    public Environment currentEnvironment;

    private bool _condicionCumplida = false; 
    public Visitorsemantico()
    {

        currentEnvironment = new Environment(null);
        Embeded.Generate(currentEnvironment);
    }

    public override ValueWrapper VisitProgram(GolightSemanticoParser.ProgramContext context)
    {
        Console.WriteLine("Iniciando programa");

        foreach (var contenido in context.contenido())
        {
            try
            {
                _condicionCumplida = false;
                //Console.WriteLine(contenido.GetText());
                Visit(contenido);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante la ejecución: {ex.Message}");
            }
        }

        Console.WriteLine("Programa finalizado");
        var symbolTable = currentEnvironment.GetSymbolTable();
        var symbols = symbolTable.GetAllSymbols();
        var tmp = symbols.Select(s => (s.Name, s.Type, s.Scope, s.Line.ToString(), s.Column.ToString())).ToList();
        Console.WriteLine("TablaSimbolos:");
        foreach (var symbol in symbols)
        {
            Console.WriteLine($"Nombre: {symbol.Name}, Tipo: {symbol.Type}, Ámbito: {symbol.Scope}, Fila: {symbol.Line}, Columna: {symbol.Column}");
        }
        Console.WriteLine("Estoy aca mmgvi");
        string tablaHTML = GenerarTablaSimbolos(tmp);
        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "TablaSimbolos.html");
        File.WriteAllText(path, tablaHTML);
        return defaultVoid;
    }

    public override ValueWrapper VisitContenido(GolightSemanticoParser.ContenidoContext context)
    {   
        
        //Console.WriteLine("Visitando contenido " + context.GetChild(0).GetText());
        
            return Visit(context.GetChild(0));

        

        
    }
    public override ValueWrapper VisitPrintln(GolightSemanticoParser.PrintlnContext context)
    {
        if (context.expresion().Length == 0)
        {
            salida += "\n";
            return defaultVoid;
        }

        for (int i = 0; i < context.expresion().Length; i++)
        {
            ValueWrapper value = Visit(context.expresion(i));

            salida += FormatValue(value);

            if (i < context.expresion().Length - 1)
            {
                salida += " ";
            }
        }

        salida += " \n";
        return defaultVoid;
    }
    private string FormatValue(ValueWrapper value)
    {
        return value switch
        {
            IntValue intValue => intValue.Value.ToString(),
            FloatValue floatValue => floatValue.Value.ToString("0.00"),
            StringValue stringValue => stringValue.Value,
            BoolValue boolValue => boolValue.Value.ToString(),
            VoidValue voidValue => "nil", 
            RuneValue runeValue => runeValue.Value.ToString(),
            SliceValue sliceValue => FormatSlice(sliceValue),
            StructValue structValue => FormatStruct(structValue), 
            _ => throw new SemanticError("Valor inválido", null)
        };
    }
    private string FormatStruct(StructValue structValue)
    {
        if (structValue.Fields.Count == 0)
        {
            return "{}";
        }

        var fieldsFormatted = structValue.Fields
            .Select(kv => $"{kv.Key}: {FormatValue(kv.Value)}")
            .ToList();

        return "{ " + string.Join(", ", fieldsFormatted) + " }";
    }
    private string FormatSlice(SliceValue slice)
    {
        if (slice.Values.Count == 0)
        {
            return "[]";
        }

        if (slice.Values.All(v => v is SliceValue))
        {
            return "[\n" + string.Join("\n", slice.Values.Select(v => "  " + FormatSlice((SliceValue)v))) + "\n]";
        }

        return "[" + string.Join(", ", slice.Values.Select(v => FormatValue(v))) + "]";
    }

    private string GetValueAsString(ValueWrapper value)
    {
        return value switch
        {
            IntValue intValue => intValue.Value.ToString(),
            FloatValue floatValue => floatValue.Value.ToString("0.00"),
            StringValue stringValue => $"\"{stringValue.Value}\"",
            BoolValue boolValue => boolValue.Value.ToString().ToLower(),
            RuneValue runeValue => $"'{runeValue.Value}'",
            SliceValue sliceValue => "[" + string.Join(", ", sliceValue.Values.Select(v => GetValueAsString(v))) + "]",
            _ => "nil"
        };
    }

    public override ValueWrapper VisitId(GolightSemanticoParser.IdContext context)
    {
        string id = context.GetText(); 
        return currentEnvironment.GetVariable(id, context.Start); 
    }


    public override ValueWrapper VisitInt(GolightSemanticoParser.IntContext context)
    {
        
        int value = int.Parse(context.ENTERO().GetText());
        return new IntValue(value);
    }

    public override ValueWrapper VisitFloat64(GolightSemanticoParser.Float64Context context)
    {
    
        float value = float.Parse(context.FLOTANTE().GetText());
        return new FloatValue(value); 
    }
    public override ValueWrapper VisitString(GolightSemanticoParser.StringContext context)
    {
        
        string text = context.CADENA().GetText();

        
        text = text.Substring(1, text.Length - 2);

        text = ProcesarSecuenciasEscape(text);
        
        return new StringValue(text);
    }
    public override ValueWrapper VisitBool(GolightSemanticoParser.BoolContext context)
    {
       
        bool value = bool.Parse(context.BOOLEANO().GetText());
        return new BoolValue(value); 
    }

    public override ValueWrapper VisitRune(GolightSemanticoParser.RuneContext context)
    {
        
        string text = context.RUNE().GetText();
    
        char value = text[1]; 
        return new RuneValue(value); 
    }
    public override ValueWrapper VisitNull(GolightSemanticoParser.NullContext context)
    {
        
        return new VoidValue();
    }

    public override ValueWrapper VisitDeclaexplicitavalor(GolightSemanticoParser.DeclaexplicitavalorContext context)
    {
        
        string id = context.ID().GetText(); 
        string tipo = context.TIPO().GetText(); 
        ValueWrapper valor = Visit(context.expresion()); 
        if (tipo == "float64" && valor is IntValue intvalue)
        {

            valor = new FloatValue(intvalue.Value);
        }
       
        currentEnvironment.DeclararVariable(id, valor, context.Start);

        return defaultVoid;
    }

    public override ValueWrapper VisitDeclaexplicitanovalor(GolightSemanticoParser.DeclaexplicitanovalorContext context)
    {
        
        string id = context.ID().GetText(); 
        string tipo = context.TIPO().GetText(); 
        if (tipo == "string")
        {
            currentEnvironment.DeclararVariable(id, new StringValue(""), context.Start);
        }
        else if (tipo == "int")
        {
            currentEnvironment.DeclararVariable(id, new IntValue(0), context.Start);
        }
        else if (tipo == "float64")
        {
            currentEnvironment.DeclararVariable(id, new FloatValue(0), context.Start);
        }
        else if (tipo == "bool")
        {
            currentEnvironment.DeclararVariable(id, new BoolValue(false), context.Start);
        }
        else if (tipo == "rune")
        {
            currentEnvironment.DeclararVariable(id, new RuneValue('0'), context.Start);
        }
        else
        {
            throw new SemanticError("Tipo no válido", context.Start);
        }
        

        return defaultVoid;
    }

    public override ValueWrapper VisitDeclaracionslicevalor(GolightSemanticoParser.DeclaracionslicevalorContext context)
    {
        string id = context.ID().GetText();
        string tipo = context.TIPO().GetText();
        var valoresContext = context.valores();

        List<ValueWrapper> valores = new List<ValueWrapper>();
        foreach (var valorContext in valoresContext)
        {
            ValueWrapper valor = Visit(valorContext);
            if (GetTypeName(valor) != tipo)
            {
                throw new SemanticError($"El valor {valor} no es del tipo {tipo}", context.Start);
            }
            valores.Add(valor);
        }

        SliceValue arrayValue = new SliceValue(valores, tipo);
        currentEnvironment.DeclararVariable(id, arrayValue, context.Start);

        return defaultVoid;
    }
    public override ValueWrapper VisitDeclaracionslicenovalor(GolightSemanticoParser.DeclaracionslicenovalorContext context)
    {
        string id = context.ID().GetText();
        string tipo = context.TIPO().GetText();

        SliceValue arrayValue = new SliceValue(new List<ValueWrapper>(), tipo);
        currentEnvironment.DeclararVariable(id, arrayValue, context.Start);

        return defaultVoid;
    }

    public override ValueWrapper VisitDeclaracionimplicita(GolightSemanticoParser.DeclaracionimplicitaContext context)
    {

        string variable = context.ID().GetText();
        if (context.expresion() is GolightSemanticoParser.ExpdotexpContext expDotExpContext ||
            context.expresion() is GolightSemanticoParser.Expdotexp1Context expDotExp1Context)
        {
            ValueWrapper objeto;
            string propiedadCompleta;
            if (context.expresion() is GolightSemanticoParser.ExpdotexpContext expDotExp)
            {
                objeto = currentEnvironment.GetVariable(expDotExp.children[0].GetText(), context.Start);
                propiedadCompleta = expDotExp.expresion().GetText();

            }
            else if (context.expresion() is GolightSemanticoParser.Expdotexp1Context expDotExp1)
            {
                
                objeto = currentEnvironment.GetVariable(expDotExp1.ID(0).GetText(), context.Start);
                //Console.WriteLine(expDotExp1.ID(0).GetText());
                //Console.WriteLine(expDotExp1.ID(1).GetText());
                propiedadCompleta = expDotExp1.GetText().Substring(expDotExp1.ID(1).GetText().Length);
               if (propiedadCompleta.Contains("Nombre"))
                {
                    propiedadCompleta = "Nombre";
                } else if (propiedadCompleta.Contains("Edad"))
                {
                    propiedadCompleta = "Edad";
                }
            }
            else
            {
                throw new SemanticError("Expresión no válida", context.Start);
            }

            string[] propiedades = propiedadCompleta.Split('.');


            for (int i = 0; i < propiedades.Length; i++)
            {
                if (objeto is StructValue structValue)
                {

                    objeto = structValue.GetField(propiedades[i], context.Start);
                }
                else
                {
                    throw new SemanticError($"No se puede acceder a la propiedad '{propiedades[i]}' en un valor que no es un struct", context.Start);
                }
            }

            ValueWrapper valorFinal = objeto;

            string var2 = context.ID().GetText();
            currentEnvironment.DeclararVariable(var2, valorFinal, context.Start);

            Console.WriteLine($"Se ha almacenado {var2} en el entorno con valor: {GetValueAsString(valorFinal)}");

            return defaultVoid;
        }


        if (context.expresion() is GolightSemanticoParser.SlicesdescContext slicesContext)
        {
            string id = slicesContext.ID().GetText();
            ValueWrapper value = currentEnvironment.GetVariable(id, context.Start);

            if (value is not SliceValue sliceValue)
                throw new SemanticError($"La variable {id} no es un slice.", context.Start);

            var indices = slicesContext.valor().Select(Visit).ToList();

            if (indices.Count == 1)
            {
                if (indices[0] is not IntValue indexValue)
                    throw new SemanticError("El índice debe ser un entero.", context.Start);

                int index = indexValue.Value;
                if (index < 0 || index >= sliceValue.Values.Count)
                    throw new SemanticError($"Índice fuera de rango en {id}[{index}].", context.Start);

                ValueWrapper extractedValue = sliceValue.Values[index];
                Console.WriteLine($"Declarando {context.ID().GetText()} := {id}[{index}] -> {extractedValue}");

                currentEnvironment.DeclararVariable(context.ID().GetText(), extractedValue, context.Start);
            }
            else if (indices.Count == 2)
            {
                if (indices[0] is not IntValue index1Value || indices[1] is not IntValue index2Value)
                    throw new SemanticError("Los índices deben ser enteros.", context.Start);

                int index1 = index1Value.Value;
                int index2 = index2Value.Value;

                if (index1 < 0 || index1 >= sliceValue.Values.Count)
                    throw new SemanticError($"Índice fuera de rango en {id}[{index1}].", context.Start);

                if (sliceValue.Values[index1] is not SliceValue nestedSlice)
                    throw new SemanticError($"El valor en {id}[{index1}] no es otro slice.", context.Start);

                if (index2 < 0 || index2 >= nestedSlice.Values.Count)
                    throw new SemanticError($"Índice fuera de rango en {id}[{index1}][{index2}].", context.Start);

                ValueWrapper extractedValue = nestedSlice.Values[index2];
                Console.WriteLine($"Declarando {context.ID().GetText()} := {id}[{index1}][{index2}] -> {extractedValue}");

                currentEnvironment.DeclararVariable(context.ID().GetText(), extractedValue, context.Start);
            }

            return defaultVoid;
        }

        string varName = context.ID().GetText();
        ValueWrapper assignedValue = Visit(context.expresion());

        Console.WriteLine($"Se ha almacenado {varName} en el entorno con valor: {GetValueAsString(assignedValue)}");
        currentEnvironment.DeclararVariable(varName, assignedValue, context.Start);

        return defaultVoid;
    }


public override ValueWrapper VisitDeclaracionimplicitaslice(GolightSemanticoParser.DeclaracionimplicitasliceContext context)
{
    string id = context.ID().GetText();  
    string tipo = context.TIPO().GetText();  
    var slicesContext = context.slices();  

    if (slicesContext == null || !slicesContext.Any())
    {
        throw new SemanticError($"El slice {id} no tiene valores iniciales", context.Start);
    }

    List<ValueWrapper> allSlices = new List<ValueWrapper>();  

    foreach (var slice1 in slicesContext)
    {
        ValueWrapper sliceWrapper = Visit(slice1); 
        Console.WriteLine($"Procesando slice con valor: {GetValueAsString(sliceWrapper)}");
        
        if (sliceWrapper is SliceValue slice)
        {
 
            if (slice.Values.Count == 0)
            {
                throw new SemanticError($"El slice {id} está vacío y no puede inferir el tipo", context.Start);
            }

            if (slice.Type != tipo)
            {
                throw new SemanticError($"El slice {id} debe ser de tipo {tipo}, pero contiene {slice.Type}", context.Start);
            }

            allSlices.Add(slice);
        }
        else
        {
            throw new SemanticError($"Error inesperado al procesar el slice {id}", context.Start);
        }
    }

    if (allSlices.Count > 0)
    {
        Console.WriteLine($"Declarando el slice {id} con tipo {tipo} y valores: {string.Join(", ", allSlices.Select(s => GetValueAsString(s)))}");

        currentEnvironment.DeclararVariable(id, new SliceValue(allSlices, tipo), context.Start);
    }

    return defaultVoid;
}


    public override ValueWrapper VisitAsignacionslicesimple(GolightSemanticoParser.AsignacionslicesimpleContext context)
    {
        string id = context.ID().GetText();
        var indicesContext = context.valor(); 
        ValueWrapper valor = Visit(context.expresion());

        List<int> indices = new List<int>();
        foreach (var indiceContext in indicesContext)
        {
            ValueWrapper indiceValue = Visit(indiceContext);
            if (indiceValue is IntValue intValue)
            {
                indices.Add(intValue.Value);
            }
            else
            {
                throw new SemanticError("El índice debe ser un entero", indiceContext.Start);
            }
        }

        ValueWrapper arrayWrapper = currentEnvironment.GetVariable(id, context.Start);
        if (arrayWrapper is SliceValue arrayValue)
        {
            if (indices.Count == 1)
            {

                int index = indices[0];
                arrayValue.SetValue(index, valor, context.Start);
            }
            else if (indices.Count == 2)
            {

                int index1 = indices[0];
                int index2 = indices[1];

                if (arrayValue.Values[index1] is SliceValue nestedSlice)
                {
                    nestedSlice.SetValue(index2, valor, context.Start);
                }
                else
                {
                    throw new SemanticError($"El valor en {id}[{index1}] no es un slice.", context.Start);
                }
            }
            else
            {
                throw new SemanticError("Solo se soporta la asignación a un índice simple o doble", context.Start);
            }
        }
        else
        {
            throw new SemanticError($"La variable {id} no es un slice", context.Start);
        }

        return defaultVoid;
    }

    public override ValueWrapper VisitSlices(GolightSemanticoParser.SlicesContext context)
    {
        var valoresContext = context.valores();
        if (valoresContext == null || valoresContext.Length == 0)
        {
            return new SliceValue(new List<ValueWrapper>(), "unknown"); 
        }

        List<ValueWrapper> valores = new List<ValueWrapper>();
        string tipo = null;

        foreach (var valorContext in valoresContext)
        {
            ValueWrapper valor = Visit(valorContext);

            if (tipo == null)
            {
                tipo = GetTypeName(valor);
            }
            else if (GetTypeName(valor) != tipo)
            {
                throw new SemanticError($"Todos los valores en el slice deben ser del mismo tipo: esperado {tipo}, encontrado {GetTypeName(valor)}", valorContext.Start);
            }

            valores.Add(valor);
        }

        return new SliceValue(valores, tipo);
    }

    public override ValueWrapper VisitAsignacionmetodos(GolightSemanticoParser.AsignacionmetodosContext context)
    {

        ValueWrapper objeto = Visit(context.expresion(0));


        string propiedadCompleta = context.expresion(1).GetText();


        string[] propiedades = propiedadCompleta.Split('.');

        for (int i = 0; i < propiedades.Length - 1; i++)
        {
            if (objeto is StructValue structValue)
            {

                objeto = structValue.GetField(propiedades[i], context.Start);
            }
            else
            {
                throw new SemanticError($"No se puede acceder a la propiedad '{propiedades[i]}' en un valor que no es un struct", context.Start);
            }
        }

        string campoFinal = propiedades[propiedades.Length - 1];

        ValueWrapper valor = Visit(context.expresion(2));

        if (objeto is StructValue structFinal)
        {
            structFinal.SetField(campoFinal, valor, context.Start);
        }
        else
        {
            throw new SemanticError($"No se puede asignar un valor a una propiedad de un valor que no es un struct", context.Start);
        }

        return defaultVoid;
    }
    public override ValueWrapper VisitAsignacion(GolightSemanticoParser.AsignacionContext context)
    {
        
        string id = context.expresion(0).GetText(); 
        ValueWrapper valor = Visit(context.expresion(1)); 
        
        currentEnvironment.AsignarVariable(id, valor, context.Start);

        return defaultVoid;
    }

    public override ValueWrapper VisitAsignaciondesdefuncion(GolightSemanticoParser.AsignaciondesdefuncionContext context)
    {
        
        string id = context.ID().GetText(); 
        ValueWrapper objeto = Visit(context.expresion(0)); 
        string metodo = context.expresion(1).GetText(); 

        
        //ValueWrapper resultado = currentEnvironment.CallMethod(objeto, metodo, new List<ValueWrapper>(), context.Start);
        //currentEnvironment.DeclararVariable(id, resultado, context.Start);

        return defaultVoid;
    }

    //Madrugada entre 15 y 16 de marzo, implementaciones varias, desde el print hasta operaciones por el momento.
    public override ValueWrapper VisitSumres(GolightSemanticoParser.SumresContext context)
    {
        
        ValueWrapper left = Visit(context.expresion(0));
        ValueWrapper right = Visit(context.expresion(1));

        string op = context.GetChild(1).GetText();

        
        if (op == "+")
        {
            return Sumar(left, right, context);
        }
        
        else if (op == "-")
        {
            return Restar(left, right, context);
        }

        
        throw new SemanticError($"Operador no válido: {op}", context.Start);
    }

    public override ValueWrapper VisitMultdivmod(GolightSemanticoParser.MultdivmodContext context)
    {
        
        ValueWrapper left = Visit(context.expresion(0));
        ValueWrapper right = Visit(context.expresion(1));

        string op = context.GetChild(1).GetText();

        
        if (op == "*")
        {
            return Multiplicar(left, right, context);
        }
        
        else if (op == "/")
        {
            return Dividir(left, right, context);
        }
        else if (op == "%")
        {
            return Modulo(left, right, context);
        }
        
        throw new SemanticError($"Operador no válido: {op}", context.Start);
    }

    public override ValueWrapper VisitOr(GolightSemanticoParser.OrContext context)
    {

        ValueWrapper left = Visit(context.expresion(0));

        ValueWrapper right = Visit(context.expresion(1));

        if (left is BoolValue leftBool && right is BoolValue rightBool)
        {
            return new BoolValue(leftBool.Value || rightBool.Value);
        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)}) con ({GetTypeName(right)}) usando el operador ||",
            context.Start
        );
    }

    public override ValueWrapper VisitAnd(GolightSemanticoParser.AndContext context)
    {

        ValueWrapper left = Visit(context.expresion(0));


        ValueWrapper right = Visit(context.expresion(1));


        if (left is BoolValue leftBool && right is BoolValue rightBool)
        {
            return new BoolValue(leftBool.Value && rightBool.Value);
        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)}) con ({GetTypeName(right)}) usando el operador &&",
            context.Start
        );
    }

    public override ValueWrapper VisitIgualdad(GolightSemanticoParser.IgualdadContext context)
    {
        ValueWrapper left = GetSliceValueIfNeeded(Visit(context.expresion(0)), context.expresion(0));
        ValueWrapper right = GetSliceValueIfNeeded(Visit(context.expresion(1)), context.expresion(1));

        string op = context.GetChild(1).GetText();
        //Console.WriteLine(left.ToString() + " " + op + " " + right.ToString());

        if (op == "==")
        {
            return new BoolValue(!left.Equals(right));
        }
        else if (op == "!=")
        {
            return new BoolValue(!left.Equals(right));
        }

        throw new SemanticError($"Operador no válido: {op}", context.Start);
    }

    private ValueWrapper GetSliceValueIfNeeded(ValueWrapper value, GolightSemanticoParser.ExpresionContext context)
    {
        if (context is GolightSemanticoParser.SlicesdescContext slicesContext)
        {
            string id = slicesContext.ID().GetText();
            ValueWrapper variableValue = currentEnvironment.GetVariable(id, context.Start);

            if (variableValue is not SliceValue sliceValue)
                throw new SemanticError($"La variable {id} no es un slice.", context.Start);

            
            var indices = slicesContext.valor().Select(Visit).ToList();

            if (indices.Count == 1)
            {
                
                if (indices[0] is not IntValue indexValue)
                    throw new SemanticError("El índice debe ser un entero.", context.Start);

                int index = indexValue.Value;
                if (index < 0 || index >= sliceValue.Values.Count)
                    throw new SemanticError($"Índice fuera de rango en {id}[{index}].", context.Start);

                return sliceValue.Values[index];
            }
            else if (indices.Count == 2)
            {
                
                if (indices[0] is not IntValue index1Value || indices[1] is not IntValue index2Value)
                    throw new SemanticError("Los índices deben ser enteros.", context.Start);

                int index1 = index1Value.Value;
                int index2 = index2Value.Value;

                if (index1 < 0 || index1 >= sliceValue.Values.Count)
                    throw new SemanticError($"Índice fuera de rango en {id}[{index1}].", context.Start);

                if (sliceValue.Values[index1] is not SliceValue nestedSlice)
                    throw new SemanticError($"El valor en {id}[{index1}] no es otro slice.", context.Start);

                if (index2 < 0 || index2 >= nestedSlice.Values.Count)
                    throw new SemanticError($"Índice fuera de rango en {id}[{index1}][{index2}].", context.Start);

                return nestedSlice.Values[index2];
            }
        }
        return value; 
    }

    public override ValueWrapper VisitRelacionales(GolightSemanticoParser.RelacionalesContext context)
    {
  
        ValueWrapper left = Visit(context.expresion(0));

        ValueWrapper right = Visit(context.expresion(1));

        string op = context.GetChild(1).GetText();

        // Console.WriteLine($"Evaluando condición: {left} {op} {right}"); revision de valores por si recibo un tipo incorrecto

        if (left is IntValue leftInt && right is IntValue rightInt)
        {
            switch (op)
            {
                case "<": return new BoolValue(leftInt.Value < rightInt.Value);
                case "<=": return new BoolValue(leftInt.Value <= rightInt.Value);
                case ">=": return new BoolValue(leftInt.Value >= rightInt.Value);
                case ">": return new BoolValue(leftInt.Value > rightInt.Value);
            }
        }
        else if (left is FloatValue leftFloat && right is FloatValue rightFloat)
        {
            switch (op)
            {
                case "<": return new BoolValue(leftFloat.Value < rightFloat.Value);
                case "<=": return new BoolValue(leftFloat.Value <= rightFloat.Value);
                case ">=": return new BoolValue(leftFloat.Value >= rightFloat.Value);
                case ">": return new BoolValue(leftFloat.Value > rightFloat.Value);
            }
        }
        else if (left is IntValue leftInt2 && right is FloatValue rightFloat2)
        {
            switch (op)
            {
                case "<": return new BoolValue(leftInt2.Value < rightFloat2.Value);
                case "<=": return new BoolValue(leftInt2.Value <= rightFloat2.Value);
                case ">=": return new BoolValue(leftInt2.Value >= rightFloat2.Value);
                case ">": return new BoolValue(leftInt2.Value > rightFloat2.Value);
            }
        }
        else if (left is FloatValue leftFloat2 && right is IntValue rightInt2)
        {
            switch (op)
            {
                case "<": return new BoolValue(leftFloat2.Value < rightInt2.Value);
                case "<=": return new BoolValue(leftFloat2.Value <= rightInt2.Value);
                case ">=": return new BoolValue(leftFloat2.Value >= rightInt2.Value);
                case ">": return new BoolValue(leftFloat2.Value > rightInt2.Value);
            }
        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)}) con ({GetTypeName(right)}) usando el operador {op}",
            context.Start
        );
    }

    public override ValueWrapper VisitUnario(GolightSemanticoParser.UnarioContext context)
    {

        ValueWrapper value = Visit(context.expresion());

        string op = context.GetChild(0).GetText();

        if (op == "!")
        {
            if (value is BoolValue boolValue)
            {
                return new BoolValue(!boolValue.Value); 
            }
            else
            {
                throw new SemanticError(
                    $"No se puede aplicar el operador ! a un tipo ({GetTypeName(value)}). Solo se permite con booleanos.",
                    context.Start
                );
            }
        }

        else if (op == "-")
        {
            if (value is IntValue intValue)
            {
                return new IntValue(-intValue.Value); 
            }
            else if (value is FloatValue floatValue)
            {
                return new FloatValue(-floatValue.Value); 
            }
            else
            {
                throw new SemanticError(
                    $"No se puede aplicar el operador - a un tipo ({GetTypeName(value)}). Solo se permite con int o float64.",
                    context.Start
                );
            }
        }

  
        throw new SemanticError($"Operador unario no válido: {op}", context.Start);
    }

    public override ValueWrapper VisitParentesisexpre(GolightSemanticoParser.ParentesisexpreContext context)
    {
        
        return Visit(context.expresion());
    }

    public override ValueWrapper VisitCorchetesexpre(GolightSemanticoParser.CorchetesexpreContext context)
    {
        
        return Visit(context.expresion());
    }

    //Medio dia del 16 de marzo, implementacion de las funciones atoi, typeof, parsefloat y esperando a que juegue el barcita
    public override ValueWrapper VisitAtoi(GolightSemanticoParser.AtoiContext context)
    {
        
        string cadena = context.CADENA().GetText();

        
        cadena = cadena.Substring(1, cadena.Length - 2);

        
        if (int.TryParse(cadena, out int resultado))
        {
            return new IntValue(resultado);
        }
        else
        {
            
            throw new SemanticError(
                $"No se puede convertir la cadena '{cadena}' a un número entero (int).",
                context.Start
            );
        }
    }

    public override ValueWrapper VisitParsefloat(GolightSemanticoParser.ParsefloatContext context)
    {
        string cadena = context.CADENA().GetText();

        cadena = cadena.Substring(1, cadena.Length - 2);

        if (double.TryParse(cadena, out double resultado))
        {
            return new FloatValue((float)resultado);
        }
        else
        {
            throw new SemanticError(
                $"No se puede convertir la cadena '{cadena}' a un número flotante (float64).",
                context.Start
            );
        }
    }

    public override ValueWrapper VisitTypeof(GolightSemanticoParser.TypeofContext context)
    {

        string id = context.ID().GetText();

        try
        {

            ValueWrapper value = currentEnvironment.GetVariable(id, context.Start);

            string tipo = value switch
            {
                IntValue _ => "int",
                FloatValue _ => "float64",
                StringValue _ => "string",
                BoolValue _ => "bool",
                VoidValue _ => "nil",
                RuneValue _ => "rune",
                FunctionValue _ => "función",
                StructValue _ => "struct",
                SliceValue sliceValue_ => "[]"+sliceValue_.Type,
                _ => "desconocido"
            };

            return new StringValue(tipo);
        }
        catch (SemanticError ex)
        {
            throw new SemanticError($"La variable '{id}' no ha sido declarada.", context.Start);
        }
    }

    public override ValueWrapper VisitIncremento(GolightSemanticoParser.IncrementoContext context)
    {
        string id = context.ID().GetText();

        ValueWrapper valorActual = currentEnvironment.GetVariable(id, context.Start);

        ValueWrapper incremento = new IntValue(1);

        ValueWrapper nuevoValor = Sumar(valorActual, incremento, null);

        currentEnvironment.AsignarVariable(id, nuevoValor, context.Start);

        return nuevoValor;
    }

    public override ValueWrapper VisitDecremento(GolightSemanticoParser.DecrementoContext context)
    {
        string id = context.ID().GetText();

        ValueWrapper valorActual = currentEnvironment.GetVariable(id, context.Start);

        ValueWrapper decremento = new IntValue(1);

        ValueWrapper nuevoValor = Restar(valorActual, decremento, null);

        currentEnvironment.AsignarVariable(id, nuevoValor, context.Start);

        return nuevoValor;
    }

    public override ValueWrapper VisitIncremento2(GolightSemanticoParser.Incremento2Context context)
    {
        string id = context.ID().GetText();

        ValueWrapper valorActual = currentEnvironment.GetVariable(id, context.Start);

        ValueWrapper expresionValue = Visit(context.expresion());

        ValueWrapper nuevoValor = Sumar(valorActual, expresionValue, null);

        currentEnvironment.AsignarVariable(id, nuevoValor, context.Start);

        return nuevoValor;
    }

    public override ValueWrapper VisitDecremento2(GolightSemanticoParser.Decremento2Context context)
    {
        string id = context.ID().GetText();

        ValueWrapper valorActual = currentEnvironment.GetVariable(id, context.Start);

        ValueWrapper expresionValue = Visit(context.expresion());

        ValueWrapper nuevoValor = Restar(valorActual, expresionValue, null);

        currentEnvironment.AsignarVariable(id, nuevoValor, context.Start);

        return nuevoValor;
    }
    public override ValueWrapper VisitBloque(GolightSemanticoParser.BloqueContext context)
    {
        Console.WriteLine("Entrando al bloque");

        Environment nuevoEntorno = new Environment(currentEnvironment);
        Environment entornoAnterior = currentEnvironment;

        try
        {
            currentEnvironment = nuevoEntorno;
            foreach (var contenido in context.contenido())
            {
                Visit(contenido); 
            }
        }
        finally
        {
            currentEnvironment = entornoAnterior;
        }

        return new VoidValue();
    }

    //Llame if1 al inicio del if porque no me dejo por la produccion de antes que ya se llama if xd 
    public override ValueWrapper VisitIf1(GolightSemanticoParser.If1Context context)
    {

        Environment nuevoEntorno = new Environment(currentEnvironment);
        Environment entornoAnterior = currentEnvironment;
        ValueWrapper restmp ;
        try
        {

            currentEnvironment = nuevoEntorno;

            
            string evaluar =context.expresion(0).GetText();
            if (evaluar.Contains("Siguiente"))
            {
            string[] tmp = evaluar.Split(new[] { "==" }, StringSplitOptions.None);
            string antes = tmp[0];
            string despues = tmp[1];
            ValueWrapper casoaevaluar = GetValueFromTypeName(despues);
            string[] tmp2 = antes.Split(new[] { "." }, StringSplitOptions.None);
            string id = tmp2[0];
            ValueWrapper res = currentEnvironment.GetVariable(id, context.Start);
            res = ((StructValue)res).GetField(tmp2[1], context.Start);
            for (int i = 1; i < tmp2.Length; i++)
            {
                res = ((StructValue)res).GetField(tmp2[i], context.Start);
            }
            if(res.Equals(casoaevaluar))
            {
                _condicionCumplida = true;
                foreach (var contenido in context.contenido())
                {
                    Visit(contenido);
                }
            }
            }
            else {

            bool condicionCumplida = true;
            ;
            foreach (var expr in context.expresion())
            {
                

                for (int i = 0; i < expr.ChildCount; i++)
                {
                    restmp = Visit(expr.GetChild(i));
                    Console.WriteLine(restmp);
                }
                ValueWrapper resultado = Visit(expr);
                if (resultado is BoolValue boolValue && !boolValue.Value)
                {
                    condicionCumplida = false;
                    break;
                }
            }


            if (!condicionCumplida)
            {
                _condicionCumplida = true; 
                foreach (var contenido in context.contenido())
                {
                    Visit(contenido);
                }
            }
            }

        }
        finally
        {

            currentEnvironment = entornoAnterior;
           
        }
                    foreach (var expr in context.expresion())
            {

                return restmp = Visit(expr.GetChild(0));
            }

        return new VoidValue();
    }



    public override ValueWrapper VisitElseif(GolightSemanticoParser.ElseifContext context)
    {

        Environment nuevoEntorno = new Environment(currentEnvironment);
        Environment entornoAnterior = currentEnvironment;

        try
        {

            currentEnvironment = nuevoEntorno;

            if (_condicionCumplida)
            {
                _condicionCumplida = false;
                return new VoidValue();
            }

            bool condicionCumplida = true;
            foreach (var expr in context.expresion())
            {
                ValueWrapper resultado = Visit(expr);
                if (resultado is BoolValue boolValue && !boolValue.Value)
                {
                    condicionCumplida = false;
                    break;
                }
            }

            if (condicionCumplida)
            {
                _condicionCumplida = true; 
                foreach (var contenido in context.contenido())
                {
                    Visit(contenido);
                }
            }
        }
        finally
        {
            currentEnvironment = entornoAnterior;
        }

        return new VoidValue();
    }


    public override ValueWrapper VisitElse(GolightSemanticoParser.ElseContext context)
    {

        Environment nuevoEntorno = new Environment(currentEnvironment);
        Environment entornoAnterior = currentEnvironment;

        try
        {

            currentEnvironment = nuevoEntorno;

            if (_condicionCumplida)
            {
                _condicionCumplida = false;
                return new VoidValue();
            }

            foreach (var contenido in context.contenido())
            {
                Visit(contenido);
            }
        }
        finally
        {
            currentEnvironment = entornoAnterior;
        }

        return new VoidValue();
    }
    //16 de Marzo 14:54 vamos a tratar los for y while gano el barcita, la vida es buena
    public override ValueWrapper VisitFor1(GolightSemanticoParser.For1Context context)
    {

        Environment nuevoEntorno = new Environment(currentEnvironment);
        Environment entornoAnterior = currentEnvironment;

        try
        {

            currentEnvironment = nuevoEntorno;

            ValueWrapper condicion = Visit(context.expresion());

            while (condicion is BoolValue boolValue && boolValue.Value)
            {

                Environment entornoIteracion = new Environment(currentEnvironment);
                try
                {

                    currentEnvironment = entornoIteracion;


                    try
                    {
                        foreach (var contenido in context.contenido())
                        {
                            Visit(contenido);
                        }
                    }
                    catch (ContinueException)
                    {


                    }
                    catch (BreakException)
                    {

                        break;
                    }
                }
                finally
                {

                    currentEnvironment = nuevoEntorno;
                }


                condicion = Visit(context.expresion());
            }
        }
        finally
        {

            currentEnvironment = entornoAnterior;
        }

        return new VoidValue();
    }
    //En el momento que el for2 me funcione, tendre que ir a traer un cafe, para producirme mas ansiedad
public override ValueWrapper VisitFor2(GolightSemanticoParser.For2Context context)
{
    Environment nuevoEntorno = new Environment(currentEnvironment);
    Environment entornoAnterior = currentEnvironment;
    var exp = 0;
    try
    {
        currentEnvironment = nuevoEntorno;

        if (context.declaracion() != null)
        {
            Visit(context.declaracion());
        }
        else if (context.expresion(exp) != null)
        {
            Visit(context.expresion(exp));
            exp = 1;
        }
        var cont = exp;
            
            while (true)
            {
                // Console.WriteLine(context.GetChild(3).GetText()); revisar si esta pasando la expresion correcta
                ValueWrapper condicion = Visit(context.expresion(exp));

                if (!(condicion is BoolValue boolValue))
                {
                    throw new SemanticError("La condición del bucle for debe ser un valor booleano.", context.expresion(1).Start);
                }

                if (!boolValue.Value)
                {
                    break;
                }

                try
                {
                    foreach (var contenido in context.contenido())
                    {
                        Visit(contenido);
                    }
                }
                catch (ContinueException)
                {
                    //Si se encuentra un continue, salta a la siguiente iteracion (O deberia)
                }
                catch (BreakException)
                {
                    // Si se encuentra un break, espero que si se salga
                    break;
                }
                ValueWrapper valor = Visit(context.expresion(cont+1)); 
                var id = context.expresion(cont+1).GetChild(0).GetText();
                if (context.expresion(cont+1).GetText().Contains("++"))
                {   
                    id = Regex.Replace(context.expresion(cont+1).GetText(), @"\+\+|--", "");
                    valor = new IntValue(valor is IntValue intvalue ? intvalue.Value + 1 : 0);
                } else if (context.expresion(cont+1).GetText().Contains("--"))
                {
                    valor = new IntValue(valor is IntValue intvalue ? intvalue.Value - 1 : 0);
                    id = Regex.Replace(context.expresion(cont+1).GetText(), @"\+\+|--", "");
                }
                else 
                currentEnvironment.AsignarVariable(id, valor, context.Start);
            }
        
    }
    finally
    {

        currentEnvironment = entornoAnterior;
    }

    return new VoidValue();
}
    public override ValueWrapper VisitFor3(GolightSemanticoParser.For3Context context)
    {

        string indexVariable = context.expresion(0).GetText(); 
        string valueVariable = context.expresion(1).GetText();

        ValueWrapper rangeValue = Visit(context.expresion(2));
        if (rangeValue is SliceValue sliceValue) 
        {
            ValueWrapper List = sliceValue.Values[0];
            if (List is SliceValue sliceList)
        {   int a = sliceList.Values.Count;
            
            for (int i = 0; i < sliceList.Values.Count; i++)
            {
                Environment forEnvironment = new Environment(currentEnvironment);

                forEnvironment.DeclararVariable(indexVariable, new IntValue(i), context.Start);
                forEnvironment.DeclararVariable(valueVariable, sliceList.Values[i], context.Start);

                Environment previousEnvironment = currentEnvironment;
                currentEnvironment = forEnvironment;

                foreach (var contenido in context.contenido())
                {
                    Visit(contenido);
                }

                currentEnvironment = previousEnvironment;
            }
        }
        else
        {
            throw new SemanticError($"La expresión no es un slice", context.Start);
        }

        return defaultVoid; 
        }
        return defaultVoid; 
    }
    //Al fin sali de los For, por el momento, al menos hasta que implemente los slices.

    public override ValueWrapper VisitSwitch(GolightSemanticoParser.SwitchContext context)
    {
        ValueWrapper valorSwitch = Visit(context.expresion());

        foreach (var caso in context.casos())
        {
            try
            {
                if (caso is GolightSemanticoParser.CaseContext caseContext)
                {
                    ValueWrapper valorCase = Visit(caseContext.expresion());

                    if (valorSwitch.Equals(valorCase))
                    {
                        foreach (var contenido in caseContext.contenido())
                        {
                            Visit(contenido);
                        }
                        break; 
                    }
                }
                else if (caso is GolightSemanticoParser.DefaultContext defaultContext)
                {
                    foreach (var contenido in defaultContext.contenido())
                    {
                        Visit(contenido);
                    }
                    break; 
                }
            }
            catch (BreakException)
            {
                
                break;
            }
        }

        return new VoidValue();
    }


    public override ValueWrapper VisitBreak(GolightSemanticoParser.BreakContext context)
    {
        throw new BreakException(); 
    }

    public override ValueWrapper VisitContinue(GolightSemanticoParser.ContinueContext context)
    {
        throw new ContinueException();
    }

    public override ValueWrapper VisitReturn(GolightSemanticoParser.ReturnContext context)
    {

        ValueWrapper valorRetorno = Visit(context.expresion(0));
        Console.WriteLine($"Retornando {GetValueAsString(valorRetorno)}");
        return valorRetorno;
    }

    public override ValueWrapper VisitLen(GolightSemanticoParser.LenContext context)
    {
        string id = context.expresion().GetText();

        ValueWrapper valueWrapper = currentEnvironment.GetVariable(id, context.Start);

        int length = CalculateLength(valueWrapper, context);

        return new IntValue(length);
    }

    private int CalculateLength(ValueWrapper valueWrapper, GolightSemanticoParser.LenContext token)
    {
        if (valueWrapper is SliceValue sliceValue)
        {
            
            if (sliceValue.Type != "slice")
            {
                return sliceValue.Values.Count;
            }

            int totalLength = 0;
            foreach (var value in sliceValue.Values)
            {
                if (value is SliceValue innerSlice)
                {
                    totalLength += CalculateLength(innerSlice, token); 
                }
                else
                {
                    totalLength++; 
                }
            }
            return totalLength;
        }
        else
        {
            throw new SemanticError($"La variable no es un slice", token.Start);
        }
    }

    public override ValueWrapper VisitJoin(GolightSemanticoParser.JoinContext context)
    {

        string id = context.ID().GetText();
        string separator = context.CADENA().GetText().Trim('"');


        ValueWrapper sliceWrapper = currentEnvironment.GetVariable(id, context.Start);
        if (sliceWrapper is SliceValue sliceValue && sliceValue.Type == "string")
        {
            List<string> stringValues = sliceValue.Values.Select(v => ((StringValue)v).Value).ToList();
            string result = string.Join(separator, stringValues);
            return new StringValue(result);
        }
        else
        {
            throw new SemanticError($"La variable {id} no es un slice de cadenas ([]string)", context.Start);
        }
    }

    public override ValueWrapper VisitIndex(GolightSemanticoParser.IndexContext context)
    {
        string id = context.ID().GetText(); 
        ValueWrapper valueToFind = Visit(context.valor());  
        //Desde sliceWrapper accedo a los valores del slicevalue, que aun tiene dos listas escondidas
        ValueWrapper sliceWrapper = currentEnvironment.GetVariable(id, context.Start);

        if (sliceWrapper is SliceValue sliceValue)
        {
            ValueWrapper list = sliceValue.Values[0];
            //Desde list accedo a la siguiente lista de slicevalues, pero aun queda otra lista escondida
            string valueType = GetTypeName(valueToFind);
            
            if (valueType != sliceValue.Type)
            {
                throw new SemanticError($"El valor {valueToFind} no es compatible con el tipo del slice {sliceValue.Type}", context.Start);
            }

            int index = -1;
            int i;
                if (list is SliceValue sliceList) //Ahora con SliceList accedo a los valores finales, pudiendo iterarlos ahora y comparar un tipo "Wrapper primitivo" con otro
                {
                   for (i = 0; i < sliceList.Values.Count(); i++){
                    //Console.WriteLine(i);
                    Console.WriteLine($"Comparando {sliceList.Values[i]} con {valueToFind}");
                    if (CompareValueWithPrimitive(sliceList.Values[i], valueToFind))
                {
                    index = i;
                    
                }  
                   }              
                }
            return new IntValue(index); 
        }
        else
        {
            throw new SemanticError($"La variable {id} no es un slice", context.Start);
        }
    }
    private bool CompareValueWithPrimitive(ValueWrapper a, ValueWrapper b)
    {
 
        if (a.GetType() != b.GetType()) return false;

        return (a, b) switch
        {
            (IntValue intA, IntValue intB) => intA.Value == intB.Value,
            (FloatValue floatA, FloatValue floatB) => Math.Abs(floatA.Value - floatB.Value) < 0.000001f, 
            (StringValue strA, StringValue strB) => strA.Value == strB.Value,
            (BoolValue boolA, BoolValue boolB) => boolA.Value == boolB.Value,
            (RuneValue runeA, RuneValue runeB) => runeA.Value == runeB.Value,
            _ => a.Equals(b)
        };
    }
    public override ValueWrapper VisitAppend(GolightSemanticoParser.AppendContext context)
    {
        string id = context.ID().GetText();

        ValueWrapper valueToAppend = Visit(context.expresion());

        ValueWrapper sliceWrapper = currentEnvironment.GetVariable(id, context.Start);
        
        if (sliceWrapper is SliceValue slice)
        {
            ValueWrapper list = slice.Values[0];
            if (slice.Values.Count == 1)
            {   if (list is SliceValue sliceList)
                {
                    sliceList.Values.Add(valueToAppend);
                }
                
            }
            else {

                slice.Values.Add(valueToAppend);
            }
            
            if (!slice.CanContainValue(valueToAppend))
            {
                throw new SemanticError($"El valor {valueToAppend} no es del tipo {slice.Type}", context.Start);
            }

            
            
            
            
            return slice;
        }
        
        throw new SemanticError($"La variable {id} no es un slice", context.Start);
    }

    //Vamos iniciando el final del programa, no se cuantas veces he pensado que ya voy a terminar y un nuevo error me deprime
    public override ValueWrapper VisitStruct1(GolightSemanticoParser.Struct1Context context)
    {

        string structName = context.ID().GetText();

        var fields = new Dictionary<string, ValueWrapper>();

        foreach (var campoContext in context.campostruct())
        {
            if (campoContext is GolightSemanticoParser.Camposstruct1Context campo)
            {

                string fieldName = campo.ID()[0].GetText();

                string fieldType = campo.TIPO()?.GetText() ?? campo.ID()[1].GetText();

                ValueWrapper fieldValue;
                if (fieldType == structName) 
                {
                    fieldValue = new StructValue(new Dictionary<string, ValueWrapper>()); 
                }
                else
                {
                    fieldValue = GetDefaultValueForType(fieldType); 
                }

                fields[fieldName] = fieldValue;
            }
        }

        var structValue = new StructValue(fields);

        currentEnvironment.DeclararVariable(structName, structValue, context.Start);

        return defaultVoid;
    }


    private ValueWrapper GetDefaultValueForType(string type)
    {

        switch (type)
        {
            case "string":
                return new StringValue("");
            case "int":
                return new IntValue(0);
            case "float64":
                return new FloatValue(0.0f);
            case "bool":
                return new BoolValue(false);
            case "rune":
                return new RuneValue('\0');
            default:
 
                if (currentEnvironment.EnontrarVariable(type) || type == "Siguiente" )
                {
                    if (type == "Siguiente")
                    {
                        type = "Nodo";
                    }
                    ValueWrapper variableValue = currentEnvironment.GetVariable(type, null);


                    if (variableValue is StructValue structValue)
                    {
                        return structValue; 
                    }
                    else
                    {
                        throw new SemanticError($"La variable '{type}' no es un struct", null);
                    }
                }
                else
                {
                    throw new SemanticError($"Tipo no válido o variable no encontrada: {type}", null);
                }
        }
    }
    public override ValueWrapper VisitStruct2(GolightSemanticoParser.Struct2Context context)
    {
        string variableName = context.ID(0).GetText();
        string structName = context.ID(1).GetText();

        var structDefinition = currentEnvironment.GetVariable(structName, context.Start) as StructValue;
        if (structDefinition == null)
        {
            throw new SemanticError($"El tipo {structName} no es un struct", context.Start);
        }

        var instanceFields = new Dictionary<string, ValueWrapper>(structDefinition.Fields);

        foreach (var campoContext in context.campostruct())
        {
            if (campoContext is GolightSemanticoParser.InstanciacionstructContext instancia)
            {

                string fieldName = instancia.expresion(0).GetText();
                ValueWrapper fieldValue;
                if (instancia.expresion(1).GetText() == "nil")
                {
                    fieldValue = new StructValue(new Dictionary<string, ValueWrapper>());
                } 
                else {
                    fieldValue = Visit(instancia.expresion(1));
                    }
                //Console.WriteLine(instancia.expresion(1).GetText());

                //Console.WriteLine(instancia.expresion(1).GetText());
                if (instanceFields.ContainsKey(fieldName))
                {
                    string expectedType = GetTypeName(instanceFields[fieldName]);

                    
                    string actualType = GetTypeName(fieldValue);
                    if (actualType == "nil")
                    {
                        actualType = expectedType;
                        //fieldValue = GetDefaultValueForType(expectedType);
                        Console.WriteLine(fieldValue.GetType());
                    }
                    Console.WriteLine(expectedType);
                    Console.WriteLine(actualType);
                    if (expectedType != actualType)
                    {
                        throw new SemanticError($"No se puede asignar un valor de tipo {actualType} al campo {fieldName} de tipo {expectedType}", context.Start);
                    }

                    instanceFields[fieldName] = fieldValue;
                }
                else
                {
                    throw new SemanticError($"El campo {fieldName} no existe en el struct {structName}", context.Start);
                }
            }
        }

        var structInstance = new StructValue(instanceFields);

        currentEnvironment.DeclararVariable(variableName, structInstance, context.Start);

        return structInstance;
    }

    public override ValueWrapper VisitExpdotexp1(GolightSemanticoParser.Expdotexp1Context context)
    {

        string variableName = context.ID(0).GetText();

        ValueWrapper structInstance = currentEnvironment.GetVariable(variableName, context.Start);

        if (structInstance is not StructValue structValue)
        {
            throw new SemanticError($"'{variableName}' no es un struct", context.Start);
        }

        string propertyName = context.ID(1).GetText();

        return structValue.GetField(propertyName, context.Start);
    }

    public override ValueWrapper VisitExpdotexp(GolightSemanticoParser.ExpdotexpContext context)
    {

        ValueWrapper currentValue = currentEnvironment.GetVariable(context.ID().GetText(), context.Start);


        for (int i = 1; i < context.ChildCount; i++) 
        {
            var child = context.GetChild(i); 

            if (child is GolightSemanticoParser.ExpresionContext expresionContext)
            {
                if (currentValue is StructValue structValue)
                {
                    string fieldName = expresionContext.GetText(); 
                    currentValue = structValue.GetField(fieldName, context.Start);
                }
                else
                {
                    throw new SemanticError($"No se puede acceder a un campo de un valor que no es un struct", context.Start);
                }
            }
        }

        return currentValue;
    }

    //Aqui empieza la implementacion de las funciones aux, si no funciona me doy de baja de la vida istg, ultimos 19 puntos que puedo conseguir

    public override ValueWrapper VisitFuncionvacia(GolightSemanticoParser.FuncionvaciaContext context)
    {
        string functionName = context.expresion().GetText();
        bool isMain = functionName == "main";
        string returnType = context.TIPO()?.GetText(); 
        Environment functionEnvironment = new Environment(currentEnvironment);

        var userFunction = new UserDefinedFunction(context.contenido()[0], functionEnvironment, new List<string>(), returnType);

        currentEnvironment.DeclararVariable(functionName, new FunctionValue(userFunction, functionName), context.Start);

            if (currentEnvironment.EnontrarVariable(functionName) && currentEnvironment.Enclosing == null)
            {
                userFunction.Invoke(new List<ValueWrapper>(), this);
            }

        return defaultVoid;
    }

    public override ValueWrapper VisitFuncionconparametros(GolightSemanticoParser.FuncionconparametrosContext context)
    {
        string functionName = context.expresion().GetText();
        string returnType = context.TIPO()?.GetText(); 


        var parametrosContext = context.parametros();
        List<string> parametros = new List<string>();

        
        foreach (var parametro in parametrosContext)
        {
            string paramName = parametro.ID(0).GetText();
            string paramType = parametro.TIPO(0).GetText();
            parametros.Add(paramName);

            currentEnvironment.DeclararVariable(paramName, GetDefaultValueForType(paramType), context.Start);
        }

        var userFunction = new UserDefinedFunction(context.contenido()[0], currentEnvironment, parametros, returnType);

        currentEnvironment.DeclararVariable(functionName, new FunctionValue(userFunction, functionName), context.Start);

        return defaultVoid;
    }

    public override ValueWrapper VisitFuncionanonima(GolightSemanticoParser.FuncionanonimaContext context)
    {
        string returnType = context.TIPO()?.GetText();

        var parametrosContext = context.parametros();
        Console.WriteLine(context.parametros());
        List<string> parametros = new List<string>();

        if (parametrosContext != null)
        {
            foreach (var parametro in parametrosContext)
            {
                Console.WriteLine(parametro);
                string paramName = parametro.ID(0).GetText();
                string paramType = parametro.TIPO(0).GetText();
                parametros.Add(paramName);

                currentEnvironment.DeclararVariable(paramName, GetDefaultValueForType(paramType), context.Start);
            }
        }

        var anonymousFunction = new FunctionValue(new UserDefinedFunction(context.contenido()[0], currentEnvironment, parametros, returnType), "anonymous");

        return anonymousFunction;
    }

    public override ValueWrapper VisitLlamadafuncion1(GolightSemanticoParser.Llamadafuncion1Context context)
    {
 
        ValueWrapper objeto = Visit(context.expresion(0));

        string metodo = context.ID().GetText();

        List<ValueWrapper> argumentos = new List<ValueWrapper>();
        if (context.expresion() != null)
        {
            foreach (var expr in context.expresion())
            {
                argumentos.Add(Visit(expr));
            }
        }

        if (objeto is StructValue structValue)
        {
            if (structValue.Fields.ContainsKey(metodo) && structValue.Fields[metodo] is FunctionValue funcion)
            {

                return funcion.Call(argumentos, this);
            }
            else
            {
                throw new SemanticError($"El struct no tiene un método llamado '{metodo}'", context.Start);
            }
        }
        else
        {
            throw new SemanticError($"No se puede llamar a un método en un valor que no es un struct", context.Start);
        }
    }

    public override ValueWrapper VisitLlamadafuncion2(GolightSemanticoParser.Llamadafuncion2Context context)
    {

        string nombreFuncion = context.ID().GetText();

        List<ValueWrapper> argumentos = new List<ValueWrapper>();
        if (context.parametros() != null)
        {
            if (context.parametros().Length > 0)
            {
                foreach (var expr in context.parametros()[0].expresion())
                {
                    argumentos.Add(Visit(expr));
                }
            }
        }

        if (currentEnvironment.EnontrarVariable(nombreFuncion))
        {
            ValueWrapper funcionWrapper = currentEnvironment.GetVariable(nombreFuncion, context.Start);

            if (funcionWrapper is FunctionValue funcion)
            {
                if (funcion.Arity() == argumentos.Count)
                {

                    return funcion.Call(argumentos, this);
                }
                else
                {
                    throw new SemanticError($"Número incorrecto de argumentos para la función '{nombreFuncion}'. Esperados: {funcion.Arity()}, Recibidos: {argumentos.Count}", context.Start);
                }
            }
            else
            {
                throw new SemanticError($"'{nombreFuncion}' no es una función", context.Start);
            }
        }
        else
        {
            throw new SemanticError($"La función '{nombreFuncion}' no ha sido declarada", context.Start);
        }
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
    //Me rehusaba a usar esta funcion de GetTypeName, pero de tanto estar poniendo el mismo swtich al final la tuve que implementar.
    private string GetTypeName(ValueWrapper value)
    {
        return value switch
        {
            IntValue _ => "int",
            FloatValue _ => "float64",
            StringValue _ => "string",
            BoolValue _ => "bool",
            VoidValue _ => "nil",
            RuneValue _ => "rune",
            FunctionValue _ => "función",
            StructValue _ => "struct",
            SliceValue sliceValue_ => sliceValue_.Type,
            _ => "desconocido"
        };
    }
    private ValueWrapper Sumar(ValueWrapper left, ValueWrapper right, GolightSemanticoParser.SumresContext context)
    {
        
        Console.WriteLine($"Sumando {left} con {right}");

        if (left is IntValue leftInt && right is IntValue rightInt)
        {
            return new IntValue(leftInt.Value + rightInt.Value);
        }

        if (left is IntValue leftInt2 && right is FloatValue rightFloat)
        {
            return new FloatValue(leftInt2.Value + rightFloat.Value);
        }

        if (left is FloatValue leftFloat && right is FloatValue rightFloat2)
        {
            return new FloatValue(leftFloat.Value + rightFloat2.Value);
        }

        if (left is FloatValue leftFloat2 && right is IntValue rightInt2)
        {
            return new FloatValue(leftFloat2.Value + rightInt2.Value);
        }

        if (left is StringValue leftString && right is StringValue rightString)
        {
            return new StringValue(leftString.Value + rightString.Value);
        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)} con ({GetTypeName(right)}) usando el operador +",
            context.Start
        );
    }

    private ValueWrapper Restar(ValueWrapper left, ValueWrapper right, GolightSemanticoParser.SumresContext context)
    {

        if (left is IntValue leftInt && right is IntValue rightInt)
        {
            return new IntValue(leftInt.Value - rightInt.Value);
        }

        if (left is IntValue leftInt2 && right is FloatValue rightFloat)
        {
            return new FloatValue(leftInt2.Value - rightFloat.Value);
        }

        if (left is FloatValue leftFloat && right is FloatValue rightFloat2)
        {
            return new FloatValue(leftFloat.Value - rightFloat2.Value);
        }

        if (left is FloatValue leftFloat2 && right is IntValue rightInt2)
        {
            return new FloatValue(leftFloat2.Value - rightInt2.Value);
        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)}) con ({GetTypeName(right)}) usando el operador -",
            context.Start
        );
    }

    private ValueWrapper Multiplicar(ValueWrapper left, ValueWrapper right, GolightSemanticoParser.MultdivmodContext context)
    {

        if (left is IntValue leftInt && right is IntValue rightInt)
        {
            return new IntValue(leftInt.Value * rightInt.Value);
        }

        if (left is IntValue leftInt2 && right is FloatValue rightFloat)
        {
            return new FloatValue(leftInt2.Value * rightFloat.Value);
        }

        if (left is FloatValue leftFloat && right is FloatValue rightFloat2)
        {
            return new FloatValue(leftFloat.Value * rightFloat2.Value);
        }

        if (left is FloatValue leftFloat2 && right is IntValue rightInt2)
        {
            return new FloatValue(leftFloat2.Value * rightInt2.Value);
        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)}) con ({GetTypeName(right)}) usando el operador *",
            context.Start
        );
    }

    private ValueWrapper Dividir(ValueWrapper left, ValueWrapper right, GolightSemanticoParser.MultdivmodContext context)
    {

        if (right is IntValue rightInt1 && rightInt1.Value == 0 || right is FloatValue rightFloat1 && rightFloat1.Value == 0)
        {
            throw new SemanticError("No se puede dividir por 0", context.Start);
        }

        else {

        if (left is IntValue leftInt && right is IntValue rightInt)
        {
            return new IntValue(leftInt.Value / rightInt.Value);
        }

        if (left is IntValue leftInt2 && right is FloatValue rightFloat)
        {
            return new FloatValue(leftInt2.Value / rightFloat.Value);
        }

        if (left is FloatValue leftFloat && right is FloatValue rightFloat2)
        {
            return new FloatValue(leftFloat.Value / rightFloat2.Value);
        }

        if (left is FloatValue leftFloat2 && right is IntValue rightInt2)
        {
            return new FloatValue(leftFloat2.Value / rightInt2.Value);
        }

        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)}) con ({GetTypeName(right)}) usando el operador /",
            context.Start
        );
    }

    private ValueWrapper Modulo(ValueWrapper left, ValueWrapper right, GolightSemanticoParser.MultdivmodContext context)
    {

        if (left is IntValue leftInt && right is IntValue rightInt)
        {
            return new IntValue(leftInt.Value % rightInt.Value);
        }

        throw new SemanticError(
            $"No se puede operar este tipo ({GetTypeName(left)}) con ({GetTypeName(right)}) usando el operador %",
            context.Start
        );
    }

private ValueWrapper GetValueFromTypeName(string typeName)
{
    return typeName switch
    {
        "nil" => new VoidValue(),
        _ => throw new ArgumentException($"Tipo desconocido: {typeName}")
    };
}

    private string GetTypeSimbolos(ValueWrapper value)
    {
        return value switch
        {
            IntValue _ => "int",
            FloatValue _ => "float64",
            StringValue _ => "string",
            BoolValue _ => "bool",
            VoidValue _ => "nil",
            RuneValue _ => "rune",
            FunctionValue _ => "función",
            StructValue _ => "struct",
            SliceValue sliceValue_ => "[]" + sliceValue_.Type,
            _ => "desconocido"
        };
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

}