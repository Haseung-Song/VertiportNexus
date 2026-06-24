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
        /// AUTO / MANUAL
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

        #endregion

        #region [Detect Properties]

        /// <summary>
        /// 탐지 항적 [ID]
        /// 
        /// [IF-GUIS-CSE-001] 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("track_id")]
        public int? TrackId { get; set; }

        /// <summary>
        /// 탐지 영상 [Frame ID]
        /// 
        /// [IF-GUIS-CSE-003] / [IF-GUIS-CSE-005]
        /// 탐지 객체 좌표 기준 영상 Frame을 구분한다.
        /// </summary>
        [JsonPropertyName("frame_id")]
        public int? FrameId { get; set; }

        /// <summary>
        /// 탐지 위치 [위도]
        /// 
        /// [IF-GUIS-CSE-001] 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        /// <summary>
        /// 탐지 위치 [경도]
        /// 
        /// [IF-GUIS-CSE-001] 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        /// <summary>
        /// 탐지 위치 [고도]
        /// 
        /// [IF-GUIS-CSE-001] 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("altitude")]
        public double? Altitude { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [X1]
        /// 
        /// 탐지 객체 Bounding Box의 좌측 상단 [X] 좌표이다.
        /// </summary>
        [JsonPropertyName("x1")]
        public double? X1 { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [Y1]
        /// 
        /// 탐지 객체 Bounding Box의 좌측 상단 [Y] 좌표이다.
        /// </summary>
        [JsonPropertyName("y1")]
        public double? Y1 { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [X2]
        /// 
        /// 탐지 객체 Bounding Box의 우측 하단 [X] 좌표이다.
        /// </summary>
        [JsonPropertyName("x2")]
        public double? X2 { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [Y2]
        /// 
        /// 탐지 객체 Bounding Box의 우측 하단 [Y] 좌표이다.
        /// </summary>
        [JsonPropertyName("y2")]
        public double? Y2 { get; set; }

        /// <summary>
        /// 객체 종류 [ID]
        /// 
        /// 탐지 객체의 Class 정보를 구분한다.
        /// </summary>
        [JsonPropertyName("class_id")]
        public int? ClassId { get; set; }

        /// <summary>
        /// 탐지 신뢰도
        /// 
        /// 탐지 객체의 Confidence 값을 보관한다.
        /// </summary>
        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        #endregion

        #region [Image Properties]

        /// <summary>
        /// 영상 밝기
        /// 
        /// [IF-GUIS-CSE-009] 영상 설정 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("brightness")]
        public int? Brightness { get; set; }

        /// <summary>
        /// 영상 대비
        /// 
        /// [IF-GUIS-CSE-009] 영상 설정 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("contrast")]
        public int? Contrast { get; set; }

        /// <summary>
        /// [Focus] 모드
        /// 
        /// [IF-GUIS-CSE-009] 영상 설정 요청에서 사용한다.
        /// 
        /// 예)
        /// AUTO
        /// MANUAL
        /// </summary>
        [JsonPropertyName("focus_mode")]
        public string FocusMode { get; set; }

        /// <summary>
        /// 영상 반전 여부
        /// 
        /// [IF-GUIS-CSE-010] 영상 플립 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("flip")]
        public bool? Flip { get; set; }

        #endregion
    }

}
