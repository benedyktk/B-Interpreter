namespace DesoloCompiler;

public class Interpreter(List<IFileLine> TheCode, TextReader ConsoleIn, TextWriter ConsoleOut)
{
    private readonly Tree Vars = new();
    private readonly Stack<StackPart> CallStack = new();
    private string ExceptionProgress = "";
    private string ReadingProgress = "";
    private string SubmissionProgress = "";

    private int PReturn(int Place)
    {
        return Vars.Return(Place).GetValueOrDefault();
    }

    private bool PHasValue(int Place)
    {
        return Vars.Return(Place) != null;
    }

    private int Read(Pointer ToRead)
    {
        return ToRead.type switch
        {
            PointerType.Value => ToRead.PointerV,
            PointerType.Pointer => PReturn(ToRead.PointerV),
            PointerType.Array => PReturn(PReturn(ToRead.PointerV)),
            PointerType.Function => CallStack.ReadTop().PassedOn[ToRead.PointerV],
            PointerType.FunctionArray => PReturn(CallStack.ReadTop().PassedOn[ToRead.PointerV]),
            _ => throw new Exception("Invalid pointer type")
        };
    }

    private bool HasValue(Pointer ToRead)
    {
        return ToRead.type switch
        {
            PointerType.Value => true,
            PointerType.Pointer => PHasValue(ToRead.PointerV),
            PointerType.Array => PHasValue(PReturn(ToRead.PointerV)),
            PointerType.Function => ToRead.PointerV >= 0 && ToRead.PointerV < CallStack.ReadTop().PassedOn.Count,
            PointerType.FunctionArray => PHasValue(CallStack.ReadTop().PassedOn[ToRead.PointerV]),
            _ => throw new Exception("Invalid pointer type")
        };
    }

