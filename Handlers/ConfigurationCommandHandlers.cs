using System.Text.Json;
using ConfigFern.Services;

namespace ConfigFern.Handlers;

public class ConfigurationCommandHandlers
{
    private readonly IConfigurationService _configService;

    public ConfigurationCommandHandlers(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task HandleAddAsync(string key, string value, string env, bool encrypted, string? desc)
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync(env);
            var keyParts = key.Split(':');
            var currentDict = config;

            // Navigate through nested structure
            for (int i = 0; i < keyParts.Length - 1; i++)
            {
                if (!currentDict.ContainsKey(keyParts[i]))
                {
                    currentDict[keyParts[i]] = new Dictionary<string, object>();
                }
                currentDict = (Dictionary<string, object>)currentDict[keyParts[i]];
            }

            var finalKey = keyParts[^1];
            var isUpdate = currentDict.ContainsKey(finalKey);
            currentDict[finalKey] = encrypted ? _configService.EncryptValue(value) : value;

            if (_configService.ValidateConfiguration(config))
            {
                await _configService.SaveConfigurationAsync(env, config);
                Console.WriteLine(isUpdate
                    ? $"Updated configuration entry '{key}' in environment '{env}'"
                    : $"Added configuration entry '{key}' to environment '{env}'");
            }
            else
            {
                Console.WriteLine("Error: Invalid configuration entry");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task HandleListAsync(string env)
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync(env);
            
            if (!config.Any())
            {
                Console.WriteLine($"No configuration entries found for environment '{env}'");
                return;
            }

            Console.WriteLine($"Configuration entries for environment '{env}':");
            PrintConfiguration(config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task HandleCompareAsync(string env1, string env2)
    {
        try
        {
            var differences = await _configService.CompareEnvironmentsAsync(env1, env2);

            if (!differences.Any())
            {
                Console.WriteLine($"No differences found between environments '{env1}' and '{env2}'");
                return;
            }

            Console.WriteLine($"Differences between environments '{env1}' and '{env2}':");
            foreach (var (key, values) in differences)
            {
                Console.WriteLine($"\nKey: {key}");
                Console.WriteLine($"- {env1}: {FormatValue(values[env1])}");
                Console.WriteLine($"- {env2}: {FormatValue(values[env2])}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task HandleValidateAsync(string env)
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync(env);
            
            if (!config.Any())
            {
                Console.WriteLine($"No configuration entries found for environment '{env}'");
                return;
            }

            var isValid = _configService.ValidateConfiguration(config);
            Console.WriteLine(isValid
                ? $"Configuration for environment '{env}' is valid"
                : $"Configuration for environment '{env}' is invalid");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void PrintConfiguration(Dictionary<string, object> config, string prefix = "")
    {
        foreach (var (key, value) in config)
        {
            if (value is Dictionary<string, object> nestedConfig)
            {
                Console.WriteLine($"{prefix}{key}:");
                PrintConfiguration(nestedConfig, prefix + "  ");
            }
            else
            {
                Console.WriteLine($"{prefix}{key}: {FormatValue(value)}");
            }
        }
    }

    private string FormatValue(object value)
    {
        return value switch
        {
            string str => str,
            _ => JsonSerializer.Serialize(value)
        };
    }
} 