namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [PTZ Mode] Controller
    /// 
    /// PTZ 제어 모드 변경 결과만 반환하고,
    /// 실제 화면 상태 반영은 [MainViewModel]에서 수행한다.
    /// </summary>
    internal sealed class PtzModeController
    {
        #region [PTZ Mode Methods]

        internal ControllerResult SetAutoMode()
        {
            return ControllerResult.Success(
                "PTZ AUTO MODE");
        }

        internal ControllerResult SetManualMode()
        {
            return ControllerResult.Success(
                "PTZ MANUAL MODE");
        }

        #endregion
    }
}
