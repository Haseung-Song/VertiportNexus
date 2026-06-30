using System;
using VertiportNexus.Common;
using VertiportNexus.Common.Constants;
using VertiportNexus.Models.Radar;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] Command Handler
    /// 
    /// Radar Packet의 Command를 기준으로
    /// 추적 요청 / BIST 요청을 분기 처리하고,
    /// 응답 Packet을 생성한다.
    /// </summary>
    internal class RadarCommandHandler
    {
        #region [Fields]

        /// <summary>
        /// [Radar] Packet Parser
        /// </summary>
        private readonly RadarPacketParser _radarPacketParser;

        /// <summary>
        /// [Radar] Packet Builder
        /// </summary>
        private readonly RadarPacketBuilder _radarPacketBuilder;

        /// <summary>
        /// [Radar] 상태 저장 서비스
        /// </summary>
        private readonly RadarStateProvider _radarStateProvider;

        /// <summary>
        /// [Radar] 추적 제어 서비스
        /// </summary>
        private readonly RadarTrackingControlService _radarTrackingControlService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RadarCommandHandler] 생성자
        /// </summary>
        /// <param name="radarPacketParser">
        /// [Radar] Packet Parser
        /// </param>
        /// <param name="radarPacketBuilder">
        /// [Radar] Packet Builder
        /// </param>
        /// <param name="radarStateProvider">
        /// [Radar] 상태 저장 서비스
        /// </param>
        internal RadarCommandHandler(
            RadarPacketParser radarPacketParser,
            RadarPacketBuilder radarPacketBuilder,
            RadarStateProvider radarStateProvider,
            RadarTrackingControlService radarTrackingControlService)
        {
            _radarPacketParser =
                radarPacketParser
                ?? throw new ArgumentNullException(
                    nameof(radarPacketParser));

            _radarPacketBuilder =
                radarPacketBuilder
                ?? throw new ArgumentNullException(
                    nameof(radarPacketBuilder));

            _radarStateProvider =
                radarStateProvider
                ?? throw new ArgumentNullException(
                    nameof(radarStateProvider));

            _radarTrackingControlService =
                radarTrackingControlService
                ?? throw new ArgumentNullException(
                    nameof(radarTrackingControlService));
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Radar] Packet 처리
        /// 
        /// 수신 byte 배열을 공통 Packet으로 파싱한 뒤,
        /// Header의 Command 값에 따라 기능별 Handler로 분기한다.
        /// </summary>
        /// <param name="packetBytes">
        /// 수신 Packet byte 배열
        /// </param>
        /// <returns>
        /// 응답 Packet byte 배열
        /// </returns>
        public byte[] Handle(
            byte[] packetBytes)
        {
            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[RADAR][CMD] Packet Handle Start");

            ConsoleLogHelper.PrintLine();

            RadarPacket radarPacket =
                _radarPacketParser
                    .ParsePacket(
                        packetBytes);

            if (radarPacket == null)
            {
                Console.WriteLine(
                    "[RADAR][CMD] Failed : Packet Parse Failed");

                ConsoleLogHelper.PrintLine();

                return null;
            }

            PrintPacketHeaderLog(
                radarPacket);

            byte[] responsePacket;

            switch (radarPacket.Header.Command)
            {
                case RadarPacketConstants.COMMAND_TRACKING_REQUEST:
                    responsePacket =
                        HandleTrackingRequest(
                            radarPacket);
                    break;

                case RadarPacketConstants.COMMAND_BIST_REQUEST:
                    responsePacket =
                        HandleBistRequest(
                            radarPacket);
                    break;

                default:
                    Console.WriteLine(
                        "[RADAR][CMD] Failed : Unknown Command");

                    Console.WriteLine(
                        "[RADAR][CMD] Command : "
                        + radarPacket.Header.Command);

                    responsePacket =
                        null;
                    break;
            }

            Console.WriteLine(
                "[RADAR][CMD] Packet Handle End");

            ConsoleLogHelper.PrintLine();

            return responsePacket;
        }

        #endregion

        #region [Command Handler Methods]

        /// <summary>
        /// [IF-CSR-CSE-001] 추적 요청 처리
        /// 
        /// Radar에서 전달된 표적 방위각 / 고각 / 거리 정보를 파싱하고,
        /// 추적 요청 결과 응답 Packet을 생성한다.
        /// </summary>
        /// <param name="radarPacket">
        /// Radar Packet
        /// </param>
        /// <returns>
        /// 추적 요청 응답 Packet
        /// </returns>
        private byte[] HandleTrackingRequest(
            RadarPacket radarPacket)
        {
            Console.WriteLine();
            Console.WriteLine(
                "[RADAR][CMD] Tracking Request");
            Console.WriteLine();

            RadarTrackingRequestPayload requestPayload =
                _radarPacketParser
                    .ParseTrackingRequest(
                        radarPacket.SubData);

            if (requestPayload == null)
            {
                Console.WriteLine(
                    "[RADAR][CMD] Tracking Request Failed : Payload Parse Failed");

                return null;
            }

            PrintTrackingRequestLog(
                requestPayload);

            _radarStateProvider
                .UpdateTrackingRequest(
                    requestPayload);

            // =====================================================
            // [Radar] Tracking 제어 수행
            //
            // Radar에서 전달한
            // Azimuth / Elevation 값을
            // ADS1000 Pan / Tilt 제어로 변환한다.
            // =====================================================
            _radarTrackingControlService
                .HandleTrackingRequest(
                    requestPayload);

            RadarTrackingResponsePayload responsePayload =
                CreateTrackingResponsePayload(
                    requestPayload,
                    RadarPacketConstants.RESULT_SUCCESS);

            byte[] responsePacket =
                _radarPacketBuilder
                    .BuildTrackingResponsePacket(
                        responsePayload);

            Console.WriteLine(
                "[RADAR][CMD] Tracking Response Packet Created");

            Console.WriteLine();

            return responsePacket;
        }

        /// <summary>
        /// [IF-CSR-CSE-002] BIST 요청 처리
        /// 
        /// Radar에서 전달된 BIST 요청 정보를 파싱하고,
        /// BIST 결과 응답 Packet을 생성한다.
        /// </summary>
        /// <param name="radarPacket">
        /// Radar Packet
        /// </param>
        /// <returns>
        /// BIST 응답 Packet
        /// </returns>
        private byte[] HandleBistRequest(
            RadarPacket radarPacket)
        {
            Console.WriteLine(
                "[RADAR][CMD] BIST Request");

            Console.WriteLine();

            RadarBistRequestPayload requestPayload =
                _radarPacketParser
                    .ParseBistRequest(
                        radarPacket.SubData);

            if (requestPayload == null)
            {
                Console.WriteLine(
                    "[RADAR][CMD] BIST Request Failed : Payload Parse Failed");

                return null;
            }

            PrintBistRequestLog(
                requestPayload);

            _radarStateProvider
                .UpdateBistRequest(
                    requestPayload);

            RadarBistResponsePayload responsePayload =
                CreateBistResponsePayload(
                    requestPayload,
                    RadarPacketConstants.RESULT_SUCCESS);

            byte[] responsePacket =
                _radarPacketBuilder
                    .BuildBistResponsePacket(
                        responsePayload);

            Console.WriteLine(
                "[RADAR][CMD] BIST Response Packet Created");

            Console.WriteLine();

            return responsePacket;
        }

        #endregion

        #region [Response Payload Create Methods]

        /// <summary>
        /// 추적 요청 응답 Payload 생성
        /// 
        /// 현재 단계에서는 추적 요청 수신 성공 기준으로
        /// 성공 응답을 생성한다.
        /// </summary>
        private RadarTrackingResponsePayload CreateTrackingResponsePayload(
            RadarTrackingRequestPayload requestPayload,
            byte result)
        {
            return new RadarTrackingResponsePayload
            {
                TimeStamp =
                    DateTimeOffset.Now.ToUnixTimeMilliseconds(),

                Id =
                    requestPayload.Id,

                TrackResult =
                    result,

                Azimuth =
                    requestPayload.Azimuth,

                Elevation =
                    requestPayload.Elevation,

                RecognitionInfo =
                    string.Empty,

                Reserved =
                    0
            };

        }

        /// <summary>
        /// BIST 응답 Payload 생성
        /// 
        /// 현재 단계에서는 BIST 요청 수신 성공 기준으로
        /// 기본 정상 응답을 생성한다.
        /// </summary>
        private RadarBistResponsePayload CreateBistResponsePayload(
            RadarBistRequestPayload requestPayload,
            byte result)
        {
            return new RadarBistResponsePayload
            {
                TimeStamp =
                    DateTimeOffset.Now.ToUnixTimeMilliseconds(),

                BistType =
                    requestPayload.BistType,

                RecvResult =
                    result,

                CameraType =
                    0,

                Latitude =
                    0,

                Longitude =
                    0,

                Height =
                    0,

                Azimuth =
                    0,

                Roll =
                    0,

                Pitch =
                    0,

                Yaw =
                    0,

                Reserved =
                    0
            };

        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// Packet Header 로그 출력
        /// </summary>
        private void PrintPacketHeaderLog(
            RadarPacket radarPacket)
        {
            Console.WriteLine(
                "[RADAR][HEADER] Start Frame : 0x"
                + radarPacket.Header.StartFrame.ToString("X2"));

            Console.WriteLine(
                "[RADAR][HEADER] Send Id : 0x"
                + radarPacket.Header.SendId.ToString("X2"));

            Console.WriteLine(
                "[RADAR][HEADER] Receive Id : 0x"
                + radarPacket.Header.ReceiveId.ToString("X2"));

            Console.WriteLine(
                "[RADAR][HEADER] Command : "
                + radarPacket.Header.Command);

            Console.WriteLine(
                "[RADAR][HEADER] Packet Number : "
                + radarPacket.Header.PacketNumber);

            Console.WriteLine(
                "[RADAR][HEADER] Packet Length : "
                + radarPacket.Header.PacketLength);

            Console.WriteLine();

            Console.WriteLine(
                "[RADAR][TAIL] Checksum : 0x"
                + radarPacket.Tail.Checksum.ToString("X2"));

            Console.WriteLine(
                "[RADAR][TAIL] End Frame : 0x"
                + radarPacket.Tail.EndFrame.ToString("X2"));

            Console.WriteLine();
        }

        /// <summary>
        /// 추적 요청 로그 출력
        /// </summary>
        private void PrintTrackingRequestLog(
            RadarTrackingRequestPayload payload)
        {
            Console.WriteLine(
                "[RADAR][TRACKING] TimeStamp : "
                + payload.TimeStamp);

            Console.WriteLine(
                "[RADAR][TRACKING] PtMove : "
                + payload.PtMove);

            Console.WriteLine(
                "[RADAR][TRACKING] Id : "
                + payload.Id);

            Console.WriteLine(
                "[RADAR][TRACKING] Azimuth : "
                + payload.Azimuth);

            Console.WriteLine(
                "[RADAR][TRACKING] Elevation : "
                + payload.Elevation);

            Console.WriteLine(
                "[RADAR][TRACKING] Distance : "
                + payload.Distance);

            Console.WriteLine(
                "[RADAR][TRACKING] Vx : "
                + payload.Vx);

            Console.WriteLine(
                "[RADAR][TRACKING] Vy : "
                + payload.Vy);

            Console.WriteLine(
                "[RADAR][TRACKING] Vz : "
                + payload.Vz);

            Console.WriteLine(
                "[RADAR][TRACKING] EcefX : "
                + payload.EcefX);

            Console.WriteLine(
                "[RADAR][TRACKING] EcefY : "
                + payload.EcefY);

            Console.WriteLine(
                "[RADAR][TRACKING] EcefZ : "
                + payload.EcefZ);

            Console.WriteLine(
                "[RADAR][TRACKING] Reserved : "
                + payload.Reserved);

            Console.WriteLine();
        }

        /// <summary>
        /// BIST 요청 로그 출력
        /// </summary>
        private void PrintBistRequestLog(
            RadarBistRequestPayload payload)
        {
            Console.WriteLine(
                "[RADAR][BIST] TimeStamp : "
                + payload.TimeStamp);

            Console.WriteLine(
                "[RADAR][BIST] BistType : "
                + payload.BistType);

            Console.WriteLine(
                "[RADAR][BIST] ComportNumber : "
                + payload.ComportNumber);

            Console.WriteLine(
                "[RADAR][BIST] CbistInterval : "
                + payload.CbistInterval);

            Console.WriteLine();
        }
        #endregion
    }

}
