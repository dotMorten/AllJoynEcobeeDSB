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
       
        private string _accessToken;
        private string _refreshToken;
        private string _authCode;
       
        public bool IsAuthenticated
        {
            get
            {
                return !string.IsNullOrEmpty(_refreshToken) && !string.IsNullOrEmpty(_accessToken) && !string.IsNullOrEmpty(_authCode);
            }
        }

        public Task<PinRequestResult> BeginPinRequest(AppScope scope)
        {
            string s = scope.ToString();
            s = s.Substring(0, 1).ToLower() + s.Substring(1);
            string url = $"{endpoint}authorize?response_type=ecobeePin&client_id={_appKey}&scope={s}";
            return GetDataAsync<PinRequestResult>(url, true);
        }

        public async Task<TokenRequestResult> BeginWaitForPin(PinRequestResult result)
        {
            DateTime start = DateTime.Now;
            while (start.AddMinutes(result.expires_in) > DateTime.Now)
            {
                await Task.Delay(result.interval * 1000).ConfigureAwait(false);
                try
                {
                    var token = await RequestTokenAsync(result.code);
                    _authCode = result.code;
                    return token;
                }
                catch(EcobeeRequestException err)
                {
                    if(err.Message == "authorization_pending" || err.Message == "slow_down")
                    {
                        //
                    }
                    else
                    {
                        throw err;
                    }
                }
            }
            throw new TimeoutException();
        }
        private async Task<TokenRequestResult> RefreshTokenAsync(string refreshToken)
        {
            string url = $"{endpoint}token?grant_type=refresh_token&refresh_token={refreshToken}&client_id={_appKey}";
            var result = await PostDataAsync<TokenRequestResult>(url, null, true).ConfigureAwait(false);
            _refreshToken = result.refresh_token;
            _accessToken = result.access_token;
            RefreshTokenUpdated?.Invoke(this, _refreshToken);
            return result;
        }
        private async Task<TokenRequestResult> RequestTokenAsync(string authCode)
        {
            string url = $"{endpoint}token?grant_type=ecobeePin&code={authCode}&client_id={_appKey}";
            var result = await PostDataAsync<TokenRequestResult>(url, null, true).ConfigureAwait(false);
            _refreshToken = result.refresh_token;
            _accessToken = result.access_token;
            RefreshTokenUpdated?.Invoke(this, _refreshToken);
            return result;
        }

        public event EventHandler<string> RefreshTokenUpdated;
        
    }
    [DataContract]
    public class TokenRequestResult
    {
        [DataMember]
        public string access_token { get; set; }
        [DataMember]
        public string token_type { get; set; }
        [DataMember]
        public int expires_in { get; set; }
        [DataMember]
        public string refresh_token { get; set; }
        [DataMember]
        public string scope { get; set; }
    }
   

    [DataContract]
    public class PinRequestResult
    {
        /// <summary>
        /// The PIN a user enters in the web portal.
        /// </summary>
        [DataMember]
        public string ecobeePin { get; set; }
        /// <summary>
        /// The number of minutes until the PIN expires. Ensure you inform the user how much time they have.
        /// </summary>
        [DataMember]
        public int expires_in { get; set; }
        /// <summary>
        /// The authorization token needed to request the access and refresh tokens.
        /// </summary>
        [DataMember]
        public string code { get; set; }
        /// <summary>
        /// The requested Scope from the original request. This must match the original request.
        /// </summary>
        [DataMember]
        public string scope { get; set; }
        /// <summary>
        /// The minimum amount of seconds which must pass between polling attempts for a token.
        /// </summary>
        [DataMember]
        public int interval { get; set; }
    }

    [DataContract]
    public enum AppScope
    {
        /// <summary>
        /// Permits read-only access to user registered Smart thermostats.
        /// </summary>
        [EnumMember(Value = "smartRead")]
        SmartRead,
        /// <summary>
        /// Permits read-write access to user registered Smart thermostats.
        /// </summary>
        [EnumMember(Value = "smartWrite")]
        SmartWrite,
        /// <summary>
        /// Permits read-write access to EMS thermostats, honours EMS set hierarchy privileges.
        /// </summary>
        [EnumMember(Value = "ems")]
        Ems
    }
}
