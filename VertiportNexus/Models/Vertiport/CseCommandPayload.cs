using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 [Payload] 모델
    /// 
    /// [ptz_move] / [ptz_stop] / [get_state] 등
    /// [ICD] 메시지의 [payload] 영역을 담는다.
    /// 
    /// 모든 명령이 같은 필드를 사용하지 않으므로,
    /// 우선 공통 사용 가능성이 있는 값을 nullable로 정의한다.
    /// </summary>
    public class CseCommandPayload
    {
        #region [PTZ Properties]

        /// <summary>
        /// [PTZ] 제어 모드
        /// 
        /// 예)
        /// absolute
        /// relative
        /// continuous
        /// </summary>
        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// [Pan] 방위각
        /// </summary>
        [JsonPropertyName("pan")]
        public double? Pan { get; set; }

        /// <summary>
        /// [Tilt] 고각
        /// </summary>
        [JsonPropertyName("tilt")]
        public double? Tilt { get; set; }

        /// <summary>
        /// [Zoom] 값
        /// </summary>
        [JsonPropertyName("zoom")]
        public double? Zoom { get; set; }

        /// <summary>
        /// [Focus] 값
        /// </summary>
        [JsonPropertyName("focus")]
        public double? Focus { get; set; }

        #endregion

        #region [Detect Properties]

        /// <summary>
        /// 탐지 항적 [ID]
        /// </summary>
        [JsonPropertyName("track_id")]
        public int? TrackId { get; set; }

        /// <summary>
        /// 탐지 중심 / 객체 좌표 [X1]
        /// </summary>
        [JsonPropertyName("x1")]
        public double? X1 { get; set; }

        /// <summary>
        /// 탐지 중심 / 객체 좌표 [Y1]
        /// </summary>
        [JsonPropertyName("y1")]
        public double? Y1 { get; set; }

        /// <summary>
        /// 탐지 영역 좌표 [X2]
        /// </summary>
        [JsonPropertyName("x2")]
        public double? X2 { get; set; }

        /// <summary>
        /// 탐지 영역 좌표 [Y2]
        /// </summary>
        [JsonPropertyName("y2")]
        public double? Y2 { get; set; }

        /// <summary>
        /// 객체 종류 [ID]
        /// </summary>
        [JsonPropertyName("class_id")]
        public int? ClassId { get; set; }

        /// <summary>
        /// 탐지 신뢰도
        /// </summary>
        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        #endregion

        #region [Image Properties]

        /// <summary>
        /// 영상 반전 여부
        /// </summary>
        [JsonPropertyName("flip")]
        public bool? Flip { get; set; }

        /// <summary>
        /// 예)
        /// [EO] 주간 채널
        /// </summary>
        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        #endregion
    }

}
