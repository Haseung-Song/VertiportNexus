using System;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Models.Camera;
using VertiportNexus.Services.ADS1000;
using VertiportNexus.Services.Camera;

namespace VertiportNexus.Services.Command
{
    /// <summary>
    /// 내부 카메라 명령 처리 서비스
    /// 
    /// [CSE] 명령 처리부에서 변환한 [CameraCommand]를 받아
    /// 현재 장비 제어 서비스에서 사용할 수 있는 명령으로 분기한다.
    /// </summary>
    internal class CameraCommandService
    {
        #region [Constants]

        /// <summary>
        /// [PTZ] 연속 이동 모드
        /// </summary>
        private const string PTZ_MODE_CONTINUOUS =
            "continuous";

        /// <summary>
        /// [PTZ] 절대 위치 이동 모드
        /// </summary>
        private const string PTZ_MODE_ABSOLUTE =
            "absolute";

        /// <summary>
        /// [PTZ] 상대 위치 이동 모드
        /// </summary>
        private const string PTZ_MODE_RELATIVE =
            "relative";

        /// <summary>
        /// [PTZ] Zoom 제어 모드
        /// </summary>
        private const string PTZ_MODE_ZOOM =
            "zoom";

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
        /// [PTZ] 정지 명령
        /// </summary>
        private const string PTZ_COMMAND_STOP =
            "stop";

        /// <summary>
        /// [Pan] 좌측 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_LEFT =
            "left";

        /// <summary>
        /// [Pan] 우측 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_RIGHT =
            "right";

        /// <summary>
        /// [Tilt] 상향 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_UP =
            "up";

        /// <summary>
        /// [Tilt] 하향 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_DOWN =
            "down";

        /// <summary>
        /// [Pan / Tilt] 좌상향 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_LEFT_UP =
            "left_up";

        /// <summary>
        /// [Pan / Tilt] 우상향 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_RIGHT_UP =
            "right_up";

        /// <summary>
        /// [Pan / Tilt] 좌하향 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_LEFT_DOWN =
            "left_down";

        /// <summary>
        /// [Pan / Tilt] 우하향 이동 명령
        /// </summary>
        private const string PTZ_COMMAND_RIGHT_DOWN =
            "right_down";

        #endregion

        #region [Fields]

        /// <summary>
        /// [ADS1000] 카메라 제어 서비스
        /// </summary>
        private readonly Ads1000CameraControlService _ads1000CameraControlService;

        /// <summary>
        /// [Camera] 상태 저장 서비스
        /// 
        /// 현재 [PTZ] 제어 모드가 [AUTO] / [MANUAL] 중
        /// 어떤 상태인지 확인하기 위해 사용한다.
        /// </summary>
        private readonly CameraStateProvider _cameraStateProvider;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CameraCommandService] 생성자
        /// 
        /// [Camera] 명령 처리에 필요한
        /// ADS1000 장비 제어 서비스와 카메라 상태 저장 서비스를 주입받는다.
        /// </summary>
        /// <param name="ads1000CameraControlService">
        /// [ADS1000] 카메라 제어 서비스
        /// </param>
        /// <param name="cameraStateProvider">
        /// [Camera] 상태 저장 서비스
        /// </param>
        public CameraCommandService(
            Ads1000CameraControlService ads1000CameraControlService,
            CameraStateProvider cameraStateProvider)
        {
            _ads1000CameraControlService =
                ads1000CameraControlService
                ?? throw new ArgumentNullException(
                    nameof(ads1000CameraControlService));

            _cameraStateProvider =
                cameraStateProvider
                ?? throw new ArgumentNullException(
                    nameof(cameraStateProvider));
        }

        #endregion

        #region [Command Handle Methods]

