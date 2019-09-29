using Microsoft.Azure.Cosmos.Table;

namespace TableExample.Entities
{
    public class User : TableEntity
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        public User(string name, string email, string role)
        {
            Name = name;
            Email = email;
            Role = role;
            PartitionKey = role;
            RowKey = email;
        }

        public User()
        {
        }
    }
}
