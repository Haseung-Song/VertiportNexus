using System;
using System.Threading.Tasks;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - MQ Communication
    /// [RabbitMQ] 수신 시작 / 중지와 MQ 상태 갱신 로직을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [MQ Methods]

        /// <summary>
        /// [RabbitMQ] 연결 상태 반영
        /// </summary>
        /// <param name="connectionState">
        /// [RabbitMQ] 연결 상태
        /// </param>
        private void SetRabbitMqConnectionState(
            ConnectionState connectionState)
        {
            // [RabbitMQ] 연결 상태 저장
            //
            // [RabbitMQ] 수신 시작 / 중지 여부를
            // 내부 상태값에 반영한다.
            _rabbitMqConnectionState =
                connectionState;

            // [RabbitMQ] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(RabbitMqConnectionStatusText));
            OnPropertyChanged(nameof(RabbitMqConnectionStatusBrush));

            // [RabbitMQ 수신 시작] 버튼 활성화 상태 갱신
            //
            // [RabbitMQ] 수신 상태에 따라
            // [MQ START] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStartButtonEnabled));

            // [RabbitMQ 수신 중지] 버튼 활성화 상태 갱신
            //
            // [RabbitMQ] 수신 상태에 따라
            // [MQ STOP] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStopButtonEnabled));

            // [RabbitMQ 통신 설정] 입력 가능 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [RabbitMQ] 수신 상태에 따라
            // RabbitMQ Host / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));
        }

        /// <summary>
        /// [RabbitMQ] 수신 시작
        /// 
        /// 화면에서 입력한 [RabbitMQ Host] / [Port]를 기준으로
        /// CSE 명령 JSON 수신을 시작한다.
        /// </summary>
        private async void StartRabbitMqReceive()
        {
            if (_isCseMqReceiveStarted ||
                _rabbitMqConnectionState == ConnectionState.Connected ||
                _rabbitMqConnectionState == ConnectionState.Connecting)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[CSE][MQ] Start Ignored : Already Started");

                Console.WriteLine();

                return;
            }

            try
            {
                _isCseMqReceiveStarted =
                    true;

                SetRabbitMqConnectionState(
                    ConnectionState.Connecting);

                // [RabbitMQ] 연결 상태 표시 지연
                //
                // RabbitMQ 수신 시작 처리가 빠르게 완료되는 경우
                // 화면에서 [Connecting] 상태가 너무 빠르게 지나가지 않도록
                // 짧은 표시 지연을 둔다.
                await Task.Delay(
                    500);

                _cseCommandReceiveService
                    .StartReceive();

                SetRabbitMqConnectionState(
                    ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                _isCseMqReceiveStarted =
                    false;

                SetRabbitMqConnectionState(
                    ConnectionState.Disconnected);

                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[CSE][MQ] Start Failed");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine();
            }

        }

        /// <summary>
        /// [RabbitMQ] 수신 중지
        /// 
        /// 현재 실행 중인 RabbitMQ CSE 명령 수신을 중지한다.
        /// </summary>
        private void StopRabbitMqReceive()
        {
            if (_rabbitMqConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[CSE][MQ] Stop Ignored : Not Started");
                Console.WriteLine();

                return;
            }

            try
            {
                // [카메라 상태] 주기 송신 중지
                //
                // RabbitMQ 수신 중지 시,
                // 실행 중인 [q.status.res] 상태 송신 Loop도 함께 종료한다.
                _cseCommandHandler
                    .StopCameraStatusPublishService();

                _mqReceiver
                    .StopReceive();

                _isCseMqReceiveStarted =
                    false;

                SetRabbitMqConnectionState(
                    ConnectionState.Disconnected);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[CSE][MQ] Stop Failed");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

        }
        #endregion
    }

}
