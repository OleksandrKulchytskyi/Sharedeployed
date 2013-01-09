using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using Ninject;
using ShareDeployed.Authorization;
using ShareDeployed.Common.Models;
using ShareDeployed.Common.Extensions;
using ShareDeployed.Extension;
using ShareDeployed.Filters;
using ShareDeployed.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebMatrix.WebData;
using Newtonsoft.Json;

namespace ShareDeployed.Controllers
{
	[Authorize]
	[InitializeSimpleMembership]
	public class AccountController : Controller
	{
		// GET: /Account/Login
		//[HandleError(ExceptionType = typeof(System.Data.DataException), View = "Shared/Error")]
		[AllowAnonymous]
		[DropPreviousSessionFilter]
		public ActionResult Login(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}

		// POST: /Account/Login
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Login(LoginModel model, string returnUrl)
		{
			if (ModelState.IsValid &&
				WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
			{
				FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);

				if (Request.Cookies["ASP.NET_SessionId"] != null)
				{
					var cookie = Request.Cookies["ASP.NET_SessionId"];
					if (Session.SessionID != cookie.Value)
					{
					}
				}

				var authClient = WebHelper.GetClientIndetification();

				int userId = -1;
				string authToken = Authorization.AuthTokenManagerEx.Instance.Generate(authClient);
				if (Authorization.AuthTokenManagerEx.Instance[authClient] != null)
				{
					userId = WebSecurity.GetUserId(model.UserName);
					Authorization.AuthTokenManagerEx.Instance[authClient].UserId = userId;
					Authorization.AuthTokenManagerEx.Instance[authClient].UserName = model.UserName;

					var cInfo = new ClientInfo() { Id = userId, UserName = model.UserName };
					Authorization.AuthTokenManagerEx.Instance.AddClientInfo(cInfo, authToken);
				}
				InitializeSessionValiable(model);

				using (var mesRepo = Infrastructure.Bootstrapper.Kernel.Get<Repositories.IMessangerRepository>())
				{
					var mesUser = mesRepo.GetUserByIdentity(string.Format("{0}_{1}", model.UserName, userId));

					//Add data of logged user to corresponding MessangerUsers table
					if (mesUser == null)
					{
						string email = string.Empty;
						using (var aspRepo = Infrastructure.Bootstrapper.Kernel.Get<Repositories.IAspUserRepository>())
						{
							//get user email from asp db login 
							if (aspRepo != null)
								email = aspRepo.GetByName(model.UserName).Email;
						}
						mesUser = new MessangerUser()
						{
							Id = Guid.NewGuid().ToString("d"),
							Identity = string.Format("{0}_{1}", model.UserName, userId),
							IsBanned = false,
							LastActivity = DateTime.Now,
							Name = model.UserName,
							Status = (int)Common.Models.UserStatus.Active,
							Note = "created from LogOn workflow",
							Email = email,
							Hash = email.ToMD5()
						};
						mesRepo.Add(mesUser);
					}

					// save messanger state to cookies object
					if (mesUser != null && Request.Cookies.Get("messanger.state") != null)
					{
						var state = JsonConvert.SerializeObject(new
						{
							userId = mesUser.Id,
							aspUserId = userId,
							userName = model.UserName,
							hash = mesUser.Hash
						});
						var cookie = new HttpCookie("messanger.state", state);
						if (model.RememberMe)
							cookie.Expires = DateTime.Now.AddDays(30);
						else
							cookie.Expires = DateTime.Now.AddHours(1);
						HttpContext.Response.Cookies.Add(cookie);
					}
					else if (mesUser != null)
					{
						var state = JsonConvert.SerializeObject(new
						{
							userId = mesUser.Id,
							aspUserId = userId,
							userName = model.UserName,
							hash = mesUser.Hash
						});
						var cookie = new HttpCookie("messanger.state", state);
						if (model.RememberMe)
							cookie.Expires = DateTime.Now.AddDays(30);
						else
							cookie.Expires = DateTime.Now.AddHours(1);

						HttpContext.Response.Cookies.Add(cookie);
					}
				}//end of using mesRepo
				return RedirectToLocal(returnUrl);
			}
			// If we got this far, something failed, redisplay form
			ModelState.AddModelError("", "The user name or password provided is incorrect.");
			return View(model);
		}

