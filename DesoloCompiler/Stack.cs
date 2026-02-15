namespace DesoloCompiler;

public class Stack<T>
{
    readonly List<T> Main = new();

    public void Push(T ToPush)
    {
        Main.Add(ToPush);
    }

    public T ReadTop()
    {
        return Main[^1];
    }

    public T Pop()
    {
        T ToReturn = ReadTop();
        Main.RemoveAt(Main.Count - 1);
        return ToReturn;
    }

    public int Size => Main.Count;
}