        /// <summary>
        /// 내부 카메라 명령 처리
        /// </summary>
        /// <param name="command">
        /// 내부 카메라 명령
        /// </param>
        public void HandleCommand(
            CameraCommand command)
        {
            if (command == null)
            {
                Console.WriteLine("[CAMERA][CMD] Handle Failed : Command is null");
                return;
            }

            PrintCommandStartLog(
                command);

            switch (command.CommandType)
            {
                case CameraCommandType.PtzMove:
                    HandlePtzMove(
                        command);
                    break;

                case CameraCommandType.PtzStop:
                    HandlePtzStop();
                    break;

                case CameraCommandType.GetState:
                    Console.WriteLine("[CAMERA][CMD] GetState Received");
                    break;

                default:
                    Console.WriteLine("[CAMERA][CMD] Unsupported Command Type : " + command.CommandType);
                    break;
            }
            PrintCommandEndLog();
        }

        #endregion

        #region [PTZ Command Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// 최종 [IF-GUIS-CSE-004] 기준
        /// [zoom] / [continuous] / [relative] / [absolute] / [auto] / [manual]
        /// 모드에 따라 실제 장비 제어 서비스를 호출한다.
        /// </summary>
        private void HandlePtzMove(
            CameraCommand command)
        {
            if (string.IsNullOrWhiteSpace(
                command.Mode))
            {
                Console.WriteLine("[CAMERA][CMD] PTZ Move Failed : Mode is empty");
                return;
            }

            string mode =
                command.Mode
                    .Trim()
                    .ToLower();

            switch (mode)
            {
                case PTZ_MODE_ZOOM:
                    HandleZoomMove(
                        command);
                    break;

                case PTZ_MODE_CONTINUOUS:
                    HandleContinuousMove(
                        command);
                    break;

                case PTZ_MODE_RELATIVE:
                    HandleRelativeMove(
                        command);
                    break;

                case PTZ_MODE_ABSOLUTE:
                    HandleAbsoluteMove(
                        command);
                    break;

                case PTZ_MODE_AUTO:
                    HandleAutoMode();
                    break;

                case PTZ_MODE_MANUAL:
                    HandleManualMode();
                    break;

                default:
                    Console.WriteLine("[CAMERA][CMD] Unsupported PTZ Mode : " + command.Mode);
                    break;
            }

        }

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정 처리
        /// 
        /// 최종 [IF-GUIS-CSE-004]에서
        /// [mode] 값이 [auto]인 경우 호출된다.
        /// 
        /// 실제 상태 저장은 [CseCommandHandler] 또는
        /// [CameraStateProvider] 연동 단계에서 처리한다.
        /// 현재 단계에서는 수신 흐름 확인용 로그만 출력한다.
        /// </summary>
        private void HandleAutoMode()
        {
            Console.WriteLine("[CAMERA][CMD] PTZ Mode : AUTO");
        }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정 처리
        /// 
        /// 최종 [IF-GUIS-CSE-004]에서
        /// [mode] 값이 [manual]인 경우 호출된다.
        /// 
        /// 실제 상태 저장은 [CseCommandHandler] 또는
        /// [CameraStateProvider] 연동 단계에서 처리한다.
        /// 현재 단계에서는 수신 흐름 확인용 로그만 출력한다.
        /// </summary>
        private void HandleManualMode()
        {
            Console.WriteLine("[CAMERA][CMD] PTZ Mode : MANUAL");
        }

        /// <summary>
        /// [PTZ] 정지 명령 처리
        /// </summary>
        private void HandlePtzStop()
        {
            _ads1000CameraControlService.StopMove();
        }

        #endregion

        #region [Continuous PTZ Methods]

