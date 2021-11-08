using WebApi.Entities;
using System.Runtime.Serialization;
namespace WebApi.Models
{
    [DataContract]
    public class UserModel : User
    {
        public string Password { get; set; }
        public string PrevPassword { get; set; }
    }
}