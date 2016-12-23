using Newtonsoft.Json;
using System.Collections.Generic;

namespace DocSearch.Models
{
    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("folders")]
        public List<string> Folders { get; set; }
    }
}