        /// <summary>
        /// [continuous] 이동 명령 처리
        /// 
        /// 최종 [IF-GUIS-CSE-004] 기준
        /// [command] 값으로 Pan / Tilt 연속 이동 방향을 분기한다.
        /// 
        /// 기존 [pan] / [tilt] 부호 기반 제어도
        /// 호환을 위해 유지한다.
        /// </summary>
        private void HandleContinuousMove(
            CameraCommand command)
        {
            if (!string.IsNullOrWhiteSpace(
                command.Command))
            {
                HandleContinuousCommandMove(
                    command.Command);

                return;
            }

            if (TryHandleContinuousPan(
                command))
            {
                return;
            }

            if (TryHandleContinuousTilt(
                command))
            {
                return;
            }

            if (TryHandleContinuousZoom(
                command))
            {
                return;
            }

            if (TryHandleContinuousFocus(
                command))
            {
                return;
            }

            Console.WriteLine(
                "[CAMERA][CMD] Continuous Move Failed : No command or direction value");
        }

        /// <summary>
        /// [continuous] [command] 이동 처리
        /// 
        /// 최종 ICD [IF-GUIS-CSE-004]의 [command] 허용값을 기준으로
        /// Pan / Tilt 연속 이동 또는 정지 명령을 수행한다.
        /// </summary>
        /// <param name="command">
        /// PTZ 이동 명령
        /// </param>
        private void HandleContinuousCommandMove(
            string command)
        {
            string normalizedCommand =
                command
                    .Trim()
                    .ToLower();

            if (IsAutoMode() &&
                IsPanTiltDirectionCommand(
                    normalizedCommand))
            {
                Console.WriteLine(
                    "[CAMERA][CMD] Continuous Pan/Tilt Ignored : PTZ Mode is AUTO");

                Console.WriteLine(
                    "[CAMERA][CMD] Command : "
                    + normalizedCommand);

                return;
            }

            switch (normalizedCommand)
            {
                case PTZ_COMMAND_STOP:
                    _ads1000CameraControlService
                        .StopPanTiltMove();
                    break;

                case PTZ_COMMAND_LEFT:
                    _ads1000CameraControlService
                        .PanLeft();
                    break;

                case PTZ_COMMAND_RIGHT:
                    _ads1000CameraControlService
                        .PanRight();
                    break;

                case PTZ_COMMAND_UP:
                    _ads1000CameraControlService
                        .TiltUp();
                    break;

                case PTZ_COMMAND_DOWN:
                    _ads1000CameraControlService
                        .TiltDown();
                    break;

                case PTZ_COMMAND_LEFT_UP:
                    _ads1000CameraControlService
                        .PanLeft();

                    _ads1000CameraControlService
                        .TiltUp();
                    break;

                case PTZ_COMMAND_RIGHT_UP:
                    _ads1000CameraControlService
                        .PanRight();

                    _ads1000CameraControlService
                        .TiltUp();
                    break;

                case PTZ_COMMAND_LEFT_DOWN:
                    _ads1000CameraControlService
                        .PanLeft();

                    _ads1000CameraControlService
                        .TiltDown();
                    break;

                case PTZ_COMMAND_RIGHT_DOWN:
                    _ads1000CameraControlService
                        .PanRight();

                    _ads1000CameraControlService
                        .TiltDown();
                    break;

                default:
                    Console.WriteLine("[CAMERA][CMD] Unsupported PTZ Command : " + command);
                    break;
            }

        }

        /// <summary>
        /// [Pan] 연속 이동 처리
        /// </summary>
        private bool TryHandleContinuousPan(
            CameraCommand command)
        {
            if (!command.Pan.HasValue ||
                command.Pan.Value == 0)
            {
                return false;
            }

            if (IsAutoMode())
            {
                Console.WriteLine(
                    "[CAMERA][CMD] Continuous Pan Ignored : PTZ Mode is AUTO");

                return true;
            }

            if (command.Pan.Value > 0)
            {
                _ads1000CameraControlService.PanRight();
            }
            else
            {
                _ads1000CameraControlService.PanLeft();
            }

            return true;
        }

