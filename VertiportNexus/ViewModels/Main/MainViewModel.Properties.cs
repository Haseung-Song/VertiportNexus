using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Binding Property
    /// 
    /// [XAML] 화면에 표시되거나 입력되는
    /// 상태값 / 설정값 / 제어 입력값 Property를 관리한다.
    /// 
    /// MainViewModel 본문 비중을 줄이기 위해
    /// 화면 Binding 관련 Property만 별도 Partial 파일로 분리한다.
    /// </summary>
    public partial class MainViewModel
    {
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
                if (_ads1000CameraControlService.PanTiltSpeedLevel != value)
                {
                    Console.WriteLine(
                        "[UI][PTZ] Pan / Tilt Speed Value Changed : "
                        + _ads1000CameraControlService.PanTiltSpeedLevel.ToString("F0")
                        + " -> "
                        + value.ToString("F0"));

                    Console.WriteLine();

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
    }

}
