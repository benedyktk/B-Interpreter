using System.Text;

namespace DesoloCompiler;

class Runner
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        var code = """

                      """;
        Interpreter MyProgram = new Interpreter(Compiler.StringToCode(code), Console.In, Console.Out);
        MyProgram.RunCode(0);
    }
}