		// POST: /Account/LogOff
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LogOff()
		{
			Authorization.AuthTokenManagerEx.Instance.RemoveToken(WebHelper.GetClientIndetification());
			if (Session != null)
			{
				string userName = string.Empty;
				int userId = -1;
				if (Session["UserName"] != null)
					userName = Session["UserName"] as string;
				if (Session["UserId"] != null)
					userId = (int)Session["UserId"];
				Authorization.AuthTokenManagerEx.Instance.RemoveClientInfo(new ClientInfo() { UserName = userName, Id = userId });
			}

			if (Request.Cookies["ASP.NET_SessionId"] != null)
				Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddYears(-30);

			if (Request.Cookies["messanger.state"] != null)
				Response.Cookies.Remove("messanger.state");

			Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", ""));
			Response.Cookies.Remove(".ASPXAUTH");
			Response.Cookies.Clear();

			WebSecurity.Logout();
			Session.Abandon();

			return RedirectToAction("Index", "Home");
		}

		// GET: /Account/Register
		[AllowAnonymous]
		public ActionResult Register()
		{
			return View();
		}

		// POST: /Account/Register
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Register(RegisterModel model)
		{
			if (ModelState.IsValid)
			{
				try
				{
					int webSecUID = -1;
					var messangerRepo = Infrastructure.Bootstrapper.Kernel.Get<Repositories.IMessangerRepository>();
					MessangerUser mesUsr = null;
					using (messangerRepo)
					{
						if (messangerRepo.GetUserByName(model.UserName) != null)
						{
							ModelState.AddModelError("UserName", "User with the same name already exist in DB");
							return View(model);
						}

						WebSecurity.CreateUserAndAccount(model.UserName, model.Password, new { Email = model.Email });
						WebSecurity.Login(model.UserName, model.Password);
						FormsAuthentication.SetAuthCookie(model.UserName, false);

						var shareContext = Infrastructure.Bootstrapper.Kernel.Get<DataAccess.ShareDeployedContext>();
						using (shareContext)
						{
							if (shareContext.User.Any(x => x.Name == model.UserName))
								throw new InvalidOperationException("User with such name is already exists in DB.");

							shareContext.User.Add(new User() { Name = model.UserName, LogonName = model.UserName });
							shareContext.SaveChanges();
						}//end using shareContext

						webSecUID = WebSecurity.CurrentUserId > 0 ? WebSecurity.CurrentUserId : WebSecurity.GetUserId(model.UserName);
						InitializeSessionValiable(model.UserName, webSecUID);
						mesUsr = new Common.Models.MessangerUser()
						{
							Name = model.UserName,
							Status = (int)UserStatus.Active,
							Email = model.Email,
							Hash = model.Email.ToMD5(),
							Identity = string.Format("{0}_{1}", model.UserName, webSecUID),
							Id = Guid.NewGuid().ToString("d"),
							LastActivity = DateTime.UtcNow
						};

						messangerRepo.Add(mesUsr);
						messangerRepo.CommitChanges();

						string ip = Extension.WebHelper.GetIpAddress();
						string clientName;
						if (!Extension.WebHelper.DetermineCompName(ip, out clientName))
							clientName = Extension.WebHelper.DetermineNameFromHeaders();
						string authToken = Authorization.AuthTokenManagerEx.Instance.Generate(ip, clientName);

						AuthTokenManagerEx.Instance.AddClientInfo(new ClientInfo() { Id = webSecUID, UserName = model.UserName },
																authToken);
					}//end using messangerRepo

					var state = JsonConvert.SerializeObject(new
					{
						userId = mesUsr.Id,
						aspUserId = webSecUID,
						userName = model.UserName,
						hash = mesUsr.Hash
					});

					var cookie = new HttpCookie("messanger.state", state);
					cookie.Expires = DateTime.Now.AddHours(1);
					HttpContext.Response.Cookies.Add(cookie);

					return RedirectToAction("Index", "Home");
				}
				catch (MembershipCreateUserException e)
				{
					ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
					MvcApplication.Logger.Error("Error occurred while register user", e);
				}
			}
			// If we got this far, something failed, redisplay form
			return View(model);
		}

