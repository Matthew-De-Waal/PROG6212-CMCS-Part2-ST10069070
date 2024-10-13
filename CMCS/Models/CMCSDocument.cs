namespace CMCS.Models
{
    public class CMCSDocument
    {
        public int DocumentID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public string Section { get; set; }
        public string Content { get; set; }
    }
}
