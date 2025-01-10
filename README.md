# ConfigFern

ConfigFern is a .NET CLI tool that aims to assist in managing application configurations across various environments. It offers features for managing settings, encrypting sensitive values, validating configurations, and comparing environments, with the hope of making your configuration management a bit easier.

> **Note:** This project is currently in development mode. Features and functionality may change as development progresses.

## Features

- Manage configuration entries across different environments (dev, staging, prod, etc.)
- Support for nested configuration sections
- Encrypt sensitive configuration values
- Validate configuration files
- Compare configurations between environments
- Standard .NET configuration format support

## Installation

1. Clone the repository
2. Build the project:
   ```bash
   dotnet build
   ```
3. Set up the encryption key (optional):
   ```bash
   export CONFIG_ENCRYPTION_KEY="your-secure-key-here"
   ```

## Usage

### Adding a Configuration Entry

```bash
# Add a simple configuration
dotnet run add --key "AppSettings:ApplicationName" --value "MyApp" --env "dev"

# Add an encrypted connection string
dotnet run add --key "ConnectionStrings:DefaultConnection" --value "Server=localhost;Database=mydb" --env "dev" --encrypted

# Add a nested configuration
dotnet run add --key "AppSettings:Features:EnableCache" --value "true" --env "prod"
```

### Listing Configuration Entries

```bash
dotnet run list --env dev
```

Example output:
```
Configuration entries for environment 'dev':
ConnectionStrings:
  DefaultConnection: Server=localhost;Database=mydb
AppSettings:
  ApplicationName: MyApp
  Features:
    EnableCache: true
```

### Comparing Environments

```bash
dotnet run compare --env1 dev --env2 prod
```

Example output:
```
Differences between environments 'dev' and 'prod':
Key: ConnectionStrings:DefaultConnection
- dev: Server=localhost;Database=mydb
- prod: Server=prod-server;Database=mydb

Key: AppSettings:Features:EnableCache
- dev: true
- prod: false
```

### Validating Configuration

```bash
dotnet run validate --env dev
```

## Configuration Structure

ConfigFern uses the standard .NET configuration format:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "ENC:Server=localhost;Database=mydb;User Id=myUsername;Password=myPassword;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AppSettings": {
    "ApplicationName": "MyDotNetApp",
    "Features": {
      "EnableCache": true,
      "RetryPolicy": {
        "MaxRetries": 3,
        "DelaySeconds": 5
      }
    }
  }
}
```

## Security

- Sensitive values are encrypted using AES encryption
- Encrypted values are prefixed with "ENC:" in the configuration file
- Encryption key should be stored securely and not committed to version control
- Set the encryption key using the `CONFIG_ENCRYPTION_KEY` environment variable

## Best Practices

1. Use nested configuration sections for better organization
2. Always encrypt sensitive information (passwords, API keys, etc.)
3. Use descriptive configuration keys
4. Follow the standard .NET configuration naming conventions
5. Regularly validate configurations across environments
6. Compare environments before deployments
7. Keep encryption keys secure and separate from the configuration files

## License

MIT License 