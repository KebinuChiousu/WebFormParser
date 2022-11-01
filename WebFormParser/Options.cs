
using CommandLine;

namespace WebFormParser
{
    public class Options
    {
        public Options()
        {
            Source = "";
            Destination = "";
            Stage = 0;
        }
        
        [Option('s', "src", HelpText = "Source Project Folder", Required = true)]
        public string Source { get; set; }

        [Option('d', "dest", HelpText = "Destination Project Folder", Required = true)]
        public string Destination { get; set; }

        [Option("stage", HelpText= "Stage to resume from", Required = false)]
        public int Stage { get; set; }
    }
}
