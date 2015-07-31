using System.Runtime.CompilerServices;
using TShockAPI;

namespace RegionEvent
{
	public static class TSPlayerExtensions
	{
		private static ConditionalWeakTable<TSPlayer, PlayerStorage> players = new ConditionalWeakTable<TSPlayer, PlayerStorage>();

		public static PlayerStorage GetPlayerInfo(this TSPlayer tsplayer)
		{
			return players.GetOrCreateValue(tsplayer);
		}
	}
}
