using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;
using RegionEvent;
using Terraria;

namespace RegionEvent
{
	class EventCommands
	{

		private static void Help(CommandArgs args)
		{
			if (args.Parameters.Count > 2)
			{
				args.Player.SendErrorMessage("Proper syntax: /re help <command/page>");
				return;
			}

			int pageNumber;
			if (args.Parameters.Count == 1 || int.TryParse(args.Parameters[1], out pageNumber))
			{
				int pageParamIndex = 1;
				if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
					return;

				List<string> availablecommands = new List<string>();
				foreach (string command in RegionEvent.commands.Keys)
				{
					availablecommands.Add("/re " + command + " " + RegionEvent.commands[command][0]);
				}
				PaginationTools.SendPage(args.Player, pageNumber, availablecommands,
			new PaginationTools.Settings
			{
				HeaderFormat = "Commands ({0}/{1}):",
				FooterFormat = "Type /re help {0} for more.",
				MaxLinesPerPage = 5
			});
				args.Player.SendInfoMessage("Type /re help <commandname> for command description.");
			}
			else
			{
				string commandName = args.Parameters[1];
				if (!RegionEvent.commands.ContainsKey(commandName))
				{
					args.Player.SendErrorMessage("Invalid command.");
					return;
				}
				args.Player.SendSuccessMessage("/re {0} help: ", commandName);
				args.Player.SendInfoMessage(RegionEvent.commands[commandName][1]);
			}
		}

