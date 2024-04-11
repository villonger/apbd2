using LegacyApp;

namespace LegacyAppTests;

public class UserServiceTest
{
    private readonly UserService _userService = new();

    [Test]
    [TestCase("John", "", "mail@gmail.com")]
    [TestCase("", "Wick", "mail@gmail.com")]
    [TestCase("John", "Wick", "mail")]
    [Parallelizable(ParallelScope.All)]
    public void does_not_add_user_when_invalid_params(string firstName, string lastName, string email)
    {
        // when
        var result = _userService.AddUser(
            firstName, lastName, email, DateTime.Parse("1982-03-21"), 1);

        // then
        Assert.IsFalse(result);
    }

    [Test]
    [TestCase(-21, 0, 1)]
    [TestCase(-21, 1, 0)]
    [TestCase(-21, 1, 1)]
    [TestCase(-20, 0, 0)]
    [Parallelizable(ParallelScope.All)]
    public void does_not_add_user_when_not_old_enough(int yOffset, int mOffset, int dOffset)
    {
        // given
        var date = DateTime.Now.AddYears(yOffset).AddMonths(mOffset).AddDays(dOffset);

        // when
        var result = _userService.AddUser("John", "Wick", "john.wick@gmail.com", date, 1);

        // then
        Assert.IsFalse(result);
    }

    [Test]
    [TestCase(1, "Kowalski")]
    [Parallelizable(ParallelScope.All)]
    public void does_not_add_user_when_low_credit_limit(int clientId, string lastName)
    {
        // when
        var result = _userService.AddUser(
            "John", lastName, "john.wick@gmail.com", DateTime.Now.AddYears(-22), clientId);

        // then
        Assert.IsFalse(result);
    }

    [Test]
    public void throws_when_client_not_exists()
    {
        // given
        var clientId = 123;

        // then
        var thrown = Assert.Throws<ArgumentException>(() => _userService.AddUser(
            "John", "Wick", "john.wick@gmail.com", DateTime.Now.AddYears(-23), clientId));

        Assert.AreEqual($"User with id {clientId} does not exist in database", thrown.Message);
    }

    [Test]
    public void throws_when_client_does_not_have_credit()
    {
        // given
        var lastName = "Wick";

        // then
        var thrown = Assert.Throws<ArgumentException>(() => _userService.AddUser(
            "John", lastName, "john.wick@gmail.com", DateTime.Now.AddYears(-23), 1));

        Assert.AreEqual($"Client {lastName} does not exist", thrown.Message);
    }

    [Test]
    [TestCase(1, "Doe")]
    [TestCase(2, "Kowalski")]
    [TestCase(3, "Kwiatkowski")]
    [TestCase(3, "Kowalski")]
    [Parallelizable(ParallelScope.All)]
    public void adds_user(int clientId, string lastName)
    {
        // when
        var result = _userService.AddUser(
            "John", lastName, "john.wick@gmail.com", DateTime.Now.AddYears(-22), clientId);

        // then
        Assert.IsTrue(result);
    }
}