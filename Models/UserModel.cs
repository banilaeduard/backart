namespace WebApi.Models
{
    using DataAccess.Entities;

    public class UserModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string DataKey { get; set; }
        public string Password { get; set; }
        public string PrevPassword { get; set; }

        public UserModel From(AppIdentityUser model)
        {
            this.Id = model.Id;
            this.UserName = model.UserName;
            this.Email = model.Email;
            this.Name = model.Name;
            this.Phone = model.PhoneNumber;
            this.Address = model.Address;
            this.DataKey = model.DataKeyLocation.locationCode;
            return this;
        }
    }
}