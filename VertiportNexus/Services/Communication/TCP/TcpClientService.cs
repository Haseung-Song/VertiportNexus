using Serilog;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.Common.Logging;

namespace VertiportNexus.Services.Communication.TCP
{
    /// <summary>
    /// [MCB] / [SCB] 장비와 [TCP] [Client] 방식으로 통신하는 서비스
    /// 
    /// 역할:
    /// 1. [MCB] / [SCB] 장비에 [TCP] 연결
    /// 2. [ADS1000] 제어 [Packet] 송신
    /// 3. 장비에서 수신되는 데이터 처리 및 수신 이벤트 전달
    /// 4. [Disconnect] 시 [Socket] / [Stream] / [Token] 리소스 정리
    /// </summary>
    public class TcpClientService
    {
        #region [Fields]

        /// <summary>
        /// [TCP] 수신 Raw Packet 로그 저장 여부
        /// 
        /// MCB / SCB 상태 Packet은 짧은 주기로 반복 수신되므로
        /// 전체 수신 Packet을 모두 저장하지 않고,
        /// 1초 간격으로만 Raw Packet 로그를 저장한다.
        /// </summary>
        private static readonly bool ENABLE_TCP_RECEIVE_PACKET_LOG =
            true;

        /// <summary>
        /// [TCP] Packet 송신 동기화 객체
        ///
        /// UI / MQ / Radar / Tracking 등 여러 실행 흐름에서
        /// 동일한 [NetworkStream]으로 동시에 송신하지 않도록 보호한다.
        /// </summary>
        private readonly object _sendLock =
            new object();

        /// <summary>
        /// [TCP] [Client] 객체
        /// 
        /// [MCB] / [SCB] 장비에 접속하는 실제 [Socket] 객체
        /// </summary>
        private TcpClient _tcpClient;

        /// <summary>
        /// [TCP] 송수신 [Stream]
        /// 
        /// [Send] / [Receive] 모두 이 [Stream]을 통해 처리한다.
        /// </summary>
        private NetworkStream _networkStream;

        /// <summary>
        /// 수신 루프 종료 제어용 [Token]
        /// 
        /// [Disconnect] 시 [Cancel] 처리한다.
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// [Log] 표시용 장비 이름
        /// 
        /// 예)
        /// [MCB]
        /// [SCB]
        /// </summary>
        private readonly string _deviceName;

        /// <summary>
        /// 마지막 수신 [Log] 출력 시간 저장
        /// 
        /// 장비 상태 [Packet]이 반복 수신될 수 있으므로,
        /// [Console] 도배 방지를 위해 일정 시간 간격으로만
        /// [Log] 출력할 때 사용한다.
        /// </summary>
        private DateTime _lastRecvLogTime =
            DateTime.MinValue;

        #endregion

        #region [Events]

        /// <summary>
        /// 수신 데이터 전달 이벤트
        /// 
        /// [ViewModel] 또는 상위 서비스로 수신 [Packet]을 전달할 때 사용한다.
        /// </summary>
        public event Action<byte[], DateTime> MessageReceived;

        #endregion

        #region [Properties]

        /// <summary>
        /// [TCP] 연결 상태
        /// </summary>
        public bool IsConnected =>
            _tcpClient != null &&
            _tcpClient.Connected;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [TcpClientService] 생성자
        /// </summary>
        /// <param name="deviceName">
        /// [Log]에 표시할 장비 이름
        /// </param>
        public TcpClientService(
            string deviceName)
        {
            _deviceName =
                string.IsNullOrWhiteSpace(deviceName)
                    ? "TCP"
                    : deviceName;
        }

        #endregion

        #region [Connect Methods]

        /// <summary>
        /// 장비에 [TCP] [Client]로 접속
        /// 
        /// 연결 성공 시 [NetworkStream]을 생성하고,
        /// 백그라운드 [ReceiveLoop]를 시작한다.
        /// </summary>
        public async Task<bool> ConnectAsync(
            string ip,
            int port)
        {
            try
            {
                if (IsConnected)
                {
                    Log.Information(
                        "[TCP][{DeviceName}] Connect Ignored : Already Connected",
                        _deviceName);

                    return true;
                }

                LogSectionHelper.Information(
                    $"[TCP][{_deviceName}] CONNECT START");

                Log.Information(
                    "[TCP][{DeviceName}] Connect Try : {Ip}:{Port}",
                    _deviceName,
                    ip,
                    port);

                _tcpClient =
                    new TcpClient();

                await _tcpClient.ConnectAsync(
                    ip,
                    port);

                _networkStream =
                    _tcpClient.GetStream();

                _cts =
                    new CancellationTokenSource();

                _ = Task.Run(() =>
                    ReceiveLoopAsync(
                        _cts.Token));

                Log.Information(
                    "[TCP][{DeviceName}] Connect Success : {Ip}:{Port}",
                    _deviceName,
                    ip,
                    port);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[TCP][{DeviceName}] Connect Failed : {Ip}:{Port}",
                    _deviceName,
                    ip,
                    port);

                Disconnect();

                return false;
            }

        }

