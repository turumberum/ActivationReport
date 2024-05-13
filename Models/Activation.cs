using System.Net;

namespace ActivationReport.Models
{
    public class Activation
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public DateTime ActDate { get; set; }
        public string CardNumber { get; set; }
        public string DateRequest { get; set; }
        public string DateAnswer { get; set; }
        public string CompanyName { get; set; }
        public string OGRN { get; set; }
        public string INN { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string? Address { get; set; }
        public string VehicleBrand { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleYear { get; set; }
        public string VehicleColor { get; set; }
        public string VRN { get; set; }
        public string VIN { get; set; }
        public string? VehiclePassport { get; set; }
        public string CryptoBlock { get; set; }
        public string TachoNumber { get; set; }

        public override string ToString()
        {
            return (
                this.CardNumber + ";" + this.DateRequest + ";" + this.DateAnswer + ";" + this.CompanyName + ";" +
                this.OGRN + ";" + this.INN + ";" + this.Region + ";" + this.City + ";" + this.Address + ";" + this.VehicleBrand + ";" +
                this.VehicleModel + ";" + this.VehicleYear + ";" + this.VehicleColor + ";" + this.VRN + ";" + this.VIN + ";" +
                this.VehiclePassport + ";" + this.CryptoBlock + ";" + this.TachoNumber
                );
        }
    }    
}
