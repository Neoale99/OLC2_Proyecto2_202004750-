using static Environment;
using errores;
public abstract record ValueWrapper;
public record IntValue(int Value) : ValueWrapper;
public record FloatValue(float Value) : ValueWrapper;
public record StringValue(string Value) : ValueWrapper;
public record BoolValue(bool Value) : ValueWrapper;
public record RuneValue(char Value) : ValueWrapper;
public record StructValue(Dictionary<string, ValueWrapper> Fields) : ValueWrapper
    {

        public ValueWrapper GetField(string fieldName, Antlr4.Runtime.IToken token)
        {

            if (!Fields.ContainsKey(fieldName))
            {
                if (fieldName == "Siguiente")
                {
                    return new VoidValue();
                }
                throw new SemanticError($"El campo '{fieldName}' no existe en el struct", token);
            }
            return Fields[fieldName];
        }


        public void SetField(string fieldName, ValueWrapper value, Antlr4.Runtime.IToken token)
        {
            if (!Fields.ContainsKey(fieldName))
            {
                throw new SemanticError($"El campo '{fieldName}' no existe en el struct", token);
            }
            
            var existingValue = Fields[fieldName];
            if (fieldName == "Siguiente")
            {
                existingValue = value;
            }
            if (existingValue.GetType() != value.GetType())
            {
                throw new SemanticError($"No se puede asignar un valor de tipo2 {GetTypeName(value)} al campo '{fieldName}' de tipo {GetTypeName(existingValue)}", token);
            }

            Fields[fieldName] = value;
        }

        public void AddField(string fieldName, ValueWrapper value, Antlr4.Runtime.IToken token)
        {
            if (Fields.ContainsKey(fieldName))
            {
                throw new SemanticError($"El campo '{fieldName}' ya existe en el struct", token);
            }
            Fields[fieldName] = value;
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
public record FunctionValue(Invocable invocable, string name) : ValueWrapper
{
    public readonly Invocable function = invocable;
    public readonly string name = name;

    public int Arity()
    {
        return invocable.Arity();
    }

    public ValueWrapper Call(List<ValueWrapper> arguments, Visitorsemantico visitor)
    {
        return function.Invoke(arguments, visitor);
    }

    public override string ToString()
    {
        return $"<función {name}>";
    }
    public string Name
    {
        get { return name; }
    }

    public Invocable Callable
    {
        get { return invocable; }
    }
}
public record VoidValue : ValueWrapper;

public record SliceValue(List<ValueWrapper> Values, string Type) : ValueWrapper
    {
        public void AddValue(ValueWrapper value, Antlr4.Runtime.IToken token)
        {
            if (GetTypeName(value) != Type)
            {
                throw new SemanticError($"El valor {value} no es del tipo {Type}", token);
            }
            Values.Add(value);
        }

        public void SetValue(int index, ValueWrapper value, Antlr4.Runtime.IToken token)
        {
            if (index < 0 || index >= Values.Count)
            {
                throw new SemanticError($"Índice {index} fuera de rango", token);
            }
            if (GetTypeName(value) != Type)
            {
                throw new SemanticError($"El valor {value} no es del tipo {Type}", token);
            }
            Values[index] = value;
        }

        public ValueWrapper GetValue(int index, Antlr4.Runtime.IToken token)
        {
            if (index < 0 || index >= Values.Count)
            {
                throw new SemanticError($"Índice {index} fuera de rango", token);
            }
            return Values[index];
        }
        private string GetTypeName(ValueWrapper value)
        {
            return value switch
            {
                IntValue => "int",
                FloatValue => "float",
                StringValue => "string",
                BoolValue => "bool",
                RuneValue => "rune",
                StructValue => "struct",
                FunctionValue => "function",
                VoidValue => "void",
                SliceValue => "slice",
                _ => throw new ArgumentException("Unknown ValueWrapper type")
            };
        }

        public bool CanContainValue(ValueWrapper value)
        {
            return GetTypeName(value) == Type || value is SliceValue;
        }
    
    public class ReturnException : Exception
    {
        public ValueWrapper Value { get; private set; }
        public Antlr4.Runtime.IToken Token { get; private set; }

        public ReturnException(ValueWrapper value, Antlr4.Runtime.IToken token = null) : base("Sentencia return")
        {
            Value = value;
            Token = token;
        }
    }

    }




