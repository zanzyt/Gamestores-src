using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

namespace GameStoresUnturned
{
	public class CommandStore : IRocketCommand
	{
		public AllowedCaller AllowedCaller
		{
			get
			{
				return AllowedCaller.Player;
			}
		}

		public bool RunFromConsole
		{
			get
			{
				return false;
			}
		}

		public string Name
		{
			get
			{
				return "store";
			}
		}

		public string Help
		{
			get
			{
				return "Command for store";
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
					"store"
				};
			}
		}

		private void SendGived(Dictionary<string, string> Args)
		{
			GameStoresUnturned.WebrequesAnswer webrequesAnswer = this.GameStores.WebRequestGet(Args);
			if (webrequesAnswer.code == 200)
			{
				Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(webrequesAnswer.response);
				if (dictionary != null && (Convert.ToInt32(dictionary["code"]) == 100 || Convert.ToInt32(dictionary["code"]) == 107))
				{
					return;
				}
			}
			Logger.LogError("Api do not responded to request. Trying again (Player received items but it was not recorded)");
			this.GameStores.RequestList.Insert(0, Args);
		}
		private string ReplaceVarsInCommand(UnturnedPlayer player, string line)
		{
			string text = line;
			string text2 = line;
			line = line.ToLower();
			int num = line.IndexOf("%steamid%");
			if (num != -1)
			{
				text2 = string.Format("{0}{1}{2}", text.Substring(0, num), player.CSteamID, text.Substring(num + 9));
			}
			int num2 = line.IndexOf("%username%");
			if (num2 != -1)
			{
				text = text2;
				text2 = text.Substring(0, num2) + player.DisplayName + text.Substring(num2 + 10);
			}
			return text2;
		}

