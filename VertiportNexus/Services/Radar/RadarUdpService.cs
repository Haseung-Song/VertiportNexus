using Serilog;
using System;
using System.Net;
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
        /// [Radar UDP] Raw Packet HEX 로그 저장 여부
        /// 
        /// 평상시에는 Packet 수신 여부와 응답 결과만 저장하고,
        /// 원본 HEX 분석이 필요한 경우에만 true로 변경한다.
        /// </summary>
        private static readonly bool ENABLE_RADAR_RAW_PACKET_LOG =
            false;

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
                    Log.Warning(
                        "[RADAR][UDP] Receive Skip : Empty Packet");

                    return;
                }

                Log.Information(
                    "[RADAR][UDP] Packet Received : Remote={RemoteEndPoint}, Length={Length}, ReceivedTime={ReceivedTime:yyyy-MM-dd HH:mm:ss.fff}",
                    remoteEndPoint,
                    receivedData.Length,
                    receivedTime);

                if (ENABLE_RADAR_RAW_PACKET_LOG)
                {
                    Log.Debug(
                        "[RADAR][UDP] RECV {Hex}",
                        BitConverter
                            .ToString(
                                receivedData)
                            .Replace(
                                "-",
                                " "));
                }

                byte[] responsePacket =
                    _radarCommandHandler
                        .Handle(
                            receivedData);

                if (responsePacket == null ||
                    responsePacket.Length == 0)
                {
                    Log.Warning(
                        "[RADAR][UDP] Response Skip : Empty Packet");

                    return;
                }

                bool isSent =
                    _radarUdpClientService
                        .Send(
                            responsePacket,
                            remoteEndPoint);

                Log.Information(
                    "[RADAR][UDP] Response Send Result : {IsSent}, Remote={RemoteEndPoint}, Length={Length}",
                    isSent,
                    remoteEndPoint,
                    responsePacket.Length);

                if (ENABLE_RADAR_RAW_PACKET_LOG)
                {
                    Log.Debug(
                        "[RADAR][UDP] SEND {Hex}",
                        BitConverter
                            .ToString(
                                responsePacket)
                            .Replace(
                                "-",
                                " "));
                }

            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[RADAR][UDP] Packet Handle Failed");
            }

        }
        #endregion
    }

}
