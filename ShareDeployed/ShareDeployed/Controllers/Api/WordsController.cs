using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	[Authorization.AuthTokenAthorization(RequireToken = true, RequireTimestamp = false)]
	public class WordsController : ApiController
	{
		private readonly DataAccess.Interfaces.IUnityOfWork _unity;
		private int _userId = -1;

		public WordsController(DataAccess.Interfaces.IUnityOfWork unity)
		{
			_unity = unity;
		}

		[HttpGet]
		[ActionName("GetAll")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public IQueryable<Word> GetAll()
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Word>();
				return repo.Find(x => x.UserId == _userId);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpGet]
		[ActionName("GetByComplexity")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public IQueryable<Word> GetByComplexity(int complex)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Word>();

				if (complex == 0)
					return repo.Find(x => x.UserId == _userId && !x.Complicated);

				return repo.Find(x => x.UserId == _userId && x.Complicated);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpPost]
		[ActionName("GetById")]
		[ShareDeployed.Extension.ExceptionHandling()]
		public Word GetById([FromUri]int id)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Word>();

				Word found = repo.FindSingle(x => x.UserId == _userId && x.Id == id);
				if (found == null)
					throw new HttpResponseException(HttpStatusCode.NotFound);

				return found;
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpPost]
		[ShareDeployed.Extension.ExceptionHandling()]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PostComlex(Word newWord)
		{
			if (newWord == null)
				throw new HttpResponseException(HttpStatusCode.BadRequest);

			try
			{
				InitializeUserId();

				if (newWord.UserId <= 0 && _userId > 0)
					newWord.UserId = _userId;

				var repo = _unity.GetRepository<Word>();
				repo.Add(newWord);
				_unity.Commit();

				var found = repo.FindSingle(x => x.UserId == newWord.UserId && x.ForeignWord.Equals(newWord.ForeignWord, StringComparison.OrdinalIgnoreCase)
								&& x.Translation.Equals(newWord.Translation, StringComparison.OrdinalIgnoreCase)
								&& x.Complicated == newWord.Complicated);
				if (found != null)
					return GenerateMessage(found);

				return new HttpResponseMessage(HttpStatusCode.InternalServerError);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetAll", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpPut]
		[ShareDeployed.Extension.ExceptionHandling()]
		[Filters.ValidateModelState()]
		public HttpResponseMessage Put(int id, Word updated)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Word>();
				repo.Update(updated);
				_unity.Commit();
				return GenerateMessage(updated);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("Error occurred while delete revenue", ex);
				return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
			}
		}

		[HttpPut]
		[ShareDeployed.Extension.ExceptionHandling()]
		public HttpResponseMessage Put(int id, int comp)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Word>();
				Word found = repo.FindSingle(x => x.Id == id);
				if (found != null &&
					found.Complicated != (comp == 1 ? true : false))
				{
					found.Complicated = comp == 1 ? true : false;
					repo.Update(found);
					_unity.Commit();

					return GenerateMessage(found);
				}
				throw new HttpResponseException(HttpStatusCode.NotFound);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("Error occurred while delete revenue", ex);
				return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
			}
		}

		[HttpDelete]
		[ShareDeployed.Extension.ExceptionHandling()]
		public HttpResponseMessage Delete(int id)
		{
			try
			{
				InitializeUserId();
				var repo = _unity.GetRepository<Word>();
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
				MvcApplication.Logger.Error("Error occurred while delete revenue", ex);
				return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
			}

			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		[NonAction]
		private HttpResponseMessage GenerateMessage(Word word)
		{
			var resp = new HttpResponseMessage(HttpStatusCode.Created);
			resp.Content = new StringContent(word.UserId.ToString());
			resp.Headers.Location = new Uri(Url.Link("DefaultApi", new { action = "get", id = word.Id }));
			return resp;
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