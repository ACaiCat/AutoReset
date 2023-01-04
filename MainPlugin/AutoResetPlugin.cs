using System.Reflection;
using Newtonsoft.Json;
using Terraria;
using Terraria.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using Rests;
using System.Diagnostics;
using Google.Protobuf.WellKnownTypes;


namespace AutoReset.MainPlugin
{
    [ApiVersion(2, 1)]
    public class AutoResetPlugin : TerrariaPlugin
    {
        public AutoResetPlugin(Main game) : base(game)
        {
        }
        public override string Name
        {
            get
            {
                return "OneKeyReset";
            }
        }

        public override Version Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
        public override string Author
        {
            get
            {
                return "棱镜 & Cai";
            }
        }

        public override string Description
        {
            get
            {
                return "完全自动重置插件";
            }
        }

        public override void Initialize()
        {

            mkdir();

            bool flag = !Directory.Exists(FilePath);
            if (flag)
            {
                Directory.CreateDirectory(FilePath);
            }
            bool flag2 = !File.Exists(ConfigPath);
            if (flag2)
            {   
                config = new ResetConfig
                {
                    Size = ResetConfig.WorldSize.Large,
                    Difficulty= ResetConfig.Difficulties.Master,
                    KillToReset = new ResetConfig.AutoReset(),
                    SetWorld = new ResetConfig.SetWorldConfig(),
                    PreResetCommands = Array.Empty<string>(),
                    PostResetCommands = Array.Empty<string>(),
                    SQLs = new string[]
                    {
                        "DELETE FROM tsCharacter"
                    },
                    Files = new Dictionary<string, string>(),
                    DelFiles = Array.Empty<string>()
                };
                File.WriteAllText(ConfigPath, config.ToJson());
            }
            else
            {
                config = JsonConvert.DeserializeObject<ResetConfig>(File.ReadAllText(ConfigPath));
            }

            TShock.RestApi.Register(new SecureRestCommand("/AutoReset/GetData", GataData, "rest.autorest.admin"));
            Commands.ChatCommands.Add(new Command("reset.admin", new CommandDelegate(ResetCmd), new string[]
            {
                "reset",
                "重置世界"
            }));
            Commands.ChatCommands.Add(new Command("", new CommandDelegate(OnWho), new string[]
            {

                "who",
                "playing",
                "online"
            }));

            Commands.ChatCommands.Add(new Command("reset.admin", new CommandDelegate(ResetSetting), new string[]
            {
                "rs",
                "重置设置"
            }));
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin, int.MaxValue);
            ServerApi.Hooks.WorldSave.Register(this, OnWorldSave, int.MaxValue);
            ServerApi.Hooks.NpcKilled.Register(this, CountKill);
            GeneralHooks.ReloadEvent += delegate (ReloadEventArgs e)
            {
                bool flag3 = File.Exists(ConfigPath);
                if (flag3)
                {
                    config = JsonConvert.DeserializeObject<ResetConfig>(File.ReadAllText(ConfigPath));
                }
                else
                {
                    config = new ResetConfig
                    {
                        KillToReset = new ResetConfig.AutoReset(),
                        SetWorld = new ResetConfig.SetWorldConfig(),
                        PreResetCommands = Array.Empty<string>(),
                        PostResetCommands = Array.Empty<string>(),
                        SQLs = new string[]
                        {
                            "DELETE FROM tsCharacter"
                        },
                        Files = new Dictionary<string, string>()
                    };
                    File.WriteAllText(ConfigPath, config.ToJson());
                }
                e.Player.SendSuccessMessage("自动重置插件配置已重载");
            };
        }


        private object GataData(RestRequestArgs args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string base64String = Utils.FileToBase64String("temp/ServerData.zip");
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            return new RestObject()
            {
                {
                    "response",
                     base64String
                },
                {
                    "name",
                    Main.worldName
                },
                {
                    "time",
                    Math.Round(ts.TotalSeconds,2)
                }
            };
        }

