
using CommandLine;

namespace WebFormParser.Utility.Options
{
    public class ProgramOptions
    {
        public ProgramOptions()
        {
            Source = "";
            Destination = "";
            Stage = 0;
            Force = false;
        }

        [Option('s', "src", HelpText = "Source Project Folder", Required = true)]
        public string Source { get; set; }

        [Option('d', "dest", HelpText = "Destination Project Folder", Required = false)]
        public string Destination { get; set; }

        [Option('f', "force", HelpText = "Force overwrite of destination folder", Required = false)]
        public bool Force { get; set; }

        [Option("stage", HelpText = "Stage to resume from", Required = false)]
        public int Stage { get; set; }
    }
}
