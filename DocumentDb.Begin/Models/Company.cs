using Newtonsoft.Json;

namespace DocumentDb.Models
{
    public class Company
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Location { get; set; }

        public Beer[] Beers { get; set; }
    }
}