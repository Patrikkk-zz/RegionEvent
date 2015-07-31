using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace RegionEvent
{
	public class PlayerStorage
	{
		public RegionStorage regionStorage;
		public int regionWarned;
		public Vector2 lastPos;
		public bool inEventRegion;
		public bool gotWarnMessage;
		public bool hostile;
		public bool killed;
		public bool groupset;
		public bool executedcommand;
		public bool promoted;
		public bool gotmessage;
		public bool muted;
		public bool disabled;

		public PlayerStorage()
		{
			regionStorage = new RegionStorage(region: null, flags: new List<string> { }, healinterval: 2, healamount: 20, manainterval: 2, damageinterval: 2, damageamount: 20, tempgroup: null, bannedItems: new List<string>(), bannedNPCs: new List<int>(), bannedTiles: new List<int>(), bannedProjectiles: new List<int>(), command: null, fromgroup: null, togroup: null, groupOnly: new List<string>(), message: null, effectOwner: false, effectAllowed: true);
			regionWarned = 0;
			lastPos = new Vector2();
			gotWarnMessage = false;
			inEventRegion = false;
			hostile = false;
			killed = false;
			groupset = false;
			executedcommand = false;
			promoted = false;
			gotmessage = false;
			muted = false;
		}


		private DateTime lastDamageUpdate = DateTime.Now;
		private DateTime lastHealUpdate = DateTime.Now;
		private DateTime lastManaUpdate = DateTime.Now;
		public DateTime lastWarned = DateTime.Now;
		public DateTime lastDisabled = DateTime.Now;

		public bool BypassFlag(TSPlayer player)
		{
			if (regionStorage.region.Owner == player.User.Name && !regionStorage.effectOwner)
				return false;
			if (regionStorage.region.AllowedIDs.Contains(player.User.ID) && !regionStorage.effectAllowed)
				return false;
			if (player.Group.HasPermission("regionevent.bypass"))
				return false;
			return true;
		}

		public void Reset()
		{
			regionWarned = 0;
			gotWarnMessage = false;
			inEventRegion = false;
			hostile = false;
			killed = false;
			groupset = false;
			executedcommand = false;
			promoted = false;
			gotmessage = false;
			muted = false;
		}

		public void Disable (TSPlayer player, string reason, bool showInConsole = true)
		{
			disabled = true;
			player.Disable(reason, showInConsole);
		}

		public void Update(TSPlayer player)
		{
			if ((DateTime.Now - lastDisabled).TotalSeconds > 10)
			{
				disabled = false;
				lastDisabled = DateTime.Now;
			}

			bool bypassFlag = BypassFlag(player);

			bool warning = ((DateTime.Now - lastWarned).TotalSeconds > 1);
			if (regionStorage.flags.Contains("HEAL"))
			{
				if (regionStorage.healinterval < 0 || regionStorage.healamount < 0)
					return;
				if ((DateTime.Now - lastHealUpdate).TotalSeconds >= regionStorage.healinterval)
				{
					lastHealUpdate = DateTime.Now;
					player.Heal(regionStorage.healamount);
				}
			}
			if (regionStorage.flags.Contains("MANA"))
			{
				if (regionStorage.manainterval < 0 || regionStorage.healamount < 0)
					return;
				if ((DateTime.Now - lastManaUpdate).TotalSeconds >= regionStorage.manainterval)
				{
					lastManaUpdate = DateTime.Now;
					var matches = TShock.Utils.GetItemByIdOrName("184");
					Item star = matches[0];
					player.GiveItem(star.netID, star.name, star.width, star.height, regionStorage.healamount);
				}
			}
			if (regionStorage.flags.Contains("PRIVATE") && !bypassFlag)
			{
				if (!gotWarnMessage)
				{
					player.Teleport(lastPos.X, lastPos.Y, 1);
					player.SendErrorMessage("No permission to enter private region!");
					gotWarnMessage = true;
				}
			}
			if (regionStorage.flags.Contains("PVP") && !bypassFlag)
			{
				if (!player.TPlayer.hostile)
				{
					player.SendSuccessMessage("PVP arena entered, pvp enabled.");
					player.TPlayer.hostile = true;
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player.Index);
				}
			}
			if (regionStorage.flags.Contains("NOPVP") && !bypassFlag)
			{
				if (player.TPlayer.hostile)
				{
					player.SendSuccessMessage("PVP arena entered, pvp disabled.");
					player.TPlayer.hostile = false;
					NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", player.Index);
				}
			}
			if (regionStorage.flags.Contains("TEMPGROUP") && !bypassFlag)
			{
				if (!groupset)
				{
					player.tempGroup = regionStorage.tempgroup;
					player.SendSuccessMessage("Your group has been temporarily set to \"{0}\"!", regionStorage.tempgroup.Name);
					groupset = true;
				}
			}
			if (regionStorage.flags.Contains("DEATH") && !bypassFlag)
			{
				if (!killed)
				{
					player.DamagePlayer(1200);
					player.SendErrorMessage("You entered a death zone! RIP");
					killed = true;
				}
			}
			if (regionStorage.flags.Contains("HURT") && !bypassFlag)
			{
				if (regionStorage.healinterval < 0 || regionStorage.healamount < 0)
					return;
				if ((DateTime.Now - lastDamageUpdate).TotalSeconds >= regionStorage.damageinterval)
				{
					lastDamageUpdate = DateTime.Now;
					player.DamagePlayer(regionStorage.damageamount);
				}
			}
			if (regionStorage.flags.Contains("COMMAND") && !bypassFlag)
			{
				if (!executedcommand)
				{
					if (regionStorage.command != null && regionStorage.command != "")
					{
						Commands.HandleCommand(TSPlayer.Server, "/" + regionStorage.command);
						executedcommand = true;
					}
				}
			}
			if (regionStorage.flags.Contains("PROMOTE") && !bypassFlag)
			{
				if (!promoted)
				{
					if (player.Group == TShock.Groups.GetGroupByName(regionStorage.fromgroup) || regionStorage.fromgroup == "*")
					{
						player.Group = TShock.Groups.GetGroupByName(regionStorage.togroup);
						player.SendInfoMessage("You have been promoted to group \"{0}\"", regionStorage.togroup);
						promoted = true;
					}
				}
			}
			if (regionStorage.flags.Contains("GROUPONLY"))
			{
				if (!gotWarnMessage && !regionStorage.groupOnly.Contains(player.Group.Name) && !bypassFlag)
				{
					player.Teleport(lastPos.X, lastPos.Y, 1);
					player.SendErrorMessage("No permission to enter private region!");
					gotWarnMessage = true;
				}
			}
			if (regionStorage.flags.Contains("MESSAGE"))
			{
				if (!gotmessage)
				{
					if (regionStorage.message != null && regionStorage.message != "")
					{
						player.SendInfoMessage(regionStorage.message);
						gotmessage = true;
					}
				}
			}
		}
	}
}
