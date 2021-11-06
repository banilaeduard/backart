using DataAccess.Entities;
using WebApi.Models;

namespace WebApi
{
    public static class AppIdentityUserExtension
    {
        public static AppIdentityUser fromUserModel(this AppIdentityUser user, UserModel userModel)
        {
            user.Name = userModel.Name;
            user.PhoneNumber = userModel.Phone;
            user.Address = userModel.Address;

            return user;
        }

        public static AppIdentityUser From(UserModel userModel)
        {
            return new AppIdentityUser
            {
                UserName = userModel.UserName ?? userModel.Email,
                Email = userModel.Email,
            }.fromUserModel(userModel);
        }
    }
}
