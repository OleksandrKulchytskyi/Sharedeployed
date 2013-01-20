using ShareDeployed.Models;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Web.Mvc;
using WebMatrix.WebData;

namespace ShareDeployed.Filters
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class InitializeSimpleMembershipAttribute : ActionFilterAttribute
	{
		//private static SimpleMembershipInitializer _initializer;
		private static SimpleMembershipInitializer2 _initializer;

		private static object _initializerLock = new object();
		private static bool _isInitialized;

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			// Ensure ASP.NET Simple Membership is initialized only once per app start
			LazyInitializer.EnsureInitialized(ref _initializer, ref _isInitialized, ref _initializerLock);
		}

		private class SimpleMembershipInitializer
		{
			public SimpleMembershipInitializer()
			{
				Database.SetInitializer<UsersContext>(null);

				try
				{
					using (var context = new UsersContext())
					{
						if (!context.Database.Exists())
						{
							MvcApplication.Logger.Info("Database is not exists");

							// Create the SimpleMembership database without Entity Framework migration schema
							((IObjectContextAdapter)context).ObjectContext.CreateDatabase();
						}
						else
							MvcApplication.Logger.Info("Database is exists");
					}

					WebSecurity.InitializeDatabaseConnection("Somee", "UserProfile", "UserId", "UserName", autoCreateTables: true);
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error("SimpleMembershipInitializer", ex);

					//throw new InvalidOperationException("The ASP.NET Simple Membership database could not be initialized."+
					//"For more information, please see http://go.microsoft.com/fwlink/?LinkId=256588", ex);
				}
			}
		}

		private class SimpleMembershipInitializer2
		{
			public SimpleMembershipInitializer2()
			{
				Database.SetInitializer<UsersContext>(null);
				try
				{
					using (var context = new UsersContext())
					{
					}

					WebSecurity.InitializeDatabaseConnection("Somee", "UserProfile", "UserId", "UserName", autoCreateTables: true);
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error("SimpleMembershipInitializer", ex);

					//throw new InvalidOperationException("The ASP.NET Simple Membership database could not be initialized."+
					//"For more information, please see http://go.microsoft.com/fwlink/?LinkId=256588", ex);
				}
			}
		}
	}
}