        /// <summary>
        /// [Tilt] 연속 이동 처리
        /// </summary>
        private bool TryHandleContinuousTilt(
            CameraCommand command)
        {
            if (!command.Tilt.HasValue ||
                command.Tilt.Value == 0)
            {
                return false;
            }

            if (IsAutoMode())
            {
                Console.WriteLine(
                    "[CAMERA][CMD] Continuous Tilt Ignored : PTZ Mode is AUTO");

                return true;
            }

            if (command.Tilt.Value > 0)
            {
                _ads1000CameraControlService.TiltUp();
            }
            else
            {
                _ads1000CameraControlService.TiltDown();
            }

            return true;
        }

        /// <summary>
        /// [Zoom] 연속 이동 처리
        /// </summary>
        private bool TryHandleContinuousZoom(
            CameraCommand command)
        {
            if (!command.Zoom.HasValue ||
                command.Zoom.Value == 0)
            {
                return false;
            }

            if (command.Zoom.Value > 0)
            {
                _ads1000CameraControlService.ZoomIn();
            }
            else
            {
                _ads1000CameraControlService.ZoomOut();
            }

            return true;
        }

        /// <summary>
        /// [Focus] 연속 이동 처리
        /// </summary>
        private bool TryHandleContinuousFocus(
            CameraCommand command)
        {
            if (!command.Focus.HasValue ||
                command.Focus.Value == 0)
            {
                return false;
            }

            if (command.Focus.Value > 0)
            {
                _ads1000CameraControlService.FocusFar();
            }
            else
            {
                _ads1000CameraControlService.FocusNear();
            }

            return true;
        }

        #endregion

        #region [Zoom Command Methods]

        /// <summary>
        /// [zoom] 이동 명령 처리
        /// 
        /// 최종 [IF-GUIS-CSE-004] 기준
        /// [Zoom] 단독 제어를 처리한다.
        /// 
        /// [Zoom] 제어는 [AUTO] / [MANUAL] 모드와 관계없이
        /// 항상 수행 가능하다.
        /// </summary>
        /// <param name="command">
        /// 내부 카메라 명령
        /// </param>
        private void HandleZoomMove(
            CameraCommand command)
        {
            if (command.ZoomPosition.HasValue)
            {
                ushort zoomPosition =
                    (ushort)Clamp(
                        command.ZoomPosition.Value,
                        0,
                        1000);

                Console.WriteLine("[CAMERA][ZOOM] Position Input : " + command.ZoomPosition.Value);
                Console.WriteLine("[CAMERA][ZOOM] Position Target : " + zoomPosition);

                _ads1000CameraControlService
                    .MoveZoomPosition(
                        zoomPosition);

                return;
            }

            if (command.Zoom.HasValue)
            {
                ushort zoomPosition =
                    ConvertZoomRatioToPosition(
                        command.Zoom.Value);

                Console.WriteLine("[CAMERA][ZOOM] Ratio Input : " + command.Zoom.Value);
                Console.WriteLine("[CAMERA][ZOOM] Position Target : " + zoomPosition);

                _ads1000CameraControlService
                    .MoveZoomPosition(
                        zoomPosition);

                return;
            }

            Console.WriteLine(
                "[CAMERA][CMD] Zoom Move Failed : Zoom value is empty");
        }

        #endregion

        #region [Absolute PTZ Methods]

