using System;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - ADS1000 Connection
    /// [MCB] / [SCB] 장비 연결 / 해제와 Home Position 자동 수행 흐름을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [TCP Connection Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결
        /// </summary>
        private async Task ConnectDevicesAsync()
        {
            // 장비 연결 진행 중이면 중복 연결 방지
            if (_isDeviceConnecting)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Connect Ignored : Connecting");
                Console.WriteLine();

                return;
            }

            // 이미 [MCB] / [SCB] 중 하나라도 연결되어 있으면 중복 연결 방지
            if (_mcbConnectionState == ConnectionState.Connected ||
                _scbConnectionState == ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Connect Ignored : Already Connected");
                Console.WriteLine();

                return;
            }

            try
            {
                MainStatusText =
                     "MCB / SCB CONNECTING...";

                OperationModeText =
                    "DEVICE CONNECTING...";

                _isDeviceConnecting =
                    true;

                // [장비 연결 / 해제 버튼] 활성화 상태 갱신
                //
                // 연결 시도 중에는 중복 연결 / 해제 요청을 방지하기 위해
                // [장비 연결] / [연결 해제] 버튼을 비활성화한다.
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [장비 통신 설정] 입력 가능 상태 갱신
                //
                // [MCB] / [SCB] 연결 상태 변경에 따라
                // IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));

                // [Radar UDP 통신 설정] 입력 가능 상태 갱신
                //
                // 장비 연결 시도 종료 후
                // [MCB] / [SCB] 연결 상태에 따라
                // Radar UDP IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));

                // [RabbitMQ 통신 설정] 입력 가능 상태 갱신
                //
                // 장비 연결 시도 종료 후
                // [MCB] / [SCB] 연결 상태에 따라
                // RabbitMQ Host / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));

                // [MCB] / [SCB] 연결 시도 상태 표시
                SetDeviceConnectionState(
                    ConnectionState.Connecting,
                    ConnectionState.Connecting);

                Ads1000ConnectionResult connectionResult =
                    await _ads1000ConnectionService.ConnectAsync(
                        McbIpAddress,
                        McbPort,
                        ScbIpAddress,
                        ScbPort);

                ApplyDeviceConnectionResult(
                    connectionResult);

                // [EO] 영상 연결 처리
                //
                // [MCB] / [SCB] 중 하나 이상 연결된 경우에만
                // [EO] RTSP 영상을 활성화한다.
                //
                // 장비 제어 연결이 모두 실패한 경우에는
                // 영상 표시를 차단하고 화면을 초기화한다.
                if (_mcbConnectionState == ConnectionState.Connected ||
                    _scbConnectionState == ConnectionState.Connected)
                {
                    _isEoVideoDisplayEnabled =
                        true;

                    OperationModeText =
                        "CAMERA CONNECTING...";

                    _eoCameraService.Connect(
                        DEFAULT_EO_RTSP_ADDRESS);
                }
                else
                {
                    _isEoVideoDisplayEnabled =
                        false;

                    _eoCameraService.Disconnect();

                    EOCameraImage =
                        null;
                }

                if (_mcbConnectionState == ConnectionState.Connected &&
                    _scbConnectionState == ConnectionState.Connected)
                {
                    // [장비 연결 후] EO RTSP 연결 성공 대기 및 Home Position 이동
                    //
                    // 장비 전원 직후 EO Camera가 아직 Ready 상태가 아닐 수 있으므로,
                    // RTSP 연결 성공 상태를 확인한 뒤 Home Position 명령을 송신한다.
                    await WaitEoRtspConnectedAndMoveHomePositionAsync();
                }

            }
            finally
            {
                _isDeviceConnecting =
                    false;

                // [장비 연결 / 해제 버튼] 활성화 상태 갱신
                //
                // 연결 시도 종료 후
                // 현재 연결 상태에 따라 [장비 연결] / [연결 해제] 버튼 활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [장비 통신 설정] 입력 가능 상태 갱신
                //
                // [MCB] / [SCB] 연결 상태 변경에 따라
                // IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));
            }

        }

        /// <summary>
        /// [장비 연결 후] EO RTSP 연결 성공 대기 및 Home Position 이동
        /// 
        /// 장비 전원 직후 EO Camera가 Ready 상태가 아닐 수 있으므로,
        /// EO RTSP 연결 성공 여부를 일정 시간 대기한 뒤
        /// 연결 성공 시 Home Position 명령을 송신한다.
        /// 
        /// RTSP 연결 실패 상태에서는 Home Position 명령을 송신하지 않는다.
        /// </summary>
        private async Task WaitEoRtspConnectedAndMoveHomePositionAsync()
        {
            const int CHECK_DELAY_MS =
                200;

            const int MAX_WAIT_MS =
                65000;

            int elapsedMs =
                0;

            Console.WriteLine(
                "[EO CAMERA] RTSP Connected Wait Start");

            ConsoleLogHelper.PrintLine();

            while (_isEoVideoDisplayEnabled &&
                   !_isEoRtspConnected &&
                   elapsedMs < MAX_WAIT_MS)
            {
                await Task.Delay(
                    CHECK_DELAY_MS);

                elapsedMs +=
                    CHECK_DELAY_MS;
            }

            if (!_isEoVideoDisplayEnabled)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Connected Wait Canceled : Display Disabled");

                ConsoleLogHelper.PrintLine();

                return;
            }

            if (!_isEoRtspConnected)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Connected Wait Failed : Timeout");

                Console.WriteLine(
                    "[DEVICE] Home Position After Connect Skipped : EO RTSP Not Connected");

                ConsoleLogHelper.PrintLine();

                return;
            }

            Console.WriteLine(
                "[EO CAMERA] RTSP Connected Wait Complete");

            ConsoleLogHelper.PrintLine();

            await MoveHomePositionAfterDeviceConnectedAsync();
        }

        /// <summary>
        /// [장비 연결 후] Home Position 이동
        /// 
        /// [MCB] / [SCB] 장비 연결이 완료되면
        /// 장비 기준 Home Position 상태에서 운용을 시작할 수 있도록
        /// Pan Home / Tilt Home 명령을 자동 송신한다.
        /// 
        /// EO 영상 연결 시도 후 Home 이동 과정을 확인할 수 있도록
        /// 짧은 대기 후 Home Position 이동을 수행한다.
        /// </summary>
        private async Task MoveHomePositionAfterDeviceConnectedAsync()
        {
            // [EO 영상 표시 대기]
            //
            // 장비 연결 직후 바로 Home Position 명령을 송신하면
            // 영상이 표시되기 전에 장비가 이동할 수 있다.
            //
            // 사용자가 화면으로 현재 방향과 Home 이동 과정을 확인할 수 있도록
            // EO 영상 연결 시도 후 짧은 대기 시간을 둔다.
            await Task.Delay(
                300);

            await MoveHomePositionWithControlLockAsync(
                "[DEVICE] Home Position After Connect");
        }

        /// <summary>
        /// [Home Position] 이동 상태 반영
        /// 
        /// Home Position 이동 진행 여부를 저장하고,
        /// 장비 연결 버튼 및 장비 제어 탭 활성 / 비활성 상태를 갱신한다.
        /// </summary>
        /// <param name="isMoving">
        /// Home Position 이동 진행 여부
        /// </param>
        private void SetHomePositionMovingState(
            bool isMoving)
        {
            _isHomePositionMoving =
                isMoving;

            // [장비 연결 버튼] 활성화 상태 갱신
            //
            // Home Position 이동 중에는
            // [장비 연결] 버튼이 비활성화되도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

            // [장비 연결 해제 버튼] 활성화 상태 갱신
            //
            // Home Position 이동 중에는
            // 장비 내부 Home Script 실행 상태를 보호하기 위해
            // [연결 해제] 버튼이 비활성화되도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));

            // [장비 제어 탭] 활성화 상태 갱신
            //
            // Home Position 이동 중에는
            // 통신 설정 / 운용 제어 / 이동 제어 탭이 비활성화되도록 갱신한다.
            OnPropertyChanged(nameof(IsDeviceControlTabEnabled));

            // [Pan / Tilt Speed] 설정 가능 상태 갱신
            //
            // Home Position 이동 중에는
            // Pan / Tilt Speed 설정을 변경하지 못하도록 갱신한다.
            OnPropertyChanged(nameof(IsPanTiltSpeedEnabled));
        }

        /// <summary>
        /// [MCB] / [SCB] 연결 상태 반영
        /// </summary>
        /// <param name="mcbConnectionState">
        /// [MCB] 연결 상태
        /// </param>
        /// <param name="scbConnectionState">
        /// [SCB] 연결 상태
        /// </param>
        private void SetDeviceConnectionState(
            ConnectionState mcbConnectionState,
            ConnectionState scbConnectionState)
        {
            // [MCB] 연결 상태 저장
            //
            // [MCB] 연결 여부를
            // 내부 상태값에 반영한다.
            _mcbConnectionState =
                mcbConnectionState;

            // [SCB] 연결 상태 저장
            //
            // [SCB] 연결 여부를
            // 내부 상태값에 반영한다.
            _scbConnectionState =
                scbConnectionState;

            // [MCB] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(McbConnectionStatusText));
            OnPropertyChanged(nameof(McbConnectionStatusBrush));

            // [SCB] 연결 상태 UI 갱신
            //
            // 연결 상태 텍스트 및
            // 상태 표시 색상을 갱신한다.
            OnPropertyChanged(nameof(ScbConnectionStatusText));
            OnPropertyChanged(nameof(ScbConnectionStatusBrush));

            // [장비 제어] 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // 화면 제어 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceControlEnabled));

            // [장비 통신 설정] 입력 가능 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceConnectionSettingEnabled));

            // [장비 제어 탭] 활성화 상태 갱신
            //
            // Home Position 이동 여부 및 연결 상태 변경에 따라
            // 장비 제어 관련 탭 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceControlTabEnabled));

            // [Pan / Tilt Speed] 설정 가능 상태 갱신
            //
            // [MCB] 연결 상태 및 Home Position 이동 상태에 따라
            // Pan / Tilt Speed 슬라이더 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsPanTiltSpeedEnabled));

            // [장비 연결] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // 중복 연결 요청 가능 여부를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

            // [장비 연결 해제 버튼] 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 변경에 따라
            // [연결 해제] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));

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
            // [Radar UDP] 수신 상태 변경에 따라
            // Radar UDP IP / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRadarUdpConnectionSettingEnabled));

            // [RabbitMQ 수신 시작] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [RabbitMQ] 수신 상태에 따라
            // [MQ START] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStartButtonEnabled));

            // [RabbitMQ 수신 중지] 버튼 활성화 상태 갱신
            //
            // [MCB] / [SCB] 연결 상태 및
            // [RabbitMQ] 수신 상태에 따라
            // [MQ STOP] 버튼 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqStopButtonEnabled));

            // [RabbitMQ 통신 설정] 입력 가능 상태 갱신
            //
            // [RabbitMQ] 수신 상태 변경에 따라
            // RabbitMQ Host / Port 입력칸 활성 / 비활성 상태를 갱신한다.
            OnPropertyChanged(nameof(IsRabbitMqConnectionSettingEnabled));
        }

        /// <summary>
        /// [ADS1000] 장비 연결 상태 변경 처리
        /// 
        /// [MCB] / [SCB] 연결 시도 결과를
        /// 장비별로 화면에 즉시 반영한다.
        /// </summary>
        /// <param name="isMcbConnected">
        /// [MCB] 연결 성공 여부
        /// </param>
        /// <param name="isScbConnected">
        /// [SCB] 연결 성공 여부
        /// </param>
        private void OnAds1000ConnectionStateChanged(
            bool? isMcbConnected,
            bool? isScbConnected)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ConnectionState mcbConnectionState =
                    isMcbConnected.HasValue
                        ? isMcbConnected.Value
                            ? ConnectionState.Connected
                            : ConnectionState.Disconnected
                        : _mcbConnectionState;

                ConnectionState scbConnectionState =
                    isScbConnected.HasValue
                        ? isScbConnected.Value
                            ? ConnectionState.Connected
                            : ConnectionState.Disconnected
                        : _scbConnectionState;

                SetDeviceConnectionState(
                    mcbConnectionState,
                    scbConnectionState);
            }));

        }

        /// <summary>
        /// [MCB] / [SCB] 연결 결과 화면 반영
        /// </summary>
        /// <param name="connectionResult">
        /// [ADS1000] 장비 연결 결과
        /// </param>
        private void ApplyDeviceConnectionResult(
            Ads1000ConnectionResult connectionResult)
        {
            if (connectionResult.IsMcbConnected &&
                connectionResult.IsScbConnected)
            {
                MainStatusText =
                    "MCB / SCB CONNECTED";

                OperationModeText =
                    "ADS1000 CONTROL";
            }
            else if (connectionResult.IsMcbConnected)
            {
                MainStatusText =
                    "MCB ONLY CONNECTED";

                OperationModeText =
                    "MCB ONLY";
            }
            else if (connectionResult.IsScbConnected)
            {
                MainStatusText =
                    "SCB ONLY CONNECTED";

                OperationModeText =
                    "SCB ONLY";
            }
            else
            {
                MainStatusText =
                    "MCB / SCB DISCONNECTED";

                OperationModeText =
                    "CONNECT FAILED";
            }

            SetDeviceConnectionState(
                connectionResult.IsMcbConnected
                    ? ConnectionState.Connected
                    : ConnectionState.Disconnected,
                connectionResult.IsScbConnected
                    ? ConnectionState.Connected
                    : ConnectionState.Disconnected);

            // [Camera] 연결 상태 저장
            //
            // [CSE] [Get PTZ State] 응답에서 사용할 수 있도록
            // [MCB] / [SCB] 중 하나 이상 연결된 경우 연결 상태로 판단한다.
            _cameraStateProvider.UpdateConnectionState(
                connectionResult.IsMcbConnected ||
                connectionResult.IsScbConnected);
        }

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결 해제
        /// </summary>
        private Task DisconnectDevicesAsync()
        {
            if (_isDeviceDisconnecting)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Disconnect Ignored : Disconnecting");
                Console.WriteLine();

                return Task.CompletedTask;
            }

            // 이미 연결 해제 상태이면 중복 해제 방지
            if (_mcbConnectionState == ConnectionState.Disconnected &&
                _scbConnectionState == ConnectionState.Disconnected)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[DEVICE] Disconnect Ignored : Already Disconnected");
                Console.WriteLine();

                return Task.CompletedTask;
            }

            try
            {
                _isDeviceDisconnecting =
                    true;

                // [장비 연결 해제 버튼] 활성화 상태 갱신
                //
                // 연결 해제 처리 중에는 중복 연결 해제 요청을 방지하기 위해
                // [연결 해제] 버튼을 비활성화한다.
                OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));

                // [장비 연결 버튼] 활성화 상태 갱신
                //
                // 연결 해제 처리 중에는 중복 연결 요청을 방지하기 위해
                // [장비 연결] 버튼 상태를 갱신한다.
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [Radar UDP] 수신 중지
                //
                // [MCB] / [SCB] 장비 연결 해제 시,
                // Radar UDP 수신 상태가 Connected로 남지 않도록
                // 실행 중인 UDP 수신을 먼저 중지한다.
                if (_radarUdpConnectionState == ConnectionState.Connected)
                {
                    _radarUdpService
                        .StopReceive();

                    SetRadarUdpConnectionState(
                        ConnectionState.Disconnected);
                }

                // [RabbitMQ] 수신 중지
                //
                // [MCB] / [SCB] 장비 연결 해제 시,
                // RabbitMQ 수신 상태가 Connected로 남지 않도록
                // 실행 중인 MQ 수신을 먼저 중지한다.
                if (_rabbitMqConnectionState == ConnectionState.Connected)
                {
                    // [카메라 상태] 주기 송신 중지
                    //
                    // 장비 연결 해제 시,
                    // 실행 중인 [q.status.res] 상태 송신 Loop를 함께 종료한다.
                    _cseCommandHandler
                        .StopCameraStatusPublishService();

                    _mqReceiver
                        .StopReceive();

                    _isCseMqReceiveStarted =
                        false;

                    SetRabbitMqConnectionState(
                        ConnectionState.Disconnected);
                }

                _ads1000ConnectionService.Disconnect();

                MainStatusText =
                    "MCB / SCB DISCONNECTED";

                OperationModeText =
                    "MODE STANDBY";

                // 장비 연결 해제 상태 반영
                SetDeviceConnectionState(
                    ConnectionState.Disconnected,
                    ConnectionState.Disconnected);

                // [Camera] 연결 상태 저장
                //
                // 연결 해제 시 [CSE] 상태 조회 응답에서
                // 미연결 상태로 반환될 수 있도록 갱신한다.
                _cameraStateProvider.UpdateConnectionState(
                    false);

                // [EO] RTSP 재연결 중지
                StopEoRtspReconnect();

                // [EO] RTSP 연결 상태 초기화
                _isEoRtspConnected =
                    false;

                // [EO] 영상 표시 차단
                _isEoVideoDisplayEnabled =
                    false;

                // [EO] [RTSP] 테스트 영상 연결 해제
                _eoCameraService.Disconnect();

                // [EO] 영상 화면 초기화
                EOCameraImage = null;
            }
            finally
            {
                _isDeviceDisconnecting =
                    false;

                // [장비 연결 버튼] 활성화 상태 갱신
                OnPropertyChanged(nameof(IsDeviceConnectButtonEnabled));

                // [장비 연결 해제 버튼] 활성화 상태 갱신
                OnPropertyChanged(nameof(IsDeviceDisconnectButtonEnabled));
            }
            return Task.CompletedTask;
        }
        #endregion
    }

}
