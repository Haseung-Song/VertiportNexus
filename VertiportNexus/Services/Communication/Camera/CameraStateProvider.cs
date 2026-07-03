using System;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [Camera] 상태 저장 서비스
    /// 
    /// [ADS1000] 수신 [Packet]에서 파싱된
    /// 현재 [Pan] / [Tilt] / [Zoom] / [Focus] 값을 보관한다.
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

        /// <summary>
        /// [Zoom] 최소 배율
        /// </summary>
        private const double MIN_ZOOM_RATIO =
            1.0;

        /// <summary>
        /// [Zoom] 최대 배율
        /// </summary>
        private const double MAX_ZOOM_RATIO =
            66.0;

        /// <summary>
        /// [Zoom] 최대 Position 값
        /// 
        /// [ADS1000] Zoom Position 값은 내부 장비 제어값이고,
        /// 상태 응답에서는 사용자 표시용 배율값으로 변환해서 사용한다.
        /// </summary>
        private const double MAX_ZOOM_POSITION =
            1000.0;

        #endregion

        #region [Fields]

        /// <summary>
        /// 상태값 동시 접근 제어 객체
        /// 
        /// [TCP] 수신 Thread와
        /// [MQ] 명령 처리 Thread가 동시에 접근할 수 있으므로
        /// lock 기준으로 상태값을 보호한다.
        /// </summary>
        private readonly object _syncLock =
            new object();

        /// <summary>
        /// 현재 [Pan] 값
        /// </summary>
        private double? _currentPan;

        /// <summary>
        /// 현재 [Tilt] 값
        /// </summary>
        private double? _currentTilt;

        /// <summary>
        /// 현재 Zoom 배율
        /// 
        /// 장비에서 수신한 Zoom Position 값을
        /// 사용자 표시용 배율값으로 변환한 값이다.
        /// </summary>
        private double? _currentZoomRatio;

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
        /// [IF-GUIS-CSE-004] 요청 또는 화면 제어로 설정되는
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

        /// <summary>
        /// 현재 [Pan] 선회 모드
        /// 
        /// [Pan Absolute] 이동 시
        /// [Via 0] / [Short] 이동 방식을 결정한다.
        /// </summary>
        private Ads1000PanTurnMode _panTurnMode =
            Ads1000PanTurnMode.Short;

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
        /// 현재 [Zoom] 배율
        /// </summary>
        public double? CurrentZoomRatio
        {
            get
            {
                lock (_syncLock)
                {
                    return _currentZoomRatio;
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

        /// <summary>
        /// 현재 [Pan] 선회 모드
        /// 
        /// [Pan Absolute] 이동 시
        /// [Via 0] / [Short] 이동 방식을 반환한다.
        /// </summary>
        public Ads1000PanTurnMode PanTurnMode
        {
            get
            {
                lock (_syncLock)
                {
                    return _panTurnMode;
                }

            }

        }

        #endregion

        #region [Update Methods]

        /// <summary>
        /// [Pan] 상태값 갱신
        /// </summary>
        public void UpdatePan(
            double pan)
        {
            lock (_syncLock)
            {
                _currentPan =
                    pan;

                UpdateLastUpdatedTime();
            }

        }

        /// <summary>
        /// [Tilt] 상태값 갱신
        /// </summary>
        public void UpdateTilt(
            double tilt)
        {
            lock (_syncLock)
            {
                _currentTilt =
                    tilt;

                UpdateLastUpdatedTime();
            }

        }

        /// <summary>
        /// [Zoom] 상태값 갱신
        /// 
        /// 장비에서 수신한 Zoom Position 값을 저장하고,
        /// 상태 응답 / 화면 표시용 Zoom 배율값도 함께 갱신한다.
        /// </summary>
        public void UpdateZoom(
            double zoom)
        {
            lock (_syncLock)
            {
                _currentZoom =
                    zoom;

                _currentZoomRatio =
                    ConvertZoomPositionToRatio(
                        zoom);

                UpdateLastUpdatedTime();
            }

        }

        /// <summary>
        /// [Focus] 상태값 갱신
        /// </summary>
        public void UpdateFocus(
            double focus)
        {
            lock (_syncLock)
            {
                _currentFocus =
                    focus;

                UpdateLastUpdatedTime();
            }

        }

        /// <summary>
        /// [PTZ] 상태값 일괄 갱신
        /// 
        /// 수신 [Packet]에 포함된 값만 갱신하고,
        /// 포함되지 않은 값은 기존 상태값을 유지한다.
        /// </summary>
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

                    _currentZoomRatio =
                        ConvertZoomPositionToRatio(
                            zoom.Value);
                }

                if (focus.HasValue)
                {
                    _currentFocus =
                        focus.Value;
                }
                UpdateLastUpdatedTime();
            }

        }

        /// <summary>
        /// [PTZ] 제어 모드 갱신
        /// 
        /// [IF-GUIS-CSE-004] 요청 또는
        /// 화면 버튼 조작으로 설정된 [AUTO] / [MANUAL] 값을 저장한다.
        /// </summary>
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

            if (normalizedMode != "AUTO" &&
                normalizedMode != "MANUAL")
            {
                return;
            }

            lock (_syncLock)
            {
                if (_ptzControlMode == normalizedMode)
                {
                    return;
                }

                _ptzControlMode =
                    normalizedMode;

                UpdateLastUpdatedTime();
            }

            PtzControlModeChanged?.Invoke(
                normalizedMode);
        }

        /// <summary>
        /// 카메라 연결 상태 갱신
        /// </summary>
        public void UpdateConnectionState(
            bool isConnected)
        {
            lock (_syncLock)
            {
                _isConnected =
                    isConnected;

                UpdateLastUpdatedTime();
            }

        }

        /// <summary>
        /// [Pan] 선회 모드 갱신
        /// 
        /// [Pan Absolute] 이동 시 사용할
        /// [Via 0] / [Short] 이동 방식을 저장한다.
        /// </summary>
        /// <param name="panTurnMode">
        /// Pan 선회 모드
        /// </param>
        public void UpdatePanTurnMode(
            Ads1000PanTurnMode panTurnMode)
        {
            lock (_syncLock)
            {
                _panTurnMode =
                    panTurnMode;
            }

        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [Zoom] 위치값을 배율로 변환
        /// 
        /// [ADS1000] 장비에서 수신되는 [Zoom] 값은
        /// [0 ~ 1000] 범위의 내부 위치값이다.
        /// 
        /// 상태 응답 [IF-CSE-GUIS-112]에서는
        /// 장비 내부 위치값이 아니라 실제 배율값을 보내야 하므로,
        /// 장비 위치값을 [1.0x ~ 66.0x] 배율 기준으로 변환한다.
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
            const double MIN_ZOOM_RATIO =
                1.0;

            const double MAX_ZOOM_RATIO =
                66.0;

            const double MIN_ZOOM_POSITION =
                0.0;

            const double MAX_ZOOM_POSITION =
                1000.0;

            double clampedZoomPosition =
                Clamp(
                    zoomPosition,
                    MIN_ZOOM_POSITION,
                    MAX_ZOOM_POSITION);

            double zoomRatio =
                MIN_ZOOM_RATIO
                + clampedZoomPosition
                / MAX_ZOOM_POSITION
                * (MAX_ZOOM_RATIO - MIN_ZOOM_RATIO);

            return zoomRatio;
        }

        /// <summary>
        /// [실수] 입력값 범위 보정
        /// 
        /// 입력된 값이 지정 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        /// <param name="value">
        /// 입력값
        /// </param>
        /// <param name="min">
        /// 최소값
        /// </param>
        /// <param name="max">
        /// 최대값
        /// </param>
        /// <returns>
        /// 범위 보정된 값
        /// </returns>
        private double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }
            return value;
        }

        /// <summary>
        /// 마지막 상태 갱신 시간 반영
        /// </summary>
        private void UpdateLastUpdatedTime()
        {
            _lastUpdatedTime =
                DateTime.Now;
        }
        #endregion
    }

}
