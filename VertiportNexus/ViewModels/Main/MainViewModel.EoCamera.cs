using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - EO Camera
    /// 
    /// EO RTSP 영상 Frame 수신 / 상태 변경 / 재연결 처리를 담당한다.
    /// 
    /// MainViewModel 본문 비중을 줄이기 위해
    /// EO Camera 관련 Event / RTSP Reconnect 로직만 별도 Partial 파일로 분리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [EO Camera Event Methods]

        /// <summary>
        /// [EO Camera] Frame 수신 처리
        /// 
        /// RTSP 수신 서비스에서 전달된 BitmapSource Frame을
        /// UI Binding 속성에 반영한다.
        /// 
        /// 프로그램 종료 중이거나 Frame 데이터가 없는 경우에는
        /// UI 객체 접근을 수행하지 않는다.
        /// </summary>
        /// <param name="bitmap">
        /// EO Camera Frame Image
        /// </param>
        private void OnEoCameraFrameReceived(
            BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                return;
            }

            try
            {
                // [EO Camera] Frame Freeze 처리
                //
                // RTSP 수신 Thread에서 생성된 BitmapSource를
                // UI Thread Binding 속성에 안전하게 전달하기 위해 Freeze 처리한다.
                if (bitmap.CanFreeze)
                {
                    bitmap.Freeze();
                }

                EOCameraImage =
                    bitmap;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(
                    "[EO CAMERA] Frame Update Skipped : "
                    + ex.Message);
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine(
                    "[EO CAMERA] Frame Update Skipped : View Disposed / "
                    + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[EO CAMERA] Frame Update Failed : "
                    + ex.Message);
            }

        }

        /// <summary>
        /// [EO] 영상 상태 변경 처리
        /// 
        /// [EoCameraService]에서 전달받은 상태 메시지를
        /// [OperationModeText]에 반영한다.
        /// 
        /// EO Camera가 Error / Connect Failed 상태인 경우,
        /// 장비 전원 직후 Camera Ready 지연 가능성을 고려하여
        /// RTSP 연결 재시도를 시작한다.
        /// </summary>
        /// <param name="statusText">
        /// [EO] 영상 상태 문자열
        /// </param>
        private void OnEoCameraStatusChanged(
            string statusText)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (statusText == "EO RTSP Connected")
                {
                    _isEoRtspConnected =
                        true;

                    OperationModeText =
                        "SURVEILLANCE MODE";

                    StopEoRtspReconnect();
                }
                else if (statusText == "EO RTSP Error" ||
                         statusText == "EO RTSP Connect Failed")
                {
                    _isEoRtspConnected =
                        false;

                    OperationModeText =
                        "CAMERA RECONNECTING...";

                    StartEoRtspReconnect();
                }

            });

        }

        /// <summary>
        /// [EO] RTSP 재연결 시작
        /// 
        /// CAMERA ERROR MODE 상태에서 EO Camera가 Ready 상태로 전환될 때까지
        /// 일정 간격으로 RTSP 연결을 재시도한다.
        /// </summary>
        private async void StartEoRtspReconnect()
        {
            const int RECONNECT_DELAY_MS =
                3000;

            const int MAX_RECONNECT_COUNT =
                20;

            if (_isEoRtspReconnectRunning)
            {
                return;
            }

            if (!_isEoVideoDisplayEnabled)
            {
                return;
            }

            _isEoRtspReconnectRunning =
                true;

            _eoRtspReconnectTryCount =
                0;

            try
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Reconnect Start");

                ConsoleLogHelper.PrintLine();

                while (_isEoRtspReconnectRunning &&
                       _isEoVideoDisplayEnabled &&
                       _eoRtspReconnectTryCount < MAX_RECONNECT_COUNT)
                {
                    _eoRtspReconnectTryCount++;

                    await Task.Delay(
                        RECONNECT_DELAY_MS);

                    if (!_isEoRtspReconnectRunning ||
                        !_isEoVideoDisplayEnabled)
                    {
                        return;
                    }

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine(
                        "[EO CAMERA] RTSP Reconnect Try : "
                        + _eoRtspReconnectTryCount
                        + " / "
                        + MAX_RECONNECT_COUNT);

                    ConsoleLogHelper.PrintLine();

                    _eoCameraService.Connect(
                        DEFAULT_EO_RTSP_ADDRESS);
                }

                if (_isEoRtspReconnectRunning)
                {
                    OperationModeText =
                        "CAMERA ERROR MODE";

                    ConsoleLogHelper.PrintLine();

                    Console.WriteLine(
                        "[EO CAMERA] RTSP Reconnect Failed : Max Retry Count");

                    ConsoleLogHelper.PrintLine();
                }

            }
            finally
            {
                _isEoRtspReconnectRunning =
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
        private void StopEoRtspReconnect()
        {
            if (!_isEoRtspReconnectRunning)
            {
                return;
            }

            _isEoRtspReconnectRunning =
                false;

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[EO CAMERA] RTSP Reconnect Stop");

            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}
