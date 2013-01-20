using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Ninject;
using ShareDeployed.Common.Caching;
using ShareDeployed.DataAccess;
using ShareDeployed.Extension;
using ShareDeployed.Repositories;
using ShareDeployed.Services;
using ShareDeployed.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Routing;

[assembly: WebActivator.PreApplicationStartMethod(typeof(ShareDeployed.Infrastructure.Bootstrapper), "PreAppStart")]
namespace ShareDeployed.Infrastructure
{
	public static class Bootstrapper
	{
		// Background task members
		private static bool _sweeping;

		private static bool _broadcasting;
		private static Timer _timerClaen;
		private static Timer _timerBroadcats;

		private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(10);
		private static readonly TimeSpan _broadcatsInterval = TimeSpan.FromMinutes(int.Parse(ConfigurationManager.AppSettings["broadcastTime"]));
		private const string SqlClient = "System.Data.SqlClient";

		internal static IKernel Kernel = null;
		private static IDependencyResolver _resolver = null;

		public static void PreAppStart()
		{
			// If we're in the VS app domain then do nothing
			if (HostingEnvironment.InClientBuildManager)
				return;

			var kernel = new StandardKernel();

			kernel.Bind<MessangerContext>().To<MessangerContext>();
			kernel.Bind<ShareDeployedContext>().To<ShareDeployedContext>();
			kernel.Bind<Models.UsersContext>().To<Models.UsersContext>();

			kernel.Bind<IMessangerRepository>().To<PersistedRepository>();
			kernel.Bind<Repositories.IAspUserRepository>().To<Repositories.WebSecurityRepository>();

			kernel.Bind<IMessangerService>().To<MessangerService>();

			kernel.Bind<ShareDeployed.Hubs.MessangerHub>().ToMethod(context =>
				  {
					  // I'm doing this manually, since we want the repository instance to be shared between the messanger service and the messanger hub itself
					  var settings = context.Kernel.Get<IAppSettings>();
					  var aspUsrRepos = context.Kernel.Get<IAspUserRepository>();
					  var repository = context.Kernel.Get<IMessangerRepository>();
					  var cache = context.Kernel.Get<ICache>();
					  var crypto = context.Kernel.Get<ICryptoService>();

					  var service = new MessangerService(cache, crypto, repository);
					  return new Hubs.MessangerHub(settings, service, repository, cache, aspUsrRepos);
				  });

			kernel.Bind<ICryptoService>().To<CryptoService>().InSingletonScope();
			kernel.Bind<IAppSettings>().To<AppSettings>().InSingletonScope();
			kernel.Bind<IVirtualPathUtil>().To<VirtualPathUtil>();
			kernel.Bind<IJavaScriptMinifier>().To<AjaxMinMinifier>().InSingletonScope();
			kernel.Bind<ICache>().To<AspNetCache>().InSingletonScope();

			#region for web api controllers

			kernel.Bind<DataAccess.Interfaces.IContext>().To<ShareDeployedContext>();
			kernel.Bind<DataAccess.Interfaces.IRepository<Common.Models.Expense>>().To<DataAccess.Implementation.ExpenseRepostitory>();
			kernel.Bind<DataAccess.Interfaces.IRepository<Common.Models.Revenue>>().To<DataAccess.Implementation.RevenueRepostitory>();
			kernel.Bind<DataAccess.Interfaces.IRepository<Common.Models.User>>().To<DataAccess.Implementation.UserRepostitory>();
			kernel.Bind<DataAccess.Interfaces.IRepository<Common.Models.Word>>().To<DataAccess.Implementation.WordRepostitory>();

			kernel.Bind<DataAccess.Interfaces.IUnityOfWork>().ToMethod(context =>
				{
					var wordRep = kernel.Get<DataAccess.Interfaces.IRepository<Common.Models.Word>>();
					var userRep = kernel.Get<DataAccess.Interfaces.IRepository<Common.Models.User>>();
					var revenueRep = kernel.Get<DataAccess.Interfaces.IRepository<Common.Models.Revenue>>();
					var expenseRep = kernel.Get<DataAccess.Interfaces.IRepository<Common.Models.Expense>>();
					var icontext = kernel.Get<DataAccess.Interfaces.IContext>();

					return new DataAccess.Implementation.UnityOfWork(userRep, expenseRep, revenueRep, wordRep, icontext);
				});

			#endregion for web api controllers

			var serializer = new Microsoft.AspNet.SignalR.Json.JsonNetSerializer(new Newtonsoft.Json.JsonSerializerSettings
			{
				DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat,
			});

			Kernel = kernel;
			_resolver = new ShareDeployed.DependencyResolvers.NinjectDependencyResolver(kernel);
			ShareDeployed.App_Start.SignalRConfig.Register(_resolver);

			//SetupRoutes(kernel);
			//set up repository factory method
			var repositoryFactory = new Func<Repositories.IMessangerRepository>(() => kernel.Get<IMessangerRepository>());

			_timerClaen = new Timer(_ => Sweep(repositoryFactory, _resolver), null, _sweepInterval, _sweepInterval);
			_timerBroadcats = new Timer(_ => BroadCastNewMessages(repositoryFactory, _resolver), null,
								TimeSpan.FromSeconds(40), _broadcatsInterval);

			ClearConnectedClients(repositoryFactory());
		}

		private static void ClearConnectedClients(IMessangerRepository repository)
		{
			try
			{
				repository.RemoveAllClients();
				repository.CommitChanges();
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("ClearConnectedClients", ex);
			}
			finally
			{
				if (repository != null)
					repository.Dispose();
			}
		}

