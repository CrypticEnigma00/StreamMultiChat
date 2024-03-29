﻿using StreamMultiChat.Blazor.Events;
using StreamMultiChat.Blazor.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamMultiChat.Blazor.Services
{
	public class ChannelService
	{
		private readonly AuthenticationService _authenticationService;
		private readonly TwitchService _twitchService;
		private readonly MacroService _macroService;

		public IList<Channel> Channels { get; } = new List<Channel>();
		public Channel AllChannel { get; }

		public event EventHandler<DisplayMessage> OnMessageReceived;
		

		public ChannelService(TwitchService twitchService, AuthenticationService authenticationService, MacroService macroService)
		{
			_authenticationService = authenticationService;
			_twitchService = twitchService;
			_macroService = macroService;

			Channels.Add(new Channel("all", _twitchService, _macroService, _authenticationService));
			AllChannel = Channels.FirstOrDefault(c => c.Id == "all");
			_twitchService.OnMessageReceived += ReceiveMessageHandler;
			_twitchService.OnModReceived += ReceiveModHandler;
			_twitchService.OnWhisperReceived += WhisperRecieved;
			_twitchService.Connect();
		}

		

		public async Task<Channel> GetChannel(string channelId)
		{
			return await Task.FromResult(Channels.FirstOrDefault(c => c.Id == channelId));
		}

		private bool CheckForChannel(string channelName)
		{
			var channelCount = Channels.Where(c => c.Id == channelName).Count();
			return channelCount > 0 ? true : false;
		}

		public async Task AddChannel(string channelName)
		{
			if (!CheckForChannel(channelName))
			{
				var channel = new Channel(channelName, _twitchService, _macroService, _authenticationService);
				channel.AddChannelString(channelName);

				if (channelName != "all")
				{
					await channel.JoinChannel();
				}

				Channels.Add(channel);

				AllChannel.AddChannelString(channelName);
			}
			await Task.CompletedTask;
		}

		public async Task RemoveChannel(Channel channel)
		{
			if (channel.Id == "all")
			{
				foreach(Channel channel1 in Channels.Where(c => c.Id != "all").ToList())
				{
					await channel1.LeaveChannel();
					AllChannel.RemoveChannelString(channel1);
					Channels.Remove(channel1);
				}
			}
			else
			{
				await channel.LeaveChannel();
				AllChannel.RemoveChannelString(channel);
				Channels.Remove(channel);
			}
		}

		public async Task JoinChannels()
		{
			foreach (var channel in AllChannel.ChannelStrings)
			{
				await _twitchService.JoinChannel(channel);
				await _twitchService.GetModerators(channel);
			}
		}

		private void ReceiveModHandler(object sender, ModReceivedEventArgs e)
		{
			var channel = GetChannel(e.Channel).Result;
			channel.AddModerators(e.Mods);
		}
		private void WhisperRecieved(object sender, WhisperReceivedEventArgs e)
		{
			MessageReceived(e.WhisperMessage.ToString(), false, null, e.WhisperMessage.Username);
		}

		private void ReceiveMessageHandler(object sender, ChatMessageReceivedEventArgs e)
		{
			var channel = GetChannel(e.ChatMessage.Channel).Result;


			MessageReceived(e.ChatMessage.ToString(), channel.IsModerator(), channel, e.ChatMessage.Username);

		}

		private void MessageReceived(string e, bool modControl, Channel channel, string user)
		{
			var handler = OnMessageReceived;
			var displayMessage = new DisplayMessage(e, modControl, channel, user);
			handler.Invoke(this, displayMessage);
		}
	}
}
