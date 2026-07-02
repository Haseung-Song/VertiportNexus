using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.ADS1000;
using VertiportNexus.Services.Camera;
using VertiportNexus.Services.Command;
using VertiportNexus.Services.Communication.MQ;
using VertiportNexus.Services.Communication.TCP;
using VertiportNexus.Services.Communication.UDP;
using VertiportNexus.Services.Radar;
using VertiportNexus.Services.Vertiport;

namespace VertiportNexus.ViewModels.Main
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

        /// <summary>
        /// [Camera] 상태 저장 서비스
        /// </summary>
        private readonly CameraStateProvider _cameraStateProvider;

        /// <summary>
        /// [Detection] 상태 저장 서비스
        /// </summary>
        private readonly DetectionStateProvider _detectionStateProvider;

        /// <summary>
        /// [Tracking] 자동 추적 제어 서비스
        /// </summary>
        private readonly TrackingControlService _trackingControlService;

        /// <summary>
        /// [MQ] 수신 서비스
        /// 
        /// [Mock MQ] / [RabbitMQ] 수신 서비스를
        /// 공통 인터페이스로 사용한다.
        /// </summary>
        private readonly IMqReceiver _mqReceiver;

        /// <summary>
        /// [MQ] 송신 서비스
        /// 
        /// [Mock MQ] / [RabbitMQ] 송신 서비스를
        /// 공통 인터페이스로 사용한다.
        /// </summary>
        private readonly IMqSender _mqSender;

        /// <summary>
        /// [Mock] [MQ] 수신 서비스
        /// 
        /// 개발용 테스트 메시지 주입 시 사용한다.
        /// 실제 운용 수신은 [_mqReceiver]를 통해 처리한다.
        /// </summary>
        private readonly MockMqReceiver _mockMqReceiver;

        /// <summary>
        /// [CSE] 명령 수신 서비스
        /// </summary>
        private readonly CseCommandReceiveService _cseCommandReceiveService;

        /// <summary>
        /// [CSE] 명령 응답 송신 서비스
        /// </summary>
        private readonly CseCommandResponseService _cseCommandResponseService;

        /// <summary>
        /// [Camera] 내부 명령 처리 서비스
        /// </summary>
        private readonly CameraCommandService _cameraCommandService;

        /// <summary>
        /// [CSE] 명령 처리 서비스
        /// </summary>
        private readonly CseCommandHandler _cseCommandHandler;

        /// <summary>
        /// [Radar] UDP 통신 서비스
        /// </summary>
        private readonly UdpClientService _radarUdpClientService;

        /// <summary>
        /// [Radar] Packet Parser
        /// </summary>
        private readonly RadarPacketParser _radarPacketParser;

        /// <summary>
        /// [Radar] Packet Builder
        /// </summary>
        private readonly RadarPacketBuilder _radarPacketBuilder;

        /// <summary>
        /// [Radar] 상태 저장 서비스
        /// </summary>
        private readonly RadarStateProvider _radarStateProvider;

        /// <summary>
        /// [Radar] Command 처리 서비스
        /// </summary>
        private readonly RadarCommandHandler _radarCommandHandler;

        /// <summary>
        /// [Radar] UDP 연동 서비스
        /// </summary>
        private readonly RadarUdpService _radarUdpService;

        /// <summary>
        /// [Radar] UDP Mock 송신 서비스
        /// 
        /// RadarUdpService 수신 흐름을 검증하기 위해
        /// Mock Packet을 UDP Loopback으로 송신한다.
        /// </summary>
        private readonly RadarUdpMockSenderService _radarUdpMockSenderService;

        /// <summary>
        /// [Radar] Mock Packet 테스트 서비스
        /// </summary>
        private readonly RadarMockPacketTestService _radarMockPacketTestService;

        /// <summary>
        /// [Radar] 추적 제어 서비스
        /// 
        /// Radar Tracking Request에서 수신한
        /// 방위각 / 고각 정보를 ADS1000 Pan / Tilt 제어로 연결한다.
        /// </summary>
        private readonly RadarTrackingControlService _radarTrackingControlService;

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
        /// [Radar] UDP 수신 대상 [IP]
        /// 
        /// Loopback 테스트 시 [127.0.0.1]을 사용하고,
        /// 실제 장비 연동 시 CSE 수신 IP 또는 테스트 대상 IP로 변경한다.
        /// </summary>
        private string _radarUdpIpAddress =
            "127.0.0.1";

        /// <summary>
        /// [Radar] UDP 수신 [Port]
        /// </summary>
        private int _radarUdpLocalPort =
            5000;

        /// <summary>
        /// [MQ] 연결 대상 [Host]
        /// </summary>
        private string _mqHostName =
            "127.0.0.1";

        /// <summary>
        /// [MQ] 연결 대상 [Port]
        /// </summary>
        private int _mqPort =
            5672;

        #endregion

        #region [Status Fields]

        /// <summary>
        /// [MQ] 상태 표시 문자열
        /// </summary>
        private string _mqStatusText =
            "RabbitMQ Ready";

        /// <summary>
        /// 마지막 [MQ] 수신 메시지 표시 문자열
        /// </summary>
        private string _lastMqMessageText = string.Empty;

        /// <summary>
        /// [CSE] [MQ] 수신 시작 여부
        /// 
        /// [RabbitMQ] 서버 연결 실패 또는 중복 시작으로 인해
        /// 프로그램 실행 흐름이 영향을 받지 않도록 상태를 관리한다.
        /// </summary>
        private bool _isCseMqReceiveStarted;

        /// <summary>
        /// 장비 연결 진행 여부
        /// 
        /// 현재 [MCB] / [SCB] [TCP Connect] 수행 중이면
        /// 중복 연결 요청을 방지한다.
        /// </summary>
        private bool _isDeviceConnecting;

        /// <summary>
        /// [Home Position] 이동 진행 여부
        /// 
        /// 장비 연결 후 자동 Home Position 이동 또는
        /// 사용자가 [HOME POSITION] 버튼을 누른 경우,
        /// 이동 완료 전까지 다른 장비 제어 명령을 막기 위해 사용한다.
        /// </summary>
        private bool _isHomePositionMoving;

        /// <summary>
        /// 장비 연결 해제 진행 여부
        /// </summary>
        private bool _isDeviceDisconnecting;

        /// <summary>
        /// 마지막 [ADS1000] 상태 로그 출력 시간
        /// </summary>
        private DateTime _lastAds1000StatusLogTime =
            DateTime.MinValue;

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
        /// [Radar] UDP 수신 상태
        /// </summary>
        private ConnectionState _radarUdpConnectionState =
            ConnectionState.Disconnected;

        /// <summary>
        /// [RabbitMQ] 수신 상태
        /// </summary>
        private ConnectionState _rabbitMqConnectionState =
            ConnectionState.Disconnected;

        /// <summary>
        /// 프로그램 전체 상태 표시 문자열
        /// </summary>
        private string _mainStatusText;

        /// <summary>
        /// 현재 운용 모드 표시 문자열
        /// </summary>
        private string _operationModeText;

        /// <summary>
        /// 현재 [PTZ] 제어 모드 표시 문자열
        /// 
        /// [IF-GUIS-CSE-008] 요청 또는
        /// 화면 버튼 조작으로 설정된 [AUTO] / [MANUAL] 값을 표시한다.
        /// </summary>
        private string _ptzControlModeText;

        /// <summary>
        /// ViewModel 종료 처리 여부
        /// 
        /// 프로그램 종료 중
        /// EO Camera Frame 수신 Callback에서
        /// UI Binding 객체 접근이 발생하지 않도록 방지한다.
        /// </summary>
        private bool _isDisposing;

        #endregion

        #region [Camera State Fields]

        /// <summary>
        /// 현재 [Pan] 값
        /// </summary>
        private double _currentPan;

        /// <summary>
        /// [Pan] 누적 위치값
        /// 
        /// 화면 표시용 [Pan] 값은 [0 ~ 360] 범위로 정규화하지만,
        /// 장비 제어용 [Pan] 위치는 한 바퀴 이상 회전한 값을 유지해야 하므로
        /// 내부적으로 누적 위치값을 별도로 관리한다.
        /// </summary>
        private double _currentPanAccumulated;

        /// <summary>
        /// [Pan] 누적 위치값 초기화 여부
        /// </summary>
        private bool _hasPanAccumulatedStatus;

        /// <summary>
        /// 마지막 [Pan] 표시 상태값
        /// 
        /// 장비 상태 Packet에서 수신한 [Pan] 값을 기준으로
        /// 회전 방향과 누적 이동량을 계산하기 위해 사용한다.
        /// </summary>
        private double _lastPanDisplayStatus;

        /// <summary>
        /// 현재 [Tilt] 값
        /// </summary>
        private double _currentTilt;

        /// <summary>
        /// 현재 [Pan / Tilt] 이동 축
        /// 
        /// [Absolute] / [Relative] / [Continuous] 이동 중
        /// Pan / Tilt Speed 값이 변경된 경우,
        /// 현재 이동 중인 축에 속도 갱신 명령을 송신하기 위해 사용한다.
        /// </summary>
        private PanTiltMoveAxis _currentPanTiltMoveAxis =
            PanTiltMoveAxis.None;

        /// <summary>
        /// 현재 [Pan / Tilt] 이동 제어 유형
        /// 
        /// 이동 중 [Pan / Tilt Speed] 값이 변경된 경우,
        /// Absolute / Relative 제어 방식에 따라
        /// 속도 갱신 Packet 형식을 다르게 선택하기 위해 사용한다.
        /// </summary>
        private PanTiltMoveType _currentPanTiltMoveType =
            PanTiltMoveType.None;

        /// <summary>
        /// 현재 [Zoom] 값
        /// </summary>
        private double _currentZoom;

        /// <summary>
        /// 현재 [Zoom] 배율 값
        /// 
        /// [Zoom] 위치값 [0 ~ 1000]을
        /// 실제 배율 기준 [x1.0 ~ x66.0]으로 변환한 값이다.
        /// </summary>
        private double _currentZoomRatio =
            1.0;

        /// <summary>
        /// 현재 [Focus] 값
        /// </summary>
        private double _currentFocus;

        /// <summary>
        /// [Pan] 선회 모드
        /// 
        /// [Pan Absolute] 이동 시
        /// [Via 0] / [Short] 이동 방식을 결정한다.
        /// </summary>
        private Ads1000PanTurnMode _panTurnMode =
            Ads1000PanTurnMode.Short;

        /// <summary>
        /// [UI] 연속 이동 제어 진행 여부
        /// 
        /// 사용자가 화면 버튼을 통해
        /// [MouseDown] 연속 이동을 시작한 경우에만 true로 설정한다.
        /// 
        /// [RabbitMQ] / [CSE] 연속 이동 명령과
        /// [UI] MouseUp 정지 처리를 분리하기 위해 사용한다.
        /// </summary>
        private bool _isUiContinuousMoveStarted;

        /// <summary>
        /// 현재 [Pan / Tilt] 연속 이동 방향
        /// 
        /// 사용자가 화면 버튼으로 [Pan] / [Tilt] 연속 이동을 시작한 경우,
        /// 이동 중 속도 변경 시 동일 방향 명령을 다시 송신하기 위해 사용한다.
        /// </summary>
        private PanTiltContinuousMoveDirection _currentPanTiltContinuousMoveDirection =
            PanTiltContinuousMoveDirection.None;

        /// <summary>
        /// [Pan] Absolute 이동 입력값
        /// </summary>
        private double? _panAbsoluteValue;

        /// <summary>
        /// [Tilt] Absolute 이동 입력값
        /// </summary>
        private double? _tiltAbsoluteValue;

        /// <summary>
        /// [Pan] Relative 이동 입력값
        /// </summary>
        private double? _panRelativeValue;

        /// <summary>
        /// [Tilt] Relative 이동 입력값
        /// </summary>
        private double? _tiltRelativeValue;

        /// <summary>
        /// [Zoom] 위치 이동 입력값
        /// </summary>
        private int? _zoomPositionValue;

        /// <summary>
        /// [Zoom] 배율 이동 입력값
        /// </summary>
        private double? _zoomRatioValue;

        /// <summary>
        /// [Focus] 위치 이동 입력값
        /// </summary>
        private int? _focusPositionValue;

        #endregion

        #region [Image Binding Fields - Test Only]

        /// <summary>
        /// [EO] 영상 출력용 [Image]
        /// </summary>
        private BitmapSource _eoCameraImage;

        /// <summary>
        /// [EO] 영상 표시 허용 여부
        /// 
        /// 연결 해제 또는 연결 중 해제 시,
        /// 뒤늦게 들어온 [Frame]이 화면에 다시 표시되지 않도록 제어한다.
        /// </summary>
        private bool _isEoVideoDisplayEnabled;

        #endregion

        #region [Camera Service Fields]

        /// <summary>
        /// [EO] [Camera] 영상 서비스
        /// </summary>
        private readonly EoCameraService _eoCameraService;

        #endregion

        #region [Command Properties]

        /// <summary>
        /// [MQ] 연결 요청 [Command]
        /// </summary>
        public ICommand StartMqReceiveCommand { get; }

        /// <summary>
        /// [MQ] 연결 해제 요청 [Command]
        /// </summary>
        public ICommand StopMqReceiveCommand { get; }

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
        /// [Pan] Absolute 이동 요청 [Command]
        /// </summary>
        public ICommand MovePanAbsoluteCommand { get; }

        /// <summary>
        /// [Tilt] Absolute 이동 요청 [Command]
        /// </summary>
        public ICommand MoveTiltAbsoluteCommand { get; }

        /// <summary>
        /// [Pan] Relative 이동 요청 [Command]
        /// </summary>
        public ICommand MovePanRelativeCommand { get; }

        /// <summary>
        /// [Tilt] Relative 이동 요청 [Command]
        /// </summary>
        public ICommand MoveTiltRelativeCommand { get; }

        /// <summary>
        /// [Home Position] 이동 요청 [Command]
        /// </summary>
        public ICommand MoveHomePositionCommand { get; }

        /// <summary>
        /// [Pan] 현재 위치 [0] 설정 요청 [Command]
        /// </summary>
        public ICommand SetPanZeroCommand { get; }

        /// <summary>
        /// [Tilt] 현재 위치 [0] 설정 요청 [Command]
        /// </summary>
        public ICommand SetTiltZeroCommand { get; }

        /// <summary>
        /// 위치 제어 입력값 초기화 요청 [Command]
        /// </summary>
        public ICommand ResetPositionInputCommand { get; }

        /// <summary>
        /// [Zoom] 위치 이동 요청 [Command]
        /// </summary>
        public ICommand SetZoomPositionCommand { get; }

        /// <summary>
        /// [Zoom] 배율 이동 요청 [Command]
        /// </summary>
        public ICommand SetZoomRatioCommand { get; }

        /// <summary>
        /// [Focus] 위치 이동 요청 [Command]
        /// </summary>
        public ICommand SetFocusPositionCommand { get; }

        /// <summary>
        /// [Status] 조회 요청 [Command]
        /// </summary>
        public ICommand RequestStatusCommand { get; }

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정 요청 [Command]
        /// </summary>
        public ICommand SetPtzAutoModeCommand { get; }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정 요청 [Command]
        /// </summary>
        public ICommand SetPtzManualModeCommand { get; }

        /// <summary>
        /// [Radar] UDP 수신 시작 요청 [Command]
        /// </summary>
        public ICommand StartRadarUdpReceiveCommand { get; }

        /// <summary>
        /// [Radar] UDP 수신 중지 요청 [Command]
        /// </summary>
        public ICommand StopRadarUdpReceiveCommand { get; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MainViewModel] 생성자
        /// </summary>
        public MainViewModel()
        {
            #region [Service Initialize]

            // [MCB] [TCP] 통신 서비스 생성
            _mcbTcpClientService =
                new TcpClientService("MCB");

            // [SCB] [TCP] 통신 서비스 생성
            _scbTcpClientService =
                new TcpClientService("SCB");

            // [MCB] 수신 이벤트 연결
            _mcbTcpClientService.MessageReceived +=
                OnMcbMessageReceived;

            // [SCB] 수신 이벤트 연결
            _scbTcpClientService.MessageReceived +=
                OnScbMessageReceived;

            // [ADS1000] 장비 [TCP] 연결 서비스 생성
            _ads1000ConnectionService =
                new Ads1000ConnectionService(
                    _mcbTcpClientService,
                    _scbTcpClientService);

            // [ADS1000] 장비 연결 상태 변경 이벤트 연결
            //
            // [MCB] / [SCB] 연결 결과를 각각 수신하여
            // 화면 연결 상태를 즉시 갱신한다.
            _ads1000ConnectionService.ConnectionStateChanged +=
                OnAds1000ConnectionStateChanged;

            // [EO] [Camera] 영상 서비스 생성
            _eoCameraService =
                new EoCameraService();

            // [EO] 영상 [Frame] 수신 이벤트 연결
            _eoCameraService.FrameReceived +=
                OnEoCameraFrameReceived;

            // [EO] 영상 상태 변경 이벤트 연결
            _eoCameraService.StatusChanged +=
                OnEoCameraStatusChanged;

            #endregion

            #region [Builder Initialize]

            // [MCB] [Packet Builder] 생성
            //
            // [Ads1000CameraControlService] 생성 시
            // Packet 생성 객체로 전달한다.
            Ads1000McbPacketBuilder mcbPacketBuilder =
                new Ads1000McbPacketBuilder();

            // [SCB] [Packet Builder] 생성
            //
            // [Ads1000CameraControlService] 생성 시
            // Packet 생성 객체로 전달한다.
            Ads1000ScbPacketBuilder scbPacketBuilder =
                new Ads1000ScbPacketBuilder();

            #endregion

            #region [Control Service Initialize]

            // [ADS1000] [Camera] 제어 서비스 생성
            _ads1000CameraControlService =
                new Ads1000CameraControlService(
                    _mcbTcpClientService,
                    _scbTcpClientService,
                    mcbPacketBuilder,
                    scbPacketBuilder);

            // [ADS1000] [Packet] 송신 결과 이벤트 연결
            _ads1000CameraControlService.SendResultChanged +=
                OnAds1000SendResultChanged;

            #endregion

            #region [Status Service Initialize]

            // [ADS1000] 상태 [Packet] 처리 서비스 생성
            _ads1000StatusService =
                new Ads1000StatusService();

            // [Camera] 상태 저장 서비스 생성
            //
            // ADS1000 수신 Packet에서 파싱된
            // Pan / Tilt / Zoom / Focus 상태와
            // 현재 PTZ 제어 모드를 보관한다.
            _cameraStateProvider =
                new CameraStateProvider();

            // [Detection] 상태 저장 서비스 생성
            //
            // 최종 ICD [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-003] 명령 처리 결과와
            // 영상처리유닛에서 전달되는 탐지 객체 정보를 보관한다.
            //
            // [AUTO] 추적 제어 시
            // 마지막 탐지 객체 [Bounding Box]를 기준으로
            // [Pan] / [Tilt] 보정값 계산에 사용한다.
            _detectionStateProvider =
                new DetectionStateProvider();

            // [Radar] 상태 저장 서비스 생성
            //
            // Radar Tracking Request 수신 상태와
            // Radar 우선 제어 여부를 보관한다.
            //
            // [CSE] detect_conf 처리 시,
            // Radar Tracking 활성 상태라면 GUI BBOX 기반 추적 제어를 수행하지 않는다.
            _radarStateProvider =
                new RadarStateProvider();

            // 내부 [Camera] 명령 처리 서비스 생성
            //
            // [CSE] 명령 처리부에서 변환한 [CameraCommand]를 받아
            // 실제 ADS1000 장비 제어 명령으로 분기한다.
            //
            // 현재 [PTZ] 제어 모드가 [AUTO]인 경우
            // Pan / Tilt 수동 제어를 무시하기 위해 [CameraStateProvider]를 함께 전달한다.
            _cameraCommandService =
                new CameraCommandService(
                    _ads1000CameraControlService,
                    _cameraStateProvider);

            // [Tracking] 자동 추적 제어 서비스 생성
            //
            // 탐지 객체 [Bounding Box] 중심점과
            // 영상 중심점을 비교하여 자동 추적 보정 방향을 계산한다.
            _trackingControlService =
                new TrackingControlService(
                    _ads1000CameraControlService);

            // [PTZ] 제어 모드 변경 이벤트 연결
            //
            // [MQ] 수신으로 [AUTO] / [MANUAL] 모드가 변경된 경우
            // 화면 표시값을 즉시 갱신한다.
            _cameraStateProvider.PtzControlModeChanged +=
                OnPtzControlModeChanged;

            #endregion

            #region [CSE Initialize]

            // [Mock] [MQ] 수신 서비스 생성
            //
            // 개발 단계에서 [JSON] 테스트 메시지를
            // 직접 주입하기 위해 별도 보관한다.
            _mockMqReceiver =
                new MockMqReceiver();

            // [MQ] 수신 서비스 지정
            //
            // 실제 [RabbitMQ]의 [q.command.req] / [q.status.req] Queue에서
            // [CSE] 명령 [JSON]을 수신한다.
            _mqReceiver =
                new RabbitMqReceiver(
                    MqHostName,
                    MqPort);

            // [MQ] 송신 서비스 지정
            //
            // [CSE] 명령 처리 결과를
            // 실제 [RabbitMQ] Queue로 송신한다.
            _mqSender =
                new RabbitMqSender(
                    MqHostName,
                    MqPort);

            // [CSE] 명령 수신 서비스 생성
            _cseCommandReceiveService =
                new CseCommandReceiveService(
                    _mqReceiver);

            // [CSE] 명령 응답 송신 서비스 생성
            //
            // [q.command.res] / [q.status.res] Queue로
            // 명령 처리 결과와 카메라 상태 응답을 송신한다.
            _cseCommandResponseService =
                new CseCommandResponseService(
                    _mqSender);

            // [CSE] 명령 처리 서비스 생성
            //
            // 최종 ICD 기준 [detect_on] / [detect_off] / [detect_conf] /
            // [ptz_move] / [get_state] 명령을 처리한다.
            //
            // Radar Tracking 활성 상태에서는
            // GUI BBOX 기반 추적보다 Radar 제어를 우선하기 위해
            // [RadarStateProvider]를 함께 전달한다.
            _cseCommandHandler =
                new CseCommandHandler(
                    _cameraCommandService,
                    _cseCommandResponseService,
                    _cameraStateProvider,
                    _detectionStateProvider,
                    _trackingControlService,
                    _radarStateProvider);

            // [CSE] 명령 수신 이벤트 연결
            _cseCommandReceiveService.CommandReceived +=
                OnCseCommandReceived;

            // [CSE] 명령 수신은 [MQ START] 버튼을 통해
            // 사용자가 수동으로 시작한다.
            //
            // [RabbitMQ] 서버 연결 실패로 인해
            // 화면 초기화가 지연되지 않도록 자동 시작하지 않는다.

            #endregion

            #region [Radar Initialize]

            // [Radar] UDP 통신 서비스 생성
            //
            // [CSR]에서 CSE로 전달되는 Radar Packet을
            // UDP로 수신하기 위해 사용한다.
            _radarUdpClientService =
                new UdpClientService(
                    "RADAR");

            // [Radar] Packet Parser 생성
            //
            // 수신 byte[] 데이터를 Header / SubData / Tail 구조로 분리하고,
            // Command별 Payload 모델로 변환한다.
            _radarPacketParser =
                new RadarPacketParser();

            // [Radar] Packet Builder 생성
            //
            // CSE에서 CSR로 송신할 응답 Packet을 생성한다.
            _radarPacketBuilder =
                new RadarPacketBuilder();

            // [Radar] 추적 제어 서비스 생성
            //
            // Radar Tracking Request에서 수신한
            // 방위각 / 고각 정보를 ADS1000 Pan / Tilt 제어로 연결한다.
            //
            // 현재 ADS1000 제어 구조는
            // Pan Absolute / Tilt Absolute 명령을 각각 송신하는 방식이므로,
            // Radar Tracking 제어도 동일한 방식으로 처리한다.
            _radarTrackingControlService =
                new RadarTrackingControlService(
                    _ads1000CameraControlService);

            // [Radar] Command 처리 서비스 생성
            //
            // Radar Packet의 Command를 기준으로
            // Tracking Request / BIST Request를 분기 처리하고,
            // Tracking Request 수신 시 Radar 우선 제어 상태를 활성화한다.
            //
            // [RadarStateProvider]는 [CSE] 명령 처리 서비스와 공유하여,
            // Radar Tracking 활성 중에는 GUI BBOX 기반 Tracking 제어를 막는다.
            _radarCommandHandler =
                new RadarCommandHandler(
                    _radarPacketParser,
                    _radarPacketBuilder,
                    _radarStateProvider,
                    _radarTrackingControlService);

            // [Radar] Mock Packet 테스트 서비스 생성
            //
            // Tracking Request / BIST Request Mock Packet을 생성하여
            // 실제 UDP 통신 없이 Radar Command 처리 로직을 테스트한다.
            _radarMockPacketTestService =
                new RadarMockPacketTestService(
                    _radarCommandHandler);

            // [Radar] UDP 연동 서비스 생성
            //
            // UDP 수신 Packet을 Handler로 전달하고,
            // 처리 결과 응답 Packet을 송신자에게 반환한다.
            _radarUdpService =
                new RadarUdpService(
                    _radarUdpClientService,
                    _radarCommandHandler);

            // [Radar] UDP Mock 송신 서비스 생성
            //
            // 실제 Radar 장비 연동 전,
            // Mock Packet을 UDP Loopback으로 송신하여
            // RadarUdpService 수신 / Handler 처리 / ADS1000 제어 흐름을 검증한다.
            _radarUdpMockSenderService =
                new RadarUdpMockSenderService(
                    _radarMockPacketTestService);

            #endregion

            #region [Command Initialize]

            StartMqReceiveCommand =
                new RelayCommand(
                    StartRabbitMqReceive);

            StopMqReceiveCommand =
                new RelayCommand(
                    StopRabbitMqReceive);

            StartTcpReceiveCommand =
                new AsyncRelayCommand(ConnectDevicesAsync);

            StopTcpReceiveCommand =
                new AsyncRelayCommand(DisconnectDevicesAsync);

            SendTcpTestCommand =
                new RelayCommand(
                    _ads1000CameraControlService.SendVersionQuery);

            PanLeftCommand =
                new RelayCommand(
                    StartPanLeftMove);

            PanRightCommand =
                new RelayCommand(
                    StartPanRightMove);

            TiltUpCommand =
                new RelayCommand(
                    StartTiltUpMove);

            TiltDownCommand =
                new RelayCommand(
                    StartTiltDownMove);

            StopMoveCommand =
                new RelayCommand(
                    StopContinuousMove);

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

            MovePanAbsoluteCommand =
                new RelayCommand(
                    MovePanAbsolute);

            MoveTiltAbsoluteCommand =
                new RelayCommand(
                    MoveTiltAbsolute);

            MovePanRelativeCommand =
                new RelayCommand(
                    MovePanRelative);

            MoveTiltRelativeCommand =
                new RelayCommand(
                    MoveTiltRelative);

            MoveHomePositionCommand =
                new AsyncRelayCommand(
                    MoveHomePositionAsync);

            SetPanZeroCommand =
                new RelayCommand(
                    SetPanZero);

            SetTiltZeroCommand =
                new RelayCommand(
                    _ads1000CameraControlService.SetTiltZero);

            ResetPositionInputCommand =
                new RelayCommand(
                    ResetPositionInput);

            SetZoomPositionCommand =
                new RelayCommand(
                    SetZoomPosition);

            SetZoomRatioCommand =
                new RelayCommand(
                    SetZoomRatio);

            SetFocusPositionCommand =
                new RelayCommand(
                    SetFocusPosition);

            RequestStatusCommand =
                new RelayCommand(
                    _ads1000CameraControlService.SendVersionQuery);

            SetPtzAutoModeCommand =
                new RelayCommand(
                    SetPtzAutoMode);

            SetPtzManualModeCommand =
                new RelayCommand(
                     SetPtzManualMode);

            StartRadarUdpReceiveCommand =
                new RelayCommand(
                    StartRadarUdpReceive);

            StopRadarUdpReceiveCommand =
                new RelayCommand(
                    StopRadarUdpReceive);

            #endregion

            #region [Default Initialize]

            InitializeDefaultValues();

            Console.WriteLine(
                "[MAIN] ADS1000 Direct TCP Test Initialize Complete");

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[CAMERA][STATE] Pan Turn Mode : "
                + _panTurnMode);

            ConsoleLogHelper.PrintLine();

            #endregion
        }

        #endregion

        #region [Network Properties]

        /// <summary>
        /// [MCB] 연결 대상 [IP]
        /// </summary>
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

        /// <summary>
        /// [MCB] 연결 대상 [Port]
        /// </summary>
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

        /// <summary>
        /// [SCB] 연결 대상 [IP]
        /// </summary>
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

        /// <summary>
        /// [SCB] 연결 대상 [Port]
        /// </summary>
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

        /// <summary>
        /// [Radar] UDP 수신 대상 [IP]
        /// </summary>
        public string RadarUdpIpAddress
        {
            get => _radarUdpIpAddress;
            set
            {
                if (_radarUdpIpAddress != value)
                {
                    _radarUdpIpAddress = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Radar] UDP 수신 [Port]
        /// </summary>
        public int RadarUdpLocalPort
        {
            get => _radarUdpLocalPort;
            set
            {
                if (_radarUdpLocalPort != value)
                {
                    _radarUdpLocalPort = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [MQ] 연결 대상 [Host]
        /// </summary>
        public string MqHostName
        {
            get => _mqHostName;
            set
            {
                if (_mqHostName != value)
                {
                    _mqHostName = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [MQ] 연결 대상 [Port]
        /// </summary>
        public int MqPort
        {
            get => _mqPort;
            set
            {
                if (_mqPort != value)
                {
                    _mqPort = value;
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [MQ Properties]

        /// <summary>
        /// [MQ] 연결 상태 표시 문자열
        /// </summary>
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

        /// <summary>
        /// 마지막 [MQ] 수신 메시지 표시 문자열
        /// </summary>
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

        #endregion

        #region [Connection Status Properties]

        /// <summary>
        /// [MCB] 연결 상태 표시 문자열
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
        /// [MCB] 연결 상태 표시 색상
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
        /// [SCB] 연결 상태 표시 문자열
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
        /// [SCB] 연결 상태 표시 색상
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

        /// <summary>
        /// [Radar] UDP 수신 상태 표시 문자열
        /// </summary>
        public string RadarUdpConnectionStatusText
        {
            get
            {
                switch (_radarUdpConnectionState)
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
        /// [Radar] UDP 수신 상태 표시 색상
        /// </summary>
        public Brush RadarUdpConnectionStatusBrush
        {
            get
            {
                switch (_radarUdpConnectionState)
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
        /// [RabbitMQ] 연결 상태 표시 문자열
        /// </summary>
        public string RabbitMqConnectionStatusText
        {
            get
            {
                switch (_rabbitMqConnectionState)
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
        /// [RabbitMQ] 연결 상태 표시 색상
        /// </summary>
        public Brush RabbitMqConnectionStatusBrush
        {
            get
            {
                switch (_rabbitMqConnectionState)
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
        /// 장비 제어 가능 여부
        /// 
        /// [MCB] / [SCB] 중 하나 이상 연결된 경우
        /// [PTZ] / [Zoom] / [Focus] 제어 영역을 활성화한다.
        /// 
        /// 단, Home Position 이동 중에는
        /// Pan / Tilt 제어 명령이 중복 송신되지 않도록
        /// 장비 제어 영역을 비활성화한다.
        /// </summary>
        public bool IsDeviceControlEnabled
        {
            get
            {
                return (_mcbConnectionState == ConnectionState.Connected ||
                        _scbConnectionState == ConnectionState.Connected);
            }

        }

        /// <summary>
        /// 장비 통신 설정 입력 가능 여부
        /// 
        /// [MCB] / [SCB] 연결 전 상태에서만
        /// IP / Port 입력값을 수정할 수 있도록 한다.
        /// 
        /// 연결 중 또는 연결 완료 상태에서는
        /// 통신 대상 정보 변경을 방지한다.
        /// </summary>
        public bool IsDeviceConnectionSettingEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Disconnected &&
                       _scbConnectionState == ConnectionState.Disconnected &&
                       !_isDeviceConnecting;
            }

        }

        /// <summary>
        /// 장비 제어 탭 활성화 여부
        /// 
        /// Home Position 이동 중에는
        /// 통신 설정 / 운용 제어 / 이동 제어 탭을 비활성화하여
        /// 장비 설정 변경 및 제어 명령 입력을 막는다.
        /// </summary>
        public bool IsDeviceControlTabEnabled
        {
            get
            {
                return !_isHomePositionMoving;
            }

        }

        /// <summary>
        /// [Pan / Tilt Speed] 설정 가능 여부
        /// 
        /// [MCB] 연결 상태에서만 Pan / Tilt Speed 설정을 허용한다.
        /// 
        /// 단, Home Position 이동 중에는
        /// 장비 내부 Home Script가 실행 중이므로
        /// Pan / Tilt Speed 설정을 비활성화한다.
        /// </summary>
        public bool IsPanTiltSpeedEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Connected &&
                       !_isHomePositionMoving;
            }

        }

        /// <summary>
        /// 장비 연결 버튼 활성화 여부
        /// 
        /// 장비 연결 처리 중이거나,
        /// Home Position 이동 중인 경우
        /// [장비 연결] 버튼을 비활성화한다.
        /// </summary>
        public bool IsDeviceConnectButtonEnabled
        {
            get
            {
                return !_isDeviceConnecting &&
                       !_isDeviceDisconnecting &&
                       !_isHomePositionMoving;
            }

        }

        /// <summary>
        /// 장비 연결 해제 버튼 활성화 여부
        /// 
        /// [MCB] / [SCB] 중 하나 이상 연결된 경우
        /// [연결 해제] 버튼을 활성화한다.
        /// 
        /// 단, Home Position 이동 중에는
        /// 장비 내부 Home Script가 실행 중일 수 있으므로
        /// 통신 연결 해제를 막는다.
        /// </summary>
        public bool IsDeviceDisconnectButtonEnabled
        {
            get
            {
                return (_mcbConnectionState == ConnectionState.Connected ||
                        _scbConnectionState == ConnectionState.Connected) &&
                       !_isDeviceDisconnecting &&
                       !_isHomePositionMoving;
            }

        }

        /// <summary>
        /// [Radar UDP 수신 시작] 버튼 활성화 여부
        /// </summary>
        public bool IsRadarUdpStartButtonEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Connected &&
                       _scbConnectionState == ConnectionState.Connected &&
                       _radarUdpConnectionState != ConnectionState.Connected;
            }

        }

        /// <summary>
        /// [Radar UDP 수신 중지] 버튼 활성화 여부
        /// </summary>
        public bool IsRadarUdpStopButtonEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Connected &&
                       _scbConnectionState == ConnectionState.Connected &&
                       _radarUdpConnectionState == ConnectionState.Connected;
            }

        }

        /// <summary>
        /// [Radar UDP 통신 설정] 입력 가능 여부
        /// </summary>
        public bool IsRadarUdpConnectionSettingEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Connected &&
                       _scbConnectionState == ConnectionState.Connected &&
                       _radarUdpConnectionState == ConnectionState.Disconnected;
            }

        }

        /// <summary>
        /// [RabbitMQ 수신 시작] 버튼 활성화 여부
        /// </summary>
        public bool IsRabbitMqStartButtonEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Connected &&
                       _scbConnectionState == ConnectionState.Connected &&
                       _rabbitMqConnectionState != ConnectionState.Connected &&
                       _rabbitMqConnectionState != ConnectionState.Connecting;
            }

        }

        /// <summary>
        /// [RabbitMQ 수신 중지] 버튼 활성화 여부
        /// </summary>
        public bool IsRabbitMqStopButtonEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Connected &&
                       _scbConnectionState == ConnectionState.Connected &&
                       _rabbitMqConnectionState == ConnectionState.Connected;
            }

        }

        /// <summary>
        /// [RabbitMQ 통신 설정] 입력 가능 여부
        /// </summary>
        public bool IsRabbitMqConnectionSettingEnabled
        {
            get
            {
                return _mcbConnectionState == ConnectionState.Connected &&
                       _scbConnectionState == ConnectionState.Connected &&
                       _rabbitMqConnectionState == ConnectionState.Disconnected;
            }

        }

        #endregion

        #region [Main Status Properties]

        /// <summary>
        /// 프로그램 전체 상태 표시 문자열
        /// </summary>
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

        /// <summary>
        /// 현재 운용 모드 표시 문자열
        /// </summary>
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

        /// <summary>
        /// 현재 [PTZ] 제어 모드 표시 문자열
        /// </summary>
        public string PtzControlModeText
        {
            get => _ptzControlModeText;
            private set
            {
                if (_ptzControlModeText != value)
                {
                    _ptzControlModeText = value;
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [Camera Status Properties]

        /// <summary>
        /// 현재 [Pan] 위치값
        /// </summary>
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

        /// <summary>
        /// 현재 [Tilt] 위치값
        /// </summary>
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

        /// <summary>
        /// 현재 [Pan] / [Tilt] 제어 속도
        /// 
        /// [ADS1000] [Pan] / [Tilt] 이동 시 사용할
        /// 제어 속도를 설정하고 화면에 표시한다.
        /// 
        /// Pan / Tilt 이동 중 속도값이 변경된 경우에는
        /// 현재 이동 중인 축에 속도 갱신 명령을 송신하여
        /// 장비 실제 이동 속도에도 변경값이 반영되도록 한다.
        /// </summary>
        public double PanTiltSpeedLevel
        {
            get => _ads1000CameraControlService.PanTiltSpeedLevel;
            set
            {
                if (_ads1000CameraControlService.PanTiltSpeedLevel != value)
                {
                    Console.WriteLine(
                        "[UI][PTZ] Pan / Tilt Speed Value Changed : "
                        + _ads1000CameraControlService.PanTiltSpeedLevel.ToString("F0")
                        + " -> "
                        + value.ToString("F0"));

                    _ads1000CameraControlService.PanTiltSpeedLevel =
                        value;

                    OnPropertyChanged();

                    ApplyCurrentPanTiltMoveSpeed();
                }

            }

        }

        /// <summary>
        /// 현재 [Zoom] 위치값
        /// </summary>
        public double CurrentZoom
        {
            get => _currentZoom;
            private set
            {
                if (Math.Abs(_currentZoom - value) > 0.001)
                {
                    _currentZoom =
                        value;

                    OnPropertyChanged();

                    OnPropertyChanged(nameof(CurrentZoomDisplayText));
                }

            }

        }

        /// <summary>
        /// 현재 [Zoom] 배율 값
        /// </summary>
        public double CurrentZoomRatio
        {
            get => _currentZoomRatio;
            private set
            {
                if (Math.Abs(_currentZoomRatio - value) > 0.001)
                {
                    _currentZoomRatio =
                        value;

                    OnPropertyChanged();

                    OnPropertyChanged(nameof(CurrentZoomDisplayText));
                }

            }

        }

        /// <summary>
        /// 현재 [Zoom] 표시 문자열
        /// 
        /// Zoom 위치값 [0 ~ 1000]과
        /// 실제 배율값 [x1.0 ~ x66.0]을 함께 표시한다.
        /// </summary>
        public string CurrentZoomDisplayText
        {
            get
            {
                return CurrentZoom.ToString("F0")
                       + " (x"
                       + CurrentZoomRatio.ToString("F1")
                       + ")";
            }

        }

        /// <summary>
        /// 현재 [Focus] 위치값
        /// </summary>
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

        #endregion

        #region [Camera Control Input Properties]

        /// <summary>
        /// [Pan] [Via 0] 선회 모드 선택 여부
        /// 
        /// [Pan Absolute] 이동 시
        /// 현재 위치에서 목표 위치까지
        /// 단거리 보정 없이 이동하도록 설정한다.
        /// </summary>
        public bool IsPanTurnViaZeroMode
        {
            get
            {
                return _panTurnMode == Ads1000PanTurnMode.ViaZero;
            }
            set
            {
                if (value &&
                    _panTurnMode != Ads1000PanTurnMode.ViaZero)
                {
                    _panTurnMode =
                        Ads1000PanTurnMode.ViaZero;

                    // [Camera 상태] Pan 선회 모드 갱신
                    //
                    // UI에서 변경한 선회 모드를
                    // CSE / MQ 명령 처리에서도 동일하게 사용할 수 있도록 저장한다.
                    _cameraStateProvider
                        .UpdatePanTurnMode(
                            _panTurnMode);

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPanTurnShortMode));
                }

            }

        }

        /// <summary>
        /// [Pan] [Short] 선회 모드 선택 여부
        /// 
        /// [Pan Absolute] 이동 시
        /// 현재 위치에서 목표 위치까지
        /// 가장 가까운 방향으로 이동하도록 설정한다.
        /// </summary>
        public bool IsPanTurnShortMode
        {
            get
            {
                return _panTurnMode == Ads1000PanTurnMode.Short;
            }
            set
            {
                if (value &&
                    _panTurnMode != Ads1000PanTurnMode.Short)
                {
                    _panTurnMode =
                        Ads1000PanTurnMode.Short;

                    // [Camera 상태] Pan 선회 모드 갱신
                    //
                    // UI에서 변경한 선회 모드를
                    // CSE / MQ 명령 처리에서도 동일하게 사용할 수 있도록 저장한다.
                    _cameraStateProvider
                        .UpdatePanTurnMode(
                            _panTurnMode);

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPanTurnViaZeroMode));
                }

            }

        }

        /// <summary>
        /// [Pan] Absolute 이동 입력값
        /// </summary>
        public double? PanAbsoluteValue
        {
            get => _panAbsoluteValue;
            set
            {
                if (_panAbsoluteValue != value)
                {
                    _panAbsoluteValue = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Tilt] Absolute 이동 입력값
        /// </summary>
        public double? TiltAbsoluteValue
        {
            get => _tiltAbsoluteValue;
            set
            {
                if (_tiltAbsoluteValue != value)
                {
                    _tiltAbsoluteValue = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Pan] Relative 이동 입력값
        /// </summary>
        public double? PanRelativeValue
        {
            get => _panRelativeValue;
            set
            {
                if (_panRelativeValue != value)
                {
                    _panRelativeValue = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Tilt] Relative 이동 입력값
        /// </summary>
        public double? TiltRelativeValue
        {
            get => _tiltRelativeValue;
            set
            {
                if (_tiltRelativeValue != value)
                {
                    _tiltRelativeValue = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Zoom] 위치 이동 입력값
        /// </summary>
        public int? ZoomPositionValue
        {
            get => _zoomPositionValue;
            set
            {
                if (_zoomPositionValue != value)
                {
                    _zoomPositionValue = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Zoom] 배율 이동 입력값
        /// 
        /// 실제 카메라 배율 기준으로 입력한다.
        /// 예)
        /// 2.0  = 2배 Zoom
        /// 33.0 = 33배 Zoom
        /// 66.0 = 66배 Zoom
        /// </summary>
        public double? ZoomRatioValue
        {
            get => _zoomRatioValue;
            set
            {
                if (_zoomRatioValue != value)
                {
                    _zoomRatioValue = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Focus] 위치 이동 입력값
        /// </summary>
        public int? FocusPositionValue
        {
            get => _focusPositionValue;
            set
            {
                if (_focusPositionValue != value)
                {
                    _focusPositionValue = value;
                    OnPropertyChanged();
                }

            }

        }

        #endregion

        #region [Image Properties]

        /// <summary>
        /// [EO] 영상 출력용 [Image]
        /// </summary>
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
                "MCB / SCB DISCONNECTED";

            OperationModeText =
                "MODE STANDBY";

            PtzControlModeText =
                _cameraStateProvider.PtzControlMode;

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

            // [위치 제어 입력값] 기본값 설정

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
            _panTurnMode =
                Ads1000PanTurnMode.Short;

            _cameraStateProvider
                .UpdatePanTurnMode(
                    _panTurnMode);

            OnPropertyChanged(nameof(IsPanTurnShortMode));
            OnPropertyChanged(nameof(IsPanTurnViaZeroMode));
        }

        #endregion

        #region [MQ Methods]

        /// <summary>
        /// [RabbitMQ] 연결 상태 반영
        /// </summary>
        /// <param name="connectionState">
        /// [RabbitMQ] 연결 상태
        /// </param>
        private void SetRabbitMqConnectionState(
            ConnectionState connectionState)
        {
            // [RabbitMQ] 연결 상태 저장
            //
            // [RabbitMQ] 수신 시작 / 중지 여부를
            // 내부 상태값에 반영한다.
            _rabbitMqConnectionState =
                connectionState;

            // [RabbitMQ] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(RabbitMqConnectionStatusText));
            OnPropertyChanged(nameof(RabbitMqConnectionStatusBrush));

            // [RabbitMQ 수신 시작] 버튼 활성화 상태 갱신
            //
            // [RabbitMQ] 수신 상태에 따라
            // [MQ START] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStartButtonEnabled));

            // [RabbitMQ 수신 중지] 버튼 활성화 상태 갱신
            //
            // [RabbitMQ] 수신 상태에 따라
            // [MQ STOP] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStopButtonEnabled));

            // [RabbitMQ 통신 설정] 입력 가능 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [RabbitMQ] 수신 상태에 따라
            // RabbitMQ Host / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));
        }

        /// <summary>
        /// [RabbitMQ] 수신 시작
        /// 
        /// 화면에서 입력한 [RabbitMQ Host] / [Port]를 기준으로
        /// CSE 명령 JSON 수신을 시작한다.
        /// </summary>
        private async void StartRabbitMqReceive()
        {
            if (_isCseMqReceiveStarted ||
                _rabbitMqConnectionState == ConnectionState.Connected ||
                _rabbitMqConnectionState == ConnectionState.Connecting)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[CSE][MQ] Start Ignored : Already Started");

                Console.WriteLine();

                return;
            }

            try
            {
                _isCseMqReceiveStarted =
                    true;

                SetRabbitMqConnectionState(
                    ConnectionState.Connecting);

                // [RabbitMQ] 연결 상태 표시 지연
                //
                // RabbitMQ 수신 시작 처리가 빠르게 완료되는 경우
                // 화면에서 [Connecting] 상태가 너무 빠르게 지나가지 않도록
                // 짧은 표시 지연을 둔다.
                await Task.Delay(
                    500);

                _cseCommandReceiveService
                    .StartReceive();

                SetRabbitMqConnectionState(
                    ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                _isCseMqReceiveStarted =
                    false;

                SetRabbitMqConnectionState(
                    ConnectionState.Disconnected);

                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[CSE][MQ] Start Failed");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine();
            }

        }

        /// <summary>
        /// [RabbitMQ] 수신 중지
        /// 
        /// 현재 실행 중인 RabbitMQ CSE 명령 수신을 중지한다.
        /// </summary>
        private void StopRabbitMqReceive()
        {
            if (_rabbitMqConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[CSE][MQ] Stop Ignored : Not Started");
                Console.WriteLine();

                return;
            }

            try
            {
                // [카메라 상태] 주기 송신 중지
                //
                // RabbitMQ 수신 중지 시,
                // 실행 중인 [q.status.res] 상태 송신 Loop도 함께 종료한다.
                _cseCommandHandler
                    .StopCameraStatusPublishService();

                _mqReceiver
                    .StopReceive();

                _isCseMqReceiveStarted =
                    false;

                SetRabbitMqConnectionState(
                    ConnectionState.Disconnected);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[CSE][MQ] Stop Failed");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

        }

        #endregion

        #region [TCP Connection Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결
        /// </summary>
        private async Task ConnectDevicesAsync()
        {
            // 장비 연결 진행 중이면 중복 연결 방지
            if (_isDeviceConnecting)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Connect Ignored : Connecting");
                Console.WriteLine();

                return;
            }

            // 이미 [MCB] / [SCB] 중 하나라도 연결되어 있으면 중복 연결 방지
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
                     "MCB / SCB CONNECTING...";

                OperationModeText =
                    "DEVICE CONNECTING...";

                _isDeviceConnecting =
                    true;

                // [장비 연결 / 해제 버튼] 활성화 상태 갱신
                //
                // 연결 시도 중에는 중복 연결 / 해제 요청을 방지하기 위해
                // [장비 연결] / [연결 해제] 버튼을 비활성화한다.
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [장비 통신 설정] 입력 가능 상태 갱신
                //
                // [MCB] / [SCB] 연결 상태 변경에 따라
                // IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));

                // [Radar UDP 통신 설정] 입력 가능 상태 갱신
                //
                // 장비 연결 시도 종료 후
                // [MCB] / [SCB] 연결 상태에 따라
                // Radar UDP IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));

                // [RabbitMQ 통신 설정] 입력 가능 상태 갱신
                //
                // 장비 연결 시도 종료 후
                // [MCB] / [SCB] 연결 상태에 따라
                // RabbitMQ Host / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));

                // [MCB] / [SCB] 연결 시도 상태 표시
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

                // [EO] 영상 연결 처리
                //
                // [MCB] / [SCB] 중 하나 이상 연결된 경우에만
                // [EO] RTSP 영상을 활성화한다.
                //
                // 장비 제어 연결이 모두 실패한 경우에는
                // 영상 표시를 차단하고 화면을 초기화한다.
                if (_mcbConnectionState == ConnectionState.Connected ||
                    _scbConnectionState == ConnectionState.Connected)
                {
                    _isEoVideoDisplayEnabled =
                        true;

                    _eoCameraService.Connect(
                        DEFAULT_EO_RTSP_ADDRESS);
                }
                else
                {
                    _isEoVideoDisplayEnabled =
                        false;

                    _eoCameraService.Disconnect();

                    EOCameraImage =
                        null;
                }

                if (_mcbConnectionState == ConnectionState.Connected &&
                    _scbConnectionState == ConnectionState.Connected)
                {
                    // [장비 연결 후] Home Position 이동
                    //
                    // EO 영상 연결을 먼저 시도한 뒤,
                    // 화면으로 현재 장비 방향을 확인할 수 있도록
                    // 짧은 대기 후 Home Position 명령을 자동 송신한다.
                    await MoveHomePositionAfterDeviceConnectedAsync();
                }

            }
            finally
            {
                _isDeviceConnecting =
                    false;

                // [장비 연결 / 해제 버튼] 활성화 상태 갱신
                //
                // 연결 시도 종료 후
                // 현재 연결 상태에 따라 [장비 연결] / [연결 해제] 버튼 활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [장비 통신 설정] 입력 가능 상태 갱신
                //
                // [MCB] / [SCB] 연결 상태 변경에 따라
                // IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));
            }

        }

        /// <summary>
        /// [장비 연결 후] Home Position 이동
        /// 
        /// [MCB] / [SCB] 장비 연결이 완료되면
        /// 장비 기준 Home Position 상태에서 운용을 시작할 수 있도록
        /// Pan Home / Tilt Home 명령을 자동 송신한다.
        /// 
        /// EO 영상 연결 시도 후 Home 이동 과정을 확인할 수 있도록
        /// 짧은 대기 후 Home Position 이동을 수행한다.
        /// </summary>
        private async Task MoveHomePositionAfterDeviceConnectedAsync()
        {
            // [EO 영상 표시 대기]
            //
            // 장비 연결 직후 바로 Home Position 명령을 송신하면
            // 영상이 표시되기 전에 장비가 이동할 수 있다.
            //
            // 사용자가 화면으로 현재 방향과 Home 이동 과정을 확인할 수 있도록
            // EO 영상 연결 시도 후 짧은 대기 시간을 둔다.
            await Task.Delay(
                300);

            await MoveHomePositionWithControlLockAsync(
                "[DEVICE] Home Position After Connect");
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
            _isHomePositionMoving =
                isMoving;

            // [장비 연결 버튼] 활성화 상태 갱신
            //
            // Home Position 이동 중에는
            // [장비 연결] 버튼이 비활성화되도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

            // [장비 연결 해제 버튼] 활성화 상태 갱신
            //
            // Home Position 이동 중에는
            // 장비 내부 Home Script 실행 상태를 보호하기 위해
            // [연결 해제] 버튼이 비활성화되도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));

            // [장비 제어 탭] 활성화 상태 갱신
            //
            // Home Position 이동 중에는
            // 통신 설정 / 운용 제어 / 이동 제어 탭이 비활성화되도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceControlTabEnabled));

            // [Pan / Tilt Speed] 설정 가능 상태 갱신
            //
            // Home Position 이동 중에는
            // Pan / Tilt Speed 설정을 변경하지 못하도록 갱신한다.
            OnPropertyChanged(nameof(IsPanTiltSpeedEnabled));
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
            _mcbConnectionState =
                mcbConnectionState;

            // [SCB] 연결 상태 저장
            //
            // [SCB] 연결 여부를
            // 내부 상태값에 반영한다.
            _scbConnectionState =
                scbConnectionState;

            // [MCB] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(McbConnectionStatusText));
            OnPropertyChanged(nameof(McbConnectionStatusBrush));

            // [SCB] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(ScbConnectionStatusText));
            OnPropertyChanged(nameof(ScbConnectionStatusBrush));

            // [장비 제어] 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // 화면 제어 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceControlEnabled));

            // [장비 통신 설정] 입력 가능 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));

            // [장비 제어 탭] 활성화 상태 갱신
            //
            // Home Position 이동 여부 및 연결 상태 변경에 따라
            // 장비 제어 관련 탭 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceControlTabEnabled));

            // [Pan / Tilt Speed] 설정 가능 상태 갱신
            //
            // [MCB] 연결 상태 및 Home Position 이동 상태에 따라
            // Pan / Tilt Speed 슬라이더 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsPanTiltSpeedEnabled));

            // [장비 연결] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // 중복 연결 요청 가능 여부를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

            // [장비 연결 해제 버튼] 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // [연결 해제] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));

            // [Radar UDP 수신 시작] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // [UDP START] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpStartButtonEnabled));

            // [Radar UDP 수신 중지] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // [UDP STOP] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpStopButtonEnabled));

            // [Radar UDP 통신 설정] 입력 가능 상태 갱신
            //
            // [Radar UDP] 수신 상태 변경에 따라
            // Radar UDP IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));

            // [RabbitMQ 수신 시작] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [RabbitMQ] 수신 상태에 따라
            // [MQ START] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStartButtonEnabled));

            // [RabbitMQ 수신 중지] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [RabbitMQ] 수신 상태에 따라
            // [MQ STOP] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStopButtonEnabled));

            // [RabbitMQ 통신 설정] 입력 가능 상태 갱신
            //
            // [RabbitMQ] 수신 상태 변경에 따라
            // RabbitMQ Host / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));
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
                        : _mcbConnectionState;

                ConnectionState scbConnectionState =
                    isScbConnected.HasValue
                        ? isScbConnected.Value
                            ? ConnectionState.Connected
                            : ConnectionState.Disconnected
                        : _scbConnectionState;

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
            _cameraStateProvider.UpdateConnectionState(
                connectionResult.IsMcbConnected ||
                connectionResult.IsScbConnected);
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

            // 이미 연결 해제 상태이면 중복 해제 방지
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

                // [장비 연결 해제 버튼] 활성화 상태 갱신
                //
                // 연결 해제 처리 중에는 중복 연결 해제 요청을 방지하기 위해
                // [연결 해제] 버튼을 비활성화한다.
                OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));

                // [장비 연결 버튼] 활성화 상태 갱신
                //
                // 연결 해제 처리 중에는 중복 연결 요청을 방지하기 위해
                // [장비 연결] 버튼 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [Radar UDP] 수신 중지
                //
                // [MCB] / [SCB] 장비 연결 해제 시,
                // Radar UDP 수신 상태가 Connected로 남지 않도록
                // 실행 중인 UDP 수신을 먼저 중지한다.
                if (_radarUdpConnectionState == ConnectionState.Connected)
                {
                    _radarUdpService
                        .StopReceive();

                    SetRadarUdpConnectionState(
                        ConnectionState.Disconnected);
                }

                // [RabbitMQ] 수신 중지
                //
                // [MCB] / [SCB] 장비 연결 해제 시,
                // RabbitMQ 수신 상태가 Connected로 남지 않도록
                // 실행 중인 MQ 수신을 먼저 중지한다.
                if (_rabbitMqConnectionState == ConnectionState.Connected)
                {
                    // [카메라 상태] 주기 송신 중지
                    //
                    // 장비 연결 해제 시,
                    // 실행 중인 [q.status.res] 상태 송신 Loop를 함께 종료한다.
                    _cseCommandHandler
                        .StopCameraStatusPublishService();

                    _mqReceiver
                        .StopReceive();

                    _isCseMqReceiveStarted =
                        false;

                    SetRabbitMqConnectionState(
                        ConnectionState.Disconnected);
                }

                _ads1000ConnectionService.Disconnect();

                MainStatusText =
                    "MCB / SCB DISCONNECTED";

                OperationModeText =
                    "MODE STANDBY";

                // 장비 연결 해제 상태 반영
                SetDeviceConnectionState(
                    ConnectionState.Disconnected,
                    ConnectionState.Disconnected);

                // [Camera] 연결 상태 저장
                //
                // 연결 해제 시 [CSE] 상태 조회 응답에서
                // 미연결 상태로 반환될 수 있도록 갱신한다.
                _cameraStateProvider.UpdateConnectionState(
                    false);

                // [EO] 영상 표시 차단
                //
                // 연결 중 해제 시에도
                // 뒤늦게 수신되는 [Frame] 표시를 방지한다.
                _isEoVideoDisplayEnabled =
                    false;

                // [EO] [RTSP] 테스트 영상 연결 해제
                _eoCameraService.Disconnect();

                // [EO] 영상 화면 초기화
                //
                // [RTSP] 연결 해제 후에도
                // 마지막 [Frame]이 화면에 남지 않도록
                // [Image] 바인딩 값을 비운다.
                EOCameraImage = null;
            }
            finally
            {
                _isDeviceDisconnecting =
                    false;

                // [장비 연결 버튼] 활성화 상태 갱신
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [장비 연결 해제 버튼] 활성화 상태 갱신
                OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));
            }
            return Task.CompletedTask;
        }

        #endregion

        #region [UDP Connection Methods]

        /// <summary>
        /// [Radar] UDP Loopback 테스트
        /// 
        /// 실제 Radar 장비 연동 전,
        /// UDP Loopback 방식으로 Tracking Request / BIST Request를 송신하여
        /// Radar UDP 수신 / Packet 파싱 / 응답 생성 / ADS1000 제어 흐름을 검증한다.
        /// </summary>
        private async Task RunRadarUdpLoopbackTestAsync()
        {
            // [Radar] Tracking 테스트 지연
            //
            // 장비 연결 직후 바로 카메라가 움직이면
            // EO 영상 화면에서 이동 전 상태를 확인하기 어렵기 때문에,
            // 영상 연결 및 초기 화면 표시 시간을 확보한다.
            await Task.Delay(
                5000);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RADAR][UDP][MOCK] Loopback Tracking Test Start");
            ConsoleLogHelper.PrintLine();

            _radarUdpMockSenderService
                .SendTrackingRequest(
                    RadarUdpIpAddress,
                    RadarUdpLocalPort);

            // [Radar] BIST 테스트 지연
            //
            // Tracking Request 처리 및 Pan / Tilt 이동 로그 확인 후,
            // BIST Request 응답 흐름을 분리해서 확인하기 위해 대기한다.
            await Task.Delay(
                3000);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RADAR][UDP][MOCK] Loopback BIST Test Start");
            ConsoleLogHelper.PrintLine();

            _radarUdpMockSenderService
                .SendBistRequest(
                    RadarUdpIpAddress,
                    RadarUdpLocalPort);
        }

        /// <summary>
        /// [Radar] UDP 연결 상태 반영
        /// </summary>
        /// <param name="connectionState">
        /// [Radar] UDP 연결 상태
        /// </param>
        private void SetRadarUdpConnectionState(
            ConnectionState connectionState)
        {
            // [Radar UDP] 연결 상태 저장
            //
            // [Radar UDP] 수신 시작 / 중지 여부를
            // 내부 상태값에 반영한다.
            _radarUdpConnectionState =
                connectionState;

            // [Radar UDP] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(RadarUdpConnectionStatusText));
            OnPropertyChanged(nameof(RadarUdpConnectionStatusBrush));

            // [Radar UDP 수신 시작] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // [UDP START] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpStartButtonEnabled));

            // [Radar UDP 수신 중지] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // [UDP STOP] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpStopButtonEnabled));

            // [Radar UDP 통신 설정] 입력 가능 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // Radar UDP IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));
        }

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// 
        /// 화면에서 입력한 [Radar UDP Port]를 기준으로
        /// Radar Packet 수신을 시작한다.
        /// </summary>
        private async void StartRadarUdpReceive()
        {
            if (_mcbConnectionState != ConnectionState.Connected ||
                _scbConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Start Failed : MCB / SCB Not Connected");
                Console.WriteLine();

                return;
            }

            if (_radarUdpConnectionState == ConnectionState.Connected ||
                _radarUdpConnectionState == ConnectionState.Connecting)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Start Ignored : Already Started");
                Console.WriteLine();

                return;
            }

            try
            {
                SetRadarUdpConnectionState(
                    ConnectionState.Connecting);

                // [Radar UDP] 연결 상태 표시 지연
                //
                // UDP는 TCP처럼 연결 Handshake가 없기 때문에
                // 수신 시작 처리가 즉시 완료된다.
                // 화면에서 [Connecting] 상태가 너무 빠르게 지나가지 않도록
                // 짧은 표시 지연을 둔다.
                await Task.Delay(
                    500);

                _radarUdpService
                    .StartReceive(
                        RadarUdpLocalPort);

                SetRadarUdpConnectionState(
                    ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                SetRadarUdpConnectionState(
                    ConnectionState.Disconnected);

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Start Failed");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// 
        /// 현재 실행 중인 Radar UDP 수신을 중지한다.
        /// </summary>
        private void StopRadarUdpReceive()
        {
            if (_radarUdpConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Stop Ignored : Not Started");
                Console.WriteLine();

                return;
            }

            try
            {
                _radarUdpService
                    .StopReceive();

                SetRadarUdpConnectionState(
                    ConnectionState.Disconnected);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Stop Failed");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

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
            List<Ads1000StatusResult> statusResults =
                _ads1000StatusService.ProcessReceivedPackets(
                    deviceName,
                    packet);

            if (statusResults.Count == 0)
                return;

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (Ads1000StatusResult statusResult in statusResults)
                {
                    if (!statusResult.IsValid)
                        continue;

                    ApplyParsedStatusValue(
                        statusResult.ParsedPacket);

                    if ((DateTime.Now - _lastAds1000StatusLogTime).TotalSeconds >= 3)
                    {
                        _lastAds1000StatusLogTime =
                            DateTime.Now;

                        Console.WriteLine(
                            $"[ADS1000] Pan   : {CurrentPan:F4}");

                        Console.WriteLine(
                            $"[ADS1000] Tilt  : {CurrentTilt:F4}");

                        Console.WriteLine(
                            $"[ADS1000] Zoom  : {CurrentZoom:F0}");

                        Console.WriteLine(
                            $"[ADS1000] Focus : {CurrentFocus:F0}");

                        ConsoleLogHelper.PrintLine();
                    }

                }

            }));

        }

        #endregion

        #region [EO Camera Event Methods]

        /// <summary>
        /// [EO Camera] Frame 수신 처리
        /// 
        /// RTSP 수신 서비스에서 전달된 BitmapSource Frame을
        /// UI Binding 속성에 반영한다.
        /// 
        /// 프로그램 종료 중이거나 Frame 데이터가 없는 경우에는
        /// UI 객체 접근을 수행하지 않는다.
        /// </summary>
        /// <param name="bitmap">
        /// EO Camera Frame Image
        /// </param>
        private void OnEoCameraFrameReceived(
            BitmapSource bitmap)
        {
            if (_isDisposing)
            {
                return;
            }

            if (bitmap == null)
            {
                return;
            }

            try
            {
                // [EO Camera] Frame Freeze 처리
                //
                // RTSP 수신 Thread에서 생성된 BitmapSource를
                // UI Thread Binding 속성에 안전하게 전달하기 위해 Freeze 처리한다.
                if (bitmap.CanFreeze)
                {
                    bitmap.Freeze();
                }

                EOCameraImage =
                    bitmap;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(
                    "[EO CAMERA] Frame Update Skipped : "
                    + ex.Message);
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine(
                    "[EO CAMERA] Frame Update Skipped : View Disposed / "
                    + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[EO CAMERA] Frame Update Failed : "
                    + ex.Message);
            }

        }

        /// <summary>
        /// [EO] 영상 상태 변경 처리
        /// 
        /// [EoCameraService]에서 전달받은 상태 메시지를
        /// [OperationModeText]에 반영한다.
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

        #region [PTZ Control Mode Methods]

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정
        /// 
        /// 화면 버튼을 통해 [PTZ] 제어 모드를 [AUTO]로 변경한다.
        /// 
        /// 현재 단계에서는 실제 자동 추적 제어를 수행하지 않고,
        /// 이후 탐지 / 레이다 연동 시 자동 제어 허용 상태값으로 사용한다.
        /// </summary>
        private void SetPtzAutoMode()
        {
            SetPtzControlMode(
                "AUTO");
        }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정
        /// 
        /// 화면 버튼을 통해 [PTZ] 제어 모드를 [MANUAL]로 변경한다.
        /// 
        /// 수동 버튼 기반 [Pan] / [Tilt] / [Zoom] / [Focus]
        /// 제어를 기본 운용 모드로 사용한다.
        /// </summary>
        private void SetPtzManualMode()
        {
            SetPtzControlMode(
                "MANUAL");
        }

        /// <summary>
        /// [PTZ] 제어 모드 설정
        /// 
        /// [AUTO] / [MANUAL] 값을 [CameraStateProvider]에 저장하고,
        /// 화면 표시값과 로그를 갱신한다.
        /// </summary>
        /// <param name="mode">
        /// 설정할 [PTZ] 제어 모드
        /// </param>
        private void SetPtzControlMode(
            string mode)
        {
            if (string.IsNullOrWhiteSpace(
                mode))
            {
                Console.WriteLine("[UI][PTZ_MODE] Set Failed : Mode is empty");
                return;
            }

            string normalizedMode =
                mode.Trim().ToUpper();

            if (normalizedMode != "AUTO" &&
                normalizedMode != "MANUAL")
            {
                Console.WriteLine("[UI][PTZ_MODE] Set Failed : Unsupported Mode : " + mode);
                return;
            }

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[UI][PTZ_MODE] Set Request");
            Console.WriteLine("[UI][PTZ_MODE] Mode : " + normalizedMode);
            ConsoleLogHelper.PrintLine();

            _cameraStateProvider.UpdatePtzControlMode(
                normalizedMode);
        }

        /// <summary>
        /// [PTZ] 제어 모드 변경 처리
        /// 
        /// [CameraStateProvider]에서 [AUTO] / [MANUAL] 모드가 변경되면
        /// [XAML] 바인딩 속성을 갱신한다.
        /// </summary>
        /// <param name="mode">
        /// 변경된 [PTZ] 제어 모드
        /// </param>
        private void OnPtzControlModeChanged(
            string mode)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                PtzControlModeText =
                    mode;

                Console.WriteLine("[UI][PTZ_MODE] Current Mode : " + PtzControlModeText);
            }));

        }

        #endregion

        #region [Camera Continuous Control Methods]

        /// <summary>
        /// [Pan / Tilt] 연속 이동 속도 재적용
        /// 
        /// UI에서 Pan / Tilt 연속 이동 중 [Pan / Tilt Speed] 값이 변경된 경우,
        /// 현재 이동 중인 방향의 명령을 다시 송신하여
        /// 장비 실제 이동 속도에 변경값을 반영한다.
        /// 
        /// ADS1000 장비는 이동 중에도 [JV] 속도 명령을 다시 수신하면
        /// 현재 이동 속도를 갱신할 수 있으므로,
        /// 별도 정지 명령 없이 동일 방향의 연속 이동 명령을 재송신한다.
        /// </summary>
        private void ApplyCurrentPanTiltContinuousMoveSpeed()
        {
            if (!_isUiContinuousMoveStarted)
            {
                return;
            }

            if (_currentPanTiltContinuousMoveDirection == PanTiltContinuousMoveDirection.None)
            {
                return;
            }

            Console.WriteLine(
                "[UI][PTZ] Pan / Tilt Speed Changed : "
                + PanTiltSpeedLevel.ToString("F0"));

            switch (_currentPanTiltContinuousMoveDirection)
            {
                case PanTiltContinuousMoveDirection.PanLeft:
                    _ads1000CameraControlService
                        .PanLeft();

                    break;

                case PanTiltContinuousMoveDirection.PanRight:
                    _ads1000CameraControlService
                        .PanRight();

                    break;

                case PanTiltContinuousMoveDirection.TiltUp:
                    _ads1000CameraControlService
                        .TiltUp();

                    break;

                case PanTiltContinuousMoveDirection.TiltDown:
                    _ads1000CameraControlService
                        .TiltDown();

                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// [Pan / Tilt] 이동 속도 재적용
        /// 
        /// UI에서 Pan / Tilt 이동 중 [Pan / Tilt Speed] 값이 변경된 경우,
        /// 현재 이동 중인 축에 속도 갱신 명령을 송신하여
        /// 장비 실제 이동 속도에 변경값을 반영한다.
        /// 
        /// Absolute 이동은 [SP=속도;BG;] 형식으로 속도 변경을 반영하고,
        /// Relative 이동은 기존 [PR] 상대 이동량이 다시 실행되지 않도록
        /// [SP=속도;] 형식으로만 송신한다.
        /// </summary>
        private void ApplyCurrentPanTiltMoveSpeed()
        {
            if (_isHomePositionMoving)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan / Tilt Speed Apply Ignored : Home Position Moving");

                return;
            }

            if (_currentPanTiltMoveAxis == PanTiltMoveAxis.None ||
                _currentPanTiltMoveType == PanTiltMoveType.None)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan / Tilt Speed Apply Ignored : Pan / Tilt Move State None");

                return;
            }

            bool includeBeginCommand =
                _currentPanTiltMoveType == PanTiltMoveType.Absolute;

            Console.WriteLine(
                "[UI][PTZ] Pan / Tilt Speed Apply : "
                + PanTiltSpeedLevel.ToString("F0")
                + " / "
                + _currentPanTiltMoveAxis
                + " / "
                + _currentPanTiltMoveType
                + " / BG="
                + includeBeginCommand);

            switch (_currentPanTiltMoveAxis)
            {
                case PanTiltMoveAxis.Pan:
                    _ads1000CameraControlService
                        .UpdatePanMoveSpeed(
                            includeBeginCommand);

                    break;

                case PanTiltMoveAxis.Tilt:
                    _ads1000CameraControlService
                        .UpdateTiltMoveSpeed(
                            includeBeginCommand);

                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// [Pan] 왼쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Pan] 왼쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이후 이동 중 [Pan / Tilt Speed] 값이 변경될 경우,
        /// 동일 방향 명령을 다시 송신할 수 있도록 현재 이동 방향을 저장한다.
        /// </summary>
        public void StartPanLeftMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanLeft;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .PanLeft();
        }

        /// <summary>
        /// [Pan] 오른쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Pan] 오른쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이후 이동 중 [Pan / Tilt Speed] 값이 변경될 경우,
        /// 동일 방향 명령을 다시 송신할 수 있도록 현재 이동 방향을 저장한다.
        /// </summary>
        public void StartPanRightMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanRight;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .PanRight();
        }

        /// <summary>
        /// [Tilt] 위쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Tilt] 위쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이후 이동 중 [Pan / Tilt Speed] 값이 변경될 경우,
        /// 동일 방향 명령을 다시 송신할 수 있도록 현재 이동 방향을 저장한다.
        /// </summary>
        public void StartTiltUpMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltUp;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .TiltUp();
        }

        /// <summary>
        /// [Tilt] 아래쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Tilt] 아래쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이후 이동 중 [Pan / Tilt Speed] 값이 변경될 경우,
        /// 동일 방향 명령을 다시 송신할 수 있도록 현재 이동 방향을 저장한다.
        /// </summary>
        public void StartTiltDownMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltDown;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .TiltDown();
        }

        /// <summary>
        /// [Zoom] 확대 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Zoom] 확대 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartZoomInMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .ZoomIn();
        }

        /// <summary>
        /// [Zoom] 축소 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Zoom] 축소 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartZoomOutMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .ZoomOut();
        }

        /// <summary>
        /// [Focus] Near 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Focus] Near 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartFocusNearMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .FocusNear();
        }

        /// <summary>
        /// [Focus] Far 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Focus] Far 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartFocusFarMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .FocusFar();
        }

        /// <summary>
        /// [UI] 장비 이동 정지
        /// 
        /// 화면 버튼을 통해 시작된
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 연속 이동뿐만 아니라,
        /// [Pan] / [Tilt] Absolute / Relative 위치 이동 중에도
        /// 정지 명령을 송신한다.
        /// 
        /// STOP 명령은 장비 이동 상태를 강제로 정지시키는 용도이므로,
        /// UI 내부 이동 상태값만 기준으로 차단하지 않고
        /// 장비가 연결된 상태라면 정지 명령을 송신한다.
        /// </summary>
        public void StopContinuousMove()
        {
            if (_mcbConnectionState != ConnectionState.Connected &&
                _scbConnectionState != ConnectionState.Connected)
            {
                Console.WriteLine(
                    "[UI][CMD] Stop Ignored : Device Not Connected");

                ConsoleLogHelper.PrintLine();

                return;
            }

            Console.WriteLine(
                "[UI][CMD] Stop Move Request");

            _isUiContinuousMoveStarted =
                false;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .StopMove();

            ConsoleLogHelper.PrintLine();

            OnPropertyChanged(nameof(IsPanTiltSpeedEnabled));
        }

        #endregion

        #region [Camera Home / Zero Control Methods]

        /// <summary>
        /// [Home Position] 이동
        /// 
        /// 사용자가 [HOME POSITION] 버튼을 누른 경우,
        /// 장비 기준 Home Position으로 이동한다.
        /// 
        /// Home Position 이동 중에는
        /// 다른 운용 제어 / 이동 제어 명령이 중복 송신되지 않도록
        /// 장비 제어 영역을 비활성화한다.
        /// </summary>
        private async Task MoveHomePositionAsync()
        {
            await MoveHomePositionWithControlLockAsync(
                "[UI][PTZ] Home Position");
        }

        /// <summary>
        /// [Home Position] 이동 공통 처리
        /// 
        /// Home Position 이동 시작 시
        /// 장비 연결 / 해제 버튼 및 운용 제어 / 이동 제어 영역을 비활성화하고,
        /// 문서 기준 [Pan Home] / [Tilt Home] 명령을 송신한다.
        /// 
        /// Home Position 완료 응답을 별도로 판단하지 않고,
        /// 현재 Pan / Tilt 상태값이 [0] 부근으로 수렴했는지 확인하여
        /// Home Position 이동 완료 여부를 판단한다.
        /// </summary>
        /// <param name="logPrefix">
        /// 로그 출력 구분 문자열
        /// </param>
        private async Task MoveHomePositionWithControlLockAsync(
            string logPrefix)
        {
            if (_isHomePositionMoving)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    logPrefix
                    + " Ignored : Home Position Moving");

                Console.WriteLine();

                return;
            }

            if (_mcbConnectionState != ConnectionState.Connected ||
                _scbConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    logPrefix
                    + " Skipped : Device Not Fully Connected");

                Console.WriteLine();

                return;
            }

            try
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    logPrefix
                    + " Move Start");

                // [Pan / Tilt] 이동 상태 초기화
                //
                // Home Position은 장비 내부 Script로 동작하므로,
                // Absolute / Relative / Continuous 이동 중 속도 갱신 대상으로 보지 않는다.
                _currentPanTiltContinuousMoveDirection =
                    PanTiltContinuousMoveDirection.None;

                _currentPanTiltMoveAxis =
                    PanTiltMoveAxis.None;

                _currentPanTiltMoveType =
                    PanTiltMoveType.None;

                SetHomePositionMovingState(
                    true);

                // [Home Position] 이동
                //
                // 문서 기준 [Pan Home] / [Tilt Home] 명령인
                // [XQ##START;]를 순차 송신한다.
                _ads1000CameraControlService
                    .MoveHomePosition();

                bool isHomePositionCompleted =
                    await WaitHomePositionCompletedAsync();

                if (isHomePositionCompleted)
                {
                    // [Pan] 누적 상태값 초기화
                    //
                    // Home Position 이동이 완료되면
                    // 장비 기준 Pan 위치가 [0]으로 복귀한 상태이므로,
                    // 소프트웨어에서 관리하는 Pan 누적 위치값도 함께 초기화한다.
                    ResetPanAccumulatedStatus();

                    Console.WriteLine(
                        logPrefix
                        + " Move Complete");
                }
                else
                {
                    Console.WriteLine(
                        logPrefix
                        + " Move Timeout");
                }

            }
            finally
            {
                // [Pan / Tilt] 이동 상태 초기화
                //
                // Home Position 종료 후에는
                // 이전 이동 상태 기준으로 속도 갱신 명령이 송신되지 않도록 초기화한다.
                _currentPanTiltContinuousMoveDirection =
                    PanTiltContinuousMoveDirection.None;

                _currentPanTiltMoveAxis =
                    PanTiltMoveAxis.None;

                _currentPanTiltMoveType =
                    PanTiltMoveType.None;

                SetHomePositionMovingState(
                    false);

                ConsoleLogHelper.PrintLine();
            }

        }

        /// <summary>
        /// [Pan] 현재 위치 [0] 설정
        /// 
        /// 장비 기준 현재 [Pan] 위치를 [0]으로 재설정하고,
        /// 소프트웨어에서 관리하는 Pan 누적 위치값도 함께 초기화한다.
        /// </summary>
        private void SetPanZero()
        {
            _ads1000CameraControlService
                .SetPanZero();

            // [Pan] 누적 상태값 초기화
            //
            // Pan Zero 명령은 현재 장비 Pan 위치를 [0]으로 재설정하므로,
            // 이후 Absolute Pan 제어가 이전 누적 회전값을 기준으로 계산되지 않도록
            // 내부 누적 위치값도 함께 초기화한다.
            ResetPanAccumulatedStatus();
        }

        /// <summary>
        /// [Home Position] 이동 완료 대기
        /// 
        /// Home Position 명령 송신 후
        /// 현재 Pan / Tilt 상태값이 [0] 부근으로 수렴했는지 확인한다.
        /// 
        /// 명확한 완료 응답 Packet을 사용하지 않으므로,
        /// 상태값 기준으로 일정 횟수 연속 만족 시 완료로 판단한다.
        /// </summary>
        /// <returns>
        /// Home Position 완료 여부
        /// </returns>
        private async Task<bool> WaitHomePositionCompletedAsync()
        {
            const int MIN_WAIT_MILLISECONDS =
                1500;

            const int CHECK_INTERVAL_MILLISECONDS =
                200;

            const int TIMEOUT_MILLISECONDS =
                20000;

            const int REQUIRED_STABLE_COUNT =
                3;

            const double PAN_TOLERANCE_DEGREES =
                0.2;

            const double TILT_TOLERANCE_DEGREES =
                0.2;

            // [Home Position] 최소 대기
            //
            // Home Position 명령 송신 직후에는
            // 장비 상태값이 아직 이동 전 위치로 들어올 수 있으므로
            // 안정 상태 판단 전 최소 대기 시간을 둔다.
            await Task.Delay(
                MIN_WAIT_MILLISECONDS);

            int stableCount =
                0;

            int elapsedMilliseconds =
                MIN_WAIT_MILLISECONDS;

            while (elapsedMilliseconds < TIMEOUT_MILLISECONDS)
            {
                if (_mcbConnectionState != ConnectionState.Connected ||
                    _scbConnectionState != ConnectionState.Connected)
                {
                    Console.WriteLine(
                        "[DEVICE] Home Position Wait Canceled : Device Disconnected");

                    return false;
                }

                double currentPan =
                    NormalizePanStatus(
                        CurrentPan);

                double currentTilt =
                    NormalizeTiltStatus(
                        CurrentTilt);

                bool isPanHome =
                    Math.Abs(currentPan) <= PAN_TOLERANCE_DEGREES;

                bool isTiltHome =
                    Math.Abs(currentTilt) <= TILT_TOLERANCE_DEGREES;

                if (isPanHome &&
                    isTiltHome)
                {
                    stableCount++;

                    if (stableCount >= REQUIRED_STABLE_COUNT)
                    {
                        Console.WriteLine(
                            "[DEVICE] Home Position Stable Count : "
                            + stableCount);

                        return true;
                    }

                }
                else
                {
                    stableCount =
                        0;
                }

                await Task.Delay(
                    CHECK_INTERVAL_MILLISECONDS);

                elapsedMilliseconds +=
                    CHECK_INTERVAL_MILLISECONDS;
            }
            return false;
        }

        #endregion

        #region [Camera Absolute Control Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동
        /// 
        /// 입력된 [Pan Absolute] 목표값을
        /// 최종 ICD 기준 [0 ~ 360] 범위로 보정한 후,
        /// 현재 [Pan] 위치와 선택된 선회 모드를 기준으로 이동 각도를 계산한다.
        /// 
        /// 화면 표시용 [Pan] 값은 [0 ~ 360] 범위로 정규화되지만,
        /// 장비의 실제 Pan 위치는 한 바퀴 이상 누적될 수 있으므로
        /// 장비 송신용 목표값은 내부 누적 위치값을 기준으로 계산한다.
        /// 
        /// 단, 현재 표시 위치와 목표 표시 위치가 이미 동일한 경우에는
        /// 장비에 불필요한 [PA] 명령을 송신하지 않는다.
        /// 
        /// [360] 입력은 [0]과 표시 위치는 같지만,
        /// 사용자가 한 바퀴 이동을 의도한 값으로 보고 별도로 처리한다.
        /// </summary>
        private void MovePanAbsolute()
        {
            const double PAN_POSITION_EPSILON =
                0.001;

            if (!PanAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Failed : Value is empty");

                return;
            }

            double currentPanCommandAngle =
                GetCurrentPanCommandAngle();

            double currentPan =
                NormalizePanStatus(
                    currentPanCommandAngle);

            double targetPan =
                Clamp(
                    PanAbsoluteValue.Value,
                    0,
                    360);

            bool isFullTurnTarget =
                Math.Abs(targetPan - 360.0) <= PAN_POSITION_EPSILON;

            double panMoveAngle;

            if (isFullTurnTarget)
            {
                panMoveAngle =
                    360.0 - currentPan;
            }
            else
            {
                panMoveAngle =
                    CalculatePanMoveAngle(
                        currentPan,
                        targetPan,
                        _panTurnMode);
            }

            // [Pan Absolute] 동일 위치 명령 차단
            //
            // 현재 표시용 [Pan] 위치와 목표 [Pan] 위치가 이미 동일한 경우,
            // 장비에 불필요한 [PA] 명령을 송신하지 않는다.
            //
            // 단, [360] 입력은 표시 위치상 [0]과 같더라도
            // 사용자가 한 바퀴 이동을 의도한 값으로 보므로
            // 동일 위치 차단 대상에서 제외한다.
            if (!isFullTurnTarget &&
                Math.Abs(panMoveAngle) <= PAN_POSITION_EPSILON)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Ignored : Already Target Position");

                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Current : "
                    + currentPan.ToString("F4"));

                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Target : "
                    + targetPan.ToString("F4"));

                return;
            }

            double panCommandTarget =
                currentPanCommandAngle + panMoveAngle;

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Input : "
                + PanAbsoluteValue.Value.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Mode : "
                + _panTurnMode);

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Current : "
                + currentPan.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Accumulated Current : "
                + currentPanCommandAngle.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Target : "
                + targetPan.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Move Angle : "
                + panMoveAngle.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Command Target : "
                + panCommandTarget.ToString("F4"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MovePanAbsolute(
                    panCommandTarget);
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동
        /// 
        /// 입력된 [Tilt Absolute] 값을
        /// 장비 물리 제한 기준 [-90 ~ 90] 범위로 보정한 후,
        /// [ADS1000] 장비에 절대 위치 이동 명령을 송신한다.
        /// 
        /// 현재 [Tilt] 상태값과 목표 위치를 로그로 출력하여,
        /// 실제 장비 응답값과 비교할 수 있도록 한다.
        /// </summary>
        private void MoveTiltAbsolute()
        {
            if (!TiltAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Failed : Value is empty");

                return;
            }

            double currentTilt =
                NormalizeTiltStatus(
                    CurrentTilt);

            double targetTilt =
                Clamp(
                    TiltAbsoluteValue.Value,
                    -90,
                    90);

            double tiltMoveAngle =
                targetTilt - currentTilt;

            // [Tilt Absolute] 동일 위치 명령 차단
            //
            // 현재 [Tilt] 위치와 목표 [Tilt] 위치가 이미 동일한 경우,
            // 장비에 불필요한 [PA] 명령을 송신하지 않는다.
            if (Math.Abs(tiltMoveAngle) <= 0.001)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Ignored : Already Target Position");

                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Current : "
                    + currentTilt.ToString("F4"));

                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Target : "
                    + targetTilt.ToString("F4"));

                return;
            }

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Input : "
                + TiltAbsoluteValue.Value.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Current : "
                + currentTilt.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Target : "
                + targetTilt.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Move Angle : "
                + tiltMoveAngle.ToString("F4"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MoveTiltAbsolute(
                    targetTilt);
        }

        #endregion

        #region [Camera Relative Control Methods]

        /// <summary>
        /// [Pan] 상대 위치 이동
        /// 
        /// 입력된 [Pan Relative] 값을 기준으로
        /// 현재 [Pan] 누적 위치에서 상대 이동량을 더한
        /// 최종 목표 위치를 계산한 후,
        /// [ADS1000] 장비에는 절대 위치 이동 명령으로 송신한다.
        /// 
        /// 장비의 [PR] 상대 이동 명령은 이동 중 속도 변경 시
        /// [SP] 단독 갱신이 즉시 반영되지 않고,
        /// [BG] 재송신 시 상대 이동량이 재실행될 수 있으므로,
        /// UI 상대 이동은 내부적으로 [PA] 절대 이동으로 변환하여 처리한다.
        /// </summary>
        private void MovePanRelative()
        {
            if (!PanRelativeValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Relative Failed : Value is empty");

                return;
            }

            double currentPanCommandAngle =
                GetCurrentPanCommandAngle();

            double currentPan =
                NormalizePanStatus(
                    currentPanCommandAngle);

            double movePan =
                PanRelativeValue.Value;

            double panCommandTarget =
                currentPanCommandAngle + movePan;

            double expectedPan =
                NormalizePanStatus(
                    currentPan + movePan);

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Input : "
                + movePan.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Current : "
                + currentPan.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Accumulated Current : "
                + currentPanCommandAngle.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Move Angle : "
                + movePan.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Expected Display : "
                + expectedPan.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Command Target : "
                + panCommandTarget.ToString("F4"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            // [Pan Relative] 속도 변경 처리 기준
            //
            // UI 동작은 Relative이지만,
            // 장비에는 [PA] 절대 위치 이동 명령으로 송신하므로
            // 이동 중 속도 변경도 Absolute 방식인 [SP=속도;BG;]를 사용한다.
            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MovePanAbsolute(
                    panCommandTarget);
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동
        /// 
        /// 입력된 [Tilt Relative] 값을 기준으로
        /// 현재 [Tilt] 위치에서 상대 이동량을 더한
        /// 최종 목표 위치를 계산한 후,
        /// [ADS1000] 장비에는 절대 위치 이동 명령으로 송신한다.
        /// 
        /// 장비의 [PR] 상대 이동 명령은 이동 중 속도 변경 시
        /// [SP] 단독 갱신이 즉시 반영되지 않고,
        /// [BG] 재송신 시 상대 이동량이 재실행될 수 있으므로,
        /// UI 상대 이동은 내부적으로 [PA] 절대 이동으로 변환하여 처리한다.
        /// </summary>
        private void MoveTiltRelative()
        {
            if (!TiltRelativeValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Relative Failed : Value is empty");

                return;
            }

            double currentTilt =
                NormalizeTiltStatus(
                    CurrentTilt);

            double moveTilt =
                TiltRelativeValue.Value;

            double targetTilt =
                Clamp(
                    currentTilt + moveTilt,
                    -90,
                    90);

            double expectedTilt =
                NormalizeTiltStatus(
                    targetTilt);

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Input : "
                + moveTilt.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Current : "
                + currentTilt.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Move Angle : "
                + moveTilt.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Expected Display : "
                + expectedTilt.ToString("F4"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Command Target : "
                + targetTilt.ToString("F4"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            // [Tilt Relative] 속도 변경 처리 기준
            //
            // UI 동작은 Relative이지만,
            // 장비에는 [PA] 절대 위치 이동 명령으로 송신하므로
            // 이동 중 속도 변경도 Absolute 방식인 [SP=속도;BG;]를 사용한다.
            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MoveTiltAbsolute(
                    targetTilt);
        }

        #endregion

        #region [Position Input Initialize Methods]

        /// <summary>
        /// [위치 제어] 입력값 초기화
        /// 
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 위치 제어 입력칸을
        /// 기본값으로 초기화한다.
        /// 
        /// [Zoom Ratio]는 최소 배율 [1x] 기준으로 초기화하고,
        /// 실제 장비 위치값은 변경하지 않는다.
        /// </summary>
        private void ResetPositionInput()
        {
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

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[UI][POSITION] Input Reset");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Zoom / Focus Position Control Methods]

        /// <summary>
        /// [Zoom] 지정 위치 이동
        /// 
        /// 입력된 [Zoom Position] 값을
        /// [0 ~ 1000] 범위로 보정한 후
        /// [ADS1000] 장비에 위치 이동 명령을 전송한다.
        /// </summary>
        private void SetZoomPosition()
        {
            if (!ZoomPositionValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][POSITION] Zoom Failed : Value is empty");

                return;
            }

            double zoom =
                Clamp(
                    ZoomPositionValue.Value,
                    0,
                    1000);

            Console.WriteLine("[UI][POSITION] Set Zoom Target : " + zoom);
            Console.WriteLine("[UI][POSITION] Current Zoom Before : " + CurrentZoom);

            _ads1000CameraControlService
                .MoveZoomPosition(
                    (ushort)zoom);
        }

        /// <summary>
        /// [Zoom] 배율 기준 위치 이동
        /// 
        /// 입력된 [Zoom Ratio] 값을
        /// 실제 배율 기준으로 보정한 후,
        /// [ADS1000] 장비 위치값 [0 ~ 1000]으로 변환하여 전송한다.
        /// 
        /// 장비 스펙 기준 최대 배율을 [66x] 기준으로 구현한다.
        /// </summary>
        private void SetZoomRatio()
        {
            if (!ZoomRatioValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][ZOOM] Ratio Failed : Value is empty");

                return;
            }

            ushort zoomPosition =
                ConvertZoomRatioToPosition(
                    ZoomRatioValue.Value);

            Console.WriteLine("[UI][ZOOM] Set Ratio Target : " + ZoomRatioValue.Value);
            Console.WriteLine("[UI][ZOOM] Converted Position : " + zoomPosition);
            Console.WriteLine("[UI][ZOOM] Current Zoom Before : " + CurrentZoom);

            _ads1000CameraControlService
                .MoveZoomPosition(
                    zoomPosition);
        }

        /// <summary>
        /// [Zoom] 배율을 [ADS1000] 위치값으로 변환
        /// 
        /// [UI] 또는 [ICD]에서 사용하는 [Zoom] 배율값을
        /// [ADS1000] 제어용 [0 ~ 1000] 위치값으로 변환한다.
        /// 
        /// 변환 기준:
        /// [1x]  = 0
        /// [66x] = 1000
        /// </summary>
        /// <param name="zoomRatio">
        /// Zoom 배율
        /// </param>
        /// <returns>
        /// ADS1000 Zoom 위치값
        /// </returns>
        private ushort ConvertZoomRatioToPosition(
            double zoomRatio)
        {
            const double MIN_ZOOM_RATIO =
                1.0;

            const double MAX_ZOOM_RATIO =
                66.0;

            double clampedZoomRatio =
                Clamp(
                    zoomRatio,
                    MIN_ZOOM_RATIO,
                    MAX_ZOOM_RATIO);

            double zoomPosition =
                (clampedZoomRatio - MIN_ZOOM_RATIO)
                / (MAX_ZOOM_RATIO - MIN_ZOOM_RATIO)
                * 1000.0;

            return (ushort)Math.Round(
                zoomPosition);
        }

        /// <summary>
        /// [Zoom] 위치값을 배율로 변환
        /// 
        /// ADS1000 장비 상태값 [0 ~ 1000]을
        /// 화면 표시용 [Zoom] 배율값 [x1.0 ~ x66.0]으로 변환한다.
        /// 
        /// 변환 기준:
        /// [0]    = [1.0x]
        /// [1000] = [66.0x]
        /// 
        /// 화면 표시 기준으로 소수점 첫째 자리까지 반올림한다.
        /// </summary>
        /// <param name="zoomPosition">
        /// ADS1000 Zoom 위치값
        /// </param>
        /// <returns>
        /// Zoom 배율
        /// </returns>
        private double ConvertZoomPositionToRatio(
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
                Clamp(
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

        /// <summary>
        /// [Focus] 지정 위치 이동
        /// 
        /// 입력된 [Focus Position] 값을
        /// [0 ~ 1000] 범위로 보정한 후
        /// [ADS1000] 장비에 위치 이동 명령을 전송한다.
        /// </summary>
        private void SetFocusPosition()
        {
            if (!FocusPositionValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][POSITION] Focus Failed : Value is empty");

                return;
            }

            double focus =
                Clamp(
                    FocusPositionValue.Value,
                    0,
                    1000);

            Console.WriteLine("[UI][POSITION] Set Focus Target : " + focus);
            Console.WriteLine("[UI][POSITION] Current Focus Before : " + CurrentFocus);

            _ads1000CameraControlService
                .MoveFocusPosition(
                    (ushort)focus);
        }

        #endregion

        #region [Status Apply Methods]

        /// <summary>
        /// [ADS1000] 파싱 상태값 화면 반영
        /// 
        /// 수신 [Packet]에서 추출된
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 값을
        /// 화면 표시용 속성과 [CameraStateProvider]에 반영한다.
        /// </summary>
        /// <param name="parsedPacket">
        /// [ADS1000] 파싱 [Packet]
        /// </param>
        private void ApplyParsedStatusValue(
            Ads1000ParsedPacket parsedPacket)
        {
            double? updatedPan =
                null;

            double? updatedTilt =
                null;

            double? updatedZoom =
                null;

            double? updatedFocus =
                null;

            if (parsedPacket.HasPanValue)
            {
                // [Pan] 누적 상태값 갱신
                //
                // 화면 표시용 [Pan] 값은 [0 ~ 360] 범위로 정규화하지만,
                // 장비 제어용 [Pan] 값은 한 바퀴 이상 회전한 위치를 유지해야 하므로
                // 상태 Packet 수신 시 누적 위치값을 별도로 갱신한다.
                UpdatePanAccumulatedStatus(
                    parsedPacket.PanValue);

                // [Pan] 상태값 갱신
                //
                // Pan은 최종 ICD 기준 [0 ~ 360] 범위로 표시한다.
                // 장비 Encoder 오차로 인해 [0] / [360] 또는 정수 위치 근처
                // 미세 오차가 발생하면 화면 표시 및 상태 응답 기준에서는 보정한다.
                CurrentPan =
                    NormalizePanStatus(
                        parsedPacket.PanValue);

                updatedPan =
                    CurrentPan;
            }

            if (parsedPacket.HasTiltValue)
            {
                // [Tilt] 상태값 갱신
                //
                // Tilt는 장비 물리 제한 기준 [-90 ~ 90] 범위로 표시한다.
                // 장비 Encoder 오차로 인해 [0] 또는 정수 위치 근처
                // 미세 오차가 발생하면 화면 표시 및 상태 응답 기준에서는 보정한다.
                CurrentTilt =
                    NormalizeTiltStatus(
                        parsedPacket.TiltValue);

                updatedTilt =
                    CurrentTilt;
            }

            if (parsedPacket.HasZoomValue)
            {
                // [Zoom] 상태값 갱신
                //
                // Zoom Position은 장비 제어 기준 [0 ~ 1000] 범위로 표시한다.
                // 화면의 현재 상태 표시에서는 Position 값과 함께
                // 실제 배율 기준 [x1.0 ~ x66.0] 값을 소수점 첫째 자리까지 표시한다.
                //
                // 장비 응답값이 범위를 벗어나거나 정수 위치 근처
                // 미세 오차가 발생하면 화면 표시 및 상태 응답 기준에서는 보정한다.
                CurrentZoom =
                    NormalizeRangePosition(
                        parsedPacket.ZoomValue,
                        0,
                        1000);

                CurrentZoomRatio =
                    ConvertZoomPositionToRatio(
                        CurrentZoom);

                updatedZoom =
                    CurrentZoom;
            }

            if (parsedPacket.HasFocusValue)
            {
                // [Focus] 상태값 갱신
                //
                // Focus Position은 장비 제어 기준 [0 ~ 1000] 범위로 표시한다.
                // 장비 응답값이 범위를 벗어나거나 정수 위치 근처
                // 미세 오차가 발생하면 화면 표시 및 상태 응답 기준에서는 보정한다.
                CurrentFocus =
                    NormalizeRangePosition(
                        parsedPacket.FocusValue,
                        0,
                        1000);

                updatedFocus =
                    CurrentFocus;
            }

            // [Camera] 상태 저장소 갱신
            //
            // [CSE] 상태 조회 응답에서 사용할 수 있도록
            // 수신 [Packet]에 포함된 상태값만 저장한다.
            _cameraStateProvider.UpdateState(
                updatedPan,
                updatedTilt,
                updatedZoom,
                updatedFocus);
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
            _cseCommandHandler.HandleCommand(
                message);
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// [Pan] 누적 상태값 갱신
        /// 
        /// 장비 상태 Packet에서 수신한 [Pan] 원본 각도값을 기준으로
        /// 장비 제어용 누적 위치값을 갱신한다.
        /// 
        /// 화면 표시용 [Pan] 값은 [0 ~ 360] 범위로 정규화하지만,
        /// 장비 상태 Packet의 [Pan] 원본값은 한 바퀴 이상 회전한
        /// 누적 각도 정보를 포함할 수 있으므로 정규화하지 않고 보관한다.
        /// </summary>
        /// <param name="panStatus">
        /// 장비에서 수신한 Pan 원본 각도값
        /// </param>
        private void UpdatePanAccumulatedStatus(
            double panStatus)
        {
            _currentPanAccumulated =
                panStatus;

            _lastPanDisplayStatus =
                NormalizePanStatus(
                    panStatus);

            _hasPanAccumulatedStatus =
                true;
        }

        /// <summary>
        /// [Pan] 제어 기준 위치값 조회
        /// 
        /// Pan 누적 상태값이 초기화된 경우에는
        /// 장비 제어용 누적 위치값을 반환하고,
        /// 아직 상태값을 수신하지 못한 경우에는
        /// 화면 표시용 현재 Pan 값을 반환한다.
        /// </summary>
        /// <returns>
        /// Pan 제어 기준 위치값
        /// </returns>
        private double GetCurrentPanCommandAngle()
        {
            if (_hasPanAccumulatedStatus)
            {
                return _currentPanAccumulated;
            }

            return CurrentPan;
        }

        /// <summary>
        /// [Pan] 누적 상태값 초기화
        /// 
        /// Home Position 또는 Pan Zero 수행 후
        /// 장비 Pan 기준 위치가 [0]으로 재설정되는 경우,
        /// 소프트웨어에서 관리하는 누적 위치값도 함께 초기화한다.
        /// </summary>
        private void ResetPanAccumulatedStatus()
        {
            _currentPanAccumulated =
                0.0;

            _lastPanDisplayStatus =
                0.0;

            _hasPanAccumulatedStatus =
                true;
        }

        /// <summary>
        /// [Pan] 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치와 목표 [Pan] 위치를 기준으로
        /// 선택된 선회 모드에 따라 장비로 송신할 이동 각도를 계산한다.
        /// 
        /// [Short] 모드는 가장 가까운 방향의 이동 각도를 계산하고,
        /// [Via 0] 모드는 단거리 보정 없이 목표 위치와 현재 위치의 차이를 사용한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="panTurnMode">
        /// Pan 선회 모드
        /// </param>
        /// <returns>
        /// 장비로 송신할 Pan 이동 각도
        /// </returns>
        private double CalculatePanMoveAngle(
            double currentPan,
            double targetPan,
            Ads1000PanTurnMode panTurnMode)
        {
            double normalizedCurrentPan =
                NormalizePanStatus(
                    currentPan);

            double normalizedTargetPan =
                NormalizePanStatus(
                    targetPan);

            if (panTurnMode == Ads1000PanTurnMode.Short)
            {
                return CalculateShortestPanDelta(
                    normalizedCurrentPan,
                    normalizedTargetPan);
            }

            return CalculateViaZeroPanDelta(
                normalizedCurrentPan,
                normalizedTargetPan);
        }

        /// <summary>
        /// [Pan] 최단 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치에서 목표 [Pan] 위치까지
        /// 가장 가까운 방향의 이동 각도를 계산한다.
        /// 
        /// 결과값은 [-180 ~ 180] 범위로 반환하며,
        /// [0 → 350] 이동처럼 360도 경계를 넘어가는 경우에도
        /// 장비가 먼 방향으로 회전하지 않도록 처리한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <returns>
        /// 최단 이동 각도
        /// </returns>
        private double CalculateShortestPanDelta(
            double currentPan,
            double targetPan)
        {
            const double FULL_ROTATION_DEGREES =
                360.0;

            const double HALF_ROTATION_DEGREES =
                180.0;

            double delta =
                (targetPan
                 - currentPan
                 + HALF_ROTATION_DEGREES
                 + FULL_ROTATION_DEGREES)
                % FULL_ROTATION_DEGREES
                - HALF_ROTATION_DEGREES;

            return NormalizeZeroAngle(
                delta);
        }

        /// <summary>
        /// [Pan] [Via 0] 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치에서 목표 [Pan] 위치까지
        /// 단거리 보정 없이 이동 각도를 계산한다.
        /// 
        /// 예)
        /// 현재 [0] / 목표 [350]인 경우
        /// [Short] 모드는 [-10]으로 계산하지만,
        /// [Via 0] 모드는 [350]으로 계산한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <returns>
        /// Via 0 기준 이동 각도
        /// </returns>
        private double CalculateViaZeroPanDelta(
            double currentPan,
            double targetPan)
        {
            double delta =
                targetPan - currentPan;

            return NormalizeZeroAngle(
                delta);
        }

        /// <summary>
        /// [각도] 미세 오차 보정
        /// 
        /// 장비 Encoder 오차 또는 계산 과정에서 발생한
        /// [0] 근처 미세값을 [0]으로 보정한다.
        /// </summary>
        /// <param name="angle">
        /// 원본 각도
        /// </param>
        /// <returns>
        /// 미세 오차가 보정된 각도
        /// </returns>
        private double NormalizeZeroAngle(
            double angle)
        {
            const double ZERO_EPSILON =
                0.001;

            if (Math.Abs(angle) <= ZERO_EPSILON)
            {
                return 0.0;
            }
            return angle;
        }

        /// <summary>
        /// [Pan] 상태값 범위 정규화
        /// 
        /// ADS1000 상태 Packet에서 수신한 Pan 값을
        /// 최종 ICD 기준 [0 ~ 360] 범위로 변환한다.
        /// 
        /// Pan 값이 360도를 초과하면 0도부터 다시 시작하고,
        /// 0도 미만이면 360도 기준으로 순환 처리한다.
        /// 
        /// 장비 Encoder 오차로 인해
        /// [0] 근처 또는 [360] 근처의 미세 오차가 발생하는 경우,
        /// 화면 표시 및 상태 응답 기준에서는 [0]으로 보정한다.
        /// </summary>
        /// <param name="pan">
        /// Pan 원본 상태값
        /// </param>
        /// <returns>
        /// [0 ~ 360] 범위로 정규화된 Pan 상태값
        /// </returns>
        private double NormalizePanStatus(
            double pan)
        {
            const double FULL_ROTATION_DEGREES =
                360.0;

            const double ZERO_EPSILON =
                0.001;

            double normalizedPan =
                pan % FULL_ROTATION_DEGREES;

            if (normalizedPan < 0)
            {
                normalizedPan +=
                    FULL_ROTATION_DEGREES;
            }

            if (Math.Abs(normalizedPan) <= ZERO_EPSILON ||
                Math.Abs(normalizedPan - FULL_ROTATION_DEGREES) <= ZERO_EPSILON)
            {
                return 0.0;
            }
            return NormalizePosition(
                normalizedPan);
        }

        /// <summary>
        /// [Tilt] 상태값 범위 정규화
        /// 
        /// ADS1000 상태 Packet에서 수신한 Tilt 값을
        /// 장비 물리 제한 기준 [-90 ~ 90] 범위로 보정한다.
        /// 
        /// 장비 Encoder 오차로 인해
        /// [0] 근처의 미세 오차가 발생하는 경우,
        /// 화면 표시 및 상태 응답 기준에서는 [0]으로 보정한다.
        /// </summary>
        /// <param name="tilt">
        /// Tilt 원본 상태값
        /// </param>
        /// <returns>
        /// [-90 ~ 90] 범위로 정규화된 Tilt 상태값
        /// </returns>
        private double NormalizeTiltStatus(
            double tilt)
        {
            const double MIN_TILT_DEGREES =
                -90.0;

            const double MAX_TILT_DEGREES =
                90.0;

            const double ZERO_EPSILON =
                0.001;

            double normalizedTilt =
                Clamp(
                    tilt,
                    MIN_TILT_DEGREES,
                    MAX_TILT_DEGREES);

            if (Math.Abs(normalizedTilt) <= ZERO_EPSILON)
            {
                return 0.0;
            }
            return NormalizePosition(
                normalizedTilt);
        }

        /// <summary>
        /// [범위 위치 상태값] 미세 오차 보정
        /// 
        /// 장비 상태 Packet에서 수신한 위치값을
        /// 지정한 최소 / 최대 범위로 보정한다.
        /// 
        /// 장비 Encoder 또는 위치 응답에서 발생하는
        /// [0] 근처 또는 정수 위치 근처의 미세 오차는
        /// 화면 표시 및 상태 응답 기준에서 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 위치값
        /// </param>
        /// <param name="min">
        /// 최소 위치값
        /// </param>
        /// <param name="max">
        /// 최대 위치값
        /// </param>
        /// <returns>
        /// 범위 및 미세 오차가 보정된 위치값
        /// </returns>
        private double NormalizeRangePosition(
            double value,
            double min,
            double max)
        {
            double clampedValue =
                Clamp(
                    value,
                    min,
                    max);

            return NormalizePosition(
                clampedValue);
        }

        /// <summary>
        /// [위치 상태값] 미세 오차 보정
        /// 
        /// 장비 Encoder 또는 위치 응답에서 발생하는
        /// [0] 근처 또는 정수 위치 근처의 미세 오차를
        /// 화면 표시 및 상태 응답 기준에서 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 위치값
        /// </param>
        /// <returns>
        /// 미세 오차가 보정된 위치값
        /// </returns>
        private double NormalizePosition(
            double value)
        {
            const double ZERO_EPSILON =
                0.001;

            const double INTEGER_EPSILON =
                0.001;

            if (Math.Abs(value) <= ZERO_EPSILON)
            {
                return 0.0;
            }

            double roundedValue =
                Math.Round(
                    value);

            if (Math.Abs(value - roundedValue) <= INTEGER_EPSILON)
            {
                return roundedValue;
            }
            return value;
        }

        /// <summary>
        /// 입력값 범위 제한
        /// 
        /// 입력값이 지정된 최소 / 최대 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 값
        /// </param>
        /// <param name="min">
        /// 최소 허용값
        /// </param>
        /// <param name="max">
        /// 최대 허용값
        /// </param>
        /// <returns>
        /// 범위 제한이 적용된 값
        /// </returns>
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

        #region [Dispose Methods]

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
        #endregion
    }

}
