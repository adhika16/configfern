# ConfigFern 🌿

![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/adhika16/configfern/ci-cd.yml)
![NuGet](https://img.shields.io/nuget/v/ConfigFern)
![License](https://img.shields.io/badge/license-MIT-brightgreen)

ConfigFern is a .NET CLI tool that aims to assist in managing application configurations across various environments. It offers features for managing settings, encrypting sensitive values, validating configurations, and comparing environments, with the hope of making your configuration management a bit easier.

> **Note:** This project is currently in development mode. Features and functionality may change as development progresses.

## Quick Start

### Prerequisites

- .NET SDK 8.0

### Installation
```bash
dotnet tool install --global ConfigFern
```

### Basic Usage
```bash
# Add a configuration
configfern add --key "AppSettings:ApplicationName" --value "MyApp" --env "dev"

# List configurations
configfern list --env dev

# Compare environments
configfern compare --env1 dev --env2 prod
```

## Documentation

For detailed documentation, please visit our [Wiki](../../wiki):

- [Features Overview](../../wiki/Features)
- [Installation Guide](../../wiki/Installation-Guide)
- [Environment Setup](../../wiki/Environment-Setup)
- [Usage Guide](../../wiki/Usage-Guide)
- [Configuration Structure](../../wiki/Configuration-Structure)
- [Security](../../wiki/Security)
- [Best Practices](../../wiki/Best-Practices)
- [Deployment Workflow](../../wiki/Deployment-Workflow)

## License

MIT License 