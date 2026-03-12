namespace DataMasking.Models
{
    public class Score
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public long SubjectId { get; set; }
        public string ScoreText { get; set; }
        public float ScoreFirst { get; set; }
        public float ScoreSecond { get; set; }
        public float ScoreFinal { get; set; }
        public float ScoreOverall { get; set; }
        public string Semester { get; set; }
        
        // Navigation properties
        public Student Student { get; set; }
        public Subject Subject { get; set; }
    }
}
