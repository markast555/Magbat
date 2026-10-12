namespace Magbat.Core
{
    /// <summary>
    /// Результат применения
    /// </summary>
    public class ApplyResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static ApplyResult Ok() => new ApplyResult { Success = true };
        public static ApplyResult Fail(string error) => new ApplyResult { Success = false, Error = error };
    }
}