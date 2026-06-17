using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.Common;

namespace VertiportNexus.Services.Communication.UDP
{
    /// <summary>
    /// [UDP] [Client] 방식으로 통신하는 서비스
    /// 
    /// 역할:
    /// 1. 지정한 [IP] / [Port] 대상으로 [UDP Packet] 송신
    /// 2. 지정한 [Local Port] 기준으로 [UDP Packet] 수신
    /// 3. 수신된 데이터 처리 및 [Console] [Log] 출력
    /// 4. [Disconnect] / [Dispose] 시 [Socket] / [Token] 리소스 정리
    /// </summary>
    public class UdpClientService : IDisposable
    {
        #region [Fields]

        /// <summary>
        /// [UDP] [Client] 객체
        /// 
        /// [UDP] [Packet] 송신 / 수신에 사용하는 실제 [Socket] 객체
        /// </summary>
        private UdpClient _udpClient;

        /// <summary>
        /// 수신 루프 종료 제어용 [Token]
        /// 
        /// [StopReceive] / [Dispose] 시 [Cancel] 처리한다.
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// [Log] 표시용 장비 이름
        /// 
        /// 예)
        /// [UDP]
        /// [MCB UDP]
        /// [SCB UDP]
        /// </summary>
        private readonly string _deviceName;

        /// <summary>
        /// 현재 [UDP] 수신 루프 실행 여부
        /// </summary>
        private bool _isReceiving;

        /// <summary>
        /// 현재 [UDP] 수신에 바인딩된 [Local Port]
        /// </summary>
        private int _localPort;

        /// <summary>
        /// 마지막 수신 [Log] 출력 시간 저장
        /// 
        /// 장비 상태 [Packet]이 반복 수신될 수 있으므로,
        /// [Console] 도배 방지를 위해 일정 시간 간격으로만
        /// [Log] 출력할 때 사용한다.
        /// </summary>
        private DateTime _lastRecvLogTime =
            DateTime.MinValue;

        /// <summary>
        /// 리소스 해제 여부
        /// </summary>
        private bool _isDisposed;

        #endregion

        #region [Events]

        /// <summary>
        /// 수신 데이터 전달 이벤트
        /// 
        /// [ViewModel] 또는 상위 서비스에서 수신 [Packet]을 받고 싶을 때 사용한다.
        /// </summary>
        public event Action<byte[], IPEndPoint> MessageReceived;

        #endregion

        #region [Properties]

        /// <summary>
        /// [UDP] 수신 루프 실행 상태
        /// </summary>
        public bool IsReceiving =>
            _isReceiving;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [UdpClientService] 생성자
        /// </summary>
        public UdpClientService()
            : this("UDP")
        {
        }

        /// <summary>
        /// [UdpClientService] 생성자
        /// </summary>
        /// <param name="deviceName">
        /// [Log]에 표시할 장비 이름
        /// </param>
        public UdpClientService(
            string deviceName)
        {
            _deviceName =
                deviceName;

            _udpClient =
                new UdpClient();
        }

        #endregion

        #region [Send]

        /// <summary>
        /// 장비로 byte[] [Packet] 송신
        /// 
        /// 지정한 [IP] / [Port] 대상으로 [byte[]] 데이터를 전송한다.
        /// </summary>
        public async Task<bool> SendAsync(
            string ip,
            int port,
            byte[] data)
        {
            try
            {
                if (!CanSend(
                    ip,
                    port,
                    data))
                {
                    return false;
                }

                IPEndPoint endPoint =
                    new IPEndPoint(
                        IPAddress.Parse(ip),
                        port);

                int sendSize =
                    await _udpClient.SendAsync(
                        data,
                        data.Length,
                        endPoint);

                PrintHexData(
                    "[UDP][" + _deviceName + "] SEND",
                    data);

                Console.WriteLine("[UDP][" + _deviceName + "] Target : " + ip + ":" + port);

                return sendSize == data.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UDP][" + _deviceName + "] Send Failed : " + ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 송신 가능 상태 확인
        /// </summary>
        private bool CanSend(
            string ip,
            int port,
            byte[] data)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                Console.WriteLine("[UDP][" + _deviceName + "] Send Failed : IP Address is empty");
                return false;
            }

            if (port <= 0 ||
                port > 65535)
            {
                Console.WriteLine("[UDP][" + _deviceName + "] Send Failed : Invalid Port");
                Console.WriteLine("[UDP][" + _deviceName + "] Port : " + port);
                return false;
            }

            if (data == null ||
                data.Length == 0)
            {
                Console.WriteLine("[UDP][" + _deviceName + "] Send Failed : Packet is empty");
                return false;
            }

            if (_udpClient == null)
            {
                _udpClient =
                    new UdpClient();
            }
            return true;
        }

        #endregion

        #region [Receive]

