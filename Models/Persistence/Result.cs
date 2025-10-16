namespace weather_wrapper.Models.Persistence
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }
        public int? StatusCode { get; }

        private Result(bool isSuccess, T value, string error, int? statusCode = null)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            StatusCode = statusCode;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null);

        public static Result<T> Failure(string error, int? statusCode = null) =>
            new Result<T>(false, default, error, statusCode);
    }
}
