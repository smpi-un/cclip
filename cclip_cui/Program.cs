using System;

namespace cclip_cui
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            cclip_lib.Program.Main(args);
            
        }
    }
}
