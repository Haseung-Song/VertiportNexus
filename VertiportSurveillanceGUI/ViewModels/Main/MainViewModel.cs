using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VertiportSurveillanceGUI.Common;

namespace VertiportSurveillanceGUI.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel]
    /// 
    /// 메인 클래스 역할:
    /// 1. [MQ] 기반 상위 시스템 명령 수신 / 응답 송신 구조 관리
    /// 2. [UDP] 기반 [EOC] 장비 제어 및 상태 수신 구조 관리
    /// 3. [MMAP] 기반 영상 / 항적 / 탐지 결과 공유 구조 관리
    /// 4. [XAML] 바인딩용 [Image] / [StatusText] / [Command] 갱신
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region [Enum Type]

        /// <summary>
        /// 현재 진행 중인 [연속 제어] 종류
        /// </summary>
        private enum ContinuousMoveType
        {
            None,
            PanTilt,
            Zoom,
            Focus
        }

        #endregion

        #region [Fields]

        #region [MQ Communication Fields]

        /// <summary>
        /// [MQ] 연결 상태 표시 문자열
        /// </summary>
        private string _mqStatusText = "MQ Disconnected";

        /// <summary>
        /// 마지막 [MQ] 수신 메시지 표시 문자열
        /// </summary>
        private string _lastMqMessageText = string.Empty;

        #endregion

        #region [UDP Communication Fields]

        /// <summary>
        /// [UDP] 수신 상태 표시 문자열
        /// </summary>
        private string _udpStatusText = "UDP Stopped";

        /// <summary>
        /// 마지막 [UDP] 수신 패킷 표시 문자열
        /// </summary>
        private string _lastUdpPacketText = string.Empty;

        #endregion

        #region [MMAP Fields]

        /// <summary>
        /// [MMAP] 영상 수신 상태 표시 문자열
        /// </summary>
        private string _mmapStatusText = "MMAP Stopped";

        /// <summary>
        /// [MMAP] 마지막 영상 [Frame Number]
        /// </summary>
        private long _lastFrameNumber;

        #endregion

        #region [Camera State Fields]

        /// <summary>
        /// 카메라 연결 상태 표시 문자열
        /// </summary>
        private string _cameraStatusText = "Camera Disconnected";

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

        /// <summary>
        /// 현재 어떤 [연속 제어]가 동작 중인지
        /// </summary>
        private ContinuousMoveType _currentMoveType = ContinuousMoveType.None;

        #endregion

        #region [Track Fields]

        /// <summary>
        /// 항적 수신 상태 표시 문자열
        /// </summary>
        private string _trackStatusText = "Track Not Received";

        /// <summary>
        /// 현재 선택된 항적 [ID]
        /// </summary>
        private int _selectedTrackId;

        #endregion

        #region [AI / Detection Fields]

        /// <summary>
        /// [AI] 분석 상태 표시 문자열
        /// </summary>
        private string _aiStatusText = "AI Stopped";

        /// <summary>
        /// 현재 탐지 객체 수
        /// </summary>
        private int _detectionCount;

        #endregion

        #region [Image Binding Fields]

        /// <summary>
        /// [EO] 또는 주 영상 출력용 [Image]
        /// </summary>
        private BitmapSource _eoCameraImage;

        /// <summary>
        /// [IR] 또는 보조 영상 출력용 [Image]
        /// </summary>
        private BitmapSource _irCameraImage;

        #endregion

        #region [Status Binding Fields]

        /// <summary>
        /// 프로그램 전체 상태 표시 문자열
        /// </summary>
        private string _mainStatusText = "Ready";

        /// <summary>
        /// 현재 운용 모드 표시 문자열
        /// </summary>
        private string _operationModeText = "Manual";

        #endregion

        #endregion

        #region [ICommand]

        #region [MQ Commands]

        /// <summary>
        /// [MQ] 연결 [Command]
        /// </summary>
        public ICommand ConnectMqCommand { get; }

        /// <summary>
        /// [MQ] 연결 해제 [Command]
        /// </summary>
        public ICommand DisconnectMqCommand { get; }

        #endregion

        #region [UDP Commands]

        /// <summary>
        /// [UDP] 수신 시작 [Command]
        /// </summary>
        public ICommand StartUdpReceiveCommand { get; }

        /// <summary>
        /// [UDP] 수신 중지 [Command]
        /// </summary>
        public ICommand StopUdpReceiveCommand { get; }

        #endregion

        #region [MMAP Commands]

        /// <summary>
        /// [MMAP] 영상 수신 시작 [Command]
        /// </summary>
        public ICommand StartMmapReadCommand { get; }

        /// <summary>
        /// [MMAP] 영상 수신 중지 [Command]
        /// </summary>
        public ICommand StopMmapReadCommand { get; }

        /// <summary>
        /// [Dummy MMAP Writer] 시작 [Command]
        /// </summary>
        public ICommand StartDummyMmapWriterCommand { get; }

        #endregion

        #region [Camera Commands]

        /// <summary>
        /// [PAN] 왼쪽 이동 [Command]
        /// </summary>
        public ICommand PanLeftCommand { get; }

        /// <summary>
        /// [PAN] 오른쪽 이동 [Command]
        /// </summary>
        public ICommand PanRightCommand { get; }

        /// <summary>
        /// [TILT] 위쪽 이동 [Command]
        /// </summary>
        public ICommand TiltUpCommand { get; }

        /// <summary>
        /// [TILT] 아래쪽 이동 [Command]
        /// </summary>
        public ICommand TiltDownCommand { get; }

        /// <summary>
        /// [PTZ] 이동 정지 [Command]
        /// </summary>
        public ICommand StopMoveCommand { get; }

        /// <summary>
        /// [ZOOM] 확대 [Command]
        /// </summary>
        public ICommand ZoomInCommand { get; }

        /// <summary>
        /// [ZOOM] 축소 [Command]
        /// </summary>
        public ICommand ZoomOutCommand { get; }

        /// <summary>
        /// [FOCUS] Near [Command]
        /// </summary>
        public ICommand FocusNearCommand { get; }

        /// <summary>
        /// [FOCUS] Far [Command]
        /// </summary>
        public ICommand FocusFarCommand { get; }

        #endregion

        #region [Track Commands]

        /// <summary>
        /// 항적 선택 [Command]
        /// </summary>
        public ICommand SelectTrackCommand { get; }

        /// <summary>
        /// 항적 기반 [Lock-On] [Command]
        /// </summary>
        public ICommand LockOnCommand { get; }

        /// <summary>
        /// 항적 기반 [Lock-Off] [Command]
        /// </summary>
        public ICommand LockOffCommand { get; }

        #endregion

        #region [AI Commands]

        /// <summary>
        /// [AI] 분석 시작 [Command]
        /// </summary>
        public ICommand StartAiCommand { get; }

        /// <summary>
        /// [AI] 분석 중지 [Command]
        /// </summary>
        public ICommand StopAiCommand { get; }

        #endregion

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MainViewModel] 생성자
        /// </summary>
        public MainViewModel()
        {
            #region [Command Initialize]

            #region [MQ Command Binding]

            ConnectMqCommand =
                new RelayCommand(ConnectMq);

            DisconnectMqCommand =
                new RelayCommand(DisconnectMq);

            #endregion

            #region [UDP Command Binding]

            StartUdpReceiveCommand =
                new RelayCommand(StartUdpReceive);

            StopUdpReceiveCommand =
                new RelayCommand(StopUdpReceive);

            #endregion

            #region [MMAP Command Binding]

            StartMmapReadCommand =
                new RelayCommand(StartMmapRead);

            StopMmapReadCommand =
                new RelayCommand(StopMmapRead);

            StartDummyMmapWriterCommand =
                new RelayCommand(StartDummyMmapWriter);

            #endregion

            #region [Camera Command Binding]

            PanLeftCommand =
                new RelayCommand(PanLeft);

            PanRightCommand =
                new RelayCommand(PanRight);

            TiltUpCommand =
                new RelayCommand(TiltUp);

            TiltDownCommand =
                new RelayCommand(TiltDown);

            StopMoveCommand =
                new RelayCommand(StopMove);

            ZoomInCommand =
                new RelayCommand(ZoomIn);

            ZoomOutCommand =
                new RelayCommand(ZoomOut);

            FocusNearCommand =
                new RelayCommand(FocusNear);

            FocusFarCommand =
                new RelayCommand(FocusFar);

            #endregion

            #region [Track Command Binding]

            SelectTrackCommand =
                new RelayCommand(SelectTrack);

            LockOnCommand =
                new RelayCommand(LockOn);

            LockOffCommand =
                new RelayCommand(LockOff);

            #endregion

            #region [AI Command Binding]

            StartAiCommand =
                new RelayCommand(StartAi);

            StopAiCommand =
                new RelayCommand(StopAi);

            #endregion

            #endregion

            #region [Service Initialize]

            /// <summary>
            /// TODO:
            /// 서비스 구현 후 순차적으로 생성한다.
            /// 
            /// - MqService
            /// - UdpClientService
            /// - MmapVideoFrameService
            /// - DummyMmapFrameWriter
            /// - CameraCommandHandler
            /// - ICameraControlService
            /// </summary>

            #endregion

            #region [Default Initialize]

            InitializeDefaultValues();

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MAIN] Vertiport Surveillance GUI -> Initialize Complete");
            ConsoleLogHelper.PrintLine();

            #endregion
        }

        #endregion

        #region [Bindable Properties]

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

        #region [UDP Properties]

        /// <summary>
        /// [UDP] 수신 상태 표시 문자열
        /// </summary>
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
        /// 마지막 [UDP] 수신 패킷 표시 문자열
        /// </summary>
        public string LastUdpPacketText
        {
            get => _lastUdpPacketText;
            private set
            {
                if (_lastUdpPacketText != value)
                {
                    _lastUdpPacketText = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region [MMAP Properties]

        /// <summary>
        /// [MMAP] 영상 수신 상태 표시 문자열
        /// </summary>
        public string MmapStatusText
        {
            get => _mmapStatusText;
            private set
            {
                if (_mmapStatusText != value)
                {
                    _mmapStatusText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// [MMAP] 마지막 영상 [Frame Number]
        /// </summary>
        public long LastFrameNumber
        {
            get => _lastFrameNumber;
            private set
            {
                if (_lastFrameNumber != value)
                {
                    _lastFrameNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region [Camera Properties]

        /// <summary>
        /// 카메라 연결 상태 표시 문자열
        /// </summary>
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

        /// <summary>
        /// 현재 [Pan] 값
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
        /// 현재 [Tilt] 값
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
        /// 현재 [Zoom] 값
        /// </summary>
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

        /// <summary>
        /// 현재 [Focus] 값
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

        #region [Track Properties]

        /// <summary>
        /// 항적 수신 상태 표시 문자열
        /// </summary>
        public string TrackStatusText
        {
            get => _trackStatusText;
            private set
            {
                if (_trackStatusText != value)
                {
                    _trackStatusText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 현재 선택된 항적 [ID]
        /// </summary>
        public int SelectedTrackId
        {
            get => _selectedTrackId;
            set
            {
                if (_selectedTrackId != value)
                {
                    _selectedTrackId = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region [AI Properties]

        /// <summary>
        /// [AI] 분석 상태 표시 문자열
        /// </summary>
        public string AiStatusText
        {
            get => _aiStatusText;
            private set
            {
                if (_aiStatusText != value)
                {
                    _aiStatusText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 현재 탐지 객체 수
        /// </summary>
        public int DetectionCount
        {
            get => _detectionCount;
            private set
            {
                if (_detectionCount != value)
                {
                    _detectionCount = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region [Image Properties]

        /// <summary>
        /// [EO] 또는 주 영상 출력용 [Image]
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

        /// <summary>
        /// [IR] 또는 보조 영상 출력용 [Image]
        /// </summary>
        public BitmapSource IRCameraImage
        {
            get => _irCameraImage;
            private set
            {
                if (_irCameraImage != value)
                {
                    _irCameraImage = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region [Status Properties]

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

        #endregion

        #endregion

        #region [Binding Collections]

        /// <summary>
        /// 화면에 표시할 [Detection Box] 목록
        /// 
        /// 실제 [DetectionBox] Model 생성 후 타입 변경 예정.
        /// </summary>
        public ObservableCollection<object> DetectionBoxes { get; }
            = new ObservableCollection<object>();

        /// <summary>
        /// 화면에 표시할 항적 목록
        /// 
        /// 실제 [AircraftTrackInfo] Model 생성 후 타입 변경 예정.
        /// </summary>
        public ObservableCollection<object> TrackList { get; }
            = new ObservableCollection<object>();

        #endregion

        #region [Initialize]

        /// <summary>
        /// 기본 상태값 초기화
        /// </summary>
        private void InitializeDefaultValues()
        {
            MainStatusText = "Ready";
            OperationModeText = "Manual";

            MqStatusText = "MQ Disconnected";
            UdpStatusText = "UDP Stopped";
            MmapStatusText = "MMAP Stopped";

            CameraStatusText = "Camera Disconnected";
            TrackStatusText = "Track Not Received";
            AiStatusText = "AI Stopped";
        }

        #endregion

        #region [MQ Methods]

        /// <summary>
        /// [MQ] 연결 처리
        /// </summary>
        private void ConnectMq()
        {
            MqStatusText = "MQ Connected";

            Console.WriteLine();
            Console.WriteLine("[MQ] CONNECT");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [MQ] 연결 해제 처리
        /// </summary>
        private void DisconnectMq()
        {
            MqStatusText = "MQ Disconnected";

            Console.WriteLine();
            Console.WriteLine("[MQ] DISCONNECT");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [UDP Methods]

        /// <summary>
        /// [UDP] 수신 시작 처리
        /// </summary>
        private void StartUdpReceive()
        {
            UdpStatusText = "UDP Receiving";

            Console.WriteLine();
            Console.WriteLine("[UDP] RECEIVE START");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [UDP] 수신 중지 처리
        /// </summary>
        private void StopUdpReceive()
        {
            UdpStatusText = "UDP Stopped";

            Console.WriteLine();
            Console.WriteLine("[UDP] RECEIVE STOP");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [MMAP Methods]

        /// <summary>
        /// [MMAP] 영상 수신 시작 처리
        /// </summary>
        private void StartMmapRead()
        {
            MmapStatusText = "MMAP Reading";

            Console.WriteLine();
            Console.WriteLine("[MMAP] READ START");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [MMAP] 영상 수신 중지 처리
        /// </summary>
        private void StopMmapRead()
        {
            MmapStatusText = "MMAP Stopped";

            Console.WriteLine();
            Console.WriteLine("[MMAP] READ STOP");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Dummy MMAP Writer] 시작 처리
        /// </summary>
        private void StartDummyMmapWriter()
        {
            Console.WriteLine();
            Console.WriteLine("[MMAP] DUMMY WRITER START");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Camera Control Methods]

        /// <summary>
        /// [PAN] 왼쪽 이동 처리
        /// </summary>
        private void PanLeft()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] PAN LEFT");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [PAN] 오른쪽 이동 처리
        /// </summary>
        private void PanRight()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] PAN RIGHT");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [TILT] 위쪽 이동 처리
        /// </summary>
        private void TiltUp()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] TILT UP");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [TILT] 아래쪽 이동 처리
        /// </summary>
        private void TiltDown()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] TILT DOWN");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [ZOOM] 확대 처리
        /// </summary>
        private void ZoomIn()
        {
            _currentMoveType = ContinuousMoveType.Zoom;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] ZOOM IN");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [ZOOM] 축소 처리
        /// </summary>
        private void ZoomOut()
        {
            _currentMoveType = ContinuousMoveType.Zoom;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] ZOOM OUT");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [FOCUS] Near 처리
        /// </summary>
        private void FocusNear()
        {
            _currentMoveType = ContinuousMoveType.Focus;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] FOCUS NEAR");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [FOCUS] Far 처리
        /// </summary>
        private void FocusFar()
        {
            _currentMoveType = ContinuousMoveType.Focus;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] FOCUS FAR");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 이동 정지 처리
        /// </summary>
        private void StopMove()
        {
            if (_currentMoveType == ContinuousMoveType.None)
                return;

            Console.WriteLine();
            Console.WriteLine($"[CAMERA] STOP MOVE : {_currentMoveType}");
            ConsoleLogHelper.PrintLine();

            _currentMoveType = ContinuousMoveType.None;
        }

        #endregion

        #region [Track Methods]

        /// <summary>
        /// 항적 선택 처리
        /// </summary>
        private void SelectTrack()
        {
            Console.WriteLine();
            Console.WriteLine($"[TRACK] SELECT : {SelectedTrackId}");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Lock-On] 처리
        /// </summary>
        private void LockOn()
        {
            Console.WriteLine();
            Console.WriteLine($"[TRACK] LOCK-ON : {SelectedTrackId}");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Lock-Off] 처리
        /// </summary>
        private void LockOff()
        {
            Console.WriteLine();
            Console.WriteLine("[TRACK] LOCK-OFF");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [AI Methods]

        /// <summary>
        /// [AI] 분석 시작 처리
        /// </summary>
        private void StartAi()
        {
            AiStatusText = "AI Running";

            Console.WriteLine();
            Console.WriteLine("[AI] START");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [AI] 분석 중지 처리
        /// </summary>
        private void StopAi()
        {
            AiStatusText = "AI Stopped";

            Console.WriteLine();
            Console.WriteLine("[AI] STOP");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [INotifyPropertyChanged]

        /// <summary>
        /// [Property] 값 변경 이벤트
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// [Property] 값 변경 알림
        /// </summary>
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