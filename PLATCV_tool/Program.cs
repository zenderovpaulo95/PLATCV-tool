/*********************************************************
 *     Professor Layton and the Curious Village tool     *
 *                  Original author ssh                  *
 *   (https://zenhax.com/viewtopic.php?p=41170#p41170)   *
 *         C# version tool made by Sudakov Pavel         *
 *********************************************************/
using System;
using System.IO;

namespace PLATCV_tool
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if((args.Length == 2 && args[0] == "export" && File.Exists(args[1]))
            || (args.Length == 3 && args[0] == "export" && File.Exists(args[1]) && Directory.Exists(args[2])))
            {
                string InputFile = args[1];
                string OutputFolder = AppDomain.CurrentDomain.BaseDirectory + "/extracted";
                if (args.Length == 3 && Directory.Exists(args[2])) OutputFolder = args[2];

                if(File.Exists(InputFile)) WorkWithFiles.ExportFiles(InputFile, OutputFolder);
            }
            else if ((args.Length == 2 && args[0] == "import")
            || (args.Length == 3 && args[0] == "import" && Directory.Exists(args[1])))
            {
                string OutputFile = args[1];
                string InputFolder = AppDomain.CurrentDomain.BaseDirectory + "/extracted";
                if (args.Length == 3 && Directory.Exists(args[1]))
                {
                    InputFolder = args[1];
                    OutputFile = args[2];
                }

                if(Directory.Exists(InputFolder)) WorkWithFiles.ImportFiles(InputFolder, OutputFile);
            }
            else if((args.Length == 2 && args[0] == "log" && File.Exists(args[1]))
                || (args.Length == 3 && args[0] == "log" && File.Exists(args[1])))
                {
                    string OutputFile = AppDomain.CurrentDomain.BaseDirectory + "/table.log";
                    string InputFile = args[1];

                    if (args.Length == 3) OutputFile = args[2];

                    WorkWithFiles.GetTable(InputFile, OutputFile);
                }
        }
    }
}
