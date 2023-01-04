using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using ReLogic.Utilities;
using Rests;
using NuGet.Protocol.Plugins;
using System.Drawing;


namespace ChromeAutoReset
{
	[ApiVersion(2, 1)]
	public class AutoResetPlugin : TerrariaPlugin
	{
		public AutoResetPlugin(Main game) : base(game)
		{
		}		public override string Name
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
				return  Assembly.GetExecutingAssembly().GetName().Version;
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
			
            bool flag = !Directory.Exists(this.FilePath);
			if (flag)
			{
				Directory.CreateDirectory(this.FilePath);
			}
			bool flag2 = !File.Exists(this.ConfigPath);
			if (flag2)
			{
				this.config = new ResetConfig
				{
                    自动击杀重置 = new ResetConfig.AutoReset(),
                    预设 = new ResetConfig.SetWorld(),
                    PreResetCommands = new string[0],
					PostResetCommands = new string[0],
					SQLs = new string[]
					{
						"DELETE FROM tsCharacter"
					},
					Files = new Dictionary<string, string>()
				};
				File.WriteAllText(this.ConfigPath, this.config.ToJson());
			}
			else
			{
				this.config = JsonConvert.DeserializeObject<ResetConfig>(File.ReadAllText(this.ConfigPath));
			}
			Commands.ChatCommands.Add(new Command("reset.admin", new CommandDelegate(this.ResetCmd), new string[]
			{
				"reset",
				"重置世界"
			}));
            Commands.ChatCommands.Add(new Command("", new CommandDelegate(this.OnWho), new string[]
            {
				
                "who",
                "playing",
				"online"
            }));

            Commands.ChatCommands.Add(new Command("reset.admin", new CommandDelegate(this.ResetSetting), new string[]
            {	
				"rs",
                "重置设置"
            }));
            ServerApi.Hooks.ServerConnect.Register(this, OnServerConnect, int.MaxValue);
			ServerApi.Hooks.WorldSave.Register(this, OnWorldSave, int.MaxValue);
			ServerApi.Hooks.NpcKilled.Register(this, CountKill);
			GeneralHooks.ReloadEvent += delegate(ReloadEventArgs e)
			{
				bool flag3 = File.Exists(this.ConfigPath);
				if (flag3)
				{
					this.config = JsonConvert.DeserializeObject<ResetConfig>(File.ReadAllText(this.ConfigPath));
				}
				else
				{
					this.config = new ResetConfig
					{
                        自动击杀重置 = new ResetConfig.AutoReset(),
                        预设 = new ResetConfig.SetWorld(),
						PreResetCommands = Array.Empty<string>(),
						PostResetCommands = Array.Empty<string>(),
						SQLs = new string[]
						{
							"DELETE FROM tsCharacter"
						},
						Files = new Dictionary<string, string>()
					};
					File.WriteAllText(this.ConfigPath, this.config.ToJson());
				}
				e.Player.SendSuccessMessage("自动重置插件配置已重载");
			};
		}

