using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ProtobufLanguageServer.Documents;
using ProtobufLanguageServer.Completions;

namespace ProtobufLanguageServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var logLevel = LogLevel.Information;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].IndexOf("debug", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(1000);
                    }

                    Debugger.Break();
                    continue;
                }

                if (args[i] == "--logLevel" && i + 1 < args.Length)
                {
                    var logLevelString = args[++i];
                    if (!Enum.TryParse(logLevelString, out logLevel))
                    {
                        logLevel = LogLevel.Information;
                        Console.WriteLine($"Invalid log level '{logLevelString}'. Defaulting to {logLevel.ToString()}.");
                    }
                }
            }

            var threadManager = new ForegroundThreadManager();
            var snapshotManager = new WorkspaceSnapshotManager(threadManager);
            var server = await LanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithLoggerFactory(new LoggerFactory())
                    .AddDefaultLoggingProvider()
                    .WithMinimumLogLevel(LogLevel.Trace)

                    // We're adding a new endpoint that will handle a specific set of language server features.
                    .WithHandler<TextDocumentSynchronizationEndpoint>()
                    .WithHandler<ProtoCompletionEndpoint>()
                    .WithHandler<ProtoDefinitionEndpoint>()
                    .WithServices(services =>
                    {
                        services.AddSingleton<ForegroundThreadManager>(threadManager);
                        services.AddSingleton<WorkspaceSnapshotManager>(snapshotManager);
                        services.AddSingleton<ProtoCompletionItemProvider, TopLevelKeywordCompletionItemProvider>();
                        services.AddSingleton<ProtoCompletionItemProvider, EnumKeywordCompletionItemProvider>();
                        services.AddSingleton<ProtoCompletionItemProvider, ServiceKeywordCompletionItemProvider>();
                        services.AddSingleton<ProtoCompletionItemProvider, MessageKeywordCompletionItemProvider>();
                    }));

            await Task.Factory.StartNew(
                () => snapshotManager.InitializeAsync(server),
                CancellationToken.None,
                TaskCreationOptions.None,
                threadManager.ForegroundScheduler);

            var languageServer = (LanguageServer)server;
            languageServer.MinimumLogLevel = logLevel;

            await server.WaitForExit;
        }
    }
}
