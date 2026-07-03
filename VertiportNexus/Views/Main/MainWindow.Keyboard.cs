using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면 - Keyboard Event
    /// 
    /// 운용 제어 화면의 방향키 입력을
    /// [MainViewModel]의 Pan / Tilt 키보드 제어로 전달한다.
    /// </summary>
    public partial class MainWindow
    {
        #region [Window Lifecycle Event Methods]

        /// <summary>
        /// [MainWindow] Loaded 처리
        /// 
        /// 방향키 입력을 Window에서 받을 수 있도록
        /// 초기 Keyboard Focus를 MainWindow에 설정한다.
        /// </summary>
        private void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Keyboard
                .Focus(
                    this);
        }

        #endregion

        #region [Pan / Tilt Keyboard Event Methods]

        /// <summary>
        /// [Window] 방향키 [KeyDown] 처리
        /// 
        /// 운용 제어 화면에서 방향키 입력을
        /// Pan / Tilt 연속 이동으로 전달한다.
        /// 
        /// TextBox 입력 중에는 방향키가 커서 이동 / 입력 보조 용도로 사용될 수 있으므로
        /// Pan / Tilt 제어로 처리하지 않는다.
        /// </summary>
        private void Window_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (IsTextBoxKeyboardFocus())
            {
                return;
            }

            if (!IsPanTiltDirectionKey(
                e.Key))
            {
                return;
            }

            _viewModel
                .HandlePanTiltKeyDown(
                    e.Key);

            e.Handled =
                true;
        }

        /// <summary>
        /// [Window] 방향키 [KeyUp] 처리
        /// 
        /// 해제된 방향키에 해당하는 Pan / Tilt 축만 정지할 수 있도록
        /// ViewModel로 KeyUp 이벤트를 전달한다.
        /// </summary>
        private void Window_PreviewKeyUp(
            object sender,
            KeyEventArgs e)
        {
            if (IsTextBoxKeyboardFocus())
            {
                return;
            }

            if (!IsPanTiltDirectionKey(
                e.Key))
            {
                return;
            }

            _viewModel
                .HandlePanTiltKeyUp(
                    e.Key);

            e.Handled =
                true;
        }

        /// <summary>
        /// [Pan / Tilt] 방향키 여부 확인
        /// </summary>
        /// <param name="key">
        /// 입력 키
        /// </param>
        /// <returns>
        /// 방향키 여부
        /// </returns>
        private bool IsPanTiltDirectionKey(
            Key key)
        {
            return key == Key.Left ||
                   key == Key.Right ||
                   key == Key.Up ||
                   key == Key.Down;
        }

        /// <summary>
        /// [TextBox] Keyboard Focus 여부 확인
        /// 
        /// 숫자 입력 TextBox 사용 중에는 방향키 입력을
        /// Pan / Tilt 제어로 사용하지 않기 위해 확인한다.
        /// </summary>
        /// <returns>
        /// TextBox Focus 여부
        /// </returns>
        private bool IsTextBoxKeyboardFocus()
        {
            return Keyboard.FocusedElement is TextBox;
        }
        #endregion
    }

}
