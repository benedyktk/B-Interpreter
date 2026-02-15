namespace DesoloCompiler;

public class Interpreter
{
    Tree Vars;
    Stack CallStack;
    List<IFileLine> CodeBase;
    int Pointer;
    string ExceptionProgress;
    string ReadingProgress;
    string SubmissionProgress;
    TextReader Input;
    TextWriter Output;
    public Interpreter(List<IFileLine> TheCode, TextReader ConsoleIn, TextWriter ConsoleOut)
    {
        Vars = new Tree();
        CallStack = new Stack();
        CodeBase = TheCode;
        ExceptionProgress = ""; ReadingProgress = "";
        Input = ConsoleIn;
        Output = ConsoleOut;
    }
    public int PReturn(int Place)
    {
        if (Vars.Return(Place) == null)
        {
            return 0;
        }
        return Vars.Return(Place).Value;
    }
    public bool PHasValue(int Place)
    {
        return Vars.Return(Place) != null;
    }
    public int Read(Pointer ToRead)
    {
        switch (ToRead.type)
        {
            case PointerType.Value:
                return ToRead.PointerV;
            case PointerType.Pointer:
                return PReturn(ToRead.PointerV);
            case PointerType.Array:
                return PReturn(PReturn(ToRead.PointerV));
            case PointerType.Function:
                return CallStack.ReadTop().PassedOn[ToRead.PointerV];
            case PointerType.FunctionArray:
                return PReturn(CallStack.ReadTop().PassedOn[ToRead.PointerV]);
        }
        throw new Exception("Invalid pointer type");
    }
    public bool HasValue(Pointer ToRead)
    {
        switch (ToRead.type)
        {
            case PointerType.Value:
                return true;
            case PointerType.Pointer:
                return PHasValue(ToRead.PointerV);
            case PointerType.Array:
                return PHasValue(PReturn(ToRead.PointerV));
            case PointerType.Function:
                return ToRead.PointerV >= 0 && ToRead.PointerV < CallStack.ReadTop().PassedOn.Count;
            case PointerType.FunctionArray:
                return PHasValue(CallStack.ReadTop().PassedOn[ToRead.PointerV]);
        }
        throw new Exception("Invalid pointer type");
    }
    public void Remove(Pointer ToRemove)
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
        }
    }
    public void Set(Pointer ToSet, int Value)
    {
        switch (ToSet.type)
        {
            case PointerType.Pointer:
                TSet(ToSet.PointerV, Value);
                break;
            case PointerType.Array:
                TSet(PReturn(ToSet.PointerV), Value);
                break;
            case PointerType.Function:
                CallStack.ReadTop().PassedOn[ToSet.PointerV] = Value;
                break;
            case PointerType.FunctionArray:
                TSet(CallStack.ReadTop().PassedOn[ToSet.PointerV], Value);
                break;
        }
    }
    public int JSToInt(IJumpSpot noname)
    {
        switch (noname)
        {
            case JumpSpotProper jsp:
                return jsp.Place;
            case JumpString:
                throw new Exception("String function escaped compiler in unknown ways.");
        }
        throw new Exception("Yeah this is just impossible");
    }
    public void Set(Pointer ToSet, bool Value)
    {
        Set(ToSet, BoolToInt(Value));
    }
    public void TSet(int ToSet, int Value)
    {
        Vars.Set(ToSet, Value);
    }
    public static int BoolToInt(bool Val)
    {
        if (Val)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
    public bool IntToBool(int Val)
    {
        return Val != 0;
    }
    public void RunCode(int InitialPointer)
    {
        Random rnd = new Random();
        int Pointer = InitialPointer;
        while (Pointer < CodeBase.Count && Pointer >= 0)
        {
            switch (CodeBase[Pointer])
            {
                case Operation op:
                    RunOperation(op, rnd);
                    break;
                case Jump jp:
                    if (IntToBool(Read(jp.ConditionPointer)) ^ jp.Opposite)
                    {
                        Pointer = JSToInt(jp.Spot) - 1;
                    }
                    break;
                case FunctionCall fc:
                    CallStack.Push(new StackPart(PointerToInt(fc.EntryPointers), Pointer));
                    Pointer = JSToInt(fc.RunLine) - 1;
                    break;
                case Return r:
                    int ReadReturnValue = 0;
                    if (r.ReturnVal != null)
                    {
                        ReadReturnValue = Read(r.ReturnVal.Value);
                    }
                    Pointer = CallStack.Pop().ReturnLine;
                    if (r.ReturnVal != null)
                    {
                        FunctionCall fc = (FunctionCall)CodeBase[Pointer];
                        if (fc.ExitPointer != null)
                        {
                            Set(fc.ExitPointer.Value, ReadReturnValue);
                        }
                    }
                    break;
                case StringLine sl:
                    if(sl.Type)
                    {
                        SubmissionProgress += sl.Text;
                    }
                    else
                    {
                        throw new Exception(sl.Text);
                    }
                    break;
            }
            Pointer++;
        }
    }
    public List<int> PointerToInt(List<Pointer> ToConvert)
    {
        List<int> Returner = new List<int>();
        for (int i = 0; i < ToConvert.Count; i++)
        {
            Returner.Add(Read(ToConvert[i]));
        }
        return Returner;
    }
    public void RunOperation(Operation ToRun, Random TheDice)
    {
        Pointer Plc = ToRun.PointerDest;
        List<int> Vals = PointerToInt(ToRun.EntryPointers);
        switch (ToRun.OpCode)
        {
            case -1:
                CheckLength(1, ToRun);
                Set(Plc, Vals[0]);
                break;
            case -2:
                CheckLength(0, 1, ToRun);
                if (Vals.Count == 0)
                {
                    Output.WriteLine();
                }
                else
                {
                    if (ToRun.EntryPointers[0].Char)
                    {
                        Output.Write((char)Vals[0]);
                    }
                    else
                    {
                        Output.Write(Vals[0]);
                    }
                }
                break;
            case -3:
                CheckLength(0, ToRun);
                Set(Plc, int.Parse(Input.ReadLine()));
                break;
            case -4:
                CheckLength(0, ToRun);
                if (ReadingProgress == "") { ReadingProgress = Input.ReadLine(); }
                Set(Plc, (int)ReadingProgress[0]); ReadingProgress = ReadingProgress.Substring(1);
                break;
            case -5:
                CheckLength(0, ToRun);
                Set(Plc, BoolToInt(ReadingProgress == ""));
                break;
            case -6:
                CheckLength(0, 1, ToRun);
                if (Vals.Count == 0)
                {
                    throw new Exception("Thrown exception: \"" + ExceptionProgress + "\"");
                }
                else
                {
                    if (ToRun.EntryPointers[0].Char)
                    {
                        ExceptionProgress += (char)Vals[0];
                    }
                    else
                    {
                        ExceptionProgress += Vals[0];
                    }
                }
                break;
            case -7:
                CheckLength(1, ToRun);
                Set(Plc, HasValue(ToRun.EntryPointers[0]));
                break;
            case -8:
                CheckLength(1, ToRun);
                Remove(ToRun.EntryPointers[0]);
                break;
            case -9:
                CheckLength(0, ToRun);
                if(SubmissionProgress.Length == 0) { throw new Exception("Read from null length submission."); }
                Set(Plc, (int)SubmissionProgress[0]); SubmissionProgress = SubmissionProgress.Substring(1);
                break;
            case -10:
                CheckLength(0, ToRun);
                Set(Plc, BoolToInt(SubmissionProgress == ""));
                break;
            case 0:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] + Vals[1]);
                break;
            case 1:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] - Vals[1]);
                break;
            case 2:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] * Vals[1]);
                break;
            case 3:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] / Vals[1]);
                break;
            case 4:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] % Vals[1]);
                break;
            case 5:
                CheckLength(1, ToRun);
                Set(Plc, -Vals[0]);
                break;
            case 6:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] & Vals[1]);
                break;
            case 7:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] | Vals[1]);
                break;
            case 8:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] ^ Vals[1]);
                break;
            case 9:
                CheckLength(1, ToRun);
                Set(Plc, -Vals[0] - 1);
                break;
            case 10:
                CheckLength(1, ToRun);
                Set(Plc, 1 - Vals[0]);
                break;
            case 11:
                CheckLength(1, ToRun);
                Set(Plc, TheDice.Next(Vals[0]));
                break;
            case 16:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] == Vals[1]);
                break;
            case 17:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] != Vals[1]);
                break;
            case 18:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] > Vals[1]);
                break;
            case 19:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] >= Vals[1]);
                break;
            case 20:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] < Vals[1]);
                break;
            case 21:
                CheckLength(2, ToRun);
                Set(Plc, Vals[0] <= Vals[1]);
                break;
        }
    }
    public void CheckLength(int CorrectL, Operation ToCheck)
    {
        if (CorrectL != ToCheck.EntryPointers.Count)
        {
            throw new Exception("Gave insufficient/too many pointers or values.");
        }
    }
    public void CheckLength(int CorrectL, int CorrectL2, Operation ToCheck)
    {
        if (CorrectL != ToCheck.EntryPointers.Count && CorrectL2 != ToCheck.EntryPointers.Count)
        {
            throw new Exception("Gave insufficient/too many pointers or values.");
        }
    }
}