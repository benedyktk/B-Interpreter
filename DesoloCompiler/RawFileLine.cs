namespace DesoloCompiler;

public interface IFileLine;

public enum OperationCode
{
    SubDone = -10,
    SubRead = -9,
    Remove = -8,
    HasVal = -7,
    ExceptHandle = -6,
    ReadDone = -5,
    ReadString = -4,
    ReadInt = -3,
    Write = -2,
    Assign = -1,
    Plus = 0,
    Minus = 1,
    Multiply = 2,
    Divide = 3,
    Modulo = 4,
    UnaryMinus = 5,
    And = 6,
    Or = 7,
    Xor = 8,
    BitwiseNot = 9,
    LogicalNot = 10,
    Random = 11,
    Equal = 16,
    NotEqual = 17,
    Greater = 18,
    GreaterOrEqual = 19,
    Lesser = 20,
    LesserOrEqual = 21,
}

public record Operation(List<Pointer> EntryPointers, Pointer PointerDest, OperationCode OpCode) : IFileLine
{
    public override string ToString()
    {
        switch (OpCode)
        {
            case OperationCode.SubDone:
                return PointerDest + " = fsubdone()";
            case OperationCode.SubRead:
                return PointerDest + " = fsubread()";
            case OperationCode.Remove:
                return "fremove(" + EntryPointers[0] + ")";
            case OperationCode.HasVal:
                return PointerDest + " = fhasval(" + EntryPointers[0] + ")";
            case OperationCode.ExceptHandle:
                if (EntryPointers.Count == 0)
                {
                    return "fexceptsend()";
                }
                else
                {
                    return "fexceptadd(" + EntryPointers[0] + ")";
                }
            case OperationCode.ReadDone:
                return PointerDest + " = freaddone()";
            case OperationCode.ReadString:
                return PointerDest + " = freadstring()";
            case OperationCode.ReadInt:
                return PointerDest + " = freadint()";
            case OperationCode.Write:
                if (EntryPointers.Count == 0)
                {
                    return "fwrite()";
                }
                else
                {
                    return "fwrite(" + EntryPointers[0] + ")";
                }
            case OperationCode.Assign:
                return PointerDest + " = " + EntryPointers[0];
            case OperationCode.Plus:
                return PointerDest + " = " + EntryPointers[0] + " + " + EntryPointers[1];
            case OperationCode.Minus:
                return PointerDest + " = " + EntryPointers[0] + " - " + EntryPointers[1];
            case OperationCode.Multiply:
                return PointerDest + " = " + EntryPointers[0] + " * " + EntryPointers[1];
            case OperationCode.Divide:
                return PointerDest + " = " + EntryPointers[0] + " / " + EntryPointers[1];
            case OperationCode.Modulo:
                return PointerDest + " = " + EntryPointers[0] + " % " + EntryPointers[1];
            case OperationCode.UnaryMinus:
                return PointerDest + " = -" + EntryPointers[0];
            case OperationCode.And:
                return PointerDest + " = " + EntryPointers[0] + " & " + EntryPointers[1];
            case OperationCode.Or:
                return PointerDest + " = " + EntryPointers[0] + " | " + EntryPointers[1];
            case OperationCode.Xor:
                return PointerDest + " = " + EntryPointers[0] + " ^ " + EntryPointers[1];
            case OperationCode.BitwiseNot:
                return PointerDest + " = i!" + EntryPointers[0];
            case OperationCode.LogicalNot:
                return PointerDest + " = !" + EntryPointers[0];
            case OperationCode.Random:
                return PointerDest + " = r" + EntryPointers[0];
            case OperationCode.Equal:
                return PointerDest + " = " + EntryPointers[0] + " == " + EntryPointers[1];
            case OperationCode.NotEqual:
                return PointerDest + " = " + EntryPointers[0] + " != " + EntryPointers[1];
            case OperationCode.Greater:
                return PointerDest + " = " + EntryPointers[0] + " > " + EntryPointers[1];
            case OperationCode.GreaterOrEqual:
                return PointerDest + " = " + EntryPointers[0] + " >= " + EntryPointers[1];
            case OperationCode.Lesser:
                return PointerDest + " = " + EntryPointers[0] + " < " + EntryPointers[1];
            case OperationCode.LesserOrEqual:
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
        foreach (var P in EntryPointers)
        {
            Insides += P + ",";
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