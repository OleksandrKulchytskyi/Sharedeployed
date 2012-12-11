using ShareDeployed.Common;
using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	public class UserController : ApiController
	{
		readonly DataAccess.Interfaces.IUnityOfWork _unity;

		public UserController(DataAccess.Interfaces.IUnityOfWork unity)
		{
			_unity = unity;
		}

		[Extension.ExceptionHandling]
		public User Get(int id)
		{
			var repo = _unity.GetRepository<User>();
			return repo.FindSingle(x => x.Id == id);
		}

		[Extension.ExceptionHandling]
		[ActionName("GetId")]
		public User GetId(string name)
		{
			var repo = _unity.GetRepository<User>();
			var user = repo.FindSingle(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (user != null)
				return user;
			else
				throw new HttpResponseException(HttpStatusCode.NotFound);
		}

		// POST api/<controller>
		[HttpPost]
		[Extension.ExceptionHandling]
		[Filters.ValidateModelState()]
		public HttpResponseMessage Post([FromBody]User user)
		{
			if (user != null)
			{
				var repo = _unity.GetRepository<User>();
				repo.Add(user);
				_unity.Commit();

				var submitted = repo.FindSingle(x => x.Id == user.Id && x.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase));

				if (submitted != null)
				{
					return ResponseFromPost(user);
				}
				else
					return new HttpResponseMessage(HttpStatusCode.InternalServerError);
			}
			else
				return new HttpResponseMessage(HttpStatusCode.BadRequest);
		}

		private HttpResponseMessage ResponseFromPost(User user)
		{
			var resp = new HttpResponseMessage(HttpStatusCode.Created);
			resp.Content = new StringContent(user.Id.ToString());
			resp.Headers.Location = new Uri(Url.Link("DefaultApi", new { action = "get", id = user.Id }));
			return resp;
		}

		// PUT api/<controller>/5
		[HttpPut]
		[Extension.ExceptionHandling]
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE api/<controller>/5
		[HttpDelete]
		[Extension.ExceptionHandling]
		public void Delete(int id)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (_unity != null)
				_unity.Dispose();

			base.Dispose(disposing);
		}
	}
}