namespace DesoloCompiler
{
    public class Compiler
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
            for (int i = 0; i < RawCodeLines.Length; i++)
            {
                string Operative = RawCodeLines[i];
                Operative = new string(Operative.Where(c => !char.IsWhiteSpace(c)).ToArray());
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
            int TotalID = 0;
            OrderStack ToUse = new OrderStack();
            for (int i = 0; i < CodeLines.Count; i++)
            {
                string Operative = CodeLines[i];

                switch (Operative[0])
                {
                    case 'd':
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

                Collection[i] = Operative[0] switch
                {
                    'f' or '#' => new LineInfo(1, TotalID, CType.None, ToUse.Size),
                    'd' => new LineInfo(0, TotalID, CType.Define, ToUse.Size),
                    'c' when Operative[1] == 'i' => new LineInfo(1, TotalID, CType.If, ToUse.Size),
                    'c' when Operative[1] == 'e' => new LineInfo(1, TotalID, CType.Else, ToUse.Size),
                    'c' when Operative[1] == 'w' => new LineInfo(1, TotalID, CType.While, ToUse.Size),
                    'c' when Operative[1] == 'u' => new LineInfo(1, TotalID, CType.Until, ToUse.Size),
                    '{' => new LineInfo(Interpreter.BoolToInt(ToUse.ReadTop() == CType.Define), TotalID, ToUse.ReadTop(), ToUse.Size),
                    '}' => new LineInfo(Interpreter.BoolToInt(ToUse.ReadTop() == CType.Define || ToUse.ReadTop() == CType.While || ToUse.ReadTop() == CType.Until), TotalID, ToUse.ReadTop(), ToUse.Size),
                    _ => throw new InvalidOperationException()
                };

                if (Operative[0] == '}')
                {
                    ToUse.Pop();
                }

                TotalID += Collection[i].count;
            }

            return Collection;
        }

        private static List<IFileLine> BuildFCode(List<string> CodeLines, LineInfo[] Collection)
        {
            List<IFileLine> FCode = new List<IFileLine>();
            Dictionary<string, int> FuncToInt = new Dictionary<string, int>();
            Dictionary<string, int> PlaceToInt = new Dictionary<string, int>();

            for (int i = 0; i < CodeLines.Count; i++)
            {
                string Operative = CodeLines[i];
                int ReadSpot = 0;
                switch (Operative[0])
                {
                    case 'f':
                    {
                        ReadSpot++;
                        var Full = ReadUntil(Operative, ReadSpot, '(');
                        ReadSpot = Full.rs;
                        List<Pointer> Info = new List<Pointer>();
                        string JIC = "";
                        if (Full.str == "except" || Full.str == "jump" || Full.str == "submit")
                        {
                            (JIC, ReadSpot) = ReadUntil2(Operative, ReadSpot);
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
                        IFileLine FileLine = Full.str switch
                        {
                            "submit" when Info.Count == 0 => new StringLine(JIC.Replace('_', ' '), true),
                            "submit" => throw new Exception("Invalid amount of parameters in Return function."),
                            "return" when Info.Count == 0 => new Return(null),
                            "return" when Info.Count == 1 => new Return(Info[0]),
                            "return" => throw new Exception("Invalid amount of parameters in Return function."),
                            "write" when Info.Count > 1 => throw new Exception("Invalid amount of parameters in Write function."),
                            "write" => new Operation(Info, new Pointer(0, 0, false), -2),
                            "jump" when Info.Count == 0 => new Jump(new JumpString(JIC), new Pointer(1, 0, false), false),
                            "jump" when Info.Count == 1 => new Jump(new JumpString(JIC), Info[0], false),
                            "jump" => throw new Exception("Invalid amount of parameters in Jump function."),
                            "except" when Info.Count == 0 => new StringLine($@"""{JIC.Replace('_', ' ')}"" at line {i}", false),
                            "except" => throw new Exception("Invalid amount of parameters in Except function."),
                            "terminate" when Info.Count != 0 => throw new Exception("Invalid amount of parameters in Terminate function."),
                            "terminate" => new Jump(new JumpSpotProper(-1), new Pointer(1, 0, false), false),
                            "exceptadd" when Info.Count != 1 => throw new Exception("Invalid amount of parameters in ExceptAdd function."),
                            "exceptadd" => new Operation(Info, new Pointer(0, 0, false), -6),
                            "exceptsend" when Info.Count != 0 => throw new Exception("Invalid amount of parameters in ExceptSend function."),
                            "exceptsend" => new Operation(Info, new Pointer(0, 0, false), -6),
                            "remove" when Info.Count != 1 => throw new Exception("Invalid amount of parameters in Remove function."),
                            "remove" => new Operation(Info, new Pointer(0, 0, false), -8),
                            "readint" or "readstring" or "readdone" or "subread" or "subdone" or "hasval" => throw new Exception("No return line of system function"),
                            _ => new FunctionCall(Info, null, new JumpString(Full.str))
                        };
                        FCode.Add(FileLine);
                        break;
                    }
                    case '#':
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
                                    FCode.Add(new Operation(new List<Pointer>(), Destination, -3));
                                    break;
                                case "readstring":
                                    FCode.Add(new Operation(new List<Pointer>(), Destination, -4));
                                    break;
                                case "subread":
                                    FCode.Add(new Operation(new List<Pointer>(), Destination, -9));
                                    break;
                                case "subdone":
                                    FCode.Add(new Operation(new List<Pointer>(), Destination, -10));
                                    break;
                                case "readdone":
                                    FCode.Add(new Operation(new List<Pointer>(), Destination, -5));
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

                        break;
                    }
                    case 'd':
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

                        break;
                    }
                    case 'c':
                    {
                        var TillP = ReadUntil(Operative, ReadSpot + 1, '(');
                        ReadSpot = TillP.rs; string ConditionalType = TillP.str;
                        CType ctype = CType.None;
                        switch(TillP.str)
                        {
                            case "if":
                                ctype = CType.If; break;
                            case "else":
                                ctype = CType.Else; break;
                            case "while":
                                ctype = CType.While; break;
                            case "until":
                                ctype = CType.Until; break;
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
                        FCode.Add(new Jump(new JumpSpotProper(ClosingLine + Interpreter.BoolToInt(ctype == CType.While || ctype == CType.Until)), Conditional.pnt, ctype != CType.Else && ctype != CType.Until));
                        break;
                    }
                    case '{' when Operative.Length == 1:
                    {
                        if (Collection[i].ctype == CType.Define)
                        {
                            FCode.Add(new StringLine("Entered function body without calling it.", false));                        
                        }

                        break;
                    }
                    case '}' when Operative.Length == 1:
                    {
                        if (Collection[i].ctype == CType.Define)
                        {
                            FCode.Add(new Return(null));
                        }
                        if (Collection[i].ctype == CType.While || Collection[i].ctype == CType.Until)
                        {
                            CType Current = Collection[i].ctype;
                            int StackSize = Collection[i].stacksize;
                            int ClosingLine = -1;
                            for (int j = i; j >= 0 && ClosingLine == -1; j--)
                            {
                                if (CodeLines[j][0] == 'c' && Collection[j].stacksize == StackSize && Current == Collection[j].ctype)
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
                        if (ToGet != null)
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
        }

        private static (Pointer pnt, int rs) ReadPointer(string Code, int ReadSpot)
        {
            ReadSpot++;
            if (ReadSpot + 1 < Code.Length && Code[ReadSpot + 1] == '\"')
            {
                return (new Pointer(Code[ReadSpot], 0, true), ReadSpot + 2);
            }
            PointerType PointerValue = PointerType.Value;
            switch (Code[ReadSpot])
            {
                case 'f' when ReadSpot + 5 <= Code.Length && Code[ReadSpot..(ReadSpot + 5)] == "false":
                    return (new Pointer(0, PointerType.Value, false), ReadSpot + 5);
                case 't' when ReadSpot + 4 <= Code.Length && Code[ReadSpot..(ReadSpot + 4)] == "true":
                    return (new Pointer(1, PointerType.Value, false), ReadSpot + 4);
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

        private static (string str, int rs) ReadUntil(string Code, int ReadSpot, char c)
        {
            var next = Code.IndexOf(c, ReadSpot);
            if (next == -1)
            {
                throw new Exception("Unexpected end of line.");
            }

            return (Code[ReadSpot..next], next + 1);
        }

        private static (string str, int rs) ReadUntil2(string Code, int ReadSpot)
        {
            var start = ReadSpot;
            while (ReadSpot < Code.Length && Code[ReadSpot] != ')' && Code[ReadSpot] != ',')
            {
                ReadSpot++;
            }
            if (ReadSpot == Code.Length)
            {
                throw new Exception("Unexpected end of line.");
            }
            return (Code[start..ReadSpot], ReadSpot + 1);
        }

        private static bool Number(char c)
        {
            return c is '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' or '-';
        }

        private static bool PointerStart(char c)
        {
            return c == '#';
        }

        private static int IdentifyUOp(string UOp)
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

        private static int IdentifyBOp(string BOp)
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
}
