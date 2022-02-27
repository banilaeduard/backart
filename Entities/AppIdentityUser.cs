namespace WebApi.Entities
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;

    using WebApi.Models;
    public class AppIdentityUser : IdentityUser
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public string DataKey { get; set; }

        public AppIdentityUser fromUserModel(UserModel userModel)
        {
            this.Name = userModel.Name;
            this.PhoneNumber = userModel.Phone;
            this.Address = userModel.Address;
            this.DataKey = userModel.DataKey;
            return this;
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