        private void OnWho(CommandArgs args)
        {
            Status status = this.status;
            switch (status)
            {
                case Status.Export:
                    args.Player.SendInfoMessage("正在导出存档数据...");
                    break;
                case Status.Cleaning:
                    args.Player.SendInfoMessage("重置数据中，请稍后...");
                    break;
                case Status.Generating:
                    args.Player.SendInfoMessage("生成地图中:" + GetProgress());

                    break;
                case Status.Available:
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NpcKilled.Deregister(this, CountKill);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
                ServerApi.Hooks.WorldSave.Deregister(this, OnWorldSave);
            }
            base.Dispose(disposing);
        }

        private void CountKill(NpcKilledEventArgs args)
        {
            if (args.npc.netID == config.KillToReset.NpcID)
            {
                config.KillToReset.KillCount++;
                File.WriteAllText(ConfigPath, config.ToJson());
                TShock.Utils.Broadcast(string.Format($"[自动重置]服务器中已经击杀{Lang.GetNPCName(config.KillToReset.NpcID)}{config.KillToReset.KillCount}/{config.KillToReset.NeedKillCount}"), Microsoft.Xna.Framework.Color.Blue);
                if (config.KillToReset.NeedKillCount <= config.KillToReset.KillCount)
                {
                    Commands.HandleCommand(TSPlayer.Server, $"{TShock.Config.Settings.CommandSpecifier}reset");
                }
            }

        }

        public bool AddSeedWorld(string type)
        {
            switch (type) //醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith
            {
                case "醉酒世界":
                    config.SetWorld.Special.Add("醉酒世界");
                    break;

                case "NotTheBees":
                    config.SetWorld.Special.Add("NotTheBees");
                    break;

                case "ForTheWorthy":
                    config.SetWorld.Special.Add("ForTheWorthy");
                    break;

                case "Celebrationmk10":
                    config.SetWorld.Special.Add("Celebrationmk10");
                    break;

                case "永恒领域":
                    config.SetWorld.Special.Add("永恒领域");
                    break;

                case "NoTraps":
                    config.SetWorld.Special.Add("NoTraps");
                    break;

                case "Remix":
                    config.SetWorld.Special.Add("Remix");
                    break;

                case "Zenith":
                    config.SetWorld.Special.Add("Zenith");
                    break;

                default:

                    return false;
            }
            File.WriteAllText(ConfigPath, config.ToJson());
            return true;

        }

