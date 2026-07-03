using System;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.Models.Camera;
using VertiportNexus.Models.CSE;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.Camera;
using VertiportNexus.Services.Command;
using VertiportNexus.Services.Radar;

namespace VertiportNexus.Services.Vertiport
{
    /// <summary>
    /// [CSE] 명령 처리 서비스
    /// 
    /// [MQ] / [JSON]으로 수신된 [CSE] 명령을 해석하고,
    /// [interface_id] 또는 [msg_type] 기준으로 실제 처리 로직을 분기한다.
    /// </summary>
    internal class CseCommandHandler
    {
        #region [Constants]

        /// <summary>
        /// [PTZ] 자동 제어 모드
        /// </summary>
        private const string PTZ_MODE_AUTO =
            "auto";

        /// <summary>
        /// [PTZ] 수동 제어 모드
        /// </summary>
        private const string PTZ_MODE_MANUAL =
            "manual";

        /// <summary>
        /// [카메라 상태] 기본 송신 주기 [Hz]
        /// </summary>
        private const int DEFAULT_STATUS_PUBLISH_FREQUENCY =
            10;

        /// <summary>
        /// [카메라 상태] 최소 송신 주기 [Hz]
        /// </summary>
        private const int MIN_STATUS_PUBLISH_FREQUENCY =
            1;

        /// <summary>
        /// [카메라 상태] 최대 송신 주기 [Hz]
        /// </summary>
        private const int MAX_STATUS_PUBLISH_FREQUENCY =
            30;

        #endregion

        #region [Fields]

        /// <summary>
        /// [Camera] 명령 처리 서비스
        /// </summary>
        private readonly CameraCommandService _cameraCommandService;

        /// <summary>
        /// [CSE] 명령 응답 송신 서비스
        /// </summary>
        private readonly CseCommandResponseService _responseService;

        /// <summary>
        /// [Camera] 상태 저장 서비스
        /// </summary>
        private readonly CameraStateProvider _cameraStateProvider;

        /// <summary>
        /// [Detection] 상태 저장 서비스
        /// </summary>
        private readonly DetectionStateProvider _detectionStateProvider;

        /// <summary>
        /// [Tracking] 자동 추적 제어 서비스
        /// </summary>
        private readonly TrackingControlService _trackingControlService;

        /// <summary>
        /// [Radar] 상태 저장 서비스
        /// </summary>
        private readonly RadarStateProvider _radarStateProvider;

        /// <summary>
        /// [카메라 상태] 주기 송신 취소 객체
        /// </summary>
        private CancellationTokenSource _statusPublishCancellationTokenSource;

        /// <summary>
        /// [카메라 상태] 주기 송신 작업
        /// </summary>
        private Task _statusPublishTask;

