using System.Windows.Input;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면 - Zoom / Focus Mouse Event
    /// 
    /// 운용 제어 화면의 Zoom / Focus 버튼 입력을
    /// [MainViewModel]의 Camera 제어 메서드로 전달한다.
    /// </summary>
    public partial class MainWindow
    {
        #region [Zoom / Focus Mouse Event Methods]

        /// <summary>
        /// [Zoom] 확대 버튼 [MouseDown]
        /// </summary>
        private void ZoomIn_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartZoomInMove();
        }

        /// <summary>
        /// [Zoom] 축소 버튼 [MouseDown]
        /// </summary>
        private void ZoomOut_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartZoomOutMove();
        }

        /// <summary>
        /// [Focus] Near 버튼 [MouseDown]
        /// </summary>
        private void FocusNear_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartFocusNearMove();
        }

        /// <summary>
        /// [Focus] Far 버튼 [MouseDown]
        /// </summary>
        private void FocusFar_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartFocusFarMove();
        }
        #endregion
    }

}
