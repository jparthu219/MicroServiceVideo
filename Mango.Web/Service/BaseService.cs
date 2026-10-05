using Mango.Web.Models;
using Mango.Web.Utility;

namespace Mango.Web.Service
{
    public class BaseService : IService.IBaseService
    {
        private readonly IHttpClientFactory _httpClient;

        public BaseService(IHttpClientFactory httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ResponseDto?> SendAsync(RequestDto requestDto)
        {
            try
            {
                HttpClient httpClient = _httpClient.CreateClient("MangoAPI");
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
                httpRequestMessage.Headers.Add("Accept", "application/json");
                httpRequestMessage.RequestUri = new Uri(requestDto.Url);
                httpRequestMessage.Method = requestDto.ApiType switch
                {
                    SD.ApiType.GET => HttpMethod.Get,
                    SD.ApiType.POST => HttpMethod.Post,
                    SD.ApiType.PUT => HttpMethod.Put,
                    SD.ApiType.DELETE => HttpMethod.Delete,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (requestDto.Data != null)
                {
                    httpRequestMessage.Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(requestDto.Data),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );
                }
                HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
                string apiResponse = await response.Content.ReadAsStringAsync();
                ResponseDto? responseDto = System.Text.Json.JsonSerializer.Deserialize<ResponseDto>(apiResponse);
                return responseDto;
            }
            catch (Exception ex)
            {
                var dto = new ResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message.ToString()
                };
                return dto;
            }
        }
    }
}