        public bool DelSeedWorld(string type)
        {
            switch (type) //醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith
            {
                case "醉酒世界":
                    config.SetWorld.Special.Remove("醉酒世界");
                    break;

                case "NotTheBees":
                    config.SetWorld.Special.Remove("NotTheBees");
                    break;

                case "ForTheWorthy":
                    config.SetWorld.Special.Remove("ForTheWorthy");
                    break;

                case "Celebrationmk10":
                    config.SetWorld.Special.Remove("Celebrationmk10");
                    break;

                case "永恒领域":
                    config.SetWorld.Special.Remove("永恒领域");
                    break;

                case "NoTraps":
                    config.SetWorld.Special.Remove("NoTraps");
                    break;

                case "Remix":
                    config.SetWorld.Special.Remove("Remix");
                    break;

                case "Zenith":
                    config.SetWorld.Special.Remove("Zenith");
                    break;

                default:

                    return false;
            }
            File.WriteAllText(ConfigPath, config.ToJson());
            return true;

        }
        private void ResetCmd(CommandArgs args)
        {
            Task.Run(delegate ()
            {
                for (int i = 5; i >= 0; i--)
                {
                    TShock.Utils.Broadcast(string.Format("[自动重置]重置进程已启动,{0}s后所有玩家将被移出服务器", i), Microsoft.Xna.Framework.Color.Yellow);
                    Thread.Sleep(1000);
                }
                TShock.Players.ForEach(delegate (TSPlayer p)
                {
                    if (p != null)
                    {
                        p.Kick("服务器已开始重置", true, true, null, false);
                    }
                });
            }).Wait();
            status = Status.Export;
            Task.Run(delegate ()
            {
                if (File.Exists("temp/ServerData.zip"))
                {
                    File.Delete("temp/ServerData.zip");
                }
                File.Copy(Main.worldPathName, $"temp/reset/World/{Main.worldName}.wld", true);
                ExportPlayer.ExportAll();
                ExportPlayer.CompressDirectoryZip("temp/reset", "temp/ServerData.zip");
                Directory.Delete("temp/reset", true);
                mkdir();
            }).Wait();
            Utils.CallAPI();
            status = Status.Cleaning;
            config.PreResetCommands.ForEach(delegate (string c)
            {
                Commands.HandleCommand(TSPlayer.Server, c);
            });
            Main.WorldFileMetadata = null;
            Main.gameMenu = true;
            switch (config.Size)
            {
                case ResetConfig.WorldSize.Small:
                    Main.maxTilesX = 4200;
                    Main.maxTilesY = 1200;
                    break;
                case ResetConfig.WorldSize.Medium:
                    Main.maxTilesX = 6400;
                    Main.maxTilesY = 1800;
                    break;
                case ResetConfig.WorldSize.Large:
                    Main.maxTilesX = 8400;
                    Main.maxTilesY = 2400;
                    break;
            }
            switch (config.Difficulty)
            {
                case ResetConfig.Difficulties.Normal:
                    Main.GameMode = 0;
                    break;
                case ResetConfig.Difficulties.Expert:
                    Main.GameMode = 1;
                    break;
                case ResetConfig.Difficulties.Master:
                    Main.GameMode = 2;
                    break;
                case ResetConfig.Difficulties.Creative:
                    Main.GameMode = 3;
                    break;
            }
            string seed = "";
            if (args.Parameters.Count != 0)
            {
                for (int i = 0; i < args.Parameters.Count; i++)
                    seed += " " + args.Parameters[i];
            }
            else if (config.SetWorld.Seed == null)
            {
                Main.ActiveWorldFileData.SetSeedToRandom();
            }

            else
            {
                seed = config.SetWorld.Seed;
            }
            Main.ActiveWorldFileData.SetSeed(seed.Trim());
            WorldGen.generatingWorld = true;
            Main.rand = new UnifiedRandom(Main.ActiveWorldFileData.Seed);
            Main.menuMode = 10;
            generationProgress = new GenerationProgress();
            Task task = WorldGen.CreateNewWorld(generationProgress);
            status = Status.Generating;
            while (!task.IsCompleted)
            {
                TShock.Log.ConsoleInfo(GetProgress());
                Thread.Sleep(100);
            }
            status = Status.Cleaning;
            Main.rand = new UnifiedRandom((int)DateTime.Now.Ticks);
            WorldFile.LoadWorld(false);
            Main.dayTime = WorldFile._tempDayTime;
            Main.time = WorldFile._tempTime;
            Main.raining = WorldFile._tempRaining;
            Main.rainTime = WorldFile._tempRainTime;
            Main.maxRaining = WorldFile._tempMaxRain;
            Main.cloudAlpha = WorldFile._tempMaxRain;
            Main.moonPhase = WorldFile._tempMoonPhase;
            Main.bloodMoon = WorldFile._tempBloodMoon;
            Main.eclipse = WorldFile._tempEclipse;
            Main.gameMenu = false;
            try
            {
                if (config.SetWorld.Name != null)
                {
                    Main.worldName = config.SetWorld.Name;
                }
                PostReset();
                config.KillToReset.KillCount = 0;
                config.SetWorld = new ResetConfig.SetWorldConfig();
                File.WriteAllText(ConfigPath, config.ToJson());

            }
            finally
            {
                generationProgress = null;
                status = Status.Available;
            }
        }


