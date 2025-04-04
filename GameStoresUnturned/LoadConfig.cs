using System;
using Rocket.API;

namespace GameStoresUnturned
{
	public class LoadConfig : IRocketPluginConfiguration, IDefaultable
	{
		public void LoadDefaults()
		{
			this.shopId = 0;
			this.serverId = 0;
			this.secretKey = "KEY";
			this.topUsers = false;
		}

		public int shopId;
		public int serverId;
		public string secretKey;
		public bool topUsers;
	}
}
