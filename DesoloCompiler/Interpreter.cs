namespace DesoloCompiler;

public class Interpreter(List<IFileLine> TheCode, TextReader ConsoleIn, TextWriter ConsoleOut)
{
    private readonly Dictionary<int, int> QuickVars = new Dictionary<int,int>();
    private readonly Tree Vars = new();
    private readonly Stack<StackPart> CallStack = new();
    private StackPart Cached;
    private string ExceptionProgress = "";
    private string ReadingProgress = "";
    private string SubmissionProgress = "";
    private int PReturn(int Place)
    {
        return QuickVars.GetValueOrDefault(Place, 0);
        return Vars.Return(Place).GetValueOrDefault();
    }
    private void UpdateCache()
    {
        Cached = CallStack.ReadTop();
    }
    private bool PHasValue(int Place)
    {
        return QuickVars.TryGetValue(Place, out int val);
        return Vars.Return(Place) != null;
    }

    private int Read(Pointer ToRead)
    {
        return ToRead.type switch
        {
            PointerType.Value => ToRead.PointerV,
            PointerType.Pointer => PReturn(ToRead.PointerV),
            PointerType.Array => PReturn(PReturn(ToRead.PointerV)),
            PointerType.Function => Cached.PassedOn[ToRead.PointerV].GetValueOrDefault(),
            PointerType.FunctionArray => PReturn(Cached.PassedOn[ToRead.PointerV].GetValueOrDefault()),
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
            PointerType.Function => ToRead.PointerV >= 0 && ToRead.PointerV < Cached.PassedOn.Length && Cached.PassedOn[ToRead.PointerV] != null,
            PointerType.FunctionArray => PHasValue(Cached.PassedOn[ToRead.PointerV].GetValueOrDefault()),
            _ => throw new Exception("Invalid pointer type")
        };
    }
    private void PRemove(int Place)
    {
        QuickVars.Remove(Place);
        return;
        Vars.Remove(Place);
    }
    private void Remove(Pointer ToRemove)
    {
        switch (ToRemove.type)
        {
            case PointerType.Pointer:
                PRemove(ToRemove.PointerV);
                break;
            case PointerType.Array:
                PRemove(PReturn(ToRemove.PointerV));
                break;
            case PointerType.Function:
                Cached.PassedOn[ToRemove.PointerV] = null;
                break;
            case PointerType.FunctionArray:
                PRemove(Cached.PassedOn[ToRemove.PointerV].GetValueOrDefault());
                break;
            case PointerType.Value:
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
                Cached.PassedOn[ToSet.PointerV] = Value;
                break;
            case PointerType.FunctionArray:
                Set(Cached.PassedOn[ToSet.PointerV].GetValueOrDefault(), Value);
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
        QuickVars[ToSet] = Value;
        return;
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
                    CallStack.Push(new StackPart(PointerToInt2(Fc.EntryPointers, Fc.RequiredAmount), Pointer));
                    UpdateCache();
                    Pointer = JsToInt(Fc.RunLine) - 1;
                    break;
                case Return Rt:
                    int ReadReturnValue = 0;
                    if (Rt.ReturnVal != null)
                    {
                        ReadReturnValue = Read(Rt.ReturnVal.Value);
                    }
                    Pointer = CallStack.Pop().ReturnLine;
                    UpdateCache();
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

    private int[] PointerToInt(Pointer[] ToConvert)
    {
        int[] Returner = new int[ToConvert.Length];
        for (int i = 0; i < ToConvert.Length; i++)
        {
            Returner[i] = Read(ToConvert[i]);
        }
        return Returner;
    }
    private int?[] PointerToInt2(Pointer[] ToConvert, int TotalLength)
    {
        int?[] Returner = new int?[TotalLength];
        for (int i = 0; i < ToConvert.Length; i++)
        {
            Returner[i] = Read(ToConvert[i]);
        }
        return Returner;
    }
    private void RunOperation(Operation ToRun, Random TheDice)
    {
        Pointer Plc = ToRun.PointerDest;
        int[] Vals = PointerToInt(ToRun.EntryPointers);
        switch (ToRun.OpCode)
        {
            case OperationCode.Assign:
                Set(Plc, Vals[0]);
                break;
            case OperationCode.WriteS:
                if (ToRun.EntryPointers[0].Char)
                {
                    ConsoleOut.Write((char)Vals[0]);
                }
                else
                {
                    ConsoleOut.Write(Vals[0]);
                }
                break;
            case OperationCode.NewLine:
                ConsoleOut.WriteLine();
                break;
            case OperationCode.ReadInt:
                Set(Plc, int.Parse(ReadConsole()));
                break;
            case OperationCode.ReadString:
                if (ReadingProgress == "") { ReadingProgress = ReadConsole(); }
                Set(Plc, ReadingProgress[0]); ReadingProgress = ReadingProgress.Substring(1);
                break;
            case OperationCode.ReadDone:
                Set(Plc, BoolToInt(ReadingProgress == ""));
                break;
            case OperationCode.ExceptAdd:
                if (ToRun.EntryPointers[0].Char)
                {
                    ExceptionProgress += (char)Vals[0];
                }
                else
                {
                    ExceptionProgress += Vals[0];
                }
                break;
            case OperationCode.ExceptSend:
                throw new Exception(ExceptionProgress);
            case OperationCode.HasVal:
                Set(Plc, HasValue(ToRun.EntryPointers[0]));
                break;
            case OperationCode.Remove:
                Remove(ToRun.EntryPointers[0]);
                break;
            case OperationCode.SubRead:
                Set(Plc, SubmissionProgress[0]);
                SubmissionProgress = SubmissionProgress[1..];
                break;
            case OperationCode.SubDone:
                Set(Plc, BoolToInt(SubmissionProgress == ""));
                break;
            case OperationCode.Plus:
                Set(Plc, Vals[0] + Vals[1]);
                break;
            case OperationCode.Minus:
                Set(Plc, Vals[0] - Vals[1]);
                break;
            case OperationCode.Multiply:
                Set(Plc, Vals[0] * Vals[1]);
                break;
            case OperationCode.Divide:
                Set(Plc, Vals[0] / Vals[1]);
                break;
            case OperationCode.Modulo:
                Set(Plc, Vals[0] % Vals[1]);
                break;
            case OperationCode.UnaryMinus:
                Set(Plc, -Vals[0]);
                break;
            case OperationCode.And:
                Set(Plc, Vals[0] & Vals[1]);
                break;
            case OperationCode.Or:
                Set(Plc, Vals[0] | Vals[1]);
                break;
            case OperationCode.Xor:
                Set(Plc, Vals[0] ^ Vals[1]);
                break;
            case OperationCode.BitwiseNot:
                Set(Plc, Vals[0] ^ -1);
                break;
            case OperationCode.LogicalNot:
                Set(Plc, 1 - Vals[0]);
                break;
            case OperationCode.Random:
                Set(Plc, TheDice.Next(Vals[0]));
                break;
            case OperationCode.LeftShift:
                Set(Plc, Vals[0] << Vals[1]);
                break;
            case OperationCode.RightShift:
                Set(Plc, Vals[0] >> Vals[1]);
                break;
            case OperationCode.Equal:
                Set(Plc, Vals[0] == Vals[1]);
                break;
            case OperationCode.NotEqual:
                Set(Plc, Vals[0] != Vals[1]);
                break;
            case OperationCode.Greater:
                Set(Plc, Vals[0] > Vals[1]);
                break;
            case OperationCode.GreaterOrEqual:
                Set(Plc, Vals[0] >= Vals[1]);
                break;
            case OperationCode.Lesser:
                Set(Plc, Vals[0] < Vals[1]);
                break;
            case OperationCode.LesserOrEqual:
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
}