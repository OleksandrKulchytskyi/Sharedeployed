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
	public class MessangerUserController : ApiController
	{
		private readonly IMessangerRepository _repository;

		public MessangerUserController(IMessangerRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			_repository = repository;
		}

		[HttpGet()]
		[ActionName("GetAll")]
		public IQueryable<MessangerUser> GetAll()
		{
			try
			{
				return _repository.Users;
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
		[ActionName("GetById")]
		public MessangerUser GetById(string userId)
		{
			try
			{
				var user = _repository.GetUserById(userId);
				if (user == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
				return user;
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
		[ActionName("GetByIdentity")]
		public MessangerUser GetByIdentity(string userIdentity)
		{
			try
			{
				var user = _repository.GetUserByIdentity(userIdentity);
				if (user == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
				return user;
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
		[ActionName("GetByIdentity")]
		public List<MessangerGroup> GetUserGoups(string userIdentity, int allGroups)
		{
			try
			{
				var user = _repository.GetUserByIdentity(userIdentity);
				if (user == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = "User was not found" });

				return _repository.Groups.Where(x => x.CreatorKey == user.Key).ToList();
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
		public HttpResponseMessage PostUser([FromBody] MessangerUser user)
		{
			if (user != null)
			{
				try
				{
					if (_repository.GetUserByName(user.Name) != null)
					{
						var msg = new HttpResponseMessage(HttpStatusCode.NotAcceptable);
						msg.ReasonPhrase = "User with specified name has already registered";
						throw new HttpResponseException(msg);
					}

					_repository.Add(user);

					var repoUser = _repository.GetUserByName(user.Name);
					return CreateResponseMessage(repoUser);
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error(ex);
					if (!(ex is HttpResponseException))
						throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
					throw;
				};
			}
			else
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
		}

		[NonAction]
		private HttpResponseMessage CreateResponseMessage(MessangerUser user)
		{
			HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.Created);
			message.Content = new StringContent(user.Key.ToString());
			message.Headers.Location = new Uri(Url.Link("DefaultApiActionParam", new { action = "get", userId = user.Id }));

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
