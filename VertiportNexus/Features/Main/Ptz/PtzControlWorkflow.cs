using System;
using System.Threading.Tasks;
using System.Windows.Input;
using VertiportNexus.Common;
using VertiportNexus.Services.Command;
using VertiportNexus.ViewModels.Main;
using VertiportNexus.ViewModels.Main.Composition;

namespace VertiportNexus.Features.Main.Ptz
{
    /// <summary>
    /// [PTZ Control] Workflow
    /// 
    /// [MainViewModel]에서 직접 수행하던
    /// Pan / Tilt / Zoom / Focus / Keyboard / Mode 제어 명령 호출 흐름을 담당한다.
    /// 
    /// 화면 Binding 값과 UI 상태 반영은 [MainViewModel]에 남기고,
    /// 실제 Controller / Service 호출과 이동 상태 관리는 본 Workflow에서 수행한다.
    /// </summary>
    internal sealed class PtzControlWorkflow
    {
        #region [Enum Type]

        /// <summary>
        /// [Pan / Tilt] 연속 이동 방향
        /// </summary>
        private enum PanTiltContinuousMoveDirection
        {
            /// <summary>
            /// 이동 없음
            /// </summary>
            None,

            /// <summary>
            /// [Pan] 왼쪽 이동
            /// </summary>
            PanLeft,

            /// <summary>
            /// [Pan] 오른쪽 이동
            /// </summary>
            PanRight,

            /// <summary>
            /// [Tilt] 위쪽 이동
            /// </summary>
            TiltUp,

            /// <summary>
            /// [Tilt] 아래쪽 이동
            /// </summary>
            TiltDown
        }

        /// <summary>
        /// [Pan / Tilt] 이동 축
        /// </summary>
        private enum PanTiltMoveAxis
        {
            /// <summary>
            /// 이동 없음
            /// </summary>
            None,

            /// <summary>
            /// [Pan] 축 이동
            /// </summary>
            Pan,

            /// <summary>
            /// [Tilt] 축 이동
            /// </summary>
            Tilt
        }

        /// <summary>
        /// [Pan / Tilt] 이동 제어 유형
        /// </summary>
        private enum PanTiltMoveType
        {
            /// <summary>
            /// 이동 없음
            /// </summary>
            None,

            /// <summary>
            /// 절대 위치 이동
            /// </summary>
            Absolute,

            /// <summary>
            /// 상대 위치 이동
            /// </summary>
            Relative,

            /// <summary>
            /// 연속 이동
            /// </summary>
            Continuous
        }

        #endregion

        #region [Fields]

        /// <summary>
        /// [MainViewModel] 구성 객체
        /// </summary>
        private readonly MainViewModelContext _context;

        /// <summary>
        /// 현재 [Pan / Tilt] 이동 축
        /// </summary>
        private PanTiltMoveAxis _currentPanTiltMoveAxis =
            PanTiltMoveAxis.None;

        /// <summary>
        /// 현재 [Pan / Tilt] 이동 제어 유형
        /// </summary>
        private PanTiltMoveType _currentPanTiltMoveType =
            PanTiltMoveType.None;

        /// <summary>
        /// [UI] 연속 이동 제어 진행 여부
        /// </summary>
        private bool _isUiContinuousMoveStarted;

        /// <summary>
        /// 현재 [Pan] 연속 이동 진행 여부
        /// </summary>
        private bool _isPanContinuousMoving;

        /// <summary>
        /// 현재 [Tilt] 연속 이동 진행 여부
        /// </summary>
        private bool _isTiltContinuousMoving;

        /// <summary>
        /// 현재 [Pan] 연속 이동 방향
        /// </summary>
        private PanTiltContinuousMoveDirection _currentPanContinuousMoveDirection =
            PanTiltContinuousMoveDirection.None;

        /// <summary>
        /// 현재 [Tilt] 연속 이동 방향
        /// </summary>
        private PanTiltContinuousMoveDirection _currentTiltContinuousMoveDirection =
            PanTiltContinuousMoveDirection.None;

