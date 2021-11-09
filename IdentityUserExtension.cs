namespace WebApi
{
    using WebApi.Entities;
    static class IdentityUserExtension
    {
        public static AppIdentityUser mapFromUser(this AppIdentityUser _user, User model)
        {
            _user.PhoneNumber = model.Phone;
            return _user;
        }
    }
}