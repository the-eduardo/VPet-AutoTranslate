using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VPet.Plugin.AutoMTL
{
    class TranslatorGoogle : TranslatorBase
    {
        public static new string providerName = "Google Translate";
        public static new string providerId = "google";
        public static new Dictionary<string, string> providedLanguages => JsonConvert.DeserializeObject<Dictionary<string, string>>(Constants.googleLangJSON);

        public TranslatorGoogle(string srcLang, string dstLang, string cacheBase) : base(srcLang, dstLang, cacheBase)
        {
        }

        // https://stackoverflow.com/questions/50963296/c-sharp-google-translate-without-api-and-with-unicode
        // https://github.com/dmytrovoytko/SublimeText-Translate/blob/master/Translator.py
        public override async Task<string> TranslateStringAsync(string input)
        {
            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={srcLang}&tl={dstLang}&dt=t&q={Uri.EscapeDataString(input)}";

                using (HttpClient httpClient = new())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        JArray json = (JArray)JsonConvert.DeserializeObject(result);

                        string output = "";
                        foreach (JArray item in json[0])
                            output += item[0].Value<string>();

                        if (output == "")
                            return null;

                        return output;
                    }
                }

                return null;
            }
            catch (Exception exc)
            {
                string error = exc.ToString();
                // Handle the exception as needed.
                return null;
            }
        }
    }
}
