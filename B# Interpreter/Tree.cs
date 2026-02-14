using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesoloCompiler
{
    public class Tree
    {
        Tree LP;
        Tree RP;
        int? End;
        int Index;
        int Level;
        public Tree()
        {
            LP = null; RP = null; End = null; Index = 0; Level = 0;
        }
        public Tree(int NewInd, int Deepness)
        {
            LP = null; RP = null; End = null; Index = NewInd; Level = Deepness;
        }
        bool SourceBit(int Num, int Bit)
        {
            return ((Num >> (31 - Bit)) & 1) == 1;
        }
        int Cut(int Num, int Bit)
        {
            return Num & (-1 & ((1 << Bit) - 1));
        }
        public int? Return(int GoalIndex)
        {
            if (Level == 32)
            {
                return End;
            }
            if (SourceBit(GoalIndex, Level + 1))
            {
                if (RP == null)
                {
                    return null;
                }
                else
                {
                    return RP.Return(GoalIndex);
                }
            }
            else
            {
                if (LP == null)
                {
                    return null;
                }
                else
                {
                    return LP.Return(GoalIndex);
                }
            }
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
                if (RP == null)
                {
                    RP = new Tree(Cut(GoalIndex, Level), Level + 1);
                }
                RP.Set(GoalIndex, ToPlace);
            }
            else
            {
                if (LP == null)
                {
                    LP = new Tree(Cut(GoalIndex, Level), Level + 1);
                }
                LP.Set(GoalIndex, ToPlace);
            }
        }
        public bool Remove(int GoalIndex)
        {
            if (SourceBit(GoalIndex, Level + 1))
            {
                if (Level == 31)
                {
                    RP = null;
                    return LP == null;
                }
                if (LP == null && RP.Remove(GoalIndex))
                {
                    RP = null;
                    return true;
                }
            }
            else
            {
                if (Level == 31)
                {
                    LP = null;
                    return RP == null;
                }
                if (RP == null && LP.Remove(GoalIndex))
                {
                    LP = null;
                    return true;
                }
            }
            return false;
        }
        public string Climb()
        {
            if (End != null)
            {
                return Index.ToString() + "=" + End.ToString() + ",";
            }
            string returner = "";
            if(LP != null) { returner += LP.Climb(); }
            if(RP != null) { returner += RP.Climb(); }
            return returner;
        }
        public override string ToString()
        {
            return Climb();
        }
    }
}
