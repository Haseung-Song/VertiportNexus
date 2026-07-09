using System;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.Models.Camera;
using VertiportNexus.Services.Camera;

namespace VertiportNexus.Features.Main.Test
{
    /// <summary>
    /// [Dummy Tracking] 테스트 Manager
    /// 
    /// 실제 드론 / AI 탐지 결과가 없는 상태에서
    /// 더미 Bounding Box 입력을 생성하고,
    /// 최신 탐지값 기준으로 AUTO Tracking 흐름을 검증한다.
    /// 
    /// [MainViewModel]은 테스트 시작 / 중지 요청만 전달하고,
    /// 30Hz 입력 Loop / 최신 탐지값 처리 Loop / 취소 토큰 관리는
    /// 본 Manager에서 수행한다.
    /// </summary>
    internal sealed class DummyTrackingTestManager
    {
        #region [Constants]

        /// <summary>
        /// [Dummy Tracking] 더미 탐지 좌표 입력 주기 [Hz]
        /// 
        /// ICD 기준 탐지 좌표가 초당 30회 들어오는 상황을 모사한다.
        /// </summary>
        private const int DUMMY_DETECTION_HZ =
            30;

        /// <summary>
        /// [Dummy Tracking] 추적 처리 주기 [Hz]
        /// 
        /// 최신 탐지 좌표를 기준으로 TrackingControlService를 호출한다.
        /// </summary>
        private const int DUMMY_TRACKING_HZ =
            30;

        #endregion

        #region [Fields]

        /// <summary>
        /// [Tracking] 자동 추적 제어 서비스
        /// </summary>
        private readonly TrackingControlService _trackingControlService;

        /// <summary>
        /// [Dummy Tracking] 테스트 취소 토큰
        /// 
        /// 실제 탐지 객체 수신 전,
        /// 더미 Bounding Box를 주기적으로 생성하여
        /// AUTO Tracking 흐름을 검증하기 위해 사용한다.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// [Dummy Tracking] 테스트 실행 여부
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 동기화 객체
        /// </summary>
        private readonly object _targetLock =
            new object();

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 정보
        /// 
        /// 30Hz로 수신되는 더미 Bounding Box 중
        /// 가장 마지막 값을 저장한다.
        /// </summary>
        private DetectionBoundingBox _latestBoundingBox;

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 수신 시간
        /// </summary>
        private DateTime _latestReceivedTime;

        /// <summary>
        /// [Dummy Tracking] 최신 탐지 객체 Frame 번호
        /// </summary>
        private int _latestFrameId;

        /// <summary>
        /// [Dummy Tracking] 마지막 처리 Frame 번호
        /// 
        /// 동일 Frame을 중복 처리하지 않기 위해 사용한다.
        /// </summary>
        private int _lastProcessedFrameId =
            -1;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Dummy Tracking] 테스트 Manager 생성자
        /// </summary>
        /// <param name="trackingControlService">
        /// [Tracking] 자동 추적 제어 서비스
        /// </param>
        internal DummyTrackingTestManager(
            TrackingControlService trackingControlService)
        {
            _trackingControlService =
                trackingControlService;
        }

        #endregion

        #region [Start / Stop Methods]

        /// <summary>
        /// [Dummy Tracking] 테스트 시작
        /// 
        /// 실제 드론 / AI 탐지 결과가 없는 상태에서
        /// 30Hz 더미 Bounding Box 입력을 생성하고,
        /// 최신 탐지값 기준으로 AUTO Tracking 흐름을 검증한다.
        /// </summary>
        /// <param name="isDeviceFullyConnected">
        /// [MCB] / [SCB] 전체 연결 여부
        /// </param>
        /// <param name="currentZoomProvider">
        /// 현재 [Zoom] 값 조회 함수
        /// </param>
        /// <returns>
        /// 비동기 작업
        /// </returns>
        internal Task StartAsync(
            bool isDeviceFullyConnected,
            Func<double> currentZoomProvider)
        {
            if (_isRunning)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Start Ignored : Already Running");

                return Task.CompletedTask;
            }

            if (!isDeviceFullyConnected)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Start Skipped : Device Not Fully Connected");

