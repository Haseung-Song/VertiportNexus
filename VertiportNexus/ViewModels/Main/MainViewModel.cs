using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
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
    public partial class MainViewModel : INotifyPropertyChanged
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
        private PanTiltContinuousMoveDirection _currentPanTiltContinuousMoveDirection
            = PanTiltContinuousMoveDirection.None;

        /// <summary>
        /// 현재 [Pan] 연속 이동 진행 여부
        /// 
        /// 대각선 이동 및 키보드 동시 입력 시
        /// Pan 축 이동 상태를 Tilt 축과 분리해서 관리한다.
        /// </summary>
        private bool _isPanContinuousMoving;

        /// <summary>
        /// 현재 [Tilt] 연속 이동 진행 여부
        /// 
        /// 대각선 이동 및 키보드 동시 입력 시
        /// Tilt 축 이동 상태를 Pan 축과 분리해서 관리한다.
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
        /// 
        /// 방향키 조합으로 대각선 이동을 처리하기 위해
        /// 현재 눌려 있는 Pan Left 키 상태를 저장한다.
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

        /// <summary>
        /// [Pan] UI Zero Offset
        /// 
        /// 사용자가 [Pan Zero]를 설정한 시점의
        /// 실제 Pan 위치값을 저장한다.
        /// 
        /// 이후 UI 기준 Pan Target 값은
        /// 해당 Offset을 더해 장비 실제 이동 목표 위치로 변환한다.
        /// </summary>
        private double _panUiZeroOffset;

        /// <summary>
        /// [Tilt] UI Zero Offset
        /// 
        /// 사용자가 [Tilt Zero]를 설정한 시점의
        /// 실제 Tilt 위치값을 저장한다.
        /// 
        /// 이후 UI 기준 Tilt Target 값은
        /// 해당 Offset을 더해 장비 실제 이동 목표 위치로 변환한다.
        /// </summary>
        private double _tiltUiZeroOffset;

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

        /// <summary>
        /// [EO] RTSP 재연결 진행 여부
        /// 
        /// 장비 전원 직후 EO Camera가 아직 Ready 상태가 아닐 경우,
        /// CAMERA ERROR MODE 상태에서 RTSP 연결을 반복 재시도하기 위해 사용한다.
        /// </summary>
        private bool _isEoRtspReconnectRunning;

        /// <summary>
        /// [EO] RTSP 재연결 시도 번호
        /// </summary>
        private int _eoRtspReconnectTryCount;

        /// <summary>
        /// [EO] RTSP 연결 완료 여부
        /// 
        /// EO Camera RTSP 연결 성공 후
        /// Home Position 이동을 수행하기 위해 사용한다.
        /// </summary>
        private bool _isEoRtspConnected;

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
            // [CSE] detect_cont 처리 시,
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
            // 최종 ICD 기준 [detect_on] / [detect_off] / [detect_cont] /
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
                    SetTiltZero);

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

            Console.WriteLine(
                "[CAMERA][STATE] Pan Turn Mode : "
                + _panTurnMode);

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
    }

}
