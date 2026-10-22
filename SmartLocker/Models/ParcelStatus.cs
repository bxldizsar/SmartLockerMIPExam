namespace SmartLocker.Models
{
    // waiting -> stored (in a box) -> collected (picked up)
    public enum ParcelStatus
    {
        Waiting,
        Stored,
        Collected
    }
}
