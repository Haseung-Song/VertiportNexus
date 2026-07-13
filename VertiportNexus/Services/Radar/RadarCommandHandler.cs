using Serilog;
using System;
using VertiportNexus.Common.Constants;
using VertiportNexus.Models.Radar;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] Command Handler
    /// 
    /// Radar Packet의 Command를 기준으로
    /// 추적 요청을 처리하고 응답 Packet을 생성한다.
    /// 
    /// BIST 관련 분기는 최종 ICD 확정 전까지
    /// 기존 참조 구조 유지를 위해 임시로 유지한다.
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
        /// 
        /// [Radar] Packet 처리에 필요한
        /// Packet Parser, 
        /// Packet Builder,
        /// Radar 상태 저장 서비스, 
        /// Radar 추적 제어 서비스를 주입받는다.
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
        /// <param name="radarTrackingControlService">
        /// [Radar] 추적 제어 서비스
        /// </param>
        public RadarCommandHandler(
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
            try
            {
                Log.Information(
                    "[RADAR][CMD] Packet Handle Start : Length={Length}",
                    packetBytes == null ? 0 : packetBytes.Length);

                RadarPacket radarPacket =
                    _radarPacketParser
                        .ParsePacket(
                            packetBytes);

                if (radarPacket == null)
                {
                    Log.Warning(
                        "[RADAR][CMD] Packet Parse Failed");

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
                        Log.Warning(
                            "[RADAR][CMD] Unknown Command : Command={Command}",
                            radarPacket.Header.Command);

                        responsePacket =
                            null;
                        break;
                }

                Log.Information(
                    "[RADAR][CMD] Packet Handle End : Command={Command}, ResponseLength={ResponseLength}",
                    radarPacket.Header.Command,
                    responsePacket == null ? 0 : responsePacket.Length);

                return responsePacket;
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[RADAR][CMD] Packet Handle Exception");

                return null;
            }

        }

        #endregion

        #region [Command Handler Methods]

        /// <summary>
        /// [IF-CSR-CSE-001] 추적 요청 처리
        /// 
        /// Radar에서 전달된 표적 방위각 / 고각 / 거리 정보를 파싱하고,
        /// Radar 우선 제어 상태를 활성화한 뒤,
        /// ADS1000 Pan / Tilt 제어 및 추적 요청 결과 응답 Packet을 생성한다.
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
            Log.Information(
                "[RADAR][CMD] Tracking Request");

            RadarTrackingRequestPayload requestPayload =
                _radarPacketParser
                    .ParseTrackingRequest(
                        radarPacket.SubData);

            if (requestPayload == null)
            {
                Log.Warning(
                    "[RADAR][CMD] Tracking Request Failed : Payload Parse Failed");

                return null;
            }

            PrintTrackingRequestLog(
                requestPayload);

            Log.Information(
                "[RADAR][TRACKING] Request Parsed : Id={Id}, Azimuth={Azimuth}, Elevation={Elevation}, Distance={Distance}",
                requestPayload.Id,
                requestPayload.Azimuth,
                requestPayload.Elevation,
                requestPayload.Distance);

            _radarStateProvider
                .UpdateTrackingRequest(
                    requestPayload);

            // [Radar Tracking] 활성 상태 갱신
            //
            // Radar Tracking Request가 수신되면,
            // GUI BBOX 기반 제어보다 Radar 지향 제어를 우선하기 위해
            // Radar Tracking 상태를 활성화한다.
            _radarStateProvider
                .StartRadarTracking();

            // [Radar] Tracking 제어 수행
            //
            // Radar에서 전달한 Azimuth / Elevation 값을
            // ADS1000 Pan / Tilt 제어로 변환한다.
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

            Log.Information(
                "[RADAR][CMD] Tracking Response Packet Created : Length={Length}",
                responsePacket == null ? 0 : responsePacket.Length);

            return responsePacket;
        }

        /// <summary>
        /// [IF-CSR-CSE-002] BIST 요청 처리
        /// 
        /// Radar에서 전달된 BIST 요청 정보를 파싱하고,
        /// BIST 결과 응답 Packet을 생성한다.
        /// 
        /// 현재 최종 ICD 기준 삭제 예정 항목이므로,
        /// 추후 Radar ICD 정리 단계에서 제거한다.
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
            Log.Information(
                "[RADAR][CMD] BIST Request");

            RadarBistRequestPayload requestPayload =
                _radarPacketParser
                    .ParseBistRequest(
                        radarPacket.SubData);

            if (requestPayload == null)
            {
                Log.Warning(
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

            Log.Information(
                "[RADAR][CMD] BIST Response Packet Created : Length={Length}",
                responsePacket == null ? 0 : responsePacket.Length);

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
        /// <param name="requestPayload">
        /// Radar 추적 요청 Payload
        /// </param>
        /// <param name="result">
        /// 처리 결과
        /// </param>
        /// <returns>
        /// Radar 추적 응답 Payload
        /// </returns>
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
        /// 
        /// 현재 최종 ICD 기준 삭제 예정 항목이므로,
        /// 추후 Radar ICD 정리 단계에서 제거한다.
        /// </summary>
        /// <param name="requestPayload">
        /// Radar BIST 요청 Payload
        /// </param>
        /// <param name="result">
        /// 처리 결과
        /// </param>
        /// <returns>
        /// Radar BIST 응답 Payload
        /// </returns>
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
        /// <param name="radarPacket">
        /// Radar Packet
        /// </param>
        private void PrintPacketHeaderLog(
            RadarPacket radarPacket)
        {
            Log.Debug(
                "[RADAR][HEADER] StartFrame=0x{StartFrame}, SendId=0x{SendId}, ReceiveId=0x{ReceiveId}, Command={Command}, PacketNumber={PacketNumber}, PacketLength={PacketLength}, Checksum=0x{Checksum}, EndFrame=0x{EndFrame}",
                radarPacket.Header.StartFrame.ToString("X2"),
                radarPacket.Header.SendId.ToString("X2"),
                radarPacket.Header.ReceiveId.ToString("X2"),
                radarPacket.Header.Command,
                radarPacket.Header.PacketNumber,
                radarPacket.Header.PacketLength,
                radarPacket.Tail.Checksum.ToString("X2"),
                radarPacket.Tail.EndFrame.ToString("X2"));
        }

        /// <summary>
        /// 추적 요청 로그 출력
        /// </summary>
        /// <param name="payload">
        /// Radar 추적 요청 Payload
        /// </param>
        private void PrintTrackingRequestLog(
            RadarTrackingRequestPayload payload)
        {
            Log.Debug(
                "[RADAR][TRACKING] TimeStamp={TimeStamp}, PtMove={PtMove}, Id={Id}, Azimuth={Azimuth}, Elevation={Elevation}, Distance={Distance}, Vx={Vx}, Vy={Vy}, Vz={Vz}, EcefX={EcefX}, EcefY={EcefY}, EcefZ={EcefZ}, Reserved={Reserved}",
                payload.TimeStamp,
                payload.PtMove,
                payload.Id,
                payload.Azimuth,
                payload.Elevation,
                payload.Distance,
                payload.Vx,
                payload.Vy,
                payload.Vz,
                payload.EcefX,
                payload.EcefY,
                payload.EcefZ,
                payload.Reserved);
        }

        /// <summary>
        /// BIST 요청 로그 출력
        /// 
        /// 현재 최종 ICD 기준 삭제 예정 항목이므로,
        /// 추후 Radar ICD 정리 단계에서 제거한다.
        /// </summary>
        /// <param name="payload">
        /// Radar BIST 요청 Payload
        /// </param>
        private void PrintBistRequestLog(
            RadarBistRequestPayload payload)
        {
            Log.Debug(
                "[RADAR][BIST] TimeStamp={TimeStamp}, BistType={BistType}, ComportNumber={ComportNumber}, CbistInterval={CbistInterval}",
                payload.TimeStamp,
                payload.BistType,
                payload.ComportNumber,
                payload.CbistInterval);
        }
        #endregion
    }

}
