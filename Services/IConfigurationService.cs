using ConfigFern.Models;

namespace ConfigFern.Services;

public interface IConfigurationService
{
    Task SaveConfigurationAsync(string environment, Dictionary<string, object> configuration);
    Task<Dictionary<string, object>> LoadConfigurationAsync(string environment);
    Task<Dictionary<string, Dictionary<string, object>>> CompareEnvironmentsAsync(string env1, string env2);
    bool ValidateConfiguration(Dictionary<string, object> configuration);
    string EncryptValue(string value);
    string DecryptValue(string encryptedValue);
} 