namespace DesoloCompiler;

public class Tree
{
    private Tree? Lp;
    private Tree? Rp;
    private int? End;
    private readonly int Index;
    private readonly int Level;

    public Tree()
    {
    }

    private Tree(int NewInd, int Deepness)
    {
        Index = NewInd;
        Level = Deepness;
    }

    private static bool SourceBit(int Num, int Bit)
    {
        return ((Num >> (31 - Bit)) & 1) == 1;
    }

    private static int Cut(int Num, int Bit)
    {
        return Num & -1 & ((1 << Bit) - 1);
    }

    public int? Return(int GoalIndex)
    {
        if (Level == 32)
        {
            return End;
        }

        return SourceBit(GoalIndex, Level + 1) ? Rp?.Return(GoalIndex) : Lp?.Return(GoalIndex);
    }

    public void Set(int GoalIndex, int ToPlace)
    {
        if (Level == 32)
        {
            End = ToPlace;
            return;
        }
        if (SourceBit(GoalIndex, Level + 1))
        {
            Rp ??= new Tree(Cut(GoalIndex, Level), Level + 1);
            Rp.Set(GoalIndex, ToPlace);
        }
        else
        {
            Lp ??= new Tree(Cut(GoalIndex, Level), Level + 1);
            Lp.Set(GoalIndex, ToPlace);
        }
    }

    public bool Remove(int GoalIndex)
    {
        if (SourceBit(GoalIndex, Level + 1))
        {
            if (Level == 31)
            {
                Rp = null;
                return Lp == null;
            }
            if (Lp == null && (Rp == null || Rp.Remove(GoalIndex)))
            {
                Rp = null;
                return true;
            }
        }
        else
        {
            if (Level == 31)
            {
                Lp = null;
                return Rp == null;
            }
            if (Rp == null && (Lp == null || Lp.Remove(GoalIndex)))
            {
                Lp = null;
                return true;
            }
        }
        return false;
    }

    private string Climb()
    {
        if (End != null)
        {
            return $"{Index}={End},";
        }

        return Lp?.Climb() + Rp?.Climb();
    }

    public override string ToString()
    {
        return Climb();
    }
}