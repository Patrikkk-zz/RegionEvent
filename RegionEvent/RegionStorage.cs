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
	public class RegionStorage
	{
		public Region region;
		public List<string> flags;
		public int healinterval;
		public int healamount;
		public int manainterval;
		public int damageinterval;
		public int damageamount;
		public Group tempgroup;
		public List<string> bannedItems;
		public List<int> bannedNPCs;
		public List<int> bannedTiles;
		public List<int> bannedProjectiles;
		public string command;
		public string fromgroup;
		public string togroup;
		public List<string> groupOnly;
		public string message;
		public bool effectOwner;
		public bool effectAllowed;


		public RegionStorage(Region region, List<string> flags, int healinterval, int healamount, int manainterval, int damageinterval, int damageamount, Group tempgroup, List<string> bannedItems, List<int> bannedNPCs, List<int> bannedTiles, List<int> bannedProjectiles, string command, string fromgroup, string togroup, List<string> groupOnly, string message, bool effectOwner, bool effectAllowed)
		{
			this.region = region;
			this.flags = flags;
			this.healinterval = healinterval;
			this.healamount = healamount;
			this.manainterval = manainterval;
			this.damageinterval = damageinterval;
			this.damageamount = damageamount;
			this.tempgroup = tempgroup;
			this.bannedItems = bannedItems;
			this.bannedNPCs = bannedNPCs;
			this.bannedTiles = bannedTiles;
			this.command = command;
			this.fromgroup = fromgroup;
			this.togroup = togroup;
			this.groupOnly = groupOnly;
			this.message = message;
			this.bannedProjectiles = bannedProjectiles;
			this.effectOwner = effectOwner;
			this.effectAllowed = effectAllowed;
		}
	}
}
