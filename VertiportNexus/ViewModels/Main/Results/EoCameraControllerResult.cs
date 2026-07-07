using System.Windows.Media.Imaging;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [EO Camera] Controller 처리 결과
    /// </summary>
    internal sealed class EoCameraControllerResult : ControllerResult
    {
        #region [Properties]

        internal BitmapSource Frame { get; private set; }
        internal bool? IsConnected { get; private set; }
        internal bool ShouldStartReconnect { get; private set; }
        internal string OperationModeText { get; private set; }

        #endregion

        #region [Constructor]

        private EoCameraControllerResult(
            bool isSuccess,
            string message)
            : base(
                  isSuccess,
                  message)
        {
        }

        #endregion

        #region [Factory Methods]

        internal static EoCameraControllerResult FrameReceived(
            BitmapSource frame)
        {
            return new EoCameraControllerResult(
                true,
                "EO Camera Frame Received")
            {
                Frame = frame
            };
        }

        internal static EoCameraControllerResult StatusChanged(
            string statusText,
            bool? isConnected,
            bool shouldStartReconnect,
            string operationModeText)
        {
            return new EoCameraControllerResult(
                true,
                statusText)
            {
                IsConnected = isConnected,
                ShouldStartReconnect = shouldStartReconnect,
                OperationModeText = operationModeText
            };
        }

        internal new static EoCameraControllerResult Failed(
            string message)
        {
            return new EoCameraControllerResult(
                false,
                message);
        }

        #endregion
    }
}
