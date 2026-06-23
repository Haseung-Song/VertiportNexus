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
        /// <param name="ads1000CameraControlService">
        /// [ADS1000] 카메라 제어 서비스
        /// </param>
        public CameraCommandService(
            Ads1000CameraControlService ads1000CameraControlService)
        {
            _ads1000CameraControlService =
                ads1000CameraControlService;
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
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CAMERA][CMD] Camera Command End");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [PTZ Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// [continuous] / [absolute] / [relative] 모드에 따라
        /// 현재 장비 제어 서비스 호출을 분기한다.
        /// </summary>
        /// <param name="command">
        /// 내부 카메라 명령
        /// </param>
        private void HandlePtzMove(
            CameraCommand command)
        {
            if (string.IsNullOrWhiteSpace(
                command.Mode))
            {
                Console.WriteLine("[CAMERA][CMD] PTZ Move Failed : Mode is empty");
                return;
            }

            if (command.Mode.Equals(
                "continuous",
                StringComparison.OrdinalIgnoreCase))
            {
                HandleContinuousMove(
                    command);

                return;
            }

            if (command.Mode.Equals(
                "absolute",
                StringComparison.OrdinalIgnoreCase))
            {
                HandleAbsoluteMove(
                    command);

                return;
            }

            if (command.Mode.Equals(
                "relative",
                StringComparison.OrdinalIgnoreCase))
            {
                HandleRelativeMove(
                    command);

                return;
            }
            Console.WriteLine("[CAMERA][CMD] Unsupported PTZ Mode : " + command.Mode);
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
        /// <param name="command">
        /// 내부 카메라 명령
        /// </param>
        private void HandleContinuousMove(
            CameraCommand command)
        {
            if (command.Pan.HasValue)
            {
                if (command.Pan.Value > 0)
                {
                    _ads1000CameraControlService.PanRight();
                    return;
                }

                if (command.Pan.Value < 0)
                {
                    _ads1000CameraControlService.PanLeft();
                    return;
                }

            }

            if (command.Tilt.HasValue)
            {
                if (command.Tilt.Value > 0)
                {
                    _ads1000CameraControlService.TiltUp();
                    return;
                }

                if (command.Tilt.Value < 0)
                {
                    _ads1000CameraControlService.TiltDown();
                    return;
                }

            }

            if (command.Zoom.HasValue)
            {
                if (command.Zoom.Value > 0)
                {
                    _ads1000CameraControlService.ZoomIn();
                    return;
                }

                if (command.Zoom.Value < 0)
                {
                    _ads1000CameraControlService.ZoomOut();
                    return;
                }

            }

            if (command.Focus.HasValue)
            {
                if (command.Focus.Value > 0)
                {
                    _ads1000CameraControlService.FocusFar();
                    return;
                }

                if (command.Focus.Value < 0)
                {
                    _ads1000CameraControlService.FocusNear();
                    return;
                }

            }
            Console.WriteLine("[CAMERA][CMD] Continuous Move Failed : No direction value");
        }

        #endregion

        #region [Absolute Move Methods]

        /// <summary>
        /// [absolute] 위치 이동 명령 처리
        /// 
        /// [Pan] / [Tilt]는 각도 기반 절대 위치 이동,
        /// [Zoom] / [Focus]는 위치값 기반 이동으로 처리한다.
        /// </summary>
        /// <param name="command">
        /// 내부 카메라 명령
        /// </param>
        private void HandleAbsoluteMove(
            CameraCommand command)
        {
            if (command.Pan.HasValue)
            {
                _ads1000CameraControlService
                    .MovePanAbsolute(
                        command.Pan.Value);
            }

            if (command.Tilt.HasValue)
            {
                _ads1000CameraControlService
                    .MoveTiltAbsolute(
                        command.Tilt.Value);
            }

            if (command.Zoom.HasValue)
            {
                _ads1000CameraControlService
                    .MoveZoomPosition(
                        (ushort)Clamp(
                            command.Zoom.Value,
                            0,
                            1000));
            }

            if (command.Focus.HasValue)
            {
                _ads1000CameraControlService
                    .MoveFocusPosition(
                        (ushort)Clamp(
                            command.Focus.Value,
                            0,
                            1000));
            }

        }

        #endregion

        #region [Relative Move Methods]

        /// <summary>
        /// [relative] 상대 이동 명령 처리
        /// 
        /// 현재 위치 기준으로 [Pan] / [Tilt] 상대 이동을 처리한다.
        /// </summary>
        /// <param name="command">
        /// 내부 카메라 명령
        /// </param>
        private void HandleRelativeMove(
            CameraCommand command)
        {
            if (command.Pan.HasValue)
            {
                _ads1000CameraControlService
                    .MovePanRelative(
                        command.Pan.Value);
            }

            if (command.Tilt.HasValue)
            {
                _ads1000CameraControlService
                    .MoveTiltRelative(
                        command.Tilt.Value);
            }

            if (command.Zoom.HasValue ||
                command.Focus.HasValue)
            {
                Console.WriteLine("[CAMERA][CMD] Relative Zoom / Focus is not supported yet");
            }

        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// 입력값을 지정 범위 안으로 제한
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
        private double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        #endregion
    }

}