        #endregion

        #region [Send Methods]

        /// <summary>
        /// 장비로 byte[] [Packet] 송신
        /// 
        /// [ADS1000] [PacketBuilder]에서 생성한 제어 [Packet]을
        /// [NetworkStream]을 통해 장비로 전송한다.
        /// </summary>
        public bool Send(
            byte[] data)
        {
            try
            {
                lock (_sendLock)
                {
                    if (!CanSend())
                    {
                        Log.Warning(
                            "[TCP][{DeviceName}] Send Failed : Not Connected",
                            _deviceName);

                        return false;
                    }

                    _networkStream.Write(
                        data,
                        0,
                        data.Length);

                    _networkStream.Flush();

                    Log.Debug(
                        "[TCP][{DeviceName}] SEND {Hex}",
                        _deviceName,
                        ToHexString(
                            data));

                    return true;
                }

            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[TCP][{DeviceName}] Send Failed",
                    _deviceName);

                return false;
            }

        }

        /// <summary>
        /// 송신 가능 상태 확인
        /// </summary>
        private bool CanSend()
        {
            return IsConnected &&
                   _networkStream != null &&
                   _networkStream.CanWrite;
        }

        #endregion

        #region [Receive Methods]

        /// <summary>
        /// 장비에서 들어오는 데이터 수신 루프
        /// 
        /// [Disconnect] 요청 전까지 계속 [ReadAsync]를 수행하며,
        /// 수신된 데이터는 [Console] [Log] 및
        /// [MessageReceived] 이벤트로 전달한다.
        /// </summary>
        private async Task ReceiveLoopAsync(
            CancellationToken token)
        {
            byte[] buffer = new byte[2048];

            try
            {
                while (!token.IsCancellationRequested &&
                       IsConnected &&
                       _networkStream != null)
                {
                    int readSize =
                        await _networkStream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length);

                    if (readSize <= 0)
                    {
                        Log.Warning(
                            "[TCP][{DeviceName}] Server Disconnected",
                            _deviceName);

                        break;
                    }

                    byte[] receivedData =
                        CopyReceivedData(
                            buffer,
                            readSize);

                    PrintReceiveLogIfNeeded(
                        receivedData);

                    RaiseMessageReceived(
                        receivedData);
                }

            }
            catch (ObjectDisposedException)
            {
                Log.Information(
                    "[TCP][{DeviceName}] Receive Loop Closed",
                    _deviceName);
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[TCP][{DeviceName}] Receive Failed",
                    _deviceName);
            }

            Disconnect();
        }

        /// <summary>
        /// [수신 버퍼]에서 실제로 [수신된 크기]만큼만 복사
        /// </summary>
        private byte[] CopyReceivedData(
            byte[] buffer,
            int readSize)
        {
            byte[] receivedData =
                new byte[readSize];

            Array.Copy(
                buffer,
                receivedData,
                readSize);

            return receivedData;
        }

        /// <summary>
        /// 마지막 [Log] 출력 이후 1초 이상 지났을 경우,
        /// 수신 [Log]를 출력한다.
        /// </summary>
        private void PrintReceiveLogIfNeeded(
            byte[] receivedData)
        {
            if (!ENABLE_TCP_RECEIVE_PACKET_LOG)
            {
                return;
            }

            if ((DateTime.Now - _lastRecvLogTime).TotalSeconds < 1)
            {
                return;
            }

            Log.Debug(
                "[TCP][{DeviceName}] RECV {Hex}",
                _deviceName,
                ToHexString(
                    receivedData));

            _lastRecvLogTime =
                DateTime.Now;
        }

        /// <summary>
        /// [ViewModel] 또는 외부 구독자에게 수신 데이터 전달
        /// </summary>
        private void RaiseMessageReceived(
            byte[] receivedData)
        {
            MessageReceived?.Invoke(
                receivedData,
                DateTime.Now);
        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// [byte] 배열을 [HEX] 문자열로 변환
        /// </summary>
        private string ToHexString(
            byte[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                return string.Empty;
            }

            return BitConverter
                .ToString(
                    data)
                .Replace(
                    "-",
                    " ");
        }

        #endregion

        #region [Disconnect Methods]

        /// <summary>
        /// [TCP] 연결 해제 및 리소스 정리
        /// 
        /// 수신 루프 종료 요청 후,
        /// [NetworkStream] / [TcpClient] /
        /// [CancellationTokenSource]를 안전하게 정리한다.
        /// </summary>
        public void Disconnect()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            lock (_sendLock)
            {
                _networkStream?.Close();
                _networkStream?.Dispose();
                _networkStream = null;

                _tcpClient?.Close();
                _tcpClient?.Dispose();
                _tcpClient = null;
            }

            LogSectionHelper.Information(
                $"[TCP][{_deviceName}] DISCONNECT");

            Log.Information(
                "[TCP][{DeviceName}] Disconnected",
                _deviceName);

        }
        #endregion
    }

}
