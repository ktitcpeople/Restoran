namespace RestaurantApi.Request
{
    public class AuthRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public int Role { get; set; } // Admin, Waiter, Cook, Courier
        public DateTime? BirthDate { get; set; }
        public int BranchID { get; set; } = 1;
    }
}
