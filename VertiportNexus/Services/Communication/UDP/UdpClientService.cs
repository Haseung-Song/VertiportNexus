using Serilog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.Common.Logging;

namespace VertiportNexus.Services.Communication.UDP
{
    /// <summary>
    /// [UDP] 통신 서비스
    /// 
    /// 지정된 [Port]로 들어오는 UDP Packet을 수신하고,
    /// 외부 대상에게 UDP Packet을 송신한다.
    /// </summary>
    public class UdpClientService
    {
        #region [Fields]

        /// <summary>
        /// [UDP] 수신 Raw Packet 로그 저장 여부
        /// 
        /// Radar UDP Packet 원본 확인을 위해
        /// 전체 수신 Packet을 모두 저장하지 않고,
        /// 1초 간격으로만 Raw Packet 로그를 저장한다.
        /// </summary>
        private static readonly bool ENABLE_UDP_RECEIVE_PACKET_LOG =
            true;

        /// <summary>
        /// [UDP] Client 객체
        /// </summary>
        private UdpClient _udpClient;

        /// <summary>
        /// 수신 루프 종료 제어용 [Token]
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// [Log] 표시용 이름
        /// </summary>
        private readonly string _serviceName;

        /// <summary>
        /// UDP 수신 실행 여부
        /// 
        /// 이미 UDP 수신 중인 경우
        /// 중복 Receive 시작을 방지한다.
        /// </summary>
        private bool _isReceiving;

        /// <summary>
        /// 마지막 수신 [Log] 출력 시간 저장
        /// 
        /// UDP Packet이 반복 수신될 수 있으므로,
        /// [Log] 파일 도배 방지를 위해 일정 시간 간격으로만
        /// 수신 Raw Packet [Log] 출력할 때 사용한다.
        /// </summary>
        private DateTime _lastRecvLogTime =
            DateTime.MinValue;

        /// <summary>
        /// [UDP] Client 접근 동기화 객체
        /// 
        /// 수신 시작 / 중지 / 송신 과정에서
        /// UdpClient 객체 접근이 겹치는 상황을 방지한다.
        /// </summary>
        private readonly object _udpClientLock =
            new object();

        #endregion

        #region [Properties]

        /// <summary>
        /// UDP 수신 실행 여부
        /// </summary>
        public bool IsReceiving
        {
            get
            {
                lock (_udpClientLock)
                {
                    return _isReceiving;
                }

            }

        }

        #endregion

        #region [Events]

        /// <summary>
        /// UDP 수신 데이터 전달 이벤트
        /// </summary>
        public event Action<byte[], IPEndPoint, DateTime> MessageReceived;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [UdpClientService] 생성자
        /// </summary>
        /// <param name="serviceName">
        /// [Log] 표시용 서비스 이름
        /// </param>
        public UdpClientService(
            string serviceName)
        {
            _serviceName =
                string.IsNullOrWhiteSpace(serviceName)
                    ? "UDP"
                    : serviceName;
        }

        #endregion

        #region [Receive Methods]

        /// <summary>
        /// UDP 수신 시작
        /// </summary>
        /// <param name="localPort">
        /// 수신 Port
        /// </param>
        public void StartReceive(
            int localPort)
        {
            lock (_udpClientLock)
            {
                if (_isReceiving)
                {
                    Log.Information(
                        "[UDP][{ServiceName}] Receive Start Ignored : Already Started, LocalPort={LocalPort}",
                        _serviceName,
                        localPort);

                    return;
                }

                try
                {
                    _udpClient =
                        new UdpClient(
                            localPort);

                    _cts =
                        new CancellationTokenSource();

                    _isReceiving =
                        true;

                    LogSectionHelper.Information(
                        $"[UDP][{_serviceName}] RECEIVE START");

                    Log.Information(
                        "[UDP][{ServiceName}] Receive Start : LocalPort={LocalPort}",
                        _serviceName,
                        localPort);

                    Task.Run(() =>
                        ReceiveLoopAsync(
                            _cts.Token));
                }
                catch (Exception ex)
                {
                    _isReceiving =
                        false;

                    _udpClient?.Dispose();
                    _udpClient = null;

                    _cts?.Dispose();
                    _cts = null;

                    Log.Error(
                        ex,
                        "[UDP][{ServiceName}] Receive Start Failed : LocalPort={LocalPort}",
                        _serviceName,
                        localPort);
                }

            }

        }

