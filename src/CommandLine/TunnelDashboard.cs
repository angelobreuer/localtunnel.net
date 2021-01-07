namespace Localtunnel.CommandLine
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Localtunnel.Tunnels;

    internal static class TunnelDashboard
    {
        public static async Task Show(Tunnel tunnel, BaseConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var startTime = DateTimeOffset.UtcNow;
            var previousWidth = -1;

            Console.CursorVisible = false;
            Console.Clear();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (previousWidth != Console.WindowWidth)
                {
                    // console resized or started, perform repaint
                    Console.Clear();
                    SetStatus(ConsoleColor.Green, $"(tunnel online @ tunnelclient {version.ToString(3)})", 0);
                    SetStatus(ConsoleColor.Blue, $"https://github.com/angelobreuer/localtunnel-client", 1);
                    previousWidth = Console.WindowWidth;
                }

                Update(tunnel, configuration, startTime);
                await Task.Delay(100, CancellationToken.None);
            }

            // update status and run last update
            SetStatus(ConsoleColor.Red, "(tunnel offline)", 0);
            Update(tunnel, configuration, startTime);

            Console.CursorVisible = true;
        }

        private static StringBuilder AppendFullLine(this StringBuilder stringBuilder, string value)
        {
            var padding = Math.Max(0, Console.WindowWidth - value.Length);
            return stringBuilder.Append(value).Append(' ', padding).AppendLine();
        }

        private static string FormatDatePart(int value, string unit)
            => $"{value} {(value is 1 ? unit : unit + "s")}";

        private static string FormatTimeSpan(TimeSpan time)
        {
            return FormatDatePart(time.Days, "day") + ", " +
                   FormatDatePart(time.Hours, "hour") + ", " +
                   FormatDatePart(time.Minutes, "minute") + ", " +
                   FormatDatePart(time.Seconds, "second") + " ";
        }

        private static void SetStatus(ConsoleColor color, string status, int offset)
        {
            var padding = Math.Max(0, Console.WindowWidth - status.Length);

            Console.SetCursorPosition(2, offset);
            Console.ForegroundColor = color;
            Console.WriteLine(status.PadRight(padding));
            Console.ResetColor();
        }

        private static void Update(Tunnel tunnel, BaseConfiguration configuration, DateTimeOffset startTime)
        {
            var scheme = configuration is HttpsProxyConfiguration ? "HTTPS" : "HTTP";
            var stringBuilder = new StringBuilder();
            var elapsed = DateTimeOffset.UtcNow - startTime;

            stringBuilder.AppendFullLine($"  Id:                         {tunnel.Information.Id}");
            stringBuilder.AppendFullLine($"  URI:                        {tunnel.Information.Url}");
            stringBuilder.AppendFullLine($"  Online since:               {FormatTimeSpan(elapsed)}");
            stringBuilder.AppendFullLine($"  Port:                       {tunnel.Information.Port}");
            stringBuilder.AppendFullLine($"  Max concurrent connections: {tunnel.Information.MaximumConnections}");
            stringBuilder.AppendFullLine($"  Current active connections: {tunnel.Connections.Count()}");
            stringBuilder.AppendFullLine(string.Empty);
            stringBuilder.AppendFullLine($"  Forwarding HTTP requests to {configuration.Host}:{configuration.Port} ({scheme})");
            stringBuilder.AppendFullLine(string.Empty);
            stringBuilder.AppendLine().AppendLine();

            Console.SetCursorPosition(left: 0, top: 3);
            Console.Write(stringBuilder);
        }
    }
}
