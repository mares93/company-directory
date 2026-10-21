using CompanyApi.Infrastructure.Security;

namespace CompanyApi.Tests;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_accepts_right_password_and_rejects_wrong_one()
    {
        var hash = _hasher.Hash("correct horse");
        Assert.True(_hasher.Verify("correct horse", hash));
        Assert.False(_hasher.Verify("wrong", hash));
        Assert.NotEqual(hash, _hasher.Hash("correct horse")); // random salt
    }

    [Fact]
    public void Verify_without_a_stored_hash_is_false_and_does_not_throw() =>
        Assert.False(_hasher.Verify("anything", null));
}
