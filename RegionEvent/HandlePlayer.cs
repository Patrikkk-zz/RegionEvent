using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using TShockAPI.DB;
using TShockAPI.Net;
using Terraria;
using TShockAPI;
using System.IO;
using System.IO.Streams;

namespace RegionEvent
{

	internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);

	internal class GetDataHandlerArgs : EventArgs
	{
		public TSPlayer Player { get; private set; }
		public MemoryStream Data { get; private set; }

		public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
		{
			Player = player;
			Data = data;
		}
	}

	class HandlePlayer
	{
		private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

		public static void InitGetDataHandler()
		{
			GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
			{
				{PacketTypes.PlayerUpdate, HandlePlayerUpdate},
				{PacketTypes.ProjectileNew, HandleProjectile},
				{PacketTypes.Tile, HandleTile}

			};
		}

		public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
		{
			GetDataHandlerDelegate handler;
			if (GetDataHandlerDelegates.TryGetValue(type, out handler))
			{
				try
				{
					return handler(new GetDataHandlerArgs(player, data));
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}
			return false;
		}

		


		private static bool HandlePlayerUpdate(GetDataHandlerArgs args) //handle BANITEM
		{
			if (args.Player == null) return false;
			int index = args.Player.Index;
			var info = args.Player.GetPlayerInfo();
			byte plr = args.Data.ReadInt8();
			BitsByte control = args.Data.ReadInt8();
			BitsByte pulley = args.Data.ReadInt8();
			byte item = args.Data.ReadInt8();

			string itemName = args.Player.TPlayer.inventory[item].name;

			if (control[5] && info.regionStorage.flags.Contains("BANITEM") && info.regionStorage.bannedItems.Contains(itemName))
			{
				if (info.inEventRegion)
				{
					control[5] = false;
					info.Disable(args.Player,"Using a banned item ({0}) in region \"{1}\"".SFormat(itemName, info.regionStorage.region.Name));
					args.Player.SendErrorMessage("You cannot use {0} in this region. Your actions are being ignored.", itemName);
					NetMessage.SendData((int)PacketTypes.PlayerUpdate, -1, args.Player.Index, "", args.Player.Index);
					return true;
				}
			}
			return false;
		}
		private static bool HandleTile(GetDataHandlerArgs args) //handle BANTILE
		{
			if (args.Player == null) return false;
			int index = args.Player.Index;
			var info = args.Player.GetPlayerInfo();
			TShockAPI.GetDataHandlers.EditAction action = (TShockAPI.GetDataHandlers.EditAction)args.Data.ReadInt8();
			var tileX = args.Data.ReadInt16();
			var tileY = args.Data.ReadInt16();
			var editData = args.Data.ReadInt16();

			if (info.regionStorage.flags.Contains("BANTILE") && !info.BypassFlag(args.Player))
			{
				if (action == GetDataHandlers.EditAction.PlaceTile && info.regionStorage.bannedTiles.Contains(editData))
				{
					args.Player.SendTileSquare(tileX, tileY, 1);
					args.Player.SendErrorMessage("You do not have permission to place this tile in this region.");
					return true;
				}
			}

			return false;
		}
		private static bool HandleProjectile(GetDataHandlerArgs args) //handle PORTAL, NOPORTAL, BANPROJ
		{
			if (args.Player == null) return false;
			int index = args.Player.Index;
			var info = args.Player.GetPlayerInfo();

			short ident = args.Data.ReadInt16();
			var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
			var vel = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
			float knockback = args.Data.ReadSingle();
			short dmg = args.Data.ReadInt16();
			byte owner = args.Data.ReadInt8();
			short type = args.Data.ReadInt16();
			BitsByte bits = args.Data.ReadInt8();

			bool bypassFlag = info.BypassFlag(args.Player);

			if (info.regionStorage.flags.Contains("BANPROJ") && info.regionStorage.bannedProjectiles.Contains(type) && !bypassFlag)
			{
				args.Player.RemoveProjectile(ident, owner);
				args.Player.Disable();
				args.Player.SendErrorMessage("No permission to use this projectile in this region!");
				return true;
			}

			if (info.regionStorage.flags.Contains("NOPORTAL") && !bypassFlag)
			{
				if (type == ProjectileID.PortalGunGate)
				{
					args.Player.RemoveProjectile(ident, owner);
					return true;
				}
			}
			return false;
		}
	}
}
