using System;
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

            Console.WriteLine("[CAMERA][CMD] Handle Start");
            Console.WriteLine("[CAMERA][CMD] Type : " + command.CommandType);
            Console.WriteLine("[CAMERA][CMD] Mode : " + command.Mode);
            Console.WriteLine("[CAMERA][CMD] Pan : " + command.Pan);
            Console.WriteLine("[CAMERA][CMD] Tilt : " + command.Tilt);
            Console.WriteLine("[CAMERA][CMD] Zoom : " + command.Zoom);
            Console.WriteLine("[CAMERA][CMD] Focus : " + command.Focus);
            Console.WriteLine("[CAMERA][CMD] Source MsgId : " + command.SourceMsgId);

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
            Console.WriteLine("[CAMERA][CMD] Handle End");
        }

        #endregion

        #region [PTZ Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// 현재 [ADS1000] 제어 서비스는 연속 제어 함수가 구현되어 있으므로,
        /// [continuous] 모드부터 실제 장비 제어와 연결한다.
        /// 
        /// [absolute] / [relative] 모드는 이후 위치 제어 [Packet] 구현 후 연결한다.
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
                HandleContinuousMove(command);
                return;
            }

            if (command.Mode.Equals(
                "absolute",
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[CAMERA][CMD] Absolute Move Received");
                Console.WriteLine("[CAMERA][CMD] Absolute Move is not connected yet");
                return;
            }

            if (command.Mode.Equals(
                "relative",
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[CAMERA][CMD] Relative Move Received");
                Console.WriteLine("[CAMERA][CMD] Relative Move is not connected yet");
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
    }

}
