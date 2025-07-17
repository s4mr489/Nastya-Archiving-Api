namespace Nastya_Archiving_project.Models.DTOs
{
    public class BaseResponseDTOs
    {
        public object Data { get; set; }
        public int StatusCode { get; set; }
        public string? Error { get; set; }

        public BaseResponseDTOs(object data, int statusCode, string? error = null)
        {
            Data = data;
            StatusCode = statusCode;
            Error = error;
        }
    }
}