		public void Execute(IRocketPlayer Player, string[] command)
		{
			UnturnedPlayer unturnedPlayer = (UnturnedPlayer)Player;
			GameStoresUnturned.WebrequesAnswer webrequesAnswer = this.GameStores.WebRequestGet(new Dictionary<string, string>
			{
				{
					"items",
					"true"
				},
				{
					"steam_id",
					string.Format("{0}", unturnedPlayer.CSteamID)
				}
			});
			string a = (command.Length != 0) ? command[0].ToString().ToLower() : "";
			if (GameStoresUnturned.Instance.Configuration.Instance.secretKey != "KEY" || GameStoresUnturned.Instance.Configuration.Instance.shopId != 0)
			{
				int code = webrequesAnswer.code;
				if (code == 0)
				{
					UnturnedChat.Say(Player, "Корзина недоступна");
					Logger.LogError("Api does not responded to a request");
					return;
				}
				if (code != 200)
				{
					if (code != 404)
					{
						UnturnedChat.Say(Player, "Корзина недоступна");
						Logger.LogError("Api does not responded to a request");
						return;
					}
					UnturnedChat.Say(Player, "Корзина недоступна");
					Logger.LogError("Response code: 404, please check your configurations");
					return;
				}
				else
				{
					Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(webrequesAnswer.response);
					if (dictionary == null)
					{
						UnturnedChat.Say(Player, "Корзина недоступна");
						Logger.LogError("Response API Error");
						return;
					}
					if (dictionary["code"] != null)
					{
						int num = Convert.ToInt32(dictionary["code"]);
						if (num == 100)
						{
							List<CommandStore.ApiItem> list = JsonConvert.DeserializeObject<List<CommandStore.ApiItem>>(dictionary["data"].ToString());
							if (!(a == "all"))
							{
								if (!(a == "list"))
								{
									if (a == "page")
									{
										goto IL_489;
									}
									if (!(a == "take"))
									{
										goto IL_8E8;
									}
									goto IL_628;
								}
							}
							else
							{
								int num2 = 0;
								using (List<CommandStore.ApiItem>.Enumerator enumerator = list.GetEnumerator())
								{
									while (enumerator.MoveNext())
									{
										CommandStore.ApiItem item = enumerator.Current;
										if (num2 > 10)
										{
											UnturnedChat.Say(Player, "Вы не можете получить больше 10 предметов за раз");
											break;
										}
										if (!this.GameStores.RequestList.Exists((Dictionary<string, string> x) => x.ContainsKey("gived") && x["gived"] == "true" && x.ContainsKey("id") && x["id"] == item.id))
										{
											if (item.type == "item")
											{
												if (Convert.ToInt32(item.amount) > 255)
												{
													item.amount = "255";
												}
												if (unturnedPlayer.GiveItem((ushort)Convert.ToInt32(item.item_id), (byte)Convert.ToInt32(item.amount)))
												{
													UnturnedChat.Say(Player, "Получен товар из магазина:\"" + item.name + "\" в количестве " + item.amount);
													this.SendGived(new Dictionary<string, string>
													{
														{
															"gived",
															"true"
														},
														{
															"id",
															item.id ?? ""
														}
													});
												}
											}
											else if (item.type == "command")
											{
												foreach (string line in item.command.ToString().Replace('\n', '|').Split(new char[]
												{
													'|'
												}))
												{
													string text = this.ReplaceVarsInCommand(unturnedPlayer, line);
													if (!R.Commands.Execute(new ConsolePlayer(), string.Join(" ", new string[]
													{
														text
													})))
													{
														Commander.execute(new CSteamID(0UL), string.Join(" ", new string[]
														{
															text
														}));
													}
												}
												this.SendGived(new Dictionary<string, string>
												{
													{
														"gived",
														"true"
													},
													{
														"id",
														item.id ?? ""
													}
												});
												UnturnedChat.Say(Player, "Получен товар из магазина:\"" + item.name + "\"");
											}
											num2++;
										}
									}
									return;
								}
							}
							int num3 = 0;
							UnturnedChat.Say(Player, "------ Страница 1 ------");
							using (List<CommandStore.ApiItem>.Enumerator enumerator = list.GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									CommandStore.ApiItem apiItem = enumerator.Current;
									if (num3 >= this.pageItems)
									{
										if (list.Count > this.pageItems)
										{
											UnturnedChat.Say(Player, "Следующая страница /store page 2");
										}
										UnturnedChat.Say(Player, "Получить конкретный товар /store take 1 (порядковый номер)");
										break;
									}
									UnturnedChat.Say(Player, string.Format("{0}. \"{1}\" {2} шт.", num3 + 1, apiItem.name, apiItem.amount));
									num3++;
								}
								return;
							}
							IL_489:
							int num4 = 1;
							try
							{
								num4 = ((command.Length > 1) ? Convert.ToInt32(command[1].ToString().ToLower()) : 1);
							}
							catch (Exception)
							{
								num4 = 1;
							}
							int num5 = list.Count % this.pageItems;
							int num6 = Convert.ToInt32(list.Count / this.pageItems);
							if (num5 > 0)
							{
								num6++;
							}
							Logger.LogError(string.Format("num {0}", num4));
							Logger.LogError(string.Format("pages {0}", num6));
							if (num4 < 1 || num4 > num6)
							{
								UnturnedChat.Say(Player, "Страница не существует");
								return;
							}
							UnturnedChat.Say(Player, string.Format("------ Страница {0} ------", num4));
							int num7 = 0;
							using (List<CommandStore.ApiItem>.Enumerator enumerator = list.GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									CommandStore.ApiItem apiItem2 = enumerator.Current;
									if (num7 + 1 >= this.pageItems * (num4 - 1))
									{
										UnturnedChat.Say(Player, string.Format("{0}. \"{1}\" {2} шт.", num7 + 1, apiItem2.name, apiItem2.amount));
									}
									if (num7 + 1 >= this.pageItems * num4 || num7 + 1 >= list.Count)
									{
										UnturnedChat.Say(Player, "Получить конкретный товар /store take num (номер в списке)");
										if (list.Count > this.pageItems * num4)
										{
											UnturnedChat.Say(Player, string.Format("Следующая страница /store page {0}", num4 + 1));
											break;
										}
										if (num4 > 1)
										{
											UnturnedChat.Say(Player, string.Format("Предыдущая страница /store page {0}", num4 - 1));
										}
										break;
									}
									else
									{
										num7++;
									}
								}
								return;
							}
							IL_628:
							try
							{
								num4 = ((command.Length > 1) ? Convert.ToInt32(command[1].ToString().ToLower()) : 1);
							}
							catch (Exception)
							{
								UnturnedChat.Say(Player, "Недопустимый номер");
								return;
							}
							if (num4 > list.Count || num4 < 1)
							{
								UnturnedChat.Say(Player, "Товара с таким номером нет в корзине");
								return;
							}
							int num8 = 0;
							using (List<CommandStore.ApiItem>.Enumerator enumerator = list.GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									CommandStore.ApiItem item = enumerator.Current;
									num8++;
									if (num8 == num4)
									{
										if (this.GameStores.RequestList.Exists((Dictionary<string, string> x) => x.ContainsKey("gived") && x["gived"] == "true" && x.ContainsKey("id") && x["id"] == item.id))
										{
											UnturnedChat.Say(Player, "Этот товар уже был выдан");
											break;
										}
										if (item.type == "item")
										{
											if (Convert.ToInt32(item.amount) > 255)
											{
												item.amount = "255";
											}
											if (unturnedPlayer.GiveItem((ushort)Convert.ToInt32(item.item_id), (byte)Convert.ToInt32(item.amount)))
											{
												UnturnedChat.Say(Player, "Получен товар из магазина:\"" + item.name + "\" в количестве " + item.amount);
												this.SendGived(new Dictionary<string, string>
												{
													{
														"gived",
														"true"
													},
													{
														"id",
														item.id ?? ""
													}
												});
											}
										}
										else if (item.type == "command")
										{
											foreach (string line2 in item.command.ToString().Replace('\n', '|').Split(new char[]
											{
												'|'
											}))
											{
												string text2 = this.ReplaceVarsInCommand(unturnedPlayer, line2);
												if (!R.Commands.Execute(new ConsolePlayer(), string.Join(" ", new string[]
												{
													text2
												})))
												{
													Commander.execute(new CSteamID(0UL), string.Join(" ", new string[]
													{
														text2
													}));
												}
											}
											this.SendGived(new Dictionary<string, string>
											{
												{
													"gived",
													"true"
												},
												{
													"id",
													item.id ?? ""
												}
											});
											UnturnedChat.Say(Player, "Получен товар из магазина:\"" + item.name + "\"");
										}
									}
								}
								return;
							}
							IL_8E8:
							UnturnedChat.Say(Player, "Доступные команды:");
							UnturnedChat.Say(Player, "/store all - Получить первые 10 товаров из корзины");
							UnturnedChat.Say(Player, "/store list - Показать первую страницу товаров в корзине");
							UnturnedChat.Say(Player, "/store page num - Показать страницу num корзины");
							UnturnedChat.Say(Player, "/store take num - Получить товар под номером num");
							return;
						}
						if (num == 104)
						{
							UnturnedChat.Say(Player, "Ваша корзина пуста");
							return;
						}
						if (!this.GameStores.initializated)
						{
							UnturnedChat.Say(Player, "Плагин не настроен");
							return;
						}
						UnturnedChat.Say(Player, "Произошла ошибка");
						return;
					}
				}
			}
			else
			{
				UnturnedChat.Say(Player, "Плагин не настроен/настроен не правильно");
			}
		}

		public LoadConfig config = new LoadConfig();
		private GameStoresUnturned GameStores = new GameStoresUnturned();
		private int pageItems = 10;
		public class ApiItem
		{
			[JsonProperty("amount")]
			public string amount { get; set; }

			[JsonProperty("name")]
			public string name { get; set; }

			[JsonProperty("type")]
			public string type { get; set; }

			[JsonProperty("id")]
			public string id { get; set; }

			[JsonProperty("command")]
			public string command { get; set; }

			[JsonProperty("item_id")]
			public string item_id { get; set; }
		}
	}
}
