
public interface Invocable
{
    int Arity();
    ValueWrapper Invoke(List<ValueWrapper> args, Visitorsemantico visitor);
}