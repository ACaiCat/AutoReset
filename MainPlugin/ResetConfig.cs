using System;
using System.Collections.Generic;
using IL.Terraria;
using Newtonsoft.Json;

namespace AutoReset.MainPlugin
{
    [JsonObject]
    public class ResetConfig
    {
        public enum WorldSize
        {
            Small = 1,
            Medium = 2,
            Large =3
        }
        public enum Difficulties
        {
            Normal = 1,
            Expert = 2,
            Master = 3,
            Creative = 4
        }
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        public class SetWorldConfig
        {
            [JsonProperty("地图种子")]
            public string? Seed = null;
            [JsonProperty("地图彩蛋")]
            public List<string> Special = new List<string>();
            [JsonProperty("地图名")]
            public string? Name = null;


        };
        public class AutoReset
        {
            [JsonProperty("生物ID")]
            public int NpcID = 50;
            [JsonProperty("需要击杀次数")]
            public int NeedKillCount = 50;
            [JsonProperty("已击杀次数")]
            public int KillCount = 0;
        }
        [JsonProperty("重置前指令")]
        public string[]? PreResetCommands;

        [JsonProperty("重置后指令")]
        public string[]? PostResetCommands;

        [JsonProperty("删除文件")]
        public string[]? DelFiles;

        [JsonProperty("替换文件")]
        public Dictionary<string, string>? Files;

        [JsonProperty("重置后SQL命令")]
        public string[]? SQLs;
        [JsonProperty("地图大小")]
        public WorldSize? Size = WorldSize.Large;
        [JsonProperty("世界难度")]
        public Difficulties? Difficulty = Difficulties.Master;
        [JsonProperty("地图预设")]
        public SetWorldConfig SetWorld = new();
        [JsonProperty("击杀重置")]
        public AutoReset KillToReset = new AutoReset();
        [JsonProperty("重置是否触发API")]
        public bool API = false;
        [JsonProperty("重置触发API")]
        public string HttpAPI = "http//XSB:8037/UploadServerDataFile?token=xxx&ServerNum=1";
    }
}
