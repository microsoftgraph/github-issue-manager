// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubIssueManager.Options;
using GitHubIssueManager.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
{
    var options = new OpenApiConfigurationOptions
    {
        Info = new()
        {
            Version = "1.0.0",
            Title = "GitHub issues management API",
            Description = "API to manage issues in a GitHub repository",
        },
    };

    var devTunnel = builder.Configuration.GetValue<string>("DevTunnel");
    if (!string.IsNullOrEmpty(devTunnel))
    {
        options.Servers =
        [
            new() { Url = $"{devTunnel}/api" },
        ];
    }

    return options;
});

builder.Services.AddOptions<GitHubOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection("GitHubOptions").Bind(settings);
    });

builder.Services.AddOptions<GraphOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection("GraphOptions").Bind(settings);
    });

builder.Services.AddSingleton<GraphConnectorService>();
builder.Services.AddSingleton<GitHubIssuesService>();

builder.Build().Run();
