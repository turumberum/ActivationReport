using System.ComponentModel.DataAnnotations;

namespace ActivationReport.Models
{
    public class Card
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [Required(ErrorMessage = "Необходимо указать номер карты")]
        public string CardNumber { get; set; }
        public Company? Company { get; set; }
    }
}