using ShareDeployed.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShareDeployed.Controllers
{
	public class HomeController : Controller
	{
		//public HomeController(IUnityOfWork unity)
		//{
		//}

		[Authorize]
		public ActionResult Index()
		{
			ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";
			//ViewBag.Title = "<title>@ViewBag.Title - ShareDeployed MVC Application"; ;
			return View();
		}

		[Authorize]
		public ActionResult Expenses()
		{
			ViewBag.Message = "Your expenses page.";
			return View(new ShareDeployed.Common.RequetResponse.RangeRequest());
		}

		[Authorize]
		public ActionResult Revenues()
		{
			ViewBag.Message = "Your revenues page.";
			return View(new ShareDeployed.Common.RequetResponse.RangeRequest());
		}

		[Authorize]
		public ActionResult AddRevenue()
		{
			ViewBag.Message = "Add revenue page";
			return View(new ShareDeployed.Common.Models.Revenue());
		}

		[Authorize]
		public ActionResult AddExpense()
		{
			ViewBag.Message = "Add expense page";
			return View(new ShareDeployed.Common.Models.Expense());
		}

		[AllowAnonymous()]
		public ActionResult TimeoutRedirect()
		{
			return View();
		}

		protected override void OnException(ExceptionContext filterContext)
		{
			base.OnException(filterContext);

			if (filterContext.Exception != null)
				MvcApplication.Logger.Error("OnException", filterContext.Exception);
		}
	}
}
