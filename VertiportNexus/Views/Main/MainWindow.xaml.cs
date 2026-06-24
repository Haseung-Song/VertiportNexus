using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면
    /// 
    /// [MainWindow.xaml]에 대한 상호 작용 논리를 처리한다.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region [Fields]

        /// <summary>
        /// [Main] 화면 [ViewModel]
        /// 
        /// [XAML]의 [Binding] 대상 객체이다.
        /// </summary>
        private readonly MainViewModel _viewModel = new MainViewModel();

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MainWindow] 생성자
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;
        }

        #endregion

        #region [Pan / Tilt Mouse Event Methods]

        /// <summary>
        /// [Pan] 좌측 버튼 [MouseDown]
        /// </summary>
        private void PanLeft_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartPanLeftMove();
        }

        /// <summary>
        /// [Pan] 우측 버튼 [MouseDown]
        /// </summary>
        private void PanRight_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartPanRightMove();
        }

        /// <summary>
        /// [Tilt] 위쪽 버튼 [MouseDown]
        /// </summary>
        private void TiltUp_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartTiltUpMove();
        }

        /// <summary>
        /// [Tilt] 아래쪽 버튼 [MouseDown]
        /// </summary>
        private void TiltDown_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartTiltDownMove();
        }

        #endregion

        #region [Zoom / Focus Mouse Event Methods]

        /// <summary>
        /// [Zoom] 확대 버튼 [MouseDown]
        /// </summary>
        private void ZoomIn_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartZoomInMove();
        }

        /// <summary>
        /// [Zoom] 축소 버튼 [MouseDown]
        /// </summary>
        private void ZoomOut_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartZoomOutMove();
        }

        /// <summary>
        /// [Focus] Near 버튼 [MouseDown]
        /// </summary>
        private void FocusNear_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartFocusNearMove();
        }

        /// <summary>
        /// [Focus] Far 버튼 [MouseDown]
        /// </summary>
        private void FocusFar_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _viewModel.StartFocusFarMove();
        }

        #endregion

        #region [Stop Mouse Event Methods]

        /// <summary>
        /// [MouseUp] / [MouseLeave] 공통 정지 처리
        /// 
        /// 화면 버튼을 통해 시작된 연속 이동을 정지한다.
        /// </summary>
        private void MoveStop_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            _viewModel.StopContinuousMove();
        }

        #endregion

        #region [TextBox Input Event Methods]

        /// <summary>
        /// [소수점 숫자] 입력 제한
        /// 
        /// [Pan] / [Tilt] 입력값에서
        /// 음수, 정수, 소수점 둘째 자리까지 허용한다.
        /// 
        /// 입력 중에는 범위 제한을 적용하지 않고,
        /// 범위 보정은 [LostFocus] 시점에 처리한다.
        /// </summary>
        private void DecimalNumberOnly_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                e.Handled =
                    true;

                return;
            }

            string newText =
                CreatePreviewText(
                    textBox,
                    e.Text);

            if (newText == "-" ||
                string.IsNullOrWhiteSpace(
                    newText))
            {
                e.Handled =
                    false;

                return;
            }

            e.Handled =
                !Regex.IsMatch(
                    newText,
                    @"^-?\d*\.?\d{0,2}$");
        }

        /// <summary>
        /// [Relative] 각도 입력 제한
        /// 
        /// [Pan Relative] / [Tilt Relative] 입력값에서
        /// 음수, 정수, 소수점 둘째 자리까지 허용한다.
        /// 
        /// 상대 이동은 현재 위치를 기준으로
        /// 이동량을 입력하는 방식이므로
        /// 절대 위치와 같은 범위 제한은 적용하지 않는다.
        /// </summary>
        private void RelativeDecimalNumberOnly_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            DecimalNumberOnly_PreviewTextInput(
                sender,
                e);
        }

        /// <summary>
        /// [정수] 입력 제한
        /// 
        /// [Zoom] / [Focus] 입력값에서
        /// 정수 입력만 허용한다.
        /// 
        /// 입력 중에는 범위 제한을 적용하지 않고,
        /// 범위 보정은 [LostFocus] 시점에 처리한다.
        /// </summary>
        private void IntegerNumberOnly_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                e.Handled =
                    true;

                return;
            }

            string newText =
                CreatePreviewText(
                    textBox,
                    e.Text);

            if (string.IsNullOrWhiteSpace(
                newText))
            {
                e.Handled =
                    false;

                return;
            }

            e.Handled =
                !Regex.IsMatch(
                    newText,
                    @"^\d*$");
        }

        #endregion

        #region [TextBox LostFocus Event Methods]

        /// <summary>
        /// [Pan] 각도 입력 범위 보정
        /// 
        /// [Pan Absolute] 입력값이
        /// [-180 ~ 180] 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        private void PanAngle_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            ClampDecimalTextBoxValue(
                sender,
                -180,
                180);
        }

        /// <summary>
        /// [Tilt] 각도 입력 범위 보정
        /// 
        /// [Tilt Absolute] 입력값이
        /// [-90 ~ 90] 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        private void TiltAngle_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            ClampDecimalTextBoxValue(
                sender,
                -90,
                90);
        }

        /// <summary>
        /// [Zoom] / [Focus] 위치 입력 범위 보정
        /// 
        /// [Zoom Position] / [Focus Position] 입력값이
        /// [0 ~ 1000] 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        private void PositionValue_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            ClampIntegerTextBoxValue(
                sender,
                0,
                1000);
        }

        #endregion

        #region [TextBox Utility Methods]

        /// <summary>
        /// [TextBox] 입력 예정 문자열 생성
        /// 
        /// 현재 선택 영역과 입력 문자를 기준으로
        /// 실제 반영될 문자열을 미리 계산한다.
        /// </summary>
        private string CreatePreviewText(
            TextBox textBox,
            string inputText)
        {
            return textBox.Text
                .Remove(
                    textBox.SelectionStart,
                    textBox.SelectionLength)
                .Insert(
                    textBox.SelectionStart,
                    inputText);
        }

        /// <summary>
        /// [소수점 숫자] 입력값 범위 보정
        /// 
        /// 입력된 숫자가 지정 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        private void ClampDecimalTextBoxValue(
            object sender,
            double min,
            double max)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            if (!double.TryParse(
                textBox.Text,
                out double value))
            {
                textBox.Text =
                    "0";

                return;
            }

            if (value < min)
            {
                value =
                    min;
            }

            if (value > max)
            {
                value =
                    max;
            }

            textBox.Text =
                value.ToString(
                    "F2");
        }

        /// <summary>
        /// [정수] 입력값 범위 보정
        /// 
        /// 입력된 숫자가 지정 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        private void ClampIntegerTextBoxValue(
            object sender,
            int min,
            int max)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            if (!int.TryParse(
                textBox.Text,
                out int value))
            {
                textBox.Text =
                    "0";

                return;
            }

            if (value < min)
            {
                value =
                    min;
            }

            if (value > max)
            {
                value =
                    max;
            }

            textBox.Text =
                value.ToString();
        }
        #endregion
    }

}
