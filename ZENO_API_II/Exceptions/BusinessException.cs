namespace ZENO_API_II.Exceptions
{
    public class BusinessException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        public BusinessException(string message, string errorCode, int statusCode = 400) 
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
} 