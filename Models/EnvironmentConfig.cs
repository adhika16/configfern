using System;

namespace ConfigFern.Models;

public class EnvironmentConfig
{
    public string? ConfigEncryptionKey { get; set; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(ConfigEncryptionKey))
        {
            throw new InvalidOperationException(
                "The CONFIG_ENCRYPTION_KEY is not set in your .env file. " +
                "You can generate a secure key using: " +
                "openssl rand -base64 32 (Unix/Linux) or " +
                "[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)) (Windows PowerShell)");
        }
    }
} 