		[HttpGet()]
		[AllowAnonymous]
		public JsonResult CheckNameAvaliability(string username)
		{
			bool result = false;
			var cont = Infrastructure.Bootstrapper.Kernel.Get<Models.UsersContext>();
			using (cont)
			{
				result = cont.UserProfiles.Any(x => x.UserName == username);
			}
			return Json(!result, JsonRequestBehavior.AllowGet);
		}

		[HttpGet()]
		[AllowAnonymous]
		public JsonResult CheckEmailAvaliability(string email)
		{
			bool result = false;
			var cont = Infrastructure.Bootstrapper.Kernel.Get<Models.UsersContext>();
			using (cont)
			{
				result = cont.UserProfiles.Any(x => x.Email == email);
			}
			return Json(!result, JsonRequestBehavior.AllowGet);
		}

		// POST: /Account/Disassociate
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Disassociate(string provider, string providerUserId)
		{
			string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
			ManageMessageId? message = null;

			// Only disassociate the account if the currently logged in user is the owner
			if (ownerAccount == User.Identity.Name)
			{
				// Use a transaction to prevent the user from deleting their last login credential
				using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
				{
					bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
					if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
					{
						OAuthWebSecurity.DeleteAccount(provider, providerUserId);
						scope.Complete();
						message = ManageMessageId.RemoveLoginSuccess;
					}
				}
			}

			return RedirectToAction("Manage", new { Message = message });
		}

		// GET: /Account/Manage
		public ActionResult Manage(ManageMessageId? message)
		{
			ViewBag.StatusMessage =
				message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
				: message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
				: message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
				: string.Empty;
			ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			ViewBag.ReturnUrl = Url.Action("Manage");
			return View();
		}

