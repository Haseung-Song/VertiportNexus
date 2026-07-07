using System;
using System.Threading.Tasks;
using VertiportNexus.Services.Vertiport;
using VertiportNexus.Services.Communication.MQ;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [RabbitMQ] 통신 Controller
    /// 
    /// RabbitMQ 수신 시작 / 중지 기능만 담당한다.
    /// 화면 Binding 상태 변경은 수행하지 않고,
    /// 처리 결과만 [ControllerResult]로 반환한다.
    /// </summary>
    internal sealed class RabbitMqController
    {
        #region [Service Fields]

        /// <summary>
        /// [CSE] 명령 수신 서비스
        /// </summary>
        private readonly CseCommandReceiveService _cseCommandReceiveService;

        /// <summary>
        /// [CSE] 명령 처리 서비스
        /// </summary>
        private readonly CseCommandHandler _cseCommandHandler;

        /// <summary>
        /// [MQ] 수신 서비스
        /// </summary>
        private readonly IMqReceiver _mqReceiver;

        #endregion

        #region [Status Fields]

        /// <summary>
        /// [RabbitMQ] 수신 시작 여부
        /// </summary>
        private bool _isReceiveStarted;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RabbitMQ] 통신 Controller 생성
        /// </summary>
        internal RabbitMqController(
            CseCommandReceiveService cseCommandReceiveService,
            CseCommandHandler cseCommandHandler,
            IMqReceiver mqReceiver)
        {
            _cseCommandReceiveService =
                cseCommandReceiveService;

            _cseCommandHandler =
                cseCommandHandler;

            _mqReceiver =
                mqReceiver;
        }

        #endregion

        #region [RabbitMQ Methods]

        /// <summary>
        /// [RabbitMQ] 수신 시작
        /// </summary>
        internal async Task<ControllerResult> StartReceiveAsync()
        {
            if (_isReceiveStarted)
            {
                return ControllerResult.Failed(
                    "RabbitMQ Receive Already Started");
            }

            try
            {
                _isReceiveStarted =
                    true;

                await Task.Delay(
                    500);

                _cseCommandReceiveService
                    .StartReceive();

                return ControllerResult.Success(
                    "RabbitMQ Receive Started");
            }
            catch (Exception ex)
            {
                _isReceiveStarted =
                    false;

                return ControllerResult.Failed(
                    "RabbitMQ Receive Failed : " + ex.Message);
            }
        }

        /// <summary>
        /// [RabbitMQ] 수신 중지
        /// </summary>
        internal ControllerResult StopReceive()
        {
            if (!_isReceiveStarted)
            {
                return ControllerResult.Failed(
                    "RabbitMQ Receive Not Started");
            }

            try
            {
                _cseCommandHandler
                    .StopCameraStatusPublishService();

                _mqReceiver
                    .StopReceive();

                _isReceiveStarted =
                    false;

                return ControllerResult.Success(
                    "RabbitMQ Receive Stopped");
            }
            catch (Exception ex)
            {
                return ControllerResult.Failed(
                    "RabbitMQ Receive Stop Failed : " + ex.Message);
            }
        }

        #endregion
    }
}
