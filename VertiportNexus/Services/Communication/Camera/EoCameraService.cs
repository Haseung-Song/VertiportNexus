using OpenCvSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Converters;
using VertiportNexus.Services.Communication.Video;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [EO] [Camera] 영상 서비스
    /// 
    /// [EO] [RTSP] 영상 연결 / 해제 / Frame 수신을 담당한다.
    /// 
    /// [MainViewModel]에서는 화면 바인딩만 처리하고,
    /// 실제 [FFmpeg] 영상 처리는 본 서비스에서 수행한다.
    /// </summary>
    internal class EoCameraService
    {
        #region [Fields]

        /// <summary>
        /// [EO] [RTSP] 영상 출력용 [FFmpeg] Decoder
        /// </summary>
        private readonly FFmpegDecoderService _eoDecoder;

        /// <summary>
        /// [EO] [RTSP] 영상 수신 루프 중지용 [Token]
        /// </summary>
        private CancellationTokenSource _eoVideoCts;

        /// <summary>
        /// [EO] [RTSP] 연결 진행 여부
        /// </summary>
        private bool _isEoVideoConnecting;

        #endregion

        #region [Events]

        /// <summary>
        /// [EO] 영상 [Frame] 수신 이벤트
        /// 
        /// [FFmpeg]에서 수신한 [Frame]을 [BitmapSource]로 변환 후 전달한다.
        /// </summary>
        public event Action<BitmapSource> FrameReceived;

        /// <summary>
        /// [EO] 영상 상태 메시지 변경 이벤트
        /// 
        /// [MainViewModel]의 영상 상태 표시 갱신에 사용한다.
        /// </summary>
        public event Action<string> StatusChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [EoCameraService] 생성자
        /// </summary>
        public EoCameraService()
        {
            _eoDecoder =
                new FFmpegDecoderService(
                    "EO");
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [EO] [RTSP] 영상 연결
        /// 
        /// [FFmpegDecoderService]로 [RTSP] Stream을 열고,
        /// 별도 [Task]에서 [Frame] 수신 루프를 시작한다.
        /// </summary>
        /// <param name="rtspAddress">
        /// [EO] [RTSP] 주소
        /// </param>
        public void Connect(
            string rtspAddress)
        {
            if (_isEoVideoConnecting)
            {
                Console.WriteLine("[EO VIDEO] RTSP Connect Ignored : Connecting");
                return;
            }

            if (string.IsNullOrWhiteSpace(
                rtspAddress))
            {
                StatusChanged?.Invoke(
                    "EO RTSP Address Empty");

                Console.WriteLine("[EO VIDEO] RTSP Connect Failed : Address is empty");
                ConsoleLogHelper.PrintLine();

                return;
            }

            _isEoVideoConnecting =
                true;

            try
            {
                StatusChanged?.Invoke(
                    "EO RTSP Connecting...");

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[EO VIDEO] RTSP Connect Try");
                Console.WriteLine("[EO VIDEO] RTSP : " + rtspAddress);
                ConsoleLogHelper.PrintLine();

                // [EO] 기존 영상 루프 정리
                //
                // 재연결 전 기존 [RTSP] 수신 루프와
                // [FFmpeg] Decoder 리소스가 남아있을 수 있으므로 먼저 정리한다.
                Disconnect(
                    true);

                _eoVideoCts =
                    new CancellationTokenSource();

                bool isOpen =
                    _eoDecoder.Open(
                        rtspAddress);

                if (!isOpen)
                {
                    StatusChanged?.Invoke(
                        "EO RTSP Connect Failed");

                    Console.WriteLine("[EO CAMERA] RTSP Open Failed");
                    ConsoleLogHelper.PrintLine();

                    return;
                }

                StatusChanged?.Invoke(
                    "EO RTSP Connected");

                Console.WriteLine("[EO CAMERA] RTSP Open Success");
                ConsoleLogHelper.PrintLine();

                _ = Task.Run(() =>
                {
                    FFmpegEoCaptureLoop(
                        _eoVideoCts.Token);
                });

            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(
                    "EO RTSP Error");

                Console.WriteLine("[EO CAMERA ERROR] " + ex.Message);
                ConsoleLogHelper.PrintLine();
            }
            finally
            {
                _isEoVideoConnecting =
                    false;
            }

        }

        /// <summary>
        /// [EO] [RTSP] 영상 연결 해제
        /// </summary>
        /// <param name="isReconnectCleanup">
        /// 재연결 전 정리 여부
        /// </param>
        public void Disconnect(
            bool isReconnectCleanup = false)
        {
            try
            {
                // [EO] 영상 수신 작업 취소 요청
                //
                // 기존 영상 수신 루프가
                // 더 이상 [Frame]을 전달하지 않도록 중지 신호를 보낸다.
                _eoVideoCts?.Cancel();

                // [EO] Decoder 종료
                //
                // RTSP / FFmpeg Decoder 리소스를 정리한다.
                _eoDecoder.Close();

                // [EO] 영상 화면 초기화
                //
                // 화면에서 기존 [EO] 영상을 제거하기 위해
                // [null] Frame을 전달한다.
                FrameReceived?.Invoke(
                    null);

                ReleaseCancellationTokenSource();

                if (isReconnectCleanup)
                {
                    Console.WriteLine("[EO VIDEO] RTSP Cleanup Before Connect Complete");
                }
                else
                {
                    Console.WriteLine("[EO VIDEO] RTSP Disconnect Complete");
                }

                ConsoleLogHelper.PrintLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EO VIDEO ERROR] Disconnect Exception : " + ex.Message);
                ConsoleLogHelper.PrintLine();
            }

        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [EO] [RTSP] Frame 수신 루프
        /// 
        /// [FFmpegDecoderService]에서 [Mat] Frame을 읽고,
        /// [WPF] [Image]에 표시할 [BitmapSource]로 변환한다.
        /// </summary>
        /// <param name="cancellationToken">
        /// 영상 수신 루프 중지 [Token]
        /// </param>
        private void FFmpegEoCaptureLoop(
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using (Mat frame =
                       _eoDecoder.ReadFrame())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (frame == null ||
                        frame.Empty())
                    {
                        Thread.Sleep(
                            10);

                        continue;
                    }

                    BitmapSource bitmap =
                        MatToBitmapSourceConverter.Convert(
                            frame);

                    bitmap?.Freeze();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    FrameReceived?.Invoke(
                        bitmap);
                }

            }

        }

        /// <summary>
        /// [EO] 영상 수신 [CancellationTokenSource] 정리
        /// </summary>
        private void ReleaseCancellationTokenSource()
        {
            _eoVideoCts?.Dispose();

            _eoVideoCts =
                null;
        }
        #endregion
    }

}