        /// <summary>
        /// [absolute] 위치 이동 명령 처리
        /// 
        /// [Pan] / [Tilt]는 각도 기반 절대 위치 이동,
        /// [Zoom] / [Focus]는 위치값 기반 이동으로 처리한다.
        /// 
        /// [AUTO] 모드에서는 Pan / Tilt 수동 제어를 무시하고,
        /// Zoom 제어는 [AUTO] / [MANUAL] 관계없이 허용한다.
        /// </summary>
        private void HandleAbsoluteMove(
            CameraCommand command)
        {
            bool isHandled =
                false;

            if (command.Pan.HasValue)
            {
                if (IsAutoMode())
                {
                    Console.WriteLine(
                        "[CAMERA][CMD] Absolute Pan Ignored : PTZ Mode is AUTO");
                }
                else
                {
                    if (!_cameraStateProvider.CurrentPan.HasValue)
                    {
                        Console.WriteLine(
                            "[CAMERA][PTZ] Absolute Pan Failed : Current Pan is empty");

                        return;
                    }

                    double currentPanRaw =
                        _cameraStateProvider
                            .CurrentPan
                            .Value;

                    double currentPanDisplay =
                        NormalizePanStatus(
                            currentPanRaw);

                    double targetPan =
                        Clamp(
                            command.Pan.Value,
                            0,
                            360);

                    double panMoveAngle =
                        CalculatePanMoveAngle(
                            currentPanRaw,
                            targetPan,
                            _cameraStateProvider.PanTurnMode);

                    double panCommandTarget =
                        currentPanRaw + panMoveAngle;

                    Console.WriteLine(
                        "[CAMERA][PTZ] Absolute Pan Input : "
                        + command.Pan.Value);

                    Console.WriteLine(
                        "[CAMERA][PTZ] Absolute Pan Mode : "
                        + _cameraStateProvider.PanTurnMode);

                    Console.WriteLine(
                        "[CAMERA][PTZ] Absolute Pan Current Raw : "
                        + currentPanRaw.ToString("F2"));

                    Console.WriteLine(
                        "[CAMERA][PTZ] Absolute Pan Current Display : "
                        + currentPanDisplay.ToString("F2"));

                    Console.WriteLine(
                        "[CAMERA][PTZ] Absolute Pan Target Display : "
                        + targetPan.ToString("F2"));

                    Console.WriteLine(
                        "[CAMERA][PTZ] Absolute Pan Move Angle : "
                        + panMoveAngle.ToString("F2"));

                    Console.WriteLine(
                        "[CAMERA][PTZ] Absolute Pan Command Target Raw : "
                        + panCommandTarget.ToString("F2"));

                    _ads1000CameraControlService
                        .MovePanAbsolute(
                            panCommandTarget);

                    isHandled =
                        true;
                }

            }

            if (command.Tilt.HasValue)
            {
                if (IsAutoMode())
                {
                    Console.WriteLine(
                        "[CAMERA][CMD] Absolute Tilt Ignored : PTZ Mode is AUTO");
                }
                else
                {
                    double tilt =
                        Clamp(
                            command.Tilt.Value,
                            -90,
                            90);

                    Console.WriteLine("[CAMERA][PTZ] Absolute Tilt Input : " + command.Tilt.Value);
                    Console.WriteLine("[CAMERA][PTZ] Absolute Tilt Target : " + tilt);

                    _ads1000CameraControlService
                        .MoveTiltAbsolute(
                            tilt);

                    isHandled =
                        true;
                }

            }

            // [Zoom] 위치값 직접 제어
            //
            // [zoom_position] 값이 들어온 경우에는
            // ADS1000 장비 위치값 [0 ~ 1000]으로 판단하여 그대로 제어한다.
            if (command.ZoomPosition.HasValue)
            {
                ushort zoomPosition =
                    (ushort)Clamp(
                        command.ZoomPosition.Value,
                        0,
                        1000);

                Console.WriteLine("[CAMERA][ZOOM] Position Input : " + command.ZoomPosition.Value);
                Console.WriteLine("[CAMERA][ZOOM] Position Target : " + zoomPosition);

                _ads1000CameraControlService
                    .MoveZoomPosition(
                        zoomPosition);

                isHandled =
                    true;
            }

            // [Zoom] 배율 제어
            //
            // [zoom] 값이 들어온 경우에는
            // 실제 배율 [1x ~ 66x] 기준으로 판단하여
            // ADS1000 장비 위치값 [0 ~ 1000]으로 변환 후 제어한다.
            else if (command.Zoom.HasValue)
            {
                ushort zoomPosition =
                    ConvertZoomRatioToPosition(
                        command.Zoom.Value);

                Console.WriteLine("[CAMERA][ZOOM] Ratio Input : " + command.Zoom.Value);
                Console.WriteLine("[CAMERA][ZOOM] Position Target : " + zoomPosition);

                _ads1000CameraControlService
                    .MoveZoomPosition(
                        zoomPosition);

                isHandled =
                    true;
            }

            if (command.Focus.HasValue)
            {
                _ads1000CameraControlService.MoveFocusPosition(
                    (ushort)Clamp(
                        command.Focus.Value,
                        0,
                        1000));

                isHandled =
                    true;
            }

            if (!isHandled)
            {
                Console.WriteLine("[CAMERA][CMD] Absolute Move Failed : No target value");
            }

        }

