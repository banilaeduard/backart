using DataAccess.Entities;
using WebApi.Models;

namespace BackArt
{
    public static class AppIdentityUserExtension
    {
        public static AppIdentityUser fromUserModel(this AppIdentityUser user, UserModel userModel)
        {
            user.Name = userModel.Name;
            user.PhoneNumber = userModel.Phone;
            user.Address = userModel.Address;
            user.DataKey = userModel.DataKey;
            return user;
        }

        public static AppIdentityUser From(UserModel userModel)
        {
            return new AppIdentityUser
            {
                UserName = userModel.UserName ?? userModel.Email,
                Email = userModel.Email,
                DataKey = userModel.DataKey,
            }.fromUserModel(userModel);
        }
    }
}