        /// <summary>
        /// [카메라 상태] 주기 송신 동시 접근 제어 객체
        /// </summary>
        private readonly object _statusPublishLock =
            new object();

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CseCommandHandler] 생성자
        /// 
        /// [CSE] 명령 처리에 필요한
        /// 카메라 제어 서비스, 응답 송신 서비스,
        /// 카메라 상태 저장 서비스, 탐지 상태 저장 서비스,
        /// 추적 제어 서비스, Radar 상태 저장 서비스를 주입받는다.
        /// </summary>
        /// <param name="cameraCommandService">
        /// [Camera] 명령 처리 서비스
        /// </param>
        /// <param name="responseService">
        /// [CSE] 명령 응답 송신 서비스
        /// </param>
        /// <param name="cameraStateProvider">
        /// [Camera] 상태 저장 서비스
        /// </param>
        /// <param name="detectionStateProvider">
        /// [Detection] 상태 저장 서비스
        /// </param>
        /// <param name="trackingControlService">
        /// [Tracking] 자동 추적 제어 서비스
        /// </param>
        /// <param name="radarStateProvider">
        /// [Radar] 상태 저장 서비스
        /// </param>
        public CseCommandHandler(
            CameraCommandService cameraCommandService,
            CseCommandResponseService responseService,
            CameraStateProvider cameraStateProvider,
            DetectionStateProvider detectionStateProvider,
            TrackingControlService trackingControlService,
            RadarStateProvider radarStateProvider)
        {
            _cameraCommandService =
                cameraCommandService
                ?? throw new ArgumentNullException(
                    nameof(cameraCommandService));

            _responseService =
                responseService
                ?? throw new ArgumentNullException(
                    nameof(responseService));

            _cameraStateProvider =
                cameraStateProvider
                ?? throw new ArgumentNullException(
                    nameof(cameraStateProvider));

            _detectionStateProvider =
                detectionStateProvider
                ?? throw new ArgumentNullException(
                    nameof(detectionStateProvider));

            _trackingControlService =
                trackingControlService
                ?? throw new ArgumentNullException(
                    nameof(trackingControlService));

            _radarStateProvider =
                radarStateProvider
                ?? throw new ArgumentNullException(
                    nameof(radarStateProvider));
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [CSE] 명령 처리
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        public void HandleCommand(
            CseCommandMessage message)
        {
            if (message == null)
            {
                Console.WriteLine("[CSE][CMD] Handle Failed : Message is null");
                return;
            }

            PrintCommandStartLog(
                message);

            try
            {
                if (!string.IsNullOrWhiteSpace(
                    message.InterfaceId))
                {
                    HandleCommandByInterfaceId(
                        message);
                }
                else
                {
                    HandleCommandByMsgType(
                        message);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[CSE][CMD] Handle Exception");
                Console.WriteLine("[CSE][CMD] Error : " + ex.Message);
                ConsoleLogHelper.PrintLine();

                SendCommandError(
                    message,
                    "INTERNAL_ERROR",
                    "Command Handle Failed : " + ex.Message);
            }
            finally
            {
                PrintCommandEndLog();
            }

        }

        /// <summary>
        /// [카메라 상태] 주기 송신 중지
        /// 
        /// RabbitMQ 수신 중지 또는 장비 연결 해제 시,
        /// 실행 중인 [q.status.res] 주기 송신 Loop를 정리한다.
        /// </summary>
        public void StopCameraStatusPublishService()
        {
            StopCameraStatusPublish();
        }

        #endregion

        #region [Command Route Methods]

        /// <summary>
        /// [CSE] 명령 [InterfaceId] 기준 처리
        /// 
        /// 최종 ICD 기준 [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-005]
        /// 인터페이스 식별자를 기준으로 명령을 분기한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleCommandByInterfaceId(
            CseCommandMessage message)
        {
            switch (message.InterfaceId)
            {
                case CseInterfaceId.DetectOn:
                    HandleDetectOn(
                        message);
                    break;

                case CseInterfaceId.DetectOff:
                    HandleDetectOff(
                        message);
                    break;

                case CseInterfaceId.DetectConf:
                    HandleDetectConf(
                        message);
                    break;

                case CseInterfaceId.PtzMove:
                    HandlePtzMove(
                        message);
                    break;

                case CseInterfaceId.GetState:
                    HandleGetState(
                        message);
                    break;

                default:
                    HandleCommandByMsgType(
                        message);
                    break;
            }

        }

        /// <summary>
        /// [CSE] 명령 [MsgType] 기준 처리
        /// 
        /// [interface_id]가 누락되었거나 일치하지 않는 경우에도
        /// 최종 ICD의 [msg_type] 기준으로 가능한 명령을 처리한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleCommandByMsgType(
            CseCommandMessage message)
        {
            switch (message.MsgType)
            {
                case CseCommandType.DetectOn:
                    HandleDetectOn(
                        message);
                    break;

                case CseCommandType.DetectOff:
                    HandleDetectOff(
                        message);
                    break;

                case CseCommandType.DetectConf:
                    HandleDetectConf(
                        message);
                    break;

                case CseCommandType.PtzMove:
                    HandlePtzMove(
                        message);
                    break;

                case CseCommandType.GetState:
                    HandleGetState(
                        message);
                    break;

                default:
                    SendUnsupportedCommandError(
                        message,
                        "UNSUPPORTED_MSG_TYPE",
                        "Unsupported MsgType : " + message.MsgType);
                    break;
            }

        }

        #endregion

        #region [Detect Command Methods]

        /// <summary>
        /// [탐지 요청] 처리
        /// 
        /// 최종 ICD [IF-GUIS-CSE-001] 기준
        /// 탐지 시작 요청과 최초 객체 화면 좌표를 처리한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleDetectOn(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Detect On");

            PrintDetectBoxPayload(
                message.Payload);

            DetectionBoundingBox boundingBox =
                CreateBoundingBox(
                    message.Payload);

            // [탐지 시작] 상태 갱신
            //
            // [detect_on]은 탐지 시작 요청이므로
            // 탐지 상태를 [LOCK ON]으로 전환하고,
            // 함께 전달된 최초 객체 화면 좌표를 마지막 탐지 객체 정보로 저장한다.
            _detectionStateProvider
                .StartDetection();

            _detectionStateProvider
                .UpdateBoundingBox(
                    boundingBox);

            SendAcceptedResponse(
                message,
                "Detect On");
        }

        /// <summary>
        /// [탐지 해제] 처리
        /// 
        /// 최종 ICD [IF-GUIS-CSE-002] 기준
        /// 탐지 / 추적 상태를 해제한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleDetectOff(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Detect Off");

            // [탐지 해제] 상태 갱신
            //
            // [detect_off]는 현재 탐지 / 추적 상태를 종료하는 요청이므로
            // 탐지 상태를 [LOCK OFF]로 전환하고,
            // 마지막 객체 화면 좌표를 초기화한다.
            _detectionStateProvider
                .StopDetection();

            // [Tracking] 자동 추적 중지
            //
            // GUI Detect Off 수신 시,
            // GUI BBOX 기반 자동 추적 제어를 중지한다.
            _trackingControlService
                .StopTracking();

            // [Radar Tracking] 상태 해제
            //
            // GUI Detect Off 수신 시,
            // 탐지 / 추적 상태가 종료되므로
            // Radar 우선 제어 상태도 함께 해제한다.
            _radarStateProvider
                .StopRadarTracking();

            SendAcceptedResponse(
                message,
                "Detect Off");
        }

        /// <summary>
        /// [탐지 결과 연속 갱신] 처리
        /// 
        /// 최종 ICD [IF-GUIS-CSE-003] 기준
        /// 탐지 중 약 [30Hz]로 수신되는 객체 화면 좌표를 처리한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleDetectConf(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Detect Confirm");

            PrintDetectBoxPayload(
                message.Payload);

            if (!_detectionStateProvider.IsDetectEnabled)
            {
                Console.WriteLine(
                    "[CSE][DETECT] Detect Conf Ignored : LOCK OFF");

                return;
            }

            DetectionBoundingBox boundingBox =
                CreateBoundingBox(
                    message.Payload);

            // [탐지 결과 연속 갱신]
            //
            // [detect_conf]는 탐지 중 약 [30Hz]로 수신되므로
            // Queue에 누적하지 않고 마지막 객체 화면 좌표만 덮어쓴다.
            _detectionStateProvider
                .UpdateBoundingBox(
                    boundingBox);

            // [Radar 우선 제어]
            //
            // Radar Tracking이 활성화된 상태에서는
            // GUI BBOX 기반 Pan / Tilt 제어를 수행하지 않는다.
            //
            // 이 경우 GUI BBOX는 최신값 저장만 수행하고,
            // 실제 PT 제어는 Radar에서 수신한 Azimuth / Elevation 기준으로 처리한다.
            if (_radarStateProvider.IsRadarTrackingActive)
            {
                Console.WriteLine(
                    "[TRACKING][BBOX] Skip : Radar Tracking Active");

                SendAcceptedResponse(
                    message,
                    "Detect Confirm");

                return;
            }

            // [AUTO Tracking] 추적 계산
            //
            // PTZ 제어 모드가 [AUTO]인 경우에만
            // 탐지 객체 Bounding Box 기준으로 자동 추적 계산을 수행한다.
            //
            // [MANUAL] 상태에서는 운용자 수동 제어를 우선하므로
            // 탐지 정보는 저장만 하고 자동 추적 제어는 수행하지 않는다.
            if (string.Equals(
                _cameraStateProvider.PtzControlMode,
                PTZ_MODE_AUTO,
                StringComparison.OrdinalIgnoreCase))
            {
                _trackingControlService
                    .ProcessTracking(
                        boundingBox,
                        _cameraStateProvider.CurrentZoomRatio ?? 1.0);
            }
            else
            {
                Console.WriteLine(
                    "[TRACKING][AUTO] Skip : PTZ Mode is "
                    + _cameraStateProvider.PtzControlMode);
            }

            SendAcceptedResponse(
                message,
                "Detect Confirm");
        }

        #endregion

        #region [PTZ Command Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// 최종 ICD [IF-GUIS-CSE-004] 기준
        /// [Pan] / [Tilt] / [Zoom] 수동 제어 명령을 처리한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandlePtzMove(
            CseCommandMessage message)
        {
            CseCommandPayload payload =
                message.Payload;

            if (payload == null)
            {
                SendCommandError(
                    message,
                    "INVALID_PAYLOAD",
                    "PTZ Move Failed : Payload is null");

                return;
            }

            // [PTZ 제어 모드] 처리
            //
            // 최종 ICD에서는 [mode] 값 기준으로
            // [manual] 모드 전환 또는 PTZ 제어 명령을 처리한다.
            if (TryHandlePtzControlMode(
                message,
                payload))
            {
                return;
            }

            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Mode : " + payload.Mode);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Command : " + payload.Command);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Pan : " + payload.Pan);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Tilt : " + payload.Tilt);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Zoom : " + payload.Zoom);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] ZoomPosition : " + payload.ZoomPosition);

            CameraCommand cameraCommand =
                new CameraCommand
                {
                    CommandType =
                        CameraCommandType.PtzMove,

                    Mode =
                        payload.Mode,

                    Command =
                        payload.Command,

                    Pan =
                        payload.Pan,

                    Tilt =
                        payload.Tilt,

                    Zoom =
                        payload.Zoom,

                    ZoomPosition =
                        payload.ZoomPosition,

                    SourceMsgId =
                        message.MsgId
                };

            _cameraCommandService
                .HandleCommand(
                    cameraCommand);

            SendAcceptedResponse(
                message,
                "PTZ Move");
        }

        /// <summary>
        /// [PTZ 제어 모드] 통합 명령 처리
        /// 
        /// [ptz_move] 명령의 [mode] 값이
        /// [manual] 또는 [auto]인 경우 내부 [PTZ 제어 모드]를 갱신한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        /// <param name="payload">
        /// [CSE] 명령 Payload
        /// </param>
        /// <returns>
        /// [PTZ 제어 모드] 처리 여부
        /// </returns>
        private bool TryHandlePtzControlMode(
            CseCommandMessage message,
            CseCommandPayload payload)
        {
            if (payload == null ||
                string.IsNullOrWhiteSpace(
                    payload.Mode))
            {
                return false;
            }

            string normalizedMode =
                payload.Mode
                    .Trim()
                    .ToLower();

            if (normalizedMode != PTZ_MODE_AUTO &&
                normalizedMode != PTZ_MODE_MANUAL)
            {
                return false;
            }

            string ptzControlMode =
                normalizedMode.ToUpper();

            PrintCommandLog(
                message,
                "PTZ Control Mode");

            Console.WriteLine("[CSE][CMD][PTZ_MODE] Mode : " + ptzControlMode);

            _cameraStateProvider
                .UpdatePtzControlMode(
                    ptzControlMode);

            // [MANUAL] 전환 시 자동 추적 정지
            //
            // 수동 운용으로 전환된 경우,
            // 이전 [AUTO] 추적에서 수행 중이던
            // Pan / Tilt 이동을 정지한다.
            if (ptzControlMode == "MANUAL")
            {
                _trackingControlService
                    .StopTracking();
            }

            SendAcceptedResponse(
                message,
                "PTZ Control Mode");

            return true;
        }

        #endregion

        #region [Status Command Methods]

        /// <summary>
        /// [카메라 상태 조회] 요청 처리
        /// 
        /// 최종 ICD [IF-GUIS-CSE-005] 기준
        /// [q.status.req] Queue로 수신된 상태 송신 주기 요청을 처리한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleGetState(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Get Camera State");

            if (message.Payload == null)
            {
                SendCommandError(
                    message,
                    "INVALID_PAYLOAD",
                    "Get Camera State Failed : Payload is null");

                return;
            }

            int frequency = message.Payload.Frequency ?? 10;

            Console.WriteLine("[CSE][STATUS] Request Frequency : " + frequency);

            // [카메라 상태 송신 주기 처리]
            //
            // [frequency] 값이 [0]이면 상태 송신을 중지하고,
            // 그 외 값이면 요청된 주기 기준으로 상태 송신을 시작한다.
            //
            // 실제 [q.status.res] 주기 송신 Loop는
            // StartCameraStatusPublish()에서 처리한다.
            if (frequency == 0)
            {
                StopCameraStatusPublish();
            }
            else
            {
                StartCameraStatusPublish(
                    frequency);
            }

        }

        /// <summary>
        /// [카메라 상태] 주기 송신 시작
        /// 
        /// 요청된 주기에 따라 [q.status.res] Queue로
        /// 카메라 상태를 주기적으로 송신한다.
        /// </summary>
        /// <param name="frequency">
        /// 상태 송신 주기 [Hz]
        /// </param>
        private void StartCameraStatusPublish(
            int frequency)
        {
            int publishFrequency =
                ClampStatusPublishFrequency(
                    frequency);

            int publishIntervalMs =
                1000 / publishFrequency;

            lock (_statusPublishLock)
            {
                StopCameraStatusPublish();

                _statusPublishCancellationTokenSource =
                    new CancellationTokenSource();

                CancellationToken cancellationToken =
                    _statusPublishCancellationTokenSource.Token;

                // [카메라 상태] 주기 송신 Task 시작
                //
                // [IF-GUIS-CSE-005] 요청으로 전달된 [frequency] 기준으로
                // 현재 카메라 상태를 [q.status.res] Queue에 반복 송신한다.
                _statusPublishTask =
                    Task.Run(
                        async () =>
                        {
                            ConsoleLogHelper.PrintLine();
                            Console.WriteLine("[CSE][STATUS] Publish Start");
                            Console.WriteLine("[CSE][STATUS] Frequency : " + publishFrequency + "Hz");
                            Console.WriteLine("[CSE][STATUS] Interval : " + publishIntervalMs + "ms");
                            ConsoleLogHelper.PrintLine();

                            while (!cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    _responseService
                                        .SendCameraStatusResponse(
                                            _cameraStateProvider);
                                }
                                catch (Exception ex)
                                {
                                    ConsoleLogHelper.PrintLine();
                                    Console.WriteLine("[CSE][STATUS] Publish Failed");
                                    Console.WriteLine("[CSE][STATUS] Error : " + ex.Message);
                                    ConsoleLogHelper.PrintLine();
                                }

                                await Task.Delay(
                                    publishIntervalMs,
                                    cancellationToken);
                            }
                        },
                        cancellationToken);
            }

        }

        /// <summary>
        /// [카메라 상태] 주기 송신 중지
        /// 
        /// 현재 실행 중인 카메라 상태 주기 송신을 중지한다.
        /// </summary>
        private void StopCameraStatusPublish()
        {
            lock (_statusPublishLock)
            {
                if (_statusPublishCancellationTokenSource == null)
                {
                    return;
                }

                try
                {
                    _statusPublishCancellationTokenSource
                        .Cancel();

                    ConsoleLogHelper.PrintLine();
                    Console.WriteLine("[CSE][STATUS] Publish Stop");
                    ConsoleLogHelper.PrintLine();
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.PrintLine();
                    Console.WriteLine("[CSE][STATUS] Publish Stop Failed");
                    Console.WriteLine("[CSE][STATUS] Error : " + ex.Message);
                    ConsoleLogHelper.PrintLine();
                }
                finally
                {
                    _statusPublishCancellationTokenSource
                        .Dispose();

                    _statusPublishCancellationTokenSource =
                        null;

                    _statusPublishTask =
                        null;
                }

            }

        }

        /// <summary>
        /// [카메라 상태] 송신 주기 보정
        /// 
        /// [IF-GUIS-CSE-005] 요청에서 수신한 [frequency] 값을
        /// 허용 범위 내 값으로 보정한다.
        /// </summary>
        /// <param name="frequency">
        /// 요청 송신 주기 [Hz]
        /// </param>
        /// <returns>
        /// 보정된 송신 주기 [Hz]
        /// </returns>
        private int ClampStatusPublishFrequency(
            int frequency)
        {
            if (frequency <= 0)
            {
                return DEFAULT_STATUS_PUBLISH_FREQUENCY;
            }

            if (frequency < MIN_STATUS_PUBLISH_FREQUENCY)
            {
                return MIN_STATUS_PUBLISH_FREQUENCY;
            }

            if (frequency > MAX_STATUS_PUBLISH_FREQUENCY)
            {
                return MAX_STATUS_PUBLISH_FREQUENCY;
            }
            return frequency;
        }

        #endregion

        #region [Detection Helper Methods]

        /// <summary>
        /// [Detection] Bounding Box 생성
        /// 
        /// [CSE] 명령 [Payload]에 포함된
        /// 탐지 객체 좌표와 탐지 정보를 내부 상태 저장용
        /// [DetectionBoundingBox] 모델로 변환한다.
        /// 
        /// [Payload]가 없는 경우에는 저장할 탐지 객체가 없으므로
        /// null을 반환한다.
        /// </summary>
        /// <param name="payload">
        /// [CSE] 명령 [Payload]
        /// </param>
        /// <returns>
        /// 탐지 객체 영역 정보
        /// </returns>
        private DetectionBoundingBox CreateBoundingBox(
            CseCommandPayload payload)
        {
            if (payload == null)
            {
                return null;
            }

            return new DetectionBoundingBox
            {
                FrameId =
                    payload.FrameId,

                X1 =
                    payload.X1,

                Y1 =
                    payload.Y1,

                X2 =
                    payload.X2,

                Y2 =
                    payload.Y2,

                ClassId =
                    payload.ClassId,

                Confidence =
                    payload.Confidence
            };

        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// [CSE] 명령 처리 시작 로그 출력
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void PrintCommandStartLog(
            CseCommandMessage message)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CSE][CMD] Command Handle Start");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);
            Console.WriteLine("[CSE][CMD] MsgType : " + message.MsgType);
            Console.WriteLine("[CSE][CMD] MsgId : " + message.MsgId);
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CSE] 명령 처리 종료 로그 출력
        /// </summary>
        private void PrintCommandEndLog()
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CSE][CMD] Command Handle End");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [CSE] 명령 기본 로그 출력
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        /// <param name="commandName">
        /// 명령 이름
        /// </param>
        private void PrintCommandLog(
            CseCommandMessage message,
            string commandName)
        {
            Console.WriteLine("[CSE][CMD] " + commandName);
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);
            Console.WriteLine("[CSE][CMD] Request MsgId : " + message.MsgId);
        }

        /// <summary>
        /// 탐지 객체 [Payload] 로그 출력
        /// </summary>
        /// <param name="payload">
        /// [CSE] 명령 Payload
        /// </param>
        private void PrintDetectBoxPayload(
            CseCommandPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            Console.WriteLine("[CSE][CMD] FrameId : " + payload.FrameId);
            Console.WriteLine("[CSE][CMD] X1 : " + payload.X1);
            Console.WriteLine("[CSE][CMD] Y1 : " + payload.Y1);
            Console.WriteLine("[CSE][CMD] X2 : " + payload.X2);
            Console.WriteLine("[CSE][CMD] Y2 : " + payload.Y2);
            Console.WriteLine("[CSE][CMD] ClassId : " + payload.ClassId);
            Console.WriteLine("[CSE][CMD] Confidence : " + payload.Confidence);
        }

        #endregion

        #region [Response Methods]

        /// <summary>
        /// [Command] 수신 성공 응답 송신
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        /// <param name="commandName">
        /// 명령 이름
        /// </param>
        private void SendAcceptedResponse(
            CseCommandMessage message,
            string commandName)
        {
            _responseService
                .SendCommandResponse(
                    message,
                    commandName + " Command Accepted");
        }

        /// <summary>
        /// [Command] 처리 실패 응답 송신
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        /// <param name="errorCode">
        /// 오류 코드
        /// </param>
        /// <param name="errorMessage">
        /// 오류 메시지
        /// </param>
        private void SendCommandError(
            CseCommandMessage message,
            string errorCode,
            string errorMessage)
        {
            Console.WriteLine("[CSE][CMD] " + errorMessage);

            _responseService
                .SendCommandErrorResponse(
                    message,
                    errorCode,
                    errorMessage);
        }

        /// <summary>
        /// 지원하지 않는 [Command] 오류 응답 송신
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        /// <param name="errorCode">
        /// 오류 코드
        /// </param>
        /// <param name="errorMessage">
        /// 오류 메시지
        /// </param>
        private void SendUnsupportedCommandError(
            CseCommandMessage message,
            string errorCode,
            string errorMessage)
        {
            Console.WriteLine("[CSE][CMD] " + errorMessage);

            _responseService
                .SendCommandErrorResponse(
                    message,
                    errorCode,
                    errorMessage);
        }
        #endregion
    }

}
