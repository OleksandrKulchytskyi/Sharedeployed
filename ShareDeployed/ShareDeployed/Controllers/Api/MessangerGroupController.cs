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
	public class MessangerGroupController : ApiController
	{
		private readonly IMessangerRepository _repository;

		public MessangerGroupController(IMessangerRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			_repository = repository;
		}

		// GET api/messangergroup
		[HttpGet()]
		[ActionName("GetAll")]
		[Filters.DisableLazyloadingFilter(false, false)]
		public IEnumerable<MessangerGroup> GetAll()
		{
			var groups = _repository.Groups.AsEnumerable();
			return groups;
		}

		[HttpGet()]
		[ActionName("GetByName")]
		[Filters.DisableLazyloadingFilter(false, false)]
		public MessangerGroup GetByName(string groupName)
		{
			try
			{
				var group = _repository.GetGroupByName(groupName);
				if (group == null)
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
				return group;
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
		[ActionName("PostGroup")]
		[Filters.ValidateModelState()]
		public HttpResponseMessage PostGroup([FromBody] MessangerGroup group)
		{
			if (group != null)
			{
				try
				{
					if (_repository.GetGroupByName(group.Name) != null)
					{
						var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
						msg.ReasonPhrase = "Group with specified name has already exists";
						throw new HttpResponseException(msg);
					}

					group.InviteCode = Guid.NewGuid().ToString("d");
					_repository.Add(group);

					var repoMsg = _repository.GetGroupByName(group.Name);
					return CreateResponseMessage(repoMsg);
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

		[HttpPost]
		[ActionName("PostGroupExtended")]
		public HttpResponseMessage PostGroupExtended([FromBody] MessangerGroup group, string userIdentity)
		{
			if (group != null && ModelState.IsValid)
			{
				try
				{
					var user = _repository.GetUserByIdentity(userIdentity);
					if (user != null && !user.IsAdmin)
					{
						var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
						msg.ReasonPhrase = "User has not enough right for creation of groups.";
						throw new HttpResponseException(msg);
					}

					if (_repository.GetGroupByName(group.Name) != null)
					{
						var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
						msg.ReasonPhrase = "Group with specified name has already exists";
						throw new HttpResponseException(msg);
					}

					group.InviteCode = Guid.NewGuid().ToString("d").Replace("-", string.Empty).Substring(0, 5);
					_repository.Add(group);

					var repoMsg = _repository.GetGroupByName(group.Name);
					return CreateResponseMessage(repoMsg);
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
		private HttpResponseMessage CreateResponseMessage(MessangerGroup group)
		{
			HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.Created);
			message.Content = new StringContent(group.Key.ToString());
			message.Headers.Location = new Uri(Url.Link("DefaultApiActionParam", new { action = "get", groupName = group.Name }));

			return message;
		}

		[HttpDelete]
		[ShareDeployed.Extension.ExceptionHandling()]
		public HttpResponseMessage Delete(int id)
		{
			try
			{
				var result = _repository.Groups.FirstOrDefault(x => x.Key == id);
				if (result != null)
				{
					_repository.Remove(result);
				}
				else
					return new HttpResponseMessage(HttpStatusCode.NotFound);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("Error occurred while delete MessangerGroup", ex);
				return new HttpResponseMessage(HttpStatusCode.ExpectationFailed);
			}
			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		protected override void Dispose(bool disposing)
		{
			if (_repository != null)
				_repository.Dispose();

			base.Dispose(disposing);
		}
	}
}