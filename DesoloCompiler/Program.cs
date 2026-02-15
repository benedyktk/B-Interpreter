using System.Text;

namespace DesoloCompiler;

public static class Runner
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        var Code = """

                      """;
        Interpreter MyProgram = new Interpreter(Compiler.StringToCode(Code), Console.In, Console.Out);
        MyProgram.RunCode(0);
    }
}