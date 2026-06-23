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
    /// [msg_type] 기준으로 실제 처리 로직을 분기한다.
    /// 
    /// 현재 단계에서는 명령 수신 / 파싱 / 분기 확인을 우선 수행하고,
    /// 이후 [ADS1000] 카메라 제어 서비스와 연결한다.
    /// </summary>
    internal class CseCommandHandler
    {
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

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CseCommandHandler] 생성자
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
        public CseCommandHandler(
            CameraCommandService cameraCommandService,
            CseCommandResponseService responseService,
            CameraStateProvider cameraStateProvider)
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

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CSE][CMD] Command Handle Start");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);
            Console.WriteLine("[CSE][CMD] MsgType : " + message.MsgType);
            Console.WriteLine("[CSE][CMD] MsgId : " + message.MsgId);
            ConsoleLogHelper.PrintLine();

            /// <summary>
            /// [ICD] 인터페이스 ID 우선 처리
            /// 
            /// 실제 연동 메시지는 [interface_id] 기준으로 분기하고,
            /// 기존 개발용 [Mock] 메시지는 [msg_type] 기준으로 보조 처리한다.
            /// </summary>
            if (!string.IsNullOrWhiteSpace(message.InterfaceId))
            {
                HandleCommandByInterfaceId(
                    message);
            }
            else
            {
                HandleCommandByMsgType(
                    message);
            }
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CSE][CMD] Command Handle End");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Command Route Methods]

        /// <summary>
        /// [CSE] 명령 [InterfaceId] 기준 처리
        /// 
        /// ICD 문서의 [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-012]
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
                case CseInterfaceId.DetectEnable:
                    HandleDetectEnable(
                        message);
                    break;

                case CseInterfaceId.DetectDisable:
                    HandleDetectDisable(
                        message);
                    break;

                case CseInterfaceId.DetectOn:
                    HandleDetectOn(
                        message);
                    break;

                case CseInterfaceId.DetectOff:
                    HandleDetectOff(
                        message);
                    break;

                case CseInterfaceId.DetectContinue:
                    HandleDetectContinue(
                        message);
                    break;

                case CseInterfaceId.PtzMove:
                    HandlePtzMove(
                        message);
                    break;

                case CseInterfaceId.PtzStop:
                    HandlePtzStop(
                        message);
                    break;

                case CseInterfaceId.PtzMode:
                    HandlePtzMode(
                        message);
                    break;

                case CseInterfaceId.SetImage:
                    HandleSetImage(
                        message);
                    break;

                case CseInterfaceId.SetFlip:
                    HandleSetFlip(
                        message);
                    break;

                case CseInterfaceId.GetConfig:
                    HandleGetConfig(
                        message);
                    break;

                case CseInterfaceId.GetPtzState:
                    HandleGetState(
                        message);
                    break;

                default:
                    Console.WriteLine("[CSE][CMD] Unsupported InterfaceId : " + message.InterfaceId);
                    break;
            }

        }

        /// <summary>
        /// [CSE] 명령 [MsgType] 기준 처리
        /// 
        /// 기존 개발용 [Mock] JSON 테스트와
        /// 임시 메시지 구조를 유지하기 위한 보조 분기이다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void HandleCommandByMsgType(
            CseCommandMessage message)
        {
            switch (message.MsgType)
            {
                case CseCommandType.PtzMove:
                    HandlePtzMove(
                        message);
                    break;

                case CseCommandType.PtzStop:
                    HandlePtzStop(
                        message);
                    break;

                case CseCommandType.GetState:
                    HandleGetState(
                        message);
                    break;

                case CseCommandType.GetConfig:
                    HandleGetConfig(
                        message);
                    break;

                case CseCommandType.DetectEnable:
                    HandleDetectEnable(
                        message);
                    break;

                case CseCommandType.DetectDisable:
                    HandleDetectDisable(
                        message);
                    break;

                case CseCommandType.DetectOn:
                    HandleDetectOn(
                        message);
                    break;

                case CseCommandType.DetectOff:
                    HandleDetectOff(
                        message);
                    break;

                case CseCommandType.DetectContinue:
                    HandleDetectContinue(
                        message);
                    break;

                case CseCommandType.PtzMode:
                    HandlePtzMode(
                        message);
                    break;

                case CseCommandType.SetImage:
                    HandleSetImage(
                        message);
                    break;

                case CseCommandType.SetFlip:
                    HandleSetFlip(
                        message);
                    break;

                default:
                    Console.WriteLine("[CSE][CMD] Unsupported MsgType : " + message.MsgType);
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
        /// 현재 단계에서는 수신 / 분기 확인 후
        /// 처리 결과 응답을 송신한다.
        /// </summary>
        /// <param name="message">
        /// [탐지 활성화 요청 메시지]
        /// </param>
        private void HandleDetectEnable(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Detect Enable");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] TrackId : " + message.Payload.TrackId);
                Console.WriteLine("[CSE][CMD] Latitude : " + message.Payload.Latitude);
                Console.WriteLine("[CSE][CMD] Longitude : " + message.Payload.Longitude);
                Console.WriteLine("[CSE][CMD] Altitude : " + message.Payload.Altitude);
            }

            // [Detect Enable] 명령 응답 송신
            //
            // 실제 탐지 기능은 아직 구현되지 않았으므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "Detect Enable Command Accepted");
        }

        /// <summary>
        /// [탐지 활성화 취소] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-002] 요청을 처리한다.
        /// 
        /// 현재 단계에서는 수신 / 분기 확인 후
        /// 처리 결과 응답을 송신한다.
        /// </summary>
        /// <param name="message">
        /// [탐지 활성화 취소 요청 메시지]
        /// </param>
        private void HandleDetectDisable(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Detect Disable");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);

            // [Detect Disable] 명령 응답 송신
            //
            // 실제 탐지 기능은 아직 구현되지 않았으므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "Detect Disable Command Accepted");
        }

        /// <summary>
        /// [탐지] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-003] 요청을 처리한다.
        /// 
        /// 현재 단계에서는 요청 데이터 확인 후
        /// 처리 결과 응답을 송신한다.
        /// </summary>
        /// <param name="message">
        /// [탐지 요청 메시지]
        /// </param>
        private void HandleDetectOn(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Detect On");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] FrameId : " + message.Payload.FrameId);
                Console.WriteLine("[CSE][CMD] X1 : " + message.Payload.X1);
                Console.WriteLine("[CSE][CMD] Y1 : " + message.Payload.Y1);
                Console.WriteLine("[CSE][CMD] X2 : " + message.Payload.X2);
                Console.WriteLine("[CSE][CMD] Y2 : " + message.Payload.Y2);
                Console.WriteLine("[CSE][CMD] ClassId : " + message.Payload.ClassId);
                Console.WriteLine("[CSE][CMD] Confidence : " + message.Payload.Confidence);
            }

            // [Detect On] 명령 응답 송신
            //
            // 실제 탐지 기능은 아직 구현되지 않았으므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "Detect On Command Accepted");
        }

        /// <summary>
        /// [탐지 해제] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-004] 요청을 처리한다.
        /// 
        /// 현재 단계에서는 수신 / 분기 확인 후
        /// 처리 결과 응답을 송신한다.
        /// </summary>
        /// <param name="message">
        /// [탐지 해제 요청 메시지]
        /// </param>
        private void HandleDetectOff(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Detect Off");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);

            // [Detect Off] 명령 응답 송신
            //
            // 실제 탐지 기능은 아직 구현되지 않았으므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "Detect Off Command Accepted");
        }

        /// <summary>
        /// [탐지 계속] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-005] 요청을 처리한다.
        /// 
        /// 현재 단계에서는 요청 데이터 확인 후
        /// 처리 결과 응답을 송신한다.
        /// </summary>
        /// <param name="message">
        /// [탐지 계속 요청 메시지]
        /// </param>
        private void HandleDetectContinue(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Detect Continue");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] FrameId : " + message.Payload.FrameId);
                Console.WriteLine("[CSE][CMD] X1 : " + message.Payload.X1);
                Console.WriteLine("[CSE][CMD] Y1 : " + message.Payload.Y1);
                Console.WriteLine("[CSE][CMD] X2 : " + message.Payload.X2);
                Console.WriteLine("[CSE][CMD] Y2 : " + message.Payload.Y2);
                Console.WriteLine("[CSE][CMD] ClassId : " + message.Payload.ClassId);
                Console.WriteLine("[CSE][CMD] Confidence : " + message.Payload.Confidence);
            }

            // [Detect Continue] 명령 응답 송신
            //
            // 실제 탐지 기능은 아직 구현되지 않았으므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "Detect Continue Command Accepted");
        }

        #endregion

        #region [PTZ Command Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-006] 요청을 처리한다.
        /// 
        /// [payload]의 [mode] / [pan] / [tilt] / [zoom] / [focus] 값을
        /// 내부 카메라 명령으로 변환하여 장비 제어 서비스로 전달한다.
        /// </summary>
        /// <param name="message">
        /// [PTZ] 이동 명령 메시지
        /// </param>
        private void HandlePtzMove(
            CseCommandMessage message)
        {
            CseCommandPayload payload =
                message.Payload;

            if (payload == null)
            {
                Console.WriteLine("[CSE][CMD] PTZ Move Failed : Payload is null");

                _responseService.SendCommandErrorResponse(
                    message,
                    "INVALID_PAYLOAD",
                    "PTZ Move Failed : Payload is null");

                return;
            }

            // [CSE] [PTZ Move] 명령 로그 출력
            //
            // ICD 기준 [IF-GUIS-CSE-006] 요청 데이터를
            // 내부 [Camera] 명령으로 변환하기 전 확인한다.
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Mode : " + message.Payload?.Mode);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Pan : " + message.Payload?.Pan);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Tilt : " + message.Payload?.Tilt);
            Console.WriteLine("[CSE][CMD][PTZ_MOVE] Zoom : " + message.Payload?.Zoom);

            // [CSE] 명령을 내부 카메라 명령으로 변환
            //
            // [CameraCommandService]는 CSE 메시지 구조를 알지 않고,
            // 내부 공통 명령 모델만 받아 ADS1000 제어로 분기한다.
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

                    SourceMsgId =
                        message.MsgId
                };

            _cameraCommandService.HandleCommand(
                cameraCommand);

            // [PTZ Move] 명령 응답 송신
            //
            // 현재 단계에서는 실제 장비 응답 확인 전이므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "PTZ Move Command Accepted");
        }

        /// <summary>
        /// [PTZ] 정지 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-007] 요청을 처리한다.
        /// 
        /// 현재 동작 중인 [Pan] / [Tilt] / [Zoom] / [Focus]
        /// 연속 제어를 정지한다.
        /// </summary>
        /// <param name="message">
        /// [PTZ] 정지 명령 메시지
        /// </param>
        private void HandlePtzStop(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] PTZ Stop");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);
            Console.WriteLine("[CSE][CMD] Request MsgId : " + message.MsgId);

            // [CSE] 정지 명령을 내부 카메라 명령으로 변환
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

            // [PTZ Stop] 명령 응답 송신
            //
            // 현재 단계에서는 실제 장비 응답 확인 전이므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "PTZ Stop Command Accepted");
        }

        #endregion

        #region [Image Command Methods]

        /// <summary>
        /// [PTZ 제어 모드] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-008] 요청을 처리한다.
        /// 
        /// [payload.mode] 값으로 전달된 [AUTO] / [MANUAL] 모드를
        /// [CameraStateProvider]에 저장하고,
        /// 이후 탐지 연동 시 자동 제어 허용 여부 판단 기준으로 사용한다.
        /// </summary>
        /// <param name="message">
        /// [PTZ 제어 모드 설정 요청 메시지]
        /// </param>
        private void HandlePtzMode(
            CseCommandMessage message)
        {
            CseCommandPayload payload =
                message.Payload;

            if (payload == null)
            {
                Console.WriteLine("[CSE][CMD] PTZ Mode Failed : Payload is null");

                _responseService.SendCommandErrorResponse(
                    message,
                    "INVALID_PAYLOAD",
                    "PTZ Mode Failed : Payload is null");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                payload.Mode))
            {
                Console.WriteLine("[CSE][CMD] PTZ Mode Failed : Mode is empty");

                _responseService.SendCommandErrorResponse(
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
                Console.WriteLine("[CSE][CMD] PTZ Mode Failed : Unsupported Mode : " + payload.Mode);

                _responseService.SendCommandErrorResponse(
                    message,
                    "INVALID_MODE",
                    "PTZ Mode Failed : Unsupported Mode : " + payload.Mode);

                return;
            }

            Console.WriteLine("[CSE][CMD] PTZ Mode");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);
            Console.WriteLine("[CSE][CMD] Request MsgId : " + message.MsgId);
            Console.WriteLine("[CSE][CMD] Mode : " + mode);

            // [PTZ Mode] 상태 저장
            //
            // [AUTO] / [MANUAL] 모드는 장비 직접 제어 명령이 아니라,
            // 이후 수동 제어 / 자동 추적 제어를 구분하기 위한
            // 운용 상태값으로 관리한다.
            _cameraStateProvider.UpdatePtzControlMode(
                mode);

            // [PTZ Mode] 명령 응답 송신
            //
            // 현재 단계에서는 모드 상태 저장 성공 기준으로
            // 처리 성공 응답을 송신한다.
            _responseService.SendCommandResponse(
                message,
                "PTZ Mode Command Accepted : " + mode);
        }

        /// <summary>
        /// [영상 설정] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-009] 요청을 처리한다.
        /// 
        /// 현재 단계에서는 영상 밝기 / 대비 / Focus Mode 설정 요청을 수신하고,
        /// 처리 결과 응답을 송신한다.
        /// </summary>
        /// <param name="message">
        /// [영상 설정 요청 메시지]
        /// </param>
        private void HandleSetImage(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Set Image");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] Brightness : " + message.Payload.Brightness);
                Console.WriteLine("[CSE][CMD] Contrast : " + message.Payload.Contrast);
                Console.WriteLine("[CSE][CMD] FocusMode : " + message.Payload.FocusMode);
            }

            // [Set Image] 명령 응답 송신
            //
            // 실제 영상 설정 적용 기능은
            // 아직 구현되지 않았으므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "Set Image Command Accepted");
        }

        /// <summary>
        /// [영상 플립] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-010] 요청을 처리한다.
        /// 
        /// 현재 단계에서는 영상 반전 요청값을 확인하고,
        /// 처리 결과 응답을 송신한다.
        /// </summary>
        /// <param name="message">
        /// [영상 플립 설정 요청 메시지]
        /// </param>
        private void HandleSetFlip(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Set Flip");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);

            if (message.Payload != null)
            {
                Console.WriteLine("[CSE][CMD] Flip : " + message.Payload.Flip);
            }

            // [Set Flip] 명령 응답 송신
            //
            // 실제 영상 반전 기능은
            // 아직 구현되지 않았으므로
            // 명령 수신 성공 여부만 응답한다.
            _responseService.SendCommandResponse(
                message,
                "Set Flip Command Accepted");
        }

        #endregion

        #region [Status Command Methods]

        /// <summary>
        /// [카메라 상태 - 설정 조회] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-011] 요청을 처리한다.
        /// 
        /// 현재 영상 설정 / 탐지 설정 상태를 조회하여
        /// [q.status.res] Queue로 응답한다.
        /// 
        /// 실제 설정값 연동은 영상 / 탐지 / 장비 상태 모델 정리 후 반영한다.
        /// </summary>
        /// <param name="message">
        /// 카메라 설정 조회 요청 메시지
        /// </param>
        private void HandleGetConfig(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Get Config");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);
            Console.WriteLine("[CSE][CMD] Request MsgId : " + message.MsgId);

            // [카메라 설정] 응답 [Payload] 생성
            //
            // 현재 단계에서는 실제 설정 저장소가 없으므로
            // 기본값 기준으로 응답 구조만 먼저 구성한다.
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

            // [Status] 응답 송신
            //
            // 현재 단계에서는 실제 설정 저장소 대신
            // 기본 설정 정보를 응답한다.
            _responseService.SendStatusResponse(
                message,
                payload);
        }

        /// <summary>
        /// [카메라 상태 - PTZ 조회] 명령 처리
        /// 
        /// ICD 기준 [IF-GUIS-CSE-012] 요청을 처리한다.
        /// 
        /// 현재 장비 연결 상태와 [Pan] / [Tilt] / [Zoom]
        /// 상태 정보를 조회하여
        /// [q.status.res] Queue로 응답한다.
        /// </summary>
        /// <param name="message">
        /// [PTZ] 상태 조회 요청 메시지
        /// </param>
        private void HandleGetState(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Get PTZ State");
            Console.WriteLine("[CSE][CMD] InterfaceId : " + message.InterfaceId);
            Console.WriteLine("[CSE][CMD] Request MsgId : " + message.MsgId);

            // [PTZ] 상태 응답 [Payload] 생성
            //
            // [ADS1000] 수신 [Packet]에서 갱신된
            // 현재 [Pan] / [Tilt] / [Zoom] 값을 사용한다.
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
                            "yyyy-MM-ddTHH:mm:ss")
                };

            // [Status] 응답 송신
            //
            // 현재 단계에서는 [CameraStateProvider]에 저장된
            // 상태 정보를 기반으로 응답한다.
            _responseService.SendStatusResponse(
                message,
                payload);
        }
        #endregion
    }

}
