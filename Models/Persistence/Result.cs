namespace weather_wrapper.Models.Persistence
{
    public class Result<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        // High level info: Success, An error occured, Server failure
        public string Message { get; set; } = string.Empty;
        // Actual error: location cannot be empty
        public string? Error { get; set; }
        public string Path { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int StatusCode { get; set; }
        // Type of error: ValidationError, ValidationException
        public string? ErrorCode { get; set; }
    }

    public static class ResultFactory
    {
        // Success factory
        public static Result<T> Success<T>(T data, string path = "")
        {
            return new Result<T> 
            {
                Success = true,
                Data = data,
                Message = "Success",
                Path = path,
                Timestamp = DateTime.Now, // Maybe pass from users timeZone
                StatusCode = 200
            };
        }

        public static Result<T> Error<T> (string error, string path, int statusCode = 400, string errorCode = null)
        {
            return new Result<T> 
            {
                Success = false,
                //Data = default(T),
                Message = "An error occured",
                Error = error,
                ErrorCode = errorCode,
                Path = path,
                Timestamp = DateTime.Now,
                StatusCode = statusCode
            };
        }

        public static Result<T> Exception<T> (string error, string path, int statusCode = 500, string errorCode = null)
        {
            return new Result<T> 
            {
                Success = false,
                //Data = default(T),
                Message = "Server Failure",
                Error = error,
                ErrorCode = errorCode,
                Path = path,
                Timestamp = DateTime.Now,
                StatusCode = statusCode
            };
        }

        public static Result<T> NotFound<T> (string message, string path)
        {
            return new Result<T> 
            {
                Success = false,
                //Data = data,
                Message = "Not Found",
                Error = $"{path} doesn't exist",
                Path = path,
                Timestamp = DateTime.Now,
                StatusCode = 404
            };
        }
    }
}
