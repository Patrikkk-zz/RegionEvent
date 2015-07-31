using Microsoft.CSharp;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Extensions;
using TShockAPI.Hooks;

namespace RegionEvent
{
	[ApiVersion(1, 20)]
	public class RegionEvent : TerrariaPlugin
	{
		public override string Author
		{
			get { return "Patrikk"; }
		}
		public override string Description
		{
			get { return "Triggers actions when entering a region."; }
		}

		public override string Name
		{
			get { return "RegionEvent"; }
		}

		public override Version Version
		{
			get { return new Version(1, 0); }
		}
		public RegionEvent(Main game)
			: base(game)
		{
			Order = 50;
		}

		private DateTime lastUpdate = DateTime.Now;
		private DateTime lastUpdate2 = DateTime.Now;
		private DateTime lastUpdate3 = DateTime.Now;

		internal static Dictionary<Region, RegionStorage> regionStorage = new Dictionary<Region, RegionStorage>();
		internal static Dictionary<string, string> flagTypes = new Dictionary<string, string>() { 
		{"HEAL", "Heals player every x second with x value."}, // done
		{"MANA", "Gives 100 mana to the player every x second."}, // done
		{"PRIVATE", "Prevents not allowed players from entering region."}, // done
		{"PVP", "Forces player's PVP ON when inside the region."}, // done
		{"NOPVP", "Forces player's PVP OFF when inside the region."}, // done 
		{"TEMPGROUP", "Puts the player in defined temporary group while in region."}, // done
		{"NOMOB", "Prevents mobs from entering region."}, //done
		{"DEATH", "Instantly kills player when entering region."}, // done
		{"HURT", "Hurts player every x second with x value while in region."}, // done
		{"BANITEM", "Prevents player from using specific items while in region."}, //done
		{"BANTILE", "Prevents player from placing specific tiles while in region."}, //done
		{"BANPROJ", "Prevents player from using specific projectiles while in region."}, //done
		{"BANMOB", "Prevents specific mobs from entering region!"}, //done
		{"COMMAND", "Executes a command when player enters region."}, //done 
		{"PROMOTE","Promotes a player in x group to y group."},//done
		{"PORTAL", "Allows portals in the region."},
		{"NOPORTAL", "Does not allow portals in the region"}, //done
		{"GROUPONLY", "Allows only specific group to enter region."}, //done
		{"BUFF", "Buffs player with defined buffs while in region."},
		{"MESSAGE", "Displays message to player when entering region."}, //done
		{"MUTE", "Mutes player when entering region."} //done
		};
		internal static Dictionary<string, string[]> commands = new Dictionary<string, string[]>() {
		{"help", new string[]{"<command/page>" , "Display RegionEvent command information"}},
		{"setflag", new string[]{"<region> <flag>", "Set a flag on defined region."}},
		{"removeflag", new string[]{"<region> <flag>","Remove a flag from a region."}},
		{"delregion", new string[]{"<region>", "Remove all tags from region."}},
		{"list", new string[]{"<region/flag>", "List all regions with flags, or list available flags."}},
		{"info", new string[]{"<region/flag>", "Show information of an EventRegion, or of a flag."}},
		{"reload", new string[]{"", "Reloads RegionEvent databse."}},
		{"bantile", new string[]{"<add/remove> <region> <ID>","Manage region banned tile list."}},
		{"banitem", new string[]{"<add/remove> <region> <Name/ID>","Manage region banned item list."}},
		{"banmob", new string[]{"<add/remove> <region> <ID>","Manage region banned NPC list."}},
		{"banproj", new string[]{"<add/remove> <region> <ID>","Manage region banned projectile list."}}
		};

