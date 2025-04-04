using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Steamworks;

namespace GameStoresUnturned
{
	public class GameStoresUnturned : RocketPlugin<LoadConfig>
	{
        public static string DataFilePath
        {
            get
            {
                return Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Plugins/GameStoresUnturned/permissions.json");
            }
        }

        public string ApiDefaultParams
		{
			get
			{
				return string.Format("shop_id={0}&secret={1}&server={2}", GameStoresUnturned.Instance.Configuration.Instance.shopId, GameStoresUnturned.Instance.Configuration.Instance.secretKey, GameStoresUnturned.Instance.Configuration.Instance.serverId);
			}
		}

		public string Request
		{
			get
			{
				return this.ApiLink + "?" + this.ApiDefaultParams;
			}
		}

		protected override void Load()
		{
			GameStoresUnturned.Instance = this;
			if (GameStoresUnturned.Instance.Configuration.Instance.secretKey == "KEY" || GameStoresUnturned.Instance.Configuration.Instance.shopId == 0)
			{
				Rocket.Core.Logging.Logger.LogError("Plugin isn't configured");
				this.initializated = false;
			}
			else
			{
				this.initializated = true;
			}
			if (!File.Exists(GameStoresUnturned.DataFilePath))
			{
				this.SavePermissions();
				UnturnedChat.Say(new ConsolePlayer(), "Created permissions file");
			}
			else
			{
				GameStoresUnturned.Permissions = GameStoresUnturned.DeserializeFile(GameStoresUnturned.DataFilePath);
				UnturnedChat.Say(new ConsolePlayer(), "Permissions loaded");
			}
			this.lastCalledSendInfo = DateTime.Now;
			this.lastEventsSended = DateTime.Now;
			this.lastCalledCheckExpire = DateTime.Now;
			this.lastCalledAddEvent = DateTime.Now;
			UnturnedPlayerEvents.OnPlayerDeath += new UnturnedPlayerEvents.PlayerDeath(this.UnturnedPlayerEvents_OnPlayerDeath);
			U.Events.OnPlayerDisconnected += new UnturnedEvents.PlayerDisconnected(this.UnturnedPlayerEvents_OnPlayerDisconnected);
		}

		public void SavePermissions()
		{
			File.WriteAllText(GameStoresUnturned.DataFilePath, string.Empty);
			GameStoresUnturned.Serialize(GameStoresUnturned.DataFilePath, GameStoresUnturned.Permissions);
		}

