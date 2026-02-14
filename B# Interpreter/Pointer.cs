namespace DesoloCompiler;

public enum PointerType
{
    Value = 0,
    Pointer = 1,
    Array = 2,
    Function = 3,
    FunctionArray = 4
}

public readonly record struct Pointer(int PointerV, PointerType type, bool Char)
{
    public override string ToString()
    {
        string ToModify = type switch
        {
            PointerType.Value => PointerV.ToString(),
            PointerType.Pointer => "p" + PointerV,
            PointerType.Array => "a" + PointerV,
            PointerType.Function => "f" + PointerV,
            PointerType.FunctionArray => "fa" + PointerV,
            _ => throw new Exception("Invalid pointer type.")
        };

        return Char ? ToModify + "\"" : ToModify;
    }
}