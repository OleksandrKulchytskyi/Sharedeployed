using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using ShareDeployed.Repositories;
using ShareDeployed.Common.Extensions;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShareDeployed.Services
{
	public interface IMembershipService
	{
		MessangerUser AuthenticateUser(string userName, string password);
		MessangerUser AddUser(string userName, string email, string password);
	}

	public sealed class DefaultMembershipService : IMembershipService
	{
		private readonly IMessangerRepository _repository;
		private readonly ICryptoService _crypto;

		public DefaultMembershipService(IMessangerRepository repository, ICryptoService cryptoServ)
		{
			System.Diagnostics.Contracts.Contract.Requires<ArgumentNullException>(repository != null);
			System.Diagnostics.Contracts.Contract.Requires<ArgumentNullException>(cryptoServ != null);

			_repository = repository;
			_crypto = cryptoServ;
		}

		public MessangerUser AuthenticateUser(string userName, string password)
		{
			MessangerUser user = _repository.VerifyUser(userName);

			if (user.HashedPassword != password.ToSha256(user.Salt))
			{
				throw new InvalidOperationException();
			}

			EnsureSaltedPassword(user, password);

			return user;
		}

		private void EnsureSaltedPassword(MessangerUser user, string password)
		{
			if (string.IsNullOrEmpty(user.Salt))
			{
				user.Salt = _crypto.CreateSalt();
			}
			user.HashedPassword = password.ToSha256(user.Salt);
		}


		public MessangerUser AddUser(string userName, string email, string password)
		{
			if (!IsValidUserName(userName))
				throw new InvalidOperationException(string.Format("'{0}' is not a valid user name.", userName));

			if (string.IsNullOrEmpty(password))
				ThrowPasswordIsRequired();

			EnsureUserNameIsAvailable(userName);

			var user = new MessangerUser
			{
				Name = userName,
				Email = email,
				Status = (int)UserStatus.Active,
				Id = Guid.NewGuid().ToString("d"),
				Salt = _crypto.CreateSalt(),
				LastActivity = DateTime.UtcNow,
			};

			ValidatePassword(password);
			user.HashedPassword = password.ToSha256(user.Salt);

			_repository.Add(user);

			return user;
		}

		private static bool IsValidUserName(string name)
		{
			return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[\\w-_.]{1,30}$");
		}

		internal static void ThrowPasswordIsRequired()
		{
			throw new InvalidOperationException("A password is required.");
		}

		private void EnsureUserNameIsAvailable(string userName)
		{
			if (UserExists(userName))
				ThrowUserExists(userName);
		}

		internal static void ThrowUserExists(string userName)
		{
			throw new InvalidOperationException(string.Format("Username {0} already taken.", userName));
		}

		private bool UserExists(string userName)
		{
			return _repository.Users.Any(u => u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
		}

		private static void ValidatePassword(string password)
		{
			if (string.IsNullOrEmpty(password) || password.Length < 6)
				throw new InvalidOperationException("Pasword validation is failed. Your password must be at least 6 characters.");
		}
	}
}