using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public sealed class ClientState
	{
		public string UserId { get; set; }
		public string ActiveGroup { get; set; }

		public static ClientState Default
		{
			get { return new ClientState(); }
		}
	}
}
