using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Кодирование строкового ключа лока в bigint для PostgreSQL advisory lock.
/// Advisory lock принимает только <c>bigint</c>, поэтому строка хешируется стабильным
/// SHA-256 (первые 8 байт, big-endian): <c>string.GetHashCode()</c> нельзя — он рандомизирован
/// per-process и дал бы разный ключ на разных репликах. Коллизии 64-битного хеша для
/// десятков сервисов практически невозможны; учтите только, что в <c>pg_locks</c> ключ
/// виден как число, а не как исходная строка.
/// </summary>
internal static class LockKeyEncoding
{
    public static long FromString(string lockKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(lockKey));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