                return Task.CompletedTask;
            }

            _isRunning =
                true;

            _lastProcessedFrameId =
                -1;

            _cancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken cancellationToken =
                _cancellationTokenSource.Token;

            ConsoleLogHelper.PrintBlock(
                "[DUMMY TRACKING] Start");

            Console.WriteLine(
                "[DUMMY TRACKING] Detection Input Hz : "
                + DUMMY_DETECTION_HZ);

            Console.WriteLine(
                "[DUMMY TRACKING] Tracking Process Hz : "
                + DUMMY_TRACKING_HZ);

            _ =
                Task.Run(
                    async () =>
                    {
                        await RunDummyDetectionInputLoopAsync(
                            cancellationToken);
                    },
                    cancellationToken);

            _ =
                Task.Run(
                    async () =>
                    {
                        await RunDummyLatestTrackingLoopAsync(
                            cancellationToken,
                            currentZoomProvider);
                    },
                    cancellationToken);

            return Task.CompletedTask;
        }

        /// <summary>
        /// [Dummy Tracking] 테스트 중지
        /// 
        /// 실행 중인 더미 Bounding Box 주입 Loop를 중지한다.
        /// </summary>
        internal void Stop()
        {
            if (!_isRunning)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Stop Ignored : Not Running");

                return;
            }

            _cancellationTokenSource
                ?.Cancel();
        }

        #endregion

        #region [Loop Methods]

        /// <summary>
        /// [Dummy Tracking] 더미 탐지 좌표 입력 Loop
        /// 
        /// ICD 기준 30Hz 탐지 좌표 수신 상황을 모사한다.
        /// 생성된 Bounding Box는 즉시 처리하지 않고,
        /// 최신 탐지값으로만 저장한다.
        /// </summary>
        /// <param name="cancellationToken">
        /// 취소 토큰
        /// </param>
        /// <returns>
        /// 비동기 작업
        /// </returns>
        private async Task RunDummyDetectionInputLoopAsync(
            CancellationToken cancellationToken)
        {
            int frameId =
                0;

            int delayMilliseconds =
                1000 / DUMMY_DETECTION_HZ;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    DetectionBoundingBox boundingBox =
                        CreateSmoothDummyTrackingBoundingBox(
                            frameId);

                    lock (_targetLock)
                    {
                        _latestBoundingBox =
                            boundingBox;

                        _latestFrameId =
                            frameId;

                        _latestReceivedTime =
                            DateTime.Now;
                    }

                    if (frameId % DUMMY_DETECTION_HZ == 0)
                    {
                        Console.WriteLine(
                            "[DUMMY TRACKING][INPUT] 30Hz Latest Frame : "
                            + frameId
                            + ", CenterX="
                            + boundingBox.CenterX
                            + ", CenterY="
                            + boundingBox.CenterY);
                    }

                    frameId++;

                    await Task.Delay(
                            delayMilliseconds,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Failed");
                Console.WriteLine(
                    ex);
            }

        }

        /// <summary>
        /// [Dummy Tracking] 최신 탐지값 기준 추적 Loop
        /// 
        /// 30Hz로 갱신되는 탐지 좌표 중
        /// 가장 마지막 Bounding Box 값을 기준으로 AUTO Tracking을 수행한다.
        /// </summary>
        /// <param name="cancellationToken">
        /// 취소 토큰
        /// </param>
        /// <param name="currentZoomProvider">
        /// 현재 [Zoom] 값 조회 함수
        /// </param>
        /// <returns>
        /// 비동기 작업
        /// </returns>
        private async Task RunDummyLatestTrackingLoopAsync(
            CancellationToken cancellationToken,
            Func<double> currentZoomProvider)
        {
            int delayMilliseconds =
                1000 / DUMMY_TRACKING_HZ;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    DetectionBoundingBox latestBoundingBox =
                        null;

                    int latestFrameId =
                        -1;

                    DateTime latestReceivedTime =
                        DateTime.MinValue;

                    lock (_targetLock)
                    {
                        latestBoundingBox =
                            _latestBoundingBox;

                        latestFrameId =
                            _latestFrameId;

                        latestReceivedTime =
                            _latestReceivedTime;
                    }

                    if (latestBoundingBox == null)
                    {
                        await Task.Delay(
                                delayMilliseconds,
                                cancellationToken)
                            .ConfigureAwait(false);

                        continue;
                    }

                    if (latestFrameId == _lastProcessedFrameId)
                    {
                        await Task.Delay(
                                delayMilliseconds,
                                cancellationToken)
                            .ConfigureAwait(false);

                        continue;
                    }

                    _lastProcessedFrameId =
                        latestFrameId;

                    double elapsedMilliseconds =
                        (DateTime.Now - latestReceivedTime)
                            .TotalMilliseconds;

                    if (latestFrameId % DUMMY_TRACKING_HZ == 0)
                    {
                        Console.WriteLine(
                            "[DUMMY TRACKING][PROCESS] Latest Frame : "
                            + latestFrameId
                            + ", ElapsedMs="
                            + elapsedMilliseconds.ToString("F1")
                            + ", CenterX="
                            + latestBoundingBox.CenterX
                            + ", CenterY="
                            + latestBoundingBox.CenterY);
                    }

                    double currentZoom =
                        currentZoomProvider();

                    _trackingControlService
                        .ProcessTracking(
                            latestBoundingBox,
                            currentZoom);

                    await Task.Delay(
                            delayMilliseconds,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

            }
            catch (TaskCanceledException)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Canceled");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Failed");

                Console.WriteLine(
                    ex);
            }
            finally
            {
                _isRunning =
                    false;

                _cancellationTokenSource =
                    null;

                ConsoleLogHelper.PrintBlock(
                    "[DUMMY TRACKING] Stop");
            }

        }

        #endregion

        #region [Create Methods]

        /// <summary>
        /// [Dummy Tracking] 부드러운 더미 Bounding Box 생성
        /// 
        /// 30Hz 탐지 좌표 입력 상황에서
        /// 탐지 객체 중심점이 화면 외곽에서 중앙으로
        /// 점진적으로 수렴하는 형태를 생성한다.
        /// </summary>
        /// <param name="frameId">
        /// 더미 탐지 Frame 번호
        /// </param>
        /// <returns>
        /// 더미 탐지 객체 영역 정보
        /// </returns>
        private DetectionBoundingBox CreateSmoothDummyTrackingBoundingBox(
            int frameId)
        {
            const double FRAME_WIDTH =
                1920.0;

            const double FRAME_HEIGHT =
                1080.0;

            const double BOX_WIDTH =
                120.0;

            const double BOX_HEIGHT =
                80.0;

            const int FRAMES_PER_SCENARIO =
                DUMMY_DETECTION_HZ * 3;

            const double MAX_OFFSET_X =
                250.0;

            const double MAX_OFFSET_Y =
                150.0;

            double frameCenterX =
                FRAME_WIDTH / 2.0;

            double frameCenterY =
                FRAME_HEIGHT / 2.0;

            int scenarioIndex =
                frameId / FRAMES_PER_SCENARIO;

            int scenarioFrame =
                frameId % FRAMES_PER_SCENARIO;

            double approachRatio =
                1.0 - ((double)scenarioFrame / (FRAMES_PER_SCENARIO - 1));

            double offsetX =
                0.0;

            double offsetY =
                0.0;

            switch (scenarioIndex % 5)
            {
                case 0:
                    // [오른쪽 → 중앙]
                    offsetX =
                        MAX_OFFSET_X * approachRatio;
                    break;

                case 1:
                    // [왼쪽 → 중앙]
                    offsetX =
                        -MAX_OFFSET_X * approachRatio;
                    break;

                case 2:
                    // [위쪽 → 중앙]
                    offsetY =
                        -MAX_OFFSET_Y * approachRatio;
                    break;

                case 3:
                    // [아래쪽 → 중앙]
                    offsetY =
                        MAX_OFFSET_Y * approachRatio;
                    break;

                default:
                    // [우상단 → 중앙]
                    offsetX =
                        MAX_OFFSET_X * approachRatio;

                    offsetY =
                        -MAX_OFFSET_Y * approachRatio;
                    break;
            }

            double centerX =
                frameCenterX + offsetX;

            double centerY =
                frameCenterY + offsetY;

            return new DetectionBoundingBox
            {
                FrameId =
                    frameId,

                X1 =
                    centerX - BOX_WIDTH / 2.0,

                Y1 =
                    centerY - BOX_HEIGHT / 2.0,

                X2 =
                    centerX + BOX_WIDTH / 2.0,

                Y2 =
                    centerY + BOX_HEIGHT / 2.0,

                ClassId =
                    1,

                Confidence =
                    1.0
            };

        }
        #endregion
    }

}
