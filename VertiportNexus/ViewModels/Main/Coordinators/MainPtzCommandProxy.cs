using System;
using System.Threading.Tasks;
using System.Windows.Input;
using VertiportNexus.Features.Main.Ptz;
using VertiportNexus.ViewModels.Main.Panels;
using VertiportNexus.ViewModels.Main.States;

namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [Main PTZ Command] Proxy
    ///
    /// MainViewModel의 Command / View Event 진입점을 대신 받아
    /// [MainPtzControlCoordinator] 실행 후 결과 반영까지 처리한다.
    /// </summary>
    internal sealed class MainPtzCommandProxy
    {
        #region [Fields]

        /// <summary>
        /// [PTZ Control] 실행 Coordinator
        /// </summary>
        private readonly MainPtzControlCoordinator _coordinator;

        /// <summary>
        /// [PTZ Control] 결과 반영 객체
        /// </summary>
        private readonly MainPtzControlResultApplier _resultApplier;

        /// <summary>
        /// Camera 화면 / 입력 상태
        /// </summary>
        private readonly MainCameraPanelViewModel _cameraPanel;

        /// <summary>
        /// Camera 내부 상태
        /// </summary>
        private readonly MainCameraState _cameraState;

        /// <summary>
        /// 연결 상태 Panel
        /// </summary>
        private readonly MainConnectionPanelViewModel _connectionPanel;

        /// <summary>
        /// 장비 전체 연결 여부 조회 함수
        /// </summary>
        private readonly Func<bool> _isDeviceFullyConnected;

        /// <summary>
        /// [Keyboard] Pan / Tilt 키보드 이동 진행 여부
        /// 
        /// 방향키 입력으로 실제 Pan / Tilt 이동 명령을 전달한 경우에만 true로 설정한다.
        /// Home Position 이동 중 KeyUp 이벤트가 들어와도
        /// 불필요한 Stop 명령이 송신되지 않도록 구분하기 위해 사용한다.
        /// </summary>
        private bool _isPanTiltKeyboardMoveActive;

        /// <summary>
        /// [Home Position] 이동 상태 변경 처리 함수
        /// </summary>
        private readonly Action<bool> _homePositionMovingStateChanged;

        /// <summary>
        /// Main 상태 문자열 변경 처리 함수
        /// </summary>
        private readonly Action<string> _mainStatusChanged;

        /// <summary>
        /// Binding 갱신 처리 함수
        /// </summary>
        private readonly Action<string> _propertyChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main PTZ Command] Proxy 생성자
        /// </summary>
        internal MainPtzCommandProxy(
            MainPtzControlCoordinator coordinator,
            MainPtzControlResultApplier resultApplier,
            MainCameraPanelViewModel cameraPanel,
            MainCameraState cameraState,
            MainConnectionPanelViewModel connectionPanel,
            Func<bool> isDeviceFullyConnected,
            Action<bool> homePositionMovingStateChanged,
            Action<string> mainStatusChanged,
            Action<string> propertyChanged)
        {
            _coordinator =
                coordinator;

            _resultApplier =
                resultApplier;

            _cameraPanel =
                cameraPanel;

            _cameraState =
                cameraState;

            _connectionPanel =
                connectionPanel;

            _isDeviceFullyConnected =
                isDeviceFullyConnected;

            _homePositionMovingStateChanged =
                homePositionMovingStateChanged;

            _mainStatusChanged =
                mainStatusChanged;

            _propertyChanged =
                propertyChanged;
        }

        #endregion

        #region [Keyboard Methods]

        /// <summary>
        /// [Keyboard] 방향키 입력 처리
        /// 
        /// Home Position 이동 중이거나 Pan / Tilt 수동 제어가 불가능한 상태에서는
        /// 키보드 Pan / Tilt 이동 명령을 전달하지 않는다.
        /// 
        /// 실제 KeyDown 이동 명령을 전달한 경우에만
        /// KeyUp에서 Stop 명령을 처리할 수 있도록 상태를 저장한다.
        /// </summary>
        /// <param name="key">
        /// 입력된 키
        /// </param>
        internal void HandlePanTiltKeyDown(
            Key key)
        {
            if (!IsPanTiltKeyboardControlKey(
                    key))
            {
                return;
            }

            bool isMoveAvailable =
                IsPtzKeyboardMoveAvailable();

            if (!isMoveAvailable)
            {
                _isPanTiltKeyboardMoveActive =
                    false;

                return;
            }

            _isPanTiltKeyboardMoveActive =
                true;

            Apply(
                _coordinator
                    .HandlePanTiltKeyDown(
                        key,
                        isMoveAvailable));
        }

        /// <summary>
        /// [Keyboard] 방향키 해제 처리
        /// 
        /// 방향키로 실제 Pan / Tilt 이동이 시작된 경우에만
        /// KeyUp Stop 명령을 전달한다.
        /// 
        /// Home Position 이동 중 발생한 KeyUp은
        /// Home Position 명령을 중단시킬 수 있으므로 Stop 명령을 송신하지 않는다.
        /// </summary>
        /// <param name="key">
        /// 해제된 키
        /// </param>
        internal void HandlePanTiltKeyUp(
            Key key)
        {
            if (!IsPanTiltKeyboardControlKey(
                    key))
            {
                return;
            }

            if (!_isPanTiltKeyboardMoveActive)
            {
                return;
            }

            _isPanTiltKeyboardMoveActive =
                false;

            bool isMoveAvailable =
                IsPtzKeyboardMoveAvailable();

            if (!isMoveAvailable)
            {
                return;
            }

            Apply(
                _coordinator
                    .HandlePanTiltKeyUp(
                        key,
                        isMoveAvailable));
        }

        /// <summary>
        /// [Keyboard] Pan / Tilt 제어 키 여부 조회
        /// </summary>
        /// <param name="key">
        /// 입력된 키
        /// </param>
        /// <returns>
        /// Pan / Tilt 제어 키 여부
        /// </returns>
        private bool IsPanTiltKeyboardControlKey(
            Key key)
        {
            return key == Key.Left ||
                   key == Key.Right ||
                   key == Key.Up ||
                   key == Key.Down;
        }

        /// <summary>
        /// Keyboard PTZ 이동 가능 여부 조회
        /// </summary>
        private bool IsPtzKeyboardMoveAvailable()
        {
            return _connectionPanel.McbConnectionState == MainViewModel.ConnectionState.Connected &&
                   !_connectionPanel.IsHomePositionMoving;
        }

        #endregion

        #region [Position Input Methods]

        /// <summary>
        /// 위치 제어 입력값 초기화
        /// </summary>
        internal void ResetPositionInput()
        {
            _cameraPanel
                .ResetPositionInput();

            NotifyCameraInputPropertiesChanged();
        }

        #endregion

        #region [Absolute / Relative Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동 요청
        /// </summary>
        internal void MovePanAbsolute()
        {
            Apply(
                _coordinator
                    .MovePanAbsolute(
                        _cameraPanel.PanAbsoluteValue));
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동 요청
        /// </summary>
        internal void MoveTiltAbsolute()
        {
            Apply(
                _coordinator
                    .MoveTiltAbsolute(
                        _cameraPanel.TiltAbsoluteValue));
        }

        /// <summary>
        /// [Pan] 상대 위치 이동 요청
        /// </summary>
        internal void MovePanRelative()
        {
            Apply(
                _coordinator
                    .MovePanRelative(
                        _cameraPanel.PanRelativeValue));
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동 요청
        /// </summary>
        internal void MoveTiltRelative()
        {
            Apply(
                _coordinator
                    .MoveTiltRelative(
                        _cameraPanel.TiltRelativeValue));
        }

        #endregion

        #region [Continuous / Lens Methods]

        /// <summary>
        /// [Pan / Tilt] 현재 이동 속도 재적용
        /// </summary>
        internal void ApplyCurrentPanTiltMoveSpeed(
            double panTiltSpeedLevel)
        {
            _coordinator
                .ApplyCurrentPanTiltMoveSpeed(
                    _connectionPanel.IsHomePositionMoving,
                    panTiltSpeedLevel);
        }

        internal void StartPanLeftMove() => Apply(_coordinator.StartPanLeftMove());

        internal void StartPanRightMove() => Apply(_coordinator.StartPanRightMove());

        internal void StartTiltUpMove() => Apply(_coordinator.StartTiltUpMove());

        internal void StartTiltDownMove() => Apply(_coordinator.StartTiltDownMove());

        internal void StartZoomInMove() => Apply(_coordinator.StartZoomInMove());

        internal void StartZoomOutMove() => Apply(_coordinator.StartZoomOutMove());

        internal void StartFocusNearMove() => Apply(_coordinator.StartFocusNearMove());

        internal void StartFocusFarMove() => Apply(_coordinator.StartFocusFarMove());

        internal void AutoFocus() => Apply(_coordinator.AutoFocus());

        internal void StopContinuousMove() => Apply(_coordinator.StopContinuousMove());

        #endregion

        #region [Home / Zero / Mode Methods]

        /// <summary>
        /// [Home Position] 이동 요청
        /// </summary>
        internal async Task MoveHomePositionAsync()
        {
            PtzControlWorkflowResult result =
                await _coordinator
                    .MoveHomePositionAsync(
                        "[UI][PTZ] Home Position",
                        _connectionPanel.IsHomePositionMoving,
                        _isDeviceFullyConnected(),
                        _isDeviceFullyConnected,
                        () => _cameraPanel.CurrentPan,
                        () => _cameraPanel.CurrentTilt,
                        _homePositionMovingStateChanged,
                        _mainStatusChanged);

            Apply(
                result);
        }

        internal void SetPanZero() => Apply(_coordinator.SetPanZero(_cameraPanel.CurrentPan));

        internal void SetTiltZero() => Apply(_coordinator.SetTiltZero(_cameraPanel.CurrentTilt));

        internal void SetAutoMode() => Apply(_coordinator.SetAutoMode());

        internal void SetManualMode() => Apply(_coordinator.SetManualMode());

        #endregion

        #region [Zoom / Focus Position Methods]

        internal void SetZoomPosition() => Apply(_coordinator.SetZoomPosition(_cameraPanel.ZoomPositionValue));

        internal void SetZoomRatio() => Apply(_coordinator.SetZoomRatio(_cameraPanel.ZoomRatioValue));

        internal void SetFocusPosition() => Apply(_coordinator.SetFocusPosition(_cameraPanel.FocusPositionValue));

        #endregion

        #region [Diagonal Methods]

        internal void StartPanLeftTiltUpMove()
        {
            StartPanLeftMove();
            StartTiltUpMove();
        }

        internal void StartPanRightTiltUpMove()
        {
            StartPanRightMove();
            StartTiltUpMove();
        }

        internal void StartPanLeftTiltDownMove()
        {
            StartPanLeftMove();
            StartTiltDownMove();
        }

        internal void StartPanRightTiltDownMove()
        {
            StartPanRightMove();
            StartTiltDownMove();
        }

        #endregion

        #region [Apply / Notify Methods]

        /// <summary>
        /// [PTZ Control] 결과 반영
        /// </summary>
        private void Apply(
            PtzControlWorkflowResult result)
        {
            _resultApplier
                .Apply(
                    result);
        }

        /// <summary>
        /// Camera 입력 Binding 갱신
        /// </summary>
        private void NotifyCameraInputPropertiesChanged()
        {
            _propertyChanged(nameof(MainViewModel.PanAbsoluteValue));
            _propertyChanged(nameof(MainViewModel.TiltAbsoluteValue));
            _propertyChanged(nameof(MainViewModel.PanRelativeValue));
            _propertyChanged(nameof(MainViewModel.TiltRelativeValue));
            _propertyChanged(nameof(MainViewModel.ZoomPositionValue));
            _propertyChanged(nameof(MainViewModel.ZoomRatioValue));
            _propertyChanged(nameof(MainViewModel.FocusPositionValue));
        }
        #endregion
    }

}
