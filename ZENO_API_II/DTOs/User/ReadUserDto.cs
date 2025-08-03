namespace ZENO_API_II.DTOs.User
{
    public class ReadUserDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Language { get; set; }

        public int Credits { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
