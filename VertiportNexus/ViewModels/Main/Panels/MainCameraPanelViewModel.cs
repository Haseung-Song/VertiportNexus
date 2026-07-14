using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.Command;
using VertiportNexus.ViewModels.Main.Composition;
using VertiportNexus.ViewModels.Main.States;

namespace VertiportNexus.ViewModels.Main.Panels
{
    /// <summary>
    /// [Main Camera Panel] 화면 표시 / 입력 상태
    ///
    /// Camera 위치 표시값, 위치 제어 입력값,
    /// Pan 선회 모드와 Pan / Tilt 속도 설정 처리를 담당한다.
    /// </summary>
    internal sealed class MainCameraPanelViewModel
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

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main Camera Panel] ViewModel 생성자
        /// </summary>
        internal MainCameraPanelViewModel(
            MainViewModelContext context,
            MainCameraState cameraState)
        {
            _context =
                context;

            _cameraState =
                cameraState;
        }

        #endregion

        #region [Current Camera Properties]

        /// <summary>
        /// 현재 [Pan] 위치값
        /// </summary>
        internal double CurrentPan =>
            _cameraState.CurrentPan;

        /// <summary>
        /// 현재 [Tilt] 위치값
        /// </summary>
        internal double CurrentTilt =>
            _cameraState.CurrentTilt;

        /// <summary>
        /// 현재 [Zoom] 위치값
        /// </summary>
        internal double CurrentZoom =>
            _cameraState.CurrentZoom;

        /// <summary>
        /// 현재 [Zoom] 배율값
        /// </summary>
        internal double CurrentZoomRatio =>
            _cameraState.CurrentZoomRatio;

        /// <summary>
        /// 현재 [Focus] 위치값
        /// </summary>
        internal double CurrentFocus =>
            _cameraState.CurrentFocus;

        /// <summary>
        /// UI Zero 기준 [Pan] 표시 문자열
        /// </summary>
        internal string CurrentPanDisplayText =>
            _cameraState
                .GetUiCurrentPan()
                .ToString("F2");

        /// <summary>
        /// UI Zero 기준 [Tilt] 표시 문자열
        /// </summary>
        internal string CurrentTiltDisplayText =>
            _cameraState
                .GetUiCurrentTilt()
                .ToString("F2");

        /// <summary>
        /// [Zoom] 위치 / 배율 표시 문자열
        /// </summary>
        internal string CurrentZoomDisplayText =>
            CurrentZoom.ToString("F0")
            + " (x"
            + CurrentZoomRatio.ToString("F1")
            + ")";

        /// <summary>
        /// [Pan] / [Tilt] 이동 속도값
        /// </summary>
        internal double PanTiltSpeedLevel =>
            _context.Ads1000CameraControlService.PanTiltSpeedLevel;

        #endregion

        #region [Input Properties]

        /// <summary>
        /// [Pan] 선회 모드
        /// </summary>
        internal Ads1000PanTurnMode PanTurnMode =>
            _cameraState.PanTurnMode;

        /// <summary>
        /// [Pan] 절대 위치 입력값
        /// </summary>
        internal double? PanAbsoluteValue =>
            _cameraState.PanAbsoluteValue;

        /// <summary>
        /// [Tilt] 절대 위치 입력값
        /// </summary>
        internal double? TiltAbsoluteValue =>
            _cameraState.TiltAbsoluteValue;

        /// <summary>
        /// [Pan] 상대 이동 입력값
        /// </summary>
        internal double? PanRelativeValue =>
            _cameraState.PanRelativeValue;

        /// <summary>
        /// [Tilt] 상대 이동 입력값
        /// </summary>
        internal double? TiltRelativeValue =>
            _cameraState.TiltRelativeValue;

        /// <summary>
        /// [Zoom] 위치 입력값
        /// </summary>
        internal int? ZoomPositionValue =>
            _cameraState.ZoomPositionValue;

        /// <summary>
        /// [Zoom] 배율 입력값
        /// </summary>
        internal double? ZoomRatioValue =>
            _cameraState.ZoomRatioValue;

        /// <summary>
        /// [Focus] 위치 입력값
        /// </summary>
        internal int? FocusPositionValue =>
            _cameraState.FocusPositionValue;

        #endregion

        #region [Current Camera Set Methods]

        /// <summary>
        /// 현재 [Pan] 위치값 저장
        /// </summary>
        internal bool SetCurrentPan(
            double value)
        {
            return _cameraState
                .SetCurrentPan(
                    value);
        }

        /// <summary>
        /// 현재 [Tilt] 위치값 저장
        /// </summary>
        internal bool SetCurrentTilt(
            double value)
        {
            return _cameraState
                .SetCurrentTilt(
                    value);
        }

        /// <summary>
        /// 현재 [Zoom] 위치값 저장
        /// </summary>
        internal bool SetCurrentZoom(
            double value)
        {
            return _cameraState
                .SetCurrentZoom(
                    value);
        }

        /// <summary>
        /// 현재 [Zoom] 배율값 저장
        /// </summary>
        internal bool SetCurrentZoomRatio(
            double value)
        {
            return _cameraState
                .SetCurrentZoomRatio(
                    value);
        }

        /// <summary>
        /// 현재 [Focus] 위치값 저장
        /// </summary>
        internal bool SetCurrentFocus(
            double value)
        {
            return _cameraState
                .SetCurrentFocus(
                    value);
        }

        #endregion

        #region [Input Set Methods]

        /// <summary>
        /// [Pan] 선회 모드 저장 및 공유 상태 갱신
        /// </summary>
        internal void SetPanTurnMode(
            Ads1000PanTurnMode panTurnMode)
        {
            _cameraState.PanTurnMode =
                panTurnMode;

            _context.CameraStateProvider
                .UpdatePanTurnMode(
                    panTurnMode);
        }

        /// <summary>
        /// [Pan] / [Tilt] 이동 속도 저장
        /// </summary>
        internal bool SetPanTiltSpeedLevel(
            double value)
        {
            double clampedValue =
                CameraCommandService.Clamp(
                    value,
                    0,
                    50);

            if (_context.Ads1000CameraControlService.PanTiltSpeedLevel == clampedValue)
            {
                return false;
            }

            ConsoleLogHelper.WriteLine(
                "[UI][PTZ] Pan / Tilt Speed Value Changed : "
                + _context.Ads1000CameraControlService.PanTiltSpeedLevel.ToString("F0")
                + " -> "
                + clampedValue.ToString("F0"));

            ConsoleLogHelper.WriteLine();

            _context.Ads1000CameraControlService.PanTiltSpeedLevel =
                clampedValue;

            return true;
        }

        /// <summary>
        /// [Pan] 절대 위치 입력값 저장
        /// </summary>
        internal bool SetPanAbsoluteValue(
            double? value)
        {
            return _cameraState
                .SetPanAbsoluteValue(
                    value);
        }

        /// <summary>
        /// [Tilt] 절대 위치 입력값 저장
        /// </summary>
        internal bool SetTiltAbsoluteValue(
            double? value)
        {
            return _cameraState
                .SetTiltAbsoluteValue(
                    value);
        }

        /// <summary>
        /// [Pan] 상대 이동 입력값 저장
        /// </summary>
        internal bool SetPanRelativeValue(
            double? value)
        {
            return _cameraState
                .SetPanRelativeValue(
                    value);
        }

        /// <summary>
        /// [Tilt] 상대 이동 입력값 저장
        /// </summary>
        internal bool SetTiltRelativeValue(
            double? value)
        {
            return _cameraState
                .SetTiltRelativeValue(
                    value);
        }

        /// <summary>
        /// [Zoom] 위치 입력값 저장
        /// </summary>
        internal bool SetZoomPositionValue(
            int? value)
        {
            return _cameraState
                .SetZoomPositionValue(
                    value);
        }

        /// <summary>
        /// [Zoom] 배율 입력값 저장
        /// </summary>
        internal bool SetZoomRatioValue(
            double? value)
        {
            return _cameraState
                .SetZoomRatioValue(
                    value);
        }

        /// <summary>
        /// [Focus] 위치 입력값 저장
        /// </summary>
        internal bool SetFocusPositionValue(
            int? value)
        {
            return _cameraState
                .SetFocusPositionValue(
                    value);
        }

        /// <summary>
        /// 위치 제어 입력값 초기화
        /// </summary>
        internal void ResetPositionInput()
        {
            SetPanAbsoluteValue(0);
            SetTiltAbsoluteValue(0);
            SetPanRelativeValue(0);
            SetTiltRelativeValue(0);
            SetZoomPositionValue(0);
            SetZoomRatioValue(1);
            SetFocusPositionValue(0);

            ConsoleLogHelper.PrintLine();
            ConsoleLogHelper.WriteLine("[UI][POSITION] Input Reset");
            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}
