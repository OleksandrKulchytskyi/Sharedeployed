using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber
{
	public static class LocalStateContainer
	{
		public static Messenger LoginMessenger = new Messenger();
	}
}