        /// <summary>
        /// [Keyboard] Pan Left 입력 상태
        /// </summary>
        private bool _isKeyboardPanLeftPressed;

        /// <summary>
        /// [Keyboard] Pan Right 입력 상태
        /// </summary>
        private bool _isKeyboardPanRightPressed;

        /// <summary>
        /// [Keyboard] Tilt Up 입력 상태
        /// </summary>
        private bool _isKeyboardTiltUpPressed;

        /// <summary>
        /// [Keyboard] Tilt Down 입력 상태
        /// </summary>
        private bool _isKeyboardTiltDownPressed;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [PTZ Control] Workflow 생성자
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        internal PtzControlWorkflow(
            MainViewModelContext context)
        {
            _context =
                context;
        }

        #endregion

        #region [Keyboard Methods]

        /// <summary>
        /// [Keyboard] 방향키 입력 처리
        /// </summary>
        /// <param name="key">
        /// 입력된 키
        /// </param>
        /// <param name="isMoveAvailable">
        /// 이동 가능 여부
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult HandlePanTiltKeyDown(
            Key key,
            bool isMoveAvailable)
        {
            _context.KeyboardPtzController
                .HandleKeyDown();

            switch (key)
            {
                case Key.Left:
                    _isKeyboardPanLeftPressed =
                        true;
                    break;

                case Key.Right:
                    _isKeyboardPanRightPressed =
                        true;
                    break;

                case Key.Up:
                    _isKeyboardTiltUpPressed =
                        true;
                    break;

                case Key.Down:
                    _isKeyboardTiltDownPressed =
                        true;
                    break;

                default:
                    return PtzControlWorkflowResult.Ignored(
                        string.Empty);
            }

            return UpdateKeyboardPanTiltMove(
                isMoveAvailable);
        }

        /// <summary>
        /// [Keyboard] 방향키 해제 처리
        /// </summary>
        /// <param name="key">
        /// 해제된 키
        /// </param>
        /// <param name="isMoveAvailable">
        /// 이동 가능 여부
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult HandlePanTiltKeyUp(
            Key key,
            bool isMoveAvailable)
        {
            _context.KeyboardPtzController
                .HandleKeyUp();

            switch (key)
            {
                case Key.Left:
                    _isKeyboardPanLeftPressed =
                        false;
                    StopPanMove();
                    return UpdateKeyboardTiltMove(
                        isMoveAvailable);

                case Key.Right:
                    _isKeyboardPanRightPressed =
                        false;
                    StopPanMove();
                    return UpdateKeyboardTiltMove(
                        isMoveAvailable);

                case Key.Up:
                    _isKeyboardTiltUpPressed =
                        false;
                    StopTiltMove();
                    return UpdateKeyboardPanMove(
                        isMoveAvailable);

                case Key.Down:
                    _isKeyboardTiltDownPressed =
                        false;
                    StopTiltMove();
                    return UpdateKeyboardPanMove(
                        isMoveAvailable);

                default:
                    return PtzControlWorkflowResult.Ignored(
                        string.Empty);
            }

        }

