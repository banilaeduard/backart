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
            if (user.DataKeyLocation != null)
            {
                user.DataKeyLocation.locationCode = userModel.DataKey;
            }
            else
            {
                user.DataKeyLocation = new DataKeyLocation()
                {
                    name = userModel.UserName,
                    locationCode = userModel.DataKey
                };
            }
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
