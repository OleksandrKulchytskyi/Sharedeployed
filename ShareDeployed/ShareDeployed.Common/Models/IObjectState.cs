using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public interface IObjectState
	{
		State State { get; set; }
	}

	public enum State
	{
		Added,
		Modified,
		Deleted,
		Unchanged
	}

	public static class EntityStateHelper
	{
		public static System.Data.EntityState ConvertState(this State entStata)
		{
			switch (entStata)
			{
				case State.Added: return System.Data.EntityState.Added;
				case State.Deleted: return System.Data.EntityState.Deleted;
				case State.Modified: return System.Data.EntityState.Modified;

				default:
					return System.Data.EntityState.Unchanged;
			}
		}

		//Only use with short lived DbContext!!!
		public static void ApplyStateChanges(this DbContext context)
		{
			foreach (var entry in context.ChangeTracker.Entries<IObjectState>())
			{
				IObjectState stateInfo = entry.Entity;
				if (stateInfo != null)
					entry.State = stateInfo.State.ConvertState();
			}
		}
	}
}
