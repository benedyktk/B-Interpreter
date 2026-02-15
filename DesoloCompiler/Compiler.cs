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
                case 'd' when Operative[6] != 'p':
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
                'f' or '#' => new LineInfo(1, TotalId, CType.None, ToUse.Size),
                'd' when Operative[6] == 'p' => new LineInfo(0, TotalId, CType.None, ToUse.Size),
                'd' => new LineInfo(0, TotalId, CType.Define, ToUse.Size),
                'c' when Operative[1] == 'i' => new LineInfo(1, TotalId, CType.If, ToUse.Size),
                'c' when Operative[1] == 'e' => new LineInfo(1, TotalId, CType.Else, ToUse.Size),
                'c' when Operative[1] == 'w' => new LineInfo(1, TotalId, CType.While, ToUse.Size),
                'c' when Operative[1] == 'u' => new LineInfo(1, TotalId, CType.Until, ToUse.Size),
                '{' => new LineInfo(Interpreter.BoolToInt(ToUse.ReadTop() == CType.Define), TotalId, ToUse.ReadTop(), ToUse.Size),
                '}' => new LineInfo(Interpreter.BoolToInt(ToUse.ReadTop() == CType.Define || ToUse.ReadTop() == CType.While || ToUse.ReadTop() == CType.Until), TotalId, ToUse.ReadTop(), ToUse.Size),
                _ => throw new InvalidOperationException()
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
                    List<Pointer> Info = new List<Pointer>();
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
                        Info.Add(Pnt);
                        if (Operative[ReadSpot] != ')')
                        {
                            ReadUntil(Operative, ref ReadSpot, ',');
                        }
                    }
                    ReadUntil(Operative, ref ReadSpot, ')');
                    IFileLine FileLine = Str switch
                    {
                        "submit" when Info.Count == 0 => new StringLine(Jic.Replace('_', ' '), true),
                        "submit" => throw new Exception("Invalid amount of parameters in Return function."),
                        "return" when Info.Count == 0 => new Return(null),
                        "return" when Info.Count == 1 => new Return(Info[0]),
                        "return" => throw new Exception("Invalid amount of parameters in Return function."),
                        "write" when Info.Count > 1 => throw new Exception("Invalid amount of parameters in Write function."),
                        "write" => new Operation(Info, new Pointer(0, 0, false), OperationCode.Write),
                        "jump" when Info.Count == 0 => new Jump(new JumpString(Jic), new Pointer(1, 0, false), false),
                        "jump" when Info.Count == 1 => new Jump(new JumpString(Jic), Info[0], false),
                        "jump" => throw new Exception("Invalid amount of parameters in Jump function."),
                        "except" when Info.Count == 0 => new StringLine($@"""{Jic.Replace('_', ' ')}"" at line {I}", false),
                        "except" => throw new Exception("Invalid amount of parameters in Except function."),
                        "terminate" when Info.Count != 0 => throw new Exception("Invalid amount of parameters in Terminate function."),
                        "terminate" => new Jump(new JumpSpotProper(-1), new Pointer(1, 0, false), false),
                        "exceptadd" when Info.Count != 1 => throw new Exception("Invalid amount of parameters in ExceptAdd function."),
                        "exceptadd" => new Operation(Info, new Pointer(0, 0, false), OperationCode.ExceptHandle),
                        "exceptsend" when Info.Count != 0 => throw new Exception("Invalid amount of parameters in ExceptSend function."),
                        "exceptsend" => new Operation(Info, new Pointer(0, 0, false), OperationCode.ExceptHandle),
                        "remove" when Info.Count != 1 => throw new Exception("Invalid amount of parameters in Remove function."),
                        "remove" => new Operation(Info, new Pointer(0, 0, false), OperationCode.Remove),
                        "readint" or "readstring" or "readdone" or "subread" or "subdone" or "hasval" => throw new Exception("No return line of system function"),
                        _ => new FunctionCall(Info, null, new JumpString(Str))
                    };
                    FCode.Add(FileLine);
                    break;
                }
                case '#':
                {
                    ReadSpot = 0;
                    var Destination = ReadPointer(Operative, ref ReadSpot);
                    if (Operative[ReadSpot] != '=')
                    {
                        throw new Exception("Invalid post-pointer symbol.");
                    }
                    ReadSpot++;
                    if (Operative[ReadSpot] == 'f')
                    {
                        ReadSpot++;
                        var Str = ReadUntil(Operative, ref ReadSpot, '(');
                        switch (Str)
                        {
                            case "readint":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, OperationCode.ReadInt));
                                break;
                            case "readstring":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, OperationCode.ReadString));
                                break;
                            case "subread":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, OperationCode.SubRead));
                                break;
                            case "subdone":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, OperationCode.SubDone));
                                break;
                            case "readdone":
                                FCode.Add(new Operation(new List<Pointer>(), Destination, OperationCode.ReadDone));
                                break;
                            case "return":
                            case "write":
                            case "jump":
                            case "except":
                            case "exceptadd":
                            case "exceptsend":
                            case "remove":
                                throw new Exception("No return line of system function");
                            default:
                                List<Pointer> Info = new List<Pointer>();
                                while (PointerStart(Operative[ReadSpot]))
                                {
                                    var Pnt = ReadPointer(Operative, ref ReadSpot);
                                    Info.Add(Pnt);
                                    if (Operative[ReadSpot] != ')')
                                    {
                                        ReadUntil(Operative, ref ReadSpot, ',');
                                    }
                                }
                                ReadUntil(Operative, ref ReadSpot, ')');
                                if (Str == "hasval")
                                {
                                    FCode.Add(new Operation(Info, Destination, OperationCode.HasVal));
                                }
                                else
                                {
                                    FCode.Add(new FunctionCall(Info, Destination, new JumpString(Str)));
                                }
                                break;
                        }
                    }
                    else
                    {
                        var UOp = ReadUntil(Operative, ref ReadSpot, '#');
                        ReadSpot--;
                        var Pnt= ReadPointer(Operative, ref ReadSpot);

                        List<Pointer> Arguments = [Pnt];
                        if (ReadSpot == Operative.Length)
                        {
                            FCode.Add(new Operation(Arguments, Destination, IdentifyUOp(UOp)));
                        }
                        else
                        {
                            var BOp = ReadUntil(Operative, ref ReadSpot, '#');
                            ReadSpot--;
                            var Pointer = ReadPointer(Operative, ref ReadSpot);
                            Arguments.Add(Pointer);
                            FCode.Add(new Operation(Arguments, Destination, IdentifyBOp(BOp)));
                        }
                    }

                    break;
                }
                case 'd':
                {
                    var TillP = ReadUntil(Operative, ref ReadSpot, '(');
                    var TillEp = ReadUntil(Operative, ref ReadSpot, ')');
                    switch (TillP)
                    {
                        case "define":
                            FuncToInt.Add(TillEp, FCode.Count + 1);
                            break;
                        case "defineplace":
                            PlaceToInt.Add(TillEp, FCode.Count);
                            break;
                        default:
                            throw new Exception("Invalid define line.");
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
                        _ => throw new Exception("Invalid conditional after c.")
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
                        throw new Exception("No ending parentheses of conditional.");
                    }
                    var Pnt = ReadPointer(Operative, ref ReadSpot);
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
                            throw new Exception("No ending parentheses of conditional");
                        }
                        FCode.Add(new Jump(new JumpSpotProper(ClosingLine), new Pointer(1, 0, false), false));
                    }
                    break;
                }
                default:
                    throw new Exception("Invalid line start.");
            }
        }

        PatchFunctionsAndJumps(FCode, FuncToInt, PlaceToInt);

        return FCode;
    }

    private static void PatchFunctionsAndJumps(List<IFileLine> FCode, Dictionary<string, int> FuncToInt, Dictionary<string, int> PlaceToInt)
    {
        for (int I = 0; I < FCode.Count; I++)
        {
            switch (FCode[I])
            {
                case FunctionCall Fc:
                    string Needed = "";
                    switch (Fc.RunLine)
                    {
                        case JumpString Js:
                            Needed = Js.Place;
                            break;
                        case JumpSpotProper:
                            throw new Exception("Int functioncall jumpspot somehow got generated inside compiler.");
                    }
                    if (FuncToInt.TryGetValue(Needed, out var Value))
                    {
                        FCode[I] = Fc with { RunLine = new JumpSpotProper(Value) };
                    }
                    else
                    {
                        throw new Exception("Called function named " + Needed + " has not been defined.");
                    }
                    break;
                case Jump Jmp:
                    string? ToGet = null;
                    switch (Jmp.Spot)
                    {
                        case JumpString Js:
                            ToGet = Js.Place;
                            break;
                    }
                    if (ToGet != null)
                    {
                        if (PlaceToInt.TryGetValue(ToGet, out Value))
                        {
                            FCode[I] = Jmp with { Spot = new JumpSpotProper(Value) };
                        }
                        else
                        {
                            throw new Exception("Jumped-to place named " + ToGet + " has not been defined.");
                        }
                    }
                    break;
            }
        }
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
        }
        if (!Number(Code[ReadSpot]))
        {
            throw new Exception("Invalid pointer reference.");
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
            throw new Exception("Unexpected end of line.");
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
            throw new Exception("Unexpected end of line.");
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
            _ => throw new Exception("Invalid unary operation name.")
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
            "==" => OperationCode.Equal,
            "!=" => OperationCode.NotEqual,
            ">" => OperationCode.Greater,
            ">=" => OperationCode.GreaterOrEqual,
            "<" => OperationCode.Lesser,
            "<=" => OperationCode.LesserOrEqual,
            _ => throw new Exception("Invalid binary operation name.")
        };
    }
}