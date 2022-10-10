using StackExchange.Redis;

namespace EventFlow.Redis;

public record PrefixedKey
{
    public PrefixedKey(string prefix, string key)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix cant be empty");
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cant be empty");

        Prefix = prefix;
        Key = key;
    }

    public string Prefix { get; private set; }
    public string Key { get; private set; }

    public override string ToString() => $"{Prefix}:{Key}";

    public static implicit operator RedisKey(PrefixedKey prefixedKey) => new RedisKey(prefixedKey.ToString());
    public static implicit operator string(PrefixedKey prefixedKey) => prefixedKey.ToString();
}