using System.Text.Json;
using ConfigFern.Services;
using Spectre.Console;

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
            await AnsiConsole.Status()
                .StartAsync($"Adding configuration entry '{key}' to environment '{env}'...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

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
                        AnsiConsole.MarkupLine(isUpdate
                            ? $"[green]Updated[/] configuration entry '{key}' in environment '{env}'"
                            : $"[green]Added[/] configuration entry '{key}' to environment '{env}'");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Error:[/] Invalid configuration entry");
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public async Task HandleListAsync(string env)
    {
        try
        {
            await AnsiConsole.Status()
                .StartAsync($"Loading configuration entries for environment '{env}'...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("blue"));

                    var config = await _configService.LoadConfigurationAsync(env);
                    
                    if (!config.Any())
                    {
                        AnsiConsole.MarkupLine($"No configuration entries found for environment '[blue]{env}[/]'");
                        return;
                    }

                    AnsiConsole.MarkupLine($"\nConfiguration entries for environment '[blue]{env}[/]':");
                    PrintConfiguration(config);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public async Task HandleCompareAsync(string env1, string env2)
    {
        try
        {
            await AnsiConsole.Status()
                .StartAsync($"Comparing environments '{env1}' and '{env2}'...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("yellow"));

                    var differences = await _configService.CompareEnvironmentsAsync(env1, env2);

                    if (!differences.Any())
                    {
                        AnsiConsole.MarkupLine($"No differences found between environments '[yellow]{env1}[/]' and '[yellow]{env2}[/]'");
                        return;
                    }

                    AnsiConsole.MarkupLine($"\nDifferences between environments '[yellow]{env1}[/]' and '[yellow]{env2}[/]':");
                    foreach (var (key, values) in differences)
                    {
                        AnsiConsole.MarkupLine($"\nKey: [blue]{key}[/]");
                        AnsiConsole.MarkupLine($"- {env1}: {FormatValue(values[env1])}");
                        AnsiConsole.MarkupLine($"- {env2}: {FormatValue(values[env2])}");
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public async Task HandleValidateAsync(string env)
    {
        try
        {
            await AnsiConsole.Status()
                .StartAsync($"Validating configuration for environment '{env}'...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("purple"));

                    var config = await _configService.LoadConfigurationAsync(env);
                    
                    if (!config.Any())
                    {
                        AnsiConsole.MarkupLine($"No configuration entries found for environment '[purple]{env}[/]'");
                        return;
                    }

                    var isValid = _configService.ValidateConfiguration(config);
                    AnsiConsole.MarkupLine(isValid
                        ? $"Configuration for environment '[purple]{env}[/]' is [green]valid[/]"
                        : $"Configuration for environment '[purple]{env}[/]' is [red]invalid[/]");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    public async Task HandleDecryptAsync(string env, string output, bool force)
    {
        try
        {
            var inputFile = $"appsettings.{env}.json";
            var outputFile = string.IsNullOrEmpty(output) 
                ? $"appsettings.{env}.decrypted.json"
                : output;

            if (!force)
            {
                var table = new Table();
                table.AddColumn("[yellow]⚠️ WARNING[/]");
                table.AddRow(new Panel(
                    "This command will create a new file containing all decrypted values.\n" +
                    "This means sensitive information will be stored in plain text.\n" +
                    "This should only be used as part of a secure deployment process.\n" +
                    "[red]This operation cannot be reversed![/]")
                    .Expand()
                    .RoundedBorder()
                    .BorderColor(Color.Yellow));

                AnsiConsole.Write(table);

                if (!AnsiConsole.Confirm("Do you want to proceed?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled by user[/]");
                    return;
                }
            }

            await AnsiConsole.Status()
                .StartAsync($"Decrypting configuration for environment '{env}'...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("red"));

                    // Load the configuration with decryption enabled
                    var config = await _configService.LoadConfigurationAsync(env);
                    
                    if (!config.Any())
                    {
                        AnsiConsole.MarkupLine($"No configuration entries found for environment '[red]{env}[/]'");
                        return;
                    }

                    // Save the decrypted configuration to the new file
                    var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(outputFile, configJson);

                    AnsiConsole.MarkupLine($"\n[green]Successfully[/] created decrypted configuration file:");
                    AnsiConsole.MarkupLine($"[blue]Source:[/] {inputFile}");
                    AnsiConsole.MarkupLine($"[blue]Output:[/] {outputFile}");
                    AnsiConsole.MarkupLine("\n[yellow]⚠️ Remember to:[/]");
                    AnsiConsole.MarkupLine("  • Keep this file secure and never commit it to version control");
                    AnsiConsole.MarkupLine("  • Delete it immediately after use");
                    AnsiConsole.MarkupLine("  • Use it only in secure deployment environments");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
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