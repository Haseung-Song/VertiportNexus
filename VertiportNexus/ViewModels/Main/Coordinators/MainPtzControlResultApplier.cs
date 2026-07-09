using System;
using VertiportNexus.Features.Main.Ptz;
using VertiportNexus.ViewModels.Main.Composition;
using VertiportNexus.ViewModels.Main.Panels;
using VertiportNexus.ViewModels.Main.States;

namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [PTZ Control] 처리 결과 반영 객체
    ///
    /// PTZ Workflow 결과를 화면 상태 / Camera 상태 / UI Zero 상태에 반영한다.
    /// </summary>
    internal sealed class MainPtzControlResultApplier
    {
        #region [Fields]

        /// <summary>
        /// [MainViewModel] 구성 객체
        /// </summary>
        private readonly MainViewModelContext _context;

        /// <summary>
        /// Camera 위치 / 입력 / UI Zero 상태
        /// </summary>
        private readonly MainCameraState _cameraState;

        /// <summary>
        /// Main 화면 상태 표시 객체
        /// </summary>
        private readonly MainStatusPanelViewModel _statusPanel;

        /// <summary>
        /// Binding Property 갱신 알림 처리기
        /// </summary>
        private readonly Action<string> _notifyPropertyChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [PTZ Control] 처리 결과 반영 객체 생성자
        /// </summary>
        internal MainPtzControlResultApplier(
            MainViewModelContext context,
            MainCameraState cameraState,
            MainStatusPanelViewModel statusPanel,
            Action<string> notifyPropertyChanged)
        {
            _context =
                context;

            _cameraState =
                cameraState;

            _statusPanel =
                statusPanel;

            _notifyPropertyChanged =
                notifyPropertyChanged;
        }

        #endregion

        #region [Mode Apply Methods]

        /// <summary>
        /// [PTZ] 제어 모드 반영
        /// </summary>
        internal void SetPtzControlMode(
            string ptzControlMode)
        {
            string normalizedMode =
                string.IsNullOrWhiteSpace(ptzControlMode)
                    ? "MANUAL"
                    : ptzControlMode.Trim().ToUpperInvariant();

            _context.CameraStateProvider
                .UpdatePtzControlMode(
                    normalizedMode);

            if (_statusPanel.SetPtzControlModeText(normalizedMode))
            {
                Notify(
                    "PtzControlModeText");

                Notify(
                    "IsManualPanTiltControlEnabled");
            }

        }

        #endregion

        #region [Result Apply Methods]

        /// <summary>
        /// [PTZ Control] Workflow 결과 반영
        /// </summary>
        internal void Apply(
            PtzControlWorkflowResult result)
        {
            if (result == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.PtzControlMode))
            {
                SetPtzControlMode(
                    result.PtzControlMode);
            }

            ApplyPanZeroResult(
                result);

            ApplyTiltZeroResult(
                result);

            if (!string.IsNullOrWhiteSpace(result.Message) &&
                _statusPanel.SetMainStatusText(result.Message))
            {
                Notify(
                    "MainStatusText");
            }

        }

        /// <summary>
        /// [Pan] UI Zero 결과 반영
        /// </summary>
        private void ApplyPanZeroResult(
            PtzControlWorkflowResult result)
        {
            if (!result.PanUiZeroOffset.HasValue)
            {
                return;
            }

            _cameraState.PanUiZeroOffset =
                result.PanUiZeroOffset.Value;

            if (_cameraState.SetPanAbsoluteValue(0))
            {
                Notify(
                    "PanAbsoluteValue");
            }

            if (_cameraState.SetPanRelativeValue(0))
            {
                Notify(
                    "PanRelativeValue");
            }

            if (result.ShouldResetPanAccumulatedStatus)
            {
                _cameraState
                    .ResetPanAccumulatedStatus();
            }

            Notify(
                "CurrentPan");

            Notify(
                "CurrentPanDisplayText");
        }

        /// <summary>
        /// [Tilt] UI Zero 결과 반영
        /// </summary>
        private void ApplyTiltZeroResult(
            PtzControlWorkflowResult result)
        {
            if (!result.TiltUiZeroOffset.HasValue)
            {
                return;
            }

            _cameraState.TiltUiZeroOffset =
                result.TiltUiZeroOffset.Value;

            if (_cameraState.SetTiltAbsoluteValue(0))
            {
                Notify(
                    "TiltAbsoluteValue");
            }

            if (_cameraState.SetTiltRelativeValue(0))
            {
                Notify(
                    "TiltRelativeValue");
            }

            Notify(
                "CurrentTilt");

            Notify(
                "CurrentTiltDisplayText");
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
