using Newtonsoft.Json;

namespace TimingShow.Update
{
    internal sealed class UpdateRelease
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("Version")]
        public string Version { get; set; }

        [JsonProperty("DownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("Changes")]
        public string Changes { get; set; }

        [JsonProperty("Sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("Size")]
        public long Size { get; set; }
    }

    internal sealed class UpdateManifest
    {
        [JsonProperty("Releases")]
        public UpdateRelease[] Releases { get; set; }
    }
}
