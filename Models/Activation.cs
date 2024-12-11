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

        public string ToStringMax()
        {
            return (
                this.CardNumber.Substring(1, 16) + ";" + 
                this.DateRequest.Substring(1, (this.DateRequest.Length - 2)) + ";" + 
                this.DateAnswer.Substring(1, (this.DateAnswer.Length - 2)) + ";" + 
                this.CompanyName.Substring(1, (this.CompanyName.Length - 2)) + ";" +
                this.OGRN.Substring(2, (this.OGRN.Length - 3)) + ";" + 
                this.INN.Substring(2, (this.INN.Length - 3)) + ";" + 
                this.Region.Substring(2, (this.Region.Length - 3)) + ";" + 
                this.City.Substring(1, (this.City.Length - 2)) + ";" + 
                this.Address.Substring(1, (this.Address.Length - 2)) + ";" + 
                this.VehicleBrand.Substring(2, (this.VehicleBrand.Length - 3)) + ";" +
                this.VehicleModel.Substring(2, (this.VehicleModel.Length - 3)) + ";" + 
                this.VehicleYear.Substring(2, (this.VehicleYear.Length - 3)) + ";" + 
                this.VehicleColor.Substring(1, (this.VehicleColor.Length - 2)) + ";" + 
                this.VRN.Substring(1, (this.VRN.Length - 2)) + ";" + 
                this.VIN.Substring(2, 17) + ";" +
                this.VehiclePassport?.Substring(2, (this.VehiclePassport.Length - 3)) + ";" + 
                this.CryptoBlock.Substring(2, 16) + ";" +
                this.TachoNumber.Substring(2, 16)
                );
        }

        public string ToStringMin()
        {
            //Номер СКЗИ	Гос. номер автомобиля	VIN	ПТС	Дата активации
            return (                
                    this.CryptoBlock.Substring(2, 16) + ";" + 
                    this.VRN.Substring(1, (this.VRN.Length - 2)) + ";" + 
                    this.VIN.Substring(2, 17) + ";" + 
                    this.VehiclePassport?.Substring(2, (this.VehiclePassport.Length - 3)) + ";" + 
                    this.DateAnswer.Substring(1, 10)
                    );
        }
    }    
}