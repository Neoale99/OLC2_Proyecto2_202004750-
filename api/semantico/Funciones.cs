using analyzer;
using errores;
public class UserDefinedFunction : Invocable
{
    private readonly GolightSemanticoParser.ContenidoContext _body;
    private readonly Environment _closure;
    private readonly string _returnType;
    private readonly List<string> _parametros; 

    public UserDefinedFunction(GolightSemanticoParser.ContenidoContext body, Environment closure, List<string> parametros, string returnType = null)
    {
        _body = body;
        _closure = closure;
        _parametros = parametros;
        _returnType = returnType;
    }

    public int Arity()
    {
        return _parametros.Count;
    }

    public ValueWrapper Invoke(List<ValueWrapper> args, Visitorsemantico visitor)
    {
        Environment functionEnvironment = _closure;
        if (args.Count != _parametros.Count)
        {
            throw new SemanticError($"Se esperaban {_parametros.Count} argumentos, pero se recibieron {args.Count}", null);
        }



        for (int i = 0; i < args.Count; i++)
        {
            string paramName = _parametros[i];
            Console.WriteLine($"Declarada variable {paramName} con valor {args[i]}");
            functionEnvironment.AsignarVariable(paramName, args[i], null); 
        }

        try
        {
            ValueWrapper result = visitor.Visit(_body);


            if (_returnType != null && GetTypeName(result) != _returnType)
            {
                throw new SemanticError($"El tipo de retorno no coincide con el esperado: {_returnType}", null);
            }

            return result;
        }
        catch (ReturnException returnEx)
        {
            return returnEx.Value;
        }

    }

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
}
