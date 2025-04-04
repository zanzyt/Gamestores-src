using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Commands;
using UnityEngine;
using Steamworks;
using Rocket.Unturned.Player;
using System.CodeDom;

namespace GameStoresUnturned
{
	internal class CommandGsPermissions : IRocketCommand
	{
		public AllowedCaller AllowedCaller
		{
			get
			{
				return AllowedCaller.Both;
			}
		}


		public string Name
		{
			get
			{
				return "gspermissions";
			}
		}

		public string Help
		{
			get
			{
				return "Command for giving permissions";
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
					"gspermissions"
				};
			}
		}

		public void Execute(IRocketPlayer caller, string[] command)
		{
			if ((command.Length != 3 || !(command[0] == "remove")) && (command.Length != 4 || !(command[0] == "add")) && (command.Length != 2 || !(command[0] == "user")))
			{
				UnturnedChat.Say(caller, U.Translate("command_generic_invalid_parameter", Array.Empty<object>()));
				throw new WrongUsageOfCommandException(caller, this);
			}
			if (command[0] == "user")
			{
				this.userInfo(caller, command);
				return;
			}
			string a = command[0].ToString().ToLower();
			IRocketPlayer rocketPlayer = UnturnedCommandExtensions.GetUnturnedPlayerParameter(command, 1);
			if (rocketPlayer == null)
			{
				rocketPlayer = UnturnedCommandExtensions.GetRocketPlayerParameter(command, 1);
			}
			string text = command[2].ToString();
			if (rocketPlayer == null)
			{
				UnturnedChat.Say(caller, "Player not found");
				return;
			}
			if (R.Permissions.GetGroup(text) == null && command[0] != "remove")
			{
				UnturnedChat.Say(caller, "Group " + text + " not found");
				return;
			}
			string key = rocketPlayer.Id ?? "";
			string text2;
			if (rocketPlayer.DisplayName != null)
			{
				text2 = rocketPlayer.DisplayName;
			}
			else
			{
				text2 = rocketPlayer.Id;
			}
			if (!(a == "add"))
			{
				if (!(a == "remove"))
				{
					UnturnedChat.Say(caller, U.Translate("command_generic_invalid_parameter", Array.Empty<object>()));
					throw new WrongUsageOfCommandException(caller, this);
				}
				if (rocketPlayer != null && text != null)
				{
					if (GameStoresUnturned.Permissions.ContainsKey(key) && GameStoresUnturned.Permissions[key].ContainsKey(text))
					{
						GameStoresUnturned.Permissions[key].Remove(text);
						this.GameStores.SavePermissions();
					}
					switch (R.Permissions.RemovePlayerFromGroup(text, rocketPlayer))
					{
                    case RocketPermissionsProviderResult.Success: // 0
                            UnturnedChat.Say(caller, U.Translate("command_p_group_player_removed", new object[]
						{
							text2,
							text
						}));
						return;
					case RocketPermissionsProviderResult.DuplicateEntry:
						UnturnedChat.Say(caller, U.Translate("command_p_duplicate_entry", new object[]
						{
							text2,
							text
						}));
						return;
					case RocketPermissionsProviderResult.GroupNotFound:
						UnturnedChat.Say(caller, U.Translate("command_p_group_not_found", new object[]
						{
							text2,
							text
						}));
						return;
					case RocketPermissionsProviderResult.PlayerNotFound:
						UnturnedChat.Say(caller, U.Translate("command_p_player_not_found", new object[]
						{
							text2,
							text
						}));
						return;
					}
					UnturnedChat.Say(caller, U.Translate("command_p_unknown_error", new object[]
					{
						text2,
						text
					}));
					return;
				}
				return;
			}
			else
			{
				if (Convert.ToInt64(command[3]) > 0L)
				{
					long num = Convert.ToInt64(command[3]);
					if (rocketPlayer != null && text != null)
					{
						switch (R.Permissions.AddPlayerToGroup(text, rocketPlayer))
						{
						case RocketPermissionsProviderResult.Success:
							UnturnedChat.Say(caller, U.Translate("command_p_group_player_added", new object[]
							{
								text2,
								text
							}) + " for " + command[3] + " seconds.");
							goto IL_24C;
						case RocketPermissionsProviderResult.DuplicateEntry:
							UnturnedChat.Say(caller, U.Translate("command_p_duplicate_entry", new object[]
							{
								text2,
								text
							}));
							UnturnedChat.Say(caller, "Time has been extended for " + command[3] + " seconds.");
							goto IL_24C;
						case RocketPermissionsProviderResult.GroupNotFound:
							UnturnedChat.Say(caller, U.Translate("command_p_group_not_found", new object[]
							{
								text2,
								text
							}));
							return;
						case RocketPermissionsProviderResult.PlayerNotFound:
							UnturnedChat.Say(caller, U.Translate("command_p_player_not_found", new object[]
							{
								text2,
								text
							}));
							return;
						}
						UnturnedChat.Say(caller, U.Translate("command_p_unknown_error", new object[]
						{
							text2,
							text
						}));
						return;
						IL_24C:
						long value = this.GameStores.ConvertToTimestamp(DateTime.Now) + num;
						if (GameStoresUnturned.Permissions.ContainsKey(key))
						{
							if (GameStoresUnturned.Permissions[key].ContainsKey(text))
							{
								if (GameStoresUnturned.Permissions[key][text] > this.GameStores.ConvertToTimestamp(DateTime.Now))
								{
									Dictionary<string, long> dictionary = GameStoresUnturned.Permissions[key];
									string key2 = text;
									dictionary[key2] += num;
								}
								else
								{
									GameStoresUnturned.Permissions[key][text] = value;
								}
							}
							else
							{
								GameStoresUnturned.Permissions[key].Add(text, value);
							}
						}
						else
						{
							GameStoresUnturned.Permissions.Add(key, new Dictionary<string, long>
							{
								{
									text,
									value
								}
							});
						}
						this.GameStores.SavePermissions();
					}
					return;
				}
				UnturnedChat.Say(caller, "Invalid time");
				return;
			}
		}
		private void userInfo(IRocketPlayer caller, string[] command)
		{
			IRocketPlayer rocketPlayer = UnturnedCommandExtensions.GetUnturnedPlayerParameter(command, 1);
			if (rocketPlayer == null)
			{
				rocketPlayer = UnturnedCommandExtensions.GetRocketPlayerParameter(command, 1);
			}
			if (rocketPlayer == null)
			{
				UnturnedChat.Say(caller, "Player not found");
				return;
			}
			List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(rocketPlayer, true);
			UnturnedChat.Say(caller, rocketPlayer.DisplayName + " groups: ");
			int num = 0;
			foreach (RocketPermissionsGroup rocketPermissionsGroup in groups)
			{
				long num2 = 0L;
				num++;
				if (GameStoresUnturned.Permissions.ContainsKey(rocketPlayer.Id))
				{
					if (GameStoresUnturned.Permissions[rocketPlayer.Id].ContainsKey(rocketPermissionsGroup.DisplayName))
					{
						num2 = GameStoresUnturned.Permissions[rocketPlayer.Id][rocketPermissionsGroup.DisplayName];
					}
					if (GameStoresUnturned.Permissions[rocketPlayer.Id].ContainsKey(rocketPermissionsGroup.Id))
					{
						num2 = GameStoresUnturned.Permissions[rocketPlayer.Id][rocketPermissionsGroup.Id];
					}
				}
				if (num2 > 0L)
				{
					num2 /= 60L;
				}
				string text = (num2 > 0L) ? "({sec} min.)" : "";
				UnturnedChat.Say(caller, string.Format("{0}. {1} ({2}) {3}", new object[]
				{
					num,
					rocketPermissionsGroup.DisplayName,
					rocketPermissionsGroup.Id,
					text
				}));
			}
		}

		public LoadConfig config = new LoadConfig();
		private GameStoresUnturned GameStores = new GameStoresUnturned();
	}
}
