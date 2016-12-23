using Newtonsoft.Json;

namespace DocSearch.Models
{
    public class DocumentBlob
    {
        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("metadata_storage_size")]
        public int? Bytes { get; set; }

        [JsonProperty("metadata_storage_name")]
        public string FileName { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }
    }
}