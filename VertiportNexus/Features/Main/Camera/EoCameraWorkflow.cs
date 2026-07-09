using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Services.Camera;
using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.Features.Main.Camera
{
    /// <summary>
    /// [EO Camera] 연동 Workflow
    /// 
    /// [MainViewModel]에 직접 포함되어 있던
    /// EO Camera Frame / 상태 처리와 RTSP 재연결 Loop를 분리한다.
    /// 
    /// [MainViewModel]은 수신된 Frame / 상태 결과를 화면에 반영하고,
    /// 본 Workflow는 EO Camera 연결 상태와 재연결 흐름을 관리한다.
    /// </summary>
    internal sealed class EoCameraWorkflow
    {
        #region [Constants]

        /// <summary>
        /// [EO] RTSP 재연결 대기 시간 [ms]
        /// </summary>
        private const int RECONNECT_DELAY_MS =
            3000;

        /// <summary>
        /// [EO] RTSP 최대 재연결 시도 횟수
        /// </summary>
        private const int MAX_RECONNECT_COUNT =
            20;

        #endregion

        #region [Fields]

        /// <summary>
        /// [EO Camera] Controller
        /// </summary>
        private readonly EoCameraController _eoCameraController;

        /// <summary>
        /// [EO Camera] 영상 서비스
        /// </summary>
        private readonly EoCameraService _eoCameraService;

        /// <summary>
        /// [EO] RTSP 연결 주소
        /// </summary>
        private readonly string _rtspAddress;

        /// <summary>
        /// [EO] 영상 표시 허용 여부
        /// 
        /// 연결 해제 또는 연결 중 해제 시,
        /// 뒤늦게 들어온 [Frame]이 화면에 다시 표시되지 않도록 제어한다.
        /// </summary>
        private bool _isVideoDisplayEnabled;

        /// <summary>
        /// [EO] RTSP 재연결 진행 여부
        /// 
        /// 장비 전원 직후 EO Camera가 아직 Ready 상태가 아닐 경우,
        /// CAMERA ERROR MODE 상태에서 RTSP 연결을 반복 재시도하기 위해 사용한다.
        /// </summary>
        private bool _isRtspReconnectRunning;

        /// <summary>
        /// [EO] RTSP 재연결 시도 번호
        /// </summary>
        private int _rtspReconnectTryCount;

        /// <summary>
        /// [EO] RTSP 연결 완료 여부
        /// 
        /// EO Camera RTSP 연결 성공 후
        /// Home Position 이동을 수행하기 위해 사용한다.
        /// </summary>
        private bool _isRtspConnected;

        #endregion

        #region [Properties]

        /// <summary>
        /// [EO] 영상 표시 허용 여부
        /// </summary>
        internal bool IsVideoDisplayEnabled
        {
            get
            {
                return _isVideoDisplayEnabled;
            }

        }

        /// <summary>
        /// [EO] RTSP 연결 완료 여부
        /// </summary>
        internal bool IsRtspConnected
        {
            get
            {
                return _isRtspConnected;
            }

        }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [EO Camera] 연동 Workflow 생성자
        /// </summary>
        /// <param name="eoCameraController">
        /// [EO Camera] Controller
        /// </param>
        /// <param name="eoCameraService">
        /// [EO Camera] 영상 서비스
        /// </param>
        /// <param name="rtspAddress">
        /// [EO] RTSP 연결 주소
        /// </param>
        internal EoCameraWorkflow(
            EoCameraController eoCameraController,
            EoCameraService eoCameraService,
            string rtspAddress)
        {
            _eoCameraController =
                eoCameraController;

            _eoCameraService =
                eoCameraService;

            _rtspAddress =
                rtspAddress;
        }

        #endregion

        #region [Connection Methods]

        /// <summary>
        /// [EO] 영상 표시 활성화
        /// 
        /// 장비 연결 완료 후 EO Camera Frame을
        /// 화면에 표시할 수 있도록 허용한다.
        /// </summary>
        internal void EnableVideoDisplay()
        {
            _isVideoDisplayEnabled =
                true;
        }

        /// <summary>
        /// [EO] 영상 표시 비활성화
        /// 
        /// 장비 연결 해제 또는 재연결 중지 시
        /// 뒤늦게 들어온 Frame이 화면에 반영되지 않도록 차단한다.
        /// </summary>
        internal void DisableVideoDisplay()
        {
            _isVideoDisplayEnabled =
                false;

            _isRtspConnected =
                false;
        }

        /// <summary>
        /// [EO] RTSP 연결 요청
        /// </summary>
        internal void Connect()
        {
            _eoCameraController
                .Connect(
                    _rtspAddress);
        }

        /// <summary>
        /// [EO] RTSP 연결 해제
        /// 
        /// RTSP 재연결 Loop를 중지하고,
        /// EO Camera Controller를 통해 연결을 해제한다.
        /// </summary>
        internal void Disconnect()
        {
            StopReconnect();

            DisableVideoDisplay();

            _eoCameraController
                .Disconnect();
        }

        #endregion

        #region [Result Methods]

        /// <summary>
        /// [EO Camera] Frame 결과 생성
        /// </summary>
        /// <param name="bitmap">
        /// EO Camera Frame Image
        /// </param>
        /// <returns>
        /// [EO Camera] Controller 처리 결과
        /// </returns>
        internal EoCameraControllerResult CreateFrameResult(
            BitmapSource bitmap)
        {
            return _eoCameraController
                .CreateFrameResult(
                    bitmap);
        }

        /// <summary>
        /// [EO Camera] 상태 결과 생성
        /// 
        /// EO Camera 상태 문자열을 Controller 처리 결과로 변환하고,
        /// RTSP 연결 성공 / 실패 상태를 Workflow 내부 상태에 반영한다.
        /// </summary>
        /// <param name="statusText">
        /// [EO] 영상 상태 문자열
        /// </param>
        /// <returns>
        /// [EO Camera] Controller 처리 결과
        /// </returns>
        internal EoCameraControllerResult CreateStatusResult(
            string statusText)
        {
            EoCameraControllerResult result =
                _eoCameraController
                    .CreateStatusResult(
                        statusText);

            if (result.IsConnected.HasValue)
            {
                _isRtspConnected =
                    result.IsConnected.Value;
            }
            return result;
        }

        #endregion

        #region [Reconnect Methods]

        /// <summary>
        /// [EO] RTSP 재연결 시작
        /// 
        /// CAMERA ERROR MODE 상태에서 EO Camera가 Ready 상태로 전환될 때까지
        /// 일정 간격으로 RTSP 연결을 재시도한다.
        /// </summary>
        /// <param name="operationModeChanged">
        /// 운용 모드 표시 문자열 변경 처리기
        /// </param>
        internal async void StartReconnect(
            Action<string> operationModeChanged)
        {
            if (_isRtspReconnectRunning)
            {
                return;
            }

            if (!_isVideoDisplayEnabled)
            {
                return;
            }

            _isRtspReconnectRunning =
                true;

            _rtspReconnectTryCount =
                0;

            try
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Reconnect Start");

                ConsoleLogHelper.PrintLine();

                while (_isRtspReconnectRunning &&
                       _isVideoDisplayEnabled &&
                       _rtspReconnectTryCount < MAX_RECONNECT_COUNT)
                {
                    _rtspReconnectTryCount++;

                    await Task.Delay(
                        RECONNECT_DELAY_MS);

                    if (!_isRtspReconnectRunning ||
                        !_isVideoDisplayEnabled)
                    {
                        return;
                    }

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine(
                        "[EO CAMERA] RTSP Reconnect Try : "
                        + _rtspReconnectTryCount
                        + " / "
                        + MAX_RECONNECT_COUNT);

                    ConsoleLogHelper.PrintLine();

                    _eoCameraService
                        .Connect(
                            _rtspAddress);
                }

                if (_isRtspReconnectRunning)
                {
                    operationModeChanged
                        ?.Invoke(
                            "CAMERA ERROR MODE");

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine(
                        "[EO CAMERA] RTSP Reconnect Failed : Max Retry Count");

                    ConsoleLogHelper.PrintLine();
                }

            }
            finally
            {
                _isRtspReconnectRunning =
                    false;
            }

        }

        /// <summary>
        /// [EO] RTSP 재연결 중지
        /// 
        /// EO Camera가 정상 연결되었거나,
        /// 장비 연결 해제 / 프로그램 종료 시
        /// RTSP 재연결 Loop를 중지한다.
        /// </summary>
        internal void StopReconnect()
        {
            if (!_isRtspReconnectRunning)
            {
                return;
            }

            _isRtspReconnectRunning =
                false;

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[EO CAMERA] RTSP Reconnect Stop");

            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}
