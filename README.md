# ConfigFern

ConfigFern is a .NET CLI tool that aims to assist in managing application configurations across various environments. It offers features for managing settings, encrypting sensitive values, validating configurations, and comparing environments, with the hope of making your configuration management a bit easier.

> **Note:** This project is currently in development mode. Features and functionality may change as development progresses.

## Features

- Manage configuration entries across different environments (dev, staging, prod, etc.)
- Support for nested configuration sections
- Encrypt sensitive configuration values
- Validate configuration files
- Compare configurations between environments
- Decrypt configuration files for deployment
- Standard .NET configuration format support

## Installation

### Global Installation

You can install ConfigFern as a global .NET CLI tool using:

```bash
dotnet tool install --global ConfigFern
```

To update to the latest version:

```bash
dotnet tool update --global ConfigFern
```

To uninstall:

```bash
dotnet tool uninstall --global ConfigFern
```

### Local Installation

#### Prerequisites
- .NET SDK 8.0

#### Installation Steps

1. Clone the repository
2. Build the project:
```bash
# Restore dependencies
dotnet restore

# Generate a NuGet package
dotnet pack -c Release

# Create a local tool manifest (if not already exists)
dotnet new tool-manifest

# Install from the generated package
dotnet tool install --local ConfigFern --add-source ./bin/Release
```

#### Running the Local Tool

After local installation, you can run the tool using:
```bash
# Using dotnet tool
dotnet configfern [command]

# Or via the local tool path
dotnet tool run configfern [command]
```

#### Troubleshooting Local Installation
- Ensure you have the correct .NET SDK version
- Check that you're in the project root directory
- Verify that all dependencies are correctly restored
- Use `dotnet restore` to resolve any dependency issues

## Environment Setup

Set up the encryption key:
```bash
# Linux/macOS
export CONFIG_ENCRYPTION_KEY="your-secure-key-here"

# Windows (PowerShell)
$env:CONFIG_ENCRYPTION_KEY="your-secure-key-here"
```

## Usage

### Adding a Configuration Entry

```bash
# Add a simple configuration
configfern add --key "AppSettings:ApplicationName" --value "MyApp" --env "dev"

# Add an encrypted connection string
configfern add --key "ConnectionStrings:DefaultConnection" --value "Server=localhost;Database=mydb" --env "dev" --encrypted

# Add a nested configuration
configfern add --key "AppSettings:Features:EnableCache" --value "true" --env "prod"
```

### Listing Configuration Entries

```bash
configfern list --env dev
```

### Comparing Environments

```bash
configfern compare --env1 dev --env2 prod
```

### Validating Configuration

```bash
configfern validate --env dev
```

### Decrypting Configuration for Deployment

```bash
# Decrypt configuration for an environment
configfern decrypt --env prod

# Decrypt with custom output path
configfern decrypt --env prod --output "./deploy/appsettings.json"

# Force decrypt (for CI/CD pipelines)
configfern decrypt --env prod --force
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
- Decryption creates a separate file with plain-text values

## Best Practices

1. Use nested configuration sections for better organization
2. Always encrypt sensitive information (passwords, API keys, etc.)
3. Use descriptive configuration keys
4. Follow the standard .NET configuration naming conventions
5. Regularly validate configurations across environments
6. Compare environments before deployments
7. Keep encryption keys secure and separate from the configuration files
8. Delete decrypted configuration files immediately after use
9. Never commit decrypted configuration files to version control

## Deployment Workflow

1. Encrypt sensitive values during development
2. Use `validate` to ensure configuration integrity
3. Use `decrypt` command in secure deployment environments
4. Immediately delete the decrypted configuration file after use

## License

MIT License 