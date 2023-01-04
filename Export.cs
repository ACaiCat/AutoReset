using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.IO;
using TShockAPI;
using TShockAPI.DB;

namespace PlayerManager
{
    /// <summary>
    /// 导出玩家
    /// </summary>
    public class ExportPlayer
    {

        public const string path = "temp/reset/Players";

        /// <summary>
        /// 导出玩家
        /// </summary>
        /// <returns>(successMsg,errMsg)</returns>
        public static void CompressDirectoryZip(string folderPath, string zipPath)
        {
            DirectoryInfo directoryInfo = new(zipPath);

            if (directoryInfo.Parent != null)
            {
                directoryInfo = directoryInfo.Parent;
            }

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            ZipFile.CreateFromDirectory(folderPath, zipPath, CompressionLevel.Optimal,false);
        }
        public static Dictionary<int, string> GetAccount(string name)
        {
            return GetAccount(TShock.CharacterDB.database, name);
        }

        /// <summary>
        /// 获取用户（指定数据库）
        /// </summary>
        public static Dictionary<int, string> GetAccount(IDbConnection db, string name)
        {
            var dict = new Dictionary<int, string>();
            try
            {
                using var reader = TShock.DB.QueryReader("SELECT * FROM Users WHERE Username = @0", name);
                while (reader.Read())
                {
                    dict.Add(reader.Get<int>("ID"), reader.Get<string>("Username"));
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
            return dict;
        }
        public static FoundPlayer GetPlayer(string name, out string errMsg)
        {
            errMsg = "";
            var found = new FoundPlayer();

            List<TSPlayer> players = TShock.Players.Where(p => p != null && p.Active && p.Name == name).ToList();
            if (players.Count > 0)
            {
                // 在线玩家
                if (players.Count == 1)
                    found.SetOnline(players[0]);
                else
                    errMsg = $"找到多个匹配项-无法判断哪个是正确的：\n{string.Join(", ", players.Select(p => p.Name))}";
            }
            else
            {
                // 离线玩家
                var offline = GetAccount(name);
                if (offline.Count == 0)
                    errMsg = $"找不到名为 {name} 的玩家!";
                else if (offline.Count == 1)
                    found.SetOffline(offline.First().Key, offline.First().Value);
                else
                    errMsg = $"找到多个匹配项-无法判断哪个是正确的：\n{string.Join(", ", offline.Values)}";
            }
            return found;
        }
        /// <summary>
        /// 导出玩家
        /// </summary>
        /// <returns>[success, error, path]</returns>
        public static Dictionary<string, string> Export(string name)
        {
            string success = "";
            string plrFile = Path.Combine(path, $"{name}.plr");

            var found = GetPlayer(name, out string error);
            if (found.online)
            {
                if (string.IsNullOrEmpty(error))
                {
                    if (ExportOne(found.plr.TPlayer, plrFile).Result)
                        success = $"已导出在线玩家 {name} .（{plrFile}）";
                    else
                        error = "导出失败.";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(error))
                {
                    var data = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), found.ID);
                    if (data != null)
                    {
                        if (data.hideVisuals == null)
                            error = $"离线玩家 {name} 的数据不完整, 无法导出！";
                        else
                        {
                            if (ExportOne(ModifyData(name, data), plrFile).Result)
                                success = $"已导出离线玩家 {name} .（{plrFile}）";
                            else
                                error = "导出失败.";
                        }
                    }
                    else
                    {
                        error = "未能从数据库中获取到玩家数据.";
                    }
                }
            }

            return new() {
                { "success", success },
                { "error", error },
                { "path", path }
            };
        }




