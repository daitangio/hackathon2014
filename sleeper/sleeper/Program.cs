using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sleeper
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.Write("Sleep:" + args[0]);
            int v=Int32.Parse(args[0]);
            System.Threading.Thread.Sleep(v);
        }
    }
}
