using System;
using VertiportNexus.Common;
using VertiportNexus.Models.Camera;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.Command;

namespace VertiportNexus.Services.Vertiport
{
    /// <summary>
    /// [CSE] 명령 처리 서비스
    /// 
    /// [MQ] / [JSON]으로 수신된 [CSE] 명령을 해석하고,
    /// [msg_type] 기준으로 실제 처리 로직을 분기한다.
    /// 
    /// 현재 단계에서는 명령 수신 / 파싱 / 분기 확인을 우선 수행하고,
    /// 이후 [ADS1000] 카메라 제어 서비스와 연결한다.
    /// </summary>
    internal class CseCommandHandler
    {
        #region [Fields]

        /// <summary>
        /// 내부 카메라 명령 처리 서비스
        /// </summary>
        private readonly CameraCommandService _cameraCommandService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CseCommandHandler] 생성자
        /// </summary>
        /// <param name="cameraCommandService">
        /// 내부 카메라 명령 처리 서비스
        /// </param>
        public CseCommandHandler(
            CameraCommandService cameraCommandService)
        {
            _cameraCommandService =
                cameraCommandService;
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [CSE] 명령 처리
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        public void HandleCommand(
            CseCommandMessage message)
        {
            if (message == null)
            {
                Console.WriteLine("[CSE][CMD] Handle Failed : Message is null");
                return;
            }

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[CSE][CMD] Handle Start");
            Console.WriteLine();
            Console.WriteLine("[CSE][CMD] MsgType : " + message.MsgType);
            Console.WriteLine("[CSE][CMD] MsgId : " + message.MsgId);

            switch (message.MsgType)
            {
                case CseCommandType.PtzMove:
                    HandlePtzMove(
                        message);
                    break;

                case CseCommandType.PtzStop:
                    HandlePtzStop(
                        message);
                    break;

                case CseCommandType.GetState:
                    HandleGetState(
                        message);
                    break;

                case CseCommandType.GetConfig:
                    HandleGetConfig(
                        message);
                    break;

                default:
                    Console.WriteLine("[CSE][CMD] Unsupported MsgType : " + message.MsgType);
                    break;
            }
            Console.WriteLine("[CSE][CMD] Handle End");
        }

        #endregion

        #region [PTZ Command Methods]

        /// <summary>
        /// [PTZ] 이동 명령 처리
        /// 
        /// [ICD] 기준 [ptz_move] 메시지를 처리한다.
        /// 
        /// 현재 단계에서는 [mode] / [pan] / [tilt] / [zoom] 값을 확인하고,
        /// 이후 [ADS1000] 제어 서비스 호출로 확장한다.
        /// </summary>
        /// <param name="message">
        /// [PTZ] 이동 명령 메시지
        /// </param>
        private void HandlePtzMove(
            CseCommandMessage message)
        {
            CseCommandPayload payload =
                message.Payload;

            if (payload == null)
            {
                Console.WriteLine("[CSE][CMD] PTZ Move Failed : Payload is null");
                return;
            }
            Console.WriteLine("[CSE][CMD] PTZ Move");
            Console.WriteLine("[CSE][CMD] Mode : " + payload.Mode);
            Console.WriteLine("[CSE][CMD] Pan : " + payload.Pan);
            Console.WriteLine("[CSE][CMD] Tilt : " + payload.Tilt);
            Console.WriteLine("[CSE][CMD] Zoom : " + payload.Zoom);
            Console.WriteLine();

            CameraCommand cameraCommand =
                new CameraCommand
                {
                    CommandType =
                        CameraCommandType.PtzMove,

                    Mode =
                        payload.Mode,

                    Pan =
                        payload.Pan,

                    Tilt =
                        payload.Tilt,

                    Zoom =
                        payload.Zoom,

                    SourceMsgId =
                        message.MsgId
                };
            _cameraCommandService.HandleCommand(
                cameraCommand);
        }

        /// <summary>
        /// [PTZ] 정지 명령 처리
        /// 
        /// [ICD] 기준 [ptz_stop] 메시지를 처리한다.
        /// </summary>
        /// <param name="message">
        /// [PTZ] 정지 명령 메시지
        /// </param>
        private void HandlePtzStop(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] PTZ Stop");
            Console.WriteLine("[CSE][CMD] Request MsgId : " + message.MsgId);

            CameraCommand cameraCommand =
                new CameraCommand
                {
                    CommandType =
                        CameraCommandType.PtzStop,

                    SourceMsgId =
                        message.MsgId
                };
            _cameraCommandService.HandleCommand(
                cameraCommand);
        }

        #endregion

        #region [Status Command Methods]

        /// <summary>
        /// 카메라 상태 조회 명령 처리
        /// 
        /// [ICD] 기준 [get_state] 메시지를 처리한다.
        /// </summary>
        /// <param name="message">
        /// 상태 조회 명령 메시지
        /// </param>
        private void HandleGetState(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Get State");
            Console.WriteLine("[CSE][CMD] ReplyTo : " + message.ReplyTo);
        }

        /// <summary>
        /// 카메라 설정 조회 명령 처리
        /// 
        /// [ICD] 기준 [get_conf] 메시지를 처리한다.
        /// </summary>
        /// <param name="message">
        /// 설정 조회 명령 메시지
        /// </param>
        private void HandleGetConfig(
            CseCommandMessage message)
        {
            Console.WriteLine("[CSE][CMD] Get Config");
            Console.WriteLine("[CSE][CMD] ReplyTo : " + message.ReplyTo);
        }
        #endregion
    }

}