        /// <summary>
        /// [Keyboard] Pan / Tilt 이동 상태 갱신
        /// </summary>
        /// <param name="isMoveAvailable">
        /// 이동 가능 여부
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        private PtzControlWorkflowResult UpdateKeyboardPanTiltMove(
            bool isMoveAvailable)
        {
            if (!isMoveAvailable)
            {
                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            PtzControlWorkflowResult panResult =
                UpdateKeyboardPanMove(
                    isMoveAvailable);

            PtzControlWorkflowResult tiltResult =
                UpdateKeyboardTiltMove(
                    isMoveAvailable);

            if (!string.IsNullOrWhiteSpace(tiltResult.Message))
            {
                return tiltResult;
            }

            return panResult;
        }

        /// <summary>
        /// [Keyboard] Pan 이동 상태 갱신
        /// </summary>
        /// <param name="isMoveAvailable">
        /// 이동 가능 여부
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        private PtzControlWorkflowResult UpdateKeyboardPanMove(
            bool isMoveAvailable)
        {
            if (!isMoveAvailable)
            {
                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            if (_isKeyboardPanLeftPressed &&
                !_isKeyboardPanRightPressed)
            {
                return StartPanLeftMove();
            }

            if (_isKeyboardPanRightPressed &&
                !_isKeyboardPanLeftPressed)
            {
                return StartPanRightMove();
            }

            return PtzControlWorkflowResult.Ignored(
                string.Empty);
        }

        /// <summary>
        /// [Keyboard] Tilt 이동 상태 갱신
        /// </summary>
        /// <param name="isMoveAvailable">
        /// 이동 가능 여부
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        private PtzControlWorkflowResult UpdateKeyboardTiltMove(
            bool isMoveAvailable)
        {
            if (!isMoveAvailable)
            {
                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            if (_isKeyboardTiltUpPressed &&
                !_isKeyboardTiltDownPressed)
            {
                return StartTiltUpMove();
            }

            if (_isKeyboardTiltDownPressed &&
                !_isKeyboardTiltUpPressed)
            {
                return StartTiltDownMove();
            }

            return PtzControlWorkflowResult.Ignored(
                string.Empty);
        }

        #endregion

        #region [Pan / Tilt Move Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동 명령 송신
        /// </summary>
        /// <param name="panCommandTarget">
        /// 장비 기준 Pan Target
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult MovePanAbsolute(
            double panCommandTarget)
        {
            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _isUiContinuousMoveStarted =
                false;

            _context.Ads1000CameraControlService
                .MovePanAbsolute(
                    panCommandTarget);

            return PtzControlWorkflowResult.Success(
                "PAN ABSOLUTE MOVE");
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동 명령 송신
        /// </summary>
        /// <param name="deviceTargetTilt">
        /// 장비 기준 Tilt Target
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult MoveTiltAbsolute(
            double deviceTargetTilt)
        {
            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _isUiContinuousMoveStarted =
                false;

            _context.Ads1000CameraControlService
                .MoveTiltAbsolute(
                    deviceTargetTilt);

            return PtzControlWorkflowResult.Success(
                "TILT ABSOLUTE MOVE");
        }

        /// <summary>
        /// [Pan] 왼쪽 연속 이동 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartPanLeftMove()
        {
            PtzControllerResult result =
                _context.PtzContinuousController
                    .StartPanLeftMove();

            _isPanContinuousMoving =
                result.IsMoving == true;

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanLeft;

            return PtzControlWorkflowResult.FromPtzControllerResult(
                result);
        }

        /// <summary>
        /// [Pan] 오른쪽 연속 이동 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartPanRightMove()
        {
            PtzControllerResult result =
                _context.PtzContinuousController
                    .StartPanRightMove();

            _isPanContinuousMoving =
                result.IsMoving == true;

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanRight;

            return PtzControlWorkflowResult.FromPtzControllerResult(
                result);
        }

        /// <summary>
        /// [Tilt] 위쪽 연속 이동 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartTiltUpMove()
        {
            PtzControllerResult result =
                _context.PtzContinuousController
                    .StartTiltUpMove();

            _isTiltContinuousMoving =
                result.IsMoving == true;

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltUp;

            return PtzControlWorkflowResult.FromPtzControllerResult(
                result);
        }

        /// <summary>
        /// [Tilt] 아래쪽 연속 이동 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartTiltDownMove()
        {
            PtzControllerResult result =
                _context.PtzContinuousController
                    .StartTiltDownMove();

            _isTiltContinuousMoving =
                result.IsMoving == true;

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltDown;

            return PtzControlWorkflowResult.FromPtzControllerResult(
                result);
        }

        /// <summary>
        /// [Pan / Tilt] 연속 이동 속도 재적용
        /// </summary>
        /// <param name="panTiltSpeedLevel">
        /// Pan / Tilt 속도값
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult ApplyCurrentPanTiltContinuousMoveSpeed(
            double panTiltSpeedLevel)
        {
            if (!_isUiContinuousMoveStarted)
            {
                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            if (!_isPanContinuousMoving &&
                !_isTiltContinuousMoving)
            {
                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            Console.WriteLine(
                "[UI][PTZ] Pan / Tilt Continuous Speed Changed : "
                + panTiltSpeedLevel.ToString("F0"));

            switch (_currentPanContinuousMoveDirection)
            {
                case PanTiltContinuousMoveDirection.PanLeft:
                    _context.Ads1000CameraControlService
                        .PanLeft();
                    break;

                case PanTiltContinuousMoveDirection.PanRight:
                    _context.Ads1000CameraControlService
                        .PanRight();
                    break;
            }

            switch (_currentTiltContinuousMoveDirection)
            {
                case PanTiltContinuousMoveDirection.TiltUp:
                    _context.Ads1000CameraControlService
                        .TiltUp();
                    break;

                case PanTiltContinuousMoveDirection.TiltDown:
                    _context.Ads1000CameraControlService
                        .TiltDown();
                    break;
            }

            return PtzControlWorkflowResult.Success(
                "PAN / TILT SPEED UPDATED");
        }

        /// <summary>
        /// [Pan / Tilt] 이동 속도 재적용
        /// </summary>
        /// <param name="isHomePositionMoving">
        /// Home Position 이동 진행 여부
        /// </param>
        /// <param name="panTiltSpeedLevel">
        /// Pan / Tilt 속도값
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult ApplyCurrentPanTiltMoveSpeed(
            bool isHomePositionMoving,
            double panTiltSpeedLevel)
        {
            if (isHomePositionMoving)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan / Tilt Speed Apply Ignored : Home Position Moving");

                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            if (_currentPanTiltMoveType == PanTiltMoveType.Continuous)
            {
                return ApplyCurrentPanTiltContinuousMoveSpeed(
                    panTiltSpeedLevel);
            }

            if (_currentPanTiltMoveAxis == PanTiltMoveAxis.None ||
                _currentPanTiltMoveType == PanTiltMoveType.None)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan / Tilt Speed Apply Ignored : Pan / Tilt Move State None");

                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            bool includeBeginCommand =
                _currentPanTiltMoveType == PanTiltMoveType.Absolute;

            Console.WriteLine(
                "[UI][PTZ] Pan / Tilt Speed Apply : "
                + panTiltSpeedLevel.ToString("F0")
                + " / "
                + _currentPanTiltMoveAxis
                + " / "
                + _currentPanTiltMoveType
                + " / BG="
                + includeBeginCommand);

            switch (_currentPanTiltMoveAxis)
            {
                case PanTiltMoveAxis.Pan:
                    _context.Ads1000CameraControlService
                        .UpdatePanMoveSpeed(
                            includeBeginCommand);
                    break;

                case PanTiltMoveAxis.Tilt:
                    _context.Ads1000CameraControlService
                        .UpdateTiltMoveSpeed(
                            includeBeginCommand);
                    break;
            }

            return PtzControlWorkflowResult.Success(
                "PAN / TILT SPEED UPDATED");
        }

        /// <summary>
        /// [Pan] 연속 이동 정지
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        private PtzControlWorkflowResult StopPanMove()
        {
            PtzControllerResult result =
                _context.PtzContinuousController
                    .StopPanMove();

            _isPanContinuousMoving =
                false;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            ClearMoveStateIfStopped();

            return PtzControlWorkflowResult.FromPtzControllerResult(
                result);
        }

        /// <summary>
        /// [Tilt] 연속 이동 정지
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        private PtzControlWorkflowResult StopTiltMove()
        {
            PtzControllerResult result =
                _context.PtzContinuousController
                    .StopTiltMove();

            _isTiltContinuousMoving =
                false;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            ClearMoveStateIfStopped();

            return PtzControlWorkflowResult.FromPtzControllerResult(
                result);
        }

        /// <summary>
        /// [UI] 장비 이동 정지
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StopContinuousMove()
        {
            PtzControllerResult result =
                _context.PtzContinuousController
                    .StopContinuousMove();

            _isPanContinuousMoving =
                false;

            _isTiltContinuousMoving =
                false;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _isUiContinuousMoveStarted =
                false;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            return PtzControlWorkflowResult.FromPtzControllerResult(
                result);
        }

        /// <summary>
        /// 이동 상태 초기화 가능 여부 확인 후 초기화
        /// </summary>
        private void ClearMoveStateIfStopped()
        {
            if (!_isPanContinuousMoving &&
                !_isTiltContinuousMoving)
            {
                _isUiContinuousMoveStarted =
                    false;

                _currentPanTiltMoveAxis =
                    PanTiltMoveAxis.None;

                _currentPanTiltMoveType =
                    PanTiltMoveType.None;
            }

        }

        #endregion

        #region [Zoom / Focus Methods]

        /// <summary>
        /// [Zoom] 확대 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartZoomInMove()
        {
            return PtzControlWorkflowResult.FromPtzControllerResult(
                _context.PtzContinuousController
                    .StartZoomInMove());
        }

        /// <summary>
        /// [Zoom] 축소 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartZoomOutMove()
        {
            return PtzControlWorkflowResult.FromPtzControllerResult(
                _context.PtzContinuousController
                    .StartZoomOutMove());
        }

        /// <summary>
        /// [Focus] Near 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartFocusNearMove()
        {
            return PtzControlWorkflowResult.FromPtzControllerResult(
                _context.PtzContinuousController
                    .StartFocusNearMove());
        }

        /// <summary>
        /// [Focus] Far 시작
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult StartFocusFarMove()
        {
            return PtzControlWorkflowResult.FromPtzControllerResult(
                _context.PtzContinuousController
                    .StartFocusFarMove());
        }

        /// <summary>
        /// [Auto Focus] 실행
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult AutoFocus()
        {
            _context.Ads1000CameraControlService
                .AutoFocus();

            return PtzControlWorkflowResult.Success(
                "AUTO FOCUS");
        }

        /// <summary>
        /// [Zoom] 지정 위치 이동
        /// </summary>
        /// <param name="zoomPositionValue">
        /// Zoom 위치 입력값
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult SetZoomPosition(
            int? zoomPositionValue)
        {
            return PtzControlWorkflowResult.FromPtzControllerResult(
                _context.ZoomFocusPositionController
                    .SetZoomPosition(
                        zoomPositionValue));
        }

        /// <summary>
        /// [Zoom] 배율 기준 위치 이동
        /// </summary>
        /// <param name="zoomRatioValue">
        /// Zoom 배율 입력값
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult SetZoomRatio(
            double? zoomRatioValue)
        {
            return PtzControlWorkflowResult.FromPtzControllerResult(
                _context.ZoomFocusPositionController
                    .SetZoomRatio(
                        zoomRatioValue));
        }

        /// <summary>
        /// [Focus] 지정 위치 이동
        /// </summary>
        /// <param name="focusPositionValue">
        /// Focus 위치 입력값
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult SetFocusPosition(
            int? focusPositionValue)
        {
            return PtzControlWorkflowResult.FromPtzControllerResult(
                _context.ZoomFocusPositionController
                    .SetFocusPosition(
                        focusPositionValue));
        }

        #endregion

        #region [Home / Zero Methods]

        /// <summary>
        /// [Home Position] 이동
        /// 
        /// Home Position 이동 Lock 처리 / Controller 호출 / 완료 대기 흐름을 수행한다.
        /// UI Binding 값 반영은 [MainViewModel]에서 결과값을 기준으로 처리한다.
        /// </summary>
        /// <param name="logPrefix">
        /// 로그 출력 구분 문자열
        /// </param>
        /// <param name="isHomePositionMoving">
        /// Home Position 이동 진행 여부
        /// </param>
        /// <param name="isDeviceFullyConnected">
        /// [MCB] / [SCB] 전체 연결 여부
        /// </param>
        /// <param name="isDeviceFullyConnectedProvider">
        /// [MCB] / [SCB] 전체 연결 여부 조회 함수
        /// </param>
        /// <param name="currentPanProvider">
        /// 현재 [Pan] 값 조회 함수
        /// </param>
        /// <param name="currentTiltProvider">
        /// 현재 [Tilt] 값 조회 함수
        /// </param>
        /// <param name="homePositionMovingStateSetter">
        /// Home Position 이동 상태 반영 함수
        /// </param>
        /// <param name="mainStatusSetter">
        /// 화면 상태 문자열 반영 함수
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal async Task<PtzControlWorkflowResult> MoveHomePositionAsync(
            string logPrefix,
            bool isHomePositionMoving,
            bool isDeviceFullyConnected,
            Func<bool> isDeviceFullyConnectedProvider,
            Func<double> currentPanProvider,
            Func<double> currentTiltProvider,
            Action<bool> homePositionMovingStateSetter,
            Action<string> mainStatusSetter)
        {
            if (isHomePositionMoving)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix + " Ignored : Home Position Moving");

                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            if (!isDeviceFullyConnected)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix + " Skipped : Device Not Fully Connected");

                return PtzControlWorkflowResult.Ignored(
                    string.Empty);
            }

            try
            {
                homePositionMovingStateSetter(
                    true);

                mainStatusSetter(
                    "HOME POSITION MOVING...");

                // [Home Position] 이동 명령 송신
                //
                // Controller는 장비 내부 Home Script 실행 명령만 송신한다.
                // 실제 이동 완료 여부는 상태 안정화 대기 로직에서 판단한다.
                PtzControllerResult result =
                    await _context.PtzHomeZeroController
                        .MoveHomePositionAsync();

                if (result != null &&
                    !result.IsSuccess)
                {
                    return PtzControlWorkflowResult.FromPtzControllerResult(
                        result);
                }

                bool isCompleted =
                    await WaitHomePositionCompletedAsync(
                        isDeviceFullyConnectedProvider,
                        currentPanProvider,
                        currentTiltProvider);

                if (!isCompleted)
                {
                    return PtzControlWorkflowResult.Ignored(
                        "HOME POSITION WAIT TIMEOUT");
                }

                double currentPan =
                    RoundAngleToProtocolScale(
                        CameraCommandService.NormalizePanStatus(
                            currentPanProvider()));

                double currentTilt =
                    RoundAngleToProtocolScale(
                        currentTiltProvider());

                mainStatusSetter(
                    "HOME POSITION STATUS SYNC...");

                // [UI] 표시 반영 대기
                //
                // CURRENT STATUS가 [0.00] 기준으로 갱신된 뒤
                // 버튼 Lock이 해제되도록 짧게 대기한다.
                await Task.Delay(
                    150);

                Console.WriteLine(
                    "[UI][PTZ] Home UI Zero Pan Offset : "
                    + currentPan.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Home UI Zero Tilt Offset : "
                    + currentTilt.ToString("F2"));

                return PtzControlWorkflowResult.HomePositionCompleted(
                    "HOME POSITION COMPLETE",
                    currentPan,
                    currentTilt);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix + " Failed : " + ex.Message);

                return PtzControlWorkflowResult.Ignored(
                    "HOME POSITION FAILED");
            }
            finally
            {
                homePositionMovingStateSetter(
                    false);
            }

        }

        /// <summary>
        /// [Pan] 현재 위치를 UI / 장비 Script 기준 [0] 위치로 저장
        /// </summary>
        /// <param name="currentPan">
        /// 현재 [Pan] 값
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult SetPanZero(
            double currentPan)
        {
            double normalizedPan =
                RoundAngleToProtocolScale(
                    CameraCommandService.NormalizePanStatus(
                        currentPan));

            int offsetValue =
                Convert.ToInt32(
                    Math.Round(
                        normalizedPan * 100.0,
                        MidpointRounding.AwayFromZero));

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Offset Request");

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Current : "
                + normalizedPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Offset Value : "
                + offsetValue);

            PtzControllerResult result =
                _context.PtzHomeZeroController
                    .SetPanZero(
                        normalizedPan);

            if (!result.IsSuccess)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Zero Failed : "
                    + result.Message);

                ConsoleLogHelper.PrintLine();

                return PtzControlWorkflowResult.FromPtzControllerResult(
                    result);
            }

