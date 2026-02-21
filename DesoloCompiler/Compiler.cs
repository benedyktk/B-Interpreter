namespace DesoloCompiler;

public static class Compiler
{
    public static List<IFileLine> StringToCode(string Code)
    {
        var CodeLines = SplitIntoCodeLines(Code);
        var Collection = BuildCollection(CodeLines);
        return BuildFCode(CodeLines, Collection);
    }

    private static List<string> SplitIntoCodeLines(string Code)
    {
        string[] RawCodeLines = Code.Split(";");
        List<string> CodeLines = new List<string>();
        foreach (var L in RawCodeLines)
        {
            var Operative = new string(L.Where(C => !char.IsWhiteSpace(C)).ToArray());
            if (Operative.Length != 0 && Operative[0] != '/')
            {
                CodeLines.Add(Operative);
            }
        }

        return CodeLines;
    }

    private static LineInfo[] BuildCollection(List<string> CodeLines)
    {
        LineInfo[] Collection = new LineInfo[CodeLines.Count];
        int TotalId = 0;
        Stack<CType> ToUse = new Stack<CType>();
        for (int I = 0; I < CodeLines.Count; I++)
        {
            string Operative = CodeLines[I];
            switch (Operative[0])
            {
                case 'd' when Operative[6] != 'p' && Operative[6] != 'c':
                    ToUse.Push(CType.Define);
                    break;
                case 'c' when Operative[1] == 'i':
                    ToUse.Push(CType.If);
                    break;
                case 'c' when Operative[1] == 'e':
                    ToUse.Push(CType.Else);
                    break;
                case 'c' when Operative[1] == 'w':
                    ToUse.Push(CType.While);
                    break;
                case 'c' when Operative[1] == 'u':
                    ToUse.Push(CType.Until);
                    break;
            }
            Collection[I] = Operative[0] switch
            {
                'f' => new LineInfo(1, TotalId, CType.None, ToUse.Size),
                '#' => new LineInfo(1, TotalId, CType.None, ToUse.Size),
                'd' when Operative[6] == 'p' || Operative[6] == 'c' => new LineInfo(0, TotalId, CType.None, ToUse.Size),
                'd' => new LineInfo(0, TotalId, CType.Define, ToUse.Size),
                'c' when Operative[1] == 'i' => new LineInfo(1, TotalId, CType.If, ToUse.Size),
                'c' when Operative[1] == 'e' => new LineInfo(1, TotalId, CType.Else, ToUse.Size),
                'c' when Operative[1] == 'w' => new LineInfo(1, TotalId, CType.While, ToUse.Size),
                'c' when Operative[1] == 'u' => new LineInfo(1, TotalId, CType.Until, ToUse.Size),
                '{' => new LineInfo(Interpreter.BoolToInt(ToUse.ReadTop() == CType.Define), TotalId, ToUse.ReadTop(), ToUse.Size),
                '}' => new LineInfo(Interpreter.BoolToInt(ToUse.ReadTop() == CType.Define || ToUse.ReadTop() == CType.While || ToUse.ReadTop() == CType.Until), TotalId, ToUse.ReadTop(), ToUse.Size),
                _ => throw new Exception("Invalid line start at line " + Operative)
            };

            if (Operative[0] == '}')
            {
                ToUse.Pop();
            }

            TotalId += Collection[I].count;
        }

        return Collection;
    }

