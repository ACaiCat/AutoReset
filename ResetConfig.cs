using System;
using System.Collections.Generic;
using IL.Terraria;
using Newtonsoft.Json;

namespace ChromeAutoReset
{
	[JsonObject]
	public class ResetConfig
	{
		public string ToJson()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
        public class SetWorld
        {
            public string ?Seed = null;
            public List<string> 彩蛋 = new List<string>();
            public string ?name = null;

        };
        public class AutoReset
        {
            public int NpcID = 50;
            public int 需要击杀次数 = 50;

			public int 已击杀次数 = 0;
        }  
        [JsonProperty("重置前指令")]
		public string[] ?PreResetCommands;

		[JsonProperty("重置后指令")]
		public string[] ?PostResetCommands;

        [JsonProperty("删除文件")]
        public Dictionary<string, string>? DelFiles;

        [JsonProperty("替换文件")]
		public Dictionary<string, string> ?Files;

		[JsonProperty("重置后SQL命令")]
		public string[] ?SQLs;

        [JsonProperty("地图预设")]
        public SetWorld 预设 = new SetWorld();
        [JsonProperty("击杀重置")]
        public AutoReset 自动击杀重置 = new AutoReset();

        [JsonProperty("重置触发API")]
        public string API = "";
    }
}
