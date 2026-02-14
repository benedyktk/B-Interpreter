using DesoloCompiler;
using Xunit;

namespace B__Interpreter.Tests;

/// <summary>
/// Tests for the B# interpreter. Key syntax notes:
/// - Destination: #pN sets var[N] directly, #aN sets var[var[N]] (indirect)
/// - Source: #N is literal value N, #pN reads var[N], #aN reads var[var[N]]
/// - Conditionals (cif/celse/cwhile) take a single pointer, not expressions
/// - Functions: define(name) / ffuncname(args) / freturn(val)
/// - Jumps: defineplace(name) / fjump(label)() / fjump(label,#cond)()
/// </summary>
public class InterpreterTests : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextReader _originalIn;

    public InterpreterTests()
    {
        _originalOut = Console.Out;
        _originalIn = Console.In;
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetIn(_originalIn);
    }

    static string Run(string code, string? input = null)
    {
        Console.SetIn(new StringReader(input ?? ""));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var compiled = Compiler.StringToCode(code);
        var interpreter = new Interpreter(compiled);
        interpreter.RunCode(0);

        return writer.ToString();
    }

    // ===================== Arithmetic =====================

    [Fact]
    public void Addition()
    {
        var code = "#p0 = #3 + #7; fwrite(#p0);";
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void Subtraction()
    {
        var code = "#p0 = #10 - #4; fwrite(#p0);";
        Assert.Equal("6", Run(code));
    }

    [Fact]
    public void Multiplication()
    {
        var code = "#p0 = #6 * #7; fwrite(#p0);";
        Assert.Equal("42", Run(code));
    }

    [Fact]
    public void Division()
    {
        var code = "#p0 = #20 / #4; fwrite(#p0);";
        Assert.Equal("5", Run(code));
    }

    [Fact]
    public void Modulo()
    {
        var code = "#p0 = #17 % #5; fwrite(#p0);";
        Assert.Equal("2", Run(code));
    }

    [Fact]
    public void Negation()
    {
        var code = "#p0 = -#5; fwrite(#p0);";
        Assert.Equal("-5", Run(code));
    }

    // ===================== Bitwise =====================

    [Fact]
    public void BitwiseAnd()
    {
        // 0b1100 & 0b1010 = 0b1000 = 8
        var code = "#p0 = #12 & #10; fwrite(#p0);";
        Assert.Equal("8", Run(code));
    }

    [Fact]
    public void BitwiseOr()
    {
        // 0b1100 | 0b1010 = 0b1110 = 14
        var code = "#p0 = #12 | #10; fwrite(#p0);";
        Assert.Equal("14", Run(code));
    }

    [Fact]
    public void BitwiseXor()
    {
        // 0b1100 ^ 0b1010 = 0b0110 = 6
        var code = "#p0 = #12 ^ #10; fwrite(#p0);";
        Assert.Equal("6", Run(code));
    }

    [Fact]
    public void BitwiseInvert()
    {
        // i! flips all bits: -(n) - 1
        var code = "#p0 = i!#5; fwrite(#p0);";
        Assert.Equal("-6", Run(code));
    }

    [Fact]
    public void LogicalNot()
    {
        // !0 = 1-0 = 1, !1 = 1-1 = 0
        var code = "#p0 = !#0; fwrite(#p0); #p1 = !#1; fwrite(#p1);";
        Assert.Equal("10", Run(code));
    }

    // ===================== Comparisons =====================

    [Fact]
    public void Equality()
    {
        var code = "#p0 = #5 == #5; fwrite(#p0); #p1 = #5 == #3; fwrite(#p1);";
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void NotEqual()
    {
        var code = "#p0 = #5 != #3; fwrite(#p0); #p1 = #5 != #5; fwrite(#p1);";
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void GreaterThan()
    {
        var code = "#p0 = #5 > #3; fwrite(#p0); #p1 = #3 > #5; fwrite(#p1);";
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void LessThan()
    {
        var code = "#p0 = #3 < #5; fwrite(#p0); #p1 = #5 < #3; fwrite(#p1);";
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void GreaterOrEqual()
    {
        var code = "#p0 = #5 >= #5; fwrite(#p0); #p1 = #3 >= #5; fwrite(#p1);";
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void LessOrEqual()
    {
        var code = "#p0 = #5 <= #5; fwrite(#p0); #p1 = #5 <= #3; fwrite(#p1);";
        Assert.Equal("10", Run(code));
    }

    // ===================== Assignment & Variables =====================

    [Fact]
    public void SimpleAssignment()
    {
        var code = "#p0 = #42; fwrite(#p0);";
        Assert.Equal("42", Run(code));
    }

    [Fact]
    public void MultipleVariables()
    {
        var code = "#p0 = #10; #p1 = #20; #p2 = #p0 + #p1; fwrite(#p2);";
        Assert.Equal("30", Run(code));
    }

    [Fact]
    public void IndirectPointer()
    {
        // var[0] = 5, var[5] = 99, #a0 reads var[var[0]] = var[5] = 99
        var code = "#p0 = #5; #p5 = #99; #p3 = #a0; fwrite(#p3);";
        Assert.Equal("99", Run(code));
    }

    [Fact]
    public void IndirectSet()
    {
        // #a0 as destination sets var[var[0]] (indirect via Array pointer)
        // var[0] = 5, then #a0 sets var[var[0]] = var[5] = 99
        var code = "#p0 = #5; #a0 = #99; fwrite(#p5);";
        Assert.Equal("99", Run(code));
    }

    // ===================== Control Flow =====================

    [Fact]
    public void IfTrue()
    {
        // #true is literal 1; ci enters body when condition is truthy
        var code = "cif(#true); {; fwrite(#1); };";
        Assert.Equal("1", Run(code));
    }

    [Fact]
    public void IfFalse()
    {
        // #0 is literal 0; ci skips body when condition is falsy
        var code = "cif(#0); {; fwrite(#1); };";
        Assert.Equal("", Run(code));
    }

    [Fact]
    public void IfElse()
    {
        // cif(#0) skips if body, ce(#0) enters else body
        var code = "cif(#0); {; fwrite(#1); }; celse(#0); {; fwrite(#2); };";
        Assert.Equal("2", Run(code));
    }

    [Fact]
    public void IfElseTrueBranch()
    {
        // cif(#1) enters if body, ce(#1) skips else body
        var code = "cif(#1); {; fwrite(#1); }; celse(#1); {; fwrite(#2); };";
        Assert.Equal("1", Run(code));
    }

    [Fact]
    public void WhileLoop()
    {
        // Count from 0 to 4
        var code = """
            #p0 = #0;
            #p10 = #p0 < #5;
            cwhile(#p10);
            {;
                fwrite(#p0);
                #p0 = #p0 + #1;
                #p10 = #p0 < #5;
            };
        """;
        Assert.Equal("01234", Run(code));
    }

    [Fact]
    public void NestedWhile()
    {
        // Outer loops 2x, inner loops 3x => 6 total increments
        var code = """
            #p0 = #0;
            #p2 = #0;
            #p20 = #p0 < #2;
            cwhile(#p20);
            {;
                #p1 = #0;
                #p21 = #p1 < #3;
                cwhile(#p21);
                {;
                    #p2 = #p2 + #1;
                    #p1 = #p1 + #1;
                    #p21 = #p1 < #3;
                };
                #p0 = #p0 + #1;
                #p20 = #p0 < #2;
            };
            fwrite(#p2);
        """;
        Assert.Equal("6", Run(code));
    }

    // ===================== Functions =====================

    [Fact]
    public void FunctionCallNoReturn()
    {
        var code = """
            fmyfunc();
            fterminate();
            define(myfunc);
            {;
                fwrite(#42);
            };
        """;
        Assert.Equal("42", Run(code));
    }

    [Fact]
    public void FunctionWithReturn()
    {
        var code = """
            #p0 = fdouble(#5);
            fwrite(#p0);
            fterminate();
            define(double);
            {;
                #p100 = #f0 * #2;
                freturn(#p100);
            };
        """;
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void FunctionWithMultipleParams()
    {
        var code = """
            #p0 = fadd(#3,#7);
            fwrite(#p0);
            fterminate();
            define(add);
            {;
                #p200 = #f0 + #0;
                #p201 = #f1 + #0;
                #p100 = #p200 + #p201;
                freturn(#p100);
            };
        """;
        Assert.Equal("10", Run(code));
    }

    [Fact]
    public void RecursiveFunction()
    {
        // Factorial of 5 = 120
        var code = """
            #p0 = ffact(#5);
            fwrite(#p0);
            fterminate();
            define(fact);
            {;
                #p100 = #f0 <= #1;
                cif(#p100);
                {;
                    freturn(#1);
                };
                #p101 = #f0 - #1;
                #p102 = ffact(#p101);
                #p103 = #f0 * #p102;
                freturn(#p103);
            };
        """;
        Assert.Equal("120", Run(code));
    }

    // ===================== I/O =====================

    [Fact]
    public void WriteNewline()
    {
        var code = "fwrite();";
        Assert.Equal(Environment.NewLine, Run(code));
    }

    [Fact]
    public void WriteChar()
    {
        // 65 = 'A', 66 = 'B'; #p0" writes var[0] as char
        var code = """
            #p0 = #65;
            fwrite(#p0");
            #p0 = #66;
            fwrite(#p0");
        """;
        Assert.Equal("AB", Run(code));
    }

    [Fact]
    public void ReadInt()
    {
        var code = "#p0 = freadint(); fwrite(#p0);";
        Assert.Equal("42", Run(code, "42"));
    }

    [Fact]
    public void ReadString()
    {
        // Reads "Hi" char by char and writes each char
        var code = """
            #p0 = freadstring();
            fwrite(#p0");
            #p0 = freadstring();
            fwrite(#p0");
        """;
        Assert.Equal("Hi", Run(code, "Hi"));
    }

    [Fact]
    public void ReadDone()
    {
        // Read one char from "AB", check readdone (should be 0 = not done)
        var code = """
            #p0 = freadstring();
            fwrite(#p0");
            #p1 = freaddone();
            fwrite(#p1);
        """;
        Assert.Equal("A0", Run(code, "AB"));
    }

    [Fact]
    public void ReadDoneAfterFullRead()
    {
        // Read both chars from "AB", check readdone (should be 1 = done)
        var code = """
            #p0 = freadstring();
            #p0 = freadstring();
            #p1 = freaddone();
            fwrite(#p1);
        """;
        Assert.Equal("1", Run(code, "AB"));
    }

    // ===================== Remove =====================

    [Fact]
    public void Remove()
    {
        // Set var[0], then remove it; unset vars default to 0
        var code = """
            #p0 = #42;
            fwrite(#p0);
            fremove(#p0);
            fwrite(#p0);
        """;
        Assert.Equal("420", Run(code));
    }

    // ===================== Jump =====================

    [Fact]
    public void UnconditionalJump()
    {
        var code = """
            fjump(skip);
            fwrite(#1);
            defineplace(skip);
            fwrite(#2);
        """;
        Assert.Equal("2", Run(code));
    }

    [Fact]
    public void ConditionalJumpTrue()
    {
        // fjump with condition 1 (true) → jumps
        var code = """
            fjump(skip,#1);
            fwrite(#1);
            defineplace(skip);
            fwrite(#2);
        """;
        Assert.Equal("2", Run(code));
    }

    [Fact]
    public void ConditionalJumpFalse()
    {
        // fjump with condition 0 (false) → does NOT jump, executes both writes
        var code = """
            fjump(skip,#0);
            fwrite(#1);
            defineplace(skip);
            fwrite(#2);
        """;
        Assert.Equal("12", Run(code));
    }

    // ===================== Terminate =====================

    [Fact]
    public void Terminate()
    {
        var code = "fwrite(#1); fterminate(); fwrite(#2);";
        Assert.Equal("1", Run(code));
    }

    // ===================== Exceptions =====================

    [Fact]
    public void ExceptionThrows()
    {
        var code = "fexcept(test_error);";
        var ex = Assert.Throws<Exception>(() => Run(code));
        Assert.Contains("test error", ex.Message);
    }

    [Fact]
    public void ExceptAddAndSend()
    {
        // Build exception message char by char ('H'=72, 'i'=105) and send
        var code = """
            fexceptadd(#72");
            fexceptadd(#105");
            fexceptsend();
        """;
        var ex = Assert.Throws<Exception>(() => Run(code));
        Assert.Contains("Hi", ex.Message);
    }

    // ===================== Literals =====================

    [Fact]
    public void TrueLiteral()
    {
        var code = "fwrite(#true);";
        Assert.Equal("1", Run(code));
    }

    [Fact]
    public void CharLiteralInSource()
    {
        // #A" is char literal for 'A' = 65
        var code = "#p0 = #A\"; fwrite(#p0);";
        Assert.Equal("65", Run(code));
    }

    [Fact]
    public void CharLiteralWrite()
    {
        // Writing a literal as char: #32" = space
        var code = "fwrite(#72\"); fwrite(#105\");";
        Assert.Equal("Hi", Run(code));
    }

    // ===================== Compiler Errors =====================

    [Fact]
    public void UndefinedFunctionThrows()
    {
        var code = "fnonexistent();";
        Assert.Throws<Exception>(() => Compiler.StringToCode(code));
    }

    [Fact]
    public void UndefinedPlaceThrows()
    {
        var code = "fjump(nowhere)();";
        Assert.Throws<Exception>(() => Compiler.StringToCode(code));
    }

    [Fact]
    public void UnmatchedBraceThrows()
    {
        var code = "cif(#1); {; fwrite(#1);";
        Assert.Throws<Exception>(() => Compiler.StringToCode(code));
    }

    // ===================== Comments =====================

    [Fact]
    public void CommentsAreIgnored()
    {
        var code = """
            / this is a comment;
            fwrite(#42);
            / another comment;
        """;
        Assert.Equal("42", Run(code));
    }

    // ===================== Integration =====================

    [Fact]
    public void FibonacciSequence()
    {
        // Print first 8 fibonacci numbers separated by spaces
        var code = """
            #p0 = #0;
            #p1 = #1;
            #p3 = #0;
            #p10 = #p3 < #8;
            cwhile(#p10);
            {;
                fwrite(#p0);
                fwrite(#32");
                #p2 = #p0 + #p1;
                #p0 = #p1;
                #p1 = #p2;
                #p3 = #p3 + #1;
                #p10 = #p3 < #8;
            };
        """;
        Assert.Equal("0 1 1 2 3 5 8 13 ", Run(code));
    }

    [Fact]
    public void EchoInput()
    {
        // Read a string char by char and echo it
        var code = """
            #p0 = freadstring();
            #p1 = freaddone();
            #p10 = !#p1;
            cwhile(#p10);
            {;
                fwrite(#p0");
                #p0 = freadstring();
                #p1 = freaddone();
                #p10 = !#p1;
            };
            fwrite(#p0");
        """;
        Assert.Equal("Hello", Run(code, "Hello"));
    }
}
