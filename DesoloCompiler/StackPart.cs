namespace DesoloCompiler;

public record StackPart(List<int> PassedOn, int ReturnLine)
{
    public override string ToString()
    {
        string Totality = "";
        for (int I = 0; I < PassedOn.Count; I++)
        {
            Totality += $"f{I}={PassedOn[I]}, ";
        }
        Totality += "rl=" + ReturnLine;
        return Totality;
    }
}