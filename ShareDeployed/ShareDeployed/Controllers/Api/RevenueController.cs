using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	[Authorization.AuthTokenAthorization(RequireToken = true)]
	public class RevenueController : ApiController
	{
		private readonly DataAccess.Interfaces.IUnityOfWork _unity;
		private int _userId;

		public RevenueController(DataAccess.Interfaces.IUnityOfWork unity)
		{
			System.Diagnostics.Contracts.Contract.Requires<ArgumentNullException>(unity != null);
			_unity = unity;
		}

		[HttpGet]
		[ActionName("All")]
		public IQueryable<Revenue> GetAll()
		{
			try
			{
				InitializeUserId();

				var repo = _unity.GetRepository<Revenue>();
				return repo.Find();
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpGet]
		[ActionName("ById")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public Revenue GetById(int id)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Revenue>();

				var ent = repo.Find(x => x.Id == id && x.UserId == _userId).FirstOrDefault();
				if (ent == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

				return ent;
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpGet]
		[ActionName("ByName")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public Revenue GetByName(string name)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Revenue>();

				var ent = repo.Find(x => x.Name.Contains(name) && x.UserId == _userId).FirstOrDefault();
				if (ent == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

				return ent;
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpGet]
		[ActionName("ByRange")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public IQueryable<Revenue> GetByRange([FromUri]string from, [FromUri]string to)
		{
			try
			{
				DateTime time1;
				DateTime time2;
				if (!DateTime.TryParse(from, out time1))
					throw new HttpResponseException(HttpStatusCode.BadRequest);

				if (!DateTime.TryParse(to, out time2))
					throw new HttpResponseException(HttpStatusCode.BadRequest);

				InitializeUserId();

				time2 = time2.AddDays(1);
				var repo = _unity.GetRepository<Revenue>();
				return repo.Find(x => (x.Time >= time1.Date && x.Time <= time2) && x.UserId == _userId);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpGet]
		[ActionName("GetTotal")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public RevenueTotal GetTotalExpense([FromUri]string v1, [FromUri]string v2)
		{
			try
			{
				DateTime time1;
				DateTime time2;
				if (!DateTime.TryParse(v2, out time1))
				{
					throw new HttpResponseException(HttpStatusCode.BadRequest);
				}

				if (!DateTime.TryParse(v1, out time2))
				{
					throw new HttpResponseException(HttpStatusCode.BadRequest);
				}
				InitializeUserId();

				time2 = time2.AddDays(1);

				var repo = _unity.GetRepository<Revenue>();
				var request = repo.Find(x => (x.Time >= time1.Date && x.Time <= time2) && x.UserId == _userId)
												.Select(x => x.Amount).AsEnumerable();
				if (request.Count() == 0)
					return new RevenueTotal() { Total = 0 };

				RevenueTotal total = new RevenueTotal();
				total.Total = request.Sum();
				return total;
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		//[HttpGet]
		//[ActionName("ByRange")]
		//public IQueryable<Revenue> GetByRange([FromBody]RangeRequest request)
		//{
		//	if (!ModelState.IsValid || request == null)
		//		throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
		//	return _revenueRepository.GetAll(x => (x.Time >= request.From && x.Time <= request.To) && x.UserId == _userId);
		//}

		[HttpPost()]
		[ActionName("Complex")]
		[ShareDeployed.Extension.ExceptionHandling()]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PostComplex([FromBody]Revenue revenue)
		{
			if (revenue != null)
			{
				InitializeUserId();
				try
				{
					var repo = _unity.GetRepository<Revenue>();
					repo.Add(revenue);
					_unity.Commit();

					var submitted = repo.Find(x => x.UserId == revenue.UserId && x.Name.Equals(revenue.Name, StringComparison.OrdinalIgnoreCase)
						&& x.Amount == revenue.Amount && x.Time.Equals(revenue.Time));

					if (submitted != null)
					{
						return ResponseFromPost(revenue);
					}
					else
						return new HttpResponseMessage(HttpStatusCode.InternalServerError);
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error("Error occurred while put Revenue", ex);
					return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
				}
			}
			else
			{
				var content = Request.Content.ReadAsStringAsync().Result;
				if (!string.IsNullOrEmpty(content))
				{
				}
				return new HttpResponseMessage(HttpStatusCode.BadRequest);
			}
		}

		// Create a 201 response for a POST action.
		[NonAction]
		private HttpResponseMessage ResponseFromPost(Revenue revenue)
		{
			var resp = new HttpResponseMessage(HttpStatusCode.Created);
			resp.Content = new StringContent(revenue.UserId.ToString());
			resp.Headers.Location = new Uri(Url.Link("DefaultApi", new { action = "get", id = revenue.Id }));
			return resp;
		}

		[HttpPut]
		[ShareDeployed.Extension.ExceptionHandling()]
		[Filters.ValidateModelState()]
		public HttpResponseMessage Put(int id, Revenue revenue)
		{
			if (revenue == null)
				return new HttpResponseMessage(HttpStatusCode.BadRequest);

			InitializeUserId();
			try
			{
				var repo = _unity.GetRepository<Revenue>();
				var ent = repo.FindSingle(x => x.Id == id && x.UserId == _userId);
				if (ent != null)
				{
					repo.Update(ent);
					_unity.Commit();
					return new HttpResponseMessage(HttpStatusCode.OK);
				}
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("Error occurred while put Revenue", ex);
				return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
			}

			return new HttpResponseMessage(HttpStatusCode.Created);
		}

		[HttpDelete]
		[ShareDeployed.Extension.ExceptionHandling()]
		public HttpResponseMessage Delete(int id)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Revenue>();

				var result = repo.Find(x => x.Id == id).FirstOrDefault();
				if (result != null)
				{
					repo.Delete(result);
					_unity.Commit();
				}
				else
					return new HttpResponseMessage(HttpStatusCode.NotFound);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("Error occurred while delete revenue", ex);
				return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		[NonAction]
		private void InitializeUserId()
		{
			if (Request.Headers.GetCookie("messanger.state") != null)
			{
				var state = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(Request.Headers.GetCookie("messanger.state"));
				_userId = state.aspUserId;
			}
			else if (Request != null && Request.Headers.Contains("UserId"))
			{
				if (!Int32.TryParse(Request.Headers.GetValues("UserId").FirstOrDefault(), out _userId))
					MvcApplication.Logger.WarnFormat("Fail to parse userId string. UserId contains in header: {0} , value: {1}",
						Request.Headers.Contains("UserId"), Request.Headers.GetValues("UserId").FirstOrDefault());
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (_unity != null)
				_unity.Dispose();

			base.Dispose(disposing);
		}
	}
}