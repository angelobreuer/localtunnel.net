namespace Localtunnel.CommandLine.Commands
{
    using System.CommandLine;
    using Localtunnel.Properties;

    internal class RootCliCommand : RootCommand
    {
        public RootCliCommand()
        {
            Add(new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: Resources.VerboseDescription));

            Add(new Option<bool>(
                aliases: new[] { "--browser", "-b" },
                description: Resources.BrowserDescription));

            Add(new Option<int>(
                aliases: new[] { "--max-connections", "-c" },
                getDefaultValue: () => 10,
                description: Resources.MaximumConnectionsDescription));

            Add(new Option<string?>(
                aliases: new[] { "--subdomain", "-d" },
                description: Resources.SubdomainDescription));

            Add(new Option<string>(
                aliases: new[] { "--server", "-s" },
                getDefaultValue: () => "localtunnel.me",
                description: Resources.ServerDescription));

            Add(new Option<string>(
                 aliases: new[] { "--host", "-h" },
                 getDefaultValue: () => "localhost",
                 description: Resources.HostDescription));

            Add(new Option<int>(
                aliases: new[] { "--port", "-p" },
                getDefaultValue: () => 80,
                description: Resources.PortDescription));

            Add(new Option<int>(
                alias: "--receive-buffer-size",
                getDefaultValue: () => 64 * 1024,
                description: Resources.ReceiveBufferSizeDescription));

            Add(new Option<bool>(
                alias: "--passthrough",
                getDefaultValue: () => false,
                description: Resources.PassthroughDescription));

            Add(new HttpCliCommand());
            Add(new HttpsCliCommand());
        }
    }
}
