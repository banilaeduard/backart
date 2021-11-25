namespace WebApi.Models
{
    using System;
    using WebApi.Entities;

    public class UserModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime Birth { get; set; }
        public string Password { get; set; }
        public string PrevPassword { get; set; }

        public UserModel From(AppIdentityUser model)
        {
            this.Id = model.Id;
            this.UserName = model.UserName;
            this.Name = model.Name;
            this.Phone = model.PhoneNumber;
            this.Address = model.Address;
            this.Birth = model.Birth;
            return this;
        }
    }
}