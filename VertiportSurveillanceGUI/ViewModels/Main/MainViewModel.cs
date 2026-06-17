using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VertiportSurveillanceGUI.Common;
using VertiportSurveillanceGUI.Converters;
using VertiportSurveillanceGUI.Models.ADS1000;
using VertiportSurveillanceGUI.Services.ADS1000;
using VertiportSurveillanceGUI.Services.Camera;
using VertiportSurveillanceGUI.Services.Communication.TCP;
using VertiportSurveillanceGUI.Services.Communication.Video;

namespace VertiportSurveillanceGUI.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel]
    /// 
    /// 메인 클래스 역할:
    /// 1. [MCB] / [SCB] [TCP] 연결 상태 관리
    /// 2. [ADS1000] [Packet Builder]를 통한 장비 제어 [Packet] 생성
    /// 3. [MCB] [Pan] / [Tilt] 제어
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

        #region [Parser Fields]

        /// <summary>
        /// [ADS1000] 수신 [Packet] 파싱 객체
        /// 
        /// [MCB] / [SCB]에서 수신되는 [AA AA] 기반 응답 [Packet]을
        /// [Cmd1] / [Length] / [Data] / [Checksum] 구조로 파싱한다.
        /// </summary>
        private readonly Ads1000PacketParser _packetParser;

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
        /// 기존 [UDP] UI 바인딩 호환용 상태 표시 문자열
        /// 
        /// 현재는 [TCP] 상태 표시로 사용한다.
        /// </summary>
        private string _udpStatusText = "TCP Disconnected";

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
        /// 카메라 제어 상태 표시 문자열
        /// </summary>
        private string _cameraStatusText = "Camera Control Ready";

        /// <summary>
        /// 프로그램 전체 상태 표시 문자열
        /// </summary>
        private string _mainStatusText = "Ready";

        /// <summary>
        /// 현재 운용 모드 표시 문자열
        /// </summary>
        private string _operationModeText = "ADS1000 Direct TCP Test";

        /// <summary>
        /// 마지막 [ADS1000] 수신 [Packet] 로그 출력 시간
        /// 
        /// [MCB] / [SCB] 상태 [Packet]은 지속적으로 수신되므로,
        /// [Console] 도배 방지 목적으로 사용한다.
        /// </summary>
        private DateTime _lastAds1000ReceiveLogTime =
            DateTime.MinValue;

        /// <summary>
        /// [ADS1000] 수신 [Packet] 로그 출력 간격
        /// 
        /// 상태 [Packet] 로그는 [1초] 단위로 제한하여 출력한다.
        /// </summary>
        private const int Ads1000ReceiveLogIntervalSeconds = 1;

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

            #region [Parser Initialize]

            /// <summary>
            /// [ADS1000] 수신 [Packet] 파싱 객체 생성
            /// 
            /// [MCB] / [SCB] 수신 이벤트에서 공통으로 사용한다.
            /// </summary>
            _packetParser =
                new Ads1000PacketParser();

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

        public string UdpStatusText
        {
            get => _udpStatusText;
            private set
            {
                if (_udpStatusText != value)
                {
                    _udpStatusText = value;
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

        public string CameraStatusText
        {
            get => _cameraStatusText;
            private set
            {
                if (_cameraStatusText != value)
                {
                    _cameraStatusText = value;
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
            MainStatusText = "Ready";
            OperationModeText = "ADS1000 Direct TCP Test";

            MqStatusText = "MQ Not Used";
            UdpStatusText = "TCP Disconnected";
            CameraStatusText = "Camera Control Ready";

            McbIpAddress = DEFAULT_DEVICE_IP_ADDRESS;
            McbPort = DEFAULT_MCB_PORT;

            ScbIpAddress = DEFAULT_DEVICE_IP_ADDRESS;
            ScbPort = DEFAULT_SCB_PORT;
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
                UdpStatusText =
                    "MCB / SCB TCP Connected";

                CameraStatusText =
                    "Device Connected";
            }
            else if (connectionResult.IsMcbConnected)
            {
                UdpStatusText =
                    "MCB TCP Connected";

                CameraStatusText =
                    "Only MCB Connected";
            }
            else if (connectionResult.IsScbConnected)
            {
                UdpStatusText =
                    "SCB TCP Connected";

                CameraStatusText =
                    "Only SCB Connected";
            }
            else
            {
                UdpStatusText =
                    "TCP Connect Failed";

                CameraStatusText =
                    "Device Connect Failed";
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

                UdpStatusText =
                    "TCP Disconnected";

                CameraStatusText =
                    "Device Disconnected";

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

        /// <summary>
        /// [MCB] [Packet] 송신
        /// </summary>
        private void SendMcbPacket(
            byte[] packet,
            string commandName)
        {
            bool result =
                _mcbTcpClientService.Send(
                    packet);

            UpdateSendResult(
                "MCB",
                packet,
                commandName,
                result);
        }

        /// <summary>
        /// [SCB] [Packet] 송신
        /// </summary>
        private void SendScbPacket(
            byte[] packet,
            string commandName)
        {
            bool result =
                _scbTcpClientService.Send(
                    packet);

            UpdateSendResult(
                "SCB",
                packet,
                commandName,
                result);
        }

        /// <summary>
        /// [Packet] 송신 결과 화면 상태 갱신
        /// </summary>
        private void UpdateSendResult(
            string deviceName,
            byte[] packet,
            string commandName,
            bool isSuccess)
        {
            if (!isSuccess)
            {
                UdpStatusText = deviceName + " Send Failed";
                CameraStatusText = deviceName + " Send Failed";
                return;
            }

            LastUdpSendPacketText =
                "[" + deviceName + "] " + ConvertToHexString(packet);

            UdpStatusText =
                deviceName + " Packet Sent";

            CameraStatusText =
                commandName;
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
                UdpStatusText =
                    sendResult.DeviceName + " Send Failed";

                CameraStatusText =
                    sendResult.DeviceName + " Send Failed";

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

            UdpStatusText =
                sendResult.DeviceName + " Packet Sent";

            CameraStatusText =
                sendResult.CommandName;
        }

        #endregion

        #region [Receive Event Methods]

        /// <summary>
        /// [MCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [MCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 파싱은 [ParseReceivedPacket] 공통 함수에서 처리한다.
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
            ParseReceivedPacket(
                "MCB",
                packet);
        }

        /// <summary>
        /// [SCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [SCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 파싱은 [ParseReceivedPacket] 공통 함수에서 처리한다.
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
            ParseReceivedPacket(
                "SCB",
                packet);
        }

        /// <summary>
        /// [ADS1000] 수신 [Packet] 파싱 및 화면 상태 반영
        /// 
        /// [MCB] / [SCB] 공통으로 수신 [Packet]을 처리한다.
        /// </summary>
        /// <param name="deviceName">
        /// 수신 장비 이름
        /// </param>
        /// <param name="packet">
        /// 수신 [Packet]
        /// </param>
        private void ParseReceivedPacket(
            string deviceName,
            byte[] packet)
        {
            string packetText =
                ConvertToHexString(packet);

            LastUdpReceivePacketText =
                "[" + deviceName + "] " + packetText;

            Ads1000ParsedPacket parsedPacket =
                _packetParser.Parse(
                    packet);

            bool canPrintLog =
                CanPrintAds1000ReceiveLog();

            if (canPrintLog)
            {
                Console.WriteLine("[ADS1000][" + deviceName + "] Parse Result");
                Console.WriteLine("[ADS1000][" + deviceName + "] Raw : " + packetText);
                Console.WriteLine("[ADS1000][" + deviceName + "] IsValid : " + parsedPacket.IsValid);

            }


            if (!parsedPacket.IsValid)
            {
                if (canPrintLog)
                {
                    Console.WriteLine("[ADS1000][" + deviceName + "] Error : " + parsedPacket.ErrorMessage);
                    ConsoleLogHelper.PrintLine();
                }

                UdpStatusText =
                    deviceName + " Packet Invalid";

                return;
            }

            if (canPrintLog)
            {
                Console.WriteLine("[ADS1000][" + deviceName + "] Cmd1 : 0x" + parsedPacket.Cmd1.ToString("X2"));
                Console.WriteLine("[ADS1000][" + deviceName + "] Length : " + parsedPacket.Length);
                Console.WriteLine("[ADS1000][" + deviceName + "] Checksum : 0x" + parsedPacket.Checksum.ToString("X2"));
            }

            ApplyParsedStatusValue(
                parsedPacket);

            if (canPrintLog)
            {
                ConsoleLogHelper.PrintLine();
            }

            UdpStatusText =
                deviceName + " Packet Parsed";

            CameraStatusText =
                parsedPacket.Description;
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
                CameraStatusText =
                    statusText;
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

        #region [Utility]

        /// <summary>
        /// [ADS1000] 수신 [Packet] 로그 출력 가능 여부 확인
        /// 
        /// [MCB] / [SCB] 상태값은 짧은 주기로 계속 들어오므로,
        /// 지정된 시간 간격이 지난 경우에만 [Console] 로그를 출력한다.
        /// </summary>
        private bool CanPrintAds1000ReceiveLog()
        {
            DateTime now =
                DateTime.Now;

            if ((now - _lastAds1000ReceiveLogTime).TotalSeconds <
                Ads1000ReceiveLogIntervalSeconds)
            {
                return false;
            }

            _lastAds1000ReceiveLogTime =
                now;

            return true;
        }

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
