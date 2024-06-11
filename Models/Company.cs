using System.Collections;
using System.ComponentModel.DataAnnotations;

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

        [Required(ErrorMessage = "Необходимо добавить хотя бы одну карту")]
        [MinCollectionCount(1, ErrorMessage = "Необходимо добавить хотя бы одну карту")]
        public required List<Card> Cards { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            int requiredLength = Staff ? 16 : 13;
            foreach (var card in Cards)
            {
                if (card.CardNumber == null || card.CardNumber.Length != requiredLength)
                {
                    string errorMessage = Staff
                        ? "Номер карты должен быть 16 символов"
                        : "Номер карты должен быть 13 символов";
                    results.Add(new ValidationResult(errorMessage, new[] { nameof(Cards) }));
                }
            }

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