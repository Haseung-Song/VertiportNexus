using System;
using System.Windows.Controls;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면 - TextBox Utility
    /// 
    /// TextBox 입력 제한 / 범위 보정에서 사용하는
    /// 공통 보조 메서드를 관리한다.
    /// </summary>
    public partial class MainWindow
    {
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
        /// 사용자 입력 완료 후
        /// 지정된 최소 / 최대 범위 내 값으로 보정한다.
        ///
        /// 유효하지 않은 입력값은
        /// 기본값 [0]으로 설정한다.
        ///
        /// 결과 값은
        /// 소수점 둘째 자리까지 표시한다.
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
                    "0.00";

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

            value =
                Math.Round(
                    value,
                    2,
                    MidpointRounding.AwayFromZero);

            textBox.Text =
                value.ToString(
                    "F2");
        }

        /// <summary>
        /// [정수] 입력값 범위 보정
        ///
        /// 사용자 입력 완료 후
        /// 지정된 최소 / 최대 범위 내 값으로 보정한다.
        ///
        /// 유효하지 않은 입력값은
        /// 기본값 [0]으로 설정한다.
        ///
        /// 결과 값은
        /// 정수 형태로 표시한다.
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
