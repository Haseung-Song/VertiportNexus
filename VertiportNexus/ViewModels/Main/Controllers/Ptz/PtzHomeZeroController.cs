using System;
using System.Threading.Tasks;
using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [PTZ Home / Zero] Controller
    /// 
    /// Home Position 이동과 Pan / Tilt Zero Offset 저장 명령을 담당한다.
    /// 화면 Lock 상태 변경은 [MainViewModel]에서 수행한다.
    /// </summary>
    internal sealed class PtzHomeZeroController
    {
        #region [Service Fields]

        private readonly Ads1000CameraControlService _cameraControlService;

        #endregion

        #region [Constructor]

        internal PtzHomeZeroController(
            Ads1000CameraControlService cameraControlService)
        {
            _cameraControlService =
                cameraControlService;
        }

        #endregion

        #region [Home / Zero Methods]

        internal async Task<PtzControllerResult> MoveHomePositionAsync()
        {
            try
            {
                _cameraControlService
                    .MoveHomePosition();

                await Task.Delay(
                    1000);

                return PtzControllerResult.Success(
                    "Home Position Move Sent",
                    isMoving: false);
            }
            catch (Exception ex)
            {
                return PtzControllerResult.Failed(
                    "Home Position Failed : " + ex.Message);
            }
        }

        internal PtzControllerResult SetPanZero(
            double currentPan)
        {
            _cameraControlService
                .SetPanZero(
                    currentPan);

            return PtzControllerResult.Success(
                "Pan Zero Offset Saved",
                panUiZeroOffset: currentPan);
        }

        internal PtzControllerResult SetTiltZero(
            double currentTilt)
        {
            _cameraControlService
                .SetTiltZero(
                    currentTilt);

            return PtzControllerResult.Success(
                "Tilt Zero Offset Saved",
                tiltUiZeroOffset: currentTilt);
        }

        #endregion
    }
}
