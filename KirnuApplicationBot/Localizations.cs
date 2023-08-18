using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace KirnuApplicationBot
{
    public static class Localizations
    {
        private static Dictionary<string, string> _localizations;

        public static void LoadLocalizations()
        {
            using (StreamReader r = new StreamReader("./localizations.json"))
            {
                _localizations = JsonConvert.DeserializeObject<Dictionary<string, string>>(r.ReadToEnd());
            }
        }

        public static string Localize(string key)
        {
            return _localizations[key];
        }
    }
}