        #endregion

        #region [Relative PTZ Methods]

        /// <summary>
        /// [relative] 상대 이동 명령 처리
        /// 
        /// 현재 위치 기준으로 [Pan] / [Tilt] 상대 이동을 처리한다.
        /// 
        /// [AUTO] 모드에서는 Pan / Tilt 수동 제어를 무시한다.
        /// </summary>
        private void HandleRelativeMove(
            CameraCommand command)
        {
            bool isHandled =
                false;

            if (command.Pan.HasValue)
            {
                if (IsAutoMode())
                {
                    Console.WriteLine(
                        "[CAMERA][CMD] Relative Pan Ignored : PTZ Mode is AUTO");

                    isHandled =
                        true;
                }
                else
                {
                    _ads1000CameraControlService.MovePanRelative(
                        command.Pan.Value);

                    isHandled =
                        true;
                }

            }

            if (command.Tilt.HasValue)
            {
                if (IsAutoMode())
                {
                    Console.WriteLine(
                        "[CAMERA][CMD] Relative Tilt Ignored : PTZ Mode is AUTO");

                    isHandled =
                        true;
                }
                else
                {
                    _ads1000CameraControlService.MoveTiltRelative(
                        command.Tilt.Value);

                    isHandled =
                        true;
                }

            }

            if (command.Zoom.HasValue ||
                command.Focus.HasValue)
            {
                Console.WriteLine("[CAMERA][CMD] Relative Zoom / Focus is not supported yet");
            }

            if (!isHandled)
            {
                Console.WriteLine("[CAMERA][CMD] Relative Move Failed : No target value");
            }

        }

        #endregion

        #region [Zoom Convert Methods]
        /// <summary>
        /// [Zoom] 배율을 [ADS1000] 위치값으로 변환
        /// 
        /// [UI] 또는 [ICD]에서 사용하는 [Zoom] 배율값을
        /// [ADS1000] 제어용 [0 ~ 1000] 위치값으로 변환한다.
        /// 
        /// 변환 기준:
        /// [1x]  = 0
        /// [66x] = 1000
        /// </summary>
        /// <param name="zoomRatio">
        /// Zoom 배율
        /// </param>
        /// <returns>
        /// ADS1000 Zoom 위치값
        /// </returns>
        internal static ushort ConvertZoomRatioToPosition(
            double zoomRatio)
        {
            const double MIN_ZOOM_RATIO =
                1.0;

            const double MAX_ZOOM_RATIO =
                66.0;

            double clampedZoomRatio =
                Clamp(
                    zoomRatio,
                    MIN_ZOOM_RATIO,
                    MAX_ZOOM_RATIO);

            double zoomPosition =
                (clampedZoomRatio - MIN_ZOOM_RATIO)
                / (MAX_ZOOM_RATIO - MIN_ZOOM_RATIO)
                * 1000.0;

            return (ushort)Math.Round(
                zoomPosition);
        }


        #endregion

        #region [Log Methods]

