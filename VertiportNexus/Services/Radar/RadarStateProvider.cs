using System;
using VertiportNexus.Models.Radar;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] 상태 저장 서비스
    /// 
    /// Radar Packet 수신 결과와
    /// 마지막 추적 요청 / BIST 요청 정보를 보관한다.
    /// </summary>
    internal class RadarStateProvider
    {
        #region [Fields]

        /// <summary>
        /// [Radar] Tracking 활성 상태
        /// 
        /// Radar Tracking Request가 수신된 이후,
        /// GUI BBOX 기반 추적 제어보다 Radar 지향 제어를 우선하기 위해 사용한다.
        /// </summary>
        private bool _isRadarTrackingActive;

        /// <summary>
        /// [Radar] 마지막 Tracking Request 수신 시간
        /// </summary>
        private DateTime? _lastRadarTrackingTime;

        /// <summary>
        /// 동기화 객체
        /// </summary>
        private readonly object _syncLock =
            new object();

        #endregion

        #region [Properties]

        /// <summary>
        /// 마지막 추적 요청 정보
        /// </summary>
        public RadarTrackingRequestPayload LastTrackingRequest { get; private set; }

        /// <summary>
        /// 마지막 BIST 요청 정보
        /// </summary>
        public RadarBistRequestPayload LastBistRequest { get; private set; }

        /// <summary>
        /// 마지막 수신 시간
        /// </summary>
        public DateTime LastReceivedTime { get; private set; }

        /// <summary>
        /// [Radar] Tracking 활성 상태
        /// </summary>
        public bool IsRadarTrackingActive
        {
            get
            {
                lock (_syncLock)
                {
                    return _isRadarTrackingActive;
                }

            }

        }

        /// <summary>
        /// [Radar] 마지막 Tracking Request 수신 시간
        /// </summary>
        public DateTime? LastRadarTrackingTime
        {
            get
            {
                lock (_syncLock)
                {
                    return _lastRadarTrackingTime;
                }

            }

        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Radar] Tracking 활성 상태 설정
        /// 
        /// Radar Tracking Request 수신 시 호출되며,
        /// GUI BBOX 기반 추적보다 Radar 지향 제어를 우선하기 위한 상태값을 설정한다.
        /// </summary>
        public void StartRadarTracking()
        {
            lock (_syncLock)
            {
                _isRadarTrackingActive =
                    true;

                _lastRadarTrackingTime =
                    DateTime.Now;
            }
            Console.WriteLine("[RADAR][STATE] Tracking Active : True");
        }

        /// <summary>
        /// [Radar] Tracking 비활성 상태 설정
        /// 
        /// GUI Detect Off 수신 또는 Radar Tracking 종료 시 호출되며,
        /// Radar 우선 제어 상태를 해제한다.
        /// </summary>
        public void StopRadarTracking()
        {
            lock (_syncLock)
            {
                _isRadarTrackingActive =
                    false;

                _lastRadarTrackingTime =
                    null;
            }
            Console.WriteLine("[RADAR][STATE] Tracking Active : False");
        }

        /// <summary>
        /// 추적 요청 상태 갱신
        /// </summary>
        /// <param name="payload">
        /// 추적 요청 Payload
        /// </param>
        public void UpdateTrackingRequest(
            RadarTrackingRequestPayload payload)
        {
            if (payload == null)
            {
                Console.WriteLine(
                    "[RADAR][STATE] Tracking Request Update Failed : Payload is null");

                return;
            }

            lock (_syncLock)
            {
                LastTrackingRequest =
                    payload;

                LastReceivedTime =
                    DateTime.Now;
            }

            Console.WriteLine(
                "[RADAR][STATE] Tracking Request Updated");

            Console.WriteLine(
                "[RADAR][STATE] Target Id : "
                + payload.Id);

            Console.WriteLine(
                "[RADAR][STATE] Azimuth : "
                + payload.Azimuth);

            Console.WriteLine(
                "[RADAR][STATE] Elevation : "
                + payload.Elevation);

            Console.WriteLine(
                "[RADAR][STATE] Distance : "
                + payload.Distance);

            Console.WriteLine();
        }

        /// <summary>
        /// BIST 요청 상태 갱신
        /// </summary>
        /// <param name="payload">
        /// BIST 요청 Payload
        /// </param>
        public void UpdateBistRequest(
            RadarBistRequestPayload payload)
        {
            if (payload == null)
            {
                Console.WriteLine(
                    "[RADAR][STATE] BIST Request Update Failed : Payload is null");

                return;
            }

            lock (_syncLock)
            {
                LastBistRequest =
                    payload;

                LastReceivedTime =
                    DateTime.Now;
            }

            Console.WriteLine(
                "[RADAR][STATE] BIST Request Updated");

            Console.WriteLine(
                "[RADAR][STATE] BIST Type : "
                + payload.BistType);

            Console.WriteLine(
                "[RADAR][STATE] Comport Number : "
                + payload.ComportNumber);

            Console.WriteLine(
                "[RADAR][STATE] CBIST Interval : "
                + payload.CbistInterval);

            Console.WriteLine();
        }
        #endregion
    }

}
