using MongoDB.Bson;

namespace MyFirstTest
{
    public class PostModel
    {
        public ObjectId Id { get; set; } //создаем поле Id с типом ObjectId для монги
        public int IdUser { get; set; } 
        public string Text { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime EditDate { get; set; }
    }
}
