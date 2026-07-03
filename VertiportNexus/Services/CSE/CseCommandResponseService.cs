using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using VertiportNexus.Common;
using VertiportNexus.Models.CSE;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.Camera;
using VertiportNexus.Services.Communication.MQ;

namespace VertiportNexus.Services.Vertiport
{
    /// <summary>
    /// [CSE] 명령 응답 송신 서비스
    /// 
    /// [CSE] 명령 처리 결과를 응답 [JSON]으로 생성하고,
    /// [q.command.res] / [q.status.res] Queue로 송신한다.
    /// </summary>
    internal class CseCommandResponseService
    {
        #region [Constants]

        /// <summary>
        /// [CSE] 응답 시간 문자열 형식
        /// </summary>
        private const string RESPONSE_TIMESTAMP_FORMAT =
            "yyyy-MM-ddTHH:mm:ss";

        /// <summary>
        /// [CSE] 성공 상태 문자열
        /// </summary>
        private const string STATUS_SUCCESS =
            "success";

        /// <summary>
        /// [CSE] 실패 상태 문자열
        /// </summary>
        private const string STATUS_ERROR =
            "error";

        #endregion

        #region [Fields]

        /// <summary>
        /// [MQ] 송신 서비스
        /// </summary>
        private readonly IMqSender _mqSender;

        /// <summary>
        /// [JSON] 직렬화 옵션
        /// </summary>
        private readonly JsonSerializerOptions _jsonSerializerOptions =
            new JsonSerializerOptions
            {
                WriteIndented =
                    true,

                DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull
            };

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
        public void SendCommandResponse(
            CseCommandMessage request,
            string message)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    STATUS_SUCCESS,
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
        public void SendCommandErrorResponse(
            CseCommandMessage request,
            string errorCode,
            string message)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    STATUS_ERROR,
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
        public void SendStatusResponse(
            CseCommandMessage request,
            CseCommandResponsePayload payload)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    STATUS_SUCCESS,
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
        public void SendStatusErrorResponse(
            CseCommandMessage request,
            string errorCode,
            string message)
        {
            CseCommandResponse response =
                CreateBaseResponse(
                    request,
                    STATUS_ERROR,
                    message,
                    errorCode);

            SendResponse(
                CseMqQueue.StatusResponse,
                response);
        }

        /// <summary>
        /// [카메라 상태] 응답 송신
        /// 
        /// 최종 ICD [IF-CSE-GUIS-005] 기준
        /// 현재 카메라 상태를 [q.status.res] Queue로 송신한다.
        /// </summary>
        /// <param name="cameraStateProvider">
        /// [Camera] 상태 저장 서비스
        /// </param>
        public void SendCameraStatusResponse(
            CameraStateProvider cameraStateProvider)
        {
            if (cameraStateProvider == null)
            {
                return;
            }

            object responseMessage =
                new
                {
                    interface_id =
                        CseInterfaceId.GetStateResponse,

                    msg_type =
                        CseCommandType.GetStateResponse,

                    msg_id =
                        Guid.NewGuid().ToString(),

                    timestamp =
                        DateTime.Now.ToString(
                            RESPONSE_TIMESTAMP_FORMAT),

                    status =
                        "success",

                    message =
                        "OK",

                    payload =
                        new
                        {
                            mode =
                                cameraStateProvider.PtzControlMode,

                            ptz =
                                new
                                {
                                    pan =
                                        Math.Round(
                                            Convert.ToDecimal(
                                                cameraStateProvider.CurrentPan ?? 0),
                                            4),

                                    tilt =
                                        Math.Round(
                                            Convert.ToDecimal(
                                                cameraStateProvider.CurrentTilt ?? 0),
                                            4),

                                    zoom =
                                        Math.Round(
                                            Convert.ToDecimal(
                                                cameraStateProvider.CurrentZoomRatio ?? 1.0),
                                            1)
                                }

                        }

                };

            string json =
                JsonSerializer.Serialize(
                    responseMessage,
                    _jsonSerializerOptions);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CSE][STATUS][SEND] Camera Status Response");
            Console.WriteLine("[CSE][STATUS][SEND] Queue : " + CseMqQueue.StatusResponse);
            Console.WriteLine(json);
            ConsoleLogHelper.PrintLine();

            _mqSender
                .Send(
                    CseMqQueue.StatusResponse,
                    json);
        }

        #endregion

        #region [Create Methods]

        /// <summary>
        /// 기본 응답 메시지 생성
        /// </summary>
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
                        RESPONSE_TIMESTAMP_FORMAT),

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
        private void SendResponse(
            string queueName,
            CseCommandResponse response)
        {
            try
            {
                string json =
                    JsonSerializer.Serialize(
                        response,
                        _jsonSerializerOptions);

                _mqSender.Send(
                    queueName,
                    json);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine("[CSE][RES] Response Send Failed");
                Console.WriteLine("[CSE][RES] Queue : " + queueName);
                Console.WriteLine("[CSE][RES] Error : " + ex.Message);

                ConsoleLogHelper.PrintLine();
            }

        }
        #endregion
    }

}
