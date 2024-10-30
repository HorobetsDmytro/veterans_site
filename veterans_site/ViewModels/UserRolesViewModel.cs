namespace veterans_site.ViewModels
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string SelectedRole { get; set; }  // Змінено для radiobutton
        public List<string> AvailableRoles { get; set; } // Список доступних ролей
    }
}
