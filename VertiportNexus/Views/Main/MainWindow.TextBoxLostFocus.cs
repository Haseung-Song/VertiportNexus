using System;
using System.Windows;
using System.Windows.Controls;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면 - TextBox LostFocus Event
    /// 
    /// Pan / Tilt / Zoom / Focus 입력 완료 후
    /// 허용 범위 내 값으로 보정한다.
    /// </summary>
    public partial class MainWindow
    {
        #region [TextBox LostFocus Event Methods]

        /// <summary>
        /// [Pan] 각도 입력 범위 보정
        /// 
        /// [Pan Absolute] 입력값이
        /// 최종 ICD 기준 [0 ~ 360] 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        private void PanAngle_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            ClampDecimalTextBoxValue(
                sender,
                0,
                360);
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

        /// <summary>
        /// [Zoom Ratio] 입력값 포커스 해제 처리
        ///
        /// [Zoom Ratio] 입력 완료 시,
        /// 
        /// 허용 범위 [1.0 ~ 66.0] 이내 값으로 보정한다.
        ///
        /// 1) 숫자가 아니면 [1.0] 적용
        /// 2) [1.0] 미만 입력 시 [1.0] 적용
        /// 3) [66.0] 초과 입력 시 [66.0] 적용
        /// 4) 소수점 첫째 자리까지 반올림하여 표시
        /// </summary>
        private void ZoomRatio_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            if (!double.TryParse(
                textBox.Text,
                out double zoomRatio))
            {
                textBox.Text =
                    "1.0";

                return;
            }

            if (zoomRatio < 1.0)
            {
                zoomRatio =
                    1.0;
            }
            else if (zoomRatio > 66.0)
            {
                zoomRatio =
                    66.0;
            }

            textBox.Text =
                Math.Round(
                    zoomRatio,
                    1)
                .ToString("F1");
        }
        #endregion
    }

}
