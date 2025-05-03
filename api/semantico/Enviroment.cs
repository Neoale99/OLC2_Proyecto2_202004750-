using errores;
public class Environment
{

    public Dictionary<string, ValueWrapper> variables = new Dictionary<string, ValueWrapper>();
    private Utilssemantico.SymbolTable symbolTable = new Utilssemantico.SymbolTable();

    private Environment? parent;

    public Environment(Environment? parent)
    {
        this.parent = parent;
    }
    public Environment? Enclosing { get { return parent; } }
    public ValueWrapper GetVariable(string id, Antlr4.Runtime.IToken token)
    {
        if (variables.ContainsKey(id))
        {
            //Console.WriteLine("Encontrada variable " + id);
            return variables[id];
        }

        if (parent != null)
        {
            return parent.GetVariable(id, token);
        }

        throw new SemanticError("La variable " + id + " no ha sido encontrada", token);
    }

    public void DeclararVariable(string id, ValueWrapper value, Antlr4.Runtime.IToken? token)
    {
        if (variables.ContainsKey(id))
        {
            if (token != null) throw new SemanticError("La variable " + id + " ya ha sido declarada", token);
        }
        else
        {
            Console.WriteLine("Declarada variable " + id + " con valor " + value);
            variables[id] = value;
            string type = GetTypeName(value);
            int line = token?.Line ?? -1;
            int column = token?.Column ?? -1;
            symbolTable.AddSymbol(new Utilssemantico.Symbol(id, type, "local", line, column));
        }
    }

    public ValueWrapper AsignarVariable(string id, ValueWrapper value, Antlr4.Runtime.IToken token)
    {
        if (variables.ContainsKey(id))
        {
            if (value is IntValue && variables[id] is FloatValue)
            {
                
                variables[id] = value;
                return variables[id];
            } else if (value.GetType() != variables[id].GetType())
            {
                throw new SemanticError("No se puede asignar un valor de tipo " + value.GetType() + " a una variable de tipo " + variables[id].GetType(), token);
            }
            variables[id] = value;
            return value;
        }

        if (parent != null)
        {
            return parent.AsignarVariable(id, value, token);
        }

        throw new SemanticError("La variable " + id + " no ha sido encontrada", token);
    }

    public bool EnontrarVariable(string id)
    {
        if (variables.ContainsKey(id))
        {
            return true;
        }

        if (parent != null)
        {
            return parent.EnontrarVariable(id);
        }

        return false;
    } 

    public void AssignSliceElement(string id, List<int> indices, ValueWrapper value, Antlr4.Runtime.IToken token)
    {
        ValueWrapper arrayWrapper = GetVariable(id, token);
        if (arrayWrapper is SliceValue arrayValue)
        {
            if (indices.Count == 1)
            {
                int index = indices[0];
                arrayValue.SetValue(index, value, token);
            }
            else
            {
                throw new SemanticError("Solo se soporta la asignación a un índice simple", token);
            }
        }
        else
        {
            throw new SemanticError($"La variable {id} no es un slice", token);
        }
    }
    public string GetTypeName(ValueWrapper value)
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
            SliceValue => "slice",
            _ => "desconocido"
        };
    }
    public Utilssemantico.SymbolTable GetSymbolTable()
    {
        return symbolTable;
    }
}