        /// <summary>
        /// 내부 카메라 명령 처리 시작 로그 출력
        /// </summary>
        private void PrintCommandStartLog(
            CameraCommand command)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CAMERA][CMD] Camera Command Start");
            Console.WriteLine("[CAMERA][CMD] Type : " + command.CommandType);
            Console.WriteLine("[CAMERA][CMD] Mode : " + command.Mode);
            Console.WriteLine("[CAMERA][CMD] Command : " + command.Command);
            Console.WriteLine("[CAMERA][CMD] Pan : " + command.Pan);
            Console.WriteLine("[CAMERA][CMD] Tilt : " + command.Tilt);
            Console.WriteLine("[CAMERA][CMD] Zoom : " + command.Zoom);
            Console.WriteLine("[CAMERA][CMD] Focus : " + command.Focus);
            Console.WriteLine("[CAMERA][CMD] Source MsgId : " + command.SourceMsgId);
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 내부 카메라 명령 처리 종료 로그 출력
        /// </summary>
        private void PrintCommandEndLog()
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CAMERA][CMD] Camera Command End");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// [PTZ] AUTO 모드 여부 확인
        /// 
        /// AUTO 모드에서는 GUI / MQ 기반 Pan / Tilt 수동 제어를 수행하지 않는다.
        /// </summary>
        /// <returns>
        /// AUTO 모드 여부
        /// </returns>
        private bool IsAutoMode()
        {
            return string.Equals(
                _cameraStateProvider.PtzControlMode,
                PTZ_MODE_AUTO,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Pan] 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치와 목표 [Pan] 위치를 기준으로
        /// 선택된 선회 모드에 따라 장비로 송신할 이동 각도를 계산한다.
        /// 
        /// [Short] 모드는 가장 가까운 방향의 이동 각도를 계산하고,
        /// [Via 0] 모드는 단거리 보정 없이 목표 위치와 현재 위치의 차이를 사용한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="panTurnMode">
        /// Pan 선회 모드
        /// </param>
        /// <returns>
        /// 장비로 송신할 Pan 이동 각도
        /// </returns>
        internal static double CalculatePanMoveAngle(
            double currentPan,
            double targetPan,
            Ads1000PanTurnMode panTurnMode)
        {
            double normalizedCurrentPan =
                NormalizePanStatus(
                    currentPan);

            double normalizedTargetPan =
                NormalizePanStatus(
                    targetPan);

            if (panTurnMode == Ads1000PanTurnMode.Short)
            {
                return CalculateShortestPanDelta(
                    normalizedCurrentPan,
                    normalizedTargetPan);
            }

            return CalculateViaZeroPanDelta(
                normalizedCurrentPan,
                normalizedTargetPan);
        }

        /// <summary>
        /// [Pan] 최단 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치에서 목표 [Pan] 위치까지
        /// 가장 가까운 방향의 이동 각도를 계산한다.
        /// 
        /// 결과값은 [-180 ~ 180] 범위로 반환한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <returns>
        /// 최단 이동 각도
        /// </returns>
        private static double CalculateShortestPanDelta(
            double currentPan,
            double targetPan)
        {
            const double FULL_ROTATION_DEGREES =
                360.0;

            const double HALF_ROTATION_DEGREES =
                180.0;

            double delta =
                (targetPan
                 - currentPan
                 + HALF_ROTATION_DEGREES
                 + FULL_ROTATION_DEGREES)
                % FULL_ROTATION_DEGREES
                - HALF_ROTATION_DEGREES;

            return NormalizeZeroAngle(
                delta);
        }

        /// <summary>
        /// [Pan] [Via 0] 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치에서 목표 [Pan] 위치까지
        /// 단거리 보정 없이 이동 각도를 계산한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <returns>
        /// Via 0 기준 이동 각도
        /// </returns>
        private static double CalculateViaZeroPanDelta(
            double currentPan,
            double targetPan)
        {
            double delta =
                targetPan - currentPan;

            return NormalizeZeroAngle(
                delta);
        }

        /// <summary>
        /// [Pan] 상태값 범위 정규화
        /// 
        /// ADS1000 상태 Packet에서 수신한 Pan 값을
        /// [0 ~ 360] 범위로 변환한다.
        /// 
        /// Pan 값이 360도를 초과하면 0도부터 다시 시작하고,
        /// 0도 미만이면 360도 기준으로 순환 처리한다.
        /// </summary>
        /// <param name="pan">
        /// Pan 원본 상태값
        /// </param>
        /// <returns>
        /// [0 ~ 360] 범위로 정규화된 Pan 상태값
        /// </returns>
        internal static double NormalizePanStatus(
            double pan)
        {
            const double FULL_ROTATION_DEGREES =
                360.0;

            const double ZERO_EPSILON =
                0.001;

            double normalizedPan =
                pan % FULL_ROTATION_DEGREES;

            if (normalizedPan < 0)
            {
                normalizedPan +=
                    FULL_ROTATION_DEGREES;
            }

            if (Math.Abs(normalizedPan) <= ZERO_EPSILON ||
                Math.Abs(normalizedPan - FULL_ROTATION_DEGREES) <= ZERO_EPSILON)
            {
                return 0.0;
            }

            return NormalizePosition(
                normalizedPan);
        }

        /// <summary>
        /// [각도] 미세 오차 보정
        /// 
        /// 장비 Encoder 오차 또는 계산 과정에서 발생한
        /// [0] 근처 미세값을 [0]으로 보정한다.
        /// </summary>
        /// <param name="angle">
        /// 원본 각도
        /// </param>
        /// <returns>
        /// 미세 오차가 보정된 각도
        /// </returns>
        /// <summary>
        /// [위치 상태값] 미세 오차 보정
        /// 
        /// 장비 Encoder 또는 위치 응답에서 발생하는
        /// [0] 근처 또는 정수 위치 근처의 미세 오차를
        /// 화면 표시 및 상태 응답 기준에서 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 위치값
        /// </param>
        /// <returns>
        /// 미세 오차가 보정된 위치값
        /// </returns>
        private static double NormalizePosition(
            double value)
        {
            const double ZERO_EPSILON =
                0.001;

            const double INTEGER_EPSILON =
                0.001;

            if (Math.Abs(value) <= ZERO_EPSILON)
            {
                return 0.0;
            }

            double roundedValue =
                Math.Round(
                    value);

            if (Math.Abs(value - roundedValue) <= INTEGER_EPSILON)
            {
                return roundedValue;
            }

            return value;
        }

        private static double NormalizeZeroAngle(
            double angle)
        {
            const double ZERO_EPSILON =
                0.001;

            if (Math.Abs(angle) <= ZERO_EPSILON)
            {
                return 0.0;
            }

            return angle;
        }

        /// <summary>
        /// [Pan / Tilt] 방향 명령 여부 확인
        /// </summary>
        /// <param name="command">
        /// PTZ 이동 명령
        /// </param>
        /// <returns>
        /// Pan / Tilt 방향 명령 여부
        /// </returns>
        private bool IsPanTiltDirectionCommand(
            string command)
        {
            return command == PTZ_COMMAND_LEFT ||
                   command == PTZ_COMMAND_RIGHT ||
                   command == PTZ_COMMAND_UP ||
                   command == PTZ_COMMAND_DOWN ||
                   command == PTZ_COMMAND_LEFT_UP ||
                   command == PTZ_COMMAND_RIGHT_UP ||
                   command == PTZ_COMMAND_LEFT_DOWN ||
                   command == PTZ_COMMAND_RIGHT_DOWN;
        }

        /// <summary>
        /// 입력값 범위 제한
        /// 
        /// 입력값이 지정된 최소 / 최대 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 값
        /// </param>
        /// <param name="min">
        /// 최소 허용값
        /// </param>
        /// <param name="max">
        /// 최대 허용값
        /// </param>
        /// <returns>
        /// 범위 제한이 적용된 값
        /// </returns>
        internal static double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }
            return value;
        }
        #endregion
    }

}
