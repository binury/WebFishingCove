using System.Text;

namespace Cove.Server.Utils;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendLineLF(this StringBuilder sb, string value = "")
    {
        sb.Append(value);
        sb.Append('\n'); // force LF instead of CRLF
        return sb;
    }
}
