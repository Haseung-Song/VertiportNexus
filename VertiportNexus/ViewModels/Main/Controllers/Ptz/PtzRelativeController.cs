using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [PTZ Relative] Controller
    /// </summary>
    internal sealed class PtzRelativeController
    {
        #region [Service Fields]

        private readonly Ads1000CameraControlService _cameraControlService;

        #endregion

        #region [Constructor]

        internal PtzRelativeController(
            Ads1000CameraControlService cameraControlService)
        {
            _cameraControlService =
                cameraControlService;
        }

        #endregion

        #region [Relative Move Methods]

        internal PtzControllerResult MovePanRelative(
            double? relativePan)
        {
            if (!relativePan.HasValue)
            {
                return PtzControllerResult.Failed(
                    "Pan Relative Failed : Value is empty");
            }

            _cameraControlService
                .MovePanRelative(
                    relativePan.Value);

            return PtzControllerResult.Success(
                "Pan Relative Move Sent");
        }

        internal PtzControllerResult MoveTiltRelative(
            double? relativeTilt)
        {
            if (!relativeTilt.HasValue)
            {
                return PtzControllerResult.Failed(
                    "Tilt Relative Failed : Value is empty");
            }

            _cameraControlService
                .MoveTiltRelative(
                    relativeTilt.Value);

            return PtzControllerResult.Success(
                "Tilt Relative Move Sent");
        }

        #endregion
    }
}