        /// <summary>
        /// [UDP] 수신 시작
        /// 
        /// 지정한 [Local Port]에 바인딩하고,
        /// 백그라운드 [ReceiveLoop]를 시작한다.
        /// </summary>
        public bool StartReceive(
            int localPort)
        {
            try
            {
                if (_isReceiving)
                {
                    Console.WriteLine("[UDP][" + _deviceName + "] Receive Already Running");
                    return true;
                }

                if (localPort <= 0 ||
                    localPort > 65535)
                {
                    Console.WriteLine("[UDP][" + _deviceName + "] Receive Start Failed : Invalid Local Port");
                    Console.WriteLine("[UDP][" + _deviceName + "] Local Port : " + localPort);

                    return false;
                }

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[UDP][" + _deviceName + "] Receive Start");
                Console.WriteLine();
                Console.WriteLine("[UDP][" + _deviceName + "] Local Port : " + localPort);
                Console.WriteLine();

                _localPort =
                    localPort;

                DisconnectClientOnly();

                _udpClient =
                    new UdpClient(
                        _localPort);

                _cts =
                    new CancellationTokenSource();

                _isReceiving =
                    true;

                _ = Task.Run(() =>
                    ReceiveLoopAsync(
                        _cts.Token));

                Console.WriteLine("[UDP][" + _deviceName + "] Receive Ready");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UDP][" + _deviceName + "] Receive Start Failed : " + ex.Message);

                StopReceive();

                return false;
            }

        }

        /// <summary>
        /// [UDP] 수신 중지
        /// </summary>
        public void StopReceive()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            DisconnectClientOnly();

            _isReceiving =
                false;

            Console.WriteLine("[UDP][" + _deviceName + "] Receive Stopped");
        }

        /// <summary>
        /// 장비에서 들어오는 데이터 수신 루프
        /// 
        /// [StopReceive] 요청 전까지 계속 [ReceiveAsync]를 수행하며,
        /// 수신된 데이터는 [Console] [Log] 및
        /// [MessageReceived] 이벤트로 전달한다.
        /// </summary>
        private async Task ReceiveLoopAsync(
            CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested &&
                       _udpClient != null)
                {
                    UdpReceiveResult result =
                        await _udpClient.ReceiveAsync();

                    if (token.IsCancellationRequested)
                        break;

                    byte[] receivedData =
                        result.Buffer;

                    PrintReceiveLogIfNeeded(
                        receivedData,
                        result.RemoteEndPoint);

                    RaiseMessageReceived(
                        receivedData,
                        result.RemoteEndPoint);
                }

            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("[UDP][" + _deviceName + "] Receive Loop Closed");
            }
            catch (SocketException ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Console.WriteLine("[UDP][" + _deviceName + "] Receive Failed : " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Console.WriteLine("[UDP][" + _deviceName + "] Receive Failed : " + ex.Message);
                }

            }
            _isReceiving = false;
        }

        /// <summary>
        /// 마지막 [Log] 출력 이후 1초 이상 지났을 경우,
        /// 수신 [Log]를 출력한다.
        /// </summary>
        private void PrintReceiveLogIfNeeded(
            byte[] receivedData,
            IPEndPoint remoteEndPoint)
        {
            if ((DateTime.Now - _lastRecvLogTime).TotalSeconds < 1)
                return;

            PrintHexData(
                "[UDP][" + _deviceName + "] RECV",
                receivedData);

            Console.WriteLine("[UDP][" + _deviceName + "] From : " + remoteEndPoint);

            ConsoleLogHelper.PrintLine();

            _lastRecvLogTime =
                DateTime.Now;
        }

        /// <summary>
        /// [ViewModel] 또는 외부 구독자에게 수신 데이터 전달
        /// </summary>
        private void RaiseMessageReceived(
            byte[] receivedData,
            IPEndPoint remoteEndPoint)
        {
            MessageReceived?.Invoke(
                receivedData,
                remoteEndPoint);
        }

        #endregion

        #region [Log]

        /// <summary>
        /// [byte] 배열을 [HEX] 문자열 형태로 [Console] 출력
        /// </summary>
        private void PrintHexData(
            string prefix,
            byte[] data)
        {
            Console.Write(prefix + " ");

            foreach (byte b in data)
            {
                Console.Write(b.ToString("X2") + " ");
            }

            Console.WriteLine();
        }

        #endregion

        #region [Disconnect]

        /// <summary>
        /// [UDP] [Client] 객체만 정리
        /// 
        /// [StopReceive] / [StartReceive]에서 [UdpClient]를 재생성할 때 사용한다.
        /// </summary>
        private void DisconnectClientOnly()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }

        #endregion

        #region [Dispose]

        /// <summary>
        /// [UDP] 연결 해제 및 리소스 정리
        /// 
        /// 수신 루프 종료 요청 후,
        /// [UdpClient] / [CancellationTokenSource]를 안전하게 정리한다.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopReceive();

            _isDisposed = true;

            Console.WriteLine("[UDP][" + _deviceName + "] Disposed");
        }
        #endregion
    }

}
