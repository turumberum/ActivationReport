namespace ActivationReport.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Staff { get; set; }
        public bool MonthlyReport {  get; set; } 
        public List<Card> Cards { get; set; }
    }
}
