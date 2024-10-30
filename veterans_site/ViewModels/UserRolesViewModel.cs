namespace veterans_site.ViewModels
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string SelectedRole { get; set; }
        public List<string> AvailableRoles { get; set; }
    }
}
