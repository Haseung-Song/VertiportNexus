using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 요청 메시지 모델
    /// 
    /// [GUIS]에서 [CSE]로 전달하는
    /// [q.command.req] / [q.status.req] [JSON] 메시지를 표현한다.
    /// </summary>
    public class CseCommandMessage
    {
        #region [Message Properties]

        /// <summary>
        /// 인터페이스 식별자
        /// 
        /// 최종 [GUIS → CSE] ICD 기준
        /// [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-005] 요청을 구분한다.
        /// </summary>
        [JsonPropertyName("interface_id")]
        public string InterfaceId { get; set; } =
            string.Empty;

        /// <summary>
        /// 메시지 타입
        /// 
        /// [interface_id]와 함께
        /// 실제 명령 처리 분기 기준으로 사용한다.
        /// 
        /// 예)
        /// detect_on
        /// detect_off
        /// detect_cont
        /// ptz_move
        /// get_state
        /// </summary>
        [JsonPropertyName("msg_type")]
        public string MsgType { get; set; } =
            string.Empty;

        /// <summary>
        /// 메시지 식별자
        /// 
        /// 요청 / 응답 매칭에 사용한다.
        /// </summary>
        [JsonPropertyName("msg_id")]
        public string MsgId { get; set; } =
            string.Empty;

        /// <summary>
        /// 메시지 생성 시간
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } =
            string.Empty;

        #endregion

        #region [Queue Properties]

        /// <summary>
        /// 응답 [Queue] 이름
        /// 
        /// 요청 처리 결과를 송신할
        /// 응답 Queue 이름을 지정한다.
        /// 
        /// 예)
        /// q.command.res
        /// q.status.res
        /// </summary>
        [JsonPropertyName("reply_to")]
        public string ReplyTo { get; set; } =
            string.Empty;

        #endregion

        #region [Payload Properties]

        /// <summary>
        /// 명령 상세 데이터
        /// 
        /// 명령 종류에 따라 필요한 값만 사용한다.
        /// </summary>
        [JsonPropertyName("payload")]
        public CseCommandPayload Payload { get; set; }

        #endregion
    }

}
