using System.Text.Json.Serialization;

namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 응답 메시지 모델
    /// 
    /// [CSE] 명령 처리 결과를
    /// [q.command.res] / [q.status.res] Queue로 송신하기 위한
    /// 응답 메시지 모델이다.
    /// </summary>
    public class CseCommandResponse
    {
        #region [Message Properties]

        /// <summary>
        /// 인터페이스 식별자
        /// 
        /// 처리한 요청에 대응되는
        /// [CSE → GUIS] 응답 Interface ID를 반환한다.
        /// </summary>
        [JsonPropertyName("interface_id")]
        public string InterfaceId { get; set; } =
            string.Empty;

        /// <summary>
        /// 응답 메시지 타입
        /// 
        /// 요청 [msg_type]에 대응되는 응답 타입을 반환한다.
        /// </summary>
        [JsonPropertyName("msg_type")]
        public string MsgType { get; set; } =
            string.Empty;

        /// <summary>
        /// 요청 메시지 [ID]
        /// 
        /// 요청 / 응답 매칭에 사용한다.
        /// </summary>
        [JsonPropertyName("req_msg_id")]
        public string RequestMsgId { get; set; } =
            string.Empty;

        /// <summary>
        /// 응답 메시지 [ID]
        /// </summary>
        [JsonPropertyName("msg_id")]
        public string MsgId { get; set; } =
            string.Empty;

        /// <summary>
        /// 응답 생성 시간
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } =
            string.Empty;

        #endregion

        #region [Result Properties]

        /// <summary>
        /// 처리 상태
        /// 
        /// 예)
        /// success
        /// error
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } =
            string.Empty;

        /// <summary>
        /// 에러 코드
        /// 
        /// [status]가 [error]인 경우
        /// 오류 원인을 구분하기 위해 사용한다.
        /// </summary>
        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; } =
            string.Empty;

        /// <summary>
        /// 결과 / 에러 메시지
        /// 
        /// 성공 또는 실패 사유를 설명한다.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } =
            string.Empty;

        #endregion

        #region [Payload Properties]

        /// <summary>
        /// 응답 상세 데이터
        /// 
        /// 상태 조회 등
        /// 추가 응답 데이터가 필요한 경우 사용한다.
        /// </summary>
        [JsonPropertyName("payload")]
        public CseCommandResponsePayload Payload { get; set; }

        #endregion
    }

}
