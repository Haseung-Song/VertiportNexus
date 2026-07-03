using System;
using System.Threading.Tasks;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Radar UDP Communication
    /// [Radar] UDP 수신 시작 / 중지와 Mock 송신 테스트 흐름을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [UDP Connection Methods]

        /// <summary>
        /// [Radar] UDP Loopback 테스트
        /// 
        /// 실제 Radar 장비 연동 전,
        /// UDP Loopback 방식으로 Tracking Request / BIST Request를 송신하여
        /// Radar UDP 수신 / Packet 파싱 / 응답 생성 / ADS1000 제어 흐름을 검증한다.
        /// </summary>
        private async Task RunRadarUdpLoopbackTestAsync()
        {
            // [Radar] Tracking 테스트 지연
            //
            // 장비 연결 직후 바로 카메라가 움직이면
            // EO 영상 화면에서 이동 전 상태를 확인하기 어렵기 때문에,
            // 영상 연결 및 초기 화면 표시 시간을 확보한다.
            await Task.Delay(
                5000);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RADAR][UDP][MOCK] Loopback Tracking Test Start");
            ConsoleLogHelper.PrintLine();

            _radarUdpMockSenderService
                .SendTrackingRequest(
                    RadarUdpIpAddress,
                    RadarUdpLocalPort);

            // [Radar] BIST 테스트 지연
            //
            // Tracking Request 처리 및 Pan / Tilt 이동 로그 확인 후,
            // BIST Request 응답 흐름을 분리해서 확인하기 위해 대기한다.
            await Task.Delay(
                3000);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RADAR][UDP][MOCK] Loopback BIST Test Start");
            ConsoleLogHelper.PrintLine();

            _radarUdpMockSenderService
                .SendBistRequest(
                    RadarUdpIpAddress,
                    RadarUdpLocalPort);
        }

        /// <summary>
        /// [Radar] UDP 연결 상태 반영
        /// </summary>
        /// <param name="connectionState">
        /// [Radar] UDP 연결 상태
        /// </param>
        private void SetRadarUdpConnectionState(
            ConnectionState connectionState)
        {
            // [Radar UDP] 연결 상태 저장
            //
            // [Radar UDP] 수신 시작 / 중지 여부를
            // 내부 상태값에 반영한다.
            _radarUdpConnectionState =
                connectionState;

            // [Radar UDP] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(RadarUdpConnectionStatusText));
            OnPropertyChanged(nameof(RadarUdpConnectionStatusBrush));

            // [Radar UDP 수신 시작] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // [UDP START] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpStartButtonEnabled));

            // [Radar UDP 수신 중지] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // [UDP STOP] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpStopButtonEnabled));

            // [Radar UDP 통신 설정] 입력 가능 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [Radar UDP] 수신 상태에 따라
            // Radar UDP IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));
        }

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// 
        /// 화면에서 입력한 [Radar UDP Port]를 기준으로
        /// Radar Packet 수신을 시작한다.
        /// </summary>
        private async void StartRadarUdpReceive()
        {
            if (_mcbConnectionState != ConnectionState.Connected ||
                _scbConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Start Failed : MCB / SCB Not Connected");
                Console.WriteLine();

                return;
            }

            if (_radarUdpConnectionState == ConnectionState.Connected ||
                _radarUdpConnectionState == ConnectionState.Connecting)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Start Ignored : Already Started");
                Console.WriteLine();

                return;
            }

            try
            {
                SetRadarUdpConnectionState(
                    ConnectionState.Connecting);

                // [Radar UDP] 연결 상태 표시 지연
                //
                // UDP는 TCP처럼 연결 Handshake가 없기 때문에
                // 수신 시작 처리가 즉시 완료된다.
                // 화면에서 [Connecting] 상태가 너무 빠르게 지나가지 않도록
                // 짧은 표시 지연을 둔다.
                await Task.Delay(
                    500);

                _radarUdpService
                    .StartReceive(
                        RadarUdpLocalPort);

                SetRadarUdpConnectionState(
                    ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                SetRadarUdpConnectionState(
                    ConnectionState.Disconnected);

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Start Failed");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// 
        /// 현재 실행 중인 Radar UDP 수신을 중지한다.
        /// </summary>
        private void StopRadarUdpReceive()
        {
            if (_radarUdpConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Stop Ignored : Not Started");
                Console.WriteLine();

                return;
            }

            try
            {
                _radarUdpService
                    .StopReceive();

                SetRadarUdpConnectionState(
                    ConnectionState.Disconnected);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RADAR][UDP] Stop Failed");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

        }
        #endregion
    }

}
