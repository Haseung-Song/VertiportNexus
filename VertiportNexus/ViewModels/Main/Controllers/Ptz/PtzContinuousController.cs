using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [PTZ Continuous] Controller
    /// 
    /// Pan / Tilt / Zoom / Focus 연속 이동과 정지 명령을 담당한다.
    /// </summary>
    internal sealed class PtzContinuousController
    {
        #region [Service Fields]

        private readonly Ads1000CameraControlService _cameraControlService;

        #endregion

        #region [Constructor]

        internal PtzContinuousController(
            Ads1000CameraControlService cameraControlService)
        {
            _cameraControlService =
                cameraControlService;
        }

        #endregion

        #region [Continuous Move Methods]

        internal PtzControllerResult StartPanLeftMove()
        {
            _cameraControlService.PanLeft();
            return PtzControllerResult.Success("Pan Left Move Started", isMoving: true);
        }

        internal PtzControllerResult StartPanRightMove()
        {
            _cameraControlService.PanRight();
            return PtzControllerResult.Success("Pan Right Move Started", isMoving: true);
        }

        internal PtzControllerResult StartTiltUpMove()
        {
            _cameraControlService.TiltUp();
            return PtzControllerResult.Success("Tilt Up Move Started", isMoving: true);
        }

        internal PtzControllerResult StartTiltDownMove()
        {
            _cameraControlService.TiltDown();
            return PtzControllerResult.Success("Tilt Down Move Started", isMoving: true);
        }

        internal PtzControllerResult StartZoomInMove()
        {
            _cameraControlService.ZoomIn();
            return PtzControllerResult.Success("Zoom In Move Started", isMoving: true);
        }

        internal PtzControllerResult StartZoomOutMove()
        {
            _cameraControlService.ZoomOut();
            return PtzControllerResult.Success("Zoom Out Move Started", isMoving: true);
        }

        internal PtzControllerResult StartFocusNearMove()
        {
            _cameraControlService.FocusNear();
            return PtzControllerResult.Success("Focus Near Move Started", isMoving: true);
        }

        internal PtzControllerResult StartFocusFarMove()
        {
            _cameraControlService.FocusFar();
            return PtzControllerResult.Success("Focus Far Move Started", isMoving: true);
        }

        internal PtzControllerResult AutoFocus()
        {
            _cameraControlService.AutoFocus();
            return PtzControllerResult.Success("Auto Focus Sent");
        }

        internal PtzControllerResult StopPanMove()
        {
            _cameraControlService.StopPanMove();
            return PtzControllerResult.Success("Pan Move Stopped", isMoving: false);
        }

        internal PtzControllerResult StopTiltMove()
        {
            _cameraControlService.StopTiltMove();
            return PtzControllerResult.Success("Tilt Move Stopped", isMoving: false);
        }

        internal PtzControllerResult StopContinuousMove()
        {
            _cameraControlService.StopMove();
            return PtzControllerResult.Success("Continuous Move Stopped", isMoving: false);
        }
        #endregion
    }

}
