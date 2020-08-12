using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

/// <summary>
/// 通过 stdio 和 node.js 实现通讯
/// </summary>
namespace NodeAddon_WithStdio
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            new Thread(new ThreadStart(StartStdioListener)).Start();
        }

        static void StartStdioListener()
        {
            string strin = Console.ReadLine();

            Console.WriteLine("C# 收到的数据: {0}", strin);
        }

    }
}