		// POST: /Account/Manage
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Manage(LocalPasswordModel model)
		{
			bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			ViewBag.HasLocalPassword = hasLocalAccount;
			ViewBag.ReturnUrl = Url.Action("Manage");
			if (hasLocalAccount)
			{
				if (ModelState.IsValid)
				{
					// ChangePassword will throw an exception rather than return false in certain failure scenarios.
					bool changePasswordSucceeded;
					try
					{
						changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
					}
					catch (Exception)
					{
						changePasswordSucceeded = false;
					}

					if (changePasswordSucceeded)
						return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
					else
						ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
				}
			}
			else
			{
				// User does not have a local password so remove any validation errors caused by a missing OldPassword field
				ModelState state = ModelState["OldPassword"];
				if (state != null)
					state.Errors.Clear();

				if (ModelState.IsValid)
				{
					try
					{
						WebSecurity.CreateAccount(User.Identity.Name, model.NewPassword);
						return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
					}
					catch (Exception e)
					{
						ModelState.AddModelError("", e);
					}
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		// POST: /Account/ExternalLogin
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLogin(string provider, string returnUrl)
		{
			return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
		}

		// GET: /Account/ExternalLoginCallback
		[AllowAnonymous]
		public ActionResult ExternalLoginCallback(string returnUrl)
		{
			AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
			if (!result.IsSuccessful)
				return RedirectToAction("ExternalLoginFailure");

			if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
				return RedirectToLocal(returnUrl);

			if (User.Identity.IsAuthenticated)
			{
				var authClient = WebHelper.GetClientIndetification();
				AuthTokenManagerEx.Instance.Generate(authClient);
				if (AuthTokenManagerEx.Instance[authClient] != null && WebSecurity.CurrentUserId > 0)
					AuthTokenManagerEx.Instance[authClient].UserId = WebSecurity.CurrentUserId;

				InitializeSessionValiable(WebSecurity.CurrentUserName);

				// If the current user is logged in add the new account
				OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
				return RedirectToLocal(returnUrl);
			}
			else
			{
				// User is new, ask for their desired membership name
				string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
				ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
				ViewBag.ReturnUrl = returnUrl;
				return View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
			}
		}

		// POST: /Account/ExternalLoginConfirmation
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
		{
			string provider = null;
			string providerUserId = null;

			if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
			{
				return RedirectToAction("Manage");
			}

			if (ModelState.IsValid)
			{
				// Insert a new user into the database
				using (UsersContext db = new UsersContext())
				{
					UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName == model.UserName);
					// Check if user already exists
					if (user == null)
					{
						// Insert name into the profile table
						db.UserProfiles.Add(new UserProfile { UserName = model.UserName, Email = "" });
						db.SaveChanges();

						OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
						OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

						return RedirectToLocal(returnUrl);
					}
					else
						ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
				}
			}

			ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
			ViewBag.ReturnUrl = returnUrl;
			return View(model);
		}

		// GET: /Account/ExternalLoginFailure
		[AllowAnonymous]
		public ActionResult ExternalLoginFailure()
		{
			return View();
		}

		[AllowAnonymous]
		[ChildActionOnly]
		public ActionResult ExternalLoginsList(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;
			return PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
		}

		[ChildActionOnly]
		public ActionResult RemoveExternalLogins()
		{
			ICollection<OAuthAccount> accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name);
			List<ExternalLogin> externalLogins = new List<ExternalLogin>();
			foreach (OAuthAccount account in accounts)
			{
				AuthenticationClientData clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

				externalLogins.Add(new ExternalLogin
				{
					Provider = account.Provider,
					ProviderDisplayName = clientData.DisplayName,
					ProviderUserId = account.ProviderUserId,
				});
			}

			ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			return PartialView("_RemoveExternalLoginsPartial", externalLogins);
		}

		#region Helpers
		private ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			else
			{
				return RedirectToAction("Index", "Home");
			}
		}

		public enum ManageMessageId
		{
			ChangePasswordSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
		}

		internal class ExternalLoginResult : ActionResult
		{
			public ExternalLoginResult(string provider, string returnUrl)
			{
				Provider = provider;
				ReturnUrl = returnUrl;
			}

			public string Provider { get; private set; }
			public string ReturnUrl { get; private set; }

			public override void ExecuteResult(ControllerContext context)
			{
				OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
			}
		}

		private static string ErrorCodeToString(MembershipCreateStatus createStatus)
		{
			// See http://go.microsoft.com/fwlink/?LinkID=177550 for a full list of status codes.
			switch (createStatus)
			{
				case MembershipCreateStatus.DuplicateUserName:
					return "User name already exists. Please enter a different user name.";

				case MembershipCreateStatus.DuplicateEmail:
					return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

				case MembershipCreateStatus.InvalidPassword:
					return "The password provided is invalid. Please enter a valid password value.";

				case MembershipCreateStatus.InvalidEmail:
					return "The e-mail address provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidAnswer:
					return "The password retrieval answer provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidQuestion:
					return "The password retrieval question provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidUserName:
					return "The user name provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.ProviderError:
					return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				case MembershipCreateStatus.UserRejected:
					return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				default:
					return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
			}
		}
		#endregion

		#region Session initializer methods
		[NonAction]
		private void InitializeSessionValiable(LoginModel model)
		{
			InitializeSessionValiable(model.UserName);
		}

		[NonAction]
		private void InitializeSessionValiable(string username, int usrId = -1)
		{
			if (HttpContext != null && HttpContext.Session != null)
			{
				if (HttpContext.Session["UserName"] == null)
					HttpContext.Session["UserName"] = username;

				int userId = usrId == -1 ? WebSecurity.GetUserId(username) : usrId;
				HttpContext.Session["UserId"] = userId;
				HttpContext.Session["UserIdentit"] = string.Format("{0}_{1}", username, userId);
			}
		}
		#endregion
	}
}
