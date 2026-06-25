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
        /// [continuous] / [absolute] / [relative] 모드에 따라
        /// 현재 장비 제어 서비스 호출을 분기한다.
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
                command.Mode.Trim().ToLower();

            switch (mode)
            {
                case PTZ_MODE_CONTINUOUS:
                    HandleContinuousMove(
                        command);
                    break;

                case PTZ_MODE_ABSOLUTE:
                    HandleAbsoluteMove(
                        command);
                    break;

                case PTZ_MODE_RELATIVE:
                    HandleRelativeMove(
                        command);
                    break;

                default:
                    Console.WriteLine("[CAMERA][CMD] Unsupported PTZ Mode : " + command.Mode);
                    break;
            }

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
        /// [pan] / [tilt] / [zoom] / [focus] 값의 부호를 기준으로
        /// 현재 구현된 연속 제어 함수를 호출한다.
        /// </summary>
        private void HandleContinuousMove(
            CameraCommand command)
        {
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
            Console.WriteLine("[CAMERA][CMD] Continuous Move Failed : No direction value");
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
