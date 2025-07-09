namespace esAPI.Models.Enums
{
    public static class Order
    {
        public enum Status
        {
            Pending = 1,
            Accepted = 2,
            Rejected = 3,
            InProgress = 4,
            Completed = 5,
            InTransit = 6,
            Disaster = 7,
            Expired = 8
        }
    }
}