namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [PTZ] Controller 처리 결과
    /// </summary>
    internal sealed class PtzControllerResult : ControllerResult
    {
        #region [Properties]

        internal double? PanUiZeroOffset { get; private set; }
        internal double? TiltUiZeroOffset { get; private set; }
        internal bool? IsMoving { get; private set; }

        #endregion

        #region [Constructor]

        private PtzControllerResult(
            bool isSuccess,
            string message)
            : base(
                  isSuccess,
                  message)
        {
        }

        #endregion

        #region [Factory Methods]

        internal static PtzControllerResult Success(
            string message,
            double? panUiZeroOffset = null,
            double? tiltUiZeroOffset = null,
            bool? isMoving = null)
        {
            return new PtzControllerResult(
                true,
                message)
            {
                PanUiZeroOffset = panUiZeroOffset,
                TiltUiZeroOffset = tiltUiZeroOffset,
                IsMoving = isMoving
            };
        }

        public new static PtzControllerResult Failed(
            string message)
        {
            return new PtzControllerResult(
                false,
                message);
        }

        #endregion
    }
}
