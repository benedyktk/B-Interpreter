using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DesoloCompiler
{
    public class Compiler
    {
        public static List<IFileLine> StringToCode(string Code)
        {
            string[] RawCodeLines = Code.Split(";");
            List<string> CodeLines = new List<string>();
            for (int i = 0; i < RawCodeLines.Length; i++)
            {
                string Operative = RawCodeLines[i];
                Operative = new string(Operative.Where(c => !char.IsWhiteSpace(c)).ToArray());
                if (Operative.Length != 0 && Operative[0] != '/')
                {
                    CodeLines.Add(Operative);
                }
            }
            List<IFileLine> FCode = new List<IFileLine>();
            Dictionary<string, int> FuncToInt = new Dictionary<string, int>();
            Dictionary<string, int> PlaceToInt = new Dictionary<string, int>();
            LineInfo[] Collection = new LineInfo[CodeLines.Count]; int TotalID = 0;
            OrderStack ToUse = new OrderStack();
            for (int i = 0; i < CodeLines.Count; i++)
            {
                string Operative = CodeLines[i];
                if (Operative[0] == 'f' || Operative[0] == '#')
                {
                    Collection[i] = new LineInfo(1, TotalID, -5, ToUse.Size);
                }
                else
                {
                    if (Operative[0] == 'd')
                    {
                        ToUse.Push(-1);
                        Collection[i] = new LineInfo(0, TotalID, -1, ToUse.Size);
                    }
                    else if (Operative[0] == 'c')
                    {
                        if (Operative[1] == 'i')
                        {
                            ToUse.Push(0);
                            Collection[i] = new LineInfo(1, TotalID, 0, ToUse.Size);
                        }
                        else if (Operative[1] == 'e')
                        {
                            ToUse.Push(1);
                            Collection[i] = new LineInfo(1, TotalID, 1, ToUse.Size);
                        }
                        else if (Operative[1] == 'w')
                        {
                            ToUse.Push(2);
                            Collection[i] = new LineInfo(1, TotalID, 2, ToUse.Size);
                        }
                    }
                    else if (Operative[0] == '{')
                    {
                        Collection[i] = new LineInfo(Interpreter.BoolToInt(ToUse.ReadTop() == -1), TotalID, ToUse.ReadTop(), ToUse.Size);
                    }
                    else if (Operative[0] == '}')
                    {
                        if (ToUse.ReadTop() == -1 || ToUse.ReadTop() == 2)
                        {
                            Collection[i] = new LineInfo(1, TotalID, ToUse.ReadTop(), ToUse.Size);
                        }
                        else
                        {
                            Collection[i] = new LineInfo(0, TotalID, ToUse.ReadTop(), ToUse.Size);
                        }
                        ToUse.Pop();
                    }
                }
                TotalID += Collection[i].count;
            }
            for (int i = 0; i < CodeLines.Count; i++)
            {
                string Operative = CodeLines[i];
                int ReadSpot = 0;
                if (Operative[0] == 'f')
                {
                    ReadSpot++;
                    var Full = ReadUntil(Operative, ReadSpot, '(');
                    ReadSpot = Full.rs;
                    List<Pointer> Info = new List<Pointer>();
                    string JIC = "";
                    if (Full.str == "except" || Full.str == "jump")
                    {
                        var Function = ReadUntil2(Operative, ReadSpot);
                        ReadSpot = Function.rs; JIC = Function.str;
                        if (ReadSpot == Operative.Length)
                        {
                            ReadSpot -= 2;
                        }
                    }
                    while (PointerStart(Operative[ReadSpot]))
                    {
                        var Combo = ReadPointer(Operative, ReadSpot);
                        ReadSpot = Combo.rs; Info.Add(Combo.pnt);
                        if (Operative[ReadSpot] != ')')
                        {
                            var Combo2 = ReadUntil(Operative, ReadSpot, ',');
                            ReadSpot = Combo2.rs;
                        }
                    }
                    ReadUntil(Operative, ReadSpot, ')');
                    switch (Full.str)
                    {
                        case "return":
                            switch (Info.Count)
                            {
                                case 0:
                                    FCode.Add(new Return(null));
                                    break;
                                case 1:
                                    FCode.Add(new Return(Info[0]));
                                    break;
                                default:
                                    throw new Exception("Invalid amount of parameters in Return function.");
                            }
                            break;
                        case "write":
                            if (Info.Count > 1)
                            {
                                throw new Exception("Invalid amount of parameters in Write function.");
                            }
                            FCode.Add(new Operation(Info, new Pointer(0, 0, false), -2));
                            break;
                        case "jump":
                            switch (Info.Count)
                            {
                                case 0:
                                    FCode.Add(new Jump(new JumpString(JIC), new Pointer(1, 0, false), false));
                                    break;
                                case 1:
                                    FCode.Add(new Jump(new JumpString(JIC), Info[0], false));
                                    break;
                                default:
                                    throw new Exception("Invalid amount of parameters in Jump function.");
                            }
                            break;
                        case "except":
                            JIC = JIC.Replace('_', ' ');
                            FCode.Add(new ExceptionLine("\"" + JIC + "\" at line " + i ));
                            break;
                        case "terminate":
                            if (Info.Count != 0)
                            {
                                throw new Exception("Invalid amount of parameters in Terminate function.");
                            }
                            FCode.Add(new Jump(new JumpSpotProper(-1), new Pointer(1, 0, false), false));
                            break;
                        case "exceptadd":
                            if (Info.Count != 1)
                            {
                                throw new Exception("Invalid amount of parameters in ExceptAdd function.");
                            }
                            FCode.Add(new Operation(Info, new Pointer(0, 0, false), -6));
                            break;
                        case "exceptsend":
                            if (Info.Count != 0)
                            {
                                throw new Exception("Invalid amount of parameters in ExceptSend function.");
                            }
                            FCode.Add(new Operation(Info, new Pointer(0, 0, false), -6));
                            break;
                        case "remove":
                            if (Info.Count != 1)
                            {
                                throw new Exception("Invalid amount of parameters in Remove function.");
                            }
                            FCode.Add(new Operation(Info, new Pointer(0, 0, false), -8));
                            break;
                        case "readint":
                        case "readstring":
                        case "readdone":
                        case "hasval":
                            throw new Exception("No return line of system function");
                        default:
                            FCode.Add(new FunctionCall(Info, null, new JumpString(Full.str)));
                            break;
                    }
                }
                else if (Operative[0] == '#')
                {
                    var Full = ReadPointer(Operative, 0);
                    Pointer Destination = Full.pnt; ReadSpot = Full.rs;
                    if (Operative[ReadSpot] != '=')
                    {
                        throw new Exception("Invalid post-pointer symbol.");
                    }
                    ReadSpot++;
                    if (Operative[ReadSpot] == 'f')
                    {
                        var Rest = ReadUntil(Operative, ReadSpot + 1, '(');
                        switch (Rest.str)
                        {
                            case "readint":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, -3)); break;
                            case "readstring":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, -4)); break;
                            case "readdone":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, -5)); break;
                            case "return":
                            case "write":
                            case "jump":
                            case "except":
                            case "exceptadd":
                            case "exceptsend":
                            case "remove":
                                throw new Exception("No return line of system function");
                            default:
                                ReadSpot = Rest.rs;
                                List<Pointer> Info = new List<Pointer>();
                                while (PointerStart(Operative[ReadSpot]))
                                {
                                    var Combo = ReadPointer(Operative, ReadSpot);
                                    ReadSpot = Combo.rs; Info.Add(Combo.pnt);
                                    if (Operative[ReadSpot] != ')')
                                    {
                                        var Combo2 = ReadUntil(Operative, ReadSpot, ',');
                                        ReadSpot = Combo2.rs;
                                    }
                                }
                                ReadUntil(Operative, ReadSpot, ')');
                                if (Rest.str == "hasval")
                                {
                                    FCode.Add(new Operation(Info, Destination, -7));
                                }
                                FCode.Add(new FunctionCall(Info, Destination, new JumpString(Rest.str)));
                                break;
                        }
                    }
                    else
                    {
                        var UnaryOperator = ReadUntil(Operative, ReadSpot, '#');
                        string UOp = UnaryOperator.str; ReadSpot = UnaryOperator.rs - 1;
                        var P1 = ReadPointer(Operative, ReadSpot);
                        List<Pointer> Arguments = new List<Pointer>();
                        Arguments.Add(P1.pnt); ReadSpot = P1.rs;
                        if (ReadSpot == Operative.Length)
                        {
                            FCode.Add(new Operation(Arguments, Destination, IdentifyUOp(UOp)));
                        }
                        else
                        {
                            var BinaryOperator = ReadUntil(Operative, ReadSpot, '#');
                            string BOp = BinaryOperator.str; ReadSpot = BinaryOperator.rs - 1;
                            var P2 = ReadPointer(Operative, ReadSpot);
                            Arguments.Add(P2.pnt); ReadSpot = P2.rs;
                            FCode.Add(new Operation(Arguments, Destination, IdentifyBOp(BOp)));
                        }
                    }
                }
                else if (Operative[0] == 'd')
                {
                    var TillP = ReadUntil(Operative, ReadSpot, '(');
                    ReadSpot = TillP.rs;
                    var TillEP = ReadUntil(Operative, ReadSpot, ')');
                    ReadSpot = TillEP.rs;
                    switch (TillP.str)
                    {
                        case "define":
                            FuncToInt.Add(TillEP.str, FCode.Count + 1);
                            break;
                        case "defineplace":
                            PlaceToInt.Add(TillEP.str, FCode.Count);
                            break;
                        default:
                            throw new Exception("Invalid define line.");
                    }
                }
                else if (Operative[0] == 'c')
                {
                    var TillP = ReadUntil(Operative, ReadSpot + 1, '(');
                    ReadSpot = TillP.rs; string ConditionalType = TillP.str;
                    int ctype = 0;
                    switch(TillP.str)
                    {
                        case "if":
                            ctype = 0; break;
                        case "else":
                            ctype = 1; break;
                        case "while":
                            ctype = 2; break;
                        default:
                            throw new Exception("Invalid conditional after c.");
                    }
                    int ClosingLine = -1; int Amount = 0;
                    int StackSize = Collection[i].stacksize;
                    for (int j = i; j < CodeLines.Count && ClosingLine == -1; j++)
                    {
                        if (CodeLines[j][0] == '}' && Collection[j].stacksize == StackSize && ctype == Collection[i].ctype)
                        {
                            ClosingLine = Collection[j].ID;
                        }
                    }
                    if (ClosingLine == -1)
                    {
                        throw new Exception("No ending parentheses of conditional.");
                    }
                    var Conditional = ReadPointer(Operative, ReadSpot);
                    ReadSpot = Conditional.rs; ReadUntil(Operative, ReadSpot, ')');
                    FCode.Add(new Jump(new JumpSpotProper(ClosingLine + Interpreter.BoolToInt(ctype == 2)), Conditional.pnt, !(ctype == 1)));
                }
                else if (Operative[0] == '{' && Operative.Length == 1)
                {
                    if (Collection[i].ctype == -1)
                    {
                        FCode.Add(new ExceptionLine("Entered function body without calling it."));                        
                    }
                }
                else if (Operative[0] == '}' && Operative.Length == 1)
                {
                    if (Collection[i].ctype == -1)
                    {
                        FCode.Add(new Return(null));
                    }
                    if (Collection[i].ctype == 2)
                    {
                        int StackSize = Collection[i].stacksize;
                        int ClosingLine = -1;
                        for (int j = i; j >= 0 && ClosingLine == -1; j--)
                        {
                            if (CodeLines[j][0] == 'c' && Collection[j].stacksize == StackSize && 2 == Collection[j].ctype)
                            {
                                ClosingLine = Collection[j].ID;
                            }
                        }
                        if (ClosingLine == -1)
                        {
                            throw new Exception("No ending parentheses of conditional");
                        }
                        FCode.Add(new Jump(new JumpSpotProper(ClosingLine), new Pointer(1, 0, false), false));
                    }
                }
                else
                {
                    throw new Exception("Invalid line start.");
                }
            }
            for (int i = 0; i < FCode.Count; i++)
            {
                switch (FCode[i])
                {
                    case FunctionCall fc:
                        string Needed = "";
                        switch (fc.RunLine)
                        {
                            case JumpString js:
                                Needed = js.Place;
                                break;
                            case JumpSpotProper jsp:
                                throw new Exception("Int functioncall jumpspot somehow got generated inside compiler.");
                        }
                        if (FuncToInt.ContainsKey(Needed))
                        {
                            FCode[i] = new FunctionCall(fc.EntryPointers, fc.ExitPointer, new JumpSpotProper(FuncToInt[Needed]));
                        }
                        else
                        {
                            throw new Exception("Called function named " + Needed + " has not been defined.");
                        }
                        break;
                    case Jump jp:
                        string ToGet = null;
                        switch (jp.Spot)
                        {
                            case JumpString js:
                                ToGet = js.Place;
                                break;
                        }
                        if(ToGet != null)
                        {
                            if (PlaceToInt.ContainsKey(ToGet))
                            {
                                FCode[i] = new Jump(new JumpSpotProper(PlaceToInt[ToGet]), jp.ConditionPointer, jp.Opposite);
                            }
                            else
                            {
                                throw new Exception("Jumped-to place named " + ToGet + " has not been defined.");
                            }
                            break;
                        }
                        break;
                }
            }
            return FCode;
        }
        public static (Pointer pnt, int rs) ReadPointer(string Code, int ReadSpot)
        {
            ReadSpot++;
            if (ReadSpot + 1 < Code.Length && Code[ReadSpot + 1] == '\"')
            {
                return (new Pointer((int)Code[ReadSpot], 0, true), ReadSpot + 2);
            }
            int PointerValue = 0;
            switch (Code[ReadSpot])
            {
                case 'p':
                    PointerValue = 1;
                    ReadSpot++;
                    break;
                case 'a':
                    PointerValue = 2;
                    ReadSpot++;
                    break;
                case 'f':
                    PointerValue = 3;
                    ReadSpot++;
                    break;
                case 't':
                    ReadSpot++;
                    if (Code[ReadSpot] == 'r')
                    {
                        ReadSpot++;
                        if (Code[ReadSpot] == 'u')
                        {
                            ReadSpot++;
                            if (Code[ReadSpot] == 'e')
                            {
                                return (new Pointer(1, 0, false), ReadSpot + 1);
                            }
                            else
                            {
                                throw new Exception("Badly spelled true.");
                            }
                        }
                        else
                        {
                            throw new Exception("Badly spelled true.");
                        }
                    }
                    else
                    {
                        throw new Exception("Badly spelled true.");
                    }
            }
            if (Code[ReadSpot] == 'a' && PointerValue == 3)
            {
                PointerValue = 4;
                ReadSpot++;
                if (Code[ReadSpot] == 'l')
                {
                    ReadSpot++;
                    if (Code[ReadSpot] == 's')
                    {
                        ReadSpot++;
                        if (Code[ReadSpot] == 'e')
                        {
                            return (new Pointer(0, 0, false), ReadSpot + 1);
                        }
                        else
                        {
                            throw new Exception("Badly spelled false.");
                        }
                    }
                    else
                    {
                        throw new Exception("Badly spelled false.");
                    }
                }
            }
            if (!Number(Code[ReadSpot]))
            {
                throw new Exception("Invalid pointer reference.");
            }
            string CreatedNum = "";
            while (ReadSpot < Code.Length && Number(Code[ReadSpot]) && !(Code[ReadSpot] == '-' && CreatedNum.Length != 0))
            {
                CreatedNum += Code[ReadSpot];
                ReadSpot++;
            }
            if (ReadSpot < Code.Length && Code[ReadSpot] == '\"')
            {
                ReadSpot++;
                return (new Pointer(int.Parse(CreatedNum), PointerValue, true), ReadSpot);
            }
            return (new Pointer(int.Parse(CreatedNum), PointerValue, false), ReadSpot);
        }
        public static (string str, int rs) ReadUntil(string Code, int ReadSpot, char c)
        {
            string Creatine = "";
            while (ReadSpot < Code.Length && Code[ReadSpot] != c)
            {
                Creatine += Code[ReadSpot];
                ReadSpot++;
            }
            if (ReadSpot == Code.Length)
            {
                throw new Exception("Unexpected end of line.");
            }
            return (Creatine, ReadSpot + 1);
        }
        public static (string str, int rs) ReadUntil2(string Code, int ReadSpot)
        {
            string Creatine = "";
            while (ReadSpot < Code.Length && Code[ReadSpot] != ')' && Code[ReadSpot] != ',')
            {
                Creatine += Code[ReadSpot];
                ReadSpot++;
            }
            if (ReadSpot == Code.Length)
            {
                throw new Exception("Unexpected end of line.");
            }
            return (Creatine, ReadSpot + 1);
        }
        public static bool Number(char c)
        {
            return c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9' || c == '-';
        }
        public static bool PointerStart(char c)
        {
            return c == '#';
        }
        public static int IdentifyUOp(string UOp)
        {
            switch (UOp)
            {
                case "-": return 5;
                case "i!": return 9;
                case "!": return 10;
                case "r": return 11;
                case "": return -1;
            }
            throw new Exception("Invalid unary operation name.");
        }
        public static int IdentifyBOp(string BOp)
        {
            switch (BOp)
            {
                case "+": return 0;
                case "-": return 1;
                case "*": return 2;
                case "/": return 3;
                case "%": return 4;
                case "&": return 6;
                case "|": return 7;
                case "^": return 8;
                case "==": return 16;
                case "!=": return 17;
                case ">": return 18;
                case ">=": return 19;
                case "<": return 20;
                case "<=": return 21;
            }
            throw new Exception("Invalid binary operation name.");
        }
    }
    public record struct LineInfo(int count, int ID, int ctype, int stacksize)
    {
        public override string ToString()
        {
            string conditional = "";
            switch (ctype)
            {
                case -5:
                    conditional = "none"; break;
                case -1:
                    conditional = "define"; break;
                case 0:
                    conditional = "if"; break;
                case 1:
                    conditional = "else"; break;
                case 2:
                    conditional = "where"; break;
            }
            return "B#Lines " + count + ", B#ID " + ID + ", Ctype " + conditional + ", Layer " + stacksize;
        }
    }
}
