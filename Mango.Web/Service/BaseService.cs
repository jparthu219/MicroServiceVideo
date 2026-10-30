using System.Net;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Newtonsoft.Json;

namespace Mango.Web.Service
{
    public class BaseService : IService.IBaseService
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly ITokenProvider _tokenProvider;

        public BaseService(IHttpClientFactory httpClient, ITokenProvider tokenProvider)
        {
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;
        }

        public async Task<ResponseDto?> SendAsync(RequestDto requestDto, bool withBearer = true)
        {
            try
            {
                HttpClient httpClient = _httpClient.CreateClient("MangoAPI");
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
                httpRequestMessage.Headers.Add("Accept", "application/json");

                // Token
                if (withBearer)
                {
                    var token = _tokenProvider.GetToken();
                    if (!string.IsNullOrEmpty(token))
                    {
                        httpRequestMessage.Headers.Add("Authorization", $"Bearer {token}");
                    }
                }
                else if (!string.IsNullOrEmpty(requestDto.AccessToken))
                {
                    httpRequestMessage.Headers.Add("Authorization", $"Bearer {requestDto.AccessToken}");
                }

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
                        JsonConvert.SerializeObject(requestDto.Data),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );
                }
                HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return new() { IsSuccess = false, Message = "Not Found" };
                    case HttpStatusCode.Forbidden:
                        return new() { IsSuccess = false, Message = "Access Denied" };
                    case HttpStatusCode.Unauthorized:
                        return new() { IsSuccess = false, Message = "Unauthorized" };
                    case HttpStatusCode.InternalServerError:
                        return new() { IsSuccess = false, Message = "Internal Server Error" };
                    default:
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        ResponseDto? responseDto = JsonConvert.DeserializeObject<ResponseDto>(apiResponse);
                        return responseDto;
                }
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
