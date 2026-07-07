namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Controller] 처리 결과
    /// 
    /// Controller에서 기능 처리 결과만 반환하고,
    /// 화면 상태 변경은 [MainViewModel]에서 수행하기 위해 사용한다.
    /// </summary>
    internal class ControllerResult
    {
        #region [Properties]

        /// <summary>
        /// 처리 성공 여부
        /// </summary>
        internal bool IsSuccess { get; private set; }

        /// <summary>
        /// 처리 결과 메시지
        /// </summary>
        internal string Message { get; private set; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Controller] 처리 결과 생성
        /// </summary>
        protected ControllerResult(
            bool isSuccess,
            string message)
        {
            IsSuccess =
                isSuccess;

            Message =
                message;
        }

        #endregion

        #region [Factory Methods]

        /// <summary>
        /// [Controller] 성공 결과 생성
        /// </summary>
        internal static ControllerResult Success(
            string message)
        {
            return new ControllerResult(
                true,
                message);
        }

        /// <summary>
        /// [Controller] 실패 결과 생성
        /// </summary>
        internal static ControllerResult Failed(
            string message)
        {
            return new ControllerResult(
                false,
                message);
        }

        #endregion
    }
}
