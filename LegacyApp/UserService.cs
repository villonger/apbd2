using System;

namespace LegacyApp
{
    public record AddUserParams(string FirstName, string LastName, string Email, DateTime DateOfBirth, int ClientId)
    {
        public bool IsInvalid()
        {
            return string.IsNullOrEmpty(FirstName)
                   || string.IsNullOrEmpty(LastName)
                   || (!Email.Contains("@") && !Email.Contains("."));
        }

        public int Age(DateTime now)
        {
            var age = now.Year - DateOfBirth.Year;
            if (now.Month < DateOfBirth.Month || (now.Month == DateOfBirth.Month && now.Day < DateOfBirth.Day))
            {
                age--;
            }

            return age;
        }
    }

    public interface ICreditLimitStrategy
    {
        bool HasCreditLimit();

        int CalculateCreditLimit(User user);
    }

    public class CreditLimitStrategyFactory
    {
        private readonly UserCreditService _userCreditService;

        public CreditLimitStrategyFactory(UserCreditService userCreditService)
        {
            _userCreditService = userCreditService;
        }

        public ICreditLimitStrategy CreditLimitStrategyFor(Client client) => client.Type switch
        {
            "VeryImportantClient" => new VeryImportantClientCreditLimitStrategy(),
            "ImportantClient" => new ImportantClientCreditLimitStrategy(_userCreditService),
            "NormalClient" => new NormalClientCreditLimitStrategy(_userCreditService),
            _ => throw new ArgumentException("Unexpected client type")
        };
    }

    public class VeryImportantClientCreditLimitStrategy : ICreditLimitStrategy
    {
        public bool HasCreditLimit()
        {
            return false;
        }

        public int CalculateCreditLimit(User user)
        {
            return 0;
        }
    }

    public class ImportantClientCreditLimitStrategy : ICreditLimitStrategy
    {
        private readonly UserCreditService _userCreditService;

        public ImportantClientCreditLimitStrategy(UserCreditService userCreditService)
        {
            _userCreditService = userCreditService;
        }

        public bool HasCreditLimit()
        {
            return false;
        }

        public int CalculateCreditLimit(User user)
        {
            var creditLimit = _userCreditService.GetCreditLimit(user.LastName, user.DateOfBirth);
            creditLimit *= 2;
            return creditLimit;
        }
    }

    public class NormalClientCreditLimitStrategy : ICreditLimitStrategy
    {
        private readonly UserCreditService _userCreditService;

        public NormalClientCreditLimitStrategy(UserCreditService userCreditService)
        {
            _userCreditService = userCreditService;
        }

        public bool HasCreditLimit()
        {
            return true;
        }

        public int CalculateCreditLimit(User user)
        {
            return _userCreditService.GetCreditLimit(user.LastName, user.DateOfBirth);
        }
    }

    public class UserService
    {
        private const int MinimumAge = 21;
        private const int MinimumCreditLimit = 500;

        private readonly CreditLimitStrategyFactory _creditLimitStrategyFactory = new(new UserCreditService());

        public bool AddUser(string firstName, string lastName, string email, DateTime dateOfBirth, int clientId)
        {
            return AddUser(new AddUserParams(firstName, lastName, email, dateOfBirth, clientId));
        }

        private bool AddUser(AddUserParams addUserParams)
        {
            if (addUserParams.IsInvalid() || IsTooYoung(addUserParams))
            {
                return false;
            }

            var clientRepository = new ClientRepository();
            var client = clientRepository.GetById(addUserParams.ClientId);

            var user = MakeUser(addUserParams, client);

            return TryAddUserWithCreditLimit(user);
        }

        private bool TryAddUserWithCreditLimit(User user)
        {
            if (DoesNotHaveEnoughCreditLimit(user))
            {
                return false;
            }

            UserDataAccess.AddUser(user);
            return true;
        }

        private bool IsTooYoung(AddUserParams addUserParams)
        {
            return addUserParams.Age(DateTime.Now) < MinimumAge;
        }

        private bool DoesNotHaveEnoughCreditLimit(User user)
        {
            return user.HasCreditLimit && user.CreditLimit < MinimumCreditLimit;
        }

        private User MakeUser(AddUserParams addUserParams, Client client)
        {
            var user = new User
            {
                Client = client,
                DateOfBirth = addUserParams.DateOfBirth,
                EmailAddress = addUserParams.Email,
                FirstName = addUserParams.FirstName,
                LastName = addUserParams.LastName
            };
            var creditLimitStrategy = _creditLimitStrategyFactory.CreditLimitStrategyFor(client);

            user.HasCreditLimit = creditLimitStrategy.HasCreditLimit();
            user.CreditLimit = creditLimitStrategy.CalculateCreditLimit(user);
            return user;
        }
    }
}