using System.IO;
using Newtonsoft.Json;

namespace Sherpa.Library
{
    public class FilePersistanceProvider<T> : IPersistanceProvider<T>
    {
        public string Path { get; set; }

        public FilePersistanceProvider(string path)
        {
            Path = path;
        }

        public void Save(T poco)
        {
            var result = JsonConvert.SerializeObject(poco, Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });
            File.WriteAllText(Path, result);
        }

        public T Load()
        {
            using (var reader = new StreamReader(Path))
            {
                var json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}
