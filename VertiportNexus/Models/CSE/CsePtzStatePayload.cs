using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] [PTZ] 상태 응답 모델
    /// 
    /// [IF-GUIS-CSE-012] 상태 조회 응답의
    /// [payload.ptz] 영역을 표현한다.
    /// </summary>
    public class CsePtzStatePayload
    {
        #region [Properties]

        /// <summary>
        /// 현재 [Pan] 위치 값
        /// 
        /// [ADS1000] 수신 [Packet]에서 파싱된
        /// 현재 [Pan] 상태값을 반환한다.
        /// </summary>
        [JsonPropertyName("pan")]
        public double? Pan { get; set; }

        /// <summary>
        /// 현재 [Tilt] 위치 값
        /// 
        /// [ADS1000] 수신 [Packet]에서 파싱된
        /// 현재 [Tilt] 상태값을 반환한다.
        /// </summary>
        [JsonPropertyName("tilt")]
        public double? Tilt { get; set; }

        /// <summary>
        /// 현재 [Zoom] 위치 값
        /// 
        /// [ADS1000] 수신 [Packet]에서 파싱된
        /// 현재 [Zoom] 상태값을 반환한다.
        /// </summary>
        [JsonPropertyName("zoom")]
        public double? Zoom { get; set; }

        #endregion
    }

}
