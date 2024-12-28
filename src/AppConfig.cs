using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

public class AppConfig
{
    public ulong[] HelpChannelIds { get; private set; }
    public int MaxConnectionAttempts { get; private set; }

    public static async Task<AppConfig> InitConfigAsync()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "bot.cfg");
        var configLines = await File.ReadAllLinesAsync(configPath);

        var config = new AppConfig();

        for (int line = 0; line < configLines.Length; line++)
        {
            var ln = configLines[line];

            if (ln.Length == 0 || char.IsWhiteSpace(ln[0])) continue;
            else
            {
                var n = ln.IndexOfAny(['=', ':']);
                if (n == -1) continue;
                
                var key = ln[0..n].Trim();
                var valueType = config.GetType().GetProperty(key).PropertyType;
                if (valueType.IsArray) valueType = valueType.GetElementType(); 
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
                        anotherLine = value[^1] == ',';

                        var castedValue = parseMethod == null ?
                            Convert.ChangeType(value[0..^1], valueType) :
                            parseMethod.Invoke(valueType, [value[0..^1]]);

                        list.Add(castedValue);
                    }
                    
                    var arr = new ArrayList(list).ToArray(valueType);
                    config.GetType().GetProperty(key).SetValue(config, arr);

                    
                }
            }
        }

        await Logger.Log("Config init complete", LogSeverity.Info);

        return config;
    }
}