using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ActivationReport.Models
{
    public class Company : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Необходимо указать название компании")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Название компании должно быть менее 2 символов")]
        public string? Name { get; set; }
        public bool Staff { get; set; }
        public bool MonthlyReport { get; set; }
        public string? Email { get; set; }

        [Required(ErrorMessage = "Необходимо добавить хотя бы одну карту")]
        [MinCollectionCount(1, ErrorMessage = "Необходимо добавить хотя бы одну карту")]
        public required List<Card> Cards { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var db = new AppDBContext();
            int requiredLength = Staff ? 16 : 13;
            string mailPattern = "[.\\-_a-z0-9]+@([a-z0-9][\\-a-z0-9]+\\.)+[a-z]{2,6}";
            string cardPattern = @"^(RUM|rum)"; //@"^(RUM|rum)\d{10}(\d{3})?&"

            if (!string.IsNullOrEmpty(Email))
            {                
                Match isMatch = Regex.Match(Email, mailPattern, RegexOptions.IgnoreCase);
                if (!isMatch.Success)
                {
                    results.Add(new ValidationResult("Некорректный адрес электронной почты", new[] { nameof(Email) }));
                }
            }

            foreach (var card in Cards)
            {
                if (string.IsNullOrEmpty(card.CardNumber))
                {
                    results.Add(new ValidationResult("Укажите номер карты мастерской", new[] { nameof(Cards) }));
                    continue;
                }

                if (card.CardNumber.Length != requiredLength)
                {
                    string errorMessage = $"Номер карты должен быть {requiredLength} символов, сейчас: {card.CardNumber.Length} символов";
                    results.Add(new ValidationResult(errorMessage, new[] { nameof(Cards) }));
                    continue;
                }

                if (!Regex.IsMatch(card.CardNumber, cardPattern, RegexOptions.IgnoreCase))
                {
                    results.Add(new ValidationResult("Номер карты должен начинаться с RUM", new[] { nameof(Cards) }));
                    continue;
                }               
                
                bool isDuplicate = db.Cards.Any(c => c.CardNumber == card.CardNumber && c.Id != card.Id);
                if (isDuplicate)
                {
                    results.Add(new ValidationResult("Номер карты уже существует в базе данных", new[] { nameof(Cards) }));
                    continue;
                }

                if (Staff)
                {
                    bool isReissue = db.Cards.Any(c => c.CardNumber.Substring(0, 15) == card.CardNumber.Substring(0, 15) && c.Id != card.Id);
                    if (isReissue)
                    {
                        results.Add(new ValidationResult("Номер карты уже существует, не требуется указывать перевыпуск карты", new[] { nameof(Cards) }));
                    }
                }
            }

            //foreach (var card in Cards)
            //{
            //    if (!string.IsNullOrEmpty(card.CardNumber))
            //    {

            //        Match isMatch = Regex.Match(card.CardNumber, cardPattern, RegexOptions.IgnoreCase);
            //        if (!isMatch.Success)
            //        {
            //            string errorMessage = "Номер карты должен начинаться с RUM";
            //            results.Add(new ValidationResult(errorMessage, new[] { nameof(Cards) }));
            //        }
            //    }

            //    if (string.IsNullOrEmpty(card.CardNumber))
            //    {
            //        string errorMessage = "Укажите номер карты мастерской";
            //        results.Add(new ValidationResult(errorMessage, new[] { nameof(Cards) }));
            //    } 
            //    else if (card.CardNumber.Length != requiredLength) 
            //    { 
            //        string errorMessage = Staff
            //            ? "Номер карты должен быть 16 символов, сейчас: " + card.CardNumber?.Length
            //            : "Номер карты должен быть 13 символов, сейчас: " + card.CardNumber?.Length;
            //        results.Add(new ValidationResult(errorMessage, new[] { nameof(Cards) }));
            //    }

            //    bool isDuplicate = db.Cards.Any(c => c.CardNumber == card.CardNumber && c.Id != card.Id);
            //    if (isDuplicate)
            //    {
            //        string errorMessage = "Номер карты уже существует в базе данных";
            //        results.Add(new ValidationResult(errorMessage, new[] { nameof(Cards) }));
            //    }
            //    else if (!string.IsNullOrEmpty(card.CardNumber) && Staff)
            //    {
            //        bool isReissue = db.Cards.Any(c => c.CardNumber.Substring(0, 15) == card.CardNumber.Substring(0, 15) && c.Id != card.Id);
            //        if (isReissue)
            //        {
            //            string errorMessage = "Номер карты уже существует, не требуется указывать перевыпуск карты";
            //            results.Add(new ValidationResult(errorMessage, new[] { nameof(Cards) }));
            //        }                    
            //    }

            //}
            return results;
        }
    }
};

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]

public class MinCollectionCountAttribute : ValidationAttribute
{
    private readonly int _minCount;

    public MinCollectionCountAttribute(int minCount)
    {
        _minCount = minCount;
        ErrorMessage = $"The collection must have at least {minCount} elements.";
    }

    public override bool IsValid(object value)
    {
        if (value is ICollection collection)
        {
            return collection.Count >= _minCount;
        }

        return false;
    }
}

public class ValidateComplexTypeAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var results = new List<ValidationResult>();
        var context = new ValidationContext(value, validationContext, validationContext.Items);
        bool isValid = Validator.TryValidateObject(value, context, results, true);

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                context = new ValidationContext(item, validationContext, validationContext.Items);
                isValid &= Validator.TryValidateObject(item, context, results, true);
            }
        }

        if (!isValid)
        {
            var compositeResults = new CompositeValidationResult($"Validation for {validationContext.DisplayName} failed!");
            results.ForEach(compositeResults.AddResult);
            return compositeResults;
        }

        return ValidationResult.Success;
    }
}

public class CompositeValidationResult : ValidationResult
{
    private readonly List<ValidationResult> _results = new List<ValidationResult>();

    public IEnumerable<ValidationResult> Results => _results;

    public CompositeValidationResult(string errorMessage) : base(errorMessage)
    {
    }

    public CompositeValidationResult(string errorMessage, IEnumerable<string> memberNames) : base(errorMessage, memberNames)
    {
    }

    protected CompositeValidationResult(ValidationResult validationResult) : base(validationResult)
    {
    }

    public void AddResult(ValidationResult validationResult)
    {
        _results.Add(validationResult);
    }
}