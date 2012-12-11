using ShareDeployed.Common.Models;
using ShareDeployed.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ShareDeployed.Controllers
{
	[Authorize]
	public class RevenueMobileController : Controller
	{
		Common.IRepository<User> _userRepository = null;
		public RevenueMobileController()
		{
			_userRepository = App_Start.NinjectWebCommon.GetInstanceOf<Common.IRepository<User>>();
		}

		public ActionResult Index()
		{
			return View();
		}

		[Authorize]
		public ActionResult CreateRevenue()
		{
			ViewBag.Title = "Create new revenue";
			Revenue revenue = new Revenue();
			string name = System.Web.HttpContext.Current.User.Identity.Name;
			if (!string.IsNullOrEmpty(name))
			{
				var task = Task.Factory.StartNew<User>(() =>
				{
					Common.Models.User usr = _userRepository.Find(x => string.Compare(x.Name, name) == 0);
					return usr;
				});
				task.Wait();
				Session["UserIdValue"] = task.Result.Id;
			}
			return View(revenue);
		}

		[HttpPost]
		[Authorize]
		public ActionResult CreateRevenue(Revenue model)
		{
			if (ModelState.IsValid && model != null)
			{
				using (HttpClient client = new HttpClient())
				{
					Uri uri = HttpContext.Request.Url;
					string host = uri.Scheme + Uri.SchemeDelimiter + uri.Host + ":" + uri.Port + "/";

					MvcApplication.Logger.InfoFormat("CreateRevenue host address is: {0}", host);

					if (!string.IsNullOrEmpty(host))
					{
						if (!host.EndsWith("/"))
							host = string.Format("{0}/", host);
						client.BaseAddress = new Uri(host);
					}

					client.DefaultRequestHeaders.Add("UserId", string.Format("{0}", Session["UserIdValue"]));
					client.DefaultRequestHeaders.Add("authenticationToken", "1111");

					var formatter = new System.Net.Http.Formatting.JsonMediaTypeFormatter();

					using (HttpResponseMessage response = client.PostAsync<Revenue>("api/Revenue/Complex/", model, formatter).Result)
					{
						if (response.IsSuccessStatusCode)
						{
							ViewBag.Message = "Data has been successfully submitted";

							var resp1 = response.Content.ReadAsStringAsync().Result;
							if (!string.IsNullOrEmpty(resp1))
								ViewBag.Response = resp1;
						}
					}
				}

				return RedirectToAction("CreateRevenue");
			}

			ViewBag.Message = "Fail to submit data";
			return RedirectToAction("CreateRevenue");
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_userRepository != null)
			{
				_userRepository.DisposeExt();
			}
		}

	}
}