        /// <summary>
        /// UDP 수신 루프
        /// 
        /// UDP Packet 수신을 반복 수행하고,
        /// 수신된 데이터를 MessageReceived 이벤트로 전달한다.
        /// </summary>
        /// <param name="token">
        /// 수신 루프 종료 제어용 [Token]
        /// </param>
        private async Task ReceiveLoopAsync(
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpClient udpClient;

                    lock (_udpClientLock)
                    {
                        udpClient =
                            _udpClient;
                    }

                    if (udpClient == null ||
                        !_isReceiving)
                    {
                        return;
                    }

                    UdpReceiveResult receiveResult =
                        await udpClient
                            .ReceiveAsync();

                    byte[] receivedData =
                        receiveResult.Buffer;

                    IPEndPoint remoteEndPoint =
                        receiveResult.RemoteEndPoint;

                    PrintReceiveLogIfNeeded(
                        receivedData,
                        remoteEndPoint);

                    RaiseMessageReceived(
                        receivedData,
                        remoteEndPoint);
                }
                catch (ObjectDisposedException)
                {
                    Log.Information(
                        "[UDP][{ServiceName}] Receive Loop Closed",
                        _serviceName);

                    // [UDP] 수신 루프 종료
                    //
                    // StopReceive() 호출로 UdpClient가 Dispose된 경우
                    // 정상적인 종료 흐름으로 처리한다.
                    return;
                }
                catch (SocketException ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    // [UDP] ConnectionReset 예외 무시
                    //
                    // UDP Loopback 테스트 중 응답 송신 시
                    // 송신 측 Socket이 이미 종료된 경우 발생할 수 있다.
                    //
                    // 수신 서비스 자체는 계속 유지되어야 하므로,
                    // 해당 예외는 로그만 출력하고 다음 수신을 계속 진행한다.
                    if (ex.SocketErrorCode ==
                        SocketError.ConnectionReset)
                    {
                        Log.Warning(
                            ex,
                            "[UDP][{ServiceName}] Receive Socket Reset Ignored",
                            _serviceName);

                        continue;
                    }

                    Log.Error(
                        ex,
                        "[UDP][{ServiceName}] Receive Socket Failed",
                        _serviceName);

                    continue;
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    Log.Error(
                        ex,
                        "[UDP][{ServiceName}] Receive Failed",
                        _serviceName);

                    continue;
                }

            }

        }

        /// <summary>
        /// 마지막 [Log] 출력 이후 1초 이상 지났을 경우,
        /// 수신 [Log]를 출력한다.
        /// </summary>
        private void PrintReceiveLogIfNeeded(
            byte[] receivedData,
            IPEndPoint remoteEndPoint)
        {
            if (!ENABLE_UDP_RECEIVE_PACKET_LOG)
            {
                return;
            }

            if ((DateTime.Now - _lastRecvLogTime).TotalSeconds < 1)
            {
                return;
            }

            Log.Debug(
                "[UDP][{ServiceName}] RECV Remote={RemoteEndPoint}, Hex={Hex}",
                _serviceName,
                remoteEndPoint,
                ToHexString(
                    receivedData));

            _lastRecvLogTime =
                DateTime.Now;
        }

        /// <summary>
        /// 수신 데이터 이벤트 전달
        /// </summary>
        /// <param name="receivedData">
        /// 수신 데이터
        /// </param>
        /// <param name="remoteEndPoint">
        /// 송신자 EndPoint
        /// </param>
        private void RaiseMessageReceived(
            byte[] receivedData,
            IPEndPoint remoteEndPoint)
        {
            MessageReceived?.Invoke(
                receivedData,
                remoteEndPoint,
                DateTime.Now);
        }

        #endregion

        #region [Send Methods]

        /// <summary>
        /// UDP Packet 송신
        /// </summary>
        /// <param name="data">
        /// 송신 데이터
        /// </param>
        /// <param name="remoteIpAddress">
        /// 송신 대상 IP
        /// </param>
        /// <param name="remotePort">
        /// 송신 대상 Port
        /// </param>
        /// <returns>
        /// 송신 성공 여부
        /// </returns>
        public bool Send(
            byte[] data,
            string remoteIpAddress,
            int remotePort)
        {
            if (data == null ||
                data.Length == 0)
            {
                Log.Warning(
                    "[UDP][{ServiceName}] Send Failed : Empty Data",
                    _serviceName);

                return false;
            }

            try
            {
                lock (_udpClientLock)
                {
                    if (_udpClient == null)
                    {
                        _udpClient =
                            new UdpClient();
                    }

                    _udpClient
                        .Send(
                            data,
                            data.Length,
                            remoteIpAddress,
                            remotePort);
                }

                Log.Debug(
                    "[UDP][{ServiceName}] SEND Remote={RemoteIp}:{RemotePort}, Hex={Hex}",
                    _serviceName,
                    remoteIpAddress,
                    remotePort,
                    ToHexString(
                        data));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[UDP][{ServiceName}] Send Failed : Remote={RemoteIp}:{RemotePort}",
                    _serviceName,
                    remoteIpAddress,
                    remotePort);

                return false;
            }

        }

        /// <summary>
        /// UDP Packet 송신
        /// </summary>
        /// <param name="data">
        /// 송신 데이터
        /// </param>
        /// <param name="remoteEndPoint">
        /// 송신 대상 EndPoint
        /// </param>
        /// <returns>
        /// 송신 성공 여부
        /// </returns>
        public bool Send(
            byte[] data,
            IPEndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
            {
                Log.Warning(
                    "[UDP][{ServiceName}] Send Failed : Remote EndPoint is null",
                    _serviceName);

                return false;
            }

            return Send(
                data,
                remoteEndPoint.Address.ToString(),
                remoteEndPoint.Port);
        }

        #endregion

        #region [Stop Methods]

        /// <summary>
        /// UDP 수신 중지
        /// 
        /// 수신 루프를 종료하고,
        /// UDP Client / Token 객체를 정리한다.
        /// </summary>
        public void StopReceive()
        {
            lock (_udpClientLock)
            {
                if (!_isReceiving &&
                    _udpClient == null)
                {
                    Log.Information(
                        "[UDP][{ServiceName}] Receive Stop Ignored : Already Stopped",
                        _serviceName);

                    return;
                }

                try
                {
                    _isReceiving =
                        false;

                    _cts?.Cancel();

                    _udpClient?.Close();
                    _udpClient?.Dispose();
                    _udpClient = null;

                    _cts?.Dispose();
                    _cts = null;

                    LogSectionHelper.Information(
                        $"[UDP][{_serviceName}] RECEIVE STOP");

                    Log.Information(
                        "[UDP][{ServiceName}] Receive Stop",
                        _serviceName);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "[UDP][{ServiceName}] Receive Stop Failed",
                        _serviceName);
                }

            }

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
    }

}
