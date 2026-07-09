using System;
using VertiportNexus.Features.Main.ADS1000;
using VertiportNexus.Services.Command;
using VertiportNexus.ViewModels.Main.States;

namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [ADS1000] 상태 적용 결과 반영 객체
    ///
    /// ADS1000 상태 Packet 처리 결과를 Camera 상태와 Binding Property에 반영한다.
    /// </summary>
    internal sealed class MainAds1000StatusResultApplier
    {
        #region [Fields]

        /// <summary>
        /// Camera 위치 / 입력 / UI Zero 상태
        /// </summary>
        private readonly MainCameraState _cameraState;

        /// <summary>
        /// Zoom 위치값을 배율값으로 변환하는 함수
        /// </summary>
        private readonly Func<double, double> _zoomRatioConverter;

        /// <summary>
        /// Binding Property 갱신 알림 처리기
        /// </summary>
        private readonly Action<string> _notifyPropertyChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [ADS1000] 상태 적용 결과 반영 객체 생성자
        /// </summary>
        internal MainAds1000StatusResultApplier(
            MainCameraState cameraState,
            Func<double, double> zoomRatioConverter,
            Action<string> notifyPropertyChanged)
        {
            _cameraState =
                cameraState;

            _zoomRatioConverter =
                zoomRatioConverter;

            _notifyPropertyChanged =
                notifyPropertyChanged;
        }

        #endregion

        #region [Apply Methods]

        /// <summary>
        /// [ADS1000] 상태 적용 결과 반영
        /// </summary>
        internal void Apply(
            Ads1000StatusApplyControllerResult result)
        {
            if (result == null ||
                !result.IsSuccess)
            {
                return;
            }

            ApplyPanStatus(
                result);

            ApplyTiltStatus(
                result);

            ApplyZoomStatus(
                result);

            ApplyFocusStatus(
                result);
        }

        /// <summary>
        /// [Pan] 상태값 반영
        /// </summary>
        private void ApplyPanStatus(
            Ads1000StatusApplyControllerResult result)
        {
            if (!result.CurrentPan.HasValue)
            {
                return;
            }

            double normalizedPan =
                CameraCommandService.NormalizePanStatus(
                    result.CurrentPan.Value);

            if (_cameraState.SetCurrentPan(normalizedPan))
            {
                Notify(
                    "CurrentPan");

                Notify(
                    "CurrentPanDisplayText");
            }

            _cameraState
                .UpdatePanAccumulatedStatus(
                    result.CurrentPan.Value);
        }

        /// <summary>
        /// [Tilt] 상태값 반영
        /// </summary>
        private void ApplyTiltStatus(
            Ads1000StatusApplyControllerResult result)
        {
            if (!result.CurrentTilt.HasValue)
            {
                return;
            }

            double normalizedTilt =
                Ads1000StatusWorkflow
                    .NormalizeTiltStatus(
                        result.CurrentTilt.Value);

            if (_cameraState.SetCurrentTilt(normalizedTilt))
            {
                Notify(
                    "CurrentTilt");

                Notify(
                    "CurrentTiltDisplayText");
            }

        }

        /// <summary>
        /// [Zoom] 상태값 반영
        /// </summary>
        private void ApplyZoomStatus(
            Ads1000StatusApplyControllerResult result)
        {
            if (!result.CurrentZoom.HasValue)
            {
                return;
            }

            double normalizedZoom =
                Ads1000StatusWorkflow
                    .NormalizeRangePosition(
                        result.CurrentZoom.Value,
                        0,
                        1000);

            bool isZoomChanged =
                _cameraState.SetCurrentZoom(
                    normalizedZoom);

            double zoomRatio =
                _zoomRatioConverter(
                    normalizedZoom);

            bool isZoomRatioChanged =
                _cameraState.SetCurrentZoomRatio(
                    zoomRatio);

            if (isZoomChanged)
            {
                Notify(
                    "CurrentZoom");
            }

            if (isZoomRatioChanged)
            {
                Notify(
                    "CurrentZoomRatio");
            }

            if (isZoomChanged ||
                isZoomRatioChanged)
            {
                Notify(
                    "CurrentZoomDisplayText");
            }

        }

        /// <summary>
        /// [Focus] 상태값 반영
        /// </summary>
        private void ApplyFocusStatus(
            Ads1000StatusApplyControllerResult result)
        {
            if (!result.CurrentFocus.HasValue)
            {
                return;
            }

            double normalizedFocus =
                Ads1000StatusWorkflow
                    .NormalizeRangePosition(
                        result.CurrentFocus.Value,
                        0,
                        1000);

            if (_cameraState.SetCurrentFocus(normalizedFocus))
            {
                Notify(
                    "CurrentFocus");
            }

        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// Binding Property 갱신 알림
        /// </summary>
        private void Notify(
            string propertyName)
        {
            _notifyPropertyChanged?.Invoke(
                propertyName);
        }
        #endregion
    }

}
