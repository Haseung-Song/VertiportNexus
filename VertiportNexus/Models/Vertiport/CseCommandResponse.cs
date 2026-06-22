using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 응답 메시지 모델
    /// 
    /// [q.command.res] / [q.status.res]로 송신할
    /// Request-Response 응답 데이터를 표현한다.
    /// </summary>
    public class CseCommandResponse
    {
        #region [Properties]

        /// <summary>
        /// 인터페이스 식별자
        /// 
        /// 요청 메시지의 [interface_id]를 그대로 반환한다.
        /// </summary>
        [JsonPropertyName("interface_id")]
        public string InterfaceId { get; set; }

        /// <summary>
        /// 응답 메시지 타입
        /// 
        /// 예)
        /// ptz_move_res / get_state_res
        /// </summary>
        [JsonPropertyName("msg_type")]
        public string MsgType { get; set; }

        /// <summary>
        /// 요청 메시지 [ID]
        /// 
        /// 요청 / 응답 매칭에 사용한다.
        /// </summary>
        [JsonPropertyName("req_msg_id")]
        public string RequestMsgId { get; set; }

        /// <summary>
        /// 응답 메시지 [ID]
        /// </summary>
        [JsonPropertyName("msg_id")]
        public string MsgId { get; set; }

        /// <summary>
        /// 응답 생성 시간
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// 처리 성공 여부
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 결과 코드
        /// </summary>
        [JsonPropertyName("result_code")]
        public string ResultCode { get; set; }

        /// <summary>
        /// 결과 메시지
        /// </summary>
        [JsonPropertyName("result_message")]
        public string ResultMessage { get; set; }

        /// <summary>
        /// 응답 상세 데이터
        /// </summary>
        [JsonPropertyName("payload")]
        public CseCommandResponsePayload Payload { get; set; }

        #endregion
    }

}
