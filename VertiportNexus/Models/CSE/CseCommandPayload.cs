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
        /// [PTZ] 제어 명령
        /// 
        /// [IF-GUIS-CSE-006] 기준
        /// [continuous] 모드에서 실제 이동 방향 또는
        /// 정지 명령을 구분한다.
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
        public string Command { get; set; }

        /// <summary>
        /// 상태 전송 주기
        /// 
        /// [IF-GUIS-CSE-005] 카메라 상태 조회 요청에서 사용한다.
        /// 
        /// 0이면 상태 송신을 중지하고,
        /// 그 외 값은 해당 주기로 상태 송신을 수행한다.
        /// 기본값은 [10Hz]로 처리한다.
        /// </summary>
        [JsonPropertyName("frequency")]
        public int? Frequency { get; set; }

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
        /// [IF-GUIS-CSE-006] 기준으로
        /// [Zoom] 값은 실제 배율 기준으로 수신한다.
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
        /// 기존 테스트 및 장비 직접 제어용 값이다.
        /// [0 ~ 1000] 범위의 ADS1000 Zoom Position 값으로 사용한다.
        /// 
        /// [IF-GUIS-CSE-006]에서 [zoom]은 배율값으로 사용하고,
        /// [zoom_position]은 장비 위치값 직접 제어용으로 사용한다.
        /// </summary>
        [JsonPropertyName("zoom_position")]
        public double? ZoomPosition { get; set; }

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
