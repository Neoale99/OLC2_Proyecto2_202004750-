
public class BreakException : Exception
{
    public BreakException() : base("Break statement")
    {
    }
}


public class ContinueException : Exception
{
    public ContinueException() : base("Continue statement")
    {
    }
}


public class ReturnException : Exception
{
    public ValueWrapper Value { get; }

    public ReturnException(ValueWrapper value) : base("Return statement")
    {
        Value = value;
    }
}