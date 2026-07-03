using VertiportNexus.Models.Vertiport;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - CSE Receive Event
    /// [RabbitMQ] 기반 CSE 명령 수신 이벤트를 처리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [CSE Receive Event Methods]

        /// <summary>
        /// [CSE] 명령 수신 처리
        /// 
        /// [MQ] 수신부에서 [JSON] 파싱이 완료된 명령을 전달받아,
        /// [CseCommandHandler]를 통해 실제 카메라 제어 명령으로 처리한다.
        /// </summary>
        /// <param name="message">
        /// [CSE] 명령 메시지
        /// </param>
        private void OnCseCommandReceived(
            CseCommandMessage message)
        {
            _cseCommandHandler.HandleCommand(
                message);
        }
        #endregion
    }

}