            ConsoleLogHelper.PrintLine();

            return PtzControlWorkflowResult.PanZeroCompleted(
                result.Message,
                normalizedPan);
        }

        /// <summary>
        /// [Tilt] 현재 위치를 UI / 장비 Script 기준 [0] 위치로 저장
        /// </summary>
        /// <param name="currentTilt">
        /// 현재 [Tilt] 값
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult SetTiltZero(
            double currentTilt)
        {
            double normalizedTilt =
                RoundAngleToProtocolScale(
                    currentTilt);

            int offsetValue =
                Convert.ToInt32(
                    Math.Round(
                        normalizedTilt * 100.0,
                        MidpointRounding.AwayFromZero));

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Offset Request");

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Current : "
                + normalizedTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Offset Value : "
                + offsetValue);

            PtzControllerResult result =
                _context.PtzHomeZeroController
                    .SetTiltZero(
                        normalizedTilt);

            if (!result.IsSuccess)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Zero Failed : "
                    + result.Message);

                ConsoleLogHelper.PrintLine();

                return PtzControlWorkflowResult.FromPtzControllerResult(
                    result);
            }

            ConsoleLogHelper.PrintLine();

            return PtzControlWorkflowResult.TiltZeroCompleted(
                result.Message,
                normalizedTilt);
        }

        /// <summary>
        /// [Home Position] 이동 완료 대기
        /// 
        /// Home Position 명령 송신 후,
        /// Pan / Tilt 상태값이 특정 좌표 [0]에 도달했는지가 아니라
        /// 일정 시간 동안 위치 변화가 거의 없는지 확인하여
        /// 이동 완료 여부를 판단한다.
        /// </summary>
        /// <param name="isDeviceFullyConnectedProvider">
        /// [MCB] / [SCB] 전체 연결 여부 조회 함수
        /// </param>
        /// <param name="currentPanProvider">
        /// 현재 [Pan] 값 조회 함수
        /// </param>
        /// <param name="currentTiltProvider">
        /// 현재 [Tilt] 값 조회 함수
        /// </param>
        /// <returns>
        /// Home Position 완료 여부
        /// </returns>
        private async Task<bool> WaitHomePositionCompletedAsync(
            Func<bool> isDeviceFullyConnectedProvider,
            Func<double> currentPanProvider,
            Func<double> currentTiltProvider)
        {
            const int MIN_WAIT_MILLISECONDS =
                1500;

            const int CHECK_INTERVAL_MILLISECONDS =
                200;

            const int TIMEOUT_MILLISECONDS =
                300000;

            const int REQUIRED_STABLE_COUNT =
                5;

            const double PAN_STABLE_TOLERANCE_DEGREES =
                0.2;

            const double TILT_STABLE_TOLERANCE_DEGREES =
                0.2;

            await Task.Delay(
                MIN_WAIT_MILLISECONDS);

            int stableCount =
                0;

            int elapsedMilliseconds =
                MIN_WAIT_MILLISECONDS;

            double previousPan =
                CameraCommandService.NormalizePanStatus(
                    currentPanProvider());

            double previousTilt =
                currentTiltProvider();

            while (elapsedMilliseconds < TIMEOUT_MILLISECONDS)
            {
                if (!isDeviceFullyConnectedProvider())
                {
                    ConsoleLogHelper.PrintBlock(
                        "[DEVICE] Home Position Wait Canceled : Device Disconnected");

                    return false;
                }

                double currentPan =
                    CameraCommandService.NormalizePanStatus(
                        currentPanProvider());

                double currentTilt =
                    currentTiltProvider();

                double panDelta =
                    Math.Abs(
                        CalculateShortestPanDelta(
                            previousPan,
                            currentPan));

                double tiltDelta =
                    Math.Abs(
                        currentTilt - previousTilt);

                bool isNearHome =
                    IsNearZeroAngle(
                        currentPan,
                        PAN_STABLE_TOLERANCE_DEGREES) &&
                    Math.Abs(
                        currentTilt) <= TILT_STABLE_TOLERANCE_DEGREES;

                bool isStable =
                    isNearHome &&
                    panDelta <= PAN_STABLE_TOLERANCE_DEGREES &&
                    tiltDelta <= TILT_STABLE_TOLERANCE_DEGREES;

                if (isStable)
                {
                    stableCount++;

                    Console.WriteLine(
                        "[DEVICE] Home Position Motion Stable Check : "
                        + stableCount
                        + " / "
                        + REQUIRED_STABLE_COUNT
                        + " Pan="
                        + currentPan.ToString("F2")
                        + ", Tilt="
                        + currentTilt.ToString("F2"));

                    if (stableCount >= REQUIRED_STABLE_COUNT)
                    {
                        return true;
                    }

                }
                else
                {
                    if (stableCount > 0)
                    {
                        Console.WriteLine(
                            "[DEVICE] Home Position Stable Count Reset");
                    }

                    stableCount =
                        0;

                    Console.WriteLine(
                        "[DEVICE] Home Position Wait Check : "
                        + "Pan="
                        + currentPan.ToString("F2")
                        + ", Tilt="
                        + currentTilt.ToString("F2")
                        + ", PanDelta="
                        + panDelta.ToString("F2")
                        + ", TiltDelta="
                        + tiltDelta.ToString("F2")
                        + ", NearHome="
                        + isNearHome);
                }

                previousPan =
                    currentPan;

                previousTilt =
                    currentTilt;

                await Task.Delay(
                    CHECK_INTERVAL_MILLISECONDS);

                elapsedMilliseconds +=
                    CHECK_INTERVAL_MILLISECONDS;
            }

            ConsoleLogHelper.PrintBlock(
                "[DEVICE] Home Position Wait Timeout");

            return false;
        }

        /// <summary>
        /// [각도] Home 기준 근접 여부 확인
        /// </summary>
        /// <param name="angle">
        /// 확인할 각도값
        /// </param>
        /// <param name="tolerance">
        /// 허용 오차
        /// </param>
        /// <returns>
        /// Home 기준 근접 여부
        /// </returns>
        private bool IsNearZeroAngle(
            double angle,
            double tolerance)
        {
            double normalizedAngle =
                CameraCommandService.NormalizePanStatus(
                    angle);

            return normalizedAngle <= tolerance ||
                   normalizedAngle >= 360.0 - tolerance;
        }

        /// <summary>
        /// [Pan] 표시 각도 기준 최단 변화량 계산
        /// </summary>
        /// <param name="previousPan">
        /// 이전 Pan 표시 각도값
        /// </param>
        /// <param name="currentPan">
        /// 현재 Pan 표시 각도값
        /// </param>
        /// <returns>
        /// Pan 최단 변화량
        /// </returns>
        private double CalculateShortestPanDelta(
            double previousPan,
            double currentPan)
        {
            double delta =
                currentPan - previousPan;

            if (delta > 180.0)
            {
                delta -=
                    360.0;
            }

            if (delta < -180.0)
            {
                delta +=
                    360.0;
            }

            return delta;
        }

        /// <summary>
        /// [Pan / Tilt] 각도값 소수점 둘째 자리 보정
        /// </summary>
        /// <param name="angle">
        /// 각도값
        /// </param>
        /// <returns>
        /// 소수점 둘째 자리로 반올림된 각도값
        /// </returns>
        private double RoundAngleToProtocolScale(
            double angle)
        {
            return Math.Round(
                angle,
                2,
                MidpointRounding.AwayFromZero);
        }

        #endregion

        #region [Mode Methods]

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult SetAutoMode()
        {
            ControllerResult result =
                _context.PtzModeController
                    .SetAutoMode();

            return PtzControlWorkflowResult.ModeChanged(
                result.Message,
                "AUTO");
        }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정
        /// </summary>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal PtzControlWorkflowResult SetManualMode()
        {
            ControllerResult result =
                _context.PtzModeController
                    .SetManualMode();

            return PtzControlWorkflowResult.ModeChanged(
                result.Message,
                "MANUAL");
        }
        #endregion
    }

}
