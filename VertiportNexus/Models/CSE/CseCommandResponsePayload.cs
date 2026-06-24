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
    /// [PTZ] 상태 조회 응답에서
    /// 
    /// 공통으로 사용한다.
    /// </summary>
    public class CseCommandResponsePayload
    {
        #region [State Properties]

        /// <summary>
        /// 카메라 연결 상태
        /// 
        /// [IF-GUIS-CSE-012] 상태 조회 응답에서
        /// 현재 카메라 제어 장비 연결 여부를 반환한다.
        /// </summary>
        [JsonPropertyName("connected")]
        public bool? Connected { get; set; }

        /// <summary>
        /// [PTZ] 상태 정보
        /// 
        /// [IF-GUIS-CSE-012] 상태 조회 응답에서
        /// 현재 [Pan] / [Tilt] / [Zoom] 상태값을 반환한다.
        /// </summary>
        [JsonPropertyName("ptz")]
        public CsePtzStatePayload Ptz { get; set; }

        /// <summary>
        /// 영상 반전 설정 여부
        /// 
        /// [IF-GUIS-CSE-012] 상태 조회 응답에서
        /// 현재 영상의 [Flip] 적용 상태를 반환한다.
        /// </summary>
        [JsonPropertyName("is_flipped")]
        public bool? IsFlipped { get; set; }

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
        /// 영상 밝기 설정값
        /// 
        /// [IF-GUIS-CSE-011] 설정 조회 응답에서
        /// 현재 영상 밝기 값을 반환한다.
        /// </summary>
        [JsonPropertyName("brightness")]
        public int? Brightness { get; set; }

        /// <summary>
        /// 영상 대비 설정값
        /// 
        /// [IF-GUIS-CSE-011] 설정 조회 응답에서
        /// 현재 영상 대비 값을 반환한다.
        /// </summary>
        [JsonPropertyName("contrast")]
        public int? Contrast { get; set; }

        /// <summary>
        /// [Focus] 모드
        /// 
        /// [IF-GUIS-CSE-011] 설정 조회 응답에서
        /// 현재 [Focus] 모드 값을 반환한다.
        /// 
        /// 예)
        /// AUTO
        /// MANUAL
        /// </summary>
        [JsonPropertyName("focus_mode")]
        public string FocusMode { get; set; }

        /// <summary>
        /// 영상 반전 설정 여부
        /// 
        /// [IF-GUIS-CSE-011] 설정 조회 응답에서
        /// 현재 영상의 [Flip] 적용 상태를 반환한다.
        /// </summary>
        [JsonPropertyName("flip")]
        public bool? Flip { get; set; }

        /// <summary>
        /// 탐지 활성화 여부
        /// 
        /// [IF-GUIS-CSE-011] 설정 조회 응답에서
        /// 현재 객체 탐지 기능의 활성화 상태를 반환한다.
        /// </summary>
        [JsonPropertyName("detect_enabled")]
        public bool? DetectEnabled { get; set; }

        #endregion
    }

}
