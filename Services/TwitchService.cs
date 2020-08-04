﻿using Microsoft.Extensions.Logging;
using StreamMultiChat.Blazor.Events;
using StreamMultiChat.Blazor.Settings;
using System;
using System.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using ChatMessage = StreamMultiChat.Blazor.Events.ChatMessage;

namespace StreamMultiChat.Blazor.Services
{
	public class TwitchService
	{
		private readonly TwitchSettings _settings;
		private readonly ILogger<TwitchService> _logger;
		private TwitchClient _client;
		private bool _correctlyConnected = false;
		//private List<string> _channels = new List<string>();
		//private List<JoinedChannel> _channels = new List<JoinedChannel>();

		public bool _connected => _client.IsConnected;

		public event EventHandler<ChatMessageReceivedEventArgs> OnMessageReceived;

		public TwitchService(TwitchSettings settings, ILogger<TwitchService> logger)
		{
			_settings = settings;
			_logger = logger;



			CreateClient();
			if (_client != null)
			{
				var creds = CreateCredentials();
				Initialize(creds);
				ConfigureHandlers();
			}
		}

		public void Connect()
		{
			if (_client != null)
			{
				_client.Connect();
				_logger.LogInformation("Connecting to Twitch");
			}
		}

		public void Disconnect()
		{
			_client.Disconnect();
		}

		public void JoinChannel(string channel)
		{

			if (_correctlyConnected)
			{
				var clientChannel = _client.JoinedChannels.Where(c => c.Channel.ToLower() == channel.ToLower()).FirstOrDefault();
				if (clientChannel is null)
				{
					_client.JoinChannel(channel);
					_logger.LogInformation($"Joined channel : {channel}");
				}
			}
			else
			{
				System.Threading.Thread.Sleep(1000);
				JoinChannel(channel);
			}
		}


		private void MessageReceived(ChatMessageReceivedEventArgs e)
		{
			var handler = OnMessageReceived;
			handler.Invoke(this, e);
		}

		public void SendMessage(string message)
		{
			foreach (var channel in _client.JoinedChannels)
			{
				SendMessage(channel.Channel, message);
			}
		}

		public void SendMessage(string channel, string message)
		{
			_client.SendMessage(channel, message);
			_logger.LogInformation($"Sending to {channel} the Message : {message}");
		}


		private ConnectionCredentials CreateCredentials()
		{
			return new ConnectionCredentials(_settings.Username, _settings.Token);
		}

		private void CreateClient()
		{
			var clientOptions = new ClientOptions
			{
				MessagesAllowedInPeriod = 750,
				ThrottlingPeriod = TimeSpan.FromSeconds(30)
			};
			var customClient = new WebSocketClient(clientOptions);
			_client = new TwitchClient(customClient);

		}

		private void Initialize(ConnectionCredentials creds)
		{
			_client.Initialize(creds);
		}

		private void ConfigureHandlers()
		{
			if (_client != null)
			{
				_client.OnConnected += OnConnected;
				_client.OnMessageReceived += MessageReceived;
			}
		}

		private void OnConnected(object sender, OnConnectedArgs args)
		{
			_logger.LogInformation($"Connection To Twitch Started.");
			_correctlyConnected = true;
		}

		private void MessageReceived(object sender, OnMessageReceivedArgs args)
		{

			_logger.LogInformation($"Message Received from Chat user : {args.ChatMessage.Username} message: {args.ChatMessage.Message}");

			var eventArgs = new ChatMessageReceivedEventArgs(new ChatMessage(
					args.ChatMessage.Message,
					args.ChatMessage.IsVip,
					args.ChatMessage.IsSubscriber,
					args.ChatMessage.IsModerator,
					args.ChatMessage.IsMe,
					args.ChatMessage.IsBroadcaster,
					args.ChatMessage.SubscribedMonthCount,
					args.ChatMessage.Id,
					args.ChatMessage.Channel,
					args.ChatMessage.Bits,
					args.ChatMessage.IsHighlighted,
					args.ChatMessage.UserId,
					args.ChatMessage.Username
					));

			MessageReceived(eventArgs);

		}
	}
}