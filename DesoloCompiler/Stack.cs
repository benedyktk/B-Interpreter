namespace DesoloCompiler;

public class Stack<T>
{
    readonly public List<T> Main = new();
    public void Push(T ToPush)
    {
        Main.Add(ToPush);
    }
    public T ReadTop()
    {
        if (Main.Count == 0)
        {
            return default;
        }
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