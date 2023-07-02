namespace Practica.Data.Entities
{
    public class Post
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public User User { get; set; }
        
        public ICollection<Reaction> Reactions { get; set; }

        public ICollection<Comment> Comments { get; set; }

        public Post()
        {
            Reactions = new List<Reaction>();
            Comments = new List<Comment>();
        }
    }
}
