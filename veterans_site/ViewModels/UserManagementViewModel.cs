namespace veterans_site.ViewModels
{
    public class UserManagementViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; }
        public int ConsultationsCount { get; set; }
        public int EventsCount { get; set; }
    }
}
