using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.DataAccess.Initializer
{
	public class ShareDbInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<DataAccess.ShareDeployedContext>
	{
		public ShareDbInitializer()
		{
			using (var dep = new DataAccess.ShareDeployedContext())
			{
				this.InitializeDatabase(dep);
				this.Seed(dep);
			}
		}

		protected override void Seed(DataAccess.ShareDeployedContext context)
		{
			if (context.Database.CompatibleWithModel(false) == true)
				context.Database.Initialize(true);
			//context.Database.Create();

			var DBScript = CreateDatabaseScript(context);
			if (!string.IsNullOrEmpty(DBScript))
			{
			}

			base.Seed(context);
		}

		public static string CreateDatabaseScript(System.Data.Entity.DbContext context)
		{
			return ((System.Data.Entity.Infrastructure.IObjectContextAdapter)context).ObjectContext.CreateDatabaseScript();
		}
	}
}
