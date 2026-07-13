using Serilog;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Features.Main.ADS1000;
using VertiportNexus.Features.Main.Camera;
using VertiportNexus.Features.Main.Communication;
using VertiportNexus.Features.Main.Connection;
using VertiportNexus.Features.Main.Ptz;
using VertiportNexus.Features.Main.Test;
using VertiportNexus.Features.Main.Ui;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.ViewModels.Main.Composition;
using VertiportNexus.ViewModels.Main.Coordinators;
using VertiportNexus.ViewModels.Main.Panels;
using VertiportNexus.ViewModels.Main.States;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel]
    /// 
    /// 화면 Binding Property / Command / Controller 호출 / 결과 반영을 담당한다.
    /// 실제 기능 처리는 [Controllers] 하위 클래스에서 수행하고,
    /// [MainViewModel]은 반환된 결과를 기준으로 UI 상태만 갱신한다.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region [Enum Type]

        /// <summary>
        /// 장비 연결 상태
        /// </summary>
        public enum ConnectionState
        {
            /// <summary>
            /// 장비 미연결 상태
            /// </summary>
            Disconnected,

            /// <summary>
            /// 장비 연결 진행 상태
            /// </summary>
            Connecting,

            /// <summary>
            /// 장비 연결 완료 상태
            /// </summary>
            Connected
        }

        #endregion

        #region [Constants]

        /// <summary>
        /// 개발실 장비 [NPort] [IP]
        /// </summary>
        private const string DEFAULT_DEVICE_IP_ADDRESS = "192.168.0.113";

        /// <summary>
        /// [MCB] [TCP] 연결 [Port]
        /// </summary>
        private const int DEFAULT_MCB_PORT = 4001;

        /// <summary>
        /// [SCB] [TCP] 연결 [Port]
        /// </summary>
        private const int DEFAULT_SCB_PORT = 4002;

        /// <summary>
        /// [EO] [RTSP] 테스트 주소
        /// </summary>
        private const string DEFAULT_EO_RTSP_ADDRESS =
            "rtsp://service:Xhddlf1!@192.168.0.110:554/rtsp_tunnel";

        #endregion

        #region [Composition Fields]

        /// <summary>
        /// [MainViewModel] 구성 객체
        /// 
        /// Service / Controller / Manager 인스턴스를 보관하며,
        /// [MainViewModel]은 필요한 기능 객체를 해당 Context를 통해 참조한다.
        /// 
        /// 현재 단계에서는 기존 개별 Service / Controller 필드를 제거하고,
        /// 모든 기능 객체 접근을 [Context] 기준으로 통일한다.
        /// </summary>
        private readonly MainViewModelContext _context;

        #endregion

        #region [Feature Fields]

        /// <summary>
        /// [Dummy Tracking] 테스트 Manager
        /// 
        /// 실제 탐지 객체가 없는 상태에서
        /// 30Hz 더미 Bounding Box 입력과 AUTO Tracking 흐름을 검증한다.
        /// 
        /// [MainViewModel]은 테스트 시작 / 중지 요청만 전달하고,
        /// 더미 탐지 좌표 생성 / 최신 탐지값 처리 Loop는
        /// [DummyTrackingTestManager]에 위임한다.
        /// </summary>
        private readonly DummyTrackingTestManager _dummyTrackingTestManager;

        /// <summary>
        /// [EO Camera] 연동 Workflow
        /// 
        /// EO Camera Frame / 상태 처리와
        /// RTSP 재연결 Loop를 [MainViewModel]에서 분리하여 처리한다.
        /// </summary>
        private readonly EoCameraWorkflow _eoCameraWorkflow;

        /// <summary>
        /// [RabbitMQ] 수신 Workflow
        /// 
        /// RabbitMQ 수신 시작 / 중지 처리 흐름을
        /// [MainViewModel]에서 분리하여 처리한다.
        /// </summary>
        private readonly RabbitMqReceiveWorkflow _rabbitMqReceiveWorkflow;

        /// <summary>
        /// [Radar] UDP 수신 Workflow
        /// 
        /// Radar UDP 수신 시작 / 중지 처리 흐름을
        /// [MainViewModel]에서 분리하여 처리한다.
        /// </summary>
        private readonly RadarUdpReceiveWorkflow _radarUdpReceiveWorkflow;

        /// <summary>
        /// [Main Communication] 실행 Coordinator
        ///
        /// RabbitMQ / Radar UDP 시작 및 중지 흐름을
        /// [MainViewModel]에서 분리하여 처리한다.
        /// </summary>
        private readonly MainCommunicationCoordinator _communicationCoordinator;

        /// <summary>
        /// [Main Communication] 처리 결과 반영 객체
        /// </summary>
        private readonly MainCommunicationResultApplier _communicationResultApplier;

        /// <summary>
        /// [Main Communication Command] Proxy
        /// </summary>
        private readonly MainCommunicationCommandProxy _communicationCommandProxy;

        /// <summary>
        /// [ADS1000] 상태 수신 / 반영 Workflow
        /// 
        /// MCB / SCB 수신 Packet 처리와
        /// 파싱된 상태값 적용 Controller 호출 흐름을 담당한다.
        /// </summary>
        private readonly Ads1000StatusWorkflow _ads1000StatusWorkflow;

        /// <summary>
        /// [ADS1000] 상태 적용 결과 반영 객체
        /// </summary>
        private readonly MainAds1000StatusResultApplier _ads1000StatusResultApplier;

        /// <summary>
        /// [PTZ Control] Workflow
        /// 
        /// Pan / Tilt / Zoom / Focus / Keyboard / Mode 제어 명령 호출 흐름을
        /// [MainViewModel]에서 분리하여 처리한다.
        /// </summary>
        private readonly PtzControlWorkflow _ptzControlWorkflow;

        /// <summary>
        /// [Main PTZ Control] 실행 Coordinator
        ///
        /// Pan / Tilt / Zoom / Focus 제어 계산과
        /// [PtzControlWorkflow] 호출 흐름을
        /// [MainViewModel]에서 분리하여 처리한다.
        /// </summary>
        private readonly MainPtzControlCoordinator _ptzControlCoordinator;

        /// <summary>
        /// [PTZ Control] 처리 결과 반영 객체
        /// </summary>
        private readonly MainPtzControlResultApplier _ptzControlResultApplier;

        /// <summary>
        /// [PTZ Command] Proxy
        /// </summary>
        private readonly MainPtzCommandProxy _ptzCommandProxy;

        /// <summary>
        /// [Device Connection] Workflow
        /// 
        /// MCB / SCB 장비 연결 / 연결 해제 흐름과
        /// 연결 직후 EO RTSP 대기 / Home Position 이동 흐름을
        /// [MainViewModel]에서 분리하여 처리한다.
        /// </summary>
        private readonly DeviceConnectionWorkflow _deviceConnectionWorkflow;

        /// <summary>
        /// [MainViewModel] UI 갱신 서비스
        /// 
        /// 연결 상태 / 통신 상태 / Home Position 상태 변경 시
        /// 반복적으로 호출되는 PropertyChanged 묶음을 담당한다.
        /// </summary>
        private readonly MainViewModelUiRefreshService _uiRefreshService;

        /// <summary>
        /// [MainViewModel] UI 상태 조회 서비스
        /// </summary>
        private readonly MainViewModelUiStateService _uiStateService;

        #endregion

        #region [Network Setting Fields]

        /// <summary>
        /// Network / MQ / Radar UDP 입력 설정 상태
        /// </summary>
        private readonly MainNetworkPanelViewModel _networkPanel =
            new MainNetworkPanelViewModel(
                DEFAULT_DEVICE_IP_ADDRESS,
                DEFAULT_MCB_PORT,
                DEFAULT_SCB_PORT);

        #endregion

        #region [Status Fields]

        /// <summary>
        /// Main / MQ / PTZ 화면 표시 상태
        /// </summary>
        private readonly MainStatusPanelViewModel _statusPanel =
            new MainStatusPanelViewModel();

        /// <summary>
        /// 장비 / 통신 연결 상태
        /// </summary>
        private readonly MainConnectionPanelViewModel _connectionPanel =
            new MainConnectionPanelViewModel();

        #endregion

        #region [Camera State Fields]

        /// <summary>
        /// Camera 위치 / 입력 / UI Zero 상태 저장 객체
        /// </summary>
        private readonly MainCameraState _cameraState;

        /// <summary>
        /// Camera 상태 / 입력 Binding 처리 Panel
        /// </summary>
        private readonly MainCameraPanelViewModel _cameraPanel;

        #endregion

        #region [Image Binding Fields - Test Only]

        private BitmapSource _eoCameraImage;

        #endregion

        #region [Command Properties]

        public ICommand StartMqReceiveCommand { get; }

        public ICommand StopMqReceiveCommand { get; }

        public ICommand StartTcpReceiveCommand { get; }

        public ICommand StopTcpReceiveCommand { get; }

        public ICommand PanLeftCommand { get; }

        public ICommand PanRightCommand { get; }

        public ICommand TiltUpCommand { get; }

        public ICommand TiltDownCommand { get; }

        public ICommand StopMoveCommand { get; }

        public ICommand ZoomInCommand { get; }

        public ICommand ZoomOutCommand { get; }

        public ICommand FocusNearCommand { get; }

        public ICommand FocusFarCommand { get; }

        public ICommand AutoFocusCommand { get; }

        public ICommand MovePanAbsoluteCommand { get; }

        public ICommand MoveTiltAbsoluteCommand { get; }

        public ICommand MovePanRelativeCommand { get; }

        public ICommand MoveTiltRelativeCommand { get; }

        public ICommand MoveHomePositionCommand { get; }

        public ICommand SetPanZeroCommand { get; }

        public ICommand SetTiltZeroCommand { get; }

        public ICommand ResetPositionInputCommand { get; }

        public ICommand SetZoomPositionCommand { get; }

        public ICommand SetZoomRatioCommand { get; }

        public ICommand SetFocusPositionCommand { get; }

        public ICommand SetPtzAutoModeCommand { get; }

        public ICommand SetPtzManualModeCommand { get; }

        public ICommand StartRadarUdpReceiveCommand { get; }

        public ICommand StopRadarUdpReceiveCommand { get; }

        public ICommand StartDummyTrackingTestCommand { get; }

        public ICommand StopDummyTrackingTestCommand { get; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MainViewModel] 생성자
        /// </summary>
        public MainViewModel()
        {
            #region [Composition Initialize]

            MainViewModelBootstrapper bootstrapper =
                new MainViewModelBootstrapper();

            _context =
                bootstrapper.Create(
                    MqHostName,
                    MqPort,
                    new MainViewModelEventHandlerSet
                    {
                        OnMcbMessageReceived =
                            OnMcbMessageReceived,

                        OnScbMessageReceived =
                            OnScbMessageReceived,

                        OnAds1000ConnectionStateChanged =
                            OnAds1000ConnectionStateChanged,

                        OnEoCameraFrameReceived =
                            OnEoCameraFrameReceived,

                        OnEoCameraStatusChanged =
                            OnEoCameraStatusChanged,

                        OnAds1000SendResultChanged =
                            OnAds1000SendResultChanged,

                        OnPtzControlModeChanged =
                            OnPtzControlModeChanged,

                        OnCseCommandReceived =
                            OnCseCommandReceived
                    });

            #endregion

            #region [Feature Initialize]

            _uiRefreshService =
                new MainViewModelUiRefreshService();

            _uiStateService =
                new MainViewModelUiStateService();

            _cameraState =
                new MainCameraState();

            _cameraPanel =
                new MainCameraPanelViewModel(
                    _context,
                    _cameraState);

            _dummyTrackingTestManager =
                new DummyTrackingTestManager(
                    _context.TrackingControlService);

            _eoCameraWorkflow =
                new EoCameraWorkflow(
                    _context.EoCameraController,
                    _context.EoCameraService,
                    DEFAULT_EO_RTSP_ADDRESS);

            _rabbitMqReceiveWorkflow =
                new RabbitMqReceiveWorkflow(
                    _context);

            _radarUdpReceiveWorkflow =
                new RadarUdpReceiveWorkflow(
                    _context);

            _communicationCoordinator =
                new MainCommunicationCoordinator(
                    _rabbitMqReceiveWorkflow,
                    _radarUdpReceiveWorkflow);

            _communicationResultApplier =
                new MainCommunicationResultApplier(
                    _statusPanel,
                    _connectionPanel,
                    _uiRefreshService,
                    OnPropertyChanged);

            _communicationCommandProxy =
                new MainCommunicationCommandProxy(
                    _communicationCoordinator,
                    _communicationResultApplier,
                    _networkPanel,
                    _connectionPanel,
                    SetRabbitMqConnectionState,
                    SetRadarUdpConnectionState);

            _ads1000StatusWorkflow =
                new Ads1000StatusWorkflow(
                    _context);

            _ptzControlWorkflow =
                new PtzControlWorkflow(
                    _context);

            _ptzControlCoordinator =
                new MainPtzControlCoordinator(
                    _ptzControlWorkflow,
                    _cameraState);

            _ads1000StatusResultApplier =
                new MainAds1000StatusResultApplier(
                    _cameraState,
                    zoomPosition =>
                    {
                        return _ptzControlCoordinator
                            .ConvertZoomPositionToRatio(
                                zoomPosition);
                    },
                    OnPropertyChanged);

            _ptzControlResultApplier =
                new MainPtzControlResultApplier(
                    _context,
                    _cameraState,
                    _statusPanel,
                    OnPropertyChanged);

            _ptzCommandProxy =
                new MainPtzCommandProxy(
                    _ptzControlCoordinator,
                    _ptzControlResultApplier,
                    _cameraPanel,
                    _cameraState,
                    _connectionPanel,
                    IsDeviceFullyConnected,
                    SetHomePositionMovingState,
                    statusText =>
                    {
                        MainStatusText =
                            statusText;
                    },
                    OnPropertyChanged);

            _deviceConnectionWorkflow =
                new DeviceConnectionWorkflow(
                    _context,
                    _eoCameraWorkflow,
                    _rabbitMqReceiveWorkflow,
                    _radarUdpReceiveWorkflow,
                    _ptzControlWorkflow);

            #endregion

            #region [Command Initialize]

            MainViewModelCommandFactory commandFactory =
                new MainViewModelCommandFactory();

            MainViewModelCommandSet commands =
                commandFactory.Create(
                    new MainViewModelCommandHandlerSet
                    {
                        StartMqReceive =
                            async () =>
                            {
                                await _communicationCommandProxy
                                    .StartRabbitMqReceiveAsync();
                            },

                        StopMqReceive =
                            _communicationCommandProxy.StopRabbitMqReceive,

                        ConnectDevicesAsync =
                            ConnectDevicesAsync,

                        DisconnectDevicesAsync =
                            DisconnectDevicesAsync,

                        StartPanLeftMove =
                            _ptzCommandProxy.StartPanLeftMove,

                        StartPanRightMove =
                            _ptzCommandProxy.StartPanRightMove,

                        StartTiltUpMove =
                            _ptzCommandProxy.StartTiltUpMove,

                        StartTiltDownMove =
                            _ptzCommandProxy.StartTiltDownMove,

                        StopContinuousMove =
                            _ptzCommandProxy.StopContinuousMove,

                        StartZoomInMove =
                            _ptzCommandProxy.StartZoomInMove,

                        StartZoomOutMove =
                            _ptzCommandProxy.StartZoomOutMove,

                        StartFocusNearMove =
                            _ptzCommandProxy.StartFocusNearMove,

                        StartFocusFarMove =
                            _ptzCommandProxy.StartFocusFarMove,

                        AutoFocus =
                            _ptzCommandProxy.AutoFocus,

                        MovePanAbsolute =
                            _ptzCommandProxy.MovePanAbsolute,

                        MoveTiltAbsolute =
                            _ptzCommandProxy.MoveTiltAbsolute,

                        MovePanRelative =
                            _ptzCommandProxy.MovePanRelative,

                        MoveTiltRelative =
                            _ptzCommandProxy.MoveTiltRelative,

                        MoveHomePositionAsync =
                            _ptzCommandProxy.MoveHomePositionAsync,

                        SetPanZero =
                            _ptzCommandProxy.SetPanZero,

                        SetTiltZero =
                            _ptzCommandProxy.SetTiltZero,

                        ResetPositionInput =
                            _ptzCommandProxy.ResetPositionInput,

                        SetZoomPosition =
                            _ptzCommandProxy.SetZoomPosition,

                        SetZoomRatio =
                            _ptzCommandProxy.SetZoomRatio,

                        SetFocusPosition =
                            _ptzCommandProxy.SetFocusPosition,

                        SetPtzAutoMode =
                            _ptzCommandProxy.SetAutoMode,

                        SetPtzManualMode =
                            _ptzCommandProxy.SetManualMode,

                        StartRadarUdpReceive =
                            async () =>
                            {
                                await _communicationCommandProxy
                                    .StartRadarUdpReceiveAsync();
                            },

                        StopRadarUdpReceive =
                            _communicationCommandProxy.StopRadarUdpReceive,

                        StartDummyTrackingTestAsync =
                            StartDummyTrackingTestAsync,

                        StopDummyTrackingTest =
                            StopDummyTrackingTest
                    });

            StartMqReceiveCommand =
                commands.StartMqReceiveCommand;

            StopMqReceiveCommand =
                commands.StopMqReceiveCommand;

            StartTcpReceiveCommand =
                commands.StartTcpReceiveCommand;

            StopTcpReceiveCommand =
                commands.StopTcpReceiveCommand;

            PanLeftCommand =
                commands.PanLeftCommand;

            PanRightCommand =
                commands.PanRightCommand;

            TiltUpCommand =
                commands.TiltUpCommand;

            TiltDownCommand =
                commands.TiltDownCommand;

            StopMoveCommand =
                commands.StopMoveCommand;

            ZoomInCommand =
                commands.ZoomInCommand;

            ZoomOutCommand =
                commands.ZoomOutCommand;

            FocusNearCommand =
                commands.FocusNearCommand;

            FocusFarCommand =
                commands.FocusFarCommand;

            AutoFocusCommand =
                commands.AutoFocusCommand;

            MovePanAbsoluteCommand =
                commands.MovePanAbsoluteCommand;

            MoveTiltAbsoluteCommand =
                commands.MoveTiltAbsoluteCommand;

            MovePanRelativeCommand =
                commands.MovePanRelativeCommand;

            MoveTiltRelativeCommand =
                commands.MoveTiltRelativeCommand;

            MoveHomePositionCommand =
                commands.MoveHomePositionCommand;

            SetPanZeroCommand =
                commands.SetPanZeroCommand;

            SetTiltZeroCommand =
                commands.SetTiltZeroCommand;

            ResetPositionInputCommand =
                commands.ResetPositionInputCommand;

            SetZoomPositionCommand =
                commands.SetZoomPositionCommand;

            SetZoomRatioCommand =
                commands.SetZoomRatioCommand;

            SetFocusPositionCommand =
                commands.SetFocusPositionCommand;

            SetPtzAutoModeCommand =
                commands.SetPtzAutoModeCommand;

            SetPtzManualModeCommand =
                commands.SetPtzManualModeCommand;

            StartRadarUdpReceiveCommand =
                commands.StartRadarUdpReceiveCommand;

            StopRadarUdpReceiveCommand =
                commands.StopRadarUdpReceiveCommand;

            StartDummyTrackingTestCommand =
                commands.StartDummyTrackingTestCommand;

            StopDummyTrackingTestCommand =
                commands.StopDummyTrackingTestCommand;

            #endregion

            #region [Default Initialize]

            Console.WriteLine(
                "[CAMERA][STATE] Pan Turn Mode : "
                + _cameraState.PanTurnMode);

            ConsoleLogHelper.PrintLine();

            InitializeDefaultValues();

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[MAIN] ADS1000 Direct TCP Test Initialize Complete");

            ConsoleLogHelper.PrintLine();

            #endregion
        }

        #endregion

        #region [Initialize]

        /// <summary>
        /// 기본 상태값 초기화
        /// </summary>
        private void InitializeDefaultValues()
        {
            MainStatusText =
                "MCB / SCB DISCONNECTED";

            OperationModeText =
                "MODE STANDBY";

            PtzControlModeText =
                _context.CameraStateProvider.PtzControlMode;

            PanTiltSpeedLevel
                = 25;

            MqStatusText =
                "RabbitMQ Ready";

            McbIpAddress =
                DEFAULT_DEVICE_IP_ADDRESS;

            McbPort =
                DEFAULT_MCB_PORT;

            ScbIpAddress =
                DEFAULT_DEVICE_IP_ADDRESS;

            ScbPort =
                DEFAULT_SCB_PORT;

            PanAbsoluteValue =
                0;

            TiltAbsoluteValue =
                0;

            PanRelativeValue =
                0;

            TiltRelativeValue =
                0;

            ZoomPositionValue =
                0;

            ZoomRatioValue =
                1;

            FocusPositionValue =
                0;

            // [Pan] 선회 모드 기본값 설정
            //
            // 장비가 불필요하게 먼 방향으로 회전하지 않도록
            // 기본 선회 모드는 [Short]로 설정한다.
            _cameraPanel
                .SetPanTurnMode(
                    Ads1000PanTurnMode.Short);

            OnPropertyChanged(nameof(IsPanTurnShortMode));

            OnPropertyChanged(nameof(IsPanTurnViaZeroMode));
        }

        #endregion

        #region [ADS1000 Control Event Methods]

        /// <summary>
        /// [ADS1000] [Packet] 송신 결과 처리
        /// 
        /// [Ads1000CameraControlService]에서 전달받은 송신 결과를
        /// 화면 상태 문자열에 반영한다.
        /// </summary>
        /// <param name="sendResult">
        /// [ADS1000] [Packet] 송신 결과
        /// </param>
        private void OnAds1000SendResultChanged(
            Ads1000SendResult sendResult)
        {
            if (!sendResult.IsSuccess)
            {
                return;
            }

        }

        #endregion

        #region [TCP Connection Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결
        /// 
        /// 실제 장비 연결 Controller 호출과
        /// EO RTSP 연결 시작 흐름은 [DeviceConnectionWorkflow]에 위임하고,
        /// [MainViewModel]은 연결 상태와 화면 상태만 갱신한다.
        /// </summary>
        private async Task ConnectDevicesAsync()
        {
            if (_connectionPanel.IsDeviceConnecting)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Connect Ignored : Connecting");

                return;
            }

            if (_connectionPanel.McbConnectionState == ConnectionState.Connected ||
                _connectionPanel.ScbConnectionState == ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Connect Ignored : Already Connected");

                return;
            }

            MainStatusText =
                "MCB / SCB CONNECTING...";

            OperationModeText =
                "DEVICE CONNECTING...";

            _connectionPanel.IsDeviceConnecting =
                true;

            SetDeviceConnectionState(
                ConnectionState.Connecting,
                ConnectionState.Connecting);

            _uiRefreshService
                .NotifyDeviceConnectionBusyStateChanged(
                    OnPropertyChanged);

            try
            {
                DeviceConnectionWorkflowResult workflowResult =
                    await _deviceConnectionWorkflow
                        .ConnectAsync(
                            McbIpAddress,
                            McbPort,
                            ScbIpAddress,
                            ScbPort);

                DeviceConnectionControllerResult result =
                    workflowResult.ConnectResult;

                if (result.IsSuccess &&
                    result.ConnectionResult != null)
                {
                    ApplyDeviceConnectionResult(
                        result.ConnectionResult);

                    MainStatusText =
                        result.Message;

                    OperationModeText =
                        "DEVICE CONNECTED";

                    // [EO RTSP] 연결 성공 대기 후 [Home Position] 이동
                    //
                    // 장비 연결 직후 EO Camera가 Ready 상태가 아닐 수 있으므로,
                    // RTSP 연결 성공 상태를 별도 비동기 흐름에서 대기한 뒤
                    // Home Position 이동을 수행한다.
                    // TODO: 초기 자동 Home Position 이동 임시 비활성화
                    //_ = WaitEoRtspConnectedAndMoveHomePositionAsync();
                }
                else
                {
                    SetDeviceConnectionState(
                        ConnectionState.Disconnected,
                        ConnectionState.Disconnected);

                    MainStatusText =
                        result.Message;

                    OperationModeText =
                        "DEVICE CONNECT FAILED";
                }

            }
            finally
            {
                _connectionPanel.IsDeviceConnecting =
                    false;

                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));
                OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));
                OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));
                OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));
                OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));
            }

        }

        /// <summary>
        /// [장비 연결 후] EO RTSP 연결 성공 대기 및 Home Position 이동
        /// 
        /// 실제 EO RTSP 연결 성공 대기 및 Home Position 이동 흐름은
        /// [DeviceConnectionWorkflow]에 위임하고,
        /// [MainViewModel]은 Workflow 결과를 화면 상태에 반영한다.
        /// </summary>
        private async Task WaitEoRtspConnectedAndMoveHomePositionAsync()
        {
            PtzControlWorkflowResult result =
                await _deviceConnectionWorkflow
                    .WaitEoRtspConnectedAndMoveHomePositionAsync(
                        _connectionPanel.IsHomePositionMoving,
                        IsDeviceFullyConnected(),
                        IsDeviceFullyConnected,
                        () => CurrentPan,
                        () => CurrentTilt,
                        SetHomePositionMovingState,
                        statusText =>
                        {
                            MainStatusText =
                                statusText;
                        });

            ApplyPtzControlWorkflowResult(
                result);
        }

        /// <summary>
        /// [Home Position] 이동 상태 반영
        /// 
        /// Home Position 이동 진행 여부를 저장하고,
        /// 장비 연결 버튼 및 장비 제어 탭 활성 / 비활성 상태를 갱신한다.
        /// </summary>
        /// <param name="isMoving">
        /// Home Position 이동 진행 여부
        /// </param>
        private void SetHomePositionMovingState(
            bool isMoving)
        {
            _connectionPanel.IsHomePositionMoving =
                isMoving;

            _uiRefreshService
                .NotifyHomePositionMovingStateChanged(
                    OnPropertyChanged);

            OnPropertyChanged(
                nameof(IsManualPanTiltControlEnabled));
        }

        /// <summary>
        /// [MCB] / [SCB] 연결 상태 반영
        /// </summary>
        /// <param name="mcbConnectionState">
        /// [MCB] 연결 상태
        /// </param>
        /// <param name="scbConnectionState">
        /// [SCB] 연결 상태
        /// </param>
        private void SetDeviceConnectionState(
            ConnectionState mcbConnectionState,
            ConnectionState scbConnectionState)
        {
            // [MCB] 연결 상태 저장
            //
            // [MCB] 연결 여부를
            // 내부 상태값에 반영한다.
            _connectionPanel.McbConnectionState =
                mcbConnectionState;

            // [SCB] 연결 상태 저장
            //
            // [SCB] 연결 여부를
            // 내부 상태값에 반영한다.
            _connectionPanel.ScbConnectionState =
                scbConnectionState;

            _uiRefreshService
                .NotifyDeviceConnectionStateChanged(
                    OnPropertyChanged);

            OnPropertyChanged(nameof(IsManualPanTiltControlEnabled));
        }

        /// <summary>
        /// [ADS1000] 장비 연결 상태 변경 처리
        /// 
        /// [MCB] / [SCB] 연결 시도 결과를
        /// 장비별로 화면에 즉시 반영한다.
        /// </summary>
        /// <param name="isMcbConnected">
        /// [MCB] 연결 성공 여부
        /// </param>
        /// <param name="isScbConnected">
        /// [SCB] 연결 성공 여부
        /// </param>
        private void OnAds1000ConnectionStateChanged(
            bool? isMcbConnected,
            bool? isScbConnected)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ConnectionState mcbConnectionState =
                    isMcbConnected.HasValue
                        ? isMcbConnected.Value
                            ? ConnectionState.Connected
                            : ConnectionState.Disconnected
                        : _connectionPanel.McbConnectionState;

                ConnectionState scbConnectionState =
                    isScbConnected.HasValue
                        ? isScbConnected.Value
                            ? ConnectionState.Connected
                            : ConnectionState.Disconnected
                        : _connectionPanel.ScbConnectionState;

                SetDeviceConnectionState(
                    mcbConnectionState,
                    scbConnectionState);
            }));

        }

        /// <summary>
        /// [MCB] / [SCB] 연결 결과 화면 반영
        /// </summary>
        /// <param name="connectionResult">
        /// [ADS1000] 장비 연결 결과
        /// </param>
        private void ApplyDeviceConnectionResult(
            Ads1000ConnectionResult connectionResult)
        {
            if (connectionResult.IsMcbConnected &&
                connectionResult.IsScbConnected)
            {
                MainStatusText =
                    "MCB / SCB CONNECTED";

                OperationModeText =
                    "ADS1000 CONTROL";
            }
            else if (connectionResult.IsMcbConnected)
            {
                MainStatusText =
                    "MCB ONLY CONNECTED";

                OperationModeText =
                    "MCB ONLY";
            }
            else if (connectionResult.IsScbConnected)
            {
                MainStatusText =
                    "SCB ONLY CONNECTED";

                OperationModeText =
                    "SCB ONLY";
            }
            else
            {
                MainStatusText =
                    "MCB / SCB DISCONNECTED";

                OperationModeText =
                    "CONNECT FAILED";
            }

            SetDeviceConnectionState(
                connectionResult.IsMcbConnected
                    ? ConnectionState.Connected
                    : ConnectionState.Disconnected,
                connectionResult.IsScbConnected
                    ? ConnectionState.Connected
                    : ConnectionState.Disconnected);

            // [Camera] 연결 상태 저장
            //
            // [CSE] [Get PTZ State] 응답에서 사용할 수 있도록
            // [MCB] / [SCB] 중 하나 이상 연결된 경우 연결 상태로 판단한다.
            _context.CameraStateProvider.UpdateConnectionState(
                connectionResult.IsMcbConnected ||
                connectionResult.IsScbConnected);
        }

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결 해제
        /// 
        /// 실제 Radar UDP / RabbitMQ 중지, EO Camera 연결 해제,
        /// 장비 연결 해제 Controller 호출은 [DeviceConnectionWorkflow]에 위임하고,
        /// [MainViewModel]은 연결 상태와 화면 상태만 갱신한다.
        /// </summary>
        private Task DisconnectDevicesAsync()
        {
            if (_connectionPanel.IsDeviceDisconnecting)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Disconnect Ignored : Disconnecting");

                return Task.CompletedTask;
            }

            if (_connectionPanel.McbConnectionState == ConnectionState.Disconnected &&
                _connectionPanel.ScbConnectionState == ConnectionState.Disconnected)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Disconnect Ignored : Already Disconnected");

                return Task.CompletedTask;
            }

            _connectionPanel.IsDeviceDisconnecting =
                true;

            try
            {
                DeviceConnectionWorkflowResult workflowResult =
                    _deviceConnectionWorkflow
                        .Disconnect(
                            _connectionPanel.RadarUdpConnectionState == ConnectionState.Connected,
                            _connectionPanel.RabbitMqConnectionState == ConnectionState.Connected);

                if (workflowResult.RadarUdpStopResult != null)
                {
                    SetRadarUdpConnectionState(
                        ConnectionState.Disconnected);

                    MainStatusText =
                        workflowResult.RadarUdpStopResult.Message;
                }

                if (workflowResult.RabbitMqStopResult != null)
                {
                    SetRabbitMqConnectionState(
                        ConnectionState.Disconnected);

                    MqStatusText =
                        workflowResult.RabbitMqStopResult.Message;
                }

                SetDeviceConnectionState(
                    ConnectionState.Disconnected,
                    ConnectionState.Disconnected);

                MainStatusText =
                    workflowResult.DisconnectResult.Message;

                OperationModeText =
                    "DEVICE DISCONNECTED";
            }
            finally
            {
                _connectionPanel.IsDeviceDisconnecting =
                    false;

                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));
                OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));
                OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));
                OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));
                OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));
            }
            return Task.CompletedTask;
        }

        #endregion

        #region [CSE Receive Event Methods]

        /// <summary>
        /// [CSE] 명령 수신 처리
        /// 
        /// [MQ] 수신부에서 [JSON] 파싱이 완료된 명령을 전달받아,
        /// [CseCommandHandler]를 통해 실제 카메라 제어 명령으로 처리한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void OnCseCommandReceived(
            CseCommandMessage message)
        {
            _context.CseCommandHandler.HandleCommand(
                message);
        }

        #endregion

        #region [EO Camera Event Methods]

        /// <summary>
        /// [EO Camera] Frame 수신 처리
        /// 
        /// RTSP 수신 서비스에서 전달된 BitmapSource Frame을
        /// [EoCameraWorkflow]를 통해 화면 반영 가능한 결과로 변환하고,
        /// UI Binding 속성에 반영한다.
        /// </summary>
        /// <param name="bitmap">
        /// EO Camera Frame Image
        /// </param>
        private void OnEoCameraFrameReceived(
            BitmapSource bitmap)
        {
            try
            {
                System.Windows.Threading.Dispatcher dispatcher =
                    App.Current?.Dispatcher;

                if (dispatcher == null ||
                    dispatcher.HasShutdownStarted ||
                    dispatcher.HasShutdownFinished)
                {
                    return;
                }

                // [EO Camera] 영상 초기화 Frame 처리
                //
                // EO RTSP Disconnect 시 EoCameraService에서
                // FrameReceived(null)을 전달하므로,
                // 이 경우 UI Image Source를 null로 초기화하여
                // 화면을 Black 상태로 되돌린다.
                if (bitmap == null)
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (dispatcher.HasShutdownStarted ||
                            dispatcher.HasShutdownFinished)
                        {
                            return;
                        }

                        EOCameraImage =
                            null;
                    }));

                    return;
                }

                if (_eoCameraWorkflow == null)
                {
                    return;
                }

                EoCameraControllerResult result =
                    _eoCameraWorkflow
                        .CreateFrameResult(
                            bitmap);

                if (result == null ||
                    result.Frame == null)
                {
                    return;
                }

                dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dispatcher.HasShutdownStarted ||
                        dispatcher.HasShutdownFinished)
                    {
                        return;
                    }

                    EOCameraImage =
                        result.Frame;
                }));

            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[EO Camera] Frame Receive Failed");
            }

        }

        /// <summary>
        /// [EO] 영상 상태 변경 처리
        /// 
        /// [EoCameraService]에서 전달받은 상태 메시지를
        /// [EoCameraWorkflow]를 통해 연결 상태 / 재연결 필요 여부로 변환하고,
        /// 화면 상태 문자열에 반영한다.
        /// </summary>
        /// <param name="statusText">
        /// [EO] 영상 상태 문자열
        /// </param>
        private void OnEoCameraStatusChanged(
            string statusText)
        {
            EoCameraControllerResult result =
                _eoCameraWorkflow
                    .CreateStatusResult(
                        statusText);

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MainStatusText =
                    result.Message;

                if (!string.IsNullOrWhiteSpace(result.OperationModeText))
                {
                    OperationModeText =
                        result.OperationModeText;
                }

                if (result.ShouldStartReconnect)
                {
                    _eoCameraWorkflow
                        .StartReconnect(
                            operationModeText =>
                            {
                                OperationModeText =
                                    operationModeText;
                            });
                }
            }));

        }

        #endregion

        #region [Dummy Tracking Test Methods]

        /// <summary>
        /// [Dummy Tracking] 테스트 시작
        /// 
        /// 실제 Dummy Tracking 실행 흐름은
        /// [DummyTrackingTestManager]에서 처리하고,
        /// [MainViewModel]은 현재 장비 연결 상태와
        /// Zoom 값 조회 함수만 전달한다.
        /// </summary>
        /// <returns>
        /// 비동기 작업
        /// </returns>
        private Task StartDummyTrackingTestAsync()
        {
            bool isDeviceFullyConnected =
                _connectionPanel.McbConnectionState == ConnectionState.Connected &&
                _connectionPanel.ScbConnectionState == ConnectionState.Connected;

            return _dummyTrackingTestManager
                .StartAsync(
                    isDeviceFullyConnected,
                    () => CurrentZoom);
        }

        /// <summary>
        /// [Dummy Tracking] 테스트 중지
        /// 
        /// 실행 중인 더미 Bounding Box 주입 Loop 중지는
        /// [DummyTrackingTestManager]에 위임한다.
        /// </summary>
        private void StopDummyTrackingTest()
        {
            _dummyTrackingTestManager
                .Stop();
        }

        #endregion

        #region [Keyboard Event Proxy Methods]

        /// <summary>
        /// [Keyboard] 방향키 입력 처리
        /// </summary>
        /// <param name="key">
        /// 입력된 키
        /// </param>
        public void HandlePanTiltKeyDown(
            Key key)
        {
            if (!IsManualPanTiltControlEnabled)
            {
                _ptzCommandProxy
                    .ResetKeyboardPanTiltState();

                return;
            }

            _ptzCommandProxy
                .HandlePanTiltKeyDown(
                    key);
        }

        /// <summary>
        /// [Keyboard] 방향키 해제 처리
        /// 
        /// 대각선 입력 상태에서 두 방향키를 거의 동시에 해제하면
        /// 일부 KeyUp 이벤트가 누락되거나 순서가 밀릴 수 있으므로,
        /// 일반 KeyUp 처리 후 한 번 더 현재 Keyboard 상태 기준으로
        /// 이동 상태를 재계산한다.
        /// </summary>
        /// <param name="key">
        /// 해제된 키
        /// </param>
        public void HandlePanTiltKeyUp(
            Key key)
        {
            _ptzCommandProxy
                .HandlePanTiltKeyUp(
                    key);

            System.Windows.Threading.Dispatcher dispatcher =
                App.Current?.Dispatcher;

            if (dispatcher == null ||
                dispatcher.HasShutdownStarted ||
                dispatcher.HasShutdownFinished)
            {
                return;
            }

            dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsManualPanTiltControlEnabled)
                {
                    _ptzCommandProxy
                        .ResetKeyboardPanTiltState();

                    return;
                }

                _ptzCommandProxy
                    .HandlePanTiltKeyUp(
                        key);
            }));
        }

        /// <summary>
        /// [Keyboard] 방향키 입력 상태 초기화 및 Pan / Tilt 연속 이동 정지
        /// 
        /// Window Focus 이탈 / KeyUp 누락 / 동시 키 해제 상황에서
        /// 잔류 이동을 방지하기 위해 Window에서 직접 호출한다.
        /// </summary>
        public void ResetKeyboardPanTiltState()
        {
            _ptzCommandProxy
                .ResetKeyboardPanTiltState();
        }

        #endregion

        #region [PTZ External Event Proxy Methods]

        /// <summary>
        /// [Pan] 왼쪽 연속 이동 요청
        /// 
        /// MainWindow Mouse Event에서 직접 호출하므로
        /// public 진입점은 유지하고 실제 처리는 Proxy에 위임한다.
        /// </summary>
        public void StartPanLeftMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartPanLeftMove();
        }

        /// <summary>
        /// [Pan] 오른쪽 연속 이동 요청
        /// </summary>
        public void StartPanRightMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartPanRightMove();
        }

        /// <summary>
        /// [Tilt] 위쪽 연속 이동 요청
        /// </summary>
        public void StartTiltUpMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartTiltUpMove();
        }

        /// <summary>
        /// [Tilt] 아래쪽 연속 이동 요청
        /// </summary>
        public void StartTiltDownMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartTiltDownMove();
        }

        /// <summary>
        /// [Zoom] 확대 연속 이동 요청
        /// </summary>
        public void StartZoomInMove()
        {
            _ptzCommandProxy
                .StartZoomInMove();
        }

        /// <summary>
        /// [Zoom] 축소 연속 이동 요청
        /// </summary>
        public void StartZoomOutMove()
        {
            _ptzCommandProxy
                .StartZoomOutMove();
        }

        /// <summary>
        /// [Focus] Near 연속 이동 요청
        /// </summary>
        public void StartFocusNearMove()
        {
            _ptzCommandProxy
                .StartFocusNearMove();
        }

        /// <summary>
        /// [Focus] Far 연속 이동 요청
        /// </summary>
        public void StartFocusFarMove()
        {
            _ptzCommandProxy
                .StartFocusFarMove();
        }

        /// <summary>
        /// [Pan / Tilt / Zoom / Focus] 연속 이동 정지 요청
        /// </summary>
        public void StopContinuousMove()
        {
            _ptzCommandProxy
                .StopContinuousMove();
        }

        /// <summary>
        /// [Pan Left / Tilt Up] 대각선 연속 이동 요청
        /// </summary>
        public void StartPanLeftTiltUpMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartPanLeftTiltUpMove();
        }

        /// <summary>
        /// [Pan Right / Tilt Up] 대각선 연속 이동 요청
        /// </summary>
        public void StartPanRightTiltUpMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartPanRightTiltUpMove();
        }

        /// <summary>
        /// [Pan Left / Tilt Down] 대각선 연속 이동 요청
        /// </summary>
        public void StartPanLeftTiltDownMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartPanLeftTiltDownMove();
        }

        /// <summary>
        /// [Pan Right / Tilt Down] 대각선 연속 이동 요청
        /// </summary>
        public void StartPanRightTiltDownMove()
        {
            if (!IsManualPanTiltControlEnabled)
            {
                return;
            }

            _ptzCommandProxy
                .StartPanRightTiltDownMove();
        }

        #endregion

        #region [Connection State Proxy Methods]

        /// <summary>
        /// [RabbitMQ] 연결 상태 반영
        /// </summary>
        private void SetRabbitMqConnectionState(
            ConnectionState connectionState)
        {
            _connectionPanel.RabbitMqConnectionState =
                connectionState;

            _uiRefreshService
                .NotifyRabbitMqConnectionStateChanged(
                    OnPropertyChanged);
        }

        /// <summary>
        /// [Radar] UDP 연결 상태 반영
        /// </summary>
        private void SetRadarUdpConnectionState(
            ConnectionState connectionState)
        {
            _connectionPanel.RadarUdpConnectionState =
                connectionState;

            _uiRefreshService
                .NotifyRadarUdpConnectionStateChanged(
                    OnPropertyChanged);
        }

        #endregion

        #region [INotifyPropertyChanged]

        /// <summary>
        /// [Property] 변경 이벤트
        /// 
        /// [XAML] 바인딩 속성 갱신 시 사용한다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// [XAML] 바인딩 갱신 알림
        /// </summary>
        /// <param name="propertyName">
        /// 변경된 [Property] 이름
        /// </param>
        private void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Binding Property 값 변경 처리
        /// </summary>
        /// <typeparam name="T">
        /// Property 값 형식
        /// </typeparam>
        /// <param name="field">
        /// 내부 저장 필드
        /// </param>
        /// <param name="value">
        /// 변경 요청 값
        /// </param>
        /// <param name="propertyName">
        /// 변경된 Property 이름
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        private bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (Equals(
                    field,
                    value))
            {
                return false;
            }

            field =
                value;

            OnPropertyChanged(
                propertyName);

            return true;
        }

        #endregion

        #region [Network Properties]

        public string McbIpAddress
        {
            get => _networkPanel.McbIpAddress;
            set
            {
                if (_networkPanel.SetMcbIpAddress(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public int McbPort
        {
            get => _networkPanel.McbPort;
            set
            {
                if (_networkPanel.SetMcbPort(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public string ScbIpAddress
        {
            get => _networkPanel.ScbIpAddress;
            set
            {
                if (_networkPanel.SetScbIpAddress(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public int ScbPort
        {
            get => _networkPanel.ScbPort;
            set
            {
                if (_networkPanel.SetScbPort(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public string RadarUdpIpAddress
        {
            get => _networkPanel.RadarUdpIpAddress;
            set
            {
                if (_networkPanel.SetRadarUdpIpAddress(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public int RadarUdpLocalPort
        {
            get => _networkPanel.RadarUdpLocalPort;
            set
            {
                if (_networkPanel.SetRadarUdpLocalPort(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public string MqHostName
        {
            get => _networkPanel.MqHostName;
            set
            {
                if (_networkPanel.SetMqHostName(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public int MqPort
        {
            get => _networkPanel.MqPort;
            set
            {
                if (_networkPanel.SetMqPort(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [MQ Properties]

        public string MqStatusText
        {
            get => _statusPanel.MqStatusText;
            private set
            {
                if (_statusPanel.SetMqStatusText(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public string LastMqMessageText
        {
            get => _statusPanel.LastMqMessageText;
            private set
            {
                if (_statusPanel.SetLastMqMessageText(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [Connection Status Properties]

        public string McbConnectionStatusText =>
            _uiStateService.GetConnectionStatusText(
                _connectionPanel.McbConnectionState);

        public Brush McbConnectionStatusBrush =>
            _uiStateService.GetConnectionStatusBrush(
                _connectionPanel.McbConnectionState);

        public string ScbConnectionStatusText =>
            _uiStateService.GetConnectionStatusText(
                _connectionPanel.ScbConnectionState);

        public Brush ScbConnectionStatusBrush =>
            _uiStateService.GetConnectionStatusBrush(
                _connectionPanel.ScbConnectionState);

        public string RadarUdpConnectionStatusText =>
            _uiStateService.GetConnectionStatusText(
                _connectionPanel.RadarUdpConnectionState);

        public Brush RadarUdpConnectionStatusBrush =>
            _uiStateService.GetConnectionStatusBrush(
                _connectionPanel.RadarUdpConnectionState);

        public string RabbitMqConnectionStatusText =>
            _uiStateService.GetConnectionStatusText(
                _connectionPanel.RabbitMqConnectionState);

        public Brush RabbitMqConnectionStatusBrush =>
            _uiStateService.GetConnectionStatusBrush(
                _connectionPanel.RabbitMqConnectionState);

        public bool IsDeviceControlEnabled =>
            _uiStateService.IsDeviceControlEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.IsDeviceConnecting,
                _connectionPanel.IsDeviceDisconnecting,
                _connectionPanel.IsHomePositionMoving);

        public bool IsDeviceConnectionSettingEnabled =>
            _uiStateService.IsDeviceConnectionSettingEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.IsDeviceConnecting,
                _connectionPanel.IsDeviceDisconnecting,
                _connectionPanel.IsHomePositionMoving);

        public bool IsDeviceControlTabEnabled =>
            _uiStateService.IsDeviceControlTabEnabled(
                _connectionPanel.IsHomePositionMoving);

        public bool IsPanTiltSpeedEnabled =>
            _uiStateService.IsPanTiltSpeedEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.IsHomePositionMoving);

        public bool IsManualPanTiltControlEnabled =>
            _uiStateService
                .IsManualPanTiltControlEnabled(
                    _connectionPanel.McbConnectionState,
                    _connectionPanel.ScbConnectionState,
                    _connectionPanel.IsDeviceConnecting,
                    _connectionPanel.IsDeviceDisconnecting,
                    _connectionPanel.IsHomePositionMoving,
                    PtzControlModeText);

        public bool IsDeviceConnectButtonEnabled =>
            _uiStateService.IsDeviceConnectButtonEnabled(
                _connectionPanel.IsDeviceConnecting,
                _connectionPanel.IsDeviceDisconnecting,
                _connectionPanel.IsHomePositionMoving);

        public bool IsDeviceDisconnectButtonEnabled =>
            _uiStateService.IsDeviceDisconnectButtonEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.IsDeviceDisconnecting,
                _connectionPanel.IsHomePositionMoving);

        public bool IsRadarUdpStartButtonEnabled =>
            _uiStateService.IsRadarUdpStartButtonEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.RadarUdpConnectionState);

        public bool IsRadarUdpStopButtonEnabled =>
            _uiStateService.IsRadarUdpStopButtonEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.RadarUdpConnectionState);

        public bool IsRadarUdpConnectionSettingEnabled =>
            _uiStateService.IsRadarUdpConnectionSettingEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.RadarUdpConnectionState);

        public bool IsRabbitMqStartButtonEnabled =>
            _uiStateService.IsRabbitMqStartButtonEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.RabbitMqConnectionState);

        public bool IsRabbitMqStopButtonEnabled =>
            _uiStateService.IsRabbitMqStopButtonEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.RabbitMqConnectionState);

        public bool IsRabbitMqConnectionSettingEnabled =>
            _uiStateService.IsRabbitMqConnectionSettingEnabled(
                _connectionPanel.McbConnectionState,
                _connectionPanel.ScbConnectionState,
                _connectionPanel.RabbitMqConnectionState);

        #endregion

        #region [Main Status Properties]

        public string MainStatusText
        {
            get => _statusPanel.MainStatusText;
            private set
            {
                if (_statusPanel.SetMainStatusText(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public string OperationModeText
        {
            get => _statusPanel.OperationModeText;
            private set
            {
                if (_statusPanel.SetOperationModeText(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public string PtzControlModeText
        {
            get => _statusPanel.PtzControlModeText;
            private set
            {
                if (_statusPanel.SetPtzControlModeText(value))
                {
                    OnPropertyChanged();

                    OnPropertyChanged(
                        nameof(IsManualPanTiltControlEnabled));
                }

            }

        }

        #endregion

        #region [Camera Status Properties]

        public double CurrentPan
        {
            get => _cameraPanel.CurrentPan;
            private set
            {
                if (_cameraPanel.SetCurrentPan(value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPanDisplayText));
                }

            }

        }

        public double CurrentTilt
        {
            get => _cameraPanel.CurrentTilt;
            private set
            {
                if (_cameraPanel.SetCurrentTilt(value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentTiltDisplayText));
                }

            }

        }

        public string CurrentPanDisplayText =>
            _cameraPanel.CurrentPanDisplayText;

        public string CurrentTiltDisplayText =>
            _cameraPanel.CurrentTiltDisplayText;

        public double PanTiltSpeedLevel
        {
            get => _cameraPanel.PanTiltSpeedLevel;
            set
            {
                if (!_cameraPanel.SetPanTiltSpeedLevel(value))
                {
                    return;
                }

                OnPropertyChanged();

                _ptzCommandProxy
                    .ApplyCurrentPanTiltMoveSpeed(
                        PanTiltSpeedLevel);
            }

        }

        public double CurrentZoom
        {
            get => _cameraPanel.CurrentZoom;
            private set
            {
                if (_cameraPanel.SetCurrentZoom(value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentZoomDisplayText));
                }

            }

        }

        public double CurrentZoomRatio
        {
            get => _cameraPanel.CurrentZoomRatio;
            private set
            {
                if (_cameraPanel.SetCurrentZoomRatio(value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentZoomDisplayText));
                }

            }

        }

        public string CurrentZoomDisplayText =>
            _cameraPanel.CurrentZoomDisplayText;

        public double CurrentFocus
        {
            get => _cameraPanel.CurrentFocus;
            private set
            {
                if (_cameraPanel.SetCurrentFocus(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [Camera Control Input Properties]

        public bool IsPanTurnViaZeroMode
        {
            get => _cameraPanel.PanTurnMode == Ads1000PanTurnMode.ViaZero;
            set
            {
                if (!value ||
                    _cameraPanel.PanTurnMode == Ads1000PanTurnMode.ViaZero)
                {
                    return;
                }

                _cameraPanel
                    .SetPanTurnMode(
                        Ads1000PanTurnMode.ViaZero);

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPanTurnShortMode));
            }

        }

        public bool IsPanTurnShortMode
        {
            get => _cameraPanel.PanTurnMode == Ads1000PanTurnMode.Short;
            set
            {
                if (!value ||
                    _cameraPanel.PanTurnMode == Ads1000PanTurnMode.Short)
                {
                    return;
                }

                _cameraPanel
                    .SetPanTurnMode(
                        Ads1000PanTurnMode.Short);

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPanTurnViaZeroMode));
            }

        }

        public double? PanAbsoluteValue
        {
            get => _cameraPanel.PanAbsoluteValue;
            set
            {
                if (_cameraPanel.SetPanAbsoluteValue(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public double? TiltAbsoluteValue
        {
            get => _cameraPanel.TiltAbsoluteValue;
            set
            {
                if (_cameraPanel.SetTiltAbsoluteValue(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public double? PanRelativeValue
        {
            get => _cameraPanel.PanRelativeValue;
            set
            {
                if (_cameraPanel.SetPanRelativeValue(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public double? TiltRelativeValue
        {
            get => _cameraPanel.TiltRelativeValue;
            set
            {
                if (_cameraPanel.SetTiltRelativeValue(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public int? ZoomPositionValue
        {
            get => _cameraPanel.ZoomPositionValue;
            set
            {
                if (_cameraPanel.SetZoomPositionValue(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public double? ZoomRatioValue
        {
            get => _cameraPanel.ZoomRatioValue;
            set
            {
                if (_cameraPanel.SetZoomRatioValue(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        public int? FocusPositionValue
        {
            get => _cameraPanel.FocusPositionValue;
            set
            {
                if (_cameraPanel.SetFocusPositionValue(value))
                {
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [Image Properties]

        public BitmapSource EOCameraImage
        {
            get => _eoCameraImage;
            private set => SetProperty(
                ref _eoCameraImage,
                value);
        }

        #endregion

        #region [PTZ Result Proxy Methods]

        /// <summary>
        /// [PTZ] 제어 모드 변경 이벤트 처리
        /// </summary>
        /// <param name="ptzControlMode">
        /// [PTZ] 제어 모드
        /// </param>
        private void OnPtzControlModeChanged(
            string ptzControlMode)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _ptzControlResultApplier
                    .SetPtzControlMode(
                        ptzControlMode);
            }));

        }

        /// <summary>
        /// [PTZ Control] 처리 결과 반영 요청
        /// </summary>
        /// <param name="result">
        /// [PTZ Control] 처리 결과
        /// </param>
        private void ApplyPtzControlWorkflowResult(
            PtzControlWorkflowResult result)
        {
            _ptzControlResultApplier
                .Apply(
                    result);
        }

        /// <summary>
        /// [MCB] / [SCB] 전체 연결 여부 조회
        /// </summary>
        /// <returns>
        /// 전체 연결 여부
        /// </returns>
        private bool IsDeviceFullyConnected()
        {
            return _connectionPanel.McbConnectionState == ConnectionState.Connected &&
                   _connectionPanel.ScbConnectionState == ConnectionState.Connected;
        }

        #endregion

        #region [ADS1000 Status Receive Methods]

        /// <summary>
        /// [MCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [MCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 Packet 처리 / 파싱 / 상태 적용 Controller 호출은
        /// [Ads1000StatusWorkflow]에 위임한다.
        /// </summary>
        /// <param name="packet">
        /// [MCB] 수신 [Packet]
        /// </param>
        /// <param name="receivedTime">
        /// 수신 시간
        /// </param>
        private void OnMcbMessageReceived(
            byte[] packet,
            DateTime receivedTime)
        {
            _ads1000StatusWorkflow
                .ProcessReceivedPacket(
                    "MCB",
                    packet,
                    ApplyAds1000StatusResult);
        }

        /// <summary>
        /// [SCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [SCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 Packet 처리 / 파싱 / 상태 적용 Controller 호출은
        /// [Ads1000StatusWorkflow]에 위임한다.
        /// </summary>
        /// <param name="packet">
        /// [SCB] 수신 [Packet]
        /// </param>
        /// <param name="receivedTime">
        /// 수신 시간
        /// </param>
        private void OnScbMessageReceived(
            byte[] packet,
            DateTime receivedTime)
        {
            _ads1000StatusWorkflow
                .ProcessReceivedPacket(
                    "SCB",
                    packet,
                    ApplyAds1000StatusResult);
        }

        /// <summary>
        /// [ADS1000] 상태 적용 결과 화면 반영
        /// 
        /// [Ads1000StatusWorkflow]에서 처리된 상태 적용 결과를 기준으로
        /// 화면 Binding 속성과 내부 상태값을 갱신한다.
        /// </summary>
        /// <param name="result">
        /// [ADS1000] 상태 적용 Controller 결과
        /// </param>
        private void ApplyAds1000StatusResult(
            Ads1000StatusApplyControllerResult result)
        {
            _ads1000StatusResultApplier
                .Apply(
                    result);
        }
        #endregion
    }

}
