namespace WebApi
{
    using Microsoft.AspNetCore.Identity;
    using WebApi.Entities;
    static class IdentityUserExtension
    {
        public static IdentityUser mapFromUser(this IdentityUser _user, User model)
        {
            _user.PhoneNumber = model.Phone;
            return _user;
        }
    }
}