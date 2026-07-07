using System;
using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Zoom / Focus Position] Controller
    /// 
    /// Zoom / Focus 위치 이동 명령을 담당하고,
    /// 화면 Binding 상태 변경은 수행하지 않는다.
    /// </summary>
    internal sealed class ZoomFocusPositionController
    {
        #region [Constants]

        private const double MIN_ZOOM_RATIO = 1.0;
        private const double MAX_ZOOM_RATIO = 66.0;

        #endregion

        #region [Service Fields]

        private readonly Ads1000CameraControlService _cameraControlService;

        #endregion

        #region [Constructor]

        internal ZoomFocusPositionController(
            Ads1000CameraControlService cameraControlService)
        {
            _cameraControlService =
                cameraControlService;
        }

        #endregion

        #region [Zoom / Focus Methods]

        internal PtzControllerResult SetZoomPosition(
            int? zoomPosition)
        {
            if (!zoomPosition.HasValue)
            {
                return PtzControllerResult.Failed(
                    "Zoom Position Failed : Value is empty");
            }

            ushort zoom =
                (ushort)Clamp(
                    zoomPosition.Value,
                    0,
                    1000);

            _cameraControlService
                .MoveZoomPosition(
                    zoom);

            return PtzControllerResult.Success(
                "Zoom Position Move Sent");
        }

        internal PtzControllerResult SetZoomRatio(
            double? zoomRatio)
        {
            if (!zoomRatio.HasValue)
            {
                return PtzControllerResult.Failed(
                    "Zoom Ratio Failed : Value is empty");
            }

            ushort zoomPosition =
                ConvertZoomRatioToPosition(
                    zoomRatio.Value);

            _cameraControlService
                .MoveZoomPosition(
                    zoomPosition);

            return PtzControllerResult.Success(
                "Zoom Ratio Move Sent");
        }

        internal PtzControllerResult SetFocusPosition(
            int? focusPosition)
        {
            if (!focusPosition.HasValue)
            {
                return PtzControllerResult.Failed(
                    "Focus Position Failed : Value is empty");
            }

            ushort focus =
                (ushort)Clamp(
                    focusPosition.Value,
                    0,
                    1000);

            _cameraControlService
                .MoveFocusPosition(
                    focus);

            return PtzControllerResult.Success(
                "Focus Position Move Sent");
        }

        #endregion

        #region [Calculation Methods]

        private ushort ConvertZoomRatioToPosition(
            double zoomRatio)
        {
            double clampedRatio =
                Clamp(
                    zoomRatio,
                    MIN_ZOOM_RATIO,
                    MAX_ZOOM_RATIO);

            double normalized =
                (clampedRatio - MIN_ZOOM_RATIO) /
                (MAX_ZOOM_RATIO - MIN_ZOOM_RATIO);

            return (ushort)Math.Round(
                normalized * 1000,
                MidpointRounding.AwayFromZero);
        }

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
