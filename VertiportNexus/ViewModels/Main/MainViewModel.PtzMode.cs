using System;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - PTZ Mode
    /// [AUTO] / [MANUAL] 제어 모드 변경 및 화면 표시 로직을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [PTZ Control Mode Methods]

        /// <summary>
        /// [PTZ] [AUTO] 모드 설정
        /// 
        /// 화면 버튼을 통해 [PTZ] 제어 모드를 [AUTO]로 변경한다.
        /// 
        /// 현재 단계에서는 실제 자동 추적 제어를 수행하지 않고,
        /// 이후 탐지 / 레이다 연동 시 자동 제어 허용 상태값으로 사용한다.
        /// </summary>
        private void SetPtzAutoMode()
        {
            SetPtzControlMode(
                "AUTO");
        }

        /// <summary>
        /// [PTZ] [MANUAL] 모드 설정
        /// 
        /// 화면 버튼을 통해 [PTZ] 제어 모드를 [MANUAL]로 변경한다.
        /// 
        /// 수동 버튼 기반 [Pan] / [Tilt] / [Zoom] / [Focus]
        /// 제어를 기본 운용 모드로 사용한다.
        /// </summary>
        private void SetPtzManualMode()
        {
            SetPtzControlMode(
                "MANUAL");
        }

        /// <summary>
        /// [PTZ] 제어 모드 설정
        /// 
        /// [AUTO] / [MANUAL] 값을 [CameraStateProvider]에 저장하고,
        /// 화면 표시값과 로그를 갱신한다.
        /// </summary>
        /// <param name="mode">
        /// 설정할 [PTZ] 제어 모드
        /// </param>
        private void SetPtzControlMode(
            string mode)
        {
            if (string.IsNullOrWhiteSpace(
                mode))
            {
                Console.WriteLine("[UI][PTZ_MODE] Set Failed : Mode is empty");
                return;
            }

            string normalizedMode =
                mode.Trim().ToUpper();

            if (normalizedMode != "AUTO" &&
                normalizedMode != "MANUAL")
            {
                Console.WriteLine("[UI][PTZ_MODE] Set Failed : Unsupported Mode : " + mode);
                return;
            }

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[UI][PTZ_MODE] Set Request");
            Console.WriteLine("[UI][PTZ_MODE] Mode : " + normalizedMode);
            ConsoleLogHelper.PrintLine();

            _cameraStateProvider.UpdatePtzControlMode(
                normalizedMode);
        }

        /// <summary>
        /// [PTZ] 제어 모드 변경 처리
        /// 
        /// [CameraStateProvider]에서 [AUTO] / [MANUAL] 모드가 변경되면
        /// [XAML] 바인딩 속성을 갱신한다.
        /// </summary>
        /// <param name="mode">
        /// 변경된 [PTZ] 제어 모드
        /// </param>
        private void OnPtzControlModeChanged(
            string mode)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                PtzControlModeText =
                    mode;

                Console.WriteLine("[UI][PTZ_MODE] Current Mode : " + PtzControlModeText);
            }));

        }
        #endregion
    }

}
