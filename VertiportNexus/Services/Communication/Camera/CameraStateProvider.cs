using System;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [Camera] 상태 저장 서비스
    /// 
    /// [ADS1000] 수신 [Packet]에서 파싱된
    /// 현재 [Pan] / [Tilt] / [Zoom] 값을 보관한다.
    /// 
    /// [MainViewModel]은 수신 상태값을 갱신하고,
    /// [CseCommandHandler]는 상태 조회 응답 생성 시 해당 값을 참조한다.
    /// </summary>
    internal class CameraStateProvider
    {
        #region [Constants]

        /// <summary>
        /// [PTZ] 기본 제어 모드
        /// </summary>
        private const string DEFAULT_PTZ_CONTROL_MODE =
            "MANUAL";

        #endregion

        #region [Fields]

        /// <summary>
        /// 상태값 동시 접근 제어 객체
        /// 
        /// [TCP] 수신 Thread와
        /// [MQ] 명령 처리 Thread가 동시에 접근할 수 있으므로
        /// lock 기준으로 상태값을 보호한다.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// 현재 [Pan] 값
        /// </summary>
        private double? _currentPan;

        /// <summary>
        /// 현재 [Tilt] 값
        /// </summary>
        private double? _currentTilt;

        /// <summary>
        /// 현재 [Zoom] 값
        /// </summary>
        private double? _currentZoom;

        /// <summary>
        /// 현재 [Focus] 값
        /// </summary>
        private double? _currentFocus;

        /// <summary>
        /// 현재 [PTZ] 제어 모드
        /// 
        /// [IF-GUIS-CSE-008] 요청으로 설정되는
        /// [AUTO] / [MANUAL] 값을 보관한다.
        /// </summary>
        private string _ptzControlMode =
            DEFAULT_PTZ_CONTROL_MODE;

        /// <summary>
        /// 마지막 상태 갱신 시간
        /// </summary>
        private DateTime? _lastUpdatedTime;

        /// <summary>
        /// 카메라 연결 상태
        /// </summary>
        private bool _isConnected;

        #endregion

        #region [Events]

        /// <summary>
        /// [PTZ] 제어 모드 변경 이벤트
        /// 
        /// [MQ] 명령 수신으로 모드가 변경된 경우
        /// 화면 표시값을 갱신하기 위해 사용한다.
        /// </summary>
        public event Action<string> PtzControlModeChanged;

        #endregion

        #region [Properties]

        /// <summary>
        /// 현재 [Pan] 값
        /// </summary>
        public double? CurrentPan
        {
            get
            {
                lock (_syncLock)
                {
                    return _currentPan;
                }

            }

        }

        /// <summary>
        /// 현재 [Tilt] 값
        /// </summary>
        public double? CurrentTilt
        {
            get
            {
                lock (_syncLock)
                {
                    return _currentTilt;
                }

            }

        }

        /// <summary>
        /// 현재 [Zoom] 값
        /// </summary>
        public double? CurrentZoom
        {
            get
            {
                lock (_syncLock)
                {
                    return _currentZoom;
                }

            }

        }

        /// <summary>
        /// 현재 [Focus] 값
        /// </summary>
        public double? CurrentFocus
        {
            get
            {
                lock (_syncLock)
                {
                    return _currentFocus;
                }

            }

        }

        /// <summary>
        /// 현재 [PTZ] 제어 모드
        /// </summary>
        public string PtzControlMode
        {
            get
            {
                lock (_syncLock)
                {
                    return _ptzControlMode;
                }

            }

        }

        /// <summary>
        /// 마지막 상태 갱신 시간
        /// </summary>
        public DateTime? LastUpdatedTime
        {
            get
            {
                lock (_syncLock)
                {
                    return _lastUpdatedTime;
                }

            }

        }

        /// <summary>
        /// 카메라 연결 상태
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (_syncLock)
                {
                    return _isConnected;
                }

            }

        }

        #endregion

        #region [Update Methods]

        /// <summary>
        /// [Pan] 상태값 갱신
        /// </summary>
        /// <param name="pan">
        /// 현재 [Pan] 값
        /// </param>
        public void UpdatePan(
            double pan)
        {
            lock (_syncLock)
            {
                _currentPan =
                    pan;

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// [Tilt] 상태값 갱신
        /// </summary>
        /// <param name="tilt">
        /// 현재 [Tilt] 값
        /// </param>
        public void UpdateTilt(
            double tilt)
        {
            lock (_syncLock)
            {
                _currentTilt =
                    tilt;

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// [Zoom] 상태값 갱신
        /// </summary>
        /// <param name="zoom">
        /// 현재 [Zoom] 값
        /// </param>
        public void UpdateZoom(
            double zoom)
        {
            lock (_syncLock)
            {
                _currentZoom =
                    zoom;

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// [PTZ] 상태값 일괄 갱신
        /// 
        /// 수신 [Packet]에 포함된 값만 갱신하고,
        /// 포함되지 않은 값은 기존 상태값을 유지한다.
        /// </summary>
        /// <param name="pan">
        /// 현재 [Pan] 값
        /// </param>
        /// <param name="tilt">
        /// 현재 [Tilt] 값
        /// </param>
        /// <param name="zoom">
        /// 현재 [Zoom] 값
        /// </param>
        public void UpdateState(
            double? pan,
            double? tilt,
            double? zoom,
            double? focus)
        {
            lock (_syncLock)
            {
                if (pan.HasValue)
                {
                    _currentPan =
                        pan.Value;
                }

                if (tilt.HasValue)
                {
                    _currentTilt =
                        tilt.Value;
                }

                if (zoom.HasValue)
                {
                    _currentZoom =
                        zoom.Value;
                }

                if (focus.HasValue)
                {
                    _currentFocus =
                        zoom.Value;
                }

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// [PTZ] 제어 모드 갱신
        /// 
        /// [IF-GUIS-CSE-008] 요청 또는
        /// 화면 버튼 조작으로 설정된 [AUTO] / [MANUAL] 값을 저장한다.
        /// </summary>
        /// <param name="mode">
        /// [PTZ] 제어 모드
        /// </param>
        public void UpdatePtzControlMode(
            string mode)
        {
            if (string.IsNullOrWhiteSpace(
                mode))
            {
                return;
            }

            string normalizedMode =
                mode.Trim().ToUpper();

            lock (_syncLock)
            {
                _ptzControlMode =
                    normalizedMode;

                _lastUpdatedTime =
                    DateTime.Now;
            }

            PtzControlModeChanged?.Invoke(
                normalizedMode);
        }

        /// <summary>
        /// 카메라 연결 상태 갱신
        /// </summary>
        /// <param name="isConnected">
        /// 연결 여부
        /// </param>
        public void UpdateConnectionState(
            bool isConnected)
        {
            lock (_syncLock)
            {
                _isConnected =
                    isConnected;

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }
        #endregion
    }

}
