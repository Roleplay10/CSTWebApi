namespace Practica.Data.Entities
{
    public class Reaction
    {
        public int Id { get; set; }
        public string ReactionType { get; set; }

        public int UserId { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }
    }
}
