namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Keyboard PTZ] Controller
    /// 
    /// 키보드 입력에 따른 PTZ 이동 처리 결과만 반환한다.
    /// </summary>
    internal sealed class KeyboardPtzController
    {
        #region [Keyboard Methods]

        internal ControllerResult HandleKeyDown()
        {
            return ControllerResult.Success(
                "Keyboard PTZ Key Down");
        }

        internal ControllerResult HandleKeyUp()
        {
            return ControllerResult.Success(
                "Keyboard PTZ Key Up");
        }
        #endregion
    }

}
