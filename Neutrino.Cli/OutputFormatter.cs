namespace Neutrino.Cli;

public class OutputFormatter
{
    private bool _isJson;

    public OutputFormatter(bool isJson)
    {
        _isJson = isJson;
    }
}