using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesoloCompiler
{
    class Runner
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
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
}