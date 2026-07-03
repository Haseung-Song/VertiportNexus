using System;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Position Input
    /// 위치 제어 입력값 초기화 로직을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Position Input Initialize Methods]

        /// <summary>
        /// [위치 제어] 입력값 초기화
        /// 
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 위치 제어 입력칸을
        /// 기본값으로 초기화한다.
        /// 
        /// [Zoom Ratio]는 최소 배율 [1x] 기준으로 초기화하고,
        /// 실제 장비 위치값은 변경하지 않는다.
        /// </summary>
        private void ResetPositionInput()
        {
            PanAbsoluteValue =
                0;

            TiltAbsoluteValue =
                0;

            PanRelativeValue =
                0;

            TiltRelativeValue =
                0;

            ZoomPositionValue =
                0;

            ZoomRatioValue =
                1;

            FocusPositionValue =
                0;

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[UI][POSITION] Input Reset");
            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}
