namespace Localtunnel.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Localtunnel.Cli.Configuration;
    using Localtunnel.Connections;
    using Localtunnel.Http;
    using Localtunnel.Properties;
    using Localtunnel.Tunnels;

    internal static class TunnelDashboard
    {
        public static async Task Show(Tunnel tunnel, BaseConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var connectionHistory = new Stack<TunnelConnection>();
            var stringBuilder = new StringBuilder();
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
                    SetStatus(ConsoleColor.Green, $"({Resources.TunnelOnline} {version!.ToString(3)})", 0);
                    SetStatus(ConsoleColor.Blue, $"https://github.com/angelobreuer/localtunnel-client", 1);
                    previousWidth = Console.WindowWidth;
                }

                Update(tunnel, stringBuilder, startTime);
                UpdateConnections(tunnel, connectionHistory);
                await Task.Delay(100, CancellationToken.None);
            }

            // update status and run last update
            Console.Clear();
            SetStatus(ConsoleColor.Red, Resources.TunnelOffline, 0);
            Update(tunnel, stringBuilder, startTime);

            Console.CursorVisible = true;
        }

        private static StringBuilder AppendFullLine(this StringBuilder stringBuilder, string value)
        {
            var padding = Math.Max(0, Console.WindowWidth - value.Length);
            return stringBuilder.Append(value).Append(' ', padding).AppendLine();
        }

        private static string FormatDatePart(int value, string singular, string plural)
            => $"{value} {(value is 1 ? singular : plural)}";

        private static string FormatTimeSpan(TimeSpan time)
        {
            return FormatDatePart(time.Days, Resources.Day, Resources.Days) + ", " +
                   FormatDatePart(time.Hours, Resources.Hour, Resources.Hours) + ", " +
                   FormatDatePart(time.Minutes, Resources.Minute, Resources.Minutes) + ", " +
                   FormatDatePart(time.Seconds, Resources.Second, Resources.Seconds) + " ";
        }

        private static string GetIssuer(HttpRequest? request)
        {
            if (request is not null && request.Value.Headers.TryGetValue("x-real-ip", out var ip))
            {
                return ip.FirstOrDefault() ?? Resources.WaitingForRequest;
            }

            return Resources.WaitingForRequest;
        }

        private static void SetStatus(ConsoleColor color, string status, int offset)
        {
            var padding = Math.Max(0, Console.WindowWidth - status.Length);

            Console.SetCursorPosition(2, offset);
            Console.ForegroundColor = color;
            Console.WriteLine(status.PadRight(padding));
            Console.ResetColor();
        }

        private static void Update(Tunnel tunnel, StringBuilder stringBuilder, DateTimeOffset startTime)
        {
            stringBuilder.Clear();

            var connections = tunnel.Connections.Count();
            var elapsed = DateTimeOffset.UtcNow - startTime;

            stringBuilder.AppendFullLine($"  {Resources.TunnelId,-32} {tunnel.Information.Id}");
            stringBuilder.AppendFullLine($"  {Resources.TunnelURI,-32} {tunnel.Information.Url}");
            stringBuilder.AppendFullLine($"  {Resources.OnlineSince,-32} {FormatTimeSpan(elapsed)}");
            stringBuilder.AppendFullLine($"  {Resources.Port,-32} {tunnel.Information.Port}");
            stringBuilder.AppendFullLine($"  {Resources.MaxConcurrentConnections,-32} {tunnel.Information.MaximumConnections}");
            stringBuilder.AppendFullLine($"  {Resources.CurrentActiveConnections,-32} {connections}");

            Console.SetCursorPosition(left: 0, top: 3);
            Console.Write(stringBuilder);
        }

        private static void UpdateConnections(Tunnel tunnel, Stack<TunnelConnection> connectionHistory)
        {
            var connections = connectionHistory.Count;

            foreach (var connection in tunnel.Connections)
            {
                if (!connectionHistory.Contains(connection))
                {
                    if (connections >= 5)
                    {
                        connectionHistory.TryPop(out _);
                    }

                    connectionHistory.Push(connection);
                }
            }

            Console.WriteLine();
            Console.WriteLine("  " + Resources.ConnectionsHeader);

            foreach (var connection in connectionHistory)
            {
                var httpConnection = connection as ProxiedHttpTunnelConnection;
                var requestMessage = httpConnection?.HttpRequest;

                var bytesIn = httpConnection?.Statistics.BytesIn / 1024F;
                var bytesOut = httpConnection?.Statistics.BytesOut / 1024F;
                var stats = $"({bytesOut:0.0} KiB {Resources.Out}, {bytesIn:0.0} KiB {Resources.In})";

                var isEstablishing = httpConnection is not null
                    && httpConnection.Statistics.BytesOut is 0
                    && httpConnection.Statistics.BytesIn is 0;

                var requestUri = requestMessage?.PathAndQuery?.ToString() ?? "???";

                if (requestUri.Length > 50)
                {
                    requestUri = requestUri[..47] + "...";
                }

                var pipe = connection.IsDisposed ? "-X->" : "--->";

                var color = isEstablishing
                    ? ConsoleColor.Blue
                    : connection.IsDisposed
                    ? ConsoleColor.DarkGray
                    : ConsoleColor.Green;

                var indicator = isEstablishing ? 'E' : connection.IsDisposed ? 'C' : 'O';
                var status = $"     [{indicator}] {GetIssuer(requestMessage)} {pipe} {requestUri} {stats}";

                Console.ForegroundColor = color;
                Console.Write(status.PadRight(Console.WindowWidth));
                Console.WriteLine();
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
