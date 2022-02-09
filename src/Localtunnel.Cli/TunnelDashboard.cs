namespace Localtunnel.Cli;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Properties;
using Localtunnel.Tracing;
using Localtunnel.Tunnels;

internal static class TunnelDashboard
{
    public static async Task Show(Tunnel tunnel, HistoryTraceListener historyTraceListener, CancellationToken cancellationToken = default)
    {
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
            UpdateConnections(tunnel, historyTraceListener.Entries);
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

    private static string GetIssuer(HttpRequestMessage? request)
    {
        if (request is not null && request.Headers.TryGetValues("x-real-ip", out var ip))
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

        var connections = 0; // TODO
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

    private static void UpdateConnections(Tunnel tunnel, IEnumerable<RequestTraceEntry> connectionHistory)
    {
        static string FormatByteSize(double value)
        {
            if (value > 1024F * 1024F)
            {
                return $"{value / 1024F / 1024F:0.0} MiB";
            }

            return $"{value / 1024F:0.0} KiB";
        }

        Console.WriteLine();
        Console.WriteLine("  " + Resources.ConnectionsHeader);

        foreach (var connection in connectionHistory)
        {
            var requestMessage = connection.RequestMessage;

            var statistics = connection.Statistics;
            var stats = $"({FormatByteSize(statistics.BytesOut)} {Resources.Out}, {FormatByteSize(statistics.BytesIn)} {Resources.In})";

            var isEstablishing = connection.RequestMessage is not null && !connection.IsCompleted;
            var requestUri = requestMessage?.RequestUri?.PathAndQuery?.ToString() ?? "???";

            if (requestUri.Length > 50)
            {
                requestUri = requestUri[..47] + "...";
            }

            var pipe = connection.IsCompleted ? "-X->" : "--->";

            var color = isEstablishing
                ? ConsoleColor.Blue
                : connection.IsCompleted
                ? ConsoleColor.DarkGray
                : ConsoleColor.Green;

            var indicator = isEstablishing ? 'E' : connection.IsCompleted ? 'C' : 'O';
            var status = $"     [{indicator}] {GetIssuer(requestMessage)} {pipe} {requestUri} {stats}";

            Console.ForegroundColor = color;
            Console.Write(status.PadRight(Console.WindowWidth));
            Console.WriteLine();
        }

        Console.ResetColor();
        Console.WriteLine();
    }
}
