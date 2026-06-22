using System;

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
        /// 현재 [Zoom] 값
        /// </summary>
        private double? _currentZoom;

        /// <summary>
        /// 현재 [Focus] 값
        /// </summary>
        private double? _currentFocus;

        /// <summary>
        /// 마지막 상태 갱신 시간
        /// </summary>
        private DateTime? _lastUpdatedTime;

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
        /// [Focus] 상태값 갱신
        /// </summary>
        /// <param name="focus">
        /// 현재 [Focus] 값
        /// </param>
        public void UpdateFocus(
            double focus)
        {
            lock (_syncLock)
            {
                _currentFocus =
                    focus;

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
        /// <param name="focus">
        /// 현재 [Focus] 값
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
                        focus.Value;
                }

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }
        #endregion
    }

}
