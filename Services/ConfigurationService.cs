using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigFern.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _configPath;
    private readonly byte[] _encryptionKey;
    private const string ENCRYPTED_PREFIX = "ENC:";

    public ConfigurationService(string configPath, string encryptionKey)
    {
        _configPath = configPath;
        _encryptionKey = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
    }

    public async Task SaveConfigurationAsync(string environment, Dictionary<string, object> configuration)
    {
        var processedConfig = await ProcessConfigurationForSaving(environment, configuration);
        var configJson = JsonConvert.SerializeObject(processedConfig, Formatting.Indented);
        var filePath = Path.Combine(_configPath, $"appsettings.{environment}.json");
        await File.WriteAllTextAsync(filePath, configJson);
    }

    public async Task<Dictionary<string, object>> LoadConfigurationAsync(string environment)
    {
        var filePath = Path.Combine(_configPath, $"appsettings.{environment}.json");
        if (!File.Exists(filePath))
            return new Dictionary<string, object>();

        var configJson = await File.ReadAllTextAsync(filePath);
        var configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
        return ProcessConfigurationForLoading(configuration ?? new Dictionary<string, object>(), decryptValues: true);
    }

    public async Task<Dictionary<string, Dictionary<string, object>>> CompareEnvironmentsAsync(string env1, string env2)
    {
        var config1 = await LoadConfigurationAsync(env1);
        var config2 = await LoadConfigurationAsync(env2);

        var differences = new Dictionary<string, Dictionary<string, object>>();
        CompareConfigurations(config1, config2, "", differences, env1, env2);

        return differences;
    }

    public bool ValidateConfiguration(Dictionary<string, object> configuration)
    {
        try
        {
            ValidateConfigurationRecursive(configuration);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string EncryptValue(string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith(ENCRYPTED_PREFIX))
                return value;

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var valueBytes = Encoding.UTF8.GetBytes(value);
            var encryptedBytes = encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length);

            var resultBytes = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, resultBytes, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, resultBytes, aes.IV.Length, encryptedBytes.Length);

            return ENCRYPTED_PREFIX + Convert.ToBase64String(resultBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to encrypt value: {ex.Message}");
        }
    }

    public string DecryptValue(string encryptedValue)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedValue) || !encryptedValue.StartsWith(ENCRYPTED_PREFIX))
                return encryptedValue;

            var base64Value = encryptedValue.Substring(ENCRYPTED_PREFIX.Length);
            var fullBytes = Convert.FromBase64String(base64Value);

            if (fullBytes.Length < 16) // IV size
                throw new InvalidOperationException("Invalid encrypted value format");

            var iv = new byte[16];
            var encryptedBytes = new byte[fullBytes.Length - 16];

            Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
            Buffer.BlockCopy(fullBytes, 16, encryptedBytes, 0, encryptedBytes.Length);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt value: {ex.Message}. Please ensure the encryption key is correct and the value is properly encrypted.");
        }
    }

    private async Task<Dictionary<string, object>> ProcessConfigurationForSaving(string environment, Dictionary<string, object> configuration)
    {
        // First, load the existing configuration to get the current encrypted state
        var filePath = Path.Combine(_configPath, $"appsettings.{environment}.json");
        Dictionary<string, object> existingConfig = new();
        
        if (File.Exists(filePath))
        {
            var configJson = await File.ReadAllTextAsync(filePath);
            existingConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson) ?? new Dictionary<string, object>();
            existingConfig = ProcessConfigurationForLoading(existingConfig, decryptValues: false);
        }

        return MergeConfigurations(configuration, existingConfig);
    }

    private Dictionary<string, object> MergeConfigurations(Dictionary<string, object> newConfig, Dictionary<string, object> existingConfig)
    {
        var result = new Dictionary<string, object>();
        foreach (var (key, value) in newConfig)
        {
            if (value is Dictionary<string, object> newDict)
            {
                var existingDict = existingConfig.ContainsKey(key) && existingConfig[key] is Dictionary<string, object> dict
                    ? dict
                    : new Dictionary<string, object>();
                result[key] = MergeConfigurations(newDict, existingDict);
            }
            else if (value is string strValue)
            {
                // If the value is already encrypted (from a new addition), preserve it
                if (strValue.StartsWith(ENCRYPTED_PREFIX))
                {
                    result[key] = strValue;
                }
                // If there's an existing encrypted value for this key, preserve the encryption
                else if (existingConfig.ContainsKey(key) && existingConfig[key] is string existingStr && existingStr.StartsWith(ENCRYPTED_PREFIX))
                {
                    result[key] = EncryptValue(strValue);
                }
                else
                {
                    result[key] = strValue;
                }
            }
            else
            {
                result[key] = value;
            }
        }
        return result;
    }

    private Dictionary<string, object> ProcessConfigurationForLoading(Dictionary<string, object> configuration, bool decryptValues = true)
    {
        var result = new Dictionary<string, object>();
        foreach (var (key, value) in configuration)
        {
            result[key] = ProcessValueForLoading(value, decryptValues);
        }
        return result;
    }

    private object ProcessValueForLoading(object value, bool decryptValues)
    {
        return value switch
        {
            Dictionary<string, object> dict => ProcessConfigurationForLoading(dict, decryptValues),
            JObject jObj => ProcessConfigurationForLoading(jObj.ToObject<Dictionary<string, object>>()!, decryptValues),
            string str => decryptValues ? DecryptValue(str) : str,
            _ => value
        };
    }

    private void ValidateConfigurationRecursive(Dictionary<string, object> configuration)
    {
        foreach (var (key, value) in configuration)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Configuration keys cannot be empty");

            if (value is Dictionary<string, object> nestedConfig)
            {
                ValidateConfigurationRecursive(nestedConfig);
            }
        }
    }

    private void CompareConfigurations(
        Dictionary<string, object> config1,
        Dictionary<string, object> config2,
        string path,
        Dictionary<string, Dictionary<string, object>> differences,
        string env1,
        string env2)
    {
        foreach (var (key, value1) in config1)
        {
            var currentPath = string.IsNullOrEmpty(path) ? key : $"{path}:{key}";
            if (!config2.TryGetValue(key, out var value2))
            {
                differences[currentPath] = new Dictionary<string, object>
                {
                    [env1] = value1,
                    [env2] = "Not set"
                };
                continue;
            }

            if (value1 is Dictionary<string, object> dict1 && value2 is Dictionary<string, object> dict2)
            {
                CompareConfigurations(dict1, dict2, currentPath, differences, env1, env2);
            }
            else if (!JToken.DeepEquals(JToken.FromObject(value1), JToken.FromObject(value2)))
            {
                differences[currentPath] = new Dictionary<string, object>
                {
                    [env1] = value1,
                    [env2] = value2
                };
            }
        }

        foreach (var (key, value2) in config2)
        {
            var currentPath = string.IsNullOrEmpty(path) ? key : $"{path}:{key}";
            if (!config1.ContainsKey(key))
            {
                differences[currentPath] = new Dictionary<string, object>
                {
                    [env1] = "Not set",
                    [env2] = value2
                };
            }
        }
    }
} 