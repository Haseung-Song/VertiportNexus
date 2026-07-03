using System.Windows.Input;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면 - Pan / Tilt Mouse Event
    /// 
    /// 운용 제어 화면의 Pan / Tilt 방향 버튼 입력을
    /// [MainViewModel]의 PTZ 제어 메서드로 전달한다.
    /// </summary>
    public partial class MainWindow
    {
        #region [Pan / Tilt Mouse Event Methods]

        /// <summary>
        /// [Pan] 좌측 버튼 [MouseDown]
        /// </summary>
        private void PanLeft_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartPanLeftMove();
        }

        /// <summary>
        /// [Pan] 우측 버튼 [MouseDown]
        /// </summary>
        private void PanRight_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartPanRightMove();
        }

        /// <summary>
        /// [Pan Left] / [Tilt Up] 대각선 버튼 [MouseDown]
        /// </summary>
        private void PanLeftTiltUp_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartPanLeftTiltUpMove();
        }

        /// <summary>
        /// [Pan Right] / [Tilt Up] 대각선 버튼 [MouseDown]
        /// </summary>
        private void PanRightTiltUp_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartPanRightTiltUpMove();
        }

        /// <summary>
        /// [Tilt] 위쪽 버튼 [MouseDown]
        /// </summary>
        private void TiltUp_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartTiltUpMove();
        }

        /// <summary>
        /// [Tilt] 아래쪽 버튼 [MouseDown]
        /// </summary>
        private void TiltDown_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartTiltDownMove();
        }

        /// <summary>
        /// [Pan Left] / [Tilt Down] 대각선 버튼 [MouseDown]
        /// </summary>
        private void PanLeftTiltDown_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartPanLeftTiltDownMove();
        }

        /// <summary>
        /// [Pan Right] / [Tilt Down] 대각선 버튼 [MouseDown]
        /// </summary>
        private void PanRightTiltDown_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel
                .StartPanRightTiltDownMove();
        }
        #endregion
    }

}
