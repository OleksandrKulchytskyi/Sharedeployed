using ShareDeployed.Common;
using ShareDeployed.Common.Extensions;
using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	[ShareDeployed.Authorization.AuthTokenAthorization(RequireToken = true)]
	public class ExpenseController : ApiController
	{
		readonly DataAccess.Interfaces.IUnityOfWork _unity;
		int _userId;

		public ExpenseController(DataAccess.Interfaces.IUnityOfWork unity)
		{
			_unity = unity;
		}

		[HttpGet]
		[ActionName("All")]
		public IQueryable<Expense> GetAll()
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Expense>();
				return repo.Find(x => x.UserId == _userId);
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
		public Expense GetById(int id)
		{
			try
			{
				InitializeUserId();

				var repo = _unity.GetRepository<Expense>();
				var ent = repo.FindSingle(x => x.Id == id && x.UserId == _userId);
				if (ent == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

				return ent;
			}
			catch (Exception ex)
			{
				if (ex is HttpResponseException)
					throw;
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpGet]
		[ActionName("ByName")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public Expense GetByName(string name)
		{
			try
			{
				InitializeUserId();
				var rep = _unity.GetRepository<Expense>();
				var ent = rep.Find(x => x.Name.Contains(name) && x.UserId == _userId).FirstOrDefault();
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
		public IQueryable<Expense> GetByRange([FromUri]string from, [FromUri]string to)
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

				var rep = _unity.GetRepository<Expense>();

				return rep.Find(x => (x.Time >= time1.Date && x.Time <= time2) && x.UserId == _userId);
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
		public ExpenseTotal GetTotalExpense([FromUri]string v1, [FromUri]string v2)
		{
			try
			{
				DateTime time1;
				DateTime time2;
				if (!DateTime.TryParse(v2, out time1))
					throw new HttpResponseException(HttpStatusCode.BadRequest);

				if (!DateTime.TryParse(v1, out time2))
					throw new HttpResponseException(HttpStatusCode.BadRequest);

				InitializeUserId();

				time2 = time2.AddDays(1);

				var rep = _unity.GetRepository<Expense>();

				var request = rep.Find(x => (x.Time >= time1.Date && x.Time <= time2) && x.UserId == _userId)
											.Select(x => x.Amount).AsEnumerable();

				//var request = _expenseRepository.GetAll(x => (x.Time >= time1.Date && x.Time <= time2) && x.UserId == _userId)
				//							.Select(x => x.Amount).AsEnumerable();
				if (request.Count() == 0)
					return new ExpenseTotal() { Total = 0 };

				ExpenseTotal total = new ExpenseTotal();
				total.Total = request.Sum();
				return total;
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		//[HttpPost()]
		//[ActionName("Complex")]
		//[ShareDeployed.Extension.ExceptionHandling()]
		//public HttpResponseMessage PostComplex([FromBody]Expense expense)
		//{
		//	if (ModelState.IsValid && expense != null)
		//	{
		//		if (expense.UserId == 0)
		//			expense.UserId = _userId;
		//		if (expense.UserId == 0 && Request.Headers.Contains("UserId"))
		//		{
		//			expense.UserId = Int32.Parse(Request.Headers.GetValues("UserId").First());
		//		}

		//		try
		//		{
		//			_expenseRepository.InsertOrUpdate(expense);

		//			var submitted = _expenseRepository.Find(x => x.UserId == expense.UserId && x.Name.Equals(expense.Name, StringComparison.OrdinalIgnoreCase)
		//				&& x.Amount == expense.Amount);

		//			if (submitted != null)
		//			{
		//				return ResponseFromPost(expense);
		//			}
		//			else
		//				return new HttpResponseMessage(HttpStatusCode.InternalServerError);
		//		}
		//		catch (Exception ex)
		//		{
		//			MvcApplication.Logger.Error("Error occurred while put Expense", ex);
		//			return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
		//		}
		//	}
		//	else
		//	{
		//		var content = Request.Content.ReadAsStringAsync().Result;
		//		if (!string.IsNullOrEmpty(content))
		//		{
		//		}
		//		return new HttpResponseMessage(HttpStatusCode.BadRequest);
		//	}
		//}

		[HttpPost()]
		[ActionName("Complex")]
		[ShareDeployed.Extension.ExceptionHandling()]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PostComplex([FromBody]Expense expense)
		{
			if (expense != null)
			{
				InitializeUserId();

				try
				{
					var repo = _unity.GetRepository<Expense>();
					if (repo != null)
					{
						repo.Add(expense);
						_unity.Commit();
					}

					var submitted = repo.FindSingle(x => x.UserId == expense.UserId && x.Name.Equals(expense.Name, StringComparison.OrdinalIgnoreCase)
						&& x.Amount == expense.Amount && x.Time.Equals(expense.Time));

					if (submitted != null)
					{
						return ResponseFromPost(expense);
					}
					else
						return new HttpResponseMessage(HttpStatusCode.InternalServerError);
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error("Error occurred while put Expense", ex);
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
		private HttpResponseMessage ResponseFromPost(Expense expense)
		{
			var resp = new HttpResponseMessage(HttpStatusCode.Created);
			resp.Content = new StringContent(expense.UserId.ToString());
			resp.Headers.Location = new Uri(Url.Link("DefaultApi", new { action = "get", id = expense.Id }));
			return resp;
		}

		[HttpPut]
		[ShareDeployed.Extension.ExceptionHandling()]
		[Filters.ValidateModelState()]
		public HttpResponseMessage Put(int id, Expense expense)
		{
			if (expense == null)
				return new HttpResponseMessage(HttpStatusCode.BadRequest);

			InitializeUserId();
			try
			{
				var repo = _unity.GetRepository<Expense>();
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
				MvcApplication.Logger.Error("Error occurred while put Expense", ex);
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
				var repo = _unity.GetRepository<Expense>();
				var result = repo.FindSingle(x => x.Id == id);
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
				MvcApplication.Logger.Error("Error occurred while delete Expense", ex);
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
