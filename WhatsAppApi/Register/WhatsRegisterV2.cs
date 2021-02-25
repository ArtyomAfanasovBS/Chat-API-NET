﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WhatsAppApi.Parser;
using WhatsAppApi.Settings;
using libaxolotl.util;

namespace WhatsAppApi.Register
{
    public static class WhatsRegisterV2
    {
        public static string GenerateIdentity(string phoneNumber, string salt = "")
        {
            return (phoneNumber + salt).Reverse().ToSHAString();
        }

        public static string GetToken(string number)
        {
            return WaToken.GenerateTokenAndroid(number);
            // return WaToken.GenerateToken(number);
        }

        public static bool RequestCode(string phoneNumber, out string password, string method = "sms", string id = null)
        {
            string response = string.Empty;
            return RequestCode(phoneNumber, out password, out response, method, id);
        }

        public static bool RequestCode(string phoneNumber, out string password, out string response, string method = "sms", string id = null)
        {
            string request = string.Empty;
            return RequestCode(phoneNumber, out password, out request, out response, method, id);
        }

        public static bool RequestCode(string phoneNumber, out string password, out string request, out string response, string method = "sms", string id = null)
        {
            response = null;
            password = null;
            request = null;
            var release = 0.ToString();
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    //auto-generate
                    id = GenerateIdentity(phoneNumber);
                }
                PhoneNumber pn = new PhoneNumber(phoneNumber);
                string token = System.Uri.EscapeDataString(WhatsRegisterV2.GetToken(pn.Number));

                byte[] sha256bytes = new byte[20]; new Random().NextBytes(sha256bytes);
                NameValueCollection QueryStringParameters = new NameValueCollection();
                QueryStringParameters.Add("cc", pn.CC);
                QueryStringParameters.Add("in", pn.Number);
                QueryStringParameters.Add("lg", pn.ISO639);
                QueryStringParameters.Add("lc", pn.ISO3166);
                QueryStringParameters.Add("mcc", pn.MCC);
                QueryStringParameters.Add("mnc", pn.MNC);
                QueryStringParameters.Add("sim_mcc", pn.MCC);
                QueryStringParameters.Add("sim_mnc", pn.MNC);
                QueryStringParameters.Add("method", method);
                QueryStringParameters.Add("reason", string.Empty);
                QueryStringParameters.Add("token", token);

                // authkey -- это публичный ключ от client_static_keypair.
                /*
                 * self.addParam("authkey", self.b64encode(config.client_static_keypair.public.data))
                 *         if config.client_static_keypair is None:
                                config.client_static_keypair = WATools.generateKeyPair()
                
                from consonance.structs.keypair import KeyPair
                 * @classmethod
                   def generateKeyPair(cls):
                   """
                   :return:
                   :rtype: KeyPair
                   """
                   return KeyPair.generate()
                 */
                QueryStringParameters.Add("authkey", libaxolotl.util.KeyHelper.generateIdentityKeyPair().getPublicKey().ToString());
                QueryStringParameters.Add("e_regid", );
                QueryStringParameters.Add("e_keytype", );
                QueryStringParameters.Add("e_ident", );
                QueryStringParameters.Add("e_skey_id", );
                QueryStringParameters.Add("e_skey_val", );
                QueryStringParameters.Add("e_skey_sig", );
                QueryStringParameters.Add("network_radio_type", "1");
                QueryStringParameters.Add("simnum", "1");
                QueryStringParameters.Add("hasinrc", "1");
                QueryStringParameters.Add("pid", new Random().Next(100, 9999).ToString());
                QueryStringParameters.Add("rc", release);
                QueryStringParameters.Add("id", id);

                // Old:
                /*QueryStringParameters.Add("mistyped", "6");
                QueryStringParameters.Add("s", "");
                QueryStringParameters.Add("copiedrc", "1");
                QueryStringParameters.Add("rcmatch", "1");
                QueryStringParameters.Add("rchash", BitConverter.ToString(HashAlgorithm.Create("sha256").ComputeHash(sha256bytes)));
                QueryStringParameters.Add("anhash", BitConverter.ToString(HashAlgorithm.Create("md5").ComputeHash(sha256bytes)));
                QueryStringParameters.Add("extexist", "1");
                QueryStringParameters.Add("extstate", "1");
                */




                NameValueCollection RequestHttpHeaders = new NameValueCollection();
                RequestHttpHeaders.Add("User-Agent", WhatsConstants.UserAgent);
                RequestHttpHeaders.Add("Accept", "text/json");

