using System;
using System.Net;
using VertiportNexus.Common;
using VertiportNexus.Services.Communication.UDP;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] UDP 연동 서비스
    /// 
    /// [CSR]에서 전달되는 UDP Packet을 수신하고,
    /// [RadarCommandHandler] 처리 결과로 생성된 응답 Packet을
    /// 송신자에게 다시 UDP로 송신한다.
    /// </summary>
    internal class RadarUdpService
    {
        #region [Fields]

        /// <summary>
        /// [Radar] UDP 통신 서비스
        /// </summary>
        private readonly UdpClientService _radarUdpClientService;

        /// <summary>
        /// [Radar] Command 처리 서비스
        /// </summary>
        private readonly RadarCommandHandler _radarCommandHandler;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RadarUdpService] 생성자
        /// </summary>
        internal RadarUdpService(
            UdpClientService radarUdpClientService,
            RadarCommandHandler radarCommandHandler)
        {
            _radarUdpClientService =
                radarUdpClientService
                ?? throw new ArgumentNullException(
                    nameof(radarUdpClientService));

            _radarCommandHandler =
                radarCommandHandler
                ?? throw new ArgumentNullException(
                    nameof(radarCommandHandler));

            // [Radar] UDP 수신 이벤트 연결
            //
            // UdpClientService에서 수신한 byte[] 데이터를
            // RadarUdpService 내부 처리 함수로 전달한다.
            _radarUdpClientService.MessageReceived +=
                OnRadarUdpMessageReceived;
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// </summary>
        public void StartReceive(
            int localPort)
        {
            _radarUdpClientService
                .StartReceive(
                    localPort);
        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// </summary>
        public void StopReceive()
        {
            _radarUdpClientService
                .StopReceive();
        }

        #endregion

        #region [Receive Event Methods]

        /// <summary>
        /// [Radar] UDP 수신 데이터 처리
        /// </summary>
        private void OnRadarUdpMessageReceived(
            byte[] receivedData,
            IPEndPoint remoteEndPoint,
            DateTime receivedTime)
        {
            try
            {
                if (receivedData == null ||
                    receivedData.Length == 0)
                {
                    Console.WriteLine("[RADAR][UDP] Receive Skip : Empty Packet");

                    return;
                }

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Packet Received");
                Console.WriteLine("[RADAR][UDP] Remote : " + remoteEndPoint);
                Console.WriteLine("[RADAR][UDP] Length : " + receivedData.Length);
                ConsoleLogHelper.PrintLine();

                byte[] responsePacket =
                    _radarCommandHandler
                        .Handle(
                            receivedData);

                if (responsePacket == null ||
                    responsePacket.Length == 0)
                {
                    Console.WriteLine("[RADAR][UDP] Response Skip : Empty Packet");

                    return;
                }

                bool isSent =
                    _radarUdpClientService
                        .Send(
                            responsePacket,
                            remoteEndPoint);

                Console.WriteLine("[RADAR][UDP] Response Send Result : " + isSent);
                ConsoleLogHelper.PrintLine();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Packet Handle Failed");
                Console.WriteLine("[RADAR][UDP] Error : " + ex.Message);
                ConsoleLogHelper.PrintLine();
            }

        }
        #endregion
    }

}
