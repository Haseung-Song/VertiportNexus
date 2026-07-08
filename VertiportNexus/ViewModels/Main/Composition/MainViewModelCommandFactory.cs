using System;
using System.Threading.Tasks;
using System.Windows.Input;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main.Composition
{
    /// <summary>
    /// [MainViewModel] Command 생성 처리
    /// 
    /// [MainViewModel] 생성자에서 직접 수행하던
    /// RelayCommand / AsyncRelayCommand 생성 책임을 분리한다.
    /// 
    /// 현재 단계에서는 Command 실행 메서드 자체는 [MainViewModel]에 유지하고,
    /// Command 객체 생성 및 연결만 본 클래스에서 처리한다.
    /// </summary>
    internal sealed class MainViewModelCommandFactory
    {
        #region [Create Methods]

        /// <summary>
        /// [MainViewModel] Command 목록 생성
        /// </summary>
        /// <param name="handlers">
        /// [MainViewModel] Command 처리기 목록
        /// </param>
        /// <returns>
        /// 생성된 [Command] 목록
        /// </returns>
        internal MainViewModelCommandSet Create(
            MainViewModelCommandHandlerSet handlers)
        {
            return new MainViewModelCommandSet
            {
                #region [Communication Commands]

                // [MQ] 연결 요청 [Command]
                StartMqReceiveCommand =
                    new RelayCommand(
                        handlers.StartMqReceive),

                // [MQ] 연결 해제 요청 [Command]
                StopMqReceiveCommand =
                    new RelayCommand(
                        handlers.StopMqReceive),

                // [TCP] 연결 요청 [Command]
                StartTcpReceiveCommand =
                    new AsyncRelayCommand(
                        handlers.ConnectDevicesAsync),

                // [TCP] 연결 해제 요청 [Command]
                StopTcpReceiveCommand =
                    new AsyncRelayCommand(
                        handlers.DisconnectDevicesAsync),

                // [Radar] UDP 수신 시작 요청 [Command]
                StartRadarUdpReceiveCommand =
                    new RelayCommand(
                        handlers.StartRadarUdpReceive),

                // [Radar] UDP 수신 중지 요청 [Command]
                StopRadarUdpReceiveCommand =
                    new RelayCommand(
                        handlers.StopRadarUdpReceive),

                #endregion

                #region [Pan / Tilt Commands]

                // [Pan] 좌측 이동 요청 [Command]
                PanLeftCommand =
                    new RelayCommand(
                        handlers.StartPanLeftMove),

                // [Pan] 우측 이동 요청 [Command]
                PanRightCommand =
                    new RelayCommand(
                        handlers.StartPanRightMove),

                // [Tilt] 상향 이동 요청 [Command]
                TiltUpCommand =
                    new RelayCommand(
                        handlers.StartTiltUpMove),

                // [Tilt] 하향 이동 요청 [Command]
                TiltDownCommand =
                    new RelayCommand(
                        handlers.StartTiltDownMove),

                // [Pan / Tilt] 정지 요청 [Command]
                StopMoveCommand =
                    new RelayCommand(
                        handlers.StopContinuousMove),

                // [Pan] Absolute 이동 요청 [Command]
                MovePanAbsoluteCommand =
                    new RelayCommand(
                        handlers.MovePanAbsolute),

                // [Tilt] Absolute 이동 요청 [Command]
                MoveTiltAbsoluteCommand =
                    new RelayCommand(
                        handlers.MoveTiltAbsolute),

                // [Pan] Relative 이동 요청 [Command]
                MovePanRelativeCommand =
                    new RelayCommand(
                        handlers.MovePanRelative),

                // [Tilt] Relative 이동 요청 [Command]
                MoveTiltRelativeCommand =
                    new RelayCommand(
                        handlers.MoveTiltRelative),

                // [Home Position] 이동 요청 [Command]
                MoveHomePositionCommand =
                    new AsyncRelayCommand(
                        handlers.MoveHomePositionAsync),

                // [Pan] 현재 위치 [0] 설정 요청 [Command]
                SetPanZeroCommand =
                    new RelayCommand(
                        handlers.SetPanZero),

                // [Tilt] 현재 위치 [0] 설정 요청 [Command]
                SetTiltZeroCommand =
                    new RelayCommand(
                        handlers.SetTiltZero),

                // 위치 제어 입력값 초기화 요청 [Command]
                ResetPositionInputCommand =
                    new RelayCommand(
                        handlers.ResetPositionInput),

                // [PTZ] [AUTO] 모드 설정 요청 [Command]
                SetPtzAutoModeCommand =
                    new RelayCommand(
                        handlers.SetPtzAutoMode),

                // [PTZ] [MANUAL] 모드 설정 요청 [Command]
                SetPtzManualModeCommand =
                    new RelayCommand(
                        handlers.SetPtzManualMode),

                #endregion

                #region [Zoom / Focus Commands]

                // [Zoom] 확대 요청 [Command]
                ZoomInCommand =
                    new RelayCommand(
                        handlers.StartZoomInMove),

                // [Zoom] 축소 요청 [Command]
                ZoomOutCommand =
                    new RelayCommand(
                        handlers.StartZoomOutMove),

                // [Focus] Near 요청 [Command]
                FocusNearCommand =
                    new RelayCommand(
                        handlers.StartFocusNearMove),

                // [Focus] Far 요청 [Command]
                FocusFarCommand =
                    new RelayCommand(
                        handlers.StartFocusFarMove),

                // [Auto Focus] 요청 [Command]
                AutoFocusCommand =
                    new RelayCommand(
                        handlers.AutoFocus),

                // [Zoom] 위치 이동 요청 [Command]
                SetZoomPositionCommand =
                    new RelayCommand(
                        handlers.SetZoomPosition),

                // [Zoom] 배율 이동 요청 [Command]
                SetZoomRatioCommand =
                    new RelayCommand(
                        handlers.SetZoomRatio),

                // [Focus] 위치 이동 요청 [Command]
                SetFocusPositionCommand =
                    new RelayCommand(
                        handlers.SetFocusPosition),

                #endregion

                #region [Test Commands]

                // [Dummy Tracking] 테스트 시작 요청 [Command]
                StartDummyTrackingTestCommand =
                    new AsyncRelayCommand(
                        handlers.StartDummyTrackingTestAsync),

                // [Dummy Tracking] 테스트 중지 요청 [Command]
                StopDummyTrackingTestCommand =
                    new RelayCommand(
                        handlers.StopDummyTrackingTest)

                #endregion
            };

        }
        #endregion
    }

    /// <summary>
    /// [MainViewModel] Command 처리기 목록
    /// 
    /// [MainViewModelCommandFactory]에서 Command를 생성할 때
    /// 기존 [MainViewModel] 메서드를 연결하기 위해 사용한다.
    /// </summary>
    internal sealed class MainViewModelCommandHandlerSet
    {
        #region [Communication Command Handler Properties]

        /// <summary>
        /// [MQ] 연결 요청 처리기
        /// </summary>
        internal Action StartMqReceive { get; set; }

        /// <summary>
        /// [MQ] 연결 해제 요청 처리기
        /// </summary>
        internal Action StopMqReceive { get; set; }

        /// <summary>
        /// [TCP] 연결 요청 처리기
        /// </summary>
        internal Func<Task> ConnectDevicesAsync { get; set; }

        /// <summary>
        /// [TCP] 연결 해제 요청 처리기
        /// </summary>
        internal Func<Task> DisconnectDevicesAsync { get; set; }

        /// <summary>
        /// [Radar] UDP 수신 시작 요청 처리기
        /// </summary>
        internal Action StartRadarUdpReceive { get; set; }

        /// <summary>
        /// [Radar] UDP 수신 중지 요청 처리기
        /// </summary>
        internal Action StopRadarUdpReceive { get; set; }

        #endregion

        #region [Pan / Tilt Command Handler Properties]

        /// <summary>
        /// [Pan] 좌측 이동 요청 처리기
        /// </summary>
        internal Action StartPanLeftMove { get; set; }

        /// <summary>
        /// [Pan] 우측 이동 요청 처리기
        /// </summary>
        internal Action StartPanRightMove { get; set; }

        /// <summary>
        /// [Tilt] 상향 이동 요청 처리기
        /// </summary>
        internal Action StartTiltUpMove { get; set; }

        /// <summary>
        /// [Tilt] 하향 이동 요청 처리기
        /// </summary>
        internal Action StartTiltDownMove { get; set; }

        /// <summary>
        /// [Pan / Tilt] 연속 이동 정지 요청 처리기
        /// </summary>
        internal Action StopContinuousMove { get; set; }

        /// <summary>
        /// [Pan] Absolute 이동 요청 처리기
        /// </summary>
        internal Action MovePanAbsolute { get; set; }

        /// <summary>
        /// [Tilt] Absolute 이동 요청 처리기
        /// </summary>
        internal Action MoveTiltAbsolute { get; set; }

        /// <summary>
        /// [Pan] Relative 이동 요청 처리기
        /// </summary>
        internal Action MovePanRelative { get; set; }

        /// <summary>
        /// [Tilt] Relative 이동 요청 처리기
        /// </summary>
        internal Action MoveTiltRelative { get; set; }

        /// <summary>
        /// [Home Position] 이동 요청 처리기
        /// </summary>
        internal Func<Task> MoveHomePositionAsync { get; set; }

        /// <summary>
        /// [Pan] 현재 위치 [0] 설정 요청 처리기
        /// </summary>
        internal Action SetPanZero { get; set; }

        /// <summary>
        /// [Tilt] 현재 위치 [0] 설정 요청 처리기
        /// </summary>
        internal Action SetTiltZero { get; set; }

        /// <summary>
        /// 위치 제어 입력값 초기화 요청 처리기
        /// </summary>
        internal Action ResetPositionInput { get; set; }

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정 요청 처리기
        /// </summary>
        internal Action SetPtzAutoMode { get; set; }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정 요청 처리기
        /// </summary>
        internal Action SetPtzManualMode { get; set; }

        #endregion

        #region [Zoom / Focus Command Handler Properties]

        /// <summary>
        /// [Zoom] 확대 요청 처리기
        /// </summary>
        internal Action StartZoomInMove { get; set; }

        /// <summary>
        /// [Zoom] 축소 요청 처리기
        /// </summary>
        internal Action StartZoomOutMove { get; set; }

        /// <summary>
        /// [Focus] Near 요청 처리기
        /// </summary>
        internal Action StartFocusNearMove { get; set; }

        /// <summary>
        /// [Focus] Far 요청 처리기
        /// </summary>
        internal Action StartFocusFarMove { get; set; }

        /// <summary>
        /// [Auto Focus] 요청 처리기
        /// </summary>
        internal Action AutoFocus { get; set; }

        /// <summary>
        /// [Zoom] 위치 이동 요청 처리기
        /// </summary>
        internal Action SetZoomPosition { get; set; }

        /// <summary>
        /// [Zoom] 배율 이동 요청 처리기
        /// </summary>
        internal Action SetZoomRatio { get; set; }

        /// <summary>
        /// [Focus] 위치 이동 요청 처리기
        /// </summary>
        internal Action SetFocusPosition { get; set; }

        #endregion

        #region [Test Command Handler Properties]

        /// <summary>
        /// [Dummy Tracking] 테스트 시작 요청 처리기
        /// </summary>
        internal Func<Task> StartDummyTrackingTestAsync { get; set; }

        /// <summary>
        /// [Dummy Tracking] 테스트 중지 요청 처리기
        /// </summary>
        internal Action StopDummyTrackingTest { get; set; }

        #endregion
    }

    /// <summary>
    /// [MainViewModel] Command 목록
    /// 
    /// [MainViewModelCommandFactory]에서 생성한 Command를
    /// [MainViewModel] 생성자에서 Binding Command Property에 할당하기 위해 사용한다.
    /// </summary>
    internal sealed class MainViewModelCommandSet
    {
        #region [Communication Command Properties]

        /// <summary>
        /// [MQ] 연결 요청 [Command]
        /// </summary>
        internal ICommand StartMqReceiveCommand { get; set; }

        /// <summary>
        /// [MQ] 연결 해제 요청 [Command]
        /// </summary>
        internal ICommand StopMqReceiveCommand { get; set; }

        /// <summary>
        /// [TCP] 연결 요청 [Command]
        /// </summary>
        internal ICommand StartTcpReceiveCommand { get; set; }

        /// <summary>
        /// [TCP] 연결 해제 요청 [Command]
        /// </summary>
        internal ICommand StopTcpReceiveCommand { get; set; }

        /// <summary>
        /// [Radar] UDP 수신 시작 요청 [Command]
        /// </summary>
        internal ICommand StartRadarUdpReceiveCommand { get; set; }

        /// <summary>
        /// [Radar] UDP 수신 중지 요청 [Command]
        /// </summary>
        internal ICommand StopRadarUdpReceiveCommand { get; set; }

        #endregion

        #region [Pan / Tilt Command Properties]

        /// <summary>
        /// [Pan] 좌측 이동 요청 [Command]
        /// </summary>
        internal ICommand PanLeftCommand { get; set; }

        /// <summary>
        /// [Pan] 우측 이동 요청 [Command]
        /// </summary>
        internal ICommand PanRightCommand { get; set; }

        /// <summary>
        /// [Tilt] 상향 이동 요청 [Command]
        /// </summary>
        internal ICommand TiltUpCommand { get; set; }

        /// <summary>
        /// [Tilt] 하향 이동 요청 [Command]
        /// </summary>
        internal ICommand TiltDownCommand { get; set; }

        /// <summary>
        /// [Pan / Tilt] 정지 요청 [Command]
        /// </summary>
        internal ICommand StopMoveCommand { get; set; }

        /// <summary>
        /// [Pan] Absolute 이동 요청 [Command]
        /// </summary>
        internal ICommand MovePanAbsoluteCommand { get; set; }

        /// <summary>
        /// [Tilt] Absolute 이동 요청 [Command]
        /// </summary>
        internal ICommand MoveTiltAbsoluteCommand { get; set; }

        /// <summary>
        /// [Pan] Relative 이동 요청 [Command]
        /// </summary>
        internal ICommand MovePanRelativeCommand { get; set; }

        /// <summary>
        /// [Tilt] Relative 이동 요청 [Command]
        /// </summary>
        internal ICommand MoveTiltRelativeCommand { get; set; }

        /// <summary>
        /// [Home Position] 이동 요청 [Command]
        /// </summary>
        internal ICommand MoveHomePositionCommand { get; set; }

        /// <summary>
        /// [Pan] 현재 위치 [0] 설정 요청 [Command]
        /// </summary>
        internal ICommand SetPanZeroCommand { get; set; }

        /// <summary>
        /// [Tilt] 현재 위치 [0] 설정 요청 [Command]
        /// </summary>
        internal ICommand SetTiltZeroCommand { get; set; }

        /// <summary>
        /// 위치 제어 입력값 초기화 요청 [Command]
        /// </summary>
        internal ICommand ResetPositionInputCommand { get; set; }

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정 요청 [Command]
        /// </summary>
        internal ICommand SetPtzAutoModeCommand { get; set; }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정 요청 [Command]
        /// </summary>
        internal ICommand SetPtzManualModeCommand { get; set; }

        #endregion

        #region [Zoom / Focus Command Properties]

        /// <summary>
        /// [Zoom] 확대 요청 [Command]
        /// </summary>
        internal ICommand ZoomInCommand { get; set; }

        /// <summary>
        /// [Zoom] 축소 요청 [Command]
        /// </summary>
        internal ICommand ZoomOutCommand { get; set; }

        /// <summary>
        /// [Focus] Near 요청 [Command]
        /// </summary>
        internal ICommand FocusNearCommand { get; set; }

        /// <summary>
        /// [Focus] Far 요청 [Command]
        /// </summary>
        internal ICommand FocusFarCommand { get; set; }

        /// <summary>
        /// [Auto Focus] 요청 [Command]
        /// </summary>
        internal ICommand AutoFocusCommand { get; set; }

        /// <summary>
        /// [Zoom] 위치 이동 요청 [Command]
        /// </summary>
        internal ICommand SetZoomPositionCommand { get; set; }

        /// <summary>
        /// [Zoom] 배율 이동 요청 [Command]
        /// </summary>
        internal ICommand SetZoomRatioCommand { get; set; }

        /// <summary>
        /// [Focus] 위치 이동 요청 [Command]
        /// </summary>
        internal ICommand SetFocusPositionCommand { get; set; }

        #endregion

        #region [Test Command Properties]

        /// <summary>
        /// [Dummy Tracking] 테스트 시작 요청 [Command]
        /// </summary>
        internal ICommand StartDummyTrackingTestCommand { get; set; }

        /// <summary>
        /// [Dummy Tracking] 테스트 중지 요청 [Command]
        /// </summary>
        internal ICommand StopDummyTrackingTestCommand { get; set; }

        #endregion
    }

}
