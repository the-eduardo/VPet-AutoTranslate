using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace VPet.Plugin.AutoMTL
{
    public abstract class TranslatorBase
    {
        // Constants
        private const string DefaultProviderName = "None";
        private const string DefaultProviderId = "none";
        private const string CacheFileExtension = ".json";

        // Provider-specific properties
        public static string ProviderName => DefaultProviderName;
        public static string ProviderId => DefaultProviderId;
        public static Dictionary<string, string> ProvidedLanguages => null;

        // Other configurable properties
        public long MsBetweenCalls { get; set; } = 20;
        public bool TitleCase { get; set; } = true;

        public readonly string SrcLang;
        public readonly string DstLang;

        // Private fields
        private readonly string _cacheBase;
        private readonly string _cacheFile;
        private readonly TextInfo _txtInfo;
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        private long _lastTime;

        protected TranslatorBase(string srcLang, string dstLang, string cacheBase)
        {
            _lastTime = 0;
            SrcLang = srcLang;
            DstLang = dstLang;
            _cacheBase = Path.Combine(cacheBase, "mtl");
            _cacheFile = Path.Combine(_cacheBase, $"{ProviderId}-{SrcLang}-{DstLang}{CacheFileExtension}");
            _txtInfo = new CultureInfo("en-US", false).TextInfo;
            InitCache();
        }

        private void InitCache()
        {
            if (!Directory.Exists(_cacheBase))
                Directory.CreateDirectory(_cacheBase);

            if (File.Exists(_cacheFile))
            {
                try
                {
                    using (var reader = new StreamReader(_cacheFile));
                        string json = reader.ReadToEnd();
                        _cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                   
                }
                catch (Exception ex)
                {
                    // Handle JSON deserialization exception gracefully
                    // Log the exception or take appropriate action
                    Console.WriteLine($"Failed to read cache: {ex.Message}");
                }
            }
        }

        private void SaveCache()
        {
            try
            {
                using (var writer = new StreamWriter(_cacheFile)) ;
                    string json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                    writer.Write(json);
                
            }
            catch (Exception ex)
            {
                // Handle JSON serialization exception gracefully
                // Log the exception or take appropriate action
                Console.WriteLine($"Failed to write cache: {ex.Message}");
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
            if (File.Exists(_cacheFile))
            {
                try
                {
                    File.Delete(_cacheFile);
                }
                catch (Exception ex)
                {
                    // Handle file deletion exception gracefully
                    // Log the exception or take appropriate action
                    Console.WriteLine($"Failed to delete cache: {ex.Message}");
                }
            }
        }

        public string Translate(string input)
        {
            string output;

            if (_cache.TryGetValue(input, out string cachedTranslation))
            {
                output = cachedTranslation;
            }
            else
            {
                long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long timeDiff = currentTime - _lastTime;

                if (timeDiff < MsBetweenCalls)
                {
                    Thread.Sleep((int)timeDiff);
                }

                _lastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                output = TranslateString(input);

                output = (output != null && !output.ToLower().Trim().Equals(input.ToLower().Trim())) ? (TitleCase ? _txtInfo.ToTitleCase(output) : output) : null;

                _cache[input] = output;
                SaveCache();
            }

            return output;
        }

        // Provider-specific translation logic in derived classes
        public abstract string TranslateString(string input);
    }
}
