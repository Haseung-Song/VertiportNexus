using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면 - TextBox Input Event
    /// 
    /// Pan / Tilt / Zoom / Focus 입력 TextBox의
    /// 문자 입력 제한 처리를 담당한다.
    /// </summary>
    public partial class MainWindow
    {
        #region [TextBox Input Event Methods]

        /// <summary>
        /// [Pan] 각도 입력 제한
        /// 
        /// [Pan Absolute] 입력값은 최종 ICD 기준
        /// [0 ~ 360] 범위의 양수 각도값이므로,
        /// 숫자와 소수점 둘째 자리까지만 허용한다.
        /// 
        /// 입력 중에는 최대 범위 제한을 적용하지 않고,
        /// 범위 보정은 [LostFocus] 시점에 처리한다.
        /// </summary>
        private void PanDecimalNumberOnly_PreviewTextInput(
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
                    @"^\d*\.?\d{0,2}$");
        }

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
        /// [Zoom Ratio] 입력 제한
        /// 
        /// [Zoom Ratio]는 [1.0 ~ 66.0] 배율 입력값이므로
        /// 숫자와 소수점만 허용하고,
        /// 소수점은 [1자리]까지만 입력 가능하도록 제한한다.
        /// </summary>
        private void ZoomRatio_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                e.Handled =
                    true;

                return;
            }

            string currentText =
                textBox.Text;

            int selectionStart =
                textBox.SelectionStart;

            int selectionLength =
                textBox.SelectionLength;

            string previewText =
                currentText.Remove(
                    selectionStart,
                    selectionLength)
                .Insert(
                    selectionStart,
                    e.Text);

            // 숫자와 소수점만 허용
            if (!Regex.IsMatch(
                previewText,
                @"^\d*\.?\d*$"))
            {
                e.Handled =
                    true;

                return;
            }

            // 소수점 [1자리]까지만 허용
            int dotIndex =
                previewText.IndexOf('.');

            if (dotIndex >= 0)
            {
                int decimalLength =
                    previewText.Length
                    - dotIndex
                    - 1;

                if (decimalLength > 1)
                {
                    e.Handled =
                        true;
                }

            }

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
    }

}
