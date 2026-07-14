using System;
using RabbitMqTestClient = VertiportNexus.MqTestClient.Services.MqTestClient;

namespace VertiportNexus.MqTestClient
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string hostName = args.Length >= 1 ? args[0] : "127.0.0.1";
            int port = args.Length >= 2 ? int.Parse(args[1]) : 5672;
            int statusFrequency = args.Length >= 3 ? int.Parse(args[2]) : 2;
            string statusMsgId = CreateMessageId("STATUS");
            bool isStatusPublishing = false;

            Console.WriteLine("VertiportNexus MQ Test Client");
            Console.WriteLine("Target: " + hostName + ":" + port);
            Console.WriteLine();

            try
            {
                using (RabbitMqTestClient client = new RabbitMqTestClient(hostName, port))
                {
                    client.StartResponseConsumers();

                    while (true)
                    {
                        PrintMenu();
                        string menu = Console.ReadLine()?.Trim();

                        if (menu == "0")
                            break;

                        switch (menu)
                        {
                            case "1":
                                client.PublishGetState(statusMsgId, statusFrequency);
                                isStatusPublishing = true;
                                break;

                            case "2":
                                client.PublishGetState(statusMsgId, 0);
                                isStatusPublishing = false;
                                break;

                            case "3":
                                client.PublishDetectOn(CreateMessageId("DETECT-ON"));
                                break;

                            case "4":
                                client.PublishDetectCont(CreateMessageId("DETECT-CONT"));
                                break;

                            case "5":
                                client.PublishDetectOff(CreateMessageId("DETECT-OFF"));
                                break;

                            case "6":
                                client.PublishUnsupportedCommand(CreateMessageId("UNSUPPORTED"));
                                break;

                            case "7":
                                client.PublishPtzControlMode(CreateMessageId("PTZ-MANUAL"), "MANUAL");
                                break;

                            case "8":
                                client.PublishPtzControlMode(CreateMessageId("PTZ-AUTO"), "AUTO");
                                break;

                            case "9":
                                PublishPtzAbsolute(client);
                                break;

                            case "10":
                                PublishPtzContinuous(client);
                                break;

                            case "11":
                                PublishPtzZoom(client);
                                break;

                            default:
                                Console.WriteLine("Unknown menu: " + menu);
                                break;
                        }
                    }

                    if (isStatusPublishing)
                        client.PublishGetState(statusMsgId, 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("[RabbitMQ] Test Failed");
                Console.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("1. get_state start");
            Console.WriteLine("2. get_state stop");
            Console.WriteLine("3. detect_on");
            Console.WriteLine("4. detect_cont");
            Console.WriteLine("5. detect_off");
            Console.WriteLine("6. unsupported command");
            Console.WriteLine("7. ptz_move mode manual");
            Console.WriteLine("8. ptz_move mode auto");
            Console.WriteLine("9. ptz_move absolute");
            Console.WriteLine("10. ptz_move continuous");
            Console.WriteLine("11. ptz_move zoom");
            Console.WriteLine("0. exit");
            Console.Write("Select: ");
        }

        private static void PublishPtzAbsolute(RabbitMqTestClient client)
        {
            float pan;
            float tilt;

            Console.Write("Pan degree (0~360): ");

            if (!float.TryParse(Console.ReadLine(), out pan))
            {
                Console.WriteLine("Invalid Pan.");
                return;
            }

            Console.Write("Tilt degree (-90~90): ");

            if (!float.TryParse(Console.ReadLine(), out tilt))
            {
                Console.WriteLine("Invalid Tilt.");
                return;
            }

            client.PublishPtzAbsolute(CreateMessageId("PTZ-ABSOLUTE"), pan, tilt);
        }

        private static void PublishPtzContinuous(RabbitMqTestClient client)
        {
            Console.WriteLine("Commands: stop, left, right, up, down, left_up, right_up, left_down, right_down");
            Console.Write("Command: ");
            string command = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine("Command is empty.");
                return;
            }

            client.PublishPtzContinuous(CreateMessageId("PTZ-CONTINUOUS"), command);
        }

        private static void PublishPtzZoom(RabbitMqTestClient client)
        {
            float zoom;

            Console.Write("Zoom ratio (1~66): ");

            if (!float.TryParse(Console.ReadLine(), out zoom))
            {
                Console.WriteLine("Invalid Zoom.");
                return;
            }

            client.PublishPtzZoom(CreateMessageId("PTZ-ZOOM"), zoom);
        }

        private static string CreateMessageId(string commandName)
        {
            return "MQ-TEST-" + commandName + "-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        }
    }
}
