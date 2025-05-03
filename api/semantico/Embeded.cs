using errores;
public class Embeded
{
    public static void Generate(Environment env)
    {

    }
}

public class TimeEmbeded : Invocable
{
    public int Arity()
    {
        return 0;
    }

    public ValueWrapper Invoke(List<ValueWrapper> args, Visitorsemantico visitor)
    {
        return new StringValue(DateTime.Now.ToString());
    }
}

public class PrintEmbeded : Invocable
{
    public int Arity()
    {
        return 1;
    }

    public ValueWrapper Invoke(List<ValueWrapper> args, Visitorsemantico visitor)
    {

        var salida = "";

        foreach (var arg in args)
        {
            
            salida += arg switch
            {
                IntValue i => salida += i.Value.ToString() + " ",
                FloatValue f => salida += f.Value.ToString() + " ",
                StringValue s => salida += s.Value + " ",
                BoolValue b => salida += b.Value.ToString() + " ",
                VoidValue v => salida += "void" + " ",
                RuneValue r => salida += r.Value.ToString() + " ",
                FunctionValue fn => salida += "<fn " + fn.name + ">" + " ",
                _ => throw new SemanticError("Valor invalido value", null)
            };
        }
        salida += "\n";

        visitor.salida += salida;

        return visitor.defaultVoid;
    }
}