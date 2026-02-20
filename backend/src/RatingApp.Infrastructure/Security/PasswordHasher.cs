using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using RatingApp.Domain.Interfaces;
using System.Security.Cryptography;

namespace RatingApp.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: KeySize);

        // Format: [4-byte iteration count][16-byte salt][32-byte hash]
        var blob = new byte[4 + SaltSize + KeySize];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), Iterations);
        salt.CopyTo(blob, 4);
        hash.CopyTo(blob, 4 + SaltSize);

        return Convert.ToBase64String(blob);
    }

    public bool Verify(string password, string storedHash)
    {
        byte[] stored = Convert.FromBase64String(storedHash);
        int iterations = BitConverter.ToInt32(stored, 0);
        byte[] salt = stored[4..(4 + SaltSize)];
        byte[] expectedHash = stored[(4 + SaltSize)..];

        byte[] actualHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: KeySize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
