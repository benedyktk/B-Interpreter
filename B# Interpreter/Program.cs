using System.Text;

namespace DesoloCompiler;

class Runner
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        string File = """
                      cwhile(#true);
                      {;
                      #p0 = freadstring();
                      fwrite(#p0);
                      };
                      """;
        Interpreter MyProgram = new Interpreter(Compiler.StringToCode(File));
        MyProgram.RunCode(0);
    }
}