                response = GetResponse("https://v.whatsapp.net/v2/code", QueryStringParameters, RequestHttpHeaders);
                // request = String.Format("https://v.whatsapp.net/v2/code?method={0}&in={1}&cc={2}&id={3}&lg={4}&lc={5}&token={6}&sim_mcc=000&sim_mnc=000", method, pn.Number, pn.CC, id, pn.ISO639, pn.ISO3166, token, pn.MCC, pn.MNC);
                // response = GetResponse(request);
                password = response.GetJsonValue("pw");
                if (!string.IsNullOrEmpty(password))
                {
                    return true;
                }
                return (response.GetJsonValue("status") == "sent");
            }
            catch (Exception e)
            {
                response = e.Message;
                return false;
            }
        }

        public static string RegisterCode(string phoneNumber, string code, string id = null)
        {
            string response = string.Empty;
            return WhatsRegisterV2.RegisterCode(phoneNumber, code, out response, id);
        }

        public static string RegisterCode(string phoneNumber, string code, out string response, string id = null)
        {
            response = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    //auto generate
                    id = GenerateIdentity(phoneNumber);
                }
                PhoneNumber pn = new PhoneNumber(phoneNumber);

                string uri = string.Format("https://v.whatsapp.net/v2/register?cc={0}&in={1}&id={2}&code={3}", pn.CC, pn.Number, id, code);
                response = GetResponse(uri);
                if (response.GetJsonValue("status") == "ok")
                {
                    return response.GetJsonValue("pw");
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string RequestExist(string phoneNumber, string id = null)
        {
            string response = string.Empty;
            return RequestExist(phoneNumber, out response, id);
        }

        public static string RequestExist(string phoneNumber, out string response, string id = null)
        {
            response = string.Empty;
            try
            {
                if (String.IsNullOrEmpty(id))
                {
                    id = GenerateIdentity(phoneNumber);
                }
                PhoneNumber pn = new PhoneNumber(phoneNumber);

                string uri = string.Format("https://v.whatsapp.net/v2/exist?cc={0}&in={1}&id={2}&&lg={3}&lc={4}", pn.CC, pn.Number, id, pn.ISO639, pn.ISO3166);
                response = GetResponse(uri);
                if (response.GetJsonValue("status") == "ok")
                {
                    return response.GetJsonValue("pw");
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // BRIAN ADDED
        private static string GetResponse(string url, NameValueCollection QueryStringParameters = null, NameValueCollection RequestHeaders = null)
        {
            string ResponseText = null;
            using (WebClient client = new WebClient())
            {
                try
                {
                    if (RequestHeaders != null)
                    {
                        if (RequestHeaders.Count > 0)
                        {
                            foreach (string header in RequestHeaders.AllKeys)
                                client.Headers.Add(header, RequestHeaders[header]);
                        }
                    }
                    if (QueryStringParameters != null)
                    {
                        if (QueryStringParameters.Count > 0)
                        {
                            foreach (string parm in QueryStringParameters.AllKeys)
                                client.QueryString.Add(parm, QueryStringParameters[parm]);
                        }
                    }
                    byte[] ResponseBytes = client.DownloadData(url);
                    ResponseText = Encoding.UTF8.GetString(ResponseBytes);
                }
                catch (WebException exception)
                {
                    if (exception.Response != null)
                    {
                        var responseStream = exception.Response.GetResponseStream();

                        if (responseStream != null)
                        {
                            using (var reader = new System.IO.StreamReader(responseStream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            return ResponseText;
        }

        private static string GetResponse(string uri)
        {
            HttpWebRequest request = HttpWebRequest.Create(new Uri(uri)) as HttpWebRequest;
            request.KeepAlive = false;
            request.UserAgent = WhatsConstants.UserAgent;
            request.Accept = "text/json";
            using (var reader = new System.IO.StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadLine();
            }
        }

        private static string ToSHAString(this IEnumerable<char> s)
        {
            return new string(s.ToArray()).ToSHAString();
        }

        public static string UrlEncode(string data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                int i = (int)c;
                if (
                    (
                        i >= 0 && i <= 31
                    )
                    ||
                    (
                        i >= 32 && i <= 47
                    )
                    ||
                    (
                        i >= 58 && i <= 64
                    )
                    ||
                    (
                        i >= 91 && i <= 96
                    )
                    ||
                    (
                        i >= 123 && i <= 126
                    )
                    ||
                    i > 127
                )
                {
                    //encode 
                    sb.Append('%');
                    sb.AppendFormat("{0:x2}", (byte)c);
                }
                else
                {
                    //do not encode
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static string ToSHAString(this string s)
        {
            byte[] data = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(s));
            string str = Encoding.GetEncoding("iso-8859-1").GetString(data);
            str = WhatsRegisterV2.UrlEncode(str).ToLower();
            return str;
        }

        private static string ToMD5String(this IEnumerable<char> s)
        {
            return new string(s.ToArray()).ToMD5String();
        }

        private static string ToMD5String(this string s)
        {
            return string.Join(string.Empty, MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(s)).Select(item => item.ToString("x2")).ToArray());
        }


        private static void GetLanguageAndLocale(this CultureInfo self, out string language, out string locale)
        {
            string name = self.Name;
            int n1 = name.IndexOf('-');
            if (n1 > 0)
            {
                int n2 = name.LastIndexOf('-');
                language = name.Substring(0, n1);
                locale = name.Substring(n2 + 1);
            }
            else
            {
                language = name;
                switch (language)
                {
                    case "cs":
                        locale = "CZ";
                        return;

                    case "da":
                        locale = "DK";
                        return;

                    case "el":
                        locale = "GR";
                        return;

                    case "ja":
                        locale = "JP";
                        return;

                    case "ko":
                        locale = "KR";
                        return;

                    case "sv":
                        locale = "SE";
                        return;

                    case "sr":
                        locale = "RS";
                        return;
                }
                locale = language.ToUpper();
            }
        }

        private static string GetJsonValue(this string s, string parameter)
        {
            Match match;
            if ((match = Regex.Match(s, string.Format("\"?{0}\"?:\"(?<Value>.+?)\"", parameter), RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
            {
                return match.Groups["Value"].Value;
            }
            return null;
        }
    }
}
