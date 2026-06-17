using System.Windows;
using System.Windows.Input;
using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// [Main] 화면 -> [ViewModel] : [XAML]의 [Binding] 연결
        /// </summary>
        private readonly MainViewModel vm =
            new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = vm;
        }

        /// <summary>
        /// [PAN] 좌측 버튼 [MouseDown]
        /// </summary>
        private void PanLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartPanLeftMove();
        }

        /// <summary>
        /// [PAN] 우측 버튼 [MouseDown]
        /// </summary>
        private void PanRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartPanRightMove();
        }

        /// <summary>
        /// [TILT] 위쪽 버튼 [MouseDown]
        /// </summary>
        private void TiltUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartTiltUpMove();
        }

        /// <summary>
        /// [TILT] 아래쪽 버튼 [MouseDown]
        /// </summary>
        private void TiltDown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartTiltDownMove();
        }

        /// <summary>
        /// [Zoom] 확대 버튼 [MouseDown]
        /// </summary>
        private void ZoomIn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartZoomInMove();
        }

        /// <summary>
        /// [Zoom] 축소 버튼 [MouseDown]
        /// </summary>
        private void ZoomOut_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartZoomOutMove();
        }

        /// <summary>
        /// [Focus] Near 버튼 [MouseDown]
        /// </summary>
        private void FocusNear_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartFocusNearMove();
        }

        /// <summary>
        /// [Focus] Far 버튼 [MouseDown]
        /// </summary>
        private void FocusFar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm?.StartFocusFarMove();
        }

        /// <summary>
        /// [MouseUp] / [MouseLeave] 공통 처리
        /// </summary>
        private void MoveStop_MouseUp(object sender, MouseEventArgs e)
        {
            vm?.StopContinuousMove();
        }

    }

}
