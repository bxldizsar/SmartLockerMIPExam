namespace SmartLocker.Models
{
    // the numbers give the real order: small < medium < large
    // so we never compare sizes as strings (alphabetical would be Large < Medium < Small)
    public enum ParcelSize
    {
        Small = 1,
        Medium = 2,
        Large = 3
    }
}