        /// <summary>
        /// 导出玩家
        /// </summary>
        /// <param name="name"></param>
        /// <returns>[success, error, path]</returns>
        public static Dictionary<string, List<string>> ExportAll()
        {
            List<string> successMsgs = new();
            List<string> errorMsgs = new();
            List<string> names = new();

            int successcount = 0;
            int faildcount = 0;
            string plr_dir = path;

            // 在线存档
            var savedlist = new List<string>();
            TShock.Players.Where(p => p != null && p.SaveServerCharacter()).ForEach(plr =>
            {
                savedlist.Add(plr.Name);
                string path1 = Path.Combine(plr_dir, plr.Name + ".plr");
                if (ExportOne(plr.TPlayer, path1).Result)
                {
                    successMsgs.Add($"已导出 {plr.Name} 的在线存档.");
                    successcount++;
                    names.Add(plr.Name);
                }
                else
                {
                    errorMsgs.Add($"导出 {plr.Name} 的在线存档时发生错误.");
                    faildcount++;
                }
            });

            // 离线存档
            var allaccount = TShock.UserAccounts.GetUserAccounts();
            allaccount.Where(acc => acc != null && !savedlist.Contains(acc.Name)).ForEach(acc =>
            {
                var data = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), acc.ID);
                if (data != null)
                {
                    if (data.hideVisuals != null)
                    {
                        string path2 = Path.Combine(plr_dir, acc.Name + ".plr");
                        if (ExportOne(ModifyData(acc.Name, data), path2).Result)
                        {
                            successMsgs.Add($"已导出 {acc.Name} 的存档.");
                            successcount++;
                            names.Add(acc.Name);
                        }
                        else
                        {
                            errorMsgs.Add($"导出 {acc.Name} 的存档时发生错误.");
                            faildcount++;
                        }
                    }
                    else
                    {
                        errorMsgs.Add($"玩家 {acc.Name} 的数据不完整, 已跳过.");
                    }
                }
            });
            successMsgs.Add($"操作完成. 成功: {successcount}, 失败: {faildcount}. \n导出位置：{plr_dir}");
            return new Dictionary<string, List<string>>{
                { "success", successMsgs },
                { "error", errorMsgs },
                { "path", new List<string>(){plr_dir} },
                { "names", names }
            };
        }

        private static async Task<bool> ExportOne(Player player, string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Player.cs Serialize();
                    //RijndaelManaged rijndaelManaged = new RijndaelManaged();
                    //using (CryptoStream cryptoStream = new CryptoStream(stream, rijndaelManaged.CreateEncryptor(Player.ENCRYPTION_KEY, Player.ENCRYPTION_KEY), CryptoStreamMode.Write))
                    Aes myAes = Aes.Create();
                    using (Stream stream = new FileStream(path, FileMode.Create))
                    {
                        using (CryptoStream cryptoStream = new(stream, myAes.CreateEncryptor(Player.ENCRYPTION_KEY, Player.ENCRYPTION_KEY), CryptoStreamMode.Write))
                        {
                            PlayerFileData playerFileData = new()
                            {
                                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                                Player = player,
                                _isCloudSave = false,
                                _path = path
                            };
                            Main.LocalFavoriteData.ClearEntry(playerFileData);
                            using (BinaryWriter binaryWriter = new(cryptoStream))
                            {
                                //230 1.4.0.5
                                //269 1.4.4.0
                                binaryWriter.Write(269);
                                playerFileData.Metadata.Write(binaryWriter);
                                binaryWriter.Write(player.name);
                                binaryWriter.Write(player.difficulty);
                                binaryWriter.Write(playerFileData.GetPlayTime().Ticks);
                                binaryWriter.Write(player.hair);
                                binaryWriter.Write(player.hairDye);
                                BitsByte bitsByte = 0;
                                for (int i = 0; i < 8; i++)
                                {
                                    bitsByte[i] = player.hideVisibleAccessory[i];
                                }
                                binaryWriter.Write(bitsByte);
                                bitsByte = 0;
                                for (int j = 0; j < 2; j++)
                                {
                                    bitsByte[j] = player.hideVisibleAccessory[j + 8];
                                }
                                binaryWriter.Write(bitsByte);
                                binaryWriter.Write(player.hideMisc);
                                binaryWriter.Write((byte)player.skinVariant);
                                binaryWriter.Write(player.statLife);
                                binaryWriter.Write(player.statLifeMax);
                                binaryWriter.Write(player.statMana);
                                binaryWriter.Write(player.statManaMax);
                                binaryWriter.Write(player.extraAccessory);
                                binaryWriter.Write(player.unlockedBiomeTorches);
                                binaryWriter.Write(player.UsingBiomeTorches);

                                binaryWriter.Write(player.ateArtisanBread);
                                binaryWriter.Write(player.usedAegisCrystal);
                                binaryWriter.Write(player.usedAegisFruit);
                                binaryWriter.Write(player.usedArcaneCrystal);
                                binaryWriter.Write(player.usedGalaxyPearl);
                                binaryWriter.Write(player.usedGummyWorm);
                                binaryWriter.Write(player.usedAmbrosia);

                                binaryWriter.Write(player.downedDD2EventAnyDifficulty);
                                binaryWriter.Write(player.taxMoney);

                                binaryWriter.Write(player.numberOfDeathsPVE);
                                binaryWriter.Write(player.numberOfDeathsPVP);

                                binaryWriter.Write(player.hairColor.R);
                                binaryWriter.Write(player.hairColor.G);
                                binaryWriter.Write(player.hairColor.B);
                                binaryWriter.Write(player.skinColor.R);
                                binaryWriter.Write(player.skinColor.G);
                                binaryWriter.Write(player.skinColor.B);
                                binaryWriter.Write(player.eyeColor.R);
                                binaryWriter.Write(player.eyeColor.G);
                                binaryWriter.Write(player.eyeColor.B);
                                binaryWriter.Write(player.shirtColor.R);
                                binaryWriter.Write(player.shirtColor.G);
                                binaryWriter.Write(player.shirtColor.B);
                                binaryWriter.Write(player.underShirtColor.R);
                                binaryWriter.Write(player.underShirtColor.G);
                                binaryWriter.Write(player.underShirtColor.B);
                                binaryWriter.Write(player.pantsColor.R);
                                binaryWriter.Write(player.pantsColor.G);
                                binaryWriter.Write(player.pantsColor.B);
                                binaryWriter.Write(player.shoeColor.R);
                                binaryWriter.Write(player.shoeColor.G);
                                binaryWriter.Write(player.shoeColor.B);
                                for (int k = 0; k < player.armor.Length; k++)
                                {
                                    binaryWriter.Write(player.armor[k].netID);
                                    binaryWriter.Write(player.armor[k].prefix);
                                }
                                for (int l = 0; l < player.dye.Length; l++)
                                {
                                    binaryWriter.Write(player.dye[l].netID);
                                    binaryWriter.Write(player.dye[l].prefix);
                                }
                                for (int m = 0; m < 58; m++)
                                {
                                    binaryWriter.Write(player.inventory[m].netID);
                                    binaryWriter.Write(player.inventory[m].stack);
                                    binaryWriter.Write(player.inventory[m].prefix);
                                    binaryWriter.Write(player.inventory[m].favorited);
                                }
                                for (int n = 0; n < player.miscEquips.Length; n++)
                                {
                                    binaryWriter.Write(player.miscEquips[n].netID);
                                    binaryWriter.Write(player.miscEquips[n].prefix);
                                    binaryWriter.Write(player.miscDyes[n].netID);
                                    binaryWriter.Write(player.miscDyes[n].prefix);
                                }
                                for (int num = 0; num < 40; num++)
                                {
                                    binaryWriter.Write(player.bank.item[num].netID);
                                    binaryWriter.Write(player.bank.item[num].stack);
                                    binaryWriter.Write(player.bank.item[num].prefix);
                                }
                                for (int num2 = 0; num2 < 40; num2++)
                                {
                                    binaryWriter.Write(player.bank2.item[num2].netID);
                                    binaryWriter.Write(player.bank2.item[num2].stack);
                                    binaryWriter.Write(player.bank2.item[num2].prefix);
                                }
                                for (int num3 = 0; num3 < 40; num3++)
                                {
                                    binaryWriter.Write(player.bank3.item[num3].netID);
                                    binaryWriter.Write(player.bank3.item[num3].stack);
                                    binaryWriter.Write(player.bank3.item[num3].prefix);
                                }
                                for (int num4 = 0; num4 < 40; num4++)
                                {
                                    binaryWriter.Write(player.bank4.item[num4].netID);
                                    binaryWriter.Write(player.bank4.item[num4].stack);
                                    binaryWriter.Write(player.bank4.item[num4].prefix);
                                    binaryWriter.Write(player.bank4.item[num4].favorited);
                                }
                                binaryWriter.Write(player.voidVaultInfo);
                                for (int num5 = 0; num5 < 44; num5++)
                                {
                                    if (Main.buffNoSave[player.buffType[num5]])
                                    {
                                        binaryWriter.Write(0);
                                        binaryWriter.Write(0);
                                    }
                                    else
                                    {
                                        binaryWriter.Write(player.buffType[num5]);
                                        binaryWriter.Write(player.buffTime[num5]);
                                    }
                                }
                                for (int num6 = 0; num6 < 200; num6++)
                                {
                                    if (player.spN[num6] == null)
                                    {
                                        binaryWriter.Write(-1);
                                        break;
                                    }
                                    binaryWriter.Write(player.spX[num6]);
                                    binaryWriter.Write(player.spY[num6]);
                                    binaryWriter.Write(player.spI[num6]);
                                    binaryWriter.Write(player.spN[num6]);
                                }
                                binaryWriter.Write(player.hbLocked);
                                for (int num7 = 0; num7 < player.hideInfo.Length; num7++)
                                {
                                    binaryWriter.Write(player.hideInfo[num7]);
                                }
                                binaryWriter.Write(player.anglerQuestsFinished);
                                for (int num8 = 0; num8 < player.DpadRadial.Bindings.Length; num8++)
                                {
                                    binaryWriter.Write(player.DpadRadial.Bindings[num8]);
                                }
                                for (int num9 = 0; num9 < player.builderAccStatus.Length; num9++)
                                {
                                    binaryWriter.Write(player.builderAccStatus[num9]);
                                }
                                binaryWriter.Write(player.bartenderQuestLog);
                                binaryWriter.Write(player.dead);
                                if (player.dead)
                                {
                                    binaryWriter.Write(player.respawnTimer);
                                }
                                long value = DateTime.UtcNow.ToBinary();
                                binaryWriter.Write(value);
                                binaryWriter.Write(player.golferScoreAccumulated);
                                SaveSacrifice(binaryWriter);
                                player.SaveTemporaryItemSlotContents(binaryWriter);
                                CreativePowerManager.Instance.SaveToPlayer(player, binaryWriter);
                                BitsByte bitsByte2 = default(BitsByte);
                                bitsByte2[0] = player.unlockedSuperCart;
                                bitsByte2[1] = player.enabledSuperCart;
                                binaryWriter.Write(bitsByte2);
                                binaryWriter.Write(player.CurrentLoadoutIndex);
                                for (int num10 = 0; num10 < player.Loadouts.Length; num10++)
                                {
                                    player.Loadouts[num10].Serialize(binaryWriter);
                                }

                                binaryWriter.Flush();
                                cryptoStream.FlushFinalBlock();
                                stream.Flush();

                            }
                        }
                    }
                    return true;
                }
                catch (Exception ex) { File.Delete(path); TShock.Log.ConsoleError(ex.Message); }
                return false;
            });
        }

        /// <summary>
        /// 导出物品研究
        /// </summary>
        /// <param name="writer"></param>
        public static void SaveSacrifice(BinaryWriter writer)
        {
            //player.creativeTracker.Save(binaryWriter);
            Dictionary<int, int> dictionary = TShock.ResearchDatastore.GetSacrificedItems();
            writer.Write(dictionary.Count);
            foreach (KeyValuePair<int, int> item in dictionary)
            {
                writer.Write(ContentSamples.ItemPersistentIdsByNetIds[item.Key]);
                writer.Write(item.Value);
            }
        }

        /// <summary>
        /// data 转 玩家
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static Player ModifyData(string name, PlayerData data)
        {
            Player player = new();
            if (data != null)
            {
                player.name = name;
                player.SpawnX = data.spawnX;
                player.SpawnY = data.spawnY;

                player.hideVisibleAccessory = data.hideVisuals;
                player.skinVariant = data.skinVariant ?? default;
                player.statLife = data.health;
                player.statLifeMax = data.maxHealth;
                player.statMana = data.mana;
                player.statManaMax = data.maxMana;
                player.extraAccessory = data.extraSlot == 1;

                player.difficulty = (byte)Main.GameModeInfo.Id;

                // 火把神
                player.unlockedBiomeTorches = data.unlockedBiomeTorches == 1;

                player.hairColor = data.hairColor ?? default;
                player.skinColor = data.skinColor ?? default;
                player.eyeColor = data.eyeColor ?? default;
                player.shirtColor = data.shirtColor ?? default;
                player.underShirtColor = data.underShirtColor ?? default;
                player.pantsColor = data.pantsColor ?? default;
                player.shoeColor = data.shoeColor ?? default;

                player.hair = data.hair ?? default;
                player.hairDye = data.hairDye;

                player.anglerQuestsFinished = data.questsCompleted;
                player.CurrentLoadoutIndex = data.currentLoadoutIndex;

                //player.numberOfDeathsPVE = data.numberOfDeathsPVE;
                //player.numberOfDeathsPVP = data.numberOfDeathsPVP;


                for (int i = 0; i < NetItem.MaxInventory; i++)
                {
                    // 0~49 背包   5*10
                    // 50、51、52、53 钱
                    // 54、55、56、57 弹药
                    // 59 ~68  饰品栏
                    // 69 ~78  社交栏
                    // 79 ~88  染料1
                    // 89 ~93  宠物、照明、矿车、坐骑、钩爪
                    // 94 ~98  染料2
                    // 99~138 储蓄罐
                    // 139~178 保险箱（商人）
                    // 179 垃圾桶
                    // 180~219 护卫熔炉
                    // 220~259 虚空保险箱
                    // 260~350 装备123
                    if (i < 59) player.inventory[i] = NetItem2Item(data.inventory[i]);
                    else if (i >= 59 && i < 79) player.armor[i - 59] = NetItem2Item(data.inventory[i]);
                    else if (i >= 79 && i < 89) player.dye[i - 79] = NetItem2Item(data.inventory[i]);
                    else if (i >= 89 && i < 94) player.miscEquips[i - 89] = NetItem2Item(data.inventory[i]);
                    else if (i >= 94 && i < 99) player.miscDyes[i - 94] = NetItem2Item(data.inventory[i]);
                    else if (i >= 99 && i < 139) player.bank.item[i - 99] = NetItem2Item(data.inventory[i]);
                    else if (i >= 139 && i < 179) player.bank2.item[i - 139] = NetItem2Item(data.inventory[i]);
                    else if (i == 179) player.trashItem = NetItem2Item(data.inventory[i]);
                    else if (i >= 180 && i < 220) player.bank3.item[i - 180] = NetItem2Item(data.inventory[i]);
                    else if (i >= 220 && i < 260) player.bank4.item[i - 220] = NetItem2Item(data.inventory[i]);

                    else if (i >= 260 && i < 280) player.Loadouts[0].Armor[i - 260] = NetItem2Item(data.inventory[i]);
                    else if (i >= 280 && i < 290) player.Loadouts[0].Dye[i - 280] = NetItem2Item(data.inventory[i]);

                    else if (i >= 290 && i < 310) player.Loadouts[1].Armor[i - 290] = NetItem2Item(data.inventory[i]);
                    else if (i >= 310 && i < 320) player.Loadouts[1].Dye[i - 310] = NetItem2Item(data.inventory[i]);

                    else if (i >= 320 && i < 340) player.Loadouts[2].Armor[i - 320] = NetItem2Item(data.inventory[i]);
                    else if (i >= 340 && i < 350) player.Loadouts[2].Dye[i - 340] = NetItem2Item(data.inventory[i]);
                }
            }
            return player;
        }

        public class FoundPlayer
        {
            // id从1开始，-1表示非ssc玩家
            // 此id，仅对db数据才有效
            public int ID = -1;
            public string Name = "";

            public bool online = false;

            public TSPlayer plr = null;

            public bool valid = false;

            public void SetOnline(TSPlayer p)
            {
                valid = true;
                online = true;

                plr = p;
                ID = p.Index;
                Name = p.Name;
            }
            public void SetOffline(int id, string name)
            {
                valid = true;
                online = false;

                ID = id;
                Name = name;

            }
        }
        public static Item NetItem2Item(NetItem item)
        {
            var i = new Item();
            i.SetDefaults(item.NetId);
            i.stack = item.Stack;
            i.prefix = item.PrefixId;
            return i;
        }


    }
}
