using System;
using System.Text;
using MatchingEngine.AzureStorage.Blob;
using MatchingEngine.Utils.Extensions;
using Newtonsoft.Json;

namespace MatchingEngine.DataAccess
{
    public static class GeneralSettingsReader
    {
        public static T ReadGeneralSettings<T>(string connectionString, Tuple<string, string> settingsLocations)
        {
            var settingsStorage = new AzureBlobStorage(connectionString);
            var settingsData =
                settingsStorage.GetAsync(settingsLocations.Item1, settingsLocations.Item2).RunSync().ToBytes();
            var str = Encoding.UTF8.GetString(settingsData);

            return JsonConvert.DeserializeObject<T>(str);
        }

        public static T ReadSettingsFromData<T>(string jsonData)
        {
            return JsonConvert.DeserializeObject<T>(jsonData);
        }
    }
}