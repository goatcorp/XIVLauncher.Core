using Config.Net;

namespace XIVLauncher.Core.Configuration.Parsers;

internal class FileInfoParser : ITypeParser
{
    public IEnumerable<Type> SupportedTypes => new[] { typeof(FileInfo) };

    public string? ToRawString(object value)
    {
        if (value is FileInfo fi)
            return fi.FullName;

        return null;
    }

    public bool TryParse(string? value, Type t, out object? result)
    {
        if (value == null)
        {
            result = null;
            return false;
        }

        if (t == typeof(FileInfo))
        {
            var fi = new FileInfo(value);
            result = fi;
            return true;
        }

        result = null;
        return false;
    }
}
