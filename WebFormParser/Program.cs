using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using CommandLine;
using WebFormParser.model;
using WebFormParser.Utility;
using WebFormParser.Utility.Options;

namespace WebFormParser
{
    internal class Program
    {
        internal static int Stage = 0;

        static void Main(string[] args)
        {
            var showHelp = false;
            switch (args.Length)
            {
                case > 0:
                    if (args[0] == "--help")
                        showHelp = true;
                    break;
                case 0:
                    showHelp = true;
                    break;
            }

            if (args.Length > 1 || showHelp)
            {
                CommandLine.Parser.Default.ParseArguments<ProgramOptions>(args)
                    .WithParsed(RunOptions);
            }

            if (showHelp || args.Length > 1)
                return;

            CommandLine.Parser.Default.ParseArguments<DemoOptions>(args)
                .WithParsed(RunOptions);
        }

        private static void RunOptions(DemoOptions opts)
        {
            if (opts.Demo)
                RunDemo();
        }

        private static void RunOptions(ProgramOptions opts)
        {
            Stage = opts.Stage;

            if (!CheckPaths(opts))
                return;

            if (Stage <= 1)
                BusinessLogic.CleanCode(opts.Source, opts.Destination);
            if (Stage == 2)
                BusinessLogic.FixCode(opts.Source, opts.Destination);
        }

        private static bool CheckPaths(ProgramOptions opts)
        {
            if (!Directory.Exists(opts.Source))
            {
                Console.WriteLine("Specify a valid Source Folder!");
                return false;
            }

            if (Directory.Exists(opts.Destination))
                return true;
            
            Console.WriteLine("Specify a valid Destination Folder!");
            
            return false;

        }

        private static void RunDemo()
        {
            Demo.PrintClassicNodes();
        }


    }
}