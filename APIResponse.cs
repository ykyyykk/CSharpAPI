public class APIResponse
{
     public bool Success { get; set; }
     public string Message { get; set; }
     public dynamic Data { get; set; }

     //TODOWarning: 還不能應用到所有地方 因為data的關係 有時間在改吧
     public APIResponse(bool success, string message, dynamic data = null)
     {
          Success = success;
          Message = message;
          Data = data;
     }
}