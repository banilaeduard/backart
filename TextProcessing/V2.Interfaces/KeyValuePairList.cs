namespace V2.Interfaces
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class KeyValuePairList
    {
        [DataMember]
        public List<KeyValuePair<string, string>> Items { get; set; } = new List<KeyValuePair<string, string>>();
    }
}
