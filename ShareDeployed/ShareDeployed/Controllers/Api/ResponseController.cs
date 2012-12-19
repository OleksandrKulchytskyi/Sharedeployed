using ShareDeployed.Common.Models;
using ShareDeployed.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Entity;

namespace ShareDeployed.Controllers.Api
{
	public class ResponseController : ApiController
	{
		readonly IMessangerRepository _repository;

		public ResponseController(IMessangerRepository rep)
		{
			_repository = rep;
		}

		[ActionName("GetAll")]
		[Filters.DisableLazyloadingFilter(false,false)]
		public IEnumerable<MessageResponse> GetAll()
		{
			return _repository.Response.AsEnumerable();
		}

		[ActionName("GetResponseMessage")]
		[Filters.DisableLazyloadingFilter(false,false)]
		public Message GetResponseMessage(int respKey)
		{
			try
			{
				var message = (from item in _repository.Message.Include(x => x.Response)
							   where item.ResponseKey == respKey
							   select item).FirstOrDefault();
				return message;
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("GetResponseMessage", ex);
				throw new HttpResponseException(HttpStatusCode.InternalServerError);
			}
		}

		[HttpPost]
		public HttpResponseMessage MarkAsSent(int key)
		{
			try
			{
				var response = _repository.Response.FirstOrDefault(x => x.Key == key);
				if (response == null)
					return new HttpResponseMessage(HttpStatusCode.NotFound);

				response.IsSent = true;

				_repository.Update(response);
				return new HttpResponseMessage(HttpStatusCode.OK);
			}
			catch (Exception ex)
			{
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					ReasonPhrase
						= ex.Message
				});
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