		private static void SetFlag(CommandArgs args)
		{
			if (args.Parameters.Count < 3)
			{
				args.Player.SendErrorMessage("Syntax: /re setflag <region> <FLAG>");
				return;
			}

			Region region = TShock.Regions.GetRegionByName(args.Parameters[1]);

			if (region == null)
			{
				args.Player.SendErrorMessage("Could not find region {0}!", args.Parameters[1]);
				return;
			}

			if (!RegionEvent.flagTypes.ContainsKey(args.Parameters[2]))
			{
				args.Player.SendErrorMessage("Error: No such flag type exists!");
				args.Player.SendErrorMessage("Type /re list flags, for a list of available flags!");
				return;
			}

			RegionStorage tempStorage = new RegionStorage(region, new List<string>() { args.Parameters[2] }, healinterval: 20, healamount: 2, manainterval: 2, damageinterval: 2, damageamount: 20, tempgroup: Group.DefaultGroup, bannedItems: new List<string>(), bannedNPCs: new List<int>(), bannedTiles: new List<int>(), bannedProjectiles: new List<int>(), command: "", fromgroup: "", togroup: "", groupOnly: new List<string>(), message: "", effectOwner:false, effectAllowed:true);

			if (RegionEvent.regionStorage.ContainsKey(region))
			{
				tempStorage = RegionEvent.regionStorage[region];

				if (RegionEvent.regionStorage[region].flags.Contains(args.Parameters[2]))
				{
					args.Player.SendErrorMessage("Region \"{0}\" already contains {1} flag!", region.Name, args.Parameters[1]);
					return;
				}
			}

			switch (args.Parameters[2])
			{
				case "HEAL":
					{
						if (args.Parameters.Count != 5)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> HEAL <interval> <amount>");
							return;
						}
						int healinterval = -1;
						int healamount = -1;
						if (!int.TryParse(args.Parameters[3], out healinterval))
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[3]);
							return;
						}
						if (!int.TryParse(args.Parameters[4], out healamount))
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[4]);
							return;
						}

						tempStorage.healinterval = healinterval;
						tempStorage.healamount = healamount;
						break;
					}
				case "MANA":
					{
						// /re setflag MANA <interval>
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> MANA <interval>");
							return;
						}
						int interval = -1;
						if (!int.TryParse(args.Parameters[3], out interval))
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[3]);
							return;
						}

						tempStorage.manainterval = interval;
						break;
					}
				case "HURT":
					{
						if (args.Parameters.Count != 5)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> HURT <interval> <amount>");
							return;
						}
						int interval = -1;
						int amount = -1;
						if (!int.TryParse(args.Parameters[3], out interval))
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[3]);
							return;
						}
						if (!int.TryParse(args.Parameters[4], out amount))
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[4]);
							return;
						}

						tempStorage.damageinterval = interval;
						tempStorage.damageamount = amount;
						break;
					}
				case "TEMPGROUP":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> TEMPGROUP <group>");
							return;
						}

						Group tempgroup = TShock.Groups.GetGroupByName(args.Parameters[3]);
						if (tempgroup == null)
						{
							args.Player.SendErrorMessage("Could not find group!");
							return;
						}
						tempStorage.tempgroup = tempgroup;

						break;
					}
				case "COMMAND":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> COMMAND <command>");
							return;
						} 
						//check if they have permission for command
						if (args.Parameters[3] != null && args.Parameters[3] != "")
						tempStorage.command = args.Parameters[3];
						break;
					}
				case "PROMOTE":
					{
						if (args.Parameters.Count != 5)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> PROMOTE <fromgroup> <togroup>");
							return;
						}
						Group fromgroup = TShock.Groups.GetGroupByName(args.Parameters[3]);
						Group togroup = TShock.Groups.GetGroupByName(args.Parameters[4]);

						if (fromgroup == null && args.Parameters[3] != "*")
						{
							args.Player.SendErrorMessage("Could not find fromgroup {0}!", args.Parameters[3]);
							return;
						}
						if (togroup == null)
						{
							args.Player.SendErrorMessage("Could not find togroup {0}!", args.Parameters[4]);
							return;
						}
						tempStorage.fromgroup = args.Parameters[3];
						tempStorage.togroup = togroup.Name;
						break;
					}
				case "BANITEM":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> BANITEM <ID/Name>");
							return;
						}
						List<Item> item = TShock.Utils.GetItemByIdOrName(args.Parameters[3]);
						if (item.Count == 0)
						{
							args.Player.SendErrorMessage("Could not find item \"{0}\"!", args.Parameters[3]);
							return;
						}
						else if (item.Count > 1)
						{
							TShock.Utils.SendMultipleMatchError(args.Player, item);
							return;
						}

						tempStorage.bannedItems.Add(item[0].name);

						break;
					}
				case "BANTILE":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> BANTILE <ID>");
							return;
						}
						int ID = -1;
						if (!int.TryParse(args.Parameters[3], out ID) || ID < 0 || ID >= Main.maxTileSets)
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[3]);
							return;
						}
						tempStorage.bannedTiles.Add(ID);

						break;
					}
				case "BANPROJ":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> BANPROJ <ID>");
							return;
						}
						int ID = -1;
						if (!int.TryParse(args.Parameters[3], out ID) || ID < 0 || ID >= Main.maxProjectileTypes)
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[3]);
							return;
						}
						tempStorage.bannedProjectiles.Add(ID);

						break;
					}
				case "BANMOB":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> BANMOB <ID>");
							return;
						}
						int ID = -1;
						if (!int.TryParse(args.Parameters[3], out ID) || ID < 0 || ID >= Main.maxNPCTypes)
						{
							args.Player.SendErrorMessage("{0} is not a valid number!", args.Parameters[3]);
							return;
						}
						tempStorage.bannedNPCs.Add(ID);

						break;
		
					}
				case "GROUPONLY":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> GROUPONLY <group>");
							return;
						}

						Group grouponly = TShock.Groups.GetGroupByName(args.Parameters[3]);
						if (grouponly == null)
						{
							args.Player.SendErrorMessage("Could not find group!");
							return;
						}
						tempStorage.groupOnly.Add(grouponly.Name);
						break;
					}
				case "MESSAGE":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("Syntax: /re setflag <region> MESSAGE <message>");
							return;
						}
						if (args.Parameters[3] != null && args.Parameters[3] != "")
							tempStorage.message = args.Parameters[3];

						break;
					}
				default:
					break;
			}


			if (!RegionEvent.regionStorage.ContainsKey(region))
			{
				RegionEvent.regionStorage.Add(region, tempStorage);

				string flags = JsonConvert.SerializeObject(RegionEvent.regionStorage[region].flags, Formatting.Indented);
				string bannedItems = JsonConvert.SerializeObject(RegionEvent.regionStorage[region].bannedItems, Formatting.Indented);
				string bannedNPCs = JsonConvert.SerializeObject(RegionEvent.regionStorage[region].bannedNPCs, Formatting.Indented);
				string bannedTiles = JsonConvert.SerializeObject(RegionEvent.regionStorage[region].bannedTiles, Formatting.Indented);
				string bannedProjectiles = JsonConvert.SerializeObject(RegionEvent.regionStorage[region].bannedProjectiles, Formatting.Indented);

				int a = -1;
				string query = "INSERT INTO RegionEvent (Name, Flags, HealInterval, HealAmount, ManaInterval, DamageInterval, DamageAmount, TempGroup, BannedItems, BannedNPCs, BannedTiles, BannedProjectiles, Command, FromGroup, ToGroup, Message, EffectOwner, EffectAllowed) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16, @17)";
				a = RegionEvent.db.Query(query, region.Name, flags, tempStorage.healinterval, tempStorage.healamount, tempStorage.manainterval, tempStorage.damageinterval, tempStorage.damageamount, tempStorage.tempgroup.Name, bannedItems, bannedNPCs, bannedTiles, bannedProjectiles, tempStorage.command, tempStorage.fromgroup, tempStorage.togroup, tempStorage.message, tempStorage.effectOwner ? 1 : 0, tempStorage.effectAllowed ? 1 : 0);
				if (a < 0)
				{
					RegionEvent.regionStorage[region].flags.Remove(args.Parameters[2]);
					args.Player.SendErrorMessage("Failed to save to DB!");
					return;
				}
				args.Player.SendSuccessMessage("Added flag {0} to region \"{1}\"!", args.Parameters[2], region.Name);
			}
			else
			{
				tempStorage.flags.Add(args.Parameters[2]);

				string flags = JsonConvert.SerializeObject(tempStorage.flags, Formatting.Indented);
				string bannedItems = JsonConvert.SerializeObject(tempStorage.bannedItems, Formatting.Indented);
				string bannedNPCs = JsonConvert.SerializeObject(tempStorage.bannedNPCs, Formatting.Indented);
				string bannedTiles = JsonConvert.SerializeObject(tempStorage.bannedTiles, Formatting.Indented);

				int a = -1;
				string query = "UPDATE RegionEvent SET `Flags`=@0, `HealInterval`=@1, HealAmount=@2, ManaInterval=@3, DamageInterval=@4, DamageAmount=@5, TempGroup=@6, BannedItems=@7, BannedNPCs=@8, BannedTiles=@9, BannedProjectiles=10, Command=@10, FromGroup=@11, ToGroup=@12, Message=@14, EffectOwner=@15, EffectAllowed=@16 WHERE Name=@17";
				a = RegionEvent.db.Query(query, flags, tempStorage.healinterval, tempStorage.healamount, tempStorage.manainterval, tempStorage.damageinterval, tempStorage.damageamount, tempStorage.tempgroup.Name, bannedItems, bannedNPCs, bannedTiles, tempStorage.command, tempStorage.fromgroup, tempStorage.togroup, tempStorage.message, tempStorage.effectOwner ? 1 : 0, tempStorage.effectAllowed ? 1 : 0, region.Name);
				if (a < 0)
				{
					args.Player.SendErrorMessage("Failed to save to DB!");
					return;
				}

				RegionEvent.regionStorage[region] = tempStorage;
				args.Player.SendSuccessMessage("Added flag {0} to region \"{1}\"!", args.Parameters[2], region.Name);
			}
		}

		private static void RemoveFlag(CommandArgs args)
		{
			if (args.Parameters.Count != 3)
			{
				args.Player.SendErrorMessage("Syntax: /re removeflag <region> <flag>");
				return;
			}
			Region region = TShock.Regions.GetRegionByName(args.Parameters[1]);
			if (region == null)
			{
				args.Player.SendErrorMessage("Region \"{0}\" does not exist.", args.Parameters[1]);
				return;
			}
			if (RegionEvent.regionStorage.ContainsKey(region))
			{
				if (!RegionEvent.regionStorage[region].flags.Contains(args.Parameters[2]))
				{
					args.Player.SendErrorMessage("Region \"{0}\" does not contain {1} flag!", region.Name, args.Parameters[2]);
					return;
				}
				RegionEvent.regionStorage[region].flags.Remove(args.Parameters[2]);



				if (RegionEvent.regionStorage[region].flags.Count == 0)
				{
					RegionEvent.regionStorage.Clear();
					int a2 = -1;
					string query2 = "DELETE FROM RegionEvent WHERE `Name`=@0";
					a2 = RegionEvent.db.Query(query2, region.Name);
					if (a2 < 0)
					{
						args.Player.SendErrorMessage("Failed to delete from DB!");
						return;
					}
					args.Player.SendSuccessMessage("Region \"{0}\" has no more flags!", region.Name);
					return;
				}
				string allFlags = JsonConvert.SerializeObject(RegionEvent.regionStorage[region].flags, Formatting.Indented);

				int a = -1;
				string query = "UPDATE `RegionEvent` SET `Flags`=@0 WHERE `Name`=@1";
				a = RegionEvent.db.Query(query, allFlags, region.Name);
				if (a < 0)
				{
					RegionEvent.regionStorage[region].flags.Add(args.Parameters[2]);
					args.Player.SendErrorMessage("Failed to save to DB!");
					return;
				}
				args.Player.SendSuccessMessage("Successfully removed {0} flag from region \"{1}\"!", args.Parameters[2], region.Name);

			}
			else
			{
				args.Player.SendErrorMessage("Region \"{0}\" does not contain any flags!", region.Name);
				return;
			}
		}

		private static void DelRegion(CommandArgs args)
		{
			if (args.Parameters.Count != 2)
			{
				args.Player.SendErrorMessage("Syntax: /re delregion <region>");
				return;
			}
			Region region = TShock.Regions.GetRegionByName(args.Parameters[1]);
			if (region == null)
			{
				args.Player.SendErrorMessage("Region \"{0}\" does not exist.", args.Parameters[1]);
				return;
			}
			if (!RegionEvent.regionStorage.ContainsKey(region))
			{
				args.Player.SendErrorMessage("Region \"{0}\" does not contain any flags!", region.Name);
				return;
			}
			RegionEvent.regionStorage.Remove(region);
			args.Player.SendSuccessMessage("Successfully removed all flags from region \"{0}\"!", region.Name);

			int a = -1;
			string query = "DELETE FROM RegionEvent WHERE `Name`=@0";
			a = RegionEvent.db.Query(query, region.Name);
			if (a < 0)
			{
				args.Player.SendErrorMessage("Failed to delete from DB!");
				return;
			}
		}

		private static void Info(CommandArgs args)
		{
			if (args.Parameters.Count != 3)
			{
				args.Player.SendErrorMessage("Syntax: /re info region/flag <name>");
				return;
			}

			switch (args.Parameters[1])
			{
				case "region":
					{
						Region region = TShock.Regions.GetRegionByName(args.Parameters[2]);
						if (region == null)
						{
							args.Player.SendErrorMessage("Region \"{0}\" does not exist.", args.Parameters[2]);
							return;
						}
						if (!RegionEvent.regionStorage.ContainsKey(region))
						{
							args.Player.SendErrorMessage("Region \"{0}\" does not contain any flags!", region.Name);
							return;
						}
						args.Player.SendInfoMessage("Flags:" + string.Join(", ", RegionEvent.regionStorage[region].flags) + ".");
						if (RegionEvent.regionStorage[region].flags.Contains("HEAL"))
						{
							args.Player.SendInfoMessage("Heal interval: {0}", RegionEvent.regionStorage[region].healinterval);
							args.Player.SendInfoMessage("Heal amount: {0}", RegionEvent.regionStorage[region].healamount);
						}
						if (RegionEvent.regionStorage[region].flags.Contains("MANA"))
						{
							args.Player.SendInfoMessage("Mana interval: {0}", RegionEvent.regionStorage[region].manainterval);
						}
						if (RegionEvent.regionStorage[region].flags.Contains("HURT"))
						{
							args.Player.SendInfoMessage("Damage interval: {0}", RegionEvent.regionStorage[region].damageinterval);
							args.Player.SendInfoMessage("Damage ammount: {0}", RegionEvent.regionStorage[region].damageamount);
						}
						if (RegionEvent.regionStorage[region].flags.Contains("TEMPGROUP"))
						{
							args.Player.SendInfoMessage("Tempgroup: {0}", RegionEvent.regionStorage[region].tempgroup);
						}
						break;
					}
				case "flag":
					{
						if (!RegionEvent.flagTypes.ContainsKey(args.Parameters[2]))
						{
							args.Player.SendErrorMessage("Error: No such flag type exists!");
							args.Player.SendErrorMessage("Type /re list flags, for a list of available flags!");
							return;
						}
						args.Player.SendInfoMessage(args.Parameters[2] + ": " + RegionEvent.flagTypes[args.Parameters[2]]);
						break;
					}
			}
		}

		private static void List(CommandArgs args)
		{
			if (args.Parameters.Count != 2)
			{
				args.Player.SendErrorMessage("Syntax: /re list region/flags");
				return;
			}
			switch (args.Parameters[1])
			{
				case "region":
					{
						if (RegionEvent.regionStorage.Count == 0)
						{
							args.Player.SendErrorMessage("There aren't any EventRegions defined!");
							return;
						}
						args.Player.SendInfoMessage("Current flagged regions: {0}", string.Join(", ", RegionEvent.regionStorage.Keys.ToList().Select(p => p.Name)));
						break;
					}
				case "flags":
				case "flag":
					{
						args.Player.SendInfoMessage("Available flag types: {0}", string.Join(", ", RegionEvent.flagTypes.Keys.ToList()));
						break;
					}
			}
		}

		private static void Reload(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Syntax: /re reload");
				return;
			}
			RegionEvent.ReloadDB();
			args.Player.SendSuccessMessage("Attempted to reload RegionEvent DB!");
		}

		internal static void REvent(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendInfoMessage("Type /re help, for command information.");
				return;
			}
			switch (args.Parameters[0])
			{
				case "help":
						Help(args);
						break;
				case "setflag":
						SetFlag(args);
						break;
				case "removeflag":
						RemoveFlag(args);
						break;
				case "delregion":
						DelRegion(args);
						break;
				case "info":
						Info(args);
						break;
				case "list":
						List(args);
						break;
				case "reload":
						Reload(args);
						break;
				case "bantile":
						BanTile(args);
						break;
				case "banitem":
						BanItem(args);
						break;
				case "banmob":
						BanMob(args);
						break;
				case "banproj":
						BanProj(args);
						break;
				default:
					{
						args.Player.SendErrorMessage("Invalid command! Type /re help, for a list of commands!");
						return;
					}
			}
		}

		private static void BanTile(CommandArgs args)
		{
			// re bantile add/remove <region> <id>
			if (args.Parameters.Count != 4)
			{
				args.Player.SendErrorMessage("Syntax: /re bantile <add/remove> <region> <ID>");
				return;
			}

			Region region = TShock.Regions.GetRegionByName(args.Parameters[2]);
			if (region == null)
			{
				args.Player.SendErrorMessage("Did not find region \"{0}\"", args.Parameters[2]);
				return;
			}
			if (!RegionEvent.regionStorage.ContainsKey(region))
			{
				args.Player.SendErrorMessage("Region is not an EventRegion! Please define BANTILE flag first!");
				return;
			}
			RegionStorage tempStorage = RegionEvent.regionStorage[region];
			int ID = -1;
			if (!int.TryParse(args.Parameters[3], out ID) || ID < 0 || ID >= Main.maxTileSets)
			{
				args.Player.SendErrorMessage("{0} is not a valid tile!", args.Parameters[3]);
			}

			switch (args.Parameters[1])
			{
				case "add":
					{
						if (tempStorage.bannedTiles.Contains(ID))
						{
							args.Player.SendErrorMessage("ID {0} is already banned!", ID);
							return;
						}
						tempStorage.bannedTiles.Add(ID);

						string bannedTiles = JsonConvert.SerializeObject(tempStorage.bannedTiles, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedTiles=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedTiles, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedTiles = tempStorage.bannedTiles;
						args.Player.SendSuccessMessage("Banned ID {0} in region \"{1}\"!", ID, region.Name);

						break;
					}
				case "remove":
				case "delete":
				case "del":
					{
						if (!tempStorage.bannedTiles.Contains(ID))
						{
							args.Player.SendErrorMessage("ID {0} is not on the banlist!", ID);
							return;
						}

						tempStorage.bannedTiles.Remove(ID);

						string bannedTiles = JsonConvert.SerializeObject(tempStorage.bannedTiles, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedTiles=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedTiles, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedTiles = tempStorage.bannedTiles;
						args.Player.SendSuccessMessage("Unbanned ID {0} from region \"{1}\"!", ID, region.Name);

						break;
					}
				default:
					args.Player.SendErrorMessage("Syntax: /re bantile <add/remove> <region> <ID>");
					return;
			}
		}

		private static void BanItem(CommandArgs args)
		{
			if (args.Parameters.Count != 4)
			{
				args.Player.SendErrorMessage("Syntax: /re banitem <add/remove> <region> <Name/ID>");
				return;
			}
			Region region = TShock.Regions.GetRegionByName(args.Parameters[2]);
			if (region == null)
			{
				args.Player.SendErrorMessage("Did not find region \"{0}\"", args.Parameters[2]);
				return;
			}
			if (!RegionEvent.regionStorage.ContainsKey(region))
			{
				args.Player.SendErrorMessage("Region is not an EventRegion! Please define BANITEM flag first!");
				return;
			}
			RegionStorage tempStorage = RegionEvent.regionStorage[region];

			List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[3]);
			if (items.Count == 0)
			{
				args.Player.SendErrorMessage("Could not find item!");
				return;
			}
			else if (items.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, items);
				return;
			}

			string item = items[0].name;

			switch (args.Parameters[1])
			{
				case "add":
					{
						if (tempStorage.bannedItems.Contains(item))
						{
							args.Player.SendErrorMessage("Item \"{0}\" is already banned!", item);
							return;
						}
						tempStorage.bannedItems.Add(item);

						string bannedItems = JsonConvert.SerializeObject(tempStorage.bannedItems, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedItems=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedItems, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedTiles = tempStorage.bannedTiles;
						args.Player.SendSuccessMessage("Banned item \"{0}\" in region \"{1}\"!", item, region.Name);

						break;
					}
				case "remove":
				case "delete":
				case "del":
					{
						if (!tempStorage.bannedItems.Contains(item))
						{
							args.Player.SendErrorMessage("Item \"{0}\" is not on the banlist!", item);
							return;
						}

						tempStorage.bannedItems.Remove(item);

						string bannedItems = JsonConvert.SerializeObject(tempStorage.bannedItems, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedItems=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedItems, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedItems = tempStorage.bannedItems;
						args.Player.SendSuccessMessage("Removed banned item \"{0}\" to region \"{1}\"!", item, region.Name);

						break;
					}
				default:
					args.Player.SendErrorMessage("Syntax: /re banitem <add/remove> <region> <Name/ID>");
					return;
			}
			
		}
		private static void BanMob(CommandArgs args)
		{
			if (args.Parameters.Count != 4)
			{
				args.Player.SendErrorMessage("Syntax: /re banmob <add/remove> <region> <ID>");
				return;
			}
			Region region = TShock.Regions.GetRegionByName(args.Parameters[2]);
			if (region == null)
			{
				args.Player.SendErrorMessage("Did not find region \"{0}\"", args.Parameters[2]);
				return;
			}
			if (!RegionEvent.regionStorage.ContainsKey(region))
			{
				args.Player.SendErrorMessage("Region is not an EventRegion! Please define BANMOB flag first!");
				return;
			}
			RegionStorage tempStorage = RegionEvent.regionStorage[region];
			int ID = -1;
			if (!int.TryParse(args.Parameters[3], out ID) || ID < 0 || ID >= Main.maxNPCTypes)
			{
				args.Player.SendErrorMessage("{0} is not a valid NPC!", args.Parameters[3]);
			}

			switch (args.Parameters[1])
			{
				case "add":
					{
						if (tempStorage.bannedNPCs.Contains(ID))
						{
							args.Player.SendErrorMessage("ID {0} is already banned!", ID);
							return;
						}
						tempStorage.bannedNPCs.Add(ID);

						string bannedNPCs = JsonConvert.SerializeObject(tempStorage.bannedNPCs, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedNPCs=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedNPCs, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedNPCs = tempStorage.bannedNPCs;
						args.Player.SendSuccessMessage("Banned ID {0} in region \"{1}\"!", ID, region.Name);

						break;
					}
				case "remove":
				case "delete":
				case "del":
					{
						if (!tempStorage.bannedNPCs.Contains(ID))
						{
							args.Player.SendErrorMessage("ID {0} is not on the banlist!", ID);
							return;
						}

						tempStorage.bannedNPCs.Remove(ID);

						string bannedNPCs = JsonConvert.SerializeObject(tempStorage.bannedNPCs, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedTiles=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedNPCs, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedNPCs = tempStorage.bannedNPCs;
						args.Player.SendSuccessMessage("Unbanned ID {0} from region \"{1}\"!", ID, region.Name);

						break;
					}
				default:
					args.Player.SendErrorMessage("Syntax: /re banmob <add/remove> <region> <ID>");
					return;
			}

		}
		private static void BanProj(CommandArgs args)
		{
			if (args.Parameters.Count != 4)
			{
				args.Player.SendErrorMessage("Syntax: /re banproj <add/remove> <region> <ID>");
				return;
			}
			Region region = TShock.Regions.GetRegionByName(args.Parameters[2]);
			if (region == null)
			{
				args.Player.SendErrorMessage("Did not find region \"{0}\"", args.Parameters[2]);
				return;
			}
			if (!RegionEvent.regionStorage.ContainsKey(region))
			{
				args.Player.SendErrorMessage("Region is not an EventRegion! Please define BANPROJ flag first!");
				return;
			}
			RegionStorage tempStorage = RegionEvent.regionStorage[region];
			int ID = -1;
			if (!int.TryParse(args.Parameters[3], out ID) || ID < 0 || ID >= Main.maxProjectileTypes)
			{
				args.Player.SendErrorMessage("{0} is not a valid Projectile!", args.Parameters[3]);
			}

			switch (args.Parameters[1])
			{
				case "add":
					{
						if (tempStorage.bannedProjectiles.Contains(ID))
						{
							args.Player.SendErrorMessage("ID {0} is already banned!", ID);
							return;
						}
						tempStorage.bannedProjectiles.Add(ID);

						string bannedProjectiles = JsonConvert.SerializeObject(tempStorage.bannedProjectiles, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedProjectiles=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedProjectiles, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedProjectiles = tempStorage.bannedProjectiles;
						args.Player.SendSuccessMessage("Banned ID {0} in region \"{1}\"!", ID, region.Name);

						break;
					}
				case "remove":
				case "delete":
				case "del":
					{
						if (!tempStorage.bannedProjectiles.Contains(ID))
						{
							args.Player.SendErrorMessage("ID {0} is not on the banlist!", ID);
							return;
						}

						tempStorage.bannedProjectiles.Remove(ID);

						string bannedProjectiles = JsonConvert.SerializeObject(tempStorage.bannedProjectiles, Formatting.Indented);

						int a = -1;
						string query = "UPDATE RegionEvent SET BannedProjectiles=@0 WHERE Name=@1";
						a = RegionEvent.db.Query(query, bannedProjectiles, region.Name);
						if (a < 0)
						{
							args.Player.SendErrorMessage("Failed to save to DB!");
							return;
						}

						RegionEvent.regionStorage[region].bannedProjectiles = tempStorage.bannedProjectiles;
						args.Player.SendSuccessMessage("Unbanned ID {0} from region \"{1}\"!", ID, region.Name);

						break;
					}
				default:
					args.Player.SendErrorMessage("Syntax: /re banproj <add/remove> <region> <ID>");
					return;
			}
		}
		


	}
}
