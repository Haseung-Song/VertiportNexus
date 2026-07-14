using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.UdpTestClient.Models;
using VertiportNexus.UdpTestClient.Protocol;

namespace VertiportNexus.UdpTestClient.Services
{
    internal sealed class UdpRadarTestClient : IDisposable
    {
        private readonly RadarTestPacketBuilder _builder = new RadarTestPacketBuilder();
        private readonly RadarResponseParser _parser = new RadarResponseParser();
        private readonly UdpClient _client;
        private readonly IPEndPoint _target;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private long _previousReceivedMs = -1;
        private int _sendCount;
        private int _receiveCount;
        private bool _disposed;

        public UdpRadarTestClient(string host, int port)
        {
            _client = new UdpClient(0);
            _target = new IPEndPoint(IPAddress.Parse(host), port);

            Console.WriteLine("Local : " + _client.Client.LocalEndPoint);
            Console.WriteLine("Remote: " + _target);
        }

        public async Task SendTrackingRequestAsync(ushort targetId, float azimuthRadian, float elevationRadian)
        {
            ThrowIfDisposed();

            byte[] request = _builder.BuildTrackingRequest(targetId, azimuthRadian, elevationRadian);
            await _client.SendAsync(request, request.Length, _target);
            _sendCount++;

            Console.WriteLine();
            Console.WriteLine("[TX #" + _sendCount + "] Tracking Request: " + request.Length + " bytes");
            Console.WriteLine("Target ID : " + targetId);
            Console.WriteLine("Azimuth   : " + azimuthRadian + " rad");
            Console.WriteLine("Elevation : " + elevationRadian + " rad");
        }

        public async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result = await ReceiveWithCancellationAsync(_client, cancellationToken);
                long receivedMs = _stopwatch.ElapsedMilliseconds;
                RadarResponse response = _parser.Parse(result.Buffer);
                _receiveCount++;

                Console.WriteLine();
                Console.WriteLine("[RX #" + _receiveCount + "]");
                Console.WriteLine("Packet Number : " + response.PacketNumber);
                Console.WriteLine("Target ID     : " + response.TargetId);
                Console.WriteLine("Track Result  : " + response.TrackResult);
                Console.WriteLine("Azimuth       : " + response.Azimuth);
                Console.WriteLine("Elevation     : " + response.Elevation);
                Console.WriteLine("Checksum      : " + (response.IsChecksumValid ? "PASS" : "FAIL"));

                if (_previousReceivedMs >= 0)
                {
                    long interval = receivedMs - _previousReceivedMs;
                    bool intervalPass = interval >= 400 && interval <= 600;
                    Console.WriteLine("Interval      : " + interval + " ms " + (intervalPass ? "PASS" : "FAIL"));
                }

                _previousReceivedMs = receivedMs;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _client.Close();
            _client.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UdpRadarTestClient));
        }

        private static async Task<UdpReceiveResult> ReceiveWithCancellationAsync(UdpClient client, CancellationToken cancellationToken)
        {
            try
            {
                Task<UdpReceiveResult> receiveTask = client.ReceiveAsync();
                Task cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
                Task completed = await Task.WhenAny(receiveTask, cancelTask);

                if (completed != receiveTask)
                    throw new OperationCanceledException(cancellationToken);

                return await receiveTask;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                throw new InvalidOperationException("UDP 대상에서 응답할 수 없습니다. VertiportNexus의 UDP 수신 시작 여부와 대상 IP/Port를 확인하세요.", ex);
            }
        }
    }
}
