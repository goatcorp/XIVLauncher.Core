using Config.Net;

using Newtonsoft.Json;

using XIVLauncher.Common.Addon;

namespace XIVLauncher.Core.Configuration.Parsers;

internal class AddonListParser : ITypeParser
{
    public IEnumerable<Type> SupportedTypes => new[] { typeof(List<AddonEntry>) };

    public string ToRawString(object value)
    {
        if (value is List<AddonEntry> list)
            return JsonConvert.SerializeObject(list);

        return string.Empty;
    }

    public bool TryParse(string value, Type t, out object result)
    {
        if (t == typeof(List<AddonEntry>))
        {
            result = JsonConvert.DeserializeObject<List<AddonEntry>>(value);
            return true;
        }

        result = null!;
        return false;
    }
}
