namespace DesoloCompiler;

public interface IFileLine;

public enum OperationCode
{
    SubDone = -12,
    SubRead = -11,
    Remove = -10,
    HasVal = -9,
    ExceptSend = -8,
    ExceptAdd = -7,
    ReadDone = -6,
    ReadString = -5,
    ReadInt = -4,
    NewLine = -3,
    WriteS = -2,
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
    LeftShift = 12,
    RightShift = 13,
    Equal = 16,
    NotEqual = 17,
    Greater = 18,
    GreaterOrEqual = 19,
    Lesser = 20,
    LesserOrEqual = 21,
}

public record Operation(Pointer[] EntryPointers, Pointer PointerDest, OperationCode OpCode) : IFileLine
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
            case OperationCode.ExceptSend:
                return "fexceptsend()";
            case OperationCode.ExceptAdd:
                return "fexceptadd(" + EntryPointers[0] + ")";
            case OperationCode.ReadDone:
                return PointerDest + " = freaddone()";
            case OperationCode.ReadString:
                return PointerDest + " = freadstring()";
            case OperationCode.ReadInt:
                return PointerDest + " = freadint()";
            case OperationCode.NewLine:
                return "fnewline()";
            case OperationCode.WriteS:
                return "fwrite(" + EntryPointers[0] + ")";
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
            case OperationCode.LeftShift:
                return PointerDest + " = " + EntryPointers[0] + " << " + EntryPointers[1];
            case OperationCode.RightShift:
                return PointerDest + " = " + EntryPointers[0] + " >> " + EntryPointers[1];
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

public record FunctionCall(Pointer[] EntryPointers, int RequiredAmount, Pointer? ExitPointer, IJumpSpot RunLine) : IFileLine
{
    public override string ToString()
    {
        string Insides = "";
        for (int i = 0; i < RequiredAmount; i++)
        {
            if (i < EntryPointers.Length)
            {
                Insides += (EntryPointers[i] + ",");
            }
            else
            {
                Insides += "null,";
            }
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