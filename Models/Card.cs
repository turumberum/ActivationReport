using System.ComponentModel.DataAnnotations;

namespace ActivationReport.Models
{
    public class Card
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Необходимо указать номер карты")]
        [StringLength(16, MinimumLength = 13, ErrorMessage = "Номер карты должен быть {2} или {1} символов")]
        public string CardNumber { get; set; }
        public Company? Company { get; set; }
    }
}
