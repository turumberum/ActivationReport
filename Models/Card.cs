namespace ActivationReport.Models
{
    public class Card
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CardNumber { get; set; }
        public Company Company { get; set; }
    }
}
