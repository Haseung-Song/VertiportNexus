using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 요청 메시지 모델
    /// 
    /// [GUIS] / [VAP] 등 외부 시스템에서 [CSE]로 전달하는
    /// [q.command.req] / [q.status.req] [JSON] 메시지를 표현한다.
    /// </summary>
    public class CseCommandMessage
    {
        #region [Properties]

        /// <summary>
        /// 메시지 타입
        /// 
        /// 예)
        /// ptz_move
        /// ptz_stop
        /// get_state
        /// </summary>
        [JsonPropertyName("msg_type")]
        public string MsgType { get; set; }

        /// <summary>
        /// 메시지 식별자
        /// 
        /// 요청 / 응답 매칭에 사용한다.
        /// </summary>
        [JsonPropertyName("msg_id")]
        public string MsgId { get; set; }

        /// <summary>
        /// 메시지 생성 시간
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// 응답 [Queue] 이름
        /// 
        /// 예)
        /// q.command.res
        /// q.status.res
        /// </summary>
        [JsonPropertyName("reply_to")]
        public string ReplyTo { get; set; }

        /// <summary>
        /// 명령 상세 데이터
        /// </summary>
        [JsonPropertyName("payload")]
        public CseCommandPayload Payload { get; set; }

        #endregion
    }

}
