using System;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [PTZ Absolute] Controller
    /// 
    /// Pan / Tilt 절대 위치 이동 명령을 생성하고 송신한다.
    /// 화면 Binding 상태 변경은 수행하지 않는다.
    /// </summary>
    internal sealed class PtzAbsoluteController
    {
        #region [Service Fields]

        /// <summary>
        /// [ADS1000] Camera 제어 서비스
        /// </summary>
        private readonly Ads1000CameraControlService _cameraControlService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [PTZ Absolute] Controller 생성
        /// </summary>
        internal PtzAbsoluteController(
            Ads1000CameraControlService cameraControlService)
        {
            _cameraControlService =
                cameraControlService;
        }

        #endregion

        #region [Absolute Move Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동
        /// </summary>
        internal PtzControllerResult MovePanAbsolute(
            double? targetPan,
            double currentPan,
            double currentPanCommandAngle,
            double panUiZeroOffset,
            Ads1000PanTurnMode panTurnMode)
        {
            if (!targetPan.HasValue)
            {
                return PtzControllerResult.Failed(
                    "Pan Absolute Failed : Value is empty");
            }

            double target =
                Clamp(
                    RoundAngleToProtocolScale(
                        targetPan.Value),
                    0,
                    360);

            double moveAngle =
                CalculatePanMoveAngle(
                    currentPan,
                    target,
                    panTurnMode);

            double commandTarget =
                currentPanCommandAngle +
                moveAngle;

            _cameraControlService
                .MovePanAbsolute(
                    commandTarget);

            return PtzControllerResult.Success(
                "Pan Absolute Move Sent");
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동
        /// </summary>
        internal PtzControllerResult MoveTiltAbsolute(
            double? targetTilt,
            double tiltUiZeroOffset)
        {
            if (!targetTilt.HasValue)
            {
                return PtzControllerResult.Failed(
                    "Tilt Absolute Failed : Value is empty");
            }

            double target =
                Clamp(
                    RoundAngleToProtocolScale(
                        targetTilt.Value + tiltUiZeroOffset),
                    -90,
                    90);

            _cameraControlService
                .MoveTiltAbsolute(
                    target);

            return PtzControllerResult.Success(
                "Tilt Absolute Move Sent");
        }

        #endregion

        #region [Calculation Methods]

        private double CalculatePanMoveAngle(
            double currentPan,
            double targetPan,
            Ads1000PanTurnMode panTurnMode)
        {
            if (panTurnMode == Ads1000PanTurnMode.ViaZero)
            {
                return CalculateViaZeroPanDelta(
                    currentPan,
                    targetPan);
            }

            return CalculateShortestPanDelta(
                currentPan,
                targetPan);
        }

        private double CalculateShortestPanDelta(
            double currentPan,
            double targetPan)
        {
            double delta =
                targetPan - currentPan;

            if (delta > 180)
            {
                delta -= 360;
            }

            if (delta < -180)
            {
                delta += 360;
            }

            return delta;
        }

        private double CalculateViaZeroPanDelta(
            double currentPan,
            double targetPan)
        {
            if (targetPan >= currentPan)
            {
                return targetPan - currentPan;
            }

            return (360 - currentPan) + targetPan;
        }

        private double RoundAngleToProtocolScale(
            double angle)
        {
            return Math.Round(
                angle,
                2,
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
