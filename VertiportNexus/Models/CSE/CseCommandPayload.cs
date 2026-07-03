using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 [Payload] 모델
    /// 
    /// [detect_on] / [detect_off] / [detect_conf] /
    /// [ptz_move] / [get_state] 메시지의
    /// [payload] 영역을 표현한다.
    /// 
    /// 명령별로 사용하는 필드가 다르므로,
    /// 선택 입력값은 nullable로 정의한다.
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
        /// AUTO
        /// MANUAL
        /// </summary>
        [JsonPropertyName("mode")]
        public string Mode { get; set; } =
            string.Empty;

        /// <summary>
        /// [PTZ] 제어 명령
        /// 
        /// [continuous] 모드에서
        /// 이동 방향 또는 정지 명령을 구분한다.
        /// 
        /// 예)
        /// stop
        /// left
        /// right
        /// up
        /// down
        /// left_up
        /// right_up
        /// left_down
        /// right_down
        /// </summary>
        [JsonPropertyName("command")]
        public string Command { get; set; } =
            string.Empty;

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
        /// [Zoom] 배율값
        /// 
        /// 외부 명령 기준의 실제 Zoom 배율값이다.
        /// 
        /// 예)
        /// 2.0 = 2배
        /// 33.0 = 33배
        /// 66.0 = 66배
        /// </summary>
        [JsonPropertyName("zoom")]
        public double? Zoom { get; set; }

        /// <summary>
        /// [Zoom] 위치값
        /// 
        /// ADS1000 장비 직접 제어 시 사용하는
        /// [0 ~ 1000] 범위의 Zoom Position 값이다.
        /// </summary>
        [JsonPropertyName("zoom_position")]
        public double? ZoomPosition { get; set; }

        #endregion

        #region [Status Properties]

        /// <summary>
        /// 상태 전송 주기
        /// 
        /// 카메라 상태 조회 요청에서 사용한다.
        /// 
        /// [0]이면 상태 송신을 중지하고,
        /// 그 외 값은 해당 주기로 상태 송신을 수행한다.
        /// </summary>
        [JsonPropertyName("frequency")]
        public int? Frequency { get; set; }

        #endregion

        #region [Detection Request Properties]

        /// <summary>
        /// 탐지 항적 [ID]
        /// 
        /// 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("track_id")]
        public int? TrackId { get; set; }

        /// <summary>
        /// 탐지 위치 [위도]
        /// 
        /// 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        /// <summary>
        /// 탐지 위치 [경도]
        /// 
        /// 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        /// <summary>
        /// 탐지 위치 [고도]
        /// 
        /// 탐지 활성화 요청에서 사용한다.
        /// </summary>
        [JsonPropertyName("altitude")]
        public double? Altitude { get; set; }

        #endregion

        #region [Detection Result Properties]

        /// <summary>
        /// 탐지 영상 [Frame ID]
        /// 
        /// 탐지 객체 좌표 기준 영상 Frame을 구분한다.
        /// </summary>
        [JsonPropertyName("frame_id")]
        public int? FrameId { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [X1]
        /// 
        /// Bounding Box 좌측 상단 [X] 좌표이다.
        /// </summary>
        [JsonPropertyName("x1")]
        public double? X1 { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [Y1]
        /// 
        /// Bounding Box 좌측 상단 [Y] 좌표이다.
        /// </summary>
        [JsonPropertyName("y1")]
        public double? Y1 { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [X2]
        /// 
        /// Bounding Box 우측 하단 [X] 좌표이다.
        /// </summary>
        [JsonPropertyName("x2")]
        public double? X2 { get; set; }

        /// <summary>
        /// 탐지 객체 화면 좌표 [Y2]
        /// 
        /// Bounding Box 우측 하단 [Y] 좌표이다.
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

        #region [Reserved Image Properties]

        /// <summary>
        /// 영상 밝기
        /// 
        /// 영상 설정 명령 확장 시 사용할 수 있는 예약 필드이다.
        /// </summary>
        [JsonPropertyName("brightness")]
        public int? Brightness { get; set; }

        /// <summary>
        /// 영상 대비
        /// 
        /// 영상 설정 명령 확장 시 사용할 수 있는 예약 필드이다.
        /// </summary>
        [JsonPropertyName("contrast")]
        public int? Contrast { get; set; }

        /// <summary>
        /// [Focus] 모드
        /// 
        /// 영상 설정 명령 확장 시 사용할 수 있는 예약 필드이다.
        /// 
        /// 예)
        /// AUTO
        /// MANUAL
        /// </summary>
        [JsonPropertyName("focus_mode")]
        public string FocusMode { get; set; } =
            string.Empty;

        /// <summary>
        /// 영상 반전 여부
        /// 
        /// 영상 설정 명령 확장 시 사용할 수 있는 예약 필드이다.
        /// </summary>
        [JsonPropertyName("flip")]
        public bool? Flip { get; set; }

        #endregion
    }

}
