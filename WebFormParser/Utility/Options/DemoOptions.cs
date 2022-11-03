using CommandLine;

namespace WebFormParser.Utility.Options;

public class DemoOptions
{
    public DemoOptions()
    {
        Demo = false;
    }

    [Option("demo", HelpText = "Demo Mode", Required = false)]
    public bool Demo { get; set; }

}