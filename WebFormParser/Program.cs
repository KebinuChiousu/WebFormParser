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

            if (!CheckPaths(ref opts))
                return;

            if (Stage <= 1)
                BusinessLogic.CleanCode(opts.Source, opts.Destination);
            if (Stage == 2)
                BusinessLogic.FixCode(opts.Source, opts.Destination);
        }

        private static bool CheckPaths(ref ProgramOptions opts)
        {
            var source = opts.Source;

            if (!Directory.Exists(source))
            {
                Console.WriteLine("Specify a valid Source Folder!");
                return false;
            }

            var dest = opts.Destination;

            if (string.IsNullOrEmpty(dest))
                dest = source + ".new";

            EnsureDestFolder(dest, opts.Force);

            opts.Destination = dest;

            return true;
        }

        private static void EnsureDestFolder(string dest, bool force)
        {
            if (Directory.Exists(dest))
                if (!force)
                    return;
                else
                    Directory.Delete(dest, true);

            Directory.CreateDirectory(dest);
        }

        private static void RunDemo()
        {
            Demo.PrintClassicNodes();
        }


    }
}