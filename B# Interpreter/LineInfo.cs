namespace DesoloCompiler;

public enum CType
{
        None = -5,
        Define = -1,
        If = 0,
        Else = 1,
        Where = 2
}

public readonly record struct LineInfo(int count, int ID, CType ctype, int stacksize)
{
    public override string ToString()
    {
        string conditional = "";
        switch (ctype)
        {
            case CType.None:
                conditional = "none"; break;
            case CType.Define:
                conditional = "define"; break;
            case CType.If:
                conditional = "if"; break;
            case CType.Else:
                conditional = "else"; break;
            case CType.Where:
                conditional = "where"; break;
        }
        return "B#Lines " + count + ", B#ID " + ID + ", Ctype " + conditional + ", Layer " + stacksize;
    }
}