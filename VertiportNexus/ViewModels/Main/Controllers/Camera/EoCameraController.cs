using System;
using System.Windows.Media.Imaging;
using VertiportNexus.Services.Camera;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [EO Camera] Controller
    /// 
    /// [EO RTSP] 연결 / 재연결 판단 / Frame 결과 변환을 담당한다.
    /// 
    /// 화면 Binding 상태 변경은 [MainViewModel]에서 수행한다.
    /// </summary>
    internal sealed class EoCameraController
    {
        #region [Service Fields]

        /// <summary>
        /// [EO] Camera 서비스
        /// </summary>
        private readonly EoCameraService _eoCameraService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [EO Camera] Controller 생성
        /// </summary>
        internal EoCameraController(
            EoCameraService eoCameraService)
        {
            _eoCameraService =
                eoCameraService;
        }

        #endregion

        #region [EO Camera Methods]

        /// <summary>
        /// [EO] RTSP 연결 시작
        /// </summary>
        internal ControllerResult Connect(
            string rtspAddress)
        {
            try
            {
                _eoCameraService
                    .Connect(
                        rtspAddress);

                return ControllerResult.Success(
                    "EO RTSP Connect Requested");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "[EO CAMERA] RTSP Connect Error : " + ex.Message);

                return ControllerResult.Failed(
                    "EO RTSP Connect Error : " + ex.Message);
            }

        }

        /// <summary>
        /// [EO] RTSP 연결 해제
        /// </summary>
        internal ControllerResult Disconnect()
        {
            try
            {
                _eoCameraService
                    .Disconnect();

                return ControllerResult.Success(
                    "EO RTSP Disconnected");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "[EO CAMERA] RTSP Disconnect Error : " + ex.Message);

                return ControllerResult.Failed(
                    "EO RTSP Disconnect Error : " + ex.Message);
            }

        }

        /// <summary>
        /// [EO] Frame 수신 결과 생성
        /// </summary>
        internal EoCameraControllerResult CreateFrameResult(
            BitmapSource frame)
        {
            return EoCameraControllerResult
                .FrameReceived(
                    frame);
        }

        /// <summary>
        /// [EO] 상태 변경 결과 생성
        /// </summary>
        internal EoCameraControllerResult CreateStatusResult(
            string statusText)
        {
            bool? isConnected =
                null;

            bool shouldStartReconnect =
                false;

            string operationModeText =
                null;

            if (statusText == "EO RTSP Connected")
            {
                isConnected =
                    true;

                operationModeText =
                    "CAMERA READY";
            }
            else if (statusText == "EO RTSP Error" ||
                     statusText == "EO RTSP Connect Failed")
            {
                isConnected =
                    false;

                shouldStartReconnect =
                    true;

                operationModeText =
                    "CAMERA RECONNECTING...";
            }

            return EoCameraControllerResult
                .StatusChanged(
                    statusText,
                    isConnected,
                    shouldStartReconnect,
                    operationModeText);
        }
        #endregion
    }

}
