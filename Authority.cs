/*
 * =================================================================================
 * Unit for develop interoperation with Linkhub APIs.
 * Functionalities are authentication for Linkhub api products, and to support
 * several base infomation(ex. Remain point).
 *
 * This library coded with .Net framework 3.5, To Process JSON and HMACSHA1.
 * If you need any other version of framework, plz contact with below. 
 * 
 * http://www.linkhub.co.kr
 * Author : Kim Seongjun (pallet027@gmail.com)
 * Written : 2014-09-22
 * Thanks for your interest. 
 * =================================================================================
*/
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Json;
using System.Net;
using System.Security.Cryptography;

namespace Linkhub
{
    public class Authority
    {
        private const String APIVersion = "1.0";
        private const String ServiceURL = "https://auth.linkhub.co.kr";
        
        private String _LinkID;
        private String _SecretKey;
        
        public Authority(String LinkID, String SecretKey)
        {
            if (String.IsNullOrEmpty(LinkID)) throw new LinkhubException(-99999999, "NO LinkID");
            if (String.IsNullOrEmpty(SecretKey)) throw new LinkhubException(-99999999, "NO SecretKey");

            this._LinkID = LinkID;
            this._SecretKey = SecretKey;
        }

        public Token getToken(String ServiceID, String access_id, List<String> scope)
        {
            return getToken(ServiceID, access_id, scope, null);
        }

        public Token getToken(String ServiceID, String access_id, List<String> scope,String ForwardIP)
        {
            if (String.IsNullOrEmpty(ServiceID)) throw new LinkhubException(-99999999, "NO ServiceID");
            if (String.IsNullOrEmpty(access_id)) throw new LinkhubException(-99999999, "NO Access_id");
 
            Token result = new Token();

            String URI = ServiceURL + "/" + ServiceID + "/Token";

            String xDate = DateTime.UtcNow.ToString("s") + "Z";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URI);

            request.Headers.Add("x-lh-date", xDate);
            
            request.Headers.Add("x-lh-version", APIVersion);

            if (ForwardIP != null) request.Headers.Add("x-lh-forwarded", ForwardIP);

            TokenRequest _TR = new TokenRequest();

            _TR.access_id = access_id;
            _TR.scope = scope;

            String postData = stringify(_TR);

            String HMAC_target = "POST\n";
            HMAC_target += Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(postData))) + "\n";
            HMAC_target += xDate + "\n";
            if (ForwardIP != null) HMAC_target += ForwardIP + "\n";
            HMAC_target += APIVersion + "\n";
            HMAC_target += "/" + ServiceID + "/Token";
            HMACSHA1 hmac = new HMACSHA1(Convert.FromBase64String(_SecretKey));

            String bearerToken = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(HMAC_target)));

            request.Headers.Add("Authorization", "LINKHUB" + " "+ _LinkID + " " + bearerToken);
            
            request.Method = "POST";

            byte[] btPostDAta = Encoding.UTF8.GetBytes(postData);

            request.ContentLength = btPostDAta.Length;

            request.GetRequestStream().Write(btPostDAta,0,btPostDAta.Length);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stReadData = response.GetResponseStream();
                result = fromJson<Token>(stReadData);

            }
            catch (Exception we)
            {
                if (we is WebException &&  ((WebException)we).Response != null)
                {
                    Stream stReadData = ((WebException)we).Response.GetResponseStream();
                    JsonValue readData = parseJson(stReadData);
                    throw new LinkhubException(readData["code"], readData["message"]);
                }
                throw new LinkhubException(-99999999, we.Message);
            }

            return result;
        }

        public Double getBalance(String BearerToken, String ServiceID)
        {
            if (String.IsNullOrEmpty(ServiceID)) throw new LinkhubException(-99999999, "NO ServiceID");
            if (String.IsNullOrEmpty(BearerToken)) throw new LinkhubException(-99999999, "NO BearerToken");

            String URI = ServiceURL + "/" + ServiceID + "/Point";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URI);

            request.Headers.Add("Authorization", "Bearer" + " " + BearerToken);

            request.Method = "GET";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream stReadData = response.GetResponseStream();

                PointResult result = fromJson<PointResult>(stReadData);

                return double.Parse( result.remainPoint);

            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    Stream stReadData = we.Response.GetResponseStream();
                    JsonValue readData = parseJson(stReadData);
                    throw new LinkhubException(readData["code"], readData["message"]);
                }
                throw new LinkhubException(-99999999, we.Message);

            }

        }

        public Double getPartnerBalance(String BearerToken, String ServiceID)
        {
            if (String.IsNullOrEmpty(ServiceID)) throw new LinkhubException(-99999999, "NO ServiceID");
            if (String.IsNullOrEmpty(BearerToken)) throw new LinkhubException(-99999999, "NO BearerToken");

            String URI = ServiceURL + "/" + ServiceID + "/PartnerPoint";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URI);

            request.Headers.Add("Authorization", "Bearer" + " " + BearerToken);

            request.Method = "GET";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream stReadData = response.GetResponseStream();

                PointResult result = fromJson<PointResult>(stReadData);

                return double.Parse( result.remainPoint);

            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    Stream stReadData = we.Response.GetResponseStream();
                    JsonValue readData = parseJson(stReadData);
                    throw new LinkhubException(readData["code"], readData["message"]);
                }

                throw new LinkhubException(-99999999, we.Message);

            }
        }
        public T fromJson<T>(Stream jsonStream)
        {
            JsonValue jv = parseJson(jsonStream);

            return JsonObject.toGraph<T>(jv);
        }
        private JsonValue parseJson(Stream input)
        {
            return JsonValue.Load(input);
        }

        public string stringify(Object input)
        {
            return JsonObject.toJsonValue(input).ToString();
        }

        private class TokenRequest
        {
            public String access_id;
            public List<String> scope;
        }

        private class PointResult
        {
            public string remainPoint = "0";
        }
        
    }
}