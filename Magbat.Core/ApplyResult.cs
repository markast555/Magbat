namespace Magbat.Core
{
    /// <summary>
    /// Результат применения
    /// </summary>
    public class ApplyResult
    {
        public ApplyError Error { get; private set; }
        public bool Success => Error == ApplyError.None;

        private ApplyResult() { }

        public static ApplyResult Ok() => new ApplyResult();
        public static ApplyResult Fail(ApplyError error) => new ApplyResult { Error = error };
    }
}