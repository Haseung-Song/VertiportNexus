using System;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.ViewModels.Main;
using VertiportNexus.ViewModels.Main.Composition;

namespace VertiportNexus.Features.Main.Communication
{
    /// <summary>
    /// [RabbitMQ] 수신 Workflow
    /// 
    /// [MainViewModel]에 직접 포함되어 있던
    /// RabbitMQ 수신 시작 / 중지 처리 흐름을 분리한다.
    /// 
    /// 화면 상태값 변경은 [MainViewModel]에서 수행하고,
    /// 본 Workflow는 RabbitMQ Controller 호출 및
    /// 예외 처리 결과 반환만 담당한다.
    /// </summary>
    internal sealed class RabbitMqReceiveWorkflow
    {
        #region [Fields]

        /// <summary>
        /// [MainViewModel] 구성 객체
        /// </summary>
        private readonly MainViewModelContext _context;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RabbitMQ] 수신 Workflow 생성자
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        internal RabbitMqReceiveWorkflow(
            MainViewModelContext context)
        {
            _context =
                context;
        }

        #endregion

        #region [Receive Methods]

        /// <summary>
        /// [RabbitMQ] 수신 시작
        /// </summary>
        /// <returns>
        /// Controller 처리 결과
        /// </returns>
        internal async Task<ControllerResult> StartAsync()
        {
            try
            {
                return await _context
                    .RabbitMqController
                    .StartReceiveAsync();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[CSE][MQ] Start Failed");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine();

                return ControllerResult.Failed(
                    "RabbitMQ Receive Failed : " + ex.Message);
            }

        }

        /// <summary>
        /// [RabbitMQ] 수신 중지
        /// </summary>
        /// <returns>
        /// Controller 처리 결과
        /// </returns>
        internal ControllerResult Stop()
        {
            try
            {
                return _context
                    .RabbitMqController
                    .StopReceive();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[CSE][MQ] Stop Failed");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine();

                return ControllerResult.Failed(
                    "RabbitMQ Receive Stop Failed : " + ex.Message);
            }

        }
        #endregion
    }

}
