using System.Text;

namespace DesoloCompiler;

public static class Runner
{
    private static string _program = """
                   defineconst(#c0, #100);
                   #p0 = #c0;
                   cuntil(#p1);
                   {;
                       #a0 = freadstring();
                       #p0 = #p0 + #1;
                       #p1 = freaddone();
                   };
                   /#p0 - limit, #p1 - current start spot, #p2 - current read spot;
                   #p1 = #c0;
                   #p3 = #p1 < #p0;
                   cwhile(#p3);
                   {;
                       #p2 = #p1;
                       #p4 = #p2 < #p0;
                       cwhile(#p4);
                       {;
                           #p-1 = #p2 - #1;
                           #p5 = fCondCapitalize(#a-1, #a2);
                           fwrite(#p5");
                           #p2 = #p2 + #1;
                           #p4 = #p2 < #p0;
                       };
                       fnewline();
                       #a1 = #0;
                       #p1 = #p1 + #1;
                       #p-2 = #a1 == #32;
                       cwhile(#p-2);
                       {;
                         #a1 = #0;
                         #p1 = #p1 + #1;
                         #p-2 = #a1 == #32;
                       };
                       #p3 = #p1 < #p0;
                   };
                   fterminate();
                   define(CondCapitalize);
                   {;
                       #f2 = #f0 == #32;
                       #f3 = #f0 == #0;
                       #f2 = #f2 | #f3;
                       cif(#f2);
                       {;
                           #f1 = fCapitalize(#f1);
                       };
                       freturn(#f1);
                   };
                   define(Capitalize);
                   {;
                       #f1 = #f0 >= #97;
                       #f2 = #f0 <= #122;
                       #f1 = #f1 & #f2;
                       cif(#f1);
                       {;
                           #f0 = #f0 - #32;
                       };
                       freturn(#f0);
                   };
                   """;

    [STAThread]
    public static void Main(string[] Args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        if (Args.Length == 0)
        {
            NoCatch(_program);
        }
        else
        {
            Console.WriteLine(Run(File.ReadAllText(Args[0])));
        }
    }

    public static string Run(string Code)
    {
        try
        {
            new Interpreter(Compiler.StringToCode(Code), Console.In, Console.Out).RunCode(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            return "Caught exception: " + ex.Message;
        }
        Console.WriteLine();
        return "Done.";
    }

    public static void NoCatch(string Code)
    {
        new Interpreter(Compiler.StringToCode(Code), Console.In, Console.Out).RunCode(0);
        Console.WriteLine();
        Console.WriteLine("Done.");
    }
}