		private static void SetupRoutes(IKernel kernel)
		{
			RouteTable.Routes.MapHttpRoute(name: "MessagesV1", routeTemplate: "api/v1/{controller}/{room}");
			RouteTable.Routes.MapHttpRoute(name: "DefaultFrontPageApi", routeTemplate: "api",
											defaults: new { controller = "ApiFrontPage" });
		}

		public static void DoMigrations()
		{
			var conString = ConfigurationManager.ConnectionStrings["Messanger"];

			if (String.IsNullOrEmpty(conString.ProviderName) ||
				!conString.ProviderName.Equals(SqlClient, StringComparison.OrdinalIgnoreCase))
				return;

			// Only run migrations for SQL server (Sql ce not supported as yet)
			var settings = new ShareDeployed.DataAccess.Migrations.MigrationConfiguration();
			var migrator = new DbMigrator(settings);
			migrator.Update();
		}

		public static void DoSomeeMigrations()
		{
			var conString = ConfigurationManager.ConnectionStrings["Somee"];

			if (String.IsNullOrEmpty(conString.ProviderName) ||
				!conString.ProviderName.Equals(SqlClient, StringComparison.OrdinalIgnoreCase))
				return;

			// Only run migrations for SQL server (Sql ce not supported as yet)
			var settings = new ShareDeployed.Migrations.Configuration();
			var migrator = new DbMigrator(settings);
			migrator.Update();
		}

		private static void Sweep(Func<Repositories.IMessangerRepository> repositoryFactory, IDependencyResolver resolver)
		{
			if (_sweeping)
			{
				return;
			}

			_sweeping = true;
			MvcApplication.Logger.InfoFormat("Begin sweep process");
			try
			{
				using (Repositories.IMessangerRepository repo = repositoryFactory())
				{
					MarkInactiveUsers(repo, resolver);
					repo.CommitChanges();
				}
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error(ex);
			}
			finally
			{
				_sweeping = false;
			}
		}

		private static void MarkInactiveUsers(Repositories.IMessangerRepository repo, IDependencyResolver resolver)
		{
			var connectionManager = resolver.Resolve<IConnectionManager>();
			var hubContext = connectionManager.GetHubContext<Hubs.MessangerHub>();
			var inactiveUsers = new List<Common.Models.MessangerUser>();

			IQueryable<Common.Models.MessangerUser> users = repo.Users.Online();

			foreach (var user in users)
			{
				var status = (Common.Models.UserStatus)user.Status;
				var elapsed = DateTime.UtcNow - user.LastActivity;

				if (elapsed.TotalMinutes > 15)
				{
					user.Status = (int)Common.Models.UserStatus.Inactive;
					inactiveUsers.Add(user);
				}
			}

			if (inactiveUsers.Count > 0)
			{
				var roomGroups = from usr in inactiveUsers
								 from grp in usr.Groups
								 select new { User = usr, Group = grp } into tuple
								 group tuple by tuple.Group into g
								 select new
								 {
									 Group = g.Key,
									 Users = g.Select(t => new UserViewModel(t.User))
								 };

				var parallelOpt = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
				var result = System.Threading.Tasks.Parallel.ForEach(roomGroups, parallelOpt, roomGroup =>
				{
					if (hubContext != null)
						hubContext.Clients.Group(roomGroup.Group.Name).markInactive(roomGroup.Users).Wait();
				});

				//foreach (var roomGroup in roomGroups)
				//{
				//	hubContext.Clients.Group(roomGroup.Group.Name).markInactive(roomGroup.Users).Wait();
				//}
			}
		}

		private static void BroadCastNewMessages(Func<Repositories.IMessangerRepository> repositoryFactory, IDependencyResolver resolver)
		{
			if (_broadcasting)
			{
				return;
			}

			_broadcasting = true;
			MvcApplication.Logger.InfoFormat("Begin broadcast messages workflow");
			try
			{
				using (Repositories.IMessangerRepository repo = repositoryFactory())
				{
					BroadcastMessagesFunc(repo, resolver);
				}
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error(ex);
			}
			finally
			{
				_broadcasting = false;
			}
		}

		private static void BroadcastMessagesFunc(Repositories.IMessangerRepository repository, IDependencyResolver resolver)
		{
			var connectionManager = resolver.Resolve<IConnectionManager>();
			var hubContext = connectionManager.GetHubContext<Hubs.MessangerHub>();

			var allGrpMsgs = (from msg in repository.GetAllNewMessges().ToList()
							  from item in msg.UsersWhoRead.DefaultIfEmpty()
							  let usr = item
							  let grp = msg.Group
							  where usr == null
							  select new { UsersWereRead = usr, Msg = msg, Group = grp } into tuple
							  group tuple by tuple.Group into g
							  select new
							  {
								  Group = g.Key,
								  Messages = g.Select(m => new ViewModels.MessageViewModel(m.Msg)),
								  ExcludeUsers = g.Select(u => u.UsersWereRead)
							  }).ToList();

			if (allGrpMsgs == null)
				return;

			var parallelOpt = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
			var result = System.Threading.Tasks.Parallel.ForEach(allGrpMsgs, parallelOpt, grpMsgs =>
				{
					if (hubContext != null && grpMsgs.Group != null)
						hubContext.Clients.Group(grpMsgs.Group.Name).
						broadcastMessages(grpMsgs.Group.Name, grpMsgs.Messages).Wait();
				});

			//old school sending workflow
			//for (int i = 0; i < allGrpMsgs.Count; i++)
			//{
			//	var groupMessages = allGrpMsgs[i];

			//	if (hubContext != null && groupMessages.Group != null)
			//		hubContext.Clients.Group(groupMessages.Group.Name).
			//		broadcastMessages(groupMessages.Group.Name, groupMessages.Messages).Wait();
			//}
		}
	}
}