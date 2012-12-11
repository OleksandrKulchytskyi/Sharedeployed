using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Migrations
{
	public class MigrationConfiguration : DbMigrationsConfiguration<MessangerContext>
	{
		public MigrationConfiguration()
		{
			AutomaticMigrationsEnabled = true;
		}
	}
}
