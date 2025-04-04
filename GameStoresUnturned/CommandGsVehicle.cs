using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using UnityEngine;
using Newtonsoft.Json;
using Steamworks;

namespace GameStoresUnturned
{
	internal class CommandGsVehicle : IRocketCommand
	{

		public AllowedCaller AllowedCaller
		{
			get
			{
				return AllowedCaller.Both;
			}
		}

		public bool RunFromConsole
		{
			get
			{
				return true;
			}
		}

		public string Name
		{
			get
			{
				return "gsvehicle";
			}
		}

		public string Help
		{
			get
			{
				return "Command for spawning vehicles";
			}
		}

		public string Syntax
		{
			get
			{
				return "<player>";
			}
		}

		public List<string> Aliases
		{
			get
			{
				return new List<string>();
			}
		}
		public List<string> Permissions
		{
			get
			{
				return new List<string>
				{
					"gsvehicle"
				};
			}
		}
		public void Execute(IRocketPlayer caller, string[] command)
		{
			if (command.Length != 2)
			{
				UnturnedChat.Say(caller, "Syntax gsvehicle <steamid> <vehicleid>");
				return;
			}
			ushort num = Convert.ToUInt16(command[1]);
			Player player = PlayerTool.getPlayer(command[0]);
			if (command[0].Length == 17)
			{
				player = PlayerTool.getPlayer(new CSteamID(Convert.ToUInt64(command[0])));
			}
			if (player == null)
			{
				UnturnedChat.Say(caller, "Player not found");
				return;
			}
			if (!CommandGsVehicle.IsValidVehicleId(num))
			{
				UnturnedChat.Say(caller, "Vehicle not found");
				return;
			}
			VehicleTool.giveVehicle(player, num);
			UnturnedChat.Say(caller, "Vehicle gived");
		}

        private static void SpawnVehicle(Vector3 pos, ushort id)
        {
            if (Physics.Raycast(
                pos + Vector3.up * 16f,
                Vector3.down,
                out RaycastHit raycastHit, // Объявление переменной в вызове метода
                32f,
                RayMasks.BLOCK_VEHICLE))
            {
                pos.y = raycastHit.point.y + 16f;
            }
            VehicleManager.spawnVehicleV2(id, pos, Quaternion.identity);
        }

        private static bool IsValidVehicleId(ushort id)
		{
			return Assets.find(EAssetType.VEHICLE, id) is VehicleAsset;
		}

		public LoadConfig config = new LoadConfig();
		private GameStoresUnturned GameStores = new GameStoresUnturned();
	}
}
