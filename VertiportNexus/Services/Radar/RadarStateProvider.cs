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

        #endregion

        #region [Public Methods]

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
