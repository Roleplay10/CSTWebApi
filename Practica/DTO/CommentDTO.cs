namespace Practica.DTO
{
    public class CommentDTO
    {
        public int Id { get; set; }
        public string content { get; set; }
        public int UserId { get; set; }
        public int PostId { get; set; }
    }
}
