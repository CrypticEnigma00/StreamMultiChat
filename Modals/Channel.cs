﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamMultiChat.Blazor.Modals
{
	public class Channel
	{
		public string Id { get; }

		public List<string> ChannelStrings { get; }

		public Channel(string channelId)
		{
			Id = channelId;
		}

		public void AddChannelString(string channel)
		{
			ChannelStrings.Add(channel);
		}

		public void RemoveChannelString(string channel)
		{
			ChannelStrings.Remove(channel);
		}

		public void RemoveChannelString(Channel channel)
		{
			ChannelStrings.Remove(channel.Id);
		}

		public IEnumerable<Macro> GetChannelMacros(IEnumerable<Macro> macros)
		{
			return macros.Where(m => m.Channel == this);
		}
	}
}