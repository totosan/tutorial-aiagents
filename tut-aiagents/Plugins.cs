using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace tut_aiagents.Plugins
{
    public sealed class NetworkMonitor
    {
        [KernelFunction()]
        [Description("Pings a host and returns the response time")]
        public static long Ping(string zielHost)
        {
            try
            {
                Ping ping = new Ping();
                PingReply antwort = ping.Send(zielHost);

                if (antwort.Status == IPStatus.Success)
                {
                    return antwort.RoundtripTime;
                }
                else
                {
                    // Indikator für fehlgeschlagenen Ping
                    return -1;
                }
            }
            catch
            {
                // Bei Exceptions ebenfalls -1 zurückgeben
                return -1;
            }
        }

        [KernelFunction(), Description("Returns whether a network connection is available")]
        public static bool IsConnected()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        [KernelFunction("adapterInfos"), Description("Returns information about the network adapters")]
        public static List<string> adapterInfos()
        {
            List<string> adapterInfos = new List<string>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                string info = $"Name: {ni.Name}, Typ: {ni.NetworkInterfaceType}, Status: {ni.OperationalStatus}";
                adapterInfos.Add(info);
            }

            return adapterInfos;
        }

        [KernelFunction("dnsResolvable"), Description("Returns whether a hostname can be resolved")]
        public static bool dnsResolvable(string hostname)
        {
            try
            {
                Dns.GetHostEntry(hostname);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [KernelFunction("traceRt"), Description("Performs a traceroute to a host")]
        public static List<string> traceRt(string zielHost)
        {
            List<string> hops = new List<string>();
            const int maximalHops = 30;
            const int timeout = 1000;

            for (int ttl = 1; ttl <= maximalHops; ttl++)
            {
                PingOptions options = new PingOptions(ttl, true);
                Ping ping = new Ping();
                byte[] buffer = new byte[32];
                PingReply antwort = ping.Send(zielHost, timeout, buffer, options);

                if (antwort.Status == IPStatus.Success)
                {
                    hops.Add(antwort.Address.ToString());
                    break;
                }
                else if (antwort.Status == IPStatus.TtlExpired)
                {
                    hops.Add(antwort.Address.ToString());
                }
                else
                {
                    hops.Add("*");
                }
            }

            return hops;
        }
    }

    public sealed class HostMetrics
    {
        // for sake of simplicity, we return fixed values

        [KernelFunction("cpuUsage"), Description("Returns the current CPU usage in percent. E.g. 50 indicates 50% CPU usage")]
        public static float cpuUsage()
        {
            return 75f;
        }

        [KernelFunction("memoryUsage"), Description("Returns the current memory usage in percent")]
        public static float memoryUsage()
        {
            return 32f;
        }

        [KernelFunction("diskUsage"), Description("Returns the current disk usage in percent")]
        public static float diskUsage()
        {
            return 95f;
        }
    }
}