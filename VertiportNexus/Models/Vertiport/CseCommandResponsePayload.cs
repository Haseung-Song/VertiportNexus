using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 응답 [Payload] 모델
    /// 
    /// [CSE] 명령 처리 결과와 함께 반환할
    /// 응답 데이터를 보관한다.
    /// 
    /// [IF-GUIS-CSE-011]
    /// 설정 조회 응답
    /// 
    /// [IF-GUIS-CSE-012]
    /// [PTZ] 상태 조회 응답
    /// 
    /// 에서 공통으로 사용한다.
    /// </summary>
    public class CseCommandResponsePayload
    {
        #region [PTZ State Properties]

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

        /// <summary>
        /// 현재 [Focus] 위치 값
        /// 
        /// [ADS1000] 수신 [Packet]에서 파싱된
        /// 현재 [Focus] 상태값을 반환한다.
        /// </summary>
        [JsonPropertyName("focus")]
        public double? Focus { get; set; }

        /// <summary>
        /// 상태 갱신 시간
        /// 
        /// 마지막 [PTZ] 상태값이
        /// 갱신된 시간을 반환한다.
        /// </summary>
        [JsonPropertyName("updated_time")]
        public string UpdatedTime { get; set; }

        #endregion

        #region [Config Properties]

        /// <summary>
        /// 영상 채널 정보
        /// 
        /// 현재 사용 중인 영상 채널 정보를 반환한다.
        /// 
        /// 예)
        /// EO
        /// IR
        /// </summary>
        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        /// <summary>
        /// 영상 반전 설정 여부
        /// 
        /// 현재 영상의 [Flip] 적용 상태를 반환한다.
        /// </summary>
        [JsonPropertyName("flip")]
        public bool? Flip { get; set; }

        /// <summary>
        /// 탐지 활성화 여부
        /// 
        /// 현재 객체 탐지 기능의
        /// 활성화 상태를 반환한다.
        /// </summary>
        [JsonPropertyName("detect_enabled")]
        public bool? DetectEnabled { get; set; }

        #endregion
    }

}
