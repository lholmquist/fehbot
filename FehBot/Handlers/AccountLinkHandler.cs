﻿using System;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Bson;
using FehBot.Vo;
using FehBot.DBUtil;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using MongoDB.Bson.IO;
using System.Text;

namespace FehBot.Handlers
{
	public class AccountLinkHandler : IHandler
	{
		private readonly Regex CodeGetExpression = new Regex(@"^\?code (\d{8})$");

		public AccountLinkHandler ()
		{
		}

		public void handle (RegistrationInfoFactory infoFactory, IrcDotNet.IrcClient client, FehBot bot, MongoDB.Driver.IMongoDatabase db, IrcDotNet.IrcUser from, IrcDotNet.IrcChannel to, string message)
		{
			if (to == null && isCode (message)) 
			{
				string code = parseCodeRequest(message);
				string nick = from.NickName;
				var document = getForCode (nick.Trim(), code.Trim(), db);
				if (document != null) {
					addListener (document, db);
					var link = new JObject ();

					link.Add("nick", document.Nick);
					link.Add("remoteUserName", document.RemoteUserName);	

					callWebHook(db, link);
				}

			}
		}

		private void addListener (NickNameLink document, IMongoDatabase db)
		{
			db.UpdatdeLinkRequestToEventUser (document);

		}

		public void callWebHook (IMongoDatabase db, JObject webHookBody)
		{
			db.GetWebHookForAction ("link").ForEach ((hook) => {
				string apiKey = hook.ApiKey;
				Uri callbackUrl = new Uri(hook.CallbackUrl);
				string secret = hook.Secret;

				using (HttpClient client = new HttpClient ()) {
					client.DefaultRequestHeaders.Accept.Clear();
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

					webHookBody.Add("apiKey",apiKey);
					webHookBody.Add("secret",secret);
					webHookBody.Add("action","link");
					Console.Out.WriteLine("Posting request, maybe");
					client.DefaultRequestHeaders.Accept.Clear();
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); 
					var content = new StringContent(webHookBody.ToString(),Encoding.UTF8, "application/json");



					client.PostAsync(callbackUrl, content).Wait();
				}
			});

		}

		private NickNameLink getForCode(string nick, string code, IMongoDatabase db)
		{
			return db.GetNickNameRequest (nick, code);

		}

		private string parseCodeRequest (string message)
		{
			return CodeGetExpression.Match (message).Groups [1].ToString();
		}

		private bool isCode (string message)
		{
			return CodeGetExpression.IsMatch (message);
		}
	}
}

