using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using ShareDeployed.Repositories;
using ShareDeployed.RoutingHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	public class MessangerController : ApiController
	{
		private readonly IMessangerRepository _repository;

		public MessangerController(IMessangerRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			_repository = repository;
		}

		[HttpGet()]
		[ActionName("GetNew")]
		[Filters.DisableLazyloadingFilter(false,false)]
		public IEnumerable<Common.Models.Message> GetNew()
		{
			try
			{
				return _repository.GetAllNewMessges().AsEnumerable();
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error(ex);
				if (!(ex is HttpResponseException))
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
				throw;
			}
		}

		[HttpGet()]
		[ActionName("Get")]
		[Filters.DisableLazyloadingFilter(false,false)]
		public Common.Models.Message Get(string id)
		{
			try
			{
				var repoMsg = _repository.GetMessagesById(id);
				if (repoMsg == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
				return repoMsg;
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error(ex);
				if (!(ex is HttpResponseException))
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
				throw;
			}
		}

		[HttpGet]
		[ActionName("Get")]
		public HttpResponseMessage MarkAsRead(string msgId, string usrId)
		{
			try
			{
				_repository.MarkMessageAsRead(usrId, msgId);
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			}
			catch (Exception ex)
			{
				if (ex is HttpResponseException)
					throw;

				MvcApplication.Logger.Error("MarkAsRead", ex);
				return new HttpResponseMessage(HttpStatusCode.InternalServerError);
			}
		}

		[HttpPost]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PostMessage([FromBody] Common.Models.Message msg)
		{
			if (msg != null)
			{
				try
				{
					string guid = Guid.NewGuid().ToString();
					msg.Id = guid;
					_repository.Add(msg);

					var repoMsg = _repository.GetMessagesById(guid);
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

		[NonAction]
		private HttpResponseMessage CreateResponseMessage(Common.Models.Message msg)
		{
			HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.Created);
			message.Content = new StringContent(msg.Key.ToString());
			message.Headers.Location = new Uri(Url.Link("DefaultApiActionParam", new { action = "get", id = msg.Id }));

			return message;
		}

		[HttpPost]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PostMessageResponse(string msgId, [FromBody] MessageResponse response)
		{

			try
			{
				var repoMsg = _repository.GetMessagesById(msgId);
				if (repoMsg == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

				repoMsg.Response = response;

				_repository.Update(repoMsg);
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Created));
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error(ex);
				if (!(ex is HttpResponseException))
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
				throw;
			}

		}

		protected override void Dispose(bool disposing)
		{
			if (_repository != null)
				_repository.Dispose();

			base.Dispose(disposing);
		}
	}
}