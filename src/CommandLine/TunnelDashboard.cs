namespace Localtunnel.CommandLine
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Localtunnel.Properties;
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
                    SetStatus(ConsoleColor.Green, $"({Resources.TunnelOnline} {version!.ToString(3)})", 0);
                    SetStatus(ConsoleColor.Blue, $"https://github.com/angelobreuer/localtunnel-client", 1);
                    previousWidth = Console.WindowWidth;
                }

                Update(tunnel, configuration, startTime);
                await Task.Delay(100, CancellationToken.None);
            }

            // update status and run last update
            SetStatus(ConsoleColor.Red, Resources.TunnelOffline, 0);
            Update(tunnel, configuration, startTime);

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
            var stringBuilder = new StringBuilder();
            var elapsed = DateTimeOffset.UtcNow - startTime;
            var scheme = configuration is HttpsProxyConfiguration ? Resources.SchemeHttp : Resources.SchemeHttps;

            stringBuilder.AppendFullLine($"  {Resources.TunnelId,-32} {tunnel.Information.Id}");
            stringBuilder.AppendFullLine($"  {Resources.TunnelURI,-32} {tunnel.Information.Url}");
            stringBuilder.AppendFullLine($"  {Resources.OnlineSince,-32} {FormatTimeSpan(elapsed)}");
            stringBuilder.AppendFullLine($"  {Resources.Port,-32} {tunnel.Information.Port}");
            stringBuilder.AppendFullLine($"  {Resources.MaxConcurrentConnections,-32} {tunnel.Information.MaximumConnections}");
            stringBuilder.AppendFullLine($"  {Resources.CurrentActiveConnections,-32} {tunnel.Connections.Count()}");
            stringBuilder.AppendFullLine(string.Empty);
            stringBuilder.AppendLine().AppendLine();

            Console.SetCursorPosition(left: 0, top: 3);
            Console.Write(stringBuilder);
        }
    }
}
