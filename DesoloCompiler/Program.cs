using System.Text;

namespace DesoloCompiler;

public static class Runner
{
    [STAThread]
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        while (true)
        {
            Console.WriteLine("Please initiate opening of next file by typing anything:");
            string a = Console.ReadLine();
            while (a == "RunInsideCode")
            {
                Console.WriteLine("Starting inner code execution... ");
                string Program = """
                                    #p0 = r#10;
                                    fwrite(#p0);
                                    """;
                Console.WriteLine(Run(Program));
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
}