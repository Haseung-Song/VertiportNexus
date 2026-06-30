using System;
using VertiportNexus.Common;
using VertiportNexus.Models.Camera;
using VertiportNexus.Services.ADS1000;

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

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CameraCommandService] 생성자
        /// </summary>
        public CameraCommandService(
            Ads1000CameraControlService ads1000CameraControlService)
        {
            _ads1000CameraControlService =
                ads1000CameraControlService
                ?? throw new ArgumentNullException(
                    nameof(ads1000CameraControlService));
        }

        #endregion

        #region [Public Methods]

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

        #region [PTZ Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// 최신 [IF-GUIS-CSE-006] 기준
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
        /// 최신 [IF-GUIS-CSE-006]에서
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
        /// 최신 [IF-GUIS-CSE-006]에서
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

        #region [Continuous Move Methods]

        /// <summary>
        /// [continuous] 이동 명령 처리
        /// 
        /// 최신 [IF-GUIS-CSE-006] 기준
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
            Console.WriteLine("[CAMERA][CMD] Continuous Move Failed : No command or direction value");
        }

        /// <summary>
        /// [continuous] [command] 이동 처리
        /// 
        /// 최신 [IF-GUIS-CSE-006]의 [command] 허용값을 기준으로
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

        #region [Zoom Move Methods]

        /// <summary>
        /// [zoom] 이동 명령 처리
        /// 
        /// 최신 [IF-GUIS-CSE-006] 기준
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
            Console.WriteLine("[CAMERA][CMD] Zoom Move Failed : Zoom value is empty");
        }

        #endregion

        #region [Absolute Move Methods]

        /// <summary>
        /// [absolute] 위치 이동 명령 처리
        /// 
        /// [Pan] / [Tilt]는 각도 기반 절대 위치 이동,
        /// [Zoom] / [Focus]는 위치값 기반 이동으로 처리한다.
        /// </summary>
        private void HandleAbsoluteMove(
            CameraCommand command)
        {
            bool isHandled =
                false;

            if (command.Pan.HasValue)
            {
                _ads1000CameraControlService.MovePanAbsolute(
                    command.Pan.Value);

                isHandled =
                    true;
            }

            if (command.Tilt.HasValue)
            {
                _ads1000CameraControlService.MoveTiltAbsolute(
                    command.Tilt.Value);

                isHandled =
                    true;
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

        #region [Relative Move Methods]

        /// <summary>
        /// [relative] 상대 이동 명령 처리
        /// 
        /// 현재 위치 기준으로 [Pan] / [Tilt] 상대 이동을 처리한다.
        /// </summary>
        private void HandleRelativeMove(
            CameraCommand command)
        {
            bool isHandled =
                false;

            if (command.Pan.HasValue)
            {
                _ads1000CameraControlService.MovePanRelative(
                    command.Pan.Value);

                isHandled =
                    true;
            }

            if (command.Tilt.HasValue)
            {
                _ads1000CameraControlService.MoveTiltRelative(
                    command.Tilt.Value);

                isHandled =
                    true;
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
        /// [IF-GUIS-CSE-006]의 [zoom] 값은 실제 배율 기준이고,
        /// [ADS1000] 장비 제어는 [0 ~ 1000] 위치값 기준으로 수행한다.
        /// 
        /// 장비 스펙 기준으로 최대 배율을 [66x] 기준으로 구현한다.
        /// </summary>
        /// <param name="zoomRatio">
        /// Zoom 배율
        /// </param>
        /// <returns>
        /// ADS1000 Zoom 위치값
        /// </returns>
        private ushort ConvertZoomRatioToPosition(
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
        private void PrintCommandStartLog(CameraCommand command)
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
        /// 입력값을 지정 범위 안으로 제한
        /// </summary>
        private double Clamp(
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
