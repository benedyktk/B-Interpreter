namespace DesoloCompiler;

public enum CType
{
        None = -5,
        Define = -1,
        If = 0,
        Else = 1,
        While = 2,
        Until = 3
}

public readonly record struct LineInfo(int count, int ID, CType ctype, int stacksize)
{
    public override string ToString()
    {

        string conditional = ctype switch
        {
            CType.None => "none",
            CType.Define => "define",
            CType.If => "if",
            CType.Else => "else",
            CType.While => "while",
            CType.Until => "until",
            _ => throw new Exception("Invalid CType")
        };
        return "B#Lines " + count + ", B#ID " + ID + ", Ctype " + conditional + ", Layer " + stacksize;
    }
}