		internal static IDbConnection db;

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -1);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
			ServerApi.Hooks.NetGetData.Register(this, GetData);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);

			RegionHooks.RegionEntered += OnRegionEntered;
			RegionHooks.RegionLeft += OnRegionLeft;
			RegionHooks.RegionDeleted += OnRegionDeleted;

			HandlePlayer.InitGetDataHandler();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
				ServerApi.Hooks.NetGetData.Deregister(this, GetData);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);

				RegionHooks.RegionEntered -= OnRegionEntered;
				RegionHooks.RegionLeft -= OnRegionLeft;
				RegionHooks.RegionDeleted -= OnRegionDeleted;
			}
			base.Dispose(disposing);
		}
		private void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command("regionevent.use", EventCommands.REvent, "regionevent", "re"));

			if (!Directory.Exists("ServerPlugins\\RegionEvents"))
				Directory.CreateDirectory("ServerPlugins\\RegionEvents");

			DBConnect();
		}

		#region Hooks

		private void OnPostInit(EventArgs args)
		{
			ReloadDB();
		}

		private static void GetData(GetDataEventArgs args)
		{
			var type = args.MsgID;
			var player = TShock.Players[args.Msg.whoAmI];

			if (player == null)
			{
				args.Handled = true;
				return;
			}
			if (args.Handled == true)
				return;
			if (!player.ConnectionAlive)
			{
				args.Handled = true;
				return;
			}

			using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
			{
				try
				{
					if (HandlePlayer.HandlerGetData(type, player, data))
						args.Handled = true;
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError(ex.ToString());
				}
			}
		}

		private void OnUpdate(EventArgs args)
		{
			foreach (TSPlayer player in TShock.Players)
			{
				if (player == null)
					continue;

				var playerInfo = player.GetPlayerInfo();

				if (!playerInfo.inEventRegion)
				{
					if (((DateTime.Now - lastUpdate).TotalSeconds >= 3))
					{
						playerInfo.lastPos = player.LastNetPosition;
						lastUpdate = DateTime.Now;
					}
					continue;
				}

				bool bypassFlag = playerInfo.BypassFlag(player);

				if (playerInfo.regionStorage.flags.Contains("PRIVATE") && !bypassFlag)
				{
					if (((DateTime.Now - lastUpdate2).TotalSeconds >= 3))
					{
						if (player.CurrentRegion == playerInfo.regionStorage.region)
						{
							playerInfo.regionWarned++;
							if (playerInfo.regionWarned >= 2)
							{
								player.Spawn();
								if (!playerInfo.gotWarnMessage)
								{
									player.SendErrorMessage("No permission to enter private region!");
									playerInfo.gotWarnMessage = true;
								}
							}
						}
						lastUpdate2 = DateTime.Now;
					}
				}
				if (playerInfo.regionStorage.flags.Contains("GROUPONLY") && !bypassFlag)
				{
					if (((DateTime.Now - lastUpdate2).TotalSeconds >= 0.7))
					{
						if (player.CurrentRegion == playerInfo.regionStorage.region && !playerInfo.regionStorage.groupOnly.Contains(player.Group.Name))
						{
							playerInfo.regionWarned++;
							if (playerInfo.regionWarned >= 2)
							{
								player.Spawn();
								if (!playerInfo.gotWarnMessage)
								{
									player.SendErrorMessage("No permission to enter private region!");
									playerInfo.gotWarnMessage = true;
								}
							}
						}
						lastUpdate2 = DateTime.Now;
					}
				}
				playerInfo.Update(player);
			}

			DateTime now = DateTime.Now;
			if ((now - lastUpdate3).TotalSeconds > 0.5)
			{
				lock (Main.npc)
				{
					foreach (NPC npc in Main.npc)
					{
						if (!npc.active)
							continue;

						var regions = TShock.Regions.InAreaRegion((int)npc.position.X / 16, (int)npc.position.Y / 16);
						if (regions != null)
						{
							Region r = TShock.Regions.GetTopRegion(regions);
							if (r != null)
							{
								if (regionStorage.ContainsKey(r))
								{
									if (regionStorage[r].flags.Contains("NOMOB") || (regionStorage[r].flags.Contains("BANMOB") && regionStorage[r].bannedNPCs.Contains(npc.type)))
									{
										npc.active = false;
										NetMessage.SendData(23, -1, -1, "", npc.whoAmI, 0f, 0f, 0f, 0);
									}
								}
							}
						}
					}
				}
				lastUpdate3 = now;
			}
		}

		private void OnChat(ServerChatEventArgs args)
		{
			if (regionStorage == null)
				return;
			TSPlayer player = TShock.Players[args.Who];
			if (player == null)
				return;
			if (regionStorage.ContainsKey(player.CurrentRegion))
			{
				var playerInfo = player.GetPlayerInfo();

				if (playerInfo.regionStorage.flags.Contains("MUTE") && !playerInfo.BypassFlag(player))
				{
					player.SendInfoMessage("Shhhsss! You cannot speak in this region!");
					args.Handled = true;
					return;
				}
			}
		}

		private void OnRegionEntered(RegionHooks.RegionEnteredEventArgs args)
		{
			if (regionStorage == null)
				return;
			if (args.Player == null)
				return;
			TSPlayer player = args.Player;
			if (regionStorage.ContainsKey(args.Region))
			{
				args.Player.GetPlayerInfo().inEventRegion = true;
				args.Player.GetPlayerInfo().regionStorage = regionStorage[args.Region];
				args.Player.SendInfoMessage("You are in EventRegion {0}", args.Region.Name);
			}
		}
		private void OnRegionLeft(RegionHooks.RegionLeftEventArgs args)
		{
			if (regionStorage == null)
				return;
			if (regionStorage.ContainsKey(args.Region))
			{
				var playerInfo = args.Player.GetPlayerInfo();

				if (regionStorage[args.Region].flags.Contains("PVP") && !playerInfo.BypassFlag(args.Player)) //toggle back PVP
				{
					args.Player.TPlayer.hostile = playerInfo.hostile;
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.Player.Index);

				}

				playerInfo.Reset();

				args.Player.SendInfoMessage("Left EventRegion {0}", args.Region.Name);
			}
		}

		private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args)
		{
			if (regionStorage.ContainsKey(args.Region))
			{
				regionStorage.Remove(args.Region);

				int a = -1;
				string query = "DELETE FROM RegionEvent WHERE `Name`=@0";
				a = db.Query(query, args.Region.Name);
				if (a < 0)
				{
					TShock.Log.ConsoleError("Failed to delete from DB!");
					TShock.Log.Error("Failed to delete from DB!");
					return;
				}
			}
		}
		#endregion

		#region DB
		internal static void ReloadDB()
		{
			regionStorage.Clear();

			List<string> flags = new List<string>();

			List<string> bannedItems = new List<string>();
			List<int> bannedNPCs = new List<int>();
			List<int> bannedTiles = new List<int>();
			List<int> bannedProjectiles = new List<int>();

			List<string> groupOnly = new List<string>();
			using (var reader = db.QueryReader("SELECT * FROM RegionEvent"))
			{
				while (reader.Read())
				{
					string name = reader.Get<string>("Name");
					string rawflags = reader.Get<string>("Flags");

					if (!string.IsNullOrEmpty(rawflags))
					{
						flags = JsonConvert.DeserializeObject<List<String>>(rawflags);
					}

					string rawitems = reader.Get<string>("BannedItems");
					if (!string.IsNullOrEmpty(rawitems))
					{
						bannedItems = JsonConvert.DeserializeObject<List<String>>(rawitems);
					}

					string rawnpcs = reader.Get<string>("BannedNPCs");
					if (!string.IsNullOrEmpty(rawnpcs))
					{
						bannedNPCs = JsonConvert.DeserializeObject<List<Int32>>(rawnpcs);
					}

					string rawtiles = reader.Get<string>("BannedTiles");
					if (!string.IsNullOrEmpty(rawtiles))
					{
						bannedTiles = JsonConvert.DeserializeObject<List<Int32>>(rawtiles);
					}

					string rawproj = reader.Get<string>("BannedProjectiles");
					if (!string.IsNullOrEmpty(rawproj))
					{
						bannedProjectiles = JsonConvert.DeserializeObject<List<Int32>>(rawproj);
					}

					string rawGroupOnly = reader.Get<string>("GroupOnly");
					if (!string.IsNullOrEmpty(rawGroupOnly))
					{
						groupOnly = JsonConvert.DeserializeObject<List<String>>(rawGroupOnly);
					}

					int healinterval = reader.Get<int>("HealInterval");
					int healamount = reader.Get<int>("HealAmount");
					int manainterval = reader.Get<int>("ManaInterval");
					int damageinterval = reader.Get<int>("DamageInterval");
					int damageamount = reader.Get<int>("DamageAmount");
					string tempgroup = reader.Get<string>("TempGroup");
					string command = reader.Get<string>("Command");
					string rawFromGroup = reader.Get<string>("FromGroup");
					string rawToGroup = reader.Get<string>("ToGroup");
					string message = reader.Get<string>("Message");
					int effectOwner = reader.Get<int>("EffectOwner");
					int effectAllowed = reader.Get<int>("EffectAllowed");


					Region region = TShock.Regions.GetRegionByName(name);
					Group group = TShock.Groups.GetGroupByName(tempgroup);
					if (region == null)
					{
						TShock.Log.ConsoleError("Failed to load region from RegionEvents!");
						TShock.Log.Error("Failed to load region from RegionEvents!");

						return;
					}
					if (group == null)
					{
						TShock.Log.ConsoleError("Failed to load tempgroup from RegionEvents!");
						TShock.Log.Error("Failed to load region from RegionEvents!");
						return;
					}
					if (regionStorage.ContainsKey(region))
					{
						TShock.Log.ConsoleError("Region \"{0}\" is already loaded!", region.Name);
						TShock.Log.Error("Region \"{0}\" is already loaded!", region.Name);
						continue;
					}
					regionStorage.Add(region, new RegionStorage(region, flags, healinterval, healamount, manainterval, damageinterval, damageamount, group, bannedItems, bannedNPCs, bannedTiles, bannedProjectiles, command, rawFromGroup, rawToGroup, groupOnly, message, Convert.ToBoolean(effectOwner), Convert.ToBoolean(effectAllowed)));
				}
			}
		}

		private void DBConnect()
		{
			switch (TShock.Config.StorageType)
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)
					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "RegionEvent.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;

			}
			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("RegionEvent",
									new SqlColumn("Name", MySqlDbType.VarChar, 25) { Primary = true },
									new SqlColumn("Flags", MySqlDbType.Text),
									new SqlColumn("HealInterval", MySqlDbType.Int32),
									new SqlColumn("HealAmount", MySqlDbType.Int32),
									new SqlColumn("ManaInterval", MySqlDbType.Int32),
									new SqlColumn("DamageInterval", MySqlDbType.Int32),
									new SqlColumn("DamageAmount", MySqlDbType.Int32),
									new SqlColumn("TempGroup", MySqlDbType.VarChar, 10),
									new SqlColumn("BannedItems", MySqlDbType.Text),
									new SqlColumn("BannedNPCs", MySqlDbType.Text),
									new SqlColumn("BannedTiles", MySqlDbType.Text),
									new SqlColumn("BannedProjectiles", MySqlDbType.Text),
									new SqlColumn("Command", MySqlDbType.Text),
									new SqlColumn("FromGroup", MySqlDbType.VarChar, 10),
									new SqlColumn("ToGroup", MySqlDbType.VarChar, 10),
									new SqlColumn("Message", MySqlDbType.Text),
									new SqlColumn("EffectOwner", MySqlDbType.Int32),
									new SqlColumn("EffectAllowed", MySqlDbType.Int32)));
		}
		#endregion
	}
}
