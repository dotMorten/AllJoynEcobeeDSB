using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Ecobee
{
    public partial class EcobeeClient
    {
        private const string endpoint = "https://api.ecobee.com/";
        private const string version = "1";
        private static string ApiEndpoint = $"{endpoint}{version}/";
        private readonly string _appKey;
        private readonly HttpClient _client;
        public EcobeeClient(string appKey)
        {
            _client = new HttpClient();
            _appKey = appKey;
        }

        public EcobeeClient(string appKey, string refreshToken, string accessToken = null)
        {
            _client = new HttpClient();
            _appKey = appKey;
            _accessToken = accessToken;
            _refreshToken = refreshToken;
        }


        private Task<T> PostDataAsync<T>(string url, IDictionary<string,string> parameters = null, bool skipAuthentication = false)
        {
            return RequestDataAsync<T>(new HttpRequestMessage(HttpMethod.Post, url) { Content = parameters == null ? null : new FormUrlEncodedContent(parameters) }, skipAuthentication);
        }

        private Task<T> GetDataAsync<T>(string url, bool skipAuthentication = false)
        {
            return RequestDataAsync<T>(new HttpRequestMessage(HttpMethod.Get, url), skipAuthentication);
        }
        private Task<T> GetDataAsync<T>(string url, object jsonParameter)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            if (jsonParameter != null)
            {
                var d = new DataContractJsonSerializer(jsonParameter.GetType());
                using (var ms = new System.IO.MemoryStream())
                {
                    d.WriteObject(ms, jsonParameter);
                    string json = Encoding.UTF8.GetString(ms.ToArray());
                    if (url.Contains("?") && !url.EndsWith("&") && !url.EndsWith("!"))
                        url += "&";
                    else
                        url += "?";
                    url += "json=" + System.Net.WebUtility.UrlEncode(json);
                    msg.RequestUri = new Uri(url);
                }
            }
            return RequestDataAsync<T>(msg);
        }

        private async Task<T> RequestDataAsync<T>(HttpRequestMessage message, bool skipAuthentication = false)
        {
            if (!skipAuthentication)
            {
                if (_accessToken == null && !string.IsNullOrEmpty(_refreshToken))
                    await RefreshTokenAsync(_refreshToken);
                if (_accessToken != null)
                {
                    message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                }
            }
            System.Diagnostics.Debug.WriteLine("***********\nRequesting " + message.RequestUri);
            var response = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
#if DEBUG
            string responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Received response: {(int)response.StatusCode} - {response.StatusCode}\n{responseContent}");
#endif
            if (response.IsSuccessStatusCode)
            {
                using (var str = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(T));
                    return (T)dcjs.ReadObject(str);
                }
            }
            else
            {
                using (var str = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(ErrorResult));
                    ErrorResult err = (ErrorResult)dcjs.ReadObject(str);
                    if((
                        (err.error == "authorization_expired") || err.status?.code == 14)&& 
                        !string.IsNullOrEmpty(_refreshToken))
                    {
                        await RefreshTokenAsync(_refreshToken).ConfigureAwait(false);
                        return await RequestDataAsync<T>(message).ConfigureAwait(false);
                    }
                    throw new EcobeeRequestException(err, response);
                }
            }
        }
    }
    
    [DataContract]
    public class ErrorResult
    {
        [DataMember]
        public Status status { get; set; }
        [DataMember]
        public string error { get; set; }
        [DataMember]
        public string error_description { get; set; }
        [DataMember]
        public string error_uri { get; set; }
    }

    public class EcobeeRequestException : Exception
    {
        internal EcobeeRequestException(ErrorResult result, HttpResponseMessage response) : base(result.error)
        {
            Description = result.error_description;
            Url = result.error_uri;
            Code = response.StatusCode;
        }
        public string Description { get; }
        public string Url { get; }
        public System.Net.HttpStatusCode Code { get; }
    }
}
