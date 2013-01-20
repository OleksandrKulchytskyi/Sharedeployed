using ShareDeployed.Common.Models;
using ShareDeployed.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	public class ApplicationController : ApiController
	{
		private readonly IMessangerRepository _repository;

		public ApplicationController(IMessangerRepository repo)
		{
			_repository = repo;
		}

		[HttpGet]
		[Filters.DisableLazyloadingFilter(false, false)]
		public IEnumerable<MessangerApplication> GetAll()
		{
			var applications = _repository.Application.AsEnumerable();
			return applications;
		}

		[HttpGet]
		[ActionName("GetById")]
		[Filters.DisableLazyloadingFilter(false, false)]
		public MessangerApplication GetById(string appId)
		{
			try
			{
				MessangerApplication app = null;
				app = _repository.Application.FirstOrDefault(x => x.AppId.Equals(appId, StringComparison.OrdinalIgnoreCase));
				if (app == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

				return app;
			}
			catch (Exception ex)
			{
				if (!(ex is HttpResponseException))
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));

				MvcApplication.Logger.Error(ex);
				throw;
			}
		}

		[HttpGet]
		[ActionName("GetResponses")]
		[Filters.DisableLazyloadingFilter()]
		public IEnumerable<Message> GetResponses(string appId, int onlyNew)
		{
			try
			{
				var app = _repository.Application.FirstOrDefault(x => x.AppId.Equals(appId, StringComparison.OrdinalIgnoreCase));
				if (app == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

				return (from item in _repository.GetDbSet<Message>()
						where item.AppKey == app.Key && item.Response != null
						&& !item.Response.IsSent
						orderby item.When
						select item).AsEnumerable();
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error(ex);
				if (!(ex is HttpResponseException))
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
				throw;
			}
		}

		[HttpPost]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PostApplication([FromBody] MessangerApplication app)
		{
			if (app != null)
			{
				try
				{
					_repository.Add(app);

					var repoMsg = _repository.GetApplicationByAppId(app.AppId);
					return CreateResponseMessage(repoMsg);
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error(ex);
					if (!(ex is HttpResponseException))
						throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
					throw;
				}
			}
			else
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
		}

		[HttpPut]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PutApplication([FromBody] MessangerApplication app)
		{
			if (app != null)
			{
				try
				{
					_repository.Update(app);
					return new HttpResponseMessage(HttpStatusCode.OK);
				}
				catch (Exception ex)
				{
					if (!(ex is HttpResponseException))
						throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
					MvcApplication.Logger.Error(ex);
					throw;
				}
			}
			else
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
		}

		[NonAction]
		private HttpResponseMessage CreateResponseMessage(Common.Models.MessangerApplication app)
		{
			HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.Created);
			message.Content = new StringContent(app.Key.ToString());
			message.Headers.Location = new Uri(Url.Link("DefaultApiActionParam", new { action = "get", id = app.AppId }));

			return message;
		}

		protected override void Dispose(bool disposing)
		{
			if (_repository != null)
				_repository.Dispose();

			base.Dispose(disposing);
		}
	}
}