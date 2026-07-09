using System;
using System.Threading.Tasks;
using System.Windows.Input;
using VertiportNexus.Features.Main.Ptz;
using VertiportNexus.Services.Command;
using VertiportNexus.ViewModels.Main.States;

namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [Main PTZ Control] 실행 Coordinator
    ///
    /// Pan / Tilt / Zoom / Focus 제어 계산과
    /// [PtzControlWorkflow] 호출 흐름을 담당한다.
    /// </summary>
    internal sealed class MainPtzControlCoordinator
    {
        #region [Constants]

        /// <summary>
        /// 위치 비교 허용 오차
        /// </summary>
        private const double POSITION_EPSILON = 0.001;

        #endregion

        #region [Fields]

        /// <summary>
        /// [PTZ Control] Workflow
        /// </summary>
        private readonly PtzControlWorkflow _ptzControlWorkflow;

        /// <summary>
        /// Camera 위치 / 입력 / UI Zero 상태
        /// </summary>
        private readonly MainCameraState _cameraState;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main PTZ Control] Coordinator 생성자
        /// </summary>
        /// <param name="ptzControlWorkflow">
        /// [PTZ Control] Workflow
        /// </param>
        /// <param name="cameraState">
        /// Camera 상태 객체
        /// </param>
        internal MainPtzControlCoordinator(
            PtzControlWorkflow ptzControlWorkflow,
            MainCameraState cameraState)
        {
            _ptzControlWorkflow =
                ptzControlWorkflow;

            _cameraState =
                cameraState;
        }

        #endregion

        #region [Keyboard Methods]

        /// <summary>
        /// [Keyboard] 방향키 입력 처리
        /// </summary>
        internal PtzControlWorkflowResult HandlePanTiltKeyDown(
            Key key,
            bool isMoveAvailable)
        {
            return _ptzControlWorkflow
                .HandlePanTiltKeyDown(
                    key,
                    isMoveAvailable);
        }

        /// <summary>
        /// [Keyboard] 방향키 해제 처리
        /// </summary>
        internal PtzControlWorkflowResult HandlePanTiltKeyUp(
            Key key,
            bool isMoveAvailable)
        {
            return _ptzControlWorkflow
                .HandlePanTiltKeyUp(
                    key,
                    isMoveAvailable);
        }

        #endregion

        #region [Absolute Control Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동
        /// </summary>
        /// <param name="panAbsoluteValue">
        /// UI 기준 [Pan] 절대 위치 입력값
        /// </param>
        /// <returns>
        /// [PTZ Control] Workflow 결과
        /// </returns>
        internal PtzControlWorkflowResult MovePanAbsolute(
            double? panAbsoluteValue)
        {
            if (!panAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Failed : Value is empty");

                return PtzControlWorkflowResult.Ignored(string.Empty);
            }

            double currentPanCommandAngle =
                _cameraState.GetCurrentPanCommandAngle(
                    _cameraState.CurrentPan);

            double currentPan =
                _cameraState.GetUiCurrentPan();

            double inputPan =
                RoundAngleToProtocolScale(
                    panAbsoluteValue.Value);

            double targetPan =
                CameraCommandService.Clamp(
                    inputPan,
                    0,
                    360);

            // [Pan] 360도 입력 보정
            //
            // Absolute Position에서 [360]은 [0]과 동일한 위치로 취급한다.
            // 기존처럼 [360 - 현재 Pan]을 강제로 더하면,
            // 현재 위치와 무관하게 장비가 오른쪽으로 추가 회전할 수 있다.
            if (Math.Abs(targetPan - 360.0) <= POSITION_EPSILON)
            {
                targetPan =
                    0.0;
            }

            double panMoveAngle =
                CameraCommandService.CalculatePanMoveAngle(
                    currentPan,
                    targetPan,
                    _cameraState.PanTurnMode);

            if (Math.Abs(panMoveAngle) <= POSITION_EPSILON)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Ignored : Already Target Position");

                return PtzControlWorkflowResult.Ignored(string.Empty);
            }

            double panCommandTarget =
                currentPanCommandAngle + panMoveAngle;

            return _ptzControlWorkflow
                .MovePanAbsolute(
                    panCommandTarget);
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동
        /// </summary>
        /// <param name="tiltAbsoluteValue">
        /// UI 기준 [Tilt] 절대 위치 입력값
        /// </param>
        /// <returns>
        /// [PTZ Control] Workflow 결과
        /// </returns>
        internal PtzControlWorkflowResult MoveTiltAbsolute(
            double? tiltAbsoluteValue)
        {
            if (!tiltAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Failed : Value is empty");

                return PtzControlWorkflowResult.Ignored(string.Empty);
            }

            double currentTilt =
                _cameraState.GetUiCurrentTilt();

            double inputTilt =
                RoundAngleToProtocolScale(
                    tiltAbsoluteValue.Value);

            double targetTilt =
                CameraCommandService.Clamp(
                    inputTilt,
                    -90,
                    90);

            double deviceTargetTilt =
                _cameraState.ConvertUiTiltTargetToDeviceTarget(
                    targetTilt);

            double tiltMoveAngle =
                targetTilt - currentTilt;

            if (Math.Abs(tiltMoveAngle) <= POSITION_EPSILON)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Ignored : Already Target Position");

                return PtzControlWorkflowResult.Ignored(string.Empty);
            }

            return _ptzControlWorkflow
                .MoveTiltAbsolute(
                    deviceTargetTilt);
        }

        #endregion

        #region [Relative Control Methods]

        /// <summary>
        /// [Pan] 상대 위치 이동
        /// </summary>
        /// <param name="panRelativeValue">
        /// UI 기준 [Pan] 상대 이동 입력값
        /// </param>
        /// <returns>
        /// [PTZ Control] Workflow 결과
        /// </returns>
        internal PtzControlWorkflowResult MovePanRelative(
            double? panRelativeValue)
        {
            if (!panRelativeValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Relative Failed : Value is empty");

                return PtzControlWorkflowResult.Ignored(string.Empty);
            }

            double currentPan =
                _cameraState.GetUiCurrentPan();

            double movePan =
                RoundAngleToProtocolScale(
                    panRelativeValue.Value);

            double targetPan =
                CameraCommandService.NormalizePanStatus(
                    currentPan + movePan);

            double panMoveAngle =
                CameraCommandService.CalculatePanMoveAngle(
                    currentPan,
                    targetPan,
                    _cameraState.PanTurnMode);

            double panCommandTarget =
                _cameraState.GetCurrentPanCommandAngle(
                    _cameraState.CurrentPan)
                + panMoveAngle;

            return _ptzControlWorkflow
                .MovePanAbsolute(
                    panCommandTarget);
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동
        /// </summary>
        /// <param name="tiltRelativeValue">
        /// UI 기준 [Tilt] 상대 이동 입력값
        /// </param>
        /// <returns>
        /// [PTZ Control] Workflow 결과
        /// </returns>
        internal PtzControlWorkflowResult MoveTiltRelative(
            double? tiltRelativeValue)
        {
            if (!tiltRelativeValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Relative Failed : Value is empty");

                return PtzControlWorkflowResult.Ignored(string.Empty);
            }

            double currentTilt =
                _cameraState.GetUiCurrentTilt();

            double moveTilt =
                RoundAngleToProtocolScale(
                    tiltRelativeValue.Value);

            double targetTilt =
                CameraCommandService.Clamp(
                    currentTilt + moveTilt,
                    -90,
                    90);

            double deviceTargetTilt =
                _cameraState.ConvertUiTiltTargetToDeviceTarget(
                    targetTilt);

            return _ptzControlWorkflow
                .MoveTiltAbsolute(
                    deviceTargetTilt);
        }

        #endregion

        #region [Continuous Control Methods]

        /// <summary>
        /// [Pan / Tilt] 이동 속도 재적용
        /// </summary>
        /// <param name="isHomePositionMoving">
        /// [Home Position] 이동 진행 여부
        /// </param>
        /// <param name="panTiltSpeedLevel">
        /// Pan / Tilt 이동 속도값
        /// </param>
        internal void ApplyCurrentPanTiltMoveSpeed(
            bool isHomePositionMoving,
            double panTiltSpeedLevel)
        {
            _ptzControlWorkflow
                .ApplyCurrentPanTiltMoveSpeed(
                    isHomePositionMoving,
                    panTiltSpeedLevel);
        }

        /// <summary>
        /// [Pan] 왼쪽 연속 이동 시작
        /// </summary>
        internal PtzControlWorkflowResult StartPanLeftMove()
        {
            return _ptzControlWorkflow
                .StartPanLeftMove();
        }

        /// <summary>
        /// [Pan] 오른쪽 연속 이동 시작
        /// </summary>
        internal PtzControlWorkflowResult StartPanRightMove()
        {
            return _ptzControlWorkflow
                .StartPanRightMove();
        }

        /// <summary>
        /// [Tilt] 위쪽 연속 이동 시작
        /// </summary>
        internal PtzControlWorkflowResult StartTiltUpMove()
        {
            return _ptzControlWorkflow
                .StartTiltUpMove();
        }

        /// <summary>
        /// [Tilt] 아래쪽 연속 이동 시작
        /// </summary>
        internal PtzControlWorkflowResult StartTiltDownMove()
        {
            return _ptzControlWorkflow
                .StartTiltDownMove();
        }

        /// <summary>
        /// [Zoom] 확대 시작
        /// </summary>
        internal PtzControlWorkflowResult StartZoomInMove()
        {
            return _ptzControlWorkflow
                .StartZoomInMove();
        }

        /// <summary>
        /// [Zoom] 축소 시작
        /// </summary>
        internal PtzControlWorkflowResult StartZoomOutMove()
        {
            return _ptzControlWorkflow
                .StartZoomOutMove();
        }

        /// <summary>
        /// [Focus] Near 시작
        /// </summary>
        internal PtzControlWorkflowResult StartFocusNearMove()
        {
            return _ptzControlWorkflow
                .StartFocusNearMove();
        }

        /// <summary>
        /// [Focus] Far 시작
        /// </summary>
        internal PtzControlWorkflowResult StartFocusFarMove()
        {
            return _ptzControlWorkflow
                .StartFocusFarMove();
        }

        /// <summary>
        /// [Auto Focus] 실행
        /// </summary>
        internal PtzControlWorkflowResult AutoFocus()
        {
            return _ptzControlWorkflow
                .AutoFocus();
        }

        /// <summary>
        /// [Pan / Tilt / Zoom / Focus] 이동 정지
        /// </summary>
        internal PtzControlWorkflowResult StopContinuousMove()
        {
            return _ptzControlWorkflow
                .StopContinuousMove();
        }

        #endregion

        #region [Home / Zero Methods]

        /// <summary>
        /// [Home Position] 이동
        /// </summary>
        internal async Task<PtzControlWorkflowResult> MoveHomePositionAsync(
            string logPrefix,
            bool isHomePositionMoving,
            bool isDeviceFullyConnectedProperty,
            Func<bool> isDeviceFullyConnected,
            Func<double> currentPanProvider,
            Func<double> currentTiltProvider,
            Action<bool> homePositionMovingStateChanged,
            Action<string> mainStatusChanged)
        {
            return await _ptzControlWorkflow
                .MoveHomePositionAsync(
                    logPrefix,
                    isHomePositionMoving,
                    isDeviceFullyConnectedProperty,
                    isDeviceFullyConnected,
                    currentPanProvider,
                    currentTiltProvider,
                    homePositionMovingStateChanged,
                    mainStatusChanged);
        }

        /// <summary>
        /// [Pan] 현재 위치를 [0] 기준으로 저장
        /// </summary>
        internal PtzControlWorkflowResult SetPanZero(
            double currentPan)
        {
            return _ptzControlWorkflow
                .SetPanZero(
                    currentPan);
        }

        /// <summary>
        /// [Tilt] 현재 위치를 [0] 기준으로 저장
        /// </summary>
        internal PtzControlWorkflowResult SetTiltZero(
            double currentTilt)
        {
            return _ptzControlWorkflow
                .SetTiltZero(
                    currentTilt);
        }

        #endregion

        #region [Mode Methods]

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정
        /// </summary>
        internal PtzControlWorkflowResult SetAutoMode()
        {
            return _ptzControlWorkflow
                .SetAutoMode();
        }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정
        /// </summary>
        internal PtzControlWorkflowResult SetManualMode()
        {
            return _ptzControlWorkflow
                .SetManualMode();
        }

        #endregion

        #region [Zoom / Focus Position Methods]

        /// <summary>
        /// [Zoom] 지정 위치 이동
        /// </summary>
        internal PtzControlWorkflowResult SetZoomPosition(
            int? zoomPositionValue)
        {
            return _ptzControlWorkflow
                .SetZoomPosition(
                    zoomPositionValue);
        }

        /// <summary>
        /// [Zoom] 배율 기준 위치 이동
        /// </summary>
        internal PtzControlWorkflowResult SetZoomRatio(
            double? zoomRatioValue)
        {
            return _ptzControlWorkflow
                .SetZoomRatio(
                    zoomRatioValue);
        }

        /// <summary>
        /// [Focus] 지정 위치 이동
        /// </summary>
        internal PtzControlWorkflowResult SetFocusPosition(
            int? focusPositionValue)
        {
            return _ptzControlWorkflow
                .SetFocusPosition(
                    focusPositionValue);
        }

        /// <summary>
        /// [Zoom] 위치값을 배율로 변환
        /// </summary>
        internal double ConvertZoomPositionToRatio(
            double zoomPosition)
        {
            const double MIN_ZOOM_POSITION =
                0.0;

            const double MAX_ZOOM_POSITION =
                1000.0;

            const double MIN_ZOOM_RATIO =
                1.0;

            const double MAX_ZOOM_RATIO =
                66.0;

            double clampedZoomPosition =
                CameraCommandService.Clamp(
                    zoomPosition,
                    MIN_ZOOM_POSITION,
                    MAX_ZOOM_POSITION);

            double zoomRatio =
                MIN_ZOOM_RATIO
                + (clampedZoomPosition - MIN_ZOOM_POSITION)
                / (MAX_ZOOM_POSITION - MIN_ZOOM_POSITION)
                * (MAX_ZOOM_RATIO - MIN_ZOOM_RATIO);

            return Math.Round(
                zoomRatio,
                1);
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// 각도값을 프로토콜 기준 소수점 둘째 자리로 보정
        /// </summary>
        private static double RoundAngleToProtocolScale(
            double angle)
        {
            return Math.Round(
                angle,
                2,
                MidpointRounding.AwayFromZero);
        }
        #endregion
    }

}