    private static List<IFileLine> BuildFCode(List<string> CodeLines, LineInfo[] Collection)
    {
        List<IFileLine> FCode = new List<IFileLine>();
        Dictionary<string, int> FuncToInt = new Dictionary<string, int>();
        Dictionary<string, int> PlaceToInt = new Dictionary<string, int>();
        Dictionary<int, Pointer> ConstValToPointer = new Dictionary<int, Pointer>();
        Dictionary<string, int> RequiredAms = new Dictionary<string, int>();
        Stack<FuncInfo> Function = new Stack<FuncInfo>();
        for (int I = 0; I < CodeLines.Count; I++)
        {
            string Operative = CodeLines[I];
            int ReadSpot = 0;
            switch (Operative[0])
            {
                case 'f':
                {
                    ReadSpot++;
                    var Str = ReadUntil(Operative, ref ReadSpot, '(');
                    List<Pointer> LInfo = new List<Pointer>();
                    string Jic = "";
                    if (Str == "except" || Str == "jump" || Str == "submit")
                    {
                        Jic = ReadUntil2(Operative, ref ReadSpot);
                        if (ReadSpot == Operative.Length)
                        {
                            ReadSpot -= 2;
                        }
                    }
                    while (PointerStart(Operative[ReadSpot]))
                    {
                        var Pnt = ReadPointer(Operative, ref ReadSpot);
                        FuncLimitCheck(Function, Pnt);
                        LInfo.Add(Pnt);
                        if (Operative[ReadSpot] != ')')
                        {
                            ReadUntil(Operative, ref ReadSpot, ',');
                        }
                    }
                    Pointer[] Info = LInfo.ToArray();
                    ReadUntil(Operative, ref ReadSpot, ')');
                    IFileLine FileLine = Str switch
                    {
                        "submit" when Info.Length == 0 => new StringLine(Jic.Replace('_', ' '), true),
                        "submit" => throw new Exception("Invalid amount of parameters in Return function at line " + Operative),
                        "return" when Info.Length == 0 => new Return(null),
                        "return" when Info.Length == 1 => new Return(Info[0]),
                        "return" => throw new Exception("Invalid amount of parameters in Return function at line " + Operative),
                        "write" when Info.Length != 1 => throw new Exception("Invalid amount of parameters in Write function at line " + Operative),
                        "write" => new Operation(Info, new Pointer(0, 0, false), OperationCode.WriteS),
                        "newline" when Info.Length != 0 => throw new Exception("Invalid amount of parameters in Newline function at line " + Operative),
                        "newline" => new Operation(Info, new Pointer(0, 0, false), OperationCode.NewLine),
                        "jump" when Info.Length == 0 => new Jump(new JumpString(Jic), new Pointer(1, 0, false), false),
                        "jump" when Info.Length == 1 => new Jump(new JumpString(Jic), Info[0], false),
                        "jump" => throw new Exception("Invalid amount of parameters in Jump function at line " + Operative),
                        "except" when Info.Length == 0 => new StringLine(Jic.Replace('_', ' '), false),
                        "except" => throw new Exception("Invalid amount of parameters in Except function at line " + Operative),
                        "terminate" when Info.Length != 0 => throw new Exception("Invalid amount of parameters in Terminate function at line " + Operative),
                        "terminate" => new Jump(new JumpSpotProper(-1), new Pointer(1, 0, false), false),
                        "exceptadd" when Info.Length != 1 => throw new Exception("Invalid amount of parameters in ExceptAdd function at line " + Operative),
                        "exceptadd" => new Operation(Info, new Pointer(0, 0, false), OperationCode.ExceptAdd),
                        "exceptsend" when Info.Length != 0 => throw new Exception("Invalid amount of parameters in ExceptSend function at line " + Operative),
                        "exceptsend" => new Operation(Info, new Pointer(0, 0, false), OperationCode.ExceptSend),
                        "remove" when Info.Length != 1 => throw new Exception("Invalid amount of parameters in Remove function at line " + Operative),
                        "remove" => new Operation(Info, new Pointer(0, 0, false), OperationCode.Remove),
                        "readint" or "readstring" or "readdone" or "subread" or "subdone" or "hasval" => throw new Exception("No return line of system function at line " + Operative),
                        _ => new FunctionCall(Info, 0, null, new JumpString(Str))
                    };
                    FCode.Add(FileLine);
                    break;
                }
                case '#':
                {
                    ReadSpot = 0;
                    var Destination = ReadPointer(Operative, ref ReadSpot);
                    FuncLimitCheck(Function, Destination);
                    if (Operative[ReadSpot] != '=')
                    {
                        throw new Exception("Invalid post-pointer symbol at line " + Operative);
                    }
                    ReadSpot++;
                    if (Operative[ReadSpot] == 'f')
                    {
                        ReadSpot++;
                        var Str = ReadUntil(Operative, ref ReadSpot, '(');
                        switch (Str)
                        {
                            case "readint":
                                FCode.Add(new Operation(new Pointer[0], Destination, OperationCode.ReadInt));
                                break;
                            case "readstring":
                                FCode.Add(new Operation(new Pointer[0], Destination, OperationCode.ReadString));
                                break;
                            case "subread":
                                FCode.Add(new Operation(new Pointer[0], Destination, OperationCode.SubRead));
                                break;
                            case "subdone":
                                FCode.Add(new Operation(new Pointer[0], Destination, OperationCode.SubDone));
                                break;
                            case "readdone":
                                FCode.Add(new Operation(new Pointer[0], Destination, OperationCode.ReadDone));
                                break;
                            case "submit":
                            case "return":
                            case "write":
                            case "newline":
                            case "jump":
                            case "except":
                            case "exceptadd":
                            case "exceptsend":
                            case "remove":
                                throw new Exception("Invalid existence of return line of system function at line " + Operative);
                            default:
                                List<Pointer> Info = new List<Pointer>();
                                while (PointerStart(Operative[ReadSpot]))
                                    {
                                    var Pnt = ReadPointer(Operative, ref ReadSpot);
                                    FuncLimitCheck(Function, Pnt);
                                    Info.Add(Pnt);
                                    if (Operative[ReadSpot] != ')')
                                    {
                                        ReadUntil(Operative, ref ReadSpot, ',');
                                    }
                                }
                                ReadUntil(Operative, ref ReadSpot, ')');
                                if (Str == "hasval")
                                {
                                    FCode.Add(new Operation(Info.ToArray(), Destination, OperationCode.HasVal));
                                }
                                else
                                {
                                    FCode.Add(new FunctionCall(Info.ToArray(), 0, Destination, new JumpString(Str)));
                                }
                                break;
                        }
                    }
                    else
                    {
                        var UOp = ReadUntil(Operative, ref ReadSpot, '#');
                        ReadSpot--;
                        var Pnt = ReadPointer(Operative, ref ReadSpot);
                        FuncLimitCheck(Function, Pnt);
                        List<Pointer> Arguments = [Pnt];
                        if (ReadSpot == Operative.Length)
                        {
                            FCode.Add(new Operation(Arguments.ToArray(), Destination, IdentifyUOp(UOp)));
                        }
                        else
                        {
                            var BOp = ReadUntil(Operative, ref ReadSpot, '#');
                            ReadSpot--;
                            var Pointer = ReadPointer(Operative, ref ReadSpot);
                            FuncLimitCheck(Function, Pointer);
                            Arguments.Add(Pointer);
                            FCode.Add(new Operation(Arguments.ToArray(), Destination, IdentifyBOp(BOp)));
                        }
                    }
                    break;
                }
                case 'd':
                {
                    var TillP = ReadUntil(Operative, ref ReadSpot, '(');
                    if (TillP == "defineconst")
                    {
                        var PointerA = ReadPointer(Operative, ref ReadSpot);
                        ReadSpot--;
                        ReadUntil(Operative, ref ReadSpot, ',');
                        var PointerB = ReadPointer(Operative, ref ReadSpot);
                        ReadSpot--;
                        ReadUntil(Operative, ref ReadSpot, ')');
                        if (PointerA.type != PointerType.Constant)
                        {
                            throw new Exception("Invalid non-constant definition at line " + Operative);
                        }
                        if (PointerB.type == PointerType.Function || PointerB.type == PointerType.FunctionArray || PointerB.type == PointerType.Constant)
                        {
                            throw new Exception("Attempted defintion of pointer as function-type pointer at line " + Operative);
                        }
                        ConstValToPointer.Add(PointerA.PointerV, PointerB);
                    }
                    else
                    {
                        var TillEp = ReadUntil(Operative, ref ReadSpot, ')');
                        switch (TillP)
                        {
                            case "define":
                                FuncToInt.Add(TillEp, FCode.Count + 1);
                                Function.Push(new FuncInfo(TillEp, 0));
                                break;
                            case "defineplace":
                                PlaceToInt.Add(TillEp, FCode.Count);
                                break;
                            default:
                                throw new Exception("Invalid define line at line " + Operative);
                        }
                    }
                    break;
                }
                case 'c':
                {
                    ReadSpot++;
                    var ConditionalType = ReadUntil(Operative, ref ReadSpot, '(');
                    CType CType = ConditionalType switch
                    {
                        "if" => CType.If,
                        "else" => CType.Else,
                        "while" => CType.While,
                        "until" => CType.Until,
                        _ => throw new Exception("Invalid conditional after c at line " + Operative)
                    };
                    int ClosingLine = -1;
                    int StackSize = Collection[I].stacksize;
                    for (int J = I; J < CodeLines.Count && ClosingLine == -1; J++)
                    {
                        if (CodeLines[J][0] == '}' && Collection[J].stacksize == StackSize && CType == Collection[I].ctype)
                        {
                            ClosingLine = Collection[J].ID;
                        }
                    }
                    if (ClosingLine == -1)
                    {
                        throw new Exception("No ending parentheses of conditional at line " + Operative);
                    }
                    var Pnt = ReadPointer(Operative, ref ReadSpot);
                    FuncLimitCheck(Function, Pnt);
                    ReadUntil(Operative, ref ReadSpot, ')');
                    FCode.Add(new Jump(new JumpSpotProper(ClosingLine + Interpreter.BoolToInt(CType is CType.While or CType.Until)), Pnt, CType != CType.Else && CType != CType.Until));
                    break;
                }
                case '{' when Operative.Length == 1:
                {
                    if (Collection[I].ctype == CType.Define)
                    {
                        FCode.Add(new StringLine("Entered function body without calling it.", false));
                    }

                    break;
                }
                case '}' when Operative.Length == 1:
                {
                    if (Collection[I].ctype == CType.Define)
                    {
                        FCode.Add(new Return(null));
                        RequiredAms.Add(Function.ReadTop().FuncName, Function.ReadTop().PointerMax);
                    }
                    if (Collection[I].ctype == CType.While || Collection[I].ctype == CType.Until)
                    {
                        CType Current = Collection[I].ctype;
                        int StackSize = Collection[I].stacksize;
                        int ClosingLine = -1;
                        for (int J = I; J >= 0 && ClosingLine == -1; J--)
                        {
                            if (CodeLines[J][0] == 'c' && Collection[J].stacksize == StackSize && Current == Collection[J].ctype)
                            {
                                ClosingLine = Collection[J].ID;
                            }
                        }
                        if (ClosingLine == -1)
                        {
                            throw new Exception("No ending parentheses of conditional at line " + Operative);
                        }
                        FCode.Add(new Jump(new JumpSpotProper(ClosingLine), new Pointer(1, 0, false), false));
                    }
                    break;
                }
                default:
                    throw new Exception("Invalid line start at line " + Operative);
            }
        }
        PatchFunctionsAndJumps(FCode, FuncToInt, PlaceToInt, ConstValToPointer, RequiredAms);
        return FCode;
    }
    public static void FuncLimitCheck(Stack<FuncInfo> ToTry, Pointer Importance)
    {
        if ((Importance.type == PointerType.Function || Importance.type == PointerType.FunctionArray) && Importance.PointerV + 1 > ToTry.ReadTop().PointerMax)
        {
            ToTry.Main[ToTry.Main.Count - 1] = new FuncInfo(ToTry.ReadTop().FuncName, Importance.PointerV + 1);
        }
    }
    private static void PatchFunctionsAndJumps(List<IFileLine> FCode, Dictionary<string, int> FuncToInt, Dictionary<string, int> PlaceToInt, Dictionary<int, Pointer> ConstValToPointer, Dictionary<string, int> RequiredAms)
    {
        for (int I = 0; I < FCode.Count; I++)
        {
            switch (FCode[I])
            {
                case FunctionCall Fc:
                    for (int i = 0; i < Fc.EntryPointers.Length; i++)
                    {
                        Fc.EntryPointers[i] = ConstCheck(Fc.EntryPointers[i], ConstValToPointer);
                    }
                    if(Fc.ExitPointer != null)
                    {
                        Fc = Fc with { ExitPointer = ConstCheck(Fc.ExitPointer.Value, ConstValToPointer) };
                    }
                    string Needed = "";
                    switch (Fc.RunLine)
                    {
                        case JumpString Js:
                            Needed = Js.Place;
                            break;
                        case JumpSpotProper:
                            throw new Exception("Int functioncall jumpspot somehow got generated inside compiler.");
                    }
                    if (FuncToInt.TryGetValue(Needed, out var Val))
                    {
                        Fc = Fc with { RunLine = new JumpSpotProper(Val) };
                    }
                    else
                    {
                        throw new Exception("Called function named " + Needed + " has not been defined.");
                    }
                    if (RequiredAms.TryGetValue(Needed, out Val))
                    {
                        Fc = Fc with { RequiredAmount = Val };
                    }
                    else
                    {
                        throw new Exception("Called function named " + Needed + " has not been closed properly.");
                    }
                    FCode[I] = Fc;
                    break;
                case Jump Jmp:
                    Jmp = Jmp with { ConditionPointer = ConstCheck(Jmp.ConditionPointer, ConstValToPointer) };
                    string? ToGet = null;
                    switch (Jmp.Spot)
                    {
                        case JumpString Js:
                            ToGet = Js.Place;
                            break;
                    }
                    if (ToGet != null)
                    {
                        if (PlaceToInt.TryGetValue(ToGet, out Val))
                        {
                            Jmp = Jmp with { Spot = new JumpSpotProper(Val) };
                        }
                        else
                        {
                            throw new Exception("Jumped-to place named " + ToGet + " has not been defined.");
                        }
                    }
                    FCode[I] = Jmp;
                    break;
                case Operation Op:
                    for (int i = 0; i < Op.EntryPointers.Length; i++)
                    {
                        Op.EntryPointers[i] = ConstCheck(Op.EntryPointers[i], ConstValToPointer);
                    }
                    Op = Op with { PointerDest = ConstCheck(Op.PointerDest, ConstValToPointer) };
                    FCode[I] = Op;
                    break;
                case Return Rt:
                    if (Rt.ReturnVal != null)
                    {
                        FCode[I] = Rt with { ReturnVal = ConstCheck(Rt.ReturnVal.Value, ConstValToPointer) };
                    }
                    break;
            }
        }
    }
    private static Pointer ConstCheck(Pointer ToCheck, Dictionary<int, Pointer> ConstValToPointer)
    {
        if (ToCheck.type == PointerType.Constant)
        {
            if (ConstValToPointer.TryGetValue(ToCheck.PointerV, out var Val))
            {
                return Val;
            }
            else
            {
                throw new Exception("Constant pointer index " + ToCheck.PointerV + " has not been defined.");
            }
        }
        return ToCheck;
    }
    private static Pointer ReadPointer(string Code, ref int ReadSpot)
    {
        ReadSpot++;
        if (ReadSpot + 1 < Code.Length && Code[ReadSpot + 1] == '\"')
        {
            ReadSpot += 2;
            return new Pointer(Code[ReadSpot - 2], 0, true);
        }
        PointerType PointerValue = PointerType.Value;
        switch (Code[ReadSpot])
        {
            case 'f' when ReadSpot + 5 <= Code.Length && Code[ReadSpot..(ReadSpot + 5)] == "false":
                ReadSpot += 5;
                return new Pointer(0, PointerType.Value, false);
            case 't' when ReadSpot + 4 <= Code.Length && Code[ReadSpot..(ReadSpot + 4)] == "true":
                ReadSpot += 4;
                return new Pointer(1, PointerType.Value, false);
            case 'f' when Code[ReadSpot + 1] == 'a':
                PointerValue = PointerType.FunctionArray;
                ReadSpot += 2;
                break;
            case 'p':
                PointerValue = PointerType.Pointer;
                ReadSpot++;
                break;
            case 'a':
                PointerValue = PointerType.Array;
                ReadSpot++;
                break;
            case 'f':
                PointerValue = PointerType.Function;
                ReadSpot++;
                break;
            case 'c':
                PointerValue = PointerType.Constant;
                ReadSpot++;
                break;
        }
        if (!Number(Code[ReadSpot]))
        {
            throw new Exception("Invalid pointer reference at line " + Code);
        }
        int Start = ReadSpot;
        while (ReadSpot < Code.Length && Number(Code[ReadSpot]) && !(Code[ReadSpot] == '-' && ReadSpot != Start))
        {
            ReadSpot++;
        }
        var CreatedNum = Code[Start..ReadSpot];
        if (ReadSpot < Code.Length && Code[ReadSpot] == '\"')
        {
            ReadSpot++;
            return new Pointer(int.Parse(CreatedNum), PointerValue, true);
        }
        return new Pointer(int.Parse(CreatedNum), PointerValue, false);
    }