        public static void mkdir()
        {
            if (!Directory.Exists("temp"))
            {
                Directory.CreateDirectory("temp");
            }
            if (!Directory.Exists("temp/reset"))
            {
                Directory.CreateDirectory("temp/reset");
            }
            if (!Directory.Exists("temp/reset/World"))
            {
                Directory.CreateDirectory("temp/reset/World");
            }
            if (!Directory.Exists("temp/reset/Players"))
            {
                Directory.CreateDirectory("temp/reset/Players");
            }
        }
        private void ResetSetting(CommandArgs args)
        {
            TSPlayer op = args.Player;
            #region help
            void ShowHelpText()
            {
                if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, op, out int pageNumber))
                    return;

                List<string> lines = new List<string> {
                    "/重置设置 info",
                    "/重置设置 Special add/del <Special类型>",
                    "有效的Special类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith",
                    "/重置设置 name <地图名>",
                    "/重置设置 seed <种子>"
                };

                PaginationTools.SendPage(
                    op, pageNumber, lines,
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "帮助 ({0}/{1})：",
                        FooterFormat = "输入 {0}重置设置 help {{0}} 查看更多".SFormat(Commands.Specifier)
                    }
                );
            }

            if (args.Parameters.Count == 0)
            {
                ShowHelpText();
                return;
            }

            string text;


            switch (args.Parameters[0].ToLowerInvariant())
            {
                // 帮助
                case "help":
                    ShowHelpText();
                    return;

                default:
                    ShowHelpText();
                    break;

                // 世界信息
                case "信息":
                case "info":
                    op.SendInfoMessage($"地图名: {(config.SetWorld.Name == null ? Main.worldName : config.SetWorld.Name)}\n" +
                                       $"种子: {(config.SetWorld.Seed == null ? "随机" : config.SetWorld.Seed)}\n" +
                                       $"Special: {(config.SetWorld.Special.Count == 0 ? "无" : string.Join("|", config.SetWorld.Special))}");
                    break;
                case "Special":
                    if (args.Parameters.Count < 2)
                    {
                        op.SendErrorMessage("用法错误!正确用法: /重置设置 Special add/del <Special类型>\n" +
                                                "有效的Special类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
                    }
                    switch (args.Parameters[1])
                    {
                        case "添加":
                        case "add":
                            if (config.SetWorld.Special.Contains(args.Parameters[2]))
                            {
                                op.SendErrorMessage("你已经添加过 " + args.Parameters[2] + " Special了");
                            }
                            else
                            {
                                if (AddSeedWorld(args.Parameters[2]))
                                {
                                    op.SendSuccessMessage("Special世界 " + args.Parameters[2] + " 添加成功");
                                }
                                else
                                {
                                    op.SendErrorMessage("无效的Special类型!\n" +
                                        "有效的Special类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
                                }
                            }
                            break;

                        case "删除":
                        case "del":
                            if (config.SetWorld.Special.Contains(args.Parameters[2]))
                            {
                                if (DelSeedWorld(args.Parameters[2]))
                                {
                                    op.SendSuccessMessage("Special世界 " + args.Parameters[2] + " 添加成功");
                                }
                                else
                                {
                                    op.SendErrorMessage("无效的Special类型!\n" +
                                        "有效的Special类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
                                }

                            }
                            else
                            {
                                op.SendErrorMessage("不存在 " + args.Parameters[2] + " Special");
                            }
                            break;
                        default:
                            op.SendErrorMessage("用法错误!正确用法: /重置设置 Special add/del <Special类型>\n" +
                                                "有效的Special类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
                            return;
                    }
                    break;
                case "名字":
                case "name":
                    if (args.Parameters.Count < 2)
                    {
                        config.SetWorld.Name = null;
                        File.WriteAllText(ConfigPath, config.ToJson());
                        op.SendSuccessMessage("世界名字已设置为跟随原世界");
                    }
                    else
                    {
                        config.SetWorld.Name = args.Parameters[1];
                        File.WriteAllText(ConfigPath, config.ToJson());
                        op.SendSuccessMessage("世界名字已设置为 " + args.Parameters[1]);
                    }
                    break;
                case "种子":
                case "seed":
                    if (args.Parameters.Count < 2)
                    {
                        config.SetWorld.Seed = null;
                        File.WriteAllText(ConfigPath, config.ToJson());
                        op.SendSuccessMessage("世界种子已设为随机");
                    }
                    else
                    {
                        string seed = "";
                        for (int i = 1; i < args.Parameters.Count; i++)
                            seed += " " + args.Parameters[i];
                        config.SetWorld.Seed = seed;
                        File.WriteAllText(ConfigPath, config.ToJson());
                        op.SendSuccessMessage("世界种子已设置为 " + seed);
                    }
                    break;

            }
        }
        private void PostReset()
        {
            config.SQLs.ForEach(delegate (string c)
            {
                TShock.DB.Query(c, Array.Empty<object>());
            });
            foreach (var i in config.DelFiles)
            {
                File.Delete(i);
            }
            foreach (KeyValuePair<string, string> keyValuePair in config.Files)
            {
                bool flag = !string.IsNullOrEmpty(keyValuePair.Value);
                if (flag)
                {
                    File.Copy(Path.Combine(FilePath, keyValuePair.Value), Path.Combine(Environment.CurrentDirectory, keyValuePair.Key), true);
                }
                else
                {
                    File.Delete(keyValuePair.Key);
                }
            }
            config.PostResetCommands.ForEach(delegate (string c)
            {
                Commands.HandleCommand(TSPlayer.Server, c);
            });
            LinqExt.ForEach(config.SetWorld.Special, delegate (string c)
            {

                switch (c)
                {
                    case "醉酒世界":
                        Main.drunkWorld = true;
                        break;

                    case "NotTheBees":
                        Main.notTheBeesWorld = true;
                        break;

                    case "ForTheWorthy":
                        Main.getGoodWorld = true;
                        break;

                    case "Celebrationmk10":
                        Main.tenthAnniversaryWorld = true;
                        break;

                    case "永恒领域":
                        Main.dontStarveWorld = true;
                        break;

                    case "NoTraps":
                        Main.noTrapsWorld = true;
                        break;

                    case "Remix":
                        Main.remixWorld = true;
                        break;

                    case "Zenith":
                        Main.zenithWorld = true;
                        break;
                }
            });
        }

        private string GetProgress()
        {
             return string.Format("{0:0.0%} - " + generationProgress.Message + " - {1:0.0%}", generationProgress.TotalProgress, generationProgress.Value);
        }

        private void OnServerJoin(JoinEventArgs args)
        {
            var plr = TShock.Players[args.Who];

            Status status = this.status;
            switch (status)
            {
                case Status.Export:
                    plr.Disconnect("正在导出存档数据...");
                    args.Handled = true;
                    break;
                case Status.Cleaning:
                    plr.Disconnect("重置数据中，请稍后...");
                    args.Handled = true;
                    break;
                case Status.Generating:
                    plr.Disconnect("生成地图中:" + GetProgress());
                    args.Handled = true;
                    break;
                case Status.Available:
                    break;
            }

        }
        private void OnWorldSave(WorldSaveEventArgs args)
        {
            args.Handled = status != Status.Available && Main.WorldFileMetadata == null;
        }

        private readonly string ConfigPath = Path.Combine(TShock.SavePath, "reset_config.json");

        private readonly string FilePath = Path.Combine(TShock.SavePath, "backup_files");

        public static ResetConfig config;

        private Status status;

        public GenerationProgress ?generationProgress = null;
    }
}
#endregion