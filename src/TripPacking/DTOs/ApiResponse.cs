namespace TripPacking.DTOs;

public class ApiResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponse Success(string msg = "success")
    {
        return new ApiResponse { Code = 200, Message = msg };
    }

    public static ApiResponse Fail(string msg, int code = 400)
    {
        return new ApiResponse { Code = code, Message = msg };
    }
}

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string msg = "success")
    {
        return new ApiResponse<T> { Code = 200, Message = msg, Data = data };
    }

    public static ApiResponse<T> Fail(string msg, int code = 400)
    {
        return new ApiResponse<T> { Code = code, Message = msg, Data = default };
    }
}
