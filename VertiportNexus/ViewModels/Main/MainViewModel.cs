using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Models.Camera;
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
        /// [Radar] 추적 제어 서비스
        /// 
        /// Radar Tracking Request에서 수신한
        /// 방위각 / 고각 정보를 ADS1000 Pan / Tilt 제어로 연결한다.
        /// </summary>
        private readonly RadarTrackingControlService _radarTrackingControlService;

        #endregion

        #region [Controller Fields]

        /// <summary>
        /// [RabbitMQ] 수신 Controller
        /// </summary>
        private readonly RabbitMqController _rabbitMqController;

        /// <summary>
        /// [Radar] UDP 수신 Controller
        /// </summary>
        private readonly RadarUdpController _radarUdpController;

        /// <summary>
        /// [Device Connection] Controller
        /// </summary>
        private readonly DeviceConnectionController _deviceConnectionController;

        /// <summary>
        /// [EO Camera] Controller
        /// </summary>
        private readonly EoCameraController _eoCameraController;

        /// <summary>
        /// [ADS1000 Receive] Controller
        /// </summary>
        private readonly Ads1000ReceiveController _ads1000ReceiveController;

        /// <summary>
        /// [ADS1000 Status Apply] Controller
        /// </summary>
        private readonly Ads1000StatusApplyController _ads1000StatusApplyController;

        /// <summary>
        /// [PTZ Absolute] Controller
        /// </summary>
        private readonly PtzAbsoluteController _ptzAbsoluteController;

        /// <summary>
        /// [PTZ Relative] Controller
        /// </summary>
        private readonly PtzRelativeController _ptzRelativeController;

        /// <summary>
        /// [PTZ Continuous] Controller
        /// </summary>
        private readonly PtzContinuousController _ptzContinuousController;

        /// <summary>
        /// [PTZ Home / Zero] Controller
        /// </summary>
        private readonly PtzHomeZeroController _ptzHomeZeroController;

        /// <summary>
        /// [Keyboard PTZ] Controller
        /// </summary>
        private readonly KeyboardPtzController _keyboardPtzController;

        /// <summary>
        /// [Zoom / Focus Position] Controller
        /// </summary>
        private readonly ZoomFocusPositionController _zoomFocusPositionController;

        /// <summary>
        /// [PTZ Mode] Controller
        /// </summary>
        private readonly PtzModeController _ptzModeController;

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
        /// [Dummy Tracking] 테스트 취소 토큰
        /// 
        /// 실제 탐지 객체 수신 전,
        /// 더미 Bounding Box를 주기적으로 생성하여
        /// AUTO Tracking 흐름을 검증하기 위해 사용한다.
        /// </summary>
        private CancellationTokenSource _dummyTrackingCancellationTokenSource;

        /// <summary>
        /// [Dummy Tracking] 테스트 실행 여부
        /// </summary>
        private bool _isDummyTrackingRunning;

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 동기화 객체
        /// </summary>
        private readonly object _dummyTrackingTargetLock =
            new object();

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 정보
        /// 
        /// 30Hz로 수신되는 더미 Bounding Box 중
        /// 가장 마지막 값을 저장한다.
        /// </summary>
        private DetectionBoundingBox _latestDummyTrackingBoundingBox;

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 수신 시간
        /// </summary>
        private DateTime _latestDummyTrackingReceivedTime;

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 Frame 번호
        /// </summary>
        private int _latestDummyTrackingFrameId;

        /// <summary>
        /// [Dummy Tracking] 마지막 처리 Frame 번호
        /// 
        /// 동일 Frame을 중복 처리하지 않기 위해 사용한다.
        /// </summary>
        private int _lastProcessedDummyTrackingFrameId =
            -1;

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
        /// [Home Position] 이동 중 CURRENT STATUS 표시 모드
        /// 
        /// Home Position 이동 중에는
        /// 사용자가 설정한 UI Zero Offset 기준이 아니라,
        /// 장비 Home 기준 Raw 상태값이 [0]으로 수렴하는 흐름을 표시하기 위해 사용한다.
        /// </summary>
        private bool _isHomePositionStatusDisplayMode;

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

        /// <summary>
        /// [Dummy Tracking] 테스트 시작 요청 [Command]
        /// </summary>
        public ICommand StartDummyTrackingTestCommand { get; }

        /// <summary>
        /// [Dummy Tracking] 테스트 중지 요청 [Command]
        /// </summary>
        public ICommand StopDummyTrackingTestCommand { get; }

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

            // [Radar] UDP 연동 서비스 생성
            //
            // UDP 수신 Packet을 Handler로 전달하고,
            // 처리 결과 응답 Packet을 송신자에게 반환한다.
            _radarUdpService =
                new RadarUdpService(
                    _radarUdpClientService,
                    _radarCommandHandler);

            #endregion

            #region [Controller Initialize]

            _rabbitMqController =
                new RabbitMqController(
                    _cseCommandReceiveService,
                    _cseCommandHandler,
                    _mqReceiver);

            _radarUdpController =
                new RadarUdpController(
                    _radarUdpService);

            _deviceConnectionController =
                new DeviceConnectionController(
                    _ads1000ConnectionService);

            _eoCameraController =
                new EoCameraController(
                    _eoCameraService);

            _ads1000ReceiveController =
                new Ads1000ReceiveController(
                    _ads1000StatusService);

            _ads1000StatusApplyController =
                new Ads1000StatusApplyController(
                    _cameraStateProvider);

            _ptzAbsoluteController =
                new PtzAbsoluteController(
                    _ads1000CameraControlService);

            _ptzRelativeController =
                new PtzRelativeController(
                    _ads1000CameraControlService);

            _ptzContinuousController =
                new PtzContinuousController(
                    _ads1000CameraControlService);

            _ptzHomeZeroController =
                new PtzHomeZeroController(
                    _ads1000CameraControlService);

            _keyboardPtzController =
                new KeyboardPtzController();

            _zoomFocusPositionController =
                new ZoomFocusPositionController(
                    _ads1000CameraControlService);

            _ptzModeController =
                new PtzModeController();

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
                    StartZoomInMove);

            ZoomOutCommand =
                new RelayCommand(
                    StartZoomOutMove);

            FocusNearCommand =
                new RelayCommand(
                    StartFocusNearMove);

            FocusFarCommand =
                new RelayCommand(
                    StartFocusFarMove);

            AutoFocusCommand =
                new RelayCommand(
                    AutoFocus);

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

            StartDummyTrackingTestCommand =
                new AsyncRelayCommand(
                    StartDummyTrackingTestAsync);

            StopDummyTrackingTestCommand =
                new RelayCommand(
                    StopDummyTrackingTest);

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
        /// </summary>
        private async Task ConnectDevicesAsync()
        {
            if (_isDeviceConnecting)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Connect Ignored : Connecting");

                return;
            }

            if (_mcbConnectionState == ConnectionState.Connected ||
                _scbConnectionState == ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Connect Ignored : Already Connected");

                return;
            }

            MainStatusText =
                "MCB / SCB CONNECTING...";

            OperationModeText =
                "DEVICE CONNECTING...";

            _isDeviceConnecting =
                true;

            SetDeviceConnectionState(
                ConnectionState.Connecting,
                ConnectionState.Connecting);

            OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));
            OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));
            OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));
            OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));
            OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));

            try
            {
                DeviceConnectionControllerResult result =
                    await _deviceConnectionController
                        .ConnectAsync(
                            McbIpAddress,
                            McbPort,
                            ScbIpAddress,
                            ScbPort);

                if (result.IsSuccess &&
                    result.ConnectionResult != null)
                {
                    ApplyDeviceConnectionResult(
                        result.ConnectionResult);

                    MainStatusText =
                        result.Message;

                    OperationModeText =
                        "DEVICE CONNECTED";

                    _isEoVideoDisplayEnabled =
                        true;

                    _eoCameraController
                        .Connect(
                            DEFAULT_EO_RTSP_ADDRESS);

                    // [EO RTSP] 연결 성공 대기 후 [Home Position] 이동
                    //
                    // 장비 연결 직후 EO Camera가 Ready 상태가 아닐 수 있으므로,
                    // RTSP 연결 성공 상태를 별도 비동기 흐름에서 대기한 뒤
                    // Home Position 이동을 수행한다.
                    _ =
                        WaitEoRtspConnectedAndMoveHomePositionAsync();
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
                _isDeviceConnecting =
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
        /// 장비 전원 직후 EO Camera가 Ready 상태가 아닐 수 있으므로,
        /// EO RTSP 연결 성공 여부를 일정 시간 대기한 뒤
        /// 연결 성공 시 Home Position 명령을 송신한다.
        /// 
        /// RTSP 연결 실패 상태에서는 Home Position 명령을 송신하지 않는다.
        /// </summary>
        private async Task WaitEoRtspConnectedAndMoveHomePositionAsync()
        {
            const int CHECK_DELAY_MS =
                200;

            const int MAX_WAIT_MS =
                65000;

            int elapsedMs =
                0;

            Console.WriteLine(
                "[EO CAMERA] RTSP Connected Wait Start");

            ConsoleLogHelper.PrintLine();

            while (_isEoVideoDisplayEnabled &&
                   !_isEoRtspConnected &&
                   elapsedMs < MAX_WAIT_MS)
            {
                await Task.Delay(
                    CHECK_DELAY_MS);

                elapsedMs +=
                    CHECK_DELAY_MS;
            }

            if (!_isEoVideoDisplayEnabled)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Connected Wait Canceled : Display Disabled");

                ConsoleLogHelper.PrintLine();

                return;
            }

            if (!_isEoRtspConnected)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Connected Wait Failed : Timeout");

                Console.WriteLine(
                    "[DEVICE] Home Position After Connect Skipped : EO RTSP Not Connected");

                ConsoleLogHelper.PrintLine();

                return;
            }

            Console.WriteLine(
                "[EO CAMERA] RTSP Connected Wait Complete");

            ConsoleLogHelper.PrintLine();

            await MoveHomePositionAfterDeviceConnectedAsync();
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

            // [장비 통신 설정] 입력 가능 상태 갱신
            //
            // Home Position 이동 중에는
            // 장비 연결 설정값을 변경하지 못하도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));

            // [장비 제어] 활성화 상태 갱신
            //
            // Home Position 이동 중에는
            // 운용 제어 / 이동 제어 영역이 비활성화되도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceControlEnabled));

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
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Disconnect Ignored : Disconnecting");

                return Task.CompletedTask;
            }

            if (_mcbConnectionState == ConnectionState.Disconnected &&
                _scbConnectionState == ConnectionState.Disconnected)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DEVICE] Disconnect Ignored : Already Disconnected");

                return Task.CompletedTask;
            }

            _isDeviceDisconnecting =
                true;

            try
            {
                if (_radarUdpConnectionState == ConnectionState.Connected)
                {
                    ControllerResult radarResult =
                        _radarUdpController
                            .StopReceive();

                    SetRadarUdpConnectionState(
                        ConnectionState.Disconnected);

                    MainStatusText =
                        radarResult.Message;
                }

                if (_rabbitMqConnectionState == ConnectionState.Connected)
                {
                    ControllerResult mqResult =
                        _rabbitMqController
                            .StopReceive();

                    SetRabbitMqConnectionState(
                        ConnectionState.Disconnected);

                    MqStatusText =
                        mqResult.Message;
                }

                StopEoRtspReconnect();

                _eoCameraController
                    .Disconnect();

                ControllerResult result =
                    _deviceConnectionController
                        .Disconnect();

                SetDeviceConnectionState(
                    ConnectionState.Disconnected,
                    ConnectionState.Disconnected);

                MainStatusText =
                    result.Message;

                OperationModeText =
                    "DEVICE DISCONNECTED";
            }
            finally
            {
                _isDeviceDisconnecting =
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
            _cseCommandHandler.HandleCommand(
                message);
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
            EoCameraControllerResult result =
                _eoCameraController
                    .CreateFrameResult(
                        bitmap);

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                EOCameraImage =
                    result.Frame;
            }));

        }

        /// <summary>
        /// [EO] 영상 상태 변경 처리
        /// 
        /// [EoCameraService]에서 전달받은 상태 메시지를
        /// [OperationModeText]에 반영한다.
        /// 
        /// EO Camera가 Error / Connect Failed 상태인 경우,
        /// 장비 전원 직후 Camera Ready 지연 가능성을 고려하여
        /// RTSP 연결 재시도를 시작한다.
        /// </summary>
        /// <param name="statusText">
        /// [EO] 영상 상태 문자열
        /// </param>
        private void OnEoCameraStatusChanged(
            string statusText)
        {
            EoCameraControllerResult result =
                _eoCameraController
                    .CreateStatusResult(
                        statusText);

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MainStatusText =
                    result.Message;

                if (result.IsConnected.HasValue)
                {
                    _isEoRtspConnected =
                        result.IsConnected.Value;
                }

                if (!string.IsNullOrWhiteSpace(result.OperationModeText))
                {
                    OperationModeText =
                        result.OperationModeText;
                }

                if (result.ShouldStartReconnect)
                {
                    StartEoRtspReconnect();
                }

            }));

        }

        /// <summary>
        /// [EO] RTSP 재연결 시작
        /// 
        /// CAMERA ERROR MODE 상태에서 EO Camera가 Ready 상태로 전환될 때까지
        /// 일정 간격으로 RTSP 연결을 재시도한다.
        /// </summary>
        private async void StartEoRtspReconnect()
        {
            const int RECONNECT_DELAY_MS =
                3000;

            const int MAX_RECONNECT_COUNT =
                20;

            if (_isEoRtspReconnectRunning)
            {
                return;
            }

            if (!_isEoVideoDisplayEnabled)
            {
                return;
            }

            _isEoRtspReconnectRunning =
                true;

            _eoRtspReconnectTryCount =
                0;

            try
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Reconnect Start");

                ConsoleLogHelper.PrintLine();

                while (_isEoRtspReconnectRunning &&
                       _isEoVideoDisplayEnabled &&
                       _eoRtspReconnectTryCount < MAX_RECONNECT_COUNT)
                {
                    _eoRtspReconnectTryCount++;

                    await Task.Delay(
                        RECONNECT_DELAY_MS);

                    if (!_isEoRtspReconnectRunning ||
                        !_isEoVideoDisplayEnabled)
                    {
                        return;
                    }

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine(
                        "[EO CAMERA] RTSP Reconnect Try : "
                        + _eoRtspReconnectTryCount
                        + " / "
                        + MAX_RECONNECT_COUNT);

                    ConsoleLogHelper.PrintLine();

                    _eoCameraService.Connect(
                        DEFAULT_EO_RTSP_ADDRESS);
                }

                if (_isEoRtspReconnectRunning)
                {
                    OperationModeText =
                        "CAMERA ERROR MODE";

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine(
                        "[EO CAMERA] RTSP Reconnect Failed : Max Retry Count");

                    ConsoleLogHelper.PrintLine();
                }

            }
            finally
            {
                _isEoRtspReconnectRunning =
                    false;
            }

        }

        /// <summary>
        /// [EO] RTSP 재연결 중지
        /// 
        /// EO Camera가 정상 연결되었거나,
        /// 장비 연결 해제 / 프로그램 종료 시
        /// RTSP 재연결 Loop를 중지한다.
        /// </summary>
        private void StopEoRtspReconnect()
        {
            if (!_isEoRtspReconnectRunning)
            {
                return;
            }

            _isEoRtspReconnectRunning =
                false;

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[EO CAMERA] RTSP Reconnect Stop");

            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Dummy Tracking Test Methods]

        /// <summary>
        /// [Dummy Tracking] 더미 탐지 좌표 입력 주기 [Hz]
        /// 
        /// ICD 기준 탐지 좌표가 초당 30회 들어오는 상황을 모사한다.
        /// </summary>
        private const int DUMMY_DETECTION_HZ =
            30;

        /// <summary>
        /// [Dummy Tracking] 추적 처리 주기 [Hz]
        /// 
        /// 최신 탐지 좌표를 기준으로 TrackingControlService를 호출한다.
        /// </summary>
        private const int DUMMY_TRACKING_HZ =
            30;

        /// <summary>
        /// [Dummy Tracking] 테스트 시작
        /// 
        /// 실제 드론 / AI 탐지 결과가 없는 상태에서
        /// 30Hz 더미 Bounding Box 입력을 생성하고,
        /// 최신 탐지값 기준으로 AUTO Tracking 흐름을 검증한다.
        /// </summary>
        /// <returns>
        /// 비동기 작업
        /// </returns>
        private Task StartDummyTrackingTestAsync()
        {
            if (_isDummyTrackingRunning)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Start Ignored : Already Running");

                return Task.CompletedTask;
            }

            if (_mcbConnectionState != ConnectionState.Connected ||
                _scbConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Start Skipped : Device Not Fully Connected");

                return Task.CompletedTask;
            }

            _isDummyTrackingRunning =
                true;

            _lastProcessedDummyTrackingFrameId =
                -1;

            _dummyTrackingCancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken cancellationToken =
                _dummyTrackingCancellationTokenSource.Token;

            ConsoleLogHelper.PrintBlock(
                "[DUMMY TRACKING] Start");

            Console.WriteLine(
                "[DUMMY TRACKING] Detection Input Hz : "
                + DUMMY_DETECTION_HZ);

            Console.WriteLine(
                "[DUMMY TRACKING] Tracking Process Hz : "
                + DUMMY_TRACKING_HZ);

            _ =
                Task.Run(
                    async () =>
                    {
                        await RunDummyDetectionInputLoopAsync(
                            cancellationToken);
                    },
                    cancellationToken);

            _ =
                Task.Run(
                    async () =>
                    {
                        await RunDummyLatestTrackingLoopAsync(
                            cancellationToken);
                    },
                    cancellationToken);

            return Task.CompletedTask;
        }

        /// <summary>
        /// [Dummy Tracking] 더미 탐지 좌표 입력 Loop
        /// 
        /// ICD 기준 30Hz 탐지 좌표 수신 상황을 모사한다.
        /// 생성된 Bounding Box는 즉시 처리하지 않고,
        /// 최신 탐지값으로만 저장한다.
        /// </summary>
        /// <param name="cancellationToken">
        /// 취소 토큰
        /// </param>
        /// <returns>
        /// 비동기 작업
        /// </returns>
        private async Task RunDummyDetectionInputLoopAsync(
            CancellationToken cancellationToken)
        {
            int frameId =
                0;

            int delayMilliseconds =
                1000 / DUMMY_DETECTION_HZ;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    DetectionBoundingBox boundingBox =
                        CreateSmoothDummyTrackingBoundingBox(
                            frameId);

                    lock (_dummyTrackingTargetLock)
                    {
                        _latestDummyTrackingBoundingBox =
                            boundingBox;

                        _latestDummyTrackingFrameId =
                            frameId;

                        _latestDummyTrackingReceivedTime =
                            DateTime.Now;
                    }

                    if (frameId % DUMMY_DETECTION_HZ == 0)
                    {
                        Console.WriteLine(
                            "[DUMMY TRACKING][INPUT] 30Hz Latest Frame : "
                            + frameId
                            + ", CenterX="
                            + boundingBox.CenterX
                            + ", CenterY="
                            + boundingBox.CenterY);
                    }

                    frameId++;

                    await Task.Delay(
                            delayMilliseconds,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <summary>
        /// [Dummy Tracking] 최신 탐지값 기준 추적 Loop
        /// 
        /// 30Hz로 갱신되는 탐지 좌표 중
        /// 가장 마지막 Bounding Box 값을 기준으로 AUTO Tracking을 수행한다.
        /// </summary>
        /// <param name="cancellationToken">
        /// 취소 토큰
        /// </param>
        /// <returns>
        /// 비동기 작업
        /// </returns>
        private async Task RunDummyLatestTrackingLoopAsync(
            CancellationToken cancellationToken)
        {
            int delayMilliseconds =
                1000 / DUMMY_TRACKING_HZ;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    DetectionBoundingBox latestBoundingBox =
                        null;

                    int latestFrameId =
                        -1;

                    DateTime latestReceivedTime =
                        DateTime.MinValue;

                    lock (_dummyTrackingTargetLock)
                    {
                        latestBoundingBox =
                            _latestDummyTrackingBoundingBox;

                        latestFrameId =
                            _latestDummyTrackingFrameId;

                        latestReceivedTime =
                            _latestDummyTrackingReceivedTime;
                    }

                    if (latestBoundingBox == null)
                    {
                        await Task.Delay(
                                delayMilliseconds,
                                cancellationToken)
                            .ConfigureAwait(false);

                        continue;
                    }

                    if (latestFrameId == _lastProcessedDummyTrackingFrameId)
                    {
                        await Task.Delay(
                                delayMilliseconds,
                                cancellationToken)
                            .ConfigureAwait(false);

                        continue;
                    }

                    _lastProcessedDummyTrackingFrameId =
                        latestFrameId;

                    double elapsedMilliseconds =
                        (DateTime.Now - latestReceivedTime)
                            .TotalMilliseconds;

                    if (latestFrameId % DUMMY_TRACKING_HZ == 0)
                    {
                        Console.WriteLine(
                            "[DUMMY TRACKING][PROCESS] Latest Frame : "
                            + latestFrameId
                            + ", ElapsedMs="
                            + elapsedMilliseconds.ToString("F1")
                            + ", CenterX="
                            + latestBoundingBox.CenterX
                            + ", CenterY="
                            + latestBoundingBox.CenterY);
                    }

                    double currentZoom =
                        CurrentZoom;

                    _trackingControlService
                        .ProcessTracking(
                            latestBoundingBox,
                            currentZoom);

                    await Task.Delay(
                            delayMilliseconds,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Canceled");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Failed");

                Console.WriteLine(
                    ex);
            }
            finally
            {
                _isDummyTrackingRunning =
                    false;

                _dummyTrackingCancellationTokenSource =
                    null;

                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Stop");
            }
        }

        /// <summary>
        /// [Dummy Tracking] 테스트 중지
        /// 
        /// 실행 중인 더미 Bounding Box 주입 Loop를 중지한다.
        /// </summary>
        private void StopDummyTrackingTest()
        {
            if (!_isDummyTrackingRunning)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Stop Ignored : Not Running");

                return;
            }

            _dummyTrackingCancellationTokenSource
                ?.Cancel();
        }

        /// <summary>
        /// [Dummy Tracking] 부드러운 더미 Bounding Box 생성
        /// 
        /// 30Hz 탐지 좌표 입력 상황에서
        /// 탐지 객체 중심점이 화면 외곽에서 중앙으로
        /// 점진적으로 수렴하는 형태를 생성한다.
        /// </summary>
        /// <param name="frameId">
        /// 더미 탐지 Frame 번호
        /// </param>
        /// <returns>
        /// 더미 탐지 객체 영역 정보
        /// </returns>
        private DetectionBoundingBox CreateSmoothDummyTrackingBoundingBox(
            int frameId)
        {
            const double FRAME_WIDTH =
                1920.0;

            const double FRAME_HEIGHT =
                1080.0;

            const double BOX_WIDTH =
                120.0;

            const double BOX_HEIGHT =
                80.0;

            const int FRAMES_PER_SCENARIO =
                DUMMY_DETECTION_HZ * 3;

            const double MAX_OFFSET_X =
                250.0;

            const double MAX_OFFSET_Y =
                150.0;

            double frameCenterX =
                FRAME_WIDTH / 2.0;

            double frameCenterY =
                FRAME_HEIGHT / 2.0;

            int scenarioIndex =
                frameId / FRAMES_PER_SCENARIO;

            int scenarioFrame =
                frameId % FRAMES_PER_SCENARIO;

            double approachRatio =
                1.0 - ((double)scenarioFrame / (FRAMES_PER_SCENARIO - 1));

            double offsetX =
                0.0;

            double offsetY =
                0.0;

            switch (scenarioIndex % 5)
            {
                case 0:
                    // [오른쪽 → 중앙]
                    offsetX =
                        MAX_OFFSET_X * approachRatio;
                    break;

                case 1:
                    // [왼쪽 → 중앙]
                    offsetX =
                        -MAX_OFFSET_X * approachRatio;
                    break;

                case 2:
                    // [위쪽 → 중앙]
                    offsetY =
                        -MAX_OFFSET_Y * approachRatio;
                    break;

                case 3:
                    // [아래쪽 → 중앙]
                    offsetY =
                        MAX_OFFSET_Y * approachRatio;
                    break;

                default:
                    // [우상단 → 중앙]
                    offsetX =
                        MAX_OFFSET_X * approachRatio;

                    offsetY =
                        -MAX_OFFSET_Y * approachRatio;
                    break;
            }

            double centerX =
                frameCenterX + offsetX;

            double centerY =
                frameCenterY + offsetY;

            return new DetectionBoundingBox
            {
                FrameId =
                    frameId,

                X1 =
                    centerX - BOX_WIDTH / 2.0,

                Y1 =
                    centerY - BOX_HEIGHT / 2.0,

                X2 =
                    centerX + BOX_WIDTH / 2.0,

                Y2 =
                    centerY + BOX_HEIGHT / 2.0,

                ClassId =
                    1,

                Confidence =
                    1.0
            };
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// [Pan] 누적 상태값 갱신
        /// 
        /// 장비 상태 Packet에서 수신한 [Pan] 원본 각도값을 기준으로
        /// 내부 누적 상태값을 갱신한다.
        /// 
        /// 화면 표시용 [Pan] 값은 [0 ~ 360] 범위로 정규화하지만,
        /// 장비 상태 Packet의 [Pan] 원본값은 한 바퀴 이상 회전한
        /// 누적 각도 정보를 포함할 수 있으므로 정규화하지 않고 보관한다.
        /// 
        /// 단, 목표 [Pan] 위치 이동 계산 시에는
        /// 해당 누적값을 직접 [0]으로 회귀시키지 않고,
        /// 현재 누적값에 최단 이동각을 더한 Target을 사용한다.
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
                CameraCommandService.NormalizePanStatus(
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
                CameraCommandService.Clamp(
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
                CameraCommandService.Clamp(
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
        /// [Pan] UI Zero 기준 현재 위치 계산
        /// 
        /// 장비에서 수신한 실제 Pan 위치값에서
        /// 사용자가 설정한 UI Zero Offset 값을 빼서,
        /// 화면 기준 Pan 현재 위치를 계산한다.
        /// </summary>
        /// <returns>
        /// UI Zero 기준 Pan 현재 위치
        /// </returns>
        private double GetUiCurrentPan()
        {
            return RoundAngleToProtocolScale(
                CameraCommandService.NormalizePanStatus(
                    CurrentPan
                    - _panUiZeroOffset));
        }

        /// <summary>
        /// [Tilt] UI Zero 기준 현재 위치 계산
        /// 
        /// 장비에서 수신한 실제 Tilt 위치값에서
        /// 사용자가 설정한 UI Zero Offset 값을 빼서,
        /// 화면 기준 Tilt 현재 위치를 계산한다.
        /// </summary>
        /// <returns>
        /// UI Zero 기준 Tilt 현재 위치
        /// </returns>
        private double GetUiCurrentTilt()
        {
            return RoundAngleToProtocolScale(
                CurrentTilt
                - _tiltUiZeroOffset);
        }

        /// <summary>
        /// [Pan] UI Target 값을 장비 실제 Target 값으로 변환
        /// 
        /// 사용자가 입력한 UI 기준 Pan Target 값에
        /// Pan UI Zero Offset 값을 더해
        /// 장비에 송신할 실제 Pan Target 값을 계산한다.
        /// </summary>
        /// <param name="uiTargetPan">
        /// UI 기준 Pan Target
        /// </param>
        /// <returns>
        /// 장비 실제 Pan Target
        /// </returns>
        private double ConvertUiPanTargetToDeviceTarget(
            double uiTargetPan)
        {
            return RoundAngleToProtocolScale(
                CameraCommandService.NormalizePanStatus(
                    uiTargetPan
                    + _panUiZeroOffset));
        }

        /// <summary>
        /// [Tilt] UI Target 값을 장비 실제 Target 값으로 변환
        /// 
        /// 사용자가 입력한 UI 기준 Tilt Target 값에
        /// Tilt UI Zero Offset 값을 더해
        /// 장비에 송신할 실제 Tilt Target 값을 계산한다.
        /// </summary>
        /// <param name="uiTargetTilt">
        /// UI 기준 Tilt Target
        /// </param>
        /// <returns>
        /// 장비 실제 Tilt Target
        /// </returns>
        private double ConvertUiTiltTargetToDeviceTarget(
            double uiTargetTilt)
        {
            return RoundAngleToProtocolScale(
                CameraCommandService.Clamp(
                    uiTargetTilt
                    + _tiltUiZeroOffset,
                    -90,
                    90));
        }

        /// <summary>
        /// [Pan / Tilt] 각도값 소수점 둘째 자리 보정
        /// 
        /// ADS3000 Offset 저장 프로토콜은
        /// 각도값을 [각도 * 100] 정수값으로 송신하므로,
        /// UI 입력 및 표시 기준도 소수점 둘째 자리로 통일한다.
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


        #region [Keyboard Control Methods]

        /// <summary>
        /// [Keyboard] 방향키 입력 처리
        /// 
        /// 운용 제어 화면에서 방향키 입력을
        /// Pan / Tilt 연속 이동 명령으로 변환한다.
        /// 
        /// 두 방향키가 동시에 눌린 경우
        /// Pan / Tilt 축을 각각 제어하여 대각선 이동으로 처리한다.
        /// </summary>
        /// <param name="key">
        /// 입력된 키
        /// </param>
        public void HandlePanTiltKeyDown(
            Key key)
        {
            _keyboardPtzController
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
                    return;
            }
            UpdateKeyboardPanTiltMove();
        }

        /// <summary>
        /// [Keyboard] 방향키 해제 처리
        /// 
        /// 해제된 방향키에 해당하는 축만 정지하고,
        /// 다른 방향키가 계속 눌려 있는 경우 해당 축 이동은 유지한다.
        /// </summary>
        /// <param name="key">
        /// 해제된 키
        /// </param>
        public void HandlePanTiltKeyUp(
            Key key)
        {
            _keyboardPtzController
                .HandleKeyUp();

            switch (key)
            {
                case Key.Left:
                    _isKeyboardPanLeftPressed =
                        false;

                    StopPanMove();

                    UpdateKeyboardTiltMove();

                    break;

                case Key.Right:
                    _isKeyboardPanRightPressed =
                        false;

                    StopPanMove();

                    UpdateKeyboardTiltMove();

                    break;

                case Key.Up:
                    _isKeyboardTiltUpPressed =
                        false;

                    StopTiltMove();

                    UpdateKeyboardPanMove();

                    break;

                case Key.Down:
                    _isKeyboardTiltDownPressed =
                        false;

                    StopTiltMove();

                    UpdateKeyboardPanMove();

                    break;

                default:
                    return;
            }

        }

        /// <summary>
        /// [Keyboard] Pan / Tilt 이동 상태 갱신
        /// </summary>
        private void UpdateKeyboardPanTiltMove()
        {
            if (_mcbConnectionState != ConnectionState.Connected ||
                _isHomePositionMoving)
            {
                return;
            }

            UpdateKeyboardPanMove();
            UpdateKeyboardTiltMove();
        }

        /// <summary>
        /// [Keyboard] Pan 이동 상태 갱신
        /// </summary>
        private void UpdateKeyboardPanMove()
        {
            if (_mcbConnectionState != ConnectionState.Connected ||
                _isHomePositionMoving)
            {
                return;
            }

            if (_isKeyboardPanLeftPressed &&
                !_isKeyboardPanRightPressed)
            {
                StartPanLeftMove();
            }
            else if (_isKeyboardPanRightPressed &&
                     !_isKeyboardPanLeftPressed)
            {
                StartPanRightMove();
            }

        }

        /// <summary>
        /// [Keyboard] Tilt 이동 상태 갱신
        /// </summary>
        private void UpdateKeyboardTiltMove()
        {
            if (_mcbConnectionState != ConnectionState.Connected ||
                _isHomePositionMoving)
            {
                return;
            }

            if (_isKeyboardTiltUpPressed &&
                !_isKeyboardTiltDownPressed)
            {
                StartTiltUpMove();
            }
            else if (_isKeyboardTiltDownPressed &&
                     !_isKeyboardTiltUpPressed)
            {
                StartTiltDownMove();
            }

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
            // 단일 상태값으로 관리한다.
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
            if (_rabbitMqConnectionState == ConnectionState.Connected ||
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

                Console.WriteLine(
                    "[CSE][MQ] Stop Ignored : Not Started");

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

                SetRabbitMqConnectionState(
                    ConnectionState.Disconnected);
            }
            catch (Exception ex)
            {
                SetRabbitMqConnectionState(
                    ConnectionState.Disconnected);

                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[CSE][MQ] Stop Failed");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine();
            }

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
                        _scbConnectionState == ConnectionState.Connected) &&
                       !_isDeviceConnecting &&
                       !_isDeviceDisconnecting &&
                       !_isHomePositionMoving;
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
                       !_isDeviceConnecting &&
                       !_isDeviceDisconnecting &&
                       !_isHomePositionMoving;
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
        /// 장비 연결 처리 중이거나, Home Position 이동 중인 경우
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
                    OnPropertyChanged(nameof(CurrentPanDisplayText));
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
                    OnPropertyChanged(nameof(CurrentTiltDisplayText));
                }

            }

        }

        /// <summary>
        /// [Pan] 현재 위치 표시 문자열
        /// 
        /// 장비 실제 Pan 위치값에서
        /// UI Zero Offset 값을 보정한 후,
        /// 소수점 둘째 자리까지 표시한다.
        /// 
        /// 사용자가 [Pan Zero]를 설정한 경우,
        /// 해당 위치가 화면 기준 [0.00]으로 표시된다.
        /// </summary>
        public string CurrentPanDisplayText
        {
            get
            {
                return GetUiCurrentPan()
                    .ToString("F2");
            }

        }

        /// <summary>
        /// [Tilt] 현재 위치 표시 문자열
        /// 
        /// 장비 실제 Tilt 위치값에서
        /// UI Zero Offset 값을 보정한 후,
        /// 소수점 둘째 자리까지 표시한다.
        /// 
        /// 사용자가 [Tilt Zero]를 설정한 경우,
        /// 해당 위치가 화면 기준 [0.00]으로 표시된다.
        /// </summary>
        public string CurrentTiltDisplayText
        {
            get
            {
                return GetUiCurrentTilt()
                    .ToString("F2");
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
                double clampedValue =
                    CameraCommandService.Clamp(
                        value,
                        5,
                        50);

                if (_ads1000CameraControlService.PanTiltSpeedLevel != clampedValue)
                {
                    Console.WriteLine(
                        "[UI][PTZ] Pan / Tilt Speed Value Changed : "
                        + _ads1000CameraControlService.PanTiltSpeedLevel.ToString("F0")
                        + " -> "
                        + clampedValue.ToString("F0"));

                    Console.WriteLine();

                    _ads1000CameraControlService.PanTiltSpeedLevel =
                        clampedValue;

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
        /// 
        /// ADS3000 Offset 저장 기준과 동일하게
        /// 소수점 둘째 자리까지의 각도값만 사용한다.
        /// </summary>
        public double? PanAbsoluteValue
        {
            get => _panAbsoluteValue;
            set
            {
                double? roundedValue =
                    value.HasValue
                        ? RoundAngleToProtocolScale(
                            value.Value)
                        : value;

                if (_panAbsoluteValue != roundedValue)
                {
                    _panAbsoluteValue =
                        roundedValue;

                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Tilt] Absolute 이동 입력값
        /// 
        /// ADS3000 Offset 저장 기준과 동일하게
        /// 소수점 둘째 자리까지의 각도값만 사용한다.
        /// </summary>
        public double? TiltAbsoluteValue
        {
            get => _tiltAbsoluteValue;
            set
            {
                double? roundedValue =
                    value.HasValue
                        ? RoundAngleToProtocolScale(
                            value.Value)
                        : value;

                if (_tiltAbsoluteValue != roundedValue)
                {
                    _tiltAbsoluteValue =
                        roundedValue;

                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Pan] Relative 이동 입력값
        /// 
        /// ADS3000 Offset 저장 기준과 동일하게
        /// 소수점 둘째 자리까지의 각도값만 사용한다.
        /// </summary>
        public double? PanRelativeValue
        {
            get => _panRelativeValue;
            set
            {
                double? roundedValue =
                    value.HasValue
                        ? RoundAngleToProtocolScale(
                            value.Value)
                        : value;

                if (_panRelativeValue != roundedValue)
                {
                    _panRelativeValue =
                        roundedValue;

                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Tilt] Relative 이동 입력값
        /// 
        /// ADS3000 Offset 저장 기준과 동일하게
        /// 소수점 둘째 자리까지의 각도값만 사용한다.
        /// </summary>
        public double? TiltRelativeValue
        {
            get => _tiltRelativeValue;
            set
            {
                double? roundedValue =
                    value.HasValue
                        ? RoundAngleToProtocolScale(
                            value.Value)
                        : value;

                if (_tiltRelativeValue != roundedValue)
                {
                    _tiltRelativeValue =
                        roundedValue;

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


        #region [Camera Absolute Control Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동
        /// 
        /// 입력된 [Pan Absolute] 목표값을
        /// UI Zero 기준 [0 ~ 360] 범위로 보정한 후,
        /// 장비 실제 Target 값으로 변환하여 이동 명령을 송신한다.
        /// 
        /// 사용자가 [Pan Zero]를 설정한 경우,
        /// UI Target [0.00]은 Zero 설정 당시의 실제 Pan 위치로 변환된다.
        /// 
        /// 단, [360] 입력은 [0]과 표시 위치는 같지만,
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
                GetUiCurrentPan();

            double inputPan =
                RoundAngleToProtocolScale(
                    PanAbsoluteValue.Value);

            double targetPan =
                CameraCommandService.Clamp(
                    inputPan,
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
                    CameraCommandService.CalculatePanMoveAngle(
                        currentPan,
                        targetPan,
                        _panTurnMode);
            }

            if (!isFullTurnTarget &&
                Math.Abs(panMoveAngle) <= PAN_POSITION_EPSILON)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Ignored : Already Target Position");

                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Current : "
                    + currentPan.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Target : "
                    + targetPan.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Pan UI Zero Offset : "
                    + _panUiZeroOffset.ToString("F2"));

                return;
            }

            double panCommandTarget =
                currentPanCommandAngle + panMoveAngle;

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Input : "
                + inputPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Mode : "
                + _panTurnMode);

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Current : "
                + currentPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Accumulated Current : "
                + currentPanCommandAngle.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan UI Zero Offset : "
                + _panUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Target : "
                + targetPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Move Angle : "
                + panMoveAngle.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Command Target Raw : "
                + panCommandTarget.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _isUiContinuousMoveStarted =
                false;

            _ads1000CameraControlService
                .MovePanAbsolute(
                    panCommandTarget);

            MainStatusText =
                "PAN ABSOLUTE MOVE";
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동
        /// 
        /// 입력된 [Tilt Absolute] 값을
        /// UI Zero 기준 [-90 ~ 90] 범위로 보정한 후,
        /// 장비 실제 Target 값으로 변환하여 이동 명령을 송신한다.
        /// 
        /// 사용자가 [Tilt Zero]를 설정한 경우,
        /// UI Target [0.00]은 Zero 설정 당시의 실제 Tilt 위치로 변환된다.
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
                GetUiCurrentTilt();

            double inputTilt =
                RoundAngleToProtocolScale(
                    TiltAbsoluteValue.Value);

            double targetTilt =
                CameraCommandService.Clamp(
                    inputTilt,
                    -90,
                    90);

            double deviceTargetTilt =
                ConvertUiTiltTargetToDeviceTarget(
                    targetTilt);

            double tiltMoveAngle =
                targetTilt - currentTilt;

            if (Math.Abs(tiltMoveAngle) <= 0.001)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Ignored : Already Target Position");

                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Current : "
                    + currentTilt.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Target : "
                    + targetTilt.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Tilt UI Zero Offset : "
                    + _tiltUiZeroOffset.ToString("F2"));

                return;
            }

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Input : "
                + inputTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Current : "
                + currentTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt UI Zero Offset : "
                + _tiltUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Target : "
                + targetTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Move Angle : "
                + tiltMoveAngle.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Command Target : "
                + deviceTargetTilt.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _isUiContinuousMoveStarted =
                false;

            _ads1000CameraControlService
                .MoveTiltAbsolute(
                    deviceTargetTilt);

            MainStatusText =
                "TILT ABSOLUTE MOVE";
        }
        #endregion


        #region [Camera Continuous Control Methods]

        /// <summary>
        /// [Pan / Tilt] 연속 이동 속도 재적용
        /// 
        /// ADS1000 장비는 이동 중에도 [JV] 속도 명령을 다시 수신하면
        /// 현재 이동 속도를 갱신할 수 있으므로,
        /// 별도 정지 명령 없이 현재 이동 중인 Pan / Tilt 방향 명령을 재송신한다.
        /// 
        /// 대각선 이동 중에는 Pan / Tilt 두 축에 모두 변경된 속도를 반영한다.
        /// </summary>
        private void ApplyCurrentPanTiltContinuousMoveSpeed()
        {
            if (!_isUiContinuousMoveStarted)
            {
                return;
            }

            if (!_isPanContinuousMoving &&
                !_isTiltContinuousMoving)
            {
                return;
            }

            Console.WriteLine(
                "[UI][PTZ] Pan / Tilt Continuous Speed Changed : "
                + PanTiltSpeedLevel.ToString("F0"));

            switch (_currentPanContinuousMoveDirection)
            {
                case PanTiltContinuousMoveDirection.PanLeft:
                    _ads1000CameraControlService
                        .PanLeft();
                    break;

                case PanTiltContinuousMoveDirection.PanRight:
                    _ads1000CameraControlService
                        .PanRight();
                    break;

                default:
                    break;
            }

            switch (_currentTiltContinuousMoveDirection)
            {
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

            if (_currentPanTiltMoveType == PanTiltMoveType.Continuous)
            {
                ApplyCurrentPanTiltContinuousMoveSpeed();

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
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Pan] 왼쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartPanLeftMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
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

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Pan] 오른쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Pan] 오른쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartPanRightMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
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

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Tilt] 위쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Tilt] 위쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartTiltUpMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
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

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Tilt] 아래쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Tilt] 아래쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartTiltDownMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
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

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Pan Left] / [Tilt Up] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltUpMove()
        {
            StartPanLeftMove();

            StartTiltUpMove();
        }

        /// <summary>
        /// [Pan Right] / [Tilt Up] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltUpMove()
        {
            StartPanRightMove();

            StartTiltUpMove();
        }

        /// <summary>
        /// [Pan Left] / [Tilt Down] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltDownMove()
        {
            StartPanLeftMove();

            StartTiltDownMove();
        }

        /// <summary>
        /// [Pan Right] / [Tilt Down] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltDownMove()
        {
            StartPanRightMove();

            StartTiltDownMove();
        }

        /// <summary>
        /// [Zoom] 확대 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Zoom] 확대 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartZoomInMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
                    .StartZoomInMove();

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Zoom] 축소 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Zoom] 축소 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartZoomOutMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
                    .StartZoomOutMove();

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Focus] Near 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Focus] Near 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartFocusNearMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
                    .StartFocusNearMove();

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Focus] Far 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Focus] Far 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartFocusFarMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
                    .StartFocusFarMove();

            _isUiContinuousMoveStarted =
                result.IsMoving == true;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Auto Focus] 실행
        /// </summary>
        private void AutoFocus()
        {
            PtzControllerResult result =
                _ptzContinuousController
                    .AutoFocus();

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Pan] 연속 이동 정지
        /// 
        /// 키보드 방향키 조합 제어 중
        /// Pan 축 입력이 해제된 경우 Pan 축만 정지한다.
        /// </summary>
        /// <returns>
        /// Pan 이동 정지 처리 결과
        /// </returns>
        private PtzControllerResult StopPanMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
                    .StopPanMove();

            _isPanContinuousMoving =
                false;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

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

            MainStatusText =
                result.Message;

            return result;
        }

        /// <summary>
        /// [Tilt] 연속 이동 정지
        /// 
        /// 키보드 방향키 조합 제어 중
        /// Tilt 축 입력이 해제된 경우 Tilt 축만 정지한다.
        /// </summary>
        /// <returns>
        /// Tilt 이동 정지 처리 결과
        /// </returns>
        private PtzControllerResult StopTiltMove()
        {
            PtzControllerResult result =
                _ptzContinuousController
                    .StopTiltMove();

            _isTiltContinuousMoving =
                false;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

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

            MainStatusText =
                result.Message;

            return result;
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
            PtzControllerResult result =
                _ptzContinuousController
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

            MainStatusText =
                result.Message;
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
        /// Pan / Tilt 상태값이 일정 시간 안정화되었는지 확인하여
        /// Home Position 이동 완료 여부를 판단한다.
        /// 
        /// Home Position 완료 후에는
        /// 장비가 실제로 도착한 현재 Pan / Tilt 위치를
        /// UI 기준 [0] 위치로 다시 저장한다.
        /// </summary>
        /// <param name="logPrefix">
        /// 로그 출력 구분 문자열
        /// </param>
        private async Task MoveHomePositionWithControlLockAsync(
            string logPrefix)
        {
            if (_isHomePositionMoving)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix + " Ignored : Home Position Moving");

                return;
            }

            if (_mcbConnectionState != ConnectionState.Connected ||
                _scbConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix + " Skipped : Device Not Fully Connected");

                return;
            }

            PtzControllerResult result =
                null;

            try
            {
                SetHomePositionMovingState(
                    true);

                MainStatusText =
                    "HOME POSITION MOVING...";

                // [Home Position] 이동 명령 송신
                //
                // Controller는 장비 내부 Home Script 실행 명령만 송신한다.
                // 실제 이동 완료 여부는 상태 안정화 대기 로직에서 판단한다.
                result =
                    await _ptzHomeZeroController
                        .MoveHomePositionAsync();

                if (result != null &&
                    !result.IsSuccess)
                {
                    MainStatusText =
                        result.Message;

                    return;
                }

                bool isCompleted =
                    await WaitHomePositionCompletedAsync();

                if (!isCompleted)
                {
                    MainStatusText =
                        "HOME POSITION WAIT TIMEOUT";

                    return;
                }

                // [Home Position] 완료 후 UI 기준 [0] 재설정
                //
                // 장비가 실제 Home 위치에 도착한 시점의
                // Pan / Tilt 값을 UI Zero Offset으로 저장하여,
                // 화면 CURRENT STATUS가 [0.00] 기준으로 표시되도록 한다.
                ApplyHomePositionUiZeroStatus();

                MainStatusText =
                    "HOME POSITION STATUS SYNC...";

                // [UI] 표시 반영 대기
                //
                // CURRENT STATUS가 [0.00] 기준으로 갱신된 뒤
                // 버튼 Lock이 해제되도록 짧게 대기한다.
                await Task.Delay(
                    150);

                MainStatusText =
                    "HOME POSITION COMPLETE";
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix + " Failed : " + ex.Message);

                MainStatusText =
                    "HOME POSITION FAILED";
            }
            finally
            {
                SetHomePositionMovingState(
                    false);
            }

        }

        /// <summary>
        /// [Home Position] 완료 후 UI 기준 위치 초기화
        /// 
        /// Home Position 이동 완료 후
        /// 장비가 실제로 도착한 현재 Pan / Tilt 위치를
        /// UI 기준 [0] 위치로 다시 저장한다.
        /// 
        /// 장비 Encoder 값을 변경하는 것이 아니라,
        /// 화면 표시 및 이후 UI Target 계산 기준만 재설정한다.
        /// </summary>
        private void ApplyHomePositionUiZeroStatus()
        {
            double currentPan =
                RoundAngleToProtocolScale(
                    CameraCommandService.NormalizePanStatus(
                        CurrentPan));

            double currentTilt =
                RoundAngleToProtocolScale(
                    CurrentTilt);

            _panUiZeroOffset =
                currentPan;

            _tiltUiZeroOffset =
                currentTilt;

            PanAbsoluteValue =
                0;

            TiltAbsoluteValue =
                0;

            PanRelativeValue =
                0;

            TiltRelativeValue =
                0;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            ResetPanAccumulatedStatus();

            OnPropertyChanged(nameof(CurrentPan));
            OnPropertyChanged(nameof(CurrentTilt));
            OnPropertyChanged(nameof(CurrentPanDisplayText));
            OnPropertyChanged(nameof(CurrentTiltDisplayText));

            Console.WriteLine(
                "[UI][PTZ] Home UI Zero Pan Offset : "
                + _panUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Home UI Zero Tilt Offset : "
                + _tiltUiZeroOffset.ToString("F2"));
        }

        /// <summary>
        /// [Pan] 현재 위치를 UI / 장비 Script 기준 [0] 위치로 저장
        /// 
        /// 현재 [Pan] 위치값을 장비 Offset 저장 프로토콜로 송신하고,
        /// 프로그램 화면에서도 현재 위치가 [0.00]으로 표시되도록
        /// UI Zero Offset을 저장한다.
        /// </summary>
        private void SetPanZero()
        {
            double currentPan =
                RoundAngleToProtocolScale(
                    CameraCommandService.NormalizePanStatus(
                        CurrentPan));

            int offsetValue =
                Convert.ToInt32(
                    Math.Round(
                        currentPan * 100.0,
                        MidpointRounding.AwayFromZero));

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Offset Request");

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Current : "
                + currentPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Offset Value : "
                + offsetValue);

            PtzControllerResult result =
                _ptzHomeZeroController
                    .SetPanZero(
                        currentPan);

            if (!result.IsSuccess)
            {
                MainStatusText =
                    result.Message;

                Console.WriteLine(
                    "[UI][PTZ] Pan Zero Failed : "
                    + result.Message);

                ConsoleLogHelper.PrintLine();

                return;
            }

            _panUiZeroOffset =
                currentPan;

            PanAbsoluteValue =
                0;

            PanRelativeValue =
                0;

            ResetPanAccumulatedStatus();

            OnPropertyChanged(nameof(CurrentPanDisplayText));
            OnPropertyChanged(nameof(CurrentPan));

            MainStatusText =
                result.Message;

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Tilt] 현재 위치를 UI / 장비 Script 기준 [0] 위치로 저장
        /// 
        /// 현재 [Tilt] 위치값을 장비 Offset 저장 프로토콜로 송신하고,
        /// 프로그램 화면에서도 현재 위치가 [0.00]으로 표시되도록
        /// UI Zero Offset을 저장한다.
        /// </summary>
        private void SetTiltZero()
        {
            double currentTilt =
                RoundAngleToProtocolScale(
                    CurrentTilt);

            int offsetValue =
                Convert.ToInt32(
                    Math.Round(
                        currentTilt * 100.0,
                        MidpointRounding.AwayFromZero));

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Offset Request");

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Current : "
                + currentTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Offset Value : "
                + offsetValue);

            PtzControllerResult result =
                _ptzHomeZeroController
                    .SetTiltZero(
                        currentTilt);

            if (!result.IsSuccess)
            {
                MainStatusText =
                    result.Message;

                Console.WriteLine(
                    "[UI][PTZ] Tilt Zero Failed : "
                    + result.Message);

                ConsoleLogHelper.PrintLine();

                return;
            }

            _tiltUiZeroOffset =
                currentTilt;

            TiltAbsoluteValue =
                0;

            TiltRelativeValue =
                0;

            OnPropertyChanged(nameof(CurrentTiltDisplayText));
            OnPropertyChanged(nameof(CurrentTilt));

            MainStatusText =
                result.Message;

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Home Position] 이동 완료 대기
        /// 
        /// Home Position 명령 송신 후,
        /// Pan / Tilt 상태값이 특정 좌표 [0]에 도달했는지가 아니라
        /// 일정 시간 동안 위치 변화가 거의 없는지 확인하여
        /// 이동 완료 여부를 판단한다.
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
                    CurrentPan);

            double previousTilt =
                CurrentTilt;

            while (elapsedMilliseconds < TIMEOUT_MILLISECONDS)
            {
                if (_mcbConnectionState != ConnectionState.Connected ||
                    _scbConnectionState != ConnectionState.Connected)
                {
                    ConsoleLogHelper.PrintBlock(
                        "[DEVICE] Home Position Wait Canceled : Device Disconnected");

                    return false;
                }

                double currentPan =
                    CameraCommandService.NormalizePanStatus(
                        CurrentPan);

                double currentTilt =
                    CurrentTilt;

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
                    stableCount =
                        0;
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
        /// 
        /// [0] 또는 [360] 근처 값을 Home 기준으로 판단한다.
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
        /// 
        /// [0 ~ 360] 범위로 정규화된 이전 Pan 값과 현재 Pan 값을 기준으로
        /// 한 바퀴 경계값을 고려하여 최단 변화량을 계산한다.
        /// 
        /// 예)
        /// 이전 [359] / 현재 [1]   => [+2]
        /// 이전 [1]   / 현재 [359] => [-2]
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
            ControllerResult result =
                _ptzModeController
                    .SetAutoMode();

            SetPtzControlMode(
                "AUTO");

            MainStatusText =
                result.Message;
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
            ControllerResult result =
                _ptzModeController
                    .SetManualMode();

            SetPtzControlMode(
                "MANUAL");

            MainStatusText =
                result.Message;
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

        #region [Camera Relative Control Methods]

        /// <summary>
        /// [Pan] 상대 위치 이동
        /// 
        /// 입력된 [Pan Relative] 값을 기준으로
        /// UI Zero 기준 현재 Pan 위치에서 상대 이동량을 더한
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

            double currentPan =
                GetUiCurrentPan();

            double movePan =
                RoundAngleToProtocolScale(
                    PanRelativeValue.Value);

            double targetPan =
                CameraCommandService.NormalizePanStatus(
                    currentPan + movePan);

            double panMoveAngle =
                CameraCommandService.CalculatePanMoveAngle(
                    currentPan,
                    targetPan,
                    _panTurnMode);

            double panCommandTarget =
                GetCurrentPanCommandAngle() + panMoveAngle;

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Input : "
                + movePan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Current : "
                + currentPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan UI Zero Offset : "
                + _panUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Move Angle : "
                + panMoveAngle.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Expected Display : "
                + targetPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Command Target Raw : "
                + panCommandTarget.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _isUiContinuousMoveStarted =
                false;

            _ads1000CameraControlService
                .MovePanAbsolute(
                    panCommandTarget);

            MainStatusText =
                "PAN RELATIVE MOVE";
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동
        /// 
        /// 입력된 [Tilt Relative] 값을 기준으로
        /// UI Zero 기준 현재 Tilt 위치에서 상대 이동량을 더한
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
                GetUiCurrentTilt();

            double moveTilt =
                RoundAngleToProtocolScale(
                    TiltRelativeValue.Value);

            double targetTilt =
                CameraCommandService.Clamp(
                    currentTilt + moveTilt,
                    -90,
                    90);

            double deviceTargetTilt =
                ConvertUiTiltTargetToDeviceTarget(
                    targetTilt);

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Input : "
                + moveTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Current : "
                + currentTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt UI Zero Offset : "
                + _tiltUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Move Angle : "
                + moveTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Expected Display : "
                + targetTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Command Target : "
                + deviceTargetTilt.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _isUiContinuousMoveStarted =
                false;

            _ads1000CameraControlService
                .MoveTiltAbsolute(
                    deviceTargetTilt);

            MainStatusText =
                "TILT RELATIVE MOVE";
        }
        #endregion


        #region [UDP Connection Methods]

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
            OnPropertyChanged(nameof(RadarUdpConnectionStatusText));
            OnPropertyChanged(nameof(RadarUdpConnectionStatusBrush));

            // [Radar UDP] 버튼 / 설정 입력 상태 갱신
            OnPropertyChanged(nameof(IsRadarUdpStartButtonEnabled));
            OnPropertyChanged(nameof(IsRadarUdpStopButtonEnabled));
            OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));
        }

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// 
        /// Controller에서 UDP 수신 시작 기능을 수행하고,
        /// 반환된 결과를 기준으로 화면 상태를 갱신한다.
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

            SetRadarUdpConnectionState(
                ConnectionState.Connecting);

            ControllerResult result =
                await _radarUdpController
                    .StartReceiveAsync(
                        RadarUdpLocalPort);

            if (result.IsSuccess)
            {
                SetRadarUdpConnectionState(
                    ConnectionState.Connected);
            }
            else
            {
                SetRadarUdpConnectionState(
                    ConnectionState.Disconnected);
            }

            MainStatusText =
                result.Message;
        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// 
        /// Controller에서 UDP 수신 중지 기능을 수행하고,
        /// 반환된 결과를 기준으로 화면 상태를 갱신한다.
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

            ControllerResult result =
                _radarUdpController
                    .StopReceive();

            if (result.IsSuccess)
            {
                SetRadarUdpConnectionState(
                    ConnectionState.Disconnected);
            }

            MainStatusText =
                result.Message;
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
            Ads1000ReceiveControllerResult result =
                _ads1000ReceiveController
                    .ProcessReceivedPacket(
                        deviceName,
                        packet);

            if (!result.IsSuccess)
            {
                Console.WriteLine(
                    "[" + deviceName + "][RECEIVE] " + result.Message);

                return;
            }

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (Ads1000ParsedPacket parsedPacket in result.ParsedPackets)
                {
                    ApplyParsedStatusValue(
                        parsedPacket);
                }

            }));

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
            Ads1000StatusApplyControllerResult result =
                _ads1000StatusApplyController
                    .Apply(
                        parsedPacket);

            if (!result.IsSuccess)
            {
                return;
            }

            if (result.CurrentPan.HasValue)
            {
                CurrentPan =
                    CameraCommandService.NormalizePanStatus(
                        result.CurrentPan.Value);

                UpdatePanAccumulatedStatus(
                    result.CurrentPan.Value);
            }

            if (result.CurrentTilt.HasValue)
            {
                CurrentTilt =
                    NormalizeTiltStatus(
                        result.CurrentTilt.Value);
            }

            if (result.CurrentZoom.HasValue)
            {
                CurrentZoom =
                    NormalizeRangePosition(
                        result.CurrentZoom.Value,
                        0,
                        1000);

                CurrentZoomRatio =
                    ConvertZoomPositionToRatio(
                        CurrentZoom);
            }

            if (result.CurrentFocus.HasValue)
            {
                CurrentFocus =
                    NormalizeRangePosition(
                        result.CurrentFocus.Value,
                        0,
                        1000);
            }

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
            PtzControllerResult result =
                _zoomFocusPositionController
                    .SetZoomPosition(
                        ZoomPositionValue);

            MainStatusText =
                result.Message;
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
            PtzControllerResult result =
                _zoomFocusPositionController
                    .SetZoomRatio(
                        ZoomRatioValue);

            MainStatusText =
                result.Message;
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

        /// <summary>
        /// [Focus] 지정 위치 이동
        /// 
        /// 입력된 [Focus Position] 값을
        /// [0 ~ 1000] 범위로 보정한 후
        /// [ADS1000] 장비에 위치 이동 명령을 전송한다.
        /// </summary>
        private void SetFocusPosition()
        {
            PtzControllerResult result =
                _zoomFocusPositionController
                    .SetFocusPosition(
                        FocusPositionValue);

            MainStatusText =
                result.Message;
        }
        #endregion
    }

}
