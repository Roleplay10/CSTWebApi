namespace Practica.Data.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public string content { get; set; }
        public int UserId { get; set; }
        public int PostId { get; set; }
        public Post Post { get; set; }
    }
}
