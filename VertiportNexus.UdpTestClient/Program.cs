using System;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.UdpTestClient.Services;

namespace VertiportNexus.UdpTestClient
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            RunAsync(args).GetAwaiter().GetResult();
        }

        private static async Task RunAsync(string[] args)
        {
            string host = args.Length >= 1 ? args[0] : "127.0.0.1";
            int port = args.Length >= 2 ? int.Parse(args[1]) : 5000;
            ushort nextTargetId = 1;

            Console.WriteLine("VertiportNexus UDP Test Client");
            Console.WriteLine("Target: " + host + ":" + port);
            Console.WriteLine();

            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (UdpRadarTestClient client = new UdpRadarTestClient(host, port))
                {
                    Task receiveTask = client.ReceiveLoopAsync(cts.Token);

                    while (true)
                    {
                        PrintMenu();
                        string menu = Console.ReadLine()?.Trim();

                        if (menu == "0")
                            break;

                        switch (menu)
                        {
                            case "1":
                                await client.SendTrackingRequestAsync(nextTargetId++, 0.1745329f, 0.0872665f);
                                break;

                            case "2":
                                await SendCustomTrackingRequestAsync(client);
                                break;

                            default:
                                Console.WriteLine("Unknown menu: " + menu);
                                break;
                        }
                    }

                    cts.Cancel();

                    try
                    {
                        await receiveTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("[UDP] Test Failed");
                Console.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private static async Task SendCustomTrackingRequestAsync(UdpRadarTestClient client)
        {
            Console.Write("Target ID: ");
            ushort targetId;

            if (!ushort.TryParse(Console.ReadLine(), out targetId))
            {
                Console.WriteLine("Invalid Target ID.");
                return;
            }

            Console.Write("Azimuth degree: ");
            float azimuthDegree;

            if (!float.TryParse(Console.ReadLine(), out azimuthDegree))
            {
                Console.WriteLine("Invalid Azimuth.");
                return;
            }

            Console.Write("Elevation degree: ");
            float elevationDegree;

            if (!float.TryParse(Console.ReadLine(), out elevationDegree))
            {
                Console.WriteLine("Invalid Elevation.");
                return;
            }

            await client.SendTrackingRequestAsync(targetId, DegreeToRadian(azimuthDegree), DegreeToRadian(elevationDegree));
        }

        private static float DegreeToRadian(float degree)
        {
            return (float)(degree * Math.PI / 180.0);
        }

        private static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("1. Send default Tracking Request (Azimuth 10°, Elevation 5°)");
            Console.WriteLine("2. Send custom Tracking Request");
            Console.WriteLine("0. Exit");
            Console.Write("Select: ");
        }
    }
}
