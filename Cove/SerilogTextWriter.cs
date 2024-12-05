using System;
using System.IO;
using System.Text;
using Serilog;

public class SerilogTextWriter : TextWriter
{
    private readonly ILogger _logger;
    private readonly bool _isError;

    public SerilogTextWriter(ILogger logger, bool isError = false)
    {
        _logger = logger;
        _isError = isError;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        if (_isError)
        {
            _logger.Error(value);
        }
        else
        {
            _logger.Information(value);
        }
    }

    public override void Write(char value)
    {
        // Optional: buffer single-character writes if needed.
        WriteLine(value.ToString());
    }
}
