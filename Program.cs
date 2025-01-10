using System.CommandLine;
using ConfigFern.Handlers;
using ConfigFern.Models;
using ConfigFern.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var envConfig = new EnvironmentConfig();
configuration.Bind(envConfig);
envConfig.Validate();

var services = new ServiceCollection()
    .AddSingleton<IConfigurationService>(sp => 
        new ConfigurationService(
            Directory.GetCurrentDirectory(),
            envConfig.ConfigEncryptionKey))
    .AddSingleton<ConfigurationCommandHandlers>()
    .BuildServiceProvider();

var handlers = services.GetRequiredService<ConfigurationCommandHandlers>();

var rootCommand = new RootCommand("ConfigFern - Configuration Management Tool");

// Add command
var addCommand = new Command("add", "Add a configuration entry");
var keyOption = new Option<string>("--key", "Configuration key (use ':' for nested keys, e.g., 'ConnectionStrings:DefaultConnection')") { IsRequired = true };
var valueOption = new Option<string>("--value", "Configuration value") { IsRequired = true };
var envOption = new Option<string>("--env", "Environment (e.g., dev, staging, prod)") { IsRequired = true };
var encryptedOption = new Option<bool>("--encrypted", "Whether the value should be encrypted");
var descOption = new Option<string>("--desc", "Description of the configuration entry");

addCommand.AddOption(keyOption);
addCommand.AddOption(valueOption);
addCommand.AddOption(envOption);
addCommand.AddOption(encryptedOption);
addCommand.AddOption(descOption);

// List command
var listCommand = new Command("list", "List configuration entries");
var listEnvOption = new Option<string>("--env", "Environment to list") { IsRequired = true };
listCommand.AddOption(listEnvOption);

// Compare command
var compareCommand = new Command("compare", "Compare configurations between environments");
var env1Option = new Option<string>("--env1", "First environment") { IsRequired = true };
var env2Option = new Option<string>("--env2", "Second environment") { IsRequired = true };
compareCommand.AddOption(env1Option);
compareCommand.AddOption(env2Option);

// Validate command
var validateCommand = new Command("validate", "Validate configuration entries");
var validateEnvOption = new Option<string>("--env", "Environment to validate") { IsRequired = true };
validateCommand.AddOption(validateEnvOption);

// Set up handlers
addCommand.SetHandler(
    (key, value, env, encrypted, desc) => handlers.HandleAddAsync(key, value, env, encrypted, desc),
    keyOption, valueOption, envOption, encryptedOption, descOption);

listCommand.SetHandler(
    (env) => handlers.HandleListAsync(env),
    listEnvOption);

compareCommand.SetHandler(
    (env1, env2) => handlers.HandleCompareAsync(env1, env2),
    env1Option, env2Option);

validateCommand.SetHandler(
    (env) => handlers.HandleValidateAsync(env),
    validateEnvOption);

rootCommand.AddCommand(addCommand);
rootCommand.AddCommand(listCommand);
rootCommand.AddCommand(compareCommand);
rootCommand.AddCommand(validateCommand);

await rootCommand.InvokeAsync(args); 