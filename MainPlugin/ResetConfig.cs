using Newtonsoft.Json;

namespace AutoReset.MainPlugin;

[JsonObject]
public class ResetConfig
{
    [JsonProperty("CaiBot服务器令牌")]
    public string CaiBotToken = "http//XSB:8037/UploadServerDataFile?token=xxx&ServerNum=1";
    

    [JsonProperty("替换文件")] public Dictionary<string, string>? Files;

    [JsonProperty("击杀重置")] public AutoReset KillToReset = new();

    [JsonProperty("重置后指令")] public string[]? PostResetCommands;

    [JsonProperty("重置前指令")] public string[]? PreResetCommands;

    [JsonProperty("重置提醒")] public bool ResetCaution;

    [JsonProperty("地图预设")] public SetWorldConfig SetWorld = new();

    [JsonProperty("重置后SQL命令")] public string[]? SqLs;

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public class SetWorldConfig
    {
        [JsonProperty("地图名")] public string? Name;

        [JsonProperty("地图种子")] public string? Seed;
    }

    public class AutoReset
    {
        [JsonProperty("已击杀次数")] public int KillCount;

        [JsonProperty("需要击杀次数")] public int NeedKillCount = 50;

        [JsonProperty("生物ID")] public int NpcId = 50;
    }
}