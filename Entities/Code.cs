namespace WebApi.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    public class Code
    {
        [Key]
        public int Id { get; set; }
        public string Display { get; set; }
        public string Value { get; set; }
        public Code Parent { get; set; }
        public List<Code> Children { get; set; }
        public bool HasChildren { get; set; }
    }
}