		private void OnWho(CommandArgs args)
		{
            Status status = this.status;
            Status status2 = status;
            if (status2 != Status.Generating)
            {
                if (status2 == Status.Cleaning)
                {
                    args.Player.SendInfoMessage("重置数据中，请稍后...");
                }
            }
            else
            {
                args.Player.SendInfoMessage("生成地图中:" + this.GetProgress());
            }
        }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
                ServerApi.Hooks.NpcKilled.Deregister(this, CountKill);
                ServerApi.Hooks.ServerConnect.Deregister(this, new HookHandler<ConnectEventArgs>(this.OnServerConnect));
				ServerApi.Hooks.WorldSave.Deregister(this, new HookHandler<WorldSaveEventArgs>(this.OnWorldSave));
			}
			base.Dispose(disposing);
		}

		private void CountKill(NpcKilledEventArgs args)
		{
			if(args.npc.netID == this.config.自动击杀重置.NpcID)
			{
				this.config.自动击杀重置.已击杀次数++;
                File.WriteAllText(this.ConfigPath, this.config.ToJson());
                TShock.Utils.Broadcast(string.Format($"[自动重置]服务器中已经击杀{Lang.GetNPCName(this.config.自动击杀重置.NpcID)}{this.config.自动击杀重置.已击杀次数}/{this.config.自动击杀重置.需要击杀次数}"), Microsoft.Xna.Framework.Color.Blue);
                if (this.config.自动击杀重置.需要击杀次数 <= this.config.自动击杀重置.已击杀次数)
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
					this.config.预设.彩蛋.Add("醉酒世界");
                    break;

                case "NotTheBees":
                    this.config.预设.彩蛋.Add("NotTheBees");
                    break;

                case "ForTheWorthy":
                    this.config.预设.彩蛋.Add("ForTheWorthy");
                    break;

                case "Celebrationmk10":
                    this.config.预设.彩蛋.Add("Celebrationmk10");
                    break;

                case "永恒领域":
                    this.config.预设.彩蛋.Add("永恒领域");
                    break;

                case "NoTraps":
                    this.config.预设.彩蛋.Add("NoTraps");
                    break;

                case "Remix":
                    this.config.预设.彩蛋.Add("Remix");
                    break;

                case "Zenith":
                    this.config.预设.彩蛋.Add("Zenith");
                    break;

                default:
              
                    return false;
            }
            File.WriteAllText(this.ConfigPath, this.config.ToJson());
            return true;

        }

		public bool DelSeedWorld(string type)
        {
            switch (type) //醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith
            {
                case "醉酒世界":
                    this.config.预设.彩蛋.Remove("醉酒世界");
                    break;

                case "NotTheBees":
                    this.config.预设.彩蛋.Remove("NotTheBees");
                    break;

                case "ForTheWorthy":
                    this.config.预设.彩蛋.Remove("ForTheWorthy");
                    break;

                case "Celebrationmk10":
                    this.config.预设.彩蛋.Remove("Celebrationmk10");
                    break;

                case "永恒领域":
                    this.config.预设.彩蛋.Remove("永恒领域");
                    break;

                case "NoTraps":
                    this.config.预设.彩蛋.Remove("NoTraps");
                    break;

                case "Remix":
                    this.config.预设.彩蛋.Remove("Remix");
                    break;

                case "Zenith":
                    this.config.预设.彩蛋.Remove("Zenith");
                    break;

                default:

                    return false;
            }
            File.WriteAllText(this.ConfigPath, this.config.ToJson());
            return true;

        }
        private void ResetCmd(CommandArgs args)
		{
			Task.Run(delegate()
			{
				for (int i = 5; i >= 0; i--)
				{
					TShock.Utils.Broadcast(string.Format("[自动重置]重置进程已启动,{0}s后所有玩家将被移出服务器", i), Microsoft.Xna.Framework.Color.Yellow);
					Thread.Sleep(1000);
				}
				LinqExt.ForEach<TSPlayer>(TShock.Players, delegate (TSPlayer p)
				{
					if (p != null)
					{
						p.Kick("服务器已开始重置", true, true, null, false);
					}
				});
			}).Wait();
			this.status = Status.Export;
            Task.Run(delegate ()
            {
                if (File.Exists("temp/ServerData.zip"))
                {
                    File.Delete("temp/ServerData.zip");
                }
                File.Copy(Main.worldPathName, "temp/reset/World/reset.wld", true);
                PlayerManager.ExportPlayer.ExportAll();
                PlayerManager.ExportPlayer.CompressDirectoryZip("temp/reset", "temp/ServerData.zip");
                Directory.Delete("temp/reset", true);
                mkdir();
            }).Wait();
            this.status = Status.Cleaning;
			LinqExt.ForEach<string>(this.config.PreResetCommands, delegate (string c)
			{
				Commands.HandleCommand(TSPlayer.Server, c);
			});
			Main.WorldFileMetadata = null;
			Main.gameMenu = true;
			Main.maxTilesX = 8400;
			Main.maxTilesY = 2400;
			string seed = "";
            if (args.Parameters.Count != 0)
			{
				for (int i = 0; i < args.Parameters.Count; i++)
					seed += " " + args.Parameters[i];
            }
			else if (this.config.预设.Seed==null)
			{
                Main.ActiveWorldFileData.SetSeedToRandom();
            }

            else
			{
				seed = this.config.预设.Seed;
            }
			Main.ActiveWorldFileData.SetSeed(seed.Trim());
			WorldGen.generatingWorld = true;
			Main.rand = new UnifiedRandom(Main.ActiveWorldFileData.Seed);
			WorldGen.gen = true;
			Main.menuMode = 888;
			this.generationProgress = new GenerationProgress();
			Task task = Task.Factory.StartNew(new Action<object>(WorldGen.worldGenCallback), this.generationProgress);
			this.status = Status.Generating;
			while (!task.IsCompleted)
			{
				TShock.Log.ConsoleInfo(this.GetProgress());
				Thread.Sleep(100);
			}
			this.status = Status.Cleaning;
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
                if (this.config.预设.name != null)
                {
                    Main.worldName = this.config.预设.name;
                }
                this.PostReset();
				this.config.自动击杀重置.已击杀次数 = 0;
                this.config.预设 = new ResetConfig.SetWorld();
                File.WriteAllText(this.ConfigPath, this.config.ToJson());

            }
			finally
			{
				this.generationProgress = null;
				this.status = Status.Available;
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
					"/重置设置 彩蛋 add/del <彩蛋类型>",
					"有效的彩蛋类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith",
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


			#endregion
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
					op.SendInfoMessage($"地图名: {(this.config.预设.name == null ? Main.worldName : this.config.预设.name)}\n" +
									   $"种子: {(this.config.预设.Seed == null ? "随机" : this.config.预设.Seed)}\n" +
									   $"彩蛋: {(this.config.预设.彩蛋.Count==0 ? "无" : String.Join("|",this.config.预设.彩蛋))}");
					break;
				case "彩蛋":
					if (args.Parameters.Count < 2)
					{
                        op.SendErrorMessage("用法错误!正确用法: /重置设置 彩蛋 add/del <彩蛋类型>\n" +
                                                "有效的彩蛋类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
                    }
                    switch (args.Parameters[1])
					{
						case "添加":
						case "add":
							if (this.config.预设.彩蛋.Contains(args.Parameters[2]))
							{
                                op.SendErrorMessage("你已经添加过 "+ args.Parameters[2] + " 彩蛋了");
                            }
							else
							{
								if (AddSeedWorld(args.Parameters[2]))
								{
                                    op.SendSuccessMessage("彩蛋世界 "+ args.Parameters[2] + " 添加成功");
                                }
								else
								{
                                    op.SendErrorMessage("无效的彩蛋类型!\n" +
                                        "有效的彩蛋类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
                                }
							}
							break;

						case "删除":
						case "del":
                            if (this.config.预设.彩蛋.Contains(args.Parameters[2]))
                            {
                                if (DelSeedWorld(args.Parameters[2]))
                                {
                                    op.SendSuccessMessage("彩蛋世界 " + args.Parameters[2] + " 添加成功");
                                }
                                else
                                {
                                    op.SendErrorMessage("无效的彩蛋类型!\n" +
                                        "有效的彩蛋类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
                                }
                                
                            }
                            else
                            {
                                op.SendErrorMessage("不存在 " + args.Parameters[2] + " 彩蛋");
                            }
                            break;
						default:
							op.SendErrorMessage("用法错误!正确用法: /重置设置 彩蛋 add/del <彩蛋类型>\n" +
												"有效的彩蛋类型: 醉酒世界|NotTheBees|ForTheWorthy|Celebrationmk10|永恒领域|NoTraps|Remix|Zenith");
							return;
					}
					break;
                case "名字":
                case "name":
					if (args.Parameters.Count<2)
					{
						this.config.预设.name = null;
                        File.WriteAllText(this.ConfigPath, this.config.ToJson());
                        op.SendSuccessMessage("世界名字已设置为跟随原世界");
                    }
					else
					{
                        this.config.预设.name = args.Parameters[1];
                        File.WriteAllText(this.ConfigPath, this.config.ToJson());
                        op.SendSuccessMessage("世界名字已设置为 "+ args.Parameters[1]);
                    }
					break;
                case "种子":
                case "seed":
                    if (args.Parameters.Count < 2)
                    {
                        this.config.预设.Seed = null;
                        File.WriteAllText(this.ConfigPath, this.config.ToJson());
                        op.SendSuccessMessage("世界种子已设为随机");
                    }
                    else
                    {
						string seed = "";
						for (int i = 1; i < args.Parameters.Count; i++)
							seed += " "+args.Parameters[i];
                        this.config.预设.Seed = seed;
                        File.WriteAllText(this.ConfigPath, this.config.ToJson());
                        op.SendSuccessMessage("世界种子已设置为 " + seed);
                    }
                    break;

            }
		}
         private void PostReset()
		{
			LinqExt.ForEach<string>(this.config.SQLs, delegate(string c)
			{
				DbExt.Query(TShock.DB, c, Array.Empty<object>());
			});
			foreach (KeyValuePair<string, string> keyValuePair in this.config.Files)
			{
				bool flag = !string.IsNullOrEmpty(keyValuePair.Value);
				if (flag)
				{
					File.Copy(Path.Combine(this.FilePath, keyValuePair.Value), Path.Combine(Environment.CurrentDirectory, keyValuePair.Key), true);
				}
				else
				{
					File.Delete(keyValuePair.Key);
				}
			}
			LinqExt.ForEach<string>(this.config.PostResetCommands, delegate(string c)
			{
				Commands.HandleCommand(TSPlayer.Server, c);
			});
            LinqExt.ForEach<string>(this.config.预设.彩蛋, delegate (string c)
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
			return string.Format("{0:0.0%} - " + this.generationProgress.Message + " - {1:0.0%}", this.generationProgress.TotalProgress, this.generationProgress.Value);
		}

		private void OnServerConnect(ConnectEventArgs args)
		{
			Status status = this.status;
			Status status2 = status;
			var plr = TShock.Players[args.Who];
			if (status2 != Status.Generating)
			{
				if (status2 == Status.Cleaning)
				{
					plr.Disconnect("重置数据中，请稍后...");
					args.Handled = true;
				}
			}
			else if (status == Status.Export)
			{
                plr.Disconnect("正在导出人物存档... ");
                args.Handled = true;
            } 
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
		private void OnWorldSave(WorldSaveEventArgs args)
		{
			args.Handled = (this.status != Status.Available && Main.WorldFileMetadata == null);
		}

		private readonly string ConfigPath = Path.Combine(TShock.SavePath, "reset_config.json");

		private readonly string FilePath = Path.Combine(TShock.SavePath, "backup_files");

		private ResetConfig config;

		private Status status;

		private GenerationProgress generationProgress;
	}
}