        public static void Serialize(string filePath, object obj)
        {
            string contents = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented // Исправлено
            });
            File.WriteAllText(filePath, contents);
        }

        public static Dictionary<string, Dictionary<string, long>> DeserializeFile(string filePath)
		{
			string text = File.ReadAllText(filePath);
			if (text.Length > 0)
			{
				return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, long>>>(text);
			}
			GameStoresUnturned.Serialize(GameStoresUnturned.DataFilePath, GameStoresUnturned.Permissions);
			return new Dictionary<string, Dictionary<string, long>>();
		}

		private void UnturnedPlayerEvents_OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
		{
		}

		private void UnturnedPlayerEvents_OnPlayerDisconnected(UnturnedPlayer player)
		{
		}

		public long ConvertToTimestamp(DateTime value)
		{
			return (long)(value - GameStoresUnturned.Epoch).TotalSeconds;
		}

		private void DoMainApiError()
		{
			if (!this.ApiError)
			{
				if (this.ApiErrorsCount == 0)
				{
					this.ApiErrorsTime = DateTime.Now;
					this.ApiErrorsCount++;
					return;
				}
				if ((DateTime.Now - this.ApiErrorsTime).TotalSeconds < 600.0)
				{
					this.ApiErrorsCount++;
					if (this.ApiErrorsCount >= 3)
					{
						this.ApiError = true;
						this.ApiErrorsCount = 0;
						this.ApiErrorsTime = DateTime.Now;
						Rocket.Core.Logging.Logger.LogWarning("Set reserve api link");
						this.ApiLink = this.ReserveLink;
						return;
					}
				}
				else
				{
					this.ApiErrorsCount = 0;
				}
			}
		}

		private void TryMainApi()
		{
			if (this.ApiError && (DateTime.Now - this.ApiErrorsTime).TotalSeconds > 30.0)
			{
				if (this.WebRequestPost(new Dictionary<string, string>(), this.MainLink).code == 200)
				{
					this.ApiError = false;
					this.ApiErrorsTime = DateTime.Now;
					this.ApiLink = this.MainLink;
					this.ApiErrorsCount = 0;
					Rocket.Core.Logging.Logger.LogWarning("Return to main api link");
					return;
				}
				this.ApiErrorsTime = DateTime.Now;
			}
		}

		public GameStoresUnturned.WebrequesAnswer WebRequestGet(Dictionary<string, string> Args)
		{
			GameStoresUnturned.WebrequesAnswer result;
			try
			{
				string text = "";
				foreach (KeyValuePair<string, string> keyValuePair in Args)
				{
					text = string.Concat(new string[]
					{
						text,
						"&",
						keyValuePair.Key,
						"=",
						keyValuePair.Value
					});
				}
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(this.ApiLink + "?" + this.ApiDefaultParams + text);
				httpWebRequest.UserAgent = "GameStores Plugin";
				httpWebRequest.Timeout = 1500;
				httpWebRequest.Credentials = CredentialCache.DefaultCredentials;
				WebResponse response = httpWebRequest.GetResponse();
				StreamReader streamReader = new StreamReader(response.GetResponseStream());
				string response2 = streamReader.ReadToEnd();
				streamReader.Close();
				response.Close();
				GameStoresUnturned.WebrequesAnswer webrequesAnswer = new GameStoresUnturned.WebrequesAnswer();
				webrequesAnswer.code = Convert.ToInt32(((HttpWebResponse)response).StatusCode);
				webrequesAnswer.response = response2;
				if (!text.Contains("gived"))
				{
					this.TryMainApi();
				}
				result = webrequesAnswer;
			}
			catch
			{
				GameStoresUnturned.WebrequesAnswer webrequesAnswer2 = new GameStoresUnturned.WebrequesAnswer();
				webrequesAnswer2.code = 0;
				webrequesAnswer2.response = null;
				this.DoMainApiError();
				result = webrequesAnswer2;
			}
			return result;
		}

		public GameStoresUnturned.WebrequesAnswer WebRequestPost(Dictionary<string, string> Args, string link = null)
		{
			string requestUriString = this.ApiLink;
			if (link != null)
			{
				requestUriString = link;
			}
			GameStoresUnturned.WebrequesAnswer result;
			try
			{
				string text = this.ApiDefaultParams;
				foreach (KeyValuePair<string, string> keyValuePair in Args)
				{
					text = string.Concat(new string[]
					{
						text,
						"&",
						keyValuePair.Key,
						"=",
						keyValuePair.Value
					});
				}
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
				httpWebRequest.Method = "POST";
				httpWebRequest.UserAgent = "GameStores Plugin";
				httpWebRequest.ContentType = "application/x-www-form-urlencoded";
				httpWebRequest.ContentLength = (long)bytes.Length;
				httpWebRequest.Timeout = 2000;
				httpWebRequest.Credentials = CredentialCache.DefaultCredentials;
				httpWebRequest.GetRequestStream().Write(bytes, 0, bytes.Length);
				WebResponse response = httpWebRequest.GetResponse();
				StreamReader streamReader = new StreamReader(response.GetResponseStream());
				string response2 = streamReader.ReadToEnd();
				streamReader.Close();
				response.Close();
				GameStoresUnturned.WebrequesAnswer webrequesAnswer = new GameStoresUnturned.WebrequesAnswer();
				webrequesAnswer.code = Convert.ToInt32(((HttpWebResponse)response).StatusCode);
				webrequesAnswer.response = response2;
				if (link == null)
				{
					this.TryMainApi();
				}
				result = webrequesAnswer;
			}
			catch
			{
				GameStoresUnturned.WebrequesAnswer webrequesAnswer2 = new GameStoresUnturned.WebrequesAnswer();
				webrequesAnswer2.code = 0;
				webrequesAnswer2.response = null;
				this.DoMainApiError();
				result = webrequesAnswer2;
			}
			return result;
		}

		private void AddEventsDataToList()
		{
		}

		private void CheckExpiredTime()
		{
			List<string> list = new List<string>();
			long num = this.ConvertToTimestamp(DateTime.Now);
			foreach (KeyValuePair<string, Dictionary<string, long>> keyValuePair in GameStoresUnturned.Permissions)
			{
				foreach (KeyValuePair<string, long> keyValuePair2 in keyValuePair.Value)
				{
					if (Convert.ToInt64(keyValuePair2.Value) < num)
					{
						list.Add("gspermissions remove " + keyValuePair.Key + " " + keyValuePair2.Key);
					}
				}
			}
			foreach (string text in list)
			{
				if (!R.Commands.Execute(new ConsolePlayer(), text))
				{
					Commander.execute(new CSteamID(0UL), text);
				}
			}
		}
		public bool SendInfo()
		{
			bool result = false;
			if (this.RequestList.Count >= 1)
			{
				int num = 0;
				if (this.RequestList.Count > 40)
				{
					num = this.RequestList.Count / 10;
				}
				else if (this.RequestList.Count >= 5)
				{
					num = 4;
				}
				for (int i = num; i >= 0; i--)
				{
					GameStoresUnturned.WebrequesAnswer webrequesAnswer = new GameStoresUnturned.WebrequesAnswer();
					webrequesAnswer.code = 0;
					if (this.RequestList[i].ContainsKey("method") && this.RequestList[i]["method"] == "topData")
					{
						webrequesAnswer = this.WebRequestPost(this.RequestList[i], null);
					}
					else
					{
						webrequesAnswer = this.WebRequestGet(this.RequestList[i]);
					}
					if (webrequesAnswer.code != 200)
					{
						result = true;
						break;
					}
					this.RequestList.RemoveAt(i);
				}
			}
			return result;
		}

		private void startThreads()
		{
		}
		private void CheckTime()
		{
			DateTime dateTime = this.lastCalledSendInfo;
			DateTime dateTime2 = this.lastCalledCheckExpire;
			DateTime dateTime3 = this.lastEventsSended;
			DateTime dateTime4 = this.lastCalledAddEvent;
			if ((DateTime.Now - this.lastCalledSendInfo).TotalSeconds > 10.0)
			{
				bool flag = this.SendInfo();
				this.lastCalledSendInfo = DateTime.Now;
				if (!flag)
				{
					this.lastCalledSendInfo.AddSeconds(20.0);
				}
			}
			if ((DateTime.Now - this.lastCalledAddEvent).TotalSeconds > 30.0)
			{
				this.AddEventsDataToList();
				this.lastCalledAddEvent = DateTime.Now;
			}
			if ((DateTime.Now - this.lastCalledCheckExpire).TotalSeconds > 50.0)
			{
				this.CheckExpiredTime();
				this.lastCalledCheckExpire = DateTime.Now;
			}
		}

		public void FixedUpdate()
		{
			this.CheckTime();
		}

		public static GameStoresUnturned Instance;

		public bool initializated;
		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private List<Dictionary<string, object>> Events = new List<Dictionary<string, object>>();
		public List<Dictionary<string, string>> RequestList = new List<Dictionary<string, string>>();
		private DateTime lastCalledSendInfo = DateTime.Now;
		private DateTime lastEventsSended = DateTime.Now;
		private DateTime lastCalledAddEvent = DateTime.Now;

		private DateTime lastCalledCheckExpire = DateTime.Now;
		public static Dictionary<string, Dictionary<string, long>> Permissions = new Dictionary<string, Dictionary<string, long>>();
		public static Dictionary<string, Dictionary<string, string>> Vehicles = new Dictionary<string, Dictionary<string, string>>();
		public string MainLink = "https://unt-api.gamestores.app/api/";
		public string ReserveLink = "https://gs.gamestores.app/api/";
		public string ApiLink = "https://unt-api.gamestores.app/api/";
		private int ApiErrorsCount;
		private bool ApiError;
		private DateTime ApiErrorsTime = DateTime.Now;
		public class WebrequesAnswer
		{
			public int code;
			public string response;
		}
	}
}
