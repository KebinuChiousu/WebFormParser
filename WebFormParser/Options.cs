
using CommandLine;

namespace WebFormParser
{
    public class Options
    {
        public Options()
        {
            Source = "";
            Destination = "";
        }
        
        [Option('s', "src", HelpText = "Source Project Folder", Required = true)]
        public string Source { get; set; }

        [Option('d', "destination", HelpText = "Destination Project Folder", Required = true)]
        public string Destination { get; set; }
    }
}