    private static string ReadUntil(string Code, ref int ReadSpot, char C)
    {
        var Start = ReadSpot;
        var Next = Code.IndexOf(C, ReadSpot);
        if (Next == -1)
        {
            throw new Exception("Unexpected end of line at line " + Code);
        }

        ReadSpot = Next + 1;
        return Code[Start..Next];
    }

    private static string ReadUntil2(string Code, ref int ReadSpot)
    {
        var Start = ReadSpot;
        while (ReadSpot < Code.Length && Code[ReadSpot] != ')' && Code[ReadSpot] != ',')
        {
            ReadSpot++;
        }
        if (ReadSpot == Code.Length)
        {
            throw new Exception("Unexpected end of line at line " + Code);
        }

        ReadSpot++;
        return Code[Start..(ReadSpot - 1)];
    }

    private static bool Number(char C)
    {
        return C is '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' or '-';
    }

    private static bool PointerStart(char C)
    {
        return C == '#';
    }

    private static OperationCode IdentifyUOp(string UOp)
    {
        return UOp switch
        {
            "-" => OperationCode.UnaryMinus,
            "i!" => OperationCode.BitwiseNot,
            "!" => OperationCode.LogicalNot,
            "r" => OperationCode.Random,
            "" => OperationCode.Assign,
            _ => throw new Exception("Invalid unary operation name called " + UOp)
        };
    }

    private static OperationCode IdentifyBOp(string BOp)
    {
        return BOp switch
        {
            "+" => OperationCode.Plus,
            "-" => OperationCode.Minus,
            "*" => OperationCode.Multiply,
            "/" => OperationCode.Divide,
            "%" => OperationCode.Modulo,
            "&" => OperationCode.And,
            "|" => OperationCode.Or,
            "^" => OperationCode.Xor,
            "<<" => OperationCode.LeftShift,
            ">>" => OperationCode.RightShift,
            "==" => OperationCode.Equal,
            "!=" => OperationCode.NotEqual,
            ">" => OperationCode.Greater,
            ">=" => OperationCode.GreaterOrEqual,
            "<" => OperationCode.Lesser,
            "<=" => OperationCode.LesserOrEqual,
            _ => throw new Exception("Invalid binary operation name called " + BOp)
        };
    }
}