    private void Remove(Pointer ToRemove)
    {
        switch (ToRemove.type)
        {
            case PointerType.Pointer:
                Vars.Remove(ToRemove.PointerV);
                break;
            case PointerType.Array:
                Vars.Remove(PReturn(ToRemove.PointerV));
                break;
            case PointerType.FunctionArray:
                Vars.Remove(CallStack.ReadTop().PassedOn[ToRemove.PointerV]);
                break;
            case PointerType.Value:
            case PointerType.Function:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Set(Pointer ToSet, int Value)
    {
        switch (ToSet.type)
        {
            case PointerType.Pointer:
                Set(ToSet.PointerV, Value);
                break;
            case PointerType.Array:
                Set(PReturn(ToSet.PointerV), Value);
                break;
            case PointerType.Function:
                CallStack.ReadTop().PassedOn[ToSet.PointerV] = Value;
                break;
            case PointerType.FunctionArray:
                Set(CallStack.ReadTop().PassedOn[ToSet.PointerV], Value);
                break;
            case PointerType.Value:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private int JsToInt(IJumpSpot NoName)
    {
        return NoName switch
        {
            JumpSpotProper Jsp => Jsp.Place,
            JumpString => throw new Exception("String function escaped compiler in unknown ways."),
            _ => throw new Exception("Yeah this is just impossible")
        };
    }

    private void Set(Pointer ToSet, bool Value)
    {
        Set(ToSet, BoolToInt(Value));
    }

    private void Set(int ToSet, int Value)
    {
        Vars.Set(ToSet, Value);
    }

    public static int BoolToInt(bool Val)
    {
        return Val ? 1 : 0;
    }

    private bool IntToBool(int Val)
    {
        return Val != 0;
    }

    public void RunCode(int Pointer)
    {
        Random Rnd = new Random();
        while (Pointer < TheCode.Count && Pointer >= 0)
        {
            switch (TheCode[Pointer])
            {
                case Operation Op:
                    RunOperation(Op, Rnd);
                    break;
                case Jump Jmp:
                    if (IntToBool(Read(Jmp.ConditionPointer)) ^ Jmp.Opposite)
                    {
                        Pointer = JsToInt(Jmp.Spot) - 1;
                    }
                    break;
                case FunctionCall Fc:
                    CallStack.Push(new StackPart(PointerToInt(Fc.EntryPointers), Pointer));
                    Pointer = JsToInt(Fc.RunLine) - 1;
                    break;
                case Return Rt:
                    int ReadReturnValue = 0;
                    if (Rt.ReturnVal != null)
                    {
                        ReadReturnValue = Read(Rt.ReturnVal.Value);
                    }
                    Pointer = CallStack.Pop().ReturnLine;
                    if (Rt.ReturnVal != null)
                    {
                        FunctionCall Fc = (FunctionCall)TheCode[Pointer];
                        if (Fc.ExitPointer != null)
                        {
                            Set(Fc.ExitPointer.Value, ReadReturnValue);
                        }
                    }
                    break;
                case StringLine Sl:
                    if (Sl.Type)
                    {
                        SubmissionProgress += Sl.Text;
                    }
                    else
                    {
                        throw new Exception(Sl.Text);
                    }
                    break;
            }
            Pointer++;
        }
    }

    private List<int> PointerToInt(List<Pointer> ToConvert)
    {
        List<int> Returner = new List<int>();
        foreach (var P in ToConvert)
        {
            Returner.Add(Read(P));
        }
        return Returner;
    }

    private void RunOperation(Operation ToRun, Random TheDice)
    {
        Pointer Plc = ToRun.PointerDest;
        List<int> Vals = PointerToInt(ToRun.EntryPointers);
        switch (ToRun.OpCode)
        {
            case OperationCode.Assign:
                CheckLength(1, ToRun);
                Set(Plc, Vals[0]);
                break;
            case OperationCode.Write:
                CheckLength(0, 1, ToRun);
                if (Vals.Count == 0)
                {
                    ConsoleOut.WriteLine();
                }
                else if (ToRun.EntryPointers[0].Char)
                {
                    ConsoleOut.Write((char)Vals[0]);
                }
                else
                {
                    ConsoleOut.Write(Vals[0]);
                }
                break;
            case OperationCode.ReadInt:
                CheckLength(0, ToRun);
                Set(Plc, int.Parse(ReadConsole()));
                break;
            case OperationCode.ReadString:
                CheckLength(0, ToRun);
                if (ReadingProgress == "") { ReadingProgress = ReadConsole(); }
                Set(Plc, ReadingProgress[0]); ReadingProgress = ReadingProgress.Substring(1);
                break;
            case OperationCode.ReadDone:
                CheckLength(0, ToRun);
                Set(Plc, BoolToInt(ReadingProgress == ""));
                break;
            case OperationCode.ExceptHandle:
                CheckLength(0, 1, ToRun);
                if (Vals.Count == 0)
                {
                    throw new Exception("Thrown exception: \"" + ExceptionProgress + "\"");
                }
                else if (ToRun.EntryPointers[0].Char)
                {
                    ExceptionProgress += (char)Vals[0];
                }
                else
                {
                    ExceptionProgress += Vals[0];
                }
                break;
            case OperationCode.HasVal:
                CheckLength(1, ToRun);
                Set(Plc, HasValue(ToRun.EntryPointers[0]));
                break;
            case OperationCode.Remove:
                CheckLength(1, ToRun);
                Remove(ToRun.EntryPointers[0]);
                break;
            case OperationCode.SubRead:
                CheckLength(0, ToRun);
                if(SubmissionProgress.Length == 0) { throw new Exception("Read from null length submission."); }
                Set(Plc, SubmissionProgress[0]);
                SubmissionProgress = SubmissionProgress[1..];
                break;
            case OperationCode.SubDone:
                CheckLength(0, ToRun);
                Set(Plc, BoolToInt(SubmissionProgress == ""));
                break;
            case OperationCode.Plus:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] + Vals[1]);
                break;
            case OperationCode.Minus:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] - Vals[1]);
                break;
            case OperationCode.Multiply:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] * Vals[1]);
                break;
            case OperationCode.Divide:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] / Vals[1]);
                break;
            case OperationCode.Modulo:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] % Vals[1]);
                break;
            case OperationCode.UnaryMinus:
                CheckLength(1, ToRun);
                Set(Plc, -Vals[0]);
                break;
            case OperationCode.And:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] & Vals[1]);
                break;
            case OperationCode.Or:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] | Vals[1]);
                break;
            case OperationCode.Xor:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] ^ Vals[1]);
                break;
            case OperationCode.BitwiseNot:
                CheckLength(1, ToRun);
                Set(Plc, -Vals[0] - 1);
                break;
            case OperationCode.LogicalNot:
                CheckLength(1, ToRun);
                Set(Plc, 1 - Vals[0]);
                break;
            case OperationCode.Random:
                CheckLength(1, ToRun);
                Set(Plc, TheDice.Next(Vals[0]));
                break;
            case OperationCode.Equal:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] == Vals[1]);
                break;
            case OperationCode.NotEqual:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] != Vals[1]);
                break;
            case OperationCode.Greater:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] > Vals[1]);
                break;
            case OperationCode.GreaterOrEqual:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] >= Vals[1]);
                break;
            case OperationCode.Lesser:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] < Vals[1]);
                break;
            case OperationCode.LesserOrEqual:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] <= Vals[1]);
                break;
        }
    }

    private string ReadConsole()
    {
        string? Input;
        do
        {
            Input = ConsoleIn.ReadLine();
        } while (Input == null);

        return Input;
    }

    private void CheckLength(int CorrectL, Operation ToCheck)
    {
        if (CorrectL != ToCheck.EntryPointers.Count)
        {
            throw new Exception("Gave insufficient/too many pointers or values.");
        }
    }

    private void CheckLength(int CorrectL, int CorrectL2, Operation ToCheck)
    {
        if (CorrectL != ToCheck.EntryPointers.Count && CorrectL2 != ToCheck.EntryPointers.Count)
        {
            throw new Exception("Gave insufficient/too many pointers or values.");
        }
    }
}