using System;
using VertiportNexus.Common;
using VertiportNexus.Models.Camera;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.Camera;
using VertiportNexus.Services.Command;

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
        /// [CSE] 상태 응답 시간 문자열 형식
        /// </summary>
        private const string STATE_TIMESTAMP_FORMAT =
            "yyyy-MM-ddTHH:mm:ss";

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

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CseCommandHandler] 생성자
        /// 
        /// [CSE] 명령 처리에 필요한
        /// 카메라 제어 서비스, 응답 송신 서비스,
        /// 카메라 상태 저장 서비스, 탐지 상태 저장 서비스를 주입받는다.
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
        public CseCommandHandler(
            CameraCommandService cameraCommandService,
            CseCommandResponseService responseService,
            CameraStateProvider cameraStateProvider,
            DetectionStateProvider detectionStateProvider,
            TrackingControlService trackingControlService)
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
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [CSE] 명령 처리
        /// </summary>
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

            PrintCommandEndLog();
        }

        #endregion

        #region [Command Route Methods]

        /// <summary>
        /// [CSE] 명령 [InterfaceId] 기준 처리
        /// 
        /// ICD 문서의 [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-012]
        /// 인터페이스 식별자를 기준으로 명령을 분기한다.
        /// </summary>
        private void HandleCommandByInterfaceId(
            CseCommandMessage message)
        {
            switch (message.InterfaceId)
            {
                case CseInterfaceId.DetectEnable:
                    HandleDetectEnable(message);
                    break;

                case CseInterfaceId.DetectDisable:
                    HandleDetectDisable(message);
                    break;

                case CseInterfaceId.DetectOn:
                    HandleDetectOn(message);
                    break;

                case CseInterfaceId.DetectOff:
                    HandleDetectOff(message);
                    break;

                case CseInterfaceId.DetectContinue:
                    HandleDetectContinue(message);
                    break;

                case CseInterfaceId.PtzMove:
                    HandlePtzMove(message);
                    break;

                case CseInterfaceId.PtzStop:
                    HandlePtzStop(message);
                    break;

                case CseInterfaceId.PtzMode:
                    HandlePtzMode(message);
                    break;

                case CseInterfaceId.SetImage:
                    HandleSetImage(message);
                    break;

                case CseInterfaceId.SetFlip:
                    HandleSetFlip(message);
                    break;

                case CseInterfaceId.GetConfig:
                    HandleGetConfig(message);
                    break;

                case CseInterfaceId.GetPtzState:
                    HandleGetState(message);
                    break;

                default:
                    SendUnsupportedCommandError(
                        message,
                        "UNSUPPORTED_INTERFACE_ID",
                        "Unsupported InterfaceId : " + message.InterfaceId);
                    break;
            }

        }

        /// <summary>
        /// [CSE] 명령 [MsgType] 기준 처리
        /// 
        /// 기존 개발용 [Mock] JSON 테스트와
        /// 임시 메시지 구조를 유지하기 위한 보조 분기이다.
        /// </summary>
        private void HandleCommandByMsgType(
            CseCommandMessage message)
        {
            switch (message.MsgType)
            {
                case CseCommandType.DetectEnable:
                    HandleDetectEnable(message);
                    break;

                case CseCommandType.DetectDisable:
                    HandleDetectDisable(message);
                    break;

                case CseCommandType.DetectOn:
                    HandleDetectOn(message);
                    break;

                case CseCommandType.DetectOff:
                    HandleDetectOff(message);
                    break;

                case CseCommandType.DetectContinue:
                    HandleDetectContinue(message);
                    break;

                case CseCommandType.PtzMove:
                    HandlePtzMove(message);
                    break;

                case CseCommandType.PtzStop:
                    HandlePtzStop(message);
                    break;

                case CseCommandType.PtzMode:
                    HandlePtzMode(message);
                    break;

                case CseCommandType.SetImage:
                    HandleSetImage(message);
                    break;

                case CseCommandType.SetFlip:
                    HandleSetFlip(message);
                    break;

                case CseCommandType.GetConfig:
                    HandleGetConfig(message);
                    break;

                case CseCommandType.GetState:
                    HandleGetState(message);
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
        /// [탐지 활성화] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-001] 요청을 처리한다.
        /// 
        /// 탐지 기능을 활성화하고,
        /// 요청 [Payload]에 포함된 항적 ID와 위치 정보를 로그로 출력한다.
        /// 
        /// 실제 AI Detector 제어 연동 전 단계에서는
        /// 내부 탐지 상태값만 갱신하고 정상 수신 응답을 반환한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleDetectEnable(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Detect Enable");

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] TrackId : " + message.Payload.TrackId);
                Console.WriteLine("[CSE][CMD] Latitude : " + message.Payload.Latitude);
                Console.WriteLine("[CSE][CMD] Longitude : " + message.Payload.Longitude);
                Console.WriteLine("[CSE][CMD] Altitude : " + message.Payload.Altitude);
            }

            _detectionStateProvider
                .UpdateDetectEnabled(
                    true);

            _detectionStateProvider
                .UpdateTrackId(
                    message.Payload?.TrackId?.ToString());

            SendAcceptedResponse(
                message,
                "Detect Enable");
        }

        /// <summary>
        /// [탐지 활성화 취소] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-002] 요청을 처리한다.
        /// 
        /// 탐지 기능을 비활성화하고,
        /// 진행 중인 탐지 상태와 마지막 탐지 객체 정보를 초기화한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleDetectDisable(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Detect Disable");

            _detectionStateProvider
                .UpdateDetectEnabled(
                    false);

            _trackingControlService
                .StopTracking();

            SendAcceptedResponse(
                message,
                "Detect Disable");
        }

        /// <summary>
        /// [탐지] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-003] 요청을 처리한다.
        /// 
        /// 탐지 동작 상태를 활성화하고,
        /// 요청 [Payload]에 포함된 탐지 객체 [Bounding Box] 정보를
        /// 마지막 탐지 객체 상태값으로 저장한다.
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

            _detectionStateProvider
                .UpdateDetectActive(
                    true);

            _detectionStateProvider
                .UpdateBoundingBox(
                    CreateBoundingBox(
                        message.Payload));

            SendAcceptedResponse(
                message,
                "Detect On");
        }

        /// <summary>
        /// [탐지 해제] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-004] 요청을 처리한다.
        /// 
        /// 탐지 동작 상태를 비활성화하고,
        /// 탐지 계속 수행 상태를 중지 상태로 갱신한다.
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

            _detectionStateProvider
                .UpdateDetectActive(
                    false);

            _trackingControlService
                .StopTracking();

            SendAcceptedResponse(
                message,
                "Detect Off");
        }

        /// <summary>
        /// [탐지 계속] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-005] 요청을 처리한다.
        /// 
        /// 탐지 계속 수행 상태를 활성화하고,
        /// 요청 [Payload]에 포함된 최신 탐지 객체 [Bounding Box] 정보를
        /// 마지막 탐지 객체 상태값으로 갱신한다.
        /// 
        /// [PTZ 제어 모드]가 [AUTO]인 경우에는
        /// 해당 [Bounding Box] 기준으로 자동 추적 제어를 수행한다.
        /// 
        /// [MANUAL] 상태에서는 운용자 수동 제어를 우선하므로
        /// 탐지 정보는 상태값으로만 저장하고 자동 추적 제어는 수행하지 않는다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleDetectContinue(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Detect Continue");

            PrintDetectBoxPayload(
                message.Payload);

            DetectionBoundingBox boundingBox =
                CreateBoundingBox(
                    message.Payload);

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[DETECTION][STATE] Bounding Box Update Start");

            _detectionStateProvider
                .UpdateDetectContinue(
                    true);

            _detectionStateProvider
                .UpdateBoundingBox(
                    boundingBox);

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[TRACKING][AUTO] Tracking Check");

            // [AUTO Tracking] 추적 계산
            //
            // [PTZ 제어 모드]가 [AUTO]인 경우에만
            // 탐지 객체 [Bounding Box] 기준으로 자동 추적 계산을 수행한다.
            //
            // 모드 문자열 비교는 대소문자를 구분하지 않는다.
            //
            // [MANUAL] 상태에서는 운용자 수동 제어를 우선하므로
            // 자동 추적 계산은 수행하지 않는다.
            if (string.Equals(
                _cameraStateProvider.PtzControlMode,
                "AUTO",
                StringComparison.OrdinalIgnoreCase))
            {
                _trackingControlService.ProcessTracking(
                    boundingBox,
                    _cameraStateProvider.CurrentZoom ?? 0);
            }
            else
            {
                Console.WriteLine(
                    "[TRACKING][AUTO] Skip : PTZ Mode is "
                    + _cameraStateProvider.PtzControlMode);
            }

            SendAcceptedResponse(
                message,
                "Detect Continue");
        }

        #endregion

        #region [PTZ Command Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-006] 요청을 처리한다.
        /// </summary>
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

            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Mode : " + payload.Mode);
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

            _cameraCommandService.HandleCommand(
                cameraCommand);

            SendAcceptedResponse(
                message,
                "PTZ Move");
        }

        /// <summary>
        /// [PTZ] 정지 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-007] 요청을 처리한다.
        /// </summary>
        private void HandlePtzStop(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "PTZ Stop");

            CameraCommand cameraCommand =
                new CameraCommand
                {
                    CommandType =
                        CameraCommandType.PtzStop,

                    SourceMsgId =
                        message.MsgId
                };

            _cameraCommandService.HandleCommand(
                cameraCommand);

            SendAcceptedResponse(
                message,
                "PTZ Stop");
        }

        /// <summary>
        /// [PTZ 제어 모드] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-008] 요청을 처리한다.
        /// </summary>
        private void HandlePtzMode(
            CseCommandMessage message)
        {
            CseCommandPayload payload =
                message.Payload;

            if (payload == null)
            {
                SendCommandError(
                    message,
                    "INVALID_PAYLOAD",
                    "PTZ Mode Failed : Payload is null");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                payload.Mode))
            {
                SendCommandError(
                    message,
                    "INVALID_MODE",
                    "PTZ Mode Failed : Mode is empty");

                return;
            }

            string mode =
                payload.Mode.Trim().ToUpper();

            if (mode != "AUTO" &&
                mode != "MANUAL")
            {
                SendCommandError(
                    message,
                    "INVALID_MODE",
                    "PTZ Mode Failed : Unsupported Mode : " + payload.Mode);

                return;
            }

            PrintCommandLog(
                message,
                "PTZ Mode");

            Console.WriteLine("[CSE][CMD] Mode : " + mode);

            _cameraStateProvider.UpdatePtzControlMode(mode);

            if (mode == "MANUAL")
            {
                _trackingControlService
                    .StopTracking();
            }

            _responseService.SendCommandResponse(
                message,
                "PTZ Mode Command Accepted : " + mode);
        }

        #endregion

        #region [Image Command Methods]

        /// <summary>
        /// [영상 설정] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-009] 요청을 처리한다.
        /// </summary>
        private void HandleSetImage(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Set Image");

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] Brightness : " + message.Payload.Brightness);
                Console.WriteLine("[CSE][CMD] Contrast : " + message.Payload.Contrast);
                Console.WriteLine("[CSE][CMD] FocusMode : " + message.Payload.FocusMode);
            }

            SendAcceptedResponse(
                message,
                "Set Image");
        }

        /// <summary>
        /// [영상 플립] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-010] 요청을 처리한다.
        /// </summary>
        private void HandleSetFlip(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Set Flip");

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] Flip : " + message.Payload.Flip);
            }

            SendAcceptedResponse(
                message,
                "Set Flip");
        }

        #endregion

        #region [Status Command Methods]

        /// <summary>
        /// [카메라 상태 - 설정 조회] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-011] 요청을 처리한다.
        /// </summary>
        private void HandleGetConfig(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Get Config");

            CseCommandResponsePayload payload =
                new CseCommandResponsePayload
                {
                    Brightness =
                        null,

                    Contrast =
                        null,

                    FocusMode =
                        "AUTO",

                    Flip =
                        false,

                    DetectEnabled =
                        false
                };

            _responseService.SendStatusResponse(
                message,
                payload);
        }

        /// <summary>
        /// [카메라 상태 - PTZ 조회] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-012] 요청을 처리한다.
        /// </summary>
        private void HandleGetState(
            CseCommandMessage message)
        {
            PrintCommandLog(
                message,
                "Get PTZ State");

            CseCommandResponsePayload payload =
                new CseCommandResponsePayload
                {
                    Connected =
                        _cameraStateProvider.IsConnected,

                    Ptz =
                        new CsePtzStatePayload
                        {
                            Pan =
                                _cameraStateProvider.CurrentPan,

                            Tilt =
                                _cameraStateProvider.CurrentTilt,

                            Zoom =
                                _cameraStateProvider.CurrentZoom
                        },

                    IsFlipped =
                        false,

                    UpdatedTime =
                        _cameraStateProvider.LastUpdatedTime?.ToString(
                            STATE_TIMESTAMP_FORMAT)
                };

            _responseService.SendStatusResponse(
                message,
                payload);
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
        private void SendAcceptedResponse(
            CseCommandMessage message,
            string commandName)
        {
            _responseService.SendCommandResponse(
                message,
                commandName + " Command Accepted");
        }

        /// <summary>
        /// [Command] 처리 실패 응답 송신
        /// </summary>
        private void SendCommandError(
            CseCommandMessage message,
            string errorCode,
            string errorMessage)
        {
            Console.WriteLine("[CSE][CMD] " + errorMessage);

            _responseService.SendCommandErrorResponse(
                message,
                errorCode,
                errorMessage);
        }

        /// <summary>
        /// 지원하지 않는 [Command] 오류 응답 송신
        /// </summary>
        private void SendUnsupportedCommandError(
            CseCommandMessage message,
            string errorCode,
            string errorMessage)
        {
            Console.WriteLine("[CSE][CMD] " + errorMessage);

            _responseService.SendCommandErrorResponse(
                message,
                errorCode,
                errorMessage);
        }
        #endregion
    }

}
