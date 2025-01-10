using System;

namespace ConfigFern.Models;

public class EnvironmentConfig
{
    public string? ConfigEncryptionKey { get; set; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(ConfigEncryptionKey))
        {
            throw new InvalidOperationException("The CONFIG_ENCRYPTION_KEY environment variable is mandatory.");
        }
    }
} 