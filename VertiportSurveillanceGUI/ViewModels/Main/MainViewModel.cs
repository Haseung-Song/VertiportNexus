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
    /// 1. [MQ] 상위 시스템 메시지 송수신 관리
    /// 2. [UDP] [EOC] 장비 제어 및 상태 수신 관리
    /// 3. [MMAP] 영상 / 항적 / 탐지 데이터 공유 관리
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
        /// [EO] 영상 출력용 [Image]
        /// </summary>
        private BitmapSource _eoCameraImage;

        /// <summary>
        /// [IR] 영상 출력용 [Image]
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

        public ICommand ConnectMqCommand { get; }

        public ICommand DisconnectMqCommand { get; }

        #endregion

        #region [UDP Commands]

        public ICommand StartUdpReceiveCommand { get; }

        public ICommand StopUdpReceiveCommand { get; }

        #endregion

        #region [MMAP Commands]

        public ICommand StartMmapReadCommand { get; }

        public ICommand StopMmapReadCommand { get; }

        public ICommand StartDummyMmapWriterCommand { get; }

        #endregion

        #region [Camera Commands]

        public ICommand PanLeftCommand { get; }

        public ICommand PanRightCommand { get; }

        public ICommand TiltUpCommand { get; }

        public ICommand TiltDownCommand { get; }

        public ICommand StopMoveCommand { get; }

        public ICommand ZoomInCommand { get; }

        public ICommand ZoomOutCommand { get; }

        public ICommand FocusNearCommand { get; }

        public ICommand FocusFarCommand { get; }

        #endregion

        #region [Track Commands]

        public ICommand SelectTrackCommand { get; }

        public ICommand LockOnCommand { get; }

        public ICommand LockOffCommand { get; }

        #endregion

        #region [AI Commands]

        public ICommand StartAiCommand { get; }

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
            Console.WriteLine("[MAIN] Vertiport Surveillance GUI Initialize Complete");
            ConsoleLogHelper.PrintLine();

            #endregion
        }

        #endregion

        #region [Bindable Properties]

        #region [MQ Properties]

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

        #endregion

        #region [UDP Properties]

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

        #endregion

        #region [Track Properties]

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

        private void ConnectMq()
        {
            MqStatusText = "MQ Connected";

            Console.WriteLine();
            Console.WriteLine("[MQ] CONNECT");
            ConsoleLogHelper.PrintLine();
        }

        private void DisconnectMq()
        {
            MqStatusText = "MQ Disconnected";

            Console.WriteLine();
            Console.WriteLine("[MQ] DISCONNECT");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [UDP Methods]

        private void StartUdpReceive()
        {
            UdpStatusText = "UDP Receiving";

            Console.WriteLine();
            Console.WriteLine("[UDP] RECEIVE START");
            ConsoleLogHelper.PrintLine();
        }

        private void StopUdpReceive()
        {
            UdpStatusText = "UDP Stopped";

            Console.WriteLine();
            Console.WriteLine("[UDP] RECEIVE STOP");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [MMAP Methods]

        private void StartMmapRead()
        {
            MmapStatusText = "MMAP Reading";

            Console.WriteLine();
            Console.WriteLine("[MMAP] READ START");
            ConsoleLogHelper.PrintLine();
        }

        private void StopMmapRead()
        {
            MmapStatusText = "MMAP Stopped";

            Console.WriteLine();
            Console.WriteLine("[MMAP] READ STOP");
            ConsoleLogHelper.PrintLine();
        }

        private void StartDummyMmapWriter()
        {
            Console.WriteLine();
            Console.WriteLine("[MMAP] DUMMY WRITER START");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Camera Control Methods]

        private void PanLeft()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] PAN LEFT");
            ConsoleLogHelper.PrintLine();
        }

        private void PanRight()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] PAN RIGHT");
            ConsoleLogHelper.PrintLine();
        }

        private void TiltUp()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] TILT UP");
            ConsoleLogHelper.PrintLine();
        }

        private void TiltDown()
        {
            _currentMoveType = ContinuousMoveType.PanTilt;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] TILT DOWN");
            ConsoleLogHelper.PrintLine();
        }

        private void ZoomIn()
        {
            _currentMoveType = ContinuousMoveType.Zoom;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] ZOOM IN");
            ConsoleLogHelper.PrintLine();
        }

        private void ZoomOut()
        {
            _currentMoveType = ContinuousMoveType.Zoom;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] ZOOM OUT");
            ConsoleLogHelper.PrintLine();
        }

        private void FocusNear()
        {
            _currentMoveType = ContinuousMoveType.Focus;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] FOCUS NEAR");
            ConsoleLogHelper.PrintLine();
        }

        private void FocusFar()
        {
            _currentMoveType = ContinuousMoveType.Focus;

            Console.WriteLine();
            Console.WriteLine("[CAMERA] FOCUS FAR");
            ConsoleLogHelper.PrintLine();
        }

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

        private void SelectTrack()
        {
            Console.WriteLine();
            Console.WriteLine($"[TRACK] SELECT : {SelectedTrackId}");
            ConsoleLogHelper.PrintLine();
        }

        private void LockOn()
        {
            Console.WriteLine();
            Console.WriteLine($"[TRACK] LOCK-ON : {SelectedTrackId}");
            ConsoleLogHelper.PrintLine();
        }

        private void LockOff()
        {
            Console.WriteLine();
            Console.WriteLine("[TRACK] LOCK-OFF");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [AI Methods]

        private void StartAi()
        {
            AiStatusText = "AI Running";

            Console.WriteLine();
            Console.WriteLine("[AI] START");
            ConsoleLogHelper.PrintLine();
        }

        private void StopAi()
        {
            AiStatusText = "AI Stopped";

            Console.WriteLine();
            Console.WriteLine("[AI] STOP");
            ConsoleLogHelper.PrintLine();
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
