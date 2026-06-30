using System;
using System.Net;
using System.Net.Sockets;
using VertiportNexus.Common;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] UDP Mock 송신 서비스
    /// 
    /// 실제 Radar 장비 연동 전,
    /// Mock Packet을 UDP로 송신하여
    /// RadarUdpService 수신 / Parser / Handler / PTZ 제어 흐름을 검증한다.
    /// </summary>
    internal class RadarUdpMockSenderService
    {
        #region [Fields]

        /// <summary>
        /// [Radar] Mock Packet 테스트 서비스
        /// 
        /// UDP로 송신할 Radar Tracking / BIST Packet을 생성한다.
        /// </summary>
        private readonly RadarMockPacketTestService _radarMockPacketTestService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RadarUdpMockSenderService] 생성자
        /// </summary>
        /// <param name="radarMockPacketTestService">
        /// [Radar] Mock Packet 테스트 서비스
        /// </param>
        internal RadarUdpMockSenderService(
            RadarMockPacketTestService radarMockPacketTestService)
        {
            _radarMockPacketTestService =
                radarMockPacketTestService
                ?? throw new ArgumentNullException(
                    nameof(radarMockPacketTestService));
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Radar] Tracking Request UDP Loopback 테스트
        /// 
        /// 생성한 Tracking Request Mock Packet을
        /// 지정한 IP / Port로 UDP 송신한다.
        /// </summary>
        /// <param name="ipAddress">
        /// 송신 대상 IP
        /// </param>
        /// <param name="port">
        /// 송신 대상 Port
        /// </param>
        public void SendTrackingRequest(
            string ipAddress,
            int port)
        {
            byte[] packet =
                _radarMockPacketTestService
                    .CreateTrackingRequestPacket();

            SendPacket(
                "[RADAR][UDP][MOCK] Tracking Request Send",
                packet,
                ipAddress,
                port);
        }

        /// <summary>
        /// [Radar] BIST Request UDP Loopback 테스트
        /// 
        /// 생성한 BIST Request Mock Packet을
        /// 지정한 IP / Port로 UDP 송신한다.
        /// </summary>
        /// <param name="ipAddress">
        /// 송신 대상 IP
        /// </param>
        /// <param name="port">
        /// 송신 대상 Port
        /// </param>
        public void SendBistRequest(
            string ipAddress,
            int port)
        {
            byte[] packet =
                _radarMockPacketTestService
                    .CreateBistRequestPacket();

            SendPacket(
                "[RADAR][UDP][MOCK] BIST Request Send",
                packet,
                ipAddress,
                port);
        }

        #endregion

        #region [Send Methods]

        /// <summary>
        /// [Radar] UDP Packet 송신
        /// </summary>
        /// <param name="title">
        /// 로그 제목
        /// </param>
        /// <param name="packet">
        /// 송신 Packet
        /// </param>
        /// <param name="ipAddress">
        /// 송신 대상 IP
        /// </param>
        /// <param name="port">
        /// 송신 대상 Port
        /// </param>
        private void SendPacket(
            string title,
            byte[] packet,
            string ipAddress,
            int port)
        {
            if (packet == null ||
                packet.Length == 0)
            {
                Console.WriteLine(
                    "[RADAR][UDP][MOCK] Send Failed : Empty Packet");

                return;
            }

            try
            {
                using (UdpClient udpClient =
                    new UdpClient())
                {
                    IPEndPoint remoteEndPoint =
                        new IPEndPoint(
                            IPAddress.Parse(
                                ipAddress),
                            port);

                    ConsoleLogHelper.PrintLine();
                    Console.WriteLine(title);
                    Console.WriteLine("[RADAR][UDP][MOCK] Target : " + ipAddress + ":" + port);
                    Console.WriteLine("[RADAR][UDP][MOCK] Length : " + packet.Length);

                    PrintHexData(
                        "[RADAR][UDP][MOCK] Packet",
                        packet);

                    ConsoleLogHelper.PrintLine();

                    udpClient
                        .Send(
                            packet,
                            packet.Length,
                            remoteEndPoint);
                }

                ConsoleLogHelper.PrintLine();
                Console.WriteLine(
                    "[RADAR][UDP][MOCK] Send Complete");
                ConsoleLogHelper.PrintLine();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP][MOCK] Send Failed");
                Console.WriteLine("[RADAR][UDP][MOCK] Error : " + ex.Message);
                ConsoleLogHelper.PrintLine();
            }

        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// [byte[]] HEX 로그 출력
        /// </summary>
        /// <param name="title">
        /// 로그 제목
        /// </param>
        /// <param name="data">
        /// 출력 대상 byte 배열
        /// </param>
        private void PrintHexData(
            string title,
            byte[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                Console.WriteLine(title + " : Empty");
                return;
            }

            Console.WriteLine(
                title);

            Console.WriteLine(
                BitConverter
                    .ToString(
                        data)
                    .Replace(
                        "-",
                        " "));
        }
        #endregion
    }

}
