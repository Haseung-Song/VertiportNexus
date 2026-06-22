using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.Communication.MQ;

namespace VertiportNexus.Services.Vertiport
{
    /// <summary>
    /// [CSE] 명령 응답 송신 서비스
    /// 
    /// [CSE] 명령 처리 결과를 응답 [JSON]으로 생성하고,
    /// [q.command.res] / 
    /// [q.status.res] Queue로 송신한다.
    /// </summary>
    internal class CseCommandResponseService
    {
        #region [Fields]

        /// <summary>
        /// [MQ] 송신 서비스
        /// </summary>
        private readonly IMqSender _mqSender;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CseCommandResponseService] 생성자
        /// </summary>
        /// <param name="mqSender">
        /// [MQ] 송신 서비스
        /// </param>
        public CseCommandResponseService(
            IMqSender mqSender)
        {
            _mqSender =
                mqSender
                ?? throw new ArgumentNullException(
                    nameof(mqSender));
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Command] 성공 응답 송신
        /// 
        /// [카메라 제어] 요청에 대한 처리 성공 결과를
        /// [q.command.res] Queue로 송신한다.
        /// </summary>
        /// <param name="request">
        /// 원본 요청 메시지
        /// </param>
        /// <param name="message">
        /// 결과 메시지
        /// </param>
        public void SendCommandResponse(
            CseCommandMessage request,
            string message)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    "success",
                    message,
                    null);

            SendResponse(
                CseMqQueue.CommandResponse,
                response);
        }

        /// <summary>
        /// [Command] 실패 응답 송신
        /// 
        /// [카메라 제어] 요청 처리 중 오류가 발생한 경우
        /// [q.command.res] Queue로 실패 결과를 송신한다.
        /// </summary>
        /// <param name="request">
        /// 원본 요청 메시지
        /// </param>
        /// <param name="errorCode">
        /// 에러 코드
        /// </param>
        /// <param name="message">
        /// 에러 메시지
        /// </param>
        public void SendCommandErrorResponse(
            CseCommandMessage request,
            string errorCode,
            string message)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    "error",
                    message,
                    errorCode);

            SendResponse(
                CseMqQueue.CommandResponse,
                response);
        }

        /// <summary>
        /// [Status] 응답 송신
        /// 
        /// [카메라 상태 조회] 요청에 대한 처리 결과를
        /// [q.status.res] Queue로 송신한다.
        /// </summary>
        /// <param name="request">
        /// 원본 요청 메시지
        /// </param>
        /// <param name="payload">
        /// 상태 응답 [Payload]
        /// </param>
        public void SendStatusResponse(
            CseCommandMessage request,
            CseCommandResponsePayload payload)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    "success",
                    "OK",
                    null);

            response.Payload =
                payload;

            SendResponse(
                CseMqQueue.StatusResponse,
                response);
        }

        /// <summary>
        /// [Status] 실패 응답 송신
        /// 
        /// 상태 조회 요청 처리 중 오류가 발생한 경우
        /// [q.status.res] Queue로 실패 결과를 송신한다.
        /// </summary>
        /// <param name="request">
        /// 원본 요청 메시지
        /// </param>
        /// <param name="errorCode">
        /// 에러 코드
        /// </param>
        /// <param name="message">
        /// 에러 메시지
        /// </param>
        public void SendStatusErrorResponse(
            CseCommandMessage request,
            string errorCode,
            string message)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    "error",
                    message,
                    errorCode);

            SendResponse(
                CseMqQueue.StatusResponse,
                response);
        }

        #endregion

        #region [Create Methods]

        /// <summary>
        /// 기본 응답 메시지 생성
        /// </summary>
        /// <param name="request">
        /// 원본 요청 메시지
        /// </param>
        /// <param name="status">
        /// 처리 상태
        /// </param>
        /// <param name="message">
        /// 결과 메시지
        /// </param>
        /// <param name="errorCode">
        /// 에러 코드
        /// </param>
        /// <returns>
        /// 기본 응답 메시지
        /// </returns>
        private CseCommandResponse CreateBaseResponse(
            CseCommandMessage request,
            string status,
            string message,
            string errorCode)
        {
            return new CseCommandResponse
            {
                InterfaceId =
                    request?.InterfaceId,

                MsgType =
                    CreateResponseMsgType(
                        request?.MsgType),

                RequestMsgId =
                    request?.MsgId,

                MsgId =
                    Guid.NewGuid().ToString(),

                Timestamp =
                    DateTime.Now.ToString(
                        "yyyy-MM-ddTHH:mm:ss"),

                Status =
                    status,

                ErrorCode =
                    errorCode,

                Message =
                    message
            };

        }

        /// <summary>
        /// 응답 메시지 타입 생성
        /// 
        /// 요청 [msg_type] 뒤에 [_res]를 붙여
        /// 응답 메시지 타입으로 사용한다.
        /// </summary>
        /// <param name="requestMsgType">
        /// 요청 메시지 타입
        /// </param>
        /// <returns>
        /// 응답 메시지 타입
        /// </returns>
        private string CreateResponseMsgType(
            string requestMsgType)
        {
            if (string.IsNullOrWhiteSpace(
                requestMsgType))
            {
                return "response";
            }
            return requestMsgType + "_res";
        }

        #endregion

        #region [Send Methods]

        /// <summary>
        /// 응답 메시지 [JSON] 직렬화 후 [MQ] 송신
        /// </summary>
        /// <param name="queueName">
        /// 송신 [Queue] 이름
        /// </param>
        /// <param name="response">
        /// 응답 메시지
        /// </param>
        private void SendResponse(
            string queueName,
            CseCommandResponse response)
        {
            string json =
                JsonSerializer.Serialize(
                    response,
                    new JsonSerializerOptions
                    {
                        WriteIndented =
                            true,

                        DefaultIgnoreCondition =
                            JsonIgnoreCondition.WhenWritingNull
                    });

            _mqSender.Send(
                queueName,
                json);
        }
        #endregion
    }

}
