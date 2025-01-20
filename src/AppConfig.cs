using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

public class AppConfig
{
    public int MaxConnectionAttempts { get; private set; } = 5;
    public ulong[] HelpChannelIds { get; private set; } = [];
    public ulong[] ResolvedTagIds { get; private set; } = [];
    public string RepositoryOwner { get; private set; } = null;
    public string RepositoryName { get; private set; } = null;
    public string RepositoryBranch { get; private set; } = null;

    public static async Task<AppConfig> InitConfigAsync()
    {
        _ = Logger.Log("Starting config initialisation.", LogSeverity.Info);
        var configPath = Path.Combine(AppContext.BaseDirectory, "bot.cfg");
        var configLines = await File.ReadAllLinesAsync(configPath);

        AppConfig config = new();

        for (int line = 0; line < configLines.Length; line++)
        {
            var ln = configLines[line];

            if (ln.Trim().Length == 0 || ln.Trim()[0] == '#') continue;
            else
            {
                var n = ln.IndexOfAny(['=', ':']);
                if (n == -1) continue;
                
                var key = ln[0..n].Trim();

                var property = config.GetType().GetProperty(key);
                if (property == null)
                {
                    _ = Logger.Log($"'{key}' in line '{line + 1}' is not a valid confiuration variable. Skipping it.", LogSeverity.Warning);
                    continue;
                }
                var valueType = property.PropertyType.IsArray ? property.PropertyType.GetElementType() : property.PropertyType;
                var parseMethod = valueType.GetMethod("Parse", [typeof(string)]);

                if (ln[n] == '=') // Parse single values fields
                {
                    var value = ln[(n + 1)..ln.Length].Replace('\r', ' ').Replace('\n', ' ').Trim();
                    var castedValue = parseMethod == null ?
                            Convert.ChangeType(value, valueType) :
                            parseMethod.Invoke(valueType, [value]);

                    config.GetType().GetProperty(key).SetValue(config, castedValue);
                }
                else if (ln[n] == ':') // Parse list fields
                {
                    var listType = typeof(List<>).MakeGenericType(valueType);
                    var list = Activator.CreateInstance(listType) as IList;

                    bool anotherLine = true;
                    while (anotherLine && line + 1 < configLines.Length && char.IsWhiteSpace(configLines[line + 1][0]))
                    {
                        line++;

                        var value = configLines[line].Replace('\r', ' ').Replace('\n', ' ').Trim();
                        if (value[0] == '#') continue;
                        anotherLine = value[^1] == ',';
                        if (anotherLine) value = value[0..^1];

                        var castedValue = parseMethod == null ?
                            Convert.ChangeType(value, valueType) :
                            parseMethod.Invoke(valueType, [value]);

                        list.Add(castedValue);
                    }
                    
                    var arr = new ArrayList(list).ToArray(valueType);
                    config.GetType().GetProperty(key).SetValue(config, arr);
                }
            }
        }

        _ = Logger.Log("Config initialisation completed.", LogSeverity.Info);

        return config;
    }
}