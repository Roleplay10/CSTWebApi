namespace Practica.Data.Entities
{
    public class Post
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string ImgUrl { get; set; }

        public User User { get; set; }
        
        public Post()
        {

        }
    }
}
