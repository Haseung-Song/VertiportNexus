using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.ADS1000;
using VertiportNexus.Services.Camera;
using VertiportNexus.Services.Communication.TCP;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel]
    /// 
    /// 메인 클래스 역할:
    /// 1. [MCB] / [SCB] [TCP] 연결 상태 관리
    /// 2. [ADS1000] [Packet Builder]를 통한 장비 제어 [Packet] 생성
    /// 3. [MCB] [Pan] / [Tilt] 제어 0
    /// 4. [SCB] [Zoom] / [Focus] 제어
    /// 5. [Console] 로그 및 [XAML] 상태 표시
    /// 
    /// 실제 [TCP] 송수신은 [TcpClientService]에서 처리하고,
    /// [ViewModel]은 [Command] 연결과 화면 상태 갱신만 담당한다.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region [Enum Type]

        /// <summary>
        /// 장비 연결 상태
        /// </summary>
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
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

        #endregion

        #region [Fields]

        #region [Service Fields]

        /// <summary>
        /// [MCB] [TCP] 통신 서비스
        /// </summary>
        private readonly TcpClientService _mcbTcpClientService;

        /// <summary>
        /// [SCB] [TCP] 통신 서비스
        /// </summary>
        private readonly TcpClientService _scbTcpClientService;

        /// <summary>
        /// [ADS1000] 장비 [TCP] 연결 서비스
        /// </summary>
        private readonly Ads1000ConnectionService _ads1000ConnectionService;

        /// <summary>
        /// [ADS1000] [Camera] 제어 서비스
        /// </summary>
        private readonly Ads1000CameraControlService _ads1000CameraControlService;

        /// <summary>
        /// [ADS1000] 상태 [Packet] 처리 서비스
        /// </summary>
        private readonly Ads1000StatusService _ads1000StatusService;

        #endregion

        #region [Builder Fields]

        /// <summary>
        /// [MCB] [Pan] / [Tilt] [Packet] 생성 객체
        /// </summary>
        private readonly Ads1000McbPacketBuilder _mcbPacketBuilder;

        /// <summary>
        /// [SCB] [Zoom] / [Focus] [Packet] 생성 객체
        /// </summary>
        private readonly Ads1000ScbPacketBuilder _scbPacketBuilder;

        #endregion

        #region [Network Setting Fields]

        /// <summary>
        /// [MCB] 연결 대상 [IP]
        /// </summary>
        private string _mcbIpAddress = DEFAULT_DEVICE_IP_ADDRESS;

        /// <summary>
        /// [MCB] 연결 대상 [Port]
        /// </summary>
        private int _mcbPort = DEFAULT_MCB_PORT;

        /// <summary>
        /// [SCB] 연결 대상 [IP]
        /// </summary>
        private string _scbIpAddress = DEFAULT_DEVICE_IP_ADDRESS;

        /// <summary>
        /// [SCB] 연결 대상 [Port]
        /// </summary>
        private int _scbPort = DEFAULT_SCB_PORT;

        /// <summary>
        /// 기존 [UDP] UI 바인딩 호환용 [Port]
        /// 
        /// 현재는 [TCP] 직접 연결 구조이므로 실제 제어에는 사용하지 않는다.
        /// </summary>
        private int _udpLocalReceivePort = 5005;

        #endregion

        #region [Status Fields]

        /// <summary>
        /// [MQ] 상태 표시 문자열
        /// </summary>
        private string _mqStatusText = "MQ Not Used";

        /// <summary>
        /// 마지막 [MQ] 수신 메시지 표시 문자열
        /// </summary>
        private string _lastMqMessageText = string.Empty;

        /// <summary>
        /// 장비 연결 진행 여부
        /// 
        /// 현재 [MCB] / [SCB] [TCP Connect] 수행 중이면
        /// 중복 연결 요청을 방지한다.
        /// </summary>
        private bool _isDeviceConnecting;

        /// <summary>
        /// 장비 연결 해제 진행 여부
        /// </summary>
        private bool _isDeviceDisconnecting;

        /// <summary>
        /// [MCB] 연결 상태
        /// </summary>
        private ConnectionState _mcbConnectionState =
            ConnectionState.Disconnected;

        /// <summary>
        /// [SCB] 연결 상태
        /// </summary>
        private ConnectionState _scbConnectionState =
            ConnectionState.Disconnected;

        /// <summary>
        /// 마지막 송신 [Packet] 표시 문자열
        /// </summary>
        private string _lastUdpSendPacketText = string.Empty;

        /// <summary>
        /// 마지막 수신 [Packet] 표시 문자열
        /// </summary>
        private string _lastUdpReceivePacketText = string.Empty;

        /// <summary>
        /// 프로그램 전체 상태 표시 문자열
        /// </summary>
        private string _mainStatusText;

        /// <summary>
        /// 현재 운용 모드 표시 문자열
        /// </summary>
        private string _operationModeText;

        #endregion

        #region [Camera State Fields]

        /// <summary>
        /// 현재 [Pan] 값
        /// </summary>
        private double _currentPan;

        /// <summary>
        /// 현재 [Tilt] 값
        /// </summary>
        private double _currentTilt;

        /// <summary>
        /// 현재 [Zoom] 값
        /// </summary>
        private double _currentZoom;

        /// <summary>
        /// 현재 [Focus] 값
        /// </summary>
        private double _currentFocus;

        #endregion

        #region [Image Binding Fields - Test Only]

        /// <summary>
        /// [EO] 영상 출력용 [Image]
        /// </summary>
        private BitmapSource _eoCameraImage;

        #endregion

        #region [Camera Service Fields]

        /// <summary>
        /// [EO] [Camera] 영상 서비스
        /// </summary>
        private readonly EoCameraService _eoCameraService;

        /// <summary>
        /// [EO] [RTSP] 테스트 주소
        /// </summary>
        private string _eoRtspAddress =
            "rtsp://service:Xhddlf1!@192.168.0.110:554/rtsp_tunnel";

        #endregion

        #endregion

        #region [ICommand]

        /// <summary>
        /// [TCP] 연결 요청 [Command]
        /// </summary>
        public ICommand ConnectTcpCommand { get; }

        /// <summary>
        /// [TCP] 연결 해제 요청 [Command]
        /// </summary>
        public ICommand DisconnectTcpCommand { get; }

        /// <summary>
        /// [TCP] 수신 시작 요청 [Command]
        /// </summary>
        public ICommand StartTcpReceiveCommand { get; }

        /// <summary>
        /// [TCP] 수신 중지 요청 [Command]
        /// </summary>
        public ICommand StopTcpReceiveCommand { get; }

        /// <summary>
        /// [TCP] 테스트 송신 요청 [Command]
        /// </summary>
        public ICommand SendTcpTestCommand { get; }

        /// <summary>
        /// [PT] 좌측 이동 요청 [Command]
        /// </summary>
        public ICommand PanLeftCommand { get; }

        /// <summary>
        /// [PT] 우측 이동 요청 [Command]
        /// </summary>
        public ICommand PanRightCommand { get; }

        /// <summary>
        /// [PT] 상향 이동 요청 [Command]
        /// </summary>
        public ICommand TiltUpCommand { get; }

        /// <summary>
        /// [PT] 하향 이동 요청 [Command]
        /// </summary>
        public ICommand TiltDownCommand { get; }

        /// <summary>
        /// [PT] 정지 요청 [Command]
        /// </summary>
        public ICommand StopMoveCommand { get; }

        /// <summary>
        /// [Zoom] 확대 요청 [Command]
        /// </summary>
        public ICommand ZoomInCommand { get; }

        /// <summary>
        /// [Zoom] 축소 요청 [Command]
        /// </summary>
        public ICommand ZoomOutCommand { get; }

        /// <summary>
        /// [Focus] Near 요청 [Command]
        /// </summary>
        public ICommand FocusNearCommand { get; }

        /// <summary>
        /// [Focus] Far 요청 [Command]
        /// </summary>
        public ICommand FocusFarCommand { get; }

        /// <summary>
        /// [Auto Focus] 요청 [Command]
        /// </summary>
        public ICommand AutoFocusCommand { get; }

        /// <summary>
        /// [Status] 조회 요청 [Command]
        /// </summary>
        public ICommand RequestStatusCommand { get; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MainViewModel] 생성자
        /// </summary>
        public MainViewModel()
        {
            #region [Service Initialize]

            /// <summary>
            /// [MCB] [TCP] 통신 서비스 생성
            /// </summary>
            _mcbTcpClientService =
                new TcpClientService("MCB");

            /// <summary>
            /// [SCB] [TCP] 통신 서비스 생성
            /// </summary>
            _scbTcpClientService =
                new TcpClientService("SCB");

            /// <summary>
            /// [MCB] 수신 이벤트 연결
            /// </summary>
            _mcbTcpClientService.MessageReceived +=
                OnMcbMessageReceived;

            /// <summary>
            /// [SCB] 수신 이벤트 연결
            /// </summary>
            _scbTcpClientService.MessageReceived +=
                OnScbMessageReceived;

            /// <summary>
            /// [ADS1000] 장비 [TCP] 연결 서비스 생성
            /// </summary>
            _ads1000ConnectionService =
                new Ads1000ConnectionService(
                    _mcbTcpClientService,
                    _scbTcpClientService);

            /// <summary>
            /// [EO] [Camera] 영상 서비스 생성
            /// </summary>
            _eoCameraService =
                new EoCameraService();

            /// <summary>
            /// [EO] 영상 [Frame] 수신 이벤트 연결
            /// </summary>
            _eoCameraService.FrameReceived +=
                OnEoCameraFrameReceived;

            /// <summary>
            /// [EO] 영상 상태 변경 이벤트 연결
            /// </summary>
            _eoCameraService.StatusChanged +=
                OnEoCameraStatusChanged;


            #endregion

            #region [Builder Initialize]

            /// <summary>
            /// [MCB] [Packet Builder] 생성
            /// </summary>
            _mcbPacketBuilder =
                new Ads1000McbPacketBuilder();

            /// <summary>
            /// [SCB] [Packet Builder] 생성
            /// </summary>
            _scbPacketBuilder =
                new Ads1000ScbPacketBuilder();

            #endregion

            #region [Control Service Initialize]

            /// <summary>
            /// [ADS1000] [Camera] 제어 서비스 생성
            /// </summary>
            _ads1000CameraControlService =
                new Ads1000CameraControlService(
                    _mcbTcpClientService,
                    _scbTcpClientService,
                    _mcbPacketBuilder,
                    _scbPacketBuilder);

            /// <summary>
            /// [ADS1000] [Packet] 송신 결과 이벤트 연결
            /// </summary>
            _ads1000CameraControlService.SendResultChanged +=
                OnAds1000SendResultChanged;

            #endregion

            #region [Status Service Initialize]

            /// <summary>
            /// [ADS1000] 상태 [Packet] 처리 서비스 생성
            /// </summary>
            _ads1000StatusService =
                new Ads1000StatusService();

            #endregion

            #region [Command Initialize]

            ConnectTcpCommand =
                new RelayCommand(ConnectMq);

            DisconnectTcpCommand =
                new RelayCommand(DisconnectMq);

            /// <summary>
            /// [MCB] / [SCB] 직접 [TCP] 연결 시작 [Command]
            /// </summary>
            StartTcpReceiveCommand =
                new AsyncRelayCommand(ConnectDevicesAsync);

            /// <summary>
            /// [MCB] / [SCB] 직접 [TCP] 연결 해제 [Command]
            /// </summary>
            StopTcpReceiveCommand =
                new AsyncRelayCommand(DisconnectDevicesAsync);

            SendTcpTestCommand =
                new RelayCommand(
                    _ads1000CameraControlService.SendVersionQuery);

            PanLeftCommand =
                new RelayCommand(
                    _ads1000CameraControlService.PanLeft);

            PanRightCommand =
                new RelayCommand(
                    _ads1000CameraControlService.PanRight);

            TiltUpCommand =
                new RelayCommand(
                    _ads1000CameraControlService.TiltUp);

            TiltDownCommand =
                new RelayCommand(
                    _ads1000CameraControlService.TiltDown);

            StopMoveCommand =
                new RelayCommand(
                    _ads1000CameraControlService.StopMove);

            ZoomInCommand =
                new RelayCommand(
                    _ads1000CameraControlService.ZoomIn);

            ZoomOutCommand =
                new RelayCommand(
                    _ads1000CameraControlService.ZoomOut);

            FocusNearCommand =
                new RelayCommand(
                    _ads1000CameraControlService.FocusNear);

            FocusFarCommand =
                new RelayCommand(
                    _ads1000CameraControlService.FocusFar);

            AutoFocusCommand =
                new RelayCommand(
                    _ads1000CameraControlService.AutoFocus);

            RequestStatusCommand =
                new RelayCommand(
                    _ads1000CameraControlService.SendVersionQuery);

            #endregion

            #region [Default Initialize]

            InitializeDefaultValues();

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MAIN] ADS1000 Direct TCP Test Initialize Complete");
            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[MAIN] MCB Target : " + McbIpAddress + ":" + McbPort);
            Console.WriteLine("[MAIN] SCB Target : " + ScbIpAddress + ":" + ScbPort);
            ConsoleLogHelper.PrintLine();

            #endregion
        }

        #endregion

        #region [Bindable Properties]

        public string McbIpAddress
        {
            get => _mcbIpAddress;
            set
            {
                if (_mcbIpAddress != value)
                {
                    _mcbIpAddress = value;
                    OnPropertyChanged();
                }

            }

        }

        public int McbPort
        {
            get => _mcbPort;
            set
            {
                if (_mcbPort != value)
                {
                    _mcbPort = value;
                    OnPropertyChanged();
                }

            }

        }

        public string ScbIpAddress
        {
            get => _scbIpAddress;
            set
            {
                if (_scbIpAddress != value)
                {
                    _scbIpAddress = value;
                    OnPropertyChanged();
                }

            }

        }

        public int ScbPort
        {
            get => _scbPort;
            set
            {
                if (_scbPort != value)
                {
                    _scbPort = value;
                    OnPropertyChanged();
                }

            }

        }


        public int UdpLocalReceivePort
        {
            get => _udpLocalReceivePort;
            set
            {
                if (_udpLocalReceivePort != value)
                {
                    _udpLocalReceivePort = value;
                    OnPropertyChanged();
                }

            }

        }

        public string MqStatusText
        {
            get => _mqStatusText;
            private set
            {
                if (_mqStatusText != value)
                {
                    _mqStatusText = value;
                    OnPropertyChanged();
                }

            }

        }

        public string LastMqMessageText
        {
            get => _lastMqMessageText;
            private set
            {
                if (_lastMqMessageText != value)
                {
                    _lastMqMessageText = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [MCB] 연결 상태 표시
        /// </summary>
        public string McbConnectionStatusText
        {
            get
            {
                switch (_mcbConnectionState)
                {
                    case ConnectionState.Connected:
                        return "● Connected";

                    case ConnectionState.Connecting:
                        return "● Connecting";

                    default:
                        return "● Disconnected";
                }

            }

        }

        /// <summary>
        /// [MCB] 연결 상태 색상
        /// </summary>
        public Brush McbConnectionStatusBrush
        {
            get
            {
                switch (_mcbConnectionState)
                {
                    case ConnectionState.Connected:
                        return Brushes.LimeGreen;

                    case ConnectionState.Connecting:
                        return Brushes.Gold;

                    default:
                        return Brushes.IndianRed;
                }

            }

        }

        /// <summary>
        /// [SCB] 연결 상태 표시
        /// </summary>
        public string ScbConnectionStatusText
        {
            get
            {
                switch (_scbConnectionState)
                {
                    case ConnectionState.Connected:
                        return "● Connected";

                    case ConnectionState.Connecting:
                        return "● Connecting";

                    default:
                        return "● Disconnected";
                }

            }

        }

        /// <summary>
        /// [SCB] 연결 상태 색상
        /// </summary>
        public Brush ScbConnectionStatusBrush
        {
            get
            {
                switch (_scbConnectionState)
                {
                    case ConnectionState.Connected:
                        return Brushes.LimeGreen;

                    case ConnectionState.Connecting:
                        return Brushes.Gold;

                    default:
                        return Brushes.IndianRed;
                }

            }

        }

        public string LastUdpSendPacketText
        {
            get => _lastUdpSendPacketText;
            private set
            {
                if (_lastUdpSendPacketText != value)
                {
                    _lastUdpSendPacketText = value;
                    OnPropertyChanged();
                }

            }

        }

        public string LastUdpReceivePacketText
        {
            get => _lastUdpReceivePacketText;
            private set
            {
                if (_lastUdpReceivePacketText != value)
                {
                    _lastUdpReceivePacketText = value;
                    OnPropertyChanged();
                }

            }

        }

        public string MainStatusText
        {
            get => _mainStatusText;
            private set
            {
                if (_mainStatusText != value)
                {
                    _mainStatusText = value;
                    OnPropertyChanged();
                }

            }

        }

        public string OperationModeText
        {
            get => _operationModeText;
            private set
            {
                if (_operationModeText != value)
                {
                    _operationModeText = value;
                    OnPropertyChanged();
                }

            }

        }

        public double CurrentPan
        {
            get => _currentPan;
            private set
            {
                if (_currentPan != value)
                {
                    _currentPan = value;
                    OnPropertyChanged();
                }

            }

        }

        public double CurrentTilt
        {
            get => _currentTilt;
            private set
            {
                if (_currentTilt != value)
                {
                    _currentTilt = value;
                    OnPropertyChanged();
                }

            }

        }

        public double PanTiltSpeedLevel
        {
            get => _ads1000CameraControlService.PanTiltSpeedLevel;
        }

        public double CurrentZoom
        {
            get => _currentZoom;
            private set
            {
                if (_currentZoom != value)
                {
                    _currentZoom = value;
                    OnPropertyChanged();
                }

            }

        }

        public double CurrentFocus
        {
            get => _currentFocus;
            private set
            {
                if (_currentFocus != value)
                {
                    _currentFocus = value;
                    OnPropertyChanged();
                }

            }

        }

        public BitmapSource EOCameraImage
        {
            get => _eoCameraImage;
            private set
            {
                if (_eoCameraImage != value)
                {
                    _eoCameraImage = value;
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [Initialize]

        /// <summary>
        /// 기본 상태값 초기화
        /// </summary>
        private void InitializeDefaultValues()
        {
            MainStatusText =
                "DISCONNECTED";

            OperationModeText =
                "STANDBY";

            MqStatusText =
                "MQ Not Used";

            McbIpAddress =
                DEFAULT_DEVICE_IP_ADDRESS;

            McbPort =
                DEFAULT_MCB_PORT;

            ScbIpAddress =
                DEFAULT_DEVICE_IP_ADDRESS;

            ScbPort =
                DEFAULT_SCB_PORT;
        }

        #endregion

        #region [MQ Methods]

        /// <summary>
        /// [MQ] 연결 처리
        /// 
        /// 현재 단계에서는 [MQ] 실제 연동 전이므로
        /// 상태값과 로그만 표시한다.
        /// </summary>
        private void ConnectMq()
        {
            MqStatusText = "MQ Not Used";

            Console.WriteLine();
            Console.WriteLine("[MQ] Current step does not use MQ");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [MQ] 연결 해제 처리
        /// 
        /// 현재 단계에서는 [MQ] 실제 연동 전이므로
        /// 상태값과 로그만 표시한다.
        /// </summary>
        private void DisconnectMq()
        {
            MqStatusText = "MQ Not Used";

            Console.WriteLine();
            Console.WriteLine("[MQ] Current step does not use MQ");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [TCP Connection Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결
        /// </summary>
        private async Task ConnectDevicesAsync()
        {
            /// <summary>
            /// 장비 연결 진행 중이면 중복 연결 방지
            /// </summary>
            if (_isDeviceConnecting)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Connect Ignored : Connecting");
                Console.WriteLine();

                return;
            }

            /// <summary>
            /// 이미 [MCB] / [SCB] 중 하나라도 연결되어 있으면 중복 연결 방지
            /// </summary>
            if (_mcbConnectionState == ConnectionState.Connected ||
                _scbConnectionState == ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Connect Ignored : Already Connected");
                Console.WriteLine();

                return;
            }

            try
            {
                MainStatusText =
                     "CONNECTING...";

                OperationModeText =
                    "DEVICE CONNECTING...";

                _isDeviceConnecting =
                    true;

                /// <summary>
                /// [MCB] / [SCB] 연결 시도 상태 표시
                /// </summary>
                SetDeviceConnectionState(
                    ConnectionState.Connecting,
                    ConnectionState.Connecting);

                Ads1000ConnectionResult connectionResult =
                    await _ads1000ConnectionService.ConnectAsync(
                        McbIpAddress,
                        McbPort,
                        ScbIpAddress,
                        ScbPort);

                ApplyDeviceConnectionResult(
                    connectionResult);

                /// <summary>
                /// [EO] [RTSP] 테스트 영상 연결
                /// </summary>
                _eoCameraService.Connect(
                    _eoRtspAddress);
            }
            finally
            {
                _isDeviceConnecting =
                    false;
            }

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
            _mcbConnectionState =
                mcbConnectionState;

            _scbConnectionState =
                scbConnectionState;

            OnPropertyChanged(nameof(McbConnectionStatusText));
            OnPropertyChanged(nameof(McbConnectionStatusBrush));

            OnPropertyChanged(nameof(ScbConnectionStatusText));
            OnPropertyChanged(nameof(ScbConnectionStatusBrush));
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
                    "CONNECTED";

                OperationModeText =
                    "ADS1000 CONTROL";
            }
            else if (connectionResult.IsMcbConnected)
            {
                MainStatusText =
                    "PARTIAL CONNECTED";

                OperationModeText =
                    "MCB ONLY";
            }
            else if (connectionResult.IsScbConnected)
            {
                MainStatusText =
                    "PARTIAL CONNECTED";

                OperationModeText =
                    "SCB ONLY";
            }
            else
            {
                MainStatusText =
                    "DISCONNECTED";

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
        }

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결 해제
        /// </summary>
        private Task DisconnectDevicesAsync()
        {
            if (_isDeviceDisconnecting)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Disconnect Ignored : Disconnecting");
                Console.WriteLine();

                return Task.CompletedTask;
            }

            /// <summary>
            /// 이미 연결 해제 상태이면 중복 해제 방지
            /// </summary>
            if (_mcbConnectionState == ConnectionState.Disconnected &&
                _scbConnectionState == ConnectionState.Disconnected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Disconnect Ignored : Already Disconnected");
                Console.WriteLine();

                return Task.CompletedTask;
            }

            try
            {
                _isDeviceDisconnecting =
                    true;

                _ads1000ConnectionService.Disconnect();

                MainStatusText =
                    "DISCONNECTED";

                OperationModeText =
                    "STANDBY";

                /// <summary>
                /// 장비 연결 해제 상태 반영
                /// </summary>
                SetDeviceConnectionState(
                    ConnectionState.Disconnected,
                    ConnectionState.Disconnected);

                /// <summary>
                /// [EO] [RTSP] 테스트 영상 연결 해제
                /// </summary>
                _eoCameraService.Disconnect();
            }
            finally
            {
                _isDeviceDisconnecting =
                    false;
            }
            return Task.CompletedTask;
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

            if (sendResult.Packet != null &&
                sendResult.Packet.Length > 0)
            {
                LastUdpSendPacketText =
                    "[" + sendResult.DeviceName + "] " +
                    ConvertToHexString(
                        sendResult.Packet);
            }

        }

        #endregion

        #region [Receive Event Methods]

        /// <summary>
        /// [MCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [MCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 파싱은 [Ads1000StatusService]에서 처리한다.
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
            ProcessReceivedPacket(
                "MCB",
                packet);
        }

        /// <summary>
        /// [SCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [SCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 파싱은 [Ads1000StatusService]에서 처리한다.
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
            ProcessReceivedPacket(
                "SCB",
                packet);
        }

        /// <summary>
        /// [ADS1000] 수신 [Packet] 처리 결과 화면 반영
        /// </summary>
        /// <param name="deviceName">
        /// 수신 장비 이름
        /// </param>
        /// <param name="packet">
        /// 수신 [Packet]
        /// </param>
        private void ProcessReceivedPacket(
            string deviceName,
            byte[] packet)
        {
            Ads1000StatusResult statusResult =
                _ads1000StatusService.ProcessReceivedPacket(
                    deviceName,
                    packet);

            LastUdpReceivePacketText =
                "[" + statusResult.DeviceName + "] " +
                statusResult.PacketText;

            if (!statusResult.IsValid)
            {
                return;
            }

            ApplyParsedStatusValue(
                statusResult.ParsedPacket);
        }

        #endregion

        #region [EO Camera Event Methods]

        /// <summary>
        /// [EO] 영상 [Frame] 수신 처리
        /// 
        /// [EoCameraService]에서 전달받은 [BitmapSource]를
        /// [XAML]에 바인딩된 [EOCameraImage]에 반영한다.
        /// </summary>
        /// <param name="bitmap">
        /// [EO] 영상 [Frame]
        /// </param>
        private void OnEoCameraFrameReceived(
            BitmapSource bitmap)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                EOCameraImage =
                    bitmap;
            });

        }

        /// <summary>
        /// [EO] 영상 상태 변경 처리
        /// 
        /// [EoCameraService]에서 전달받은 상태 메시지를
        /// [CameraStatusText]에 반영한다.
        /// </summary>
        /// <param name="statusText">
        /// [EO] 영상 상태 문자열
        /// </param>
        private void OnEoCameraStatusChanged(
            string statusText)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (statusText == "EO RTSP Connected")
                {
                    OperationModeText =
                        "SURVEILLANCE MODE";
                }
                else if (statusText == "EO RTSP Error" ||
                         statusText == "EO RTSP Connect Failed")
                {
                    OperationModeText =
                        "CAMERA ERROR MODE";
                }

            });

        }

        #endregion

        #region [Camera Control Methods]

        /// <summary>
        /// [Pan] 왼쪽 이동
        /// </summary>
        public void StartPanLeftMove()
        {
            _ads1000CameraControlService.PanLeft();
        }

        /// <summary>
        /// [Pan] 오른쪽 이동
        /// </summary>
        public void StartPanRightMove()
        {
            _ads1000CameraControlService.PanRight();
        }

        /// <summary>
        /// [Tilt] 위쪽 이동
        /// </summary>
        public void StartTiltUpMove()
        {
            _ads1000CameraControlService.TiltUp();
        }

        /// <summary>
        /// [Tilt] 아래쪽 이동
        /// </summary>
        public void StartTiltDownMove()
        {
            _ads1000CameraControlService.TiltDown();
        }

        /// <summary>
        /// [Zoom] 확대
        /// </summary>
        public void StartZoomInMove()
        {
            _ads1000CameraControlService.ZoomIn();
        }

        /// <summary>
        /// [Zoom] 축소
        /// </summary>
        public void StartZoomOutMove()
        {
            _ads1000CameraControlService.ZoomOut();
        }

        /// <summary>
        /// [Focus] Near
        /// </summary>
        public void StartFocusNearMove()
        {
            _ads1000CameraControlService.FocusNear();
        }

        /// <summary>
        /// [Focus] Far
        /// </summary>
        public void StartFocusFarMove()
        {
            _ads1000CameraControlService.FocusFar();
        }

        public void StopContinuousMove()
        {
            _ads1000CameraControlService.StopMove();
        }

        #endregion

        #region [Status Apply Methods]

        /// <summary>
        /// [ADS1000] 파싱 상태값을 화면 표시용 속성에 반영
        /// </summary>
        private void ApplyParsedStatusValue(
            Ads1000ParsedPacket parsedPacket)
        {
            if (parsedPacket.HasPanValue)
            {
                CurrentPan =
                    Clamp(
                        parsedPacket.PanValue,
                        -180,
                        180);
            }

            if (parsedPacket.HasTiltValue)
            {
                CurrentTilt =
                    Clamp(
                        parsedPacket.TiltValue,
                        -95,
                        95);
            }

            if (parsedPacket.HasZoomValue)
            {
                CurrentZoom =
                    Clamp(
                        parsedPacket.ZoomValue,
                        0,
                        1000);
            }

            if (parsedPacket.HasFocusValue)
            {
                CurrentFocus =
                    Clamp(
                        parsedPacket.FocusValue,
                        0,
                        1000);
            }
        }

        #endregion

        #region [Utility]

        /// <summary>
        /// [byte[]] 데이터를 [HEX] 문자열로 변환
        /// </summary>
        private string ConvertToHexString(
            byte[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                return string.Empty;
            }

            return BitConverter
                .ToString(data)
                .Replace("-", " ");
        }

        /// <summary>
        /// 값 범위 제한
        /// </summary>
        private double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        #endregion

        #region [INotifyPropertyChanged]

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

}
