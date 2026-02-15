using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesoloCompiler
{
    public interface IFileLine
    {

    }
    public record Operation(List<Pointer> EntryPointers, Pointer PointerDest, int OpCode) : IFileLine
    {
        public override string ToString()
        {
            switch (OpCode)
            {
                case -10:
                    return PointerDest + " = fsubdone()";
                case -9:
                    return PointerDest + " = fsubread()";
                case -8:
                    return "fremove(" + EntryPointers[0] + ")";
                case -7:
                    return PointerDest + " = fhasval(" + EntryPointers[0] + ")";
                case -6:
                    if (EntryPointers.Count == 0)
                    {
                        return "fexceptsend()";
                    }
                    else
                    {
                        return "fexceptadd(" + EntryPointers[0] + ")";
                    }
                case -5:
                    return PointerDest + " = freaddone()";
                case -4:
                    return PointerDest + " = freadstring()";
                case -3:
                    return PointerDest + " = freadint()";
                case -2:
                    if (EntryPointers.Count == 0)
                    {
                        return "fwrite()";
                    }
                    else
                    {
                        return "fwrite(" + EntryPointers[0] + ")";
                    }
                case -1:
                    return PointerDest + " = " + EntryPointers[0];
                case 0:
                    return PointerDest + " = " + EntryPointers[0] + " + " + EntryPointers[1];
                case 1:
                    return PointerDest + " = " + EntryPointers[0] + " - " + EntryPointers[1];
                case 2:
                    return PointerDest + " = " + EntryPointers[0] + " * " + EntryPointers[1];
                case 3:
                    return PointerDest + " = " + EntryPointers[0] + " / " + EntryPointers[1];
                case 4:
                    return PointerDest + " = " + EntryPointers[0] + " % " + EntryPointers[1];
                case 5:
                    return PointerDest + " = -" + EntryPointers[0];
                case 6:
                    return PointerDest + " = " + EntryPointers[0] + " & " + EntryPointers[1];
                case 7:
                    return PointerDest + " = " + EntryPointers[0] + " | " + EntryPointers[1];
                case 8:
                    return PointerDest + " = " + EntryPointers[0] + " ^ " + EntryPointers[1];
                case 9:
                    return PointerDest + " = i!" + EntryPointers[0];
                case 10:
                    return PointerDest + " = !" + EntryPointers[0];
                case 11:
                    return PointerDest + " = r" + EntryPointers[0];
                case 16:
                    return PointerDest + " = " + EntryPointers[0] + " == " + EntryPointers[1];
                case 17:
                    return PointerDest + " = " + EntryPointers[0] + " != " + EntryPointers[1];
                case 18:
                    return PointerDest + " = " + EntryPointers[0] + " > " + EntryPointers[1];
                case 19:
                    return PointerDest + " = " + EntryPointers[0] + " >= " + EntryPointers[1];
                case 20:
                    return PointerDest + " = " + EntryPointers[0] + " < " + EntryPointers[1];
                case 21:
                    return PointerDest + " = " + EntryPointers[0] + " <= " + EntryPointers[1];
            }
            throw new Exception("Invalid OpCode type.");
        }
    };
    public record Jump(IJumpSpot Spot, Pointer ConditionPointer, bool Opposite) : IFileLine
    {
        public override string ToString()
        {
            if (Opposite)
            {
                return "fjump(" + Spot + ",!" + ConditionPointer + ")";
            }
            return "fjump(" + Spot + "," + ConditionPointer + ")";
        }
    }

    public record FunctionCall(List<Pointer> EntryPointers, Pointer? ExitPointer, IJumpSpot RunLine) : IFileLine
    {
        public override string ToString()
        {
            string Insides = "";
            for (int i = 0; i < EntryPointers.Count; i++)
            {
                Insides += EntryPointers[i].ToString() + ",";
            }
            string Body = "fcall(" + Insides + RunLine + ")";
            if (ExitPointer == null)
            {
                return Body;
            }
            else
            {
                return ExitPointer.Value + " = " + Body;
            }
        }
    }

    public record Return(Pointer? ReturnVal) : IFileLine
    {
        public override string ToString()
        {
            if (ReturnVal == null)
            {
                return "freturn()";
            }
            else
            {
                return "freturn(" + ReturnVal + ")";
            }
        }
    }
    public record StringLine(string Text, bool Type) : IFileLine
    {
        public override string ToString()
        {
            if (Type)
            {
                return "fsubmit(" + Text + ")";
            }
            else
            {
                return "fexcept(" + Text + ")";
            }
        }
    }
}
