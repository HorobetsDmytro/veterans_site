namespace veterans_site.ViewModels
{
    public class UserIndexViewModel
    {
        public IEnumerable<UserManagementViewModel> Users { get; set; }
        public string SearchString { get; set; }
        public string CurrentSort { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string CurrentFilter { get; set; }
    }
}
