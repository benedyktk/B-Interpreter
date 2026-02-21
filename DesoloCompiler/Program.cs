using System.Text;

namespace DesoloCompiler;

public static class Runner
{
    [STAThread]
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        string Program = """
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
        NoCatch(Program);
        return;
        while (true)
        {
            Console.WriteLine("Please initiate opening of next file by typing anything:");
            string a = Console.ReadLine();
            while (a == "RunInsideCode")
            {
                Console.WriteLine("Starting inner code execution... ");

                NoCatch(Program);
                Console.WriteLine("Please initiate rerunning of inner code by typing anything:");
                string b = Console.ReadLine();
                if (b == "StopInsideCode")
                {
                    a = "C'mon brother stop";
                }
            }
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select a .dsl/.txt file to run:";
            dialog.Filter = "DSL and Text files (*.dsl;*.txt)|*.dsl;*.txt";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Console.WriteLine("Opening program...");
                Console.WriteLine(Run(System.IO.File.ReadAllText(dialog.FileName)));
            }
            else
            {
                Console.WriteLine("No file selected.");
            }
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