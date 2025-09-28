using Domain.Interfaces.Validations;

namespace UserProfilesDomain;

public static class Validations
{
    public static readonly Validation DisplayName = CommonValidations.DescriptiveName();
    public static readonly Validation FirstName = CommonValidations.DescriptiveName();
    public static readonly Validation LastName = CommonValidations.DescriptiveName();
    public static readonly Validation PhoneNumber = CommonValidations.PhoneNumber;
    public static readonly Validation Timezone = CommonValidations.Timezone;
    public static readonly Validation Locale = CommonValidations.Locale;

    public static class Address
    {
        public static readonly Validation City = CommonValidations.DescriptiveName();
        public static readonly Validation CountryCode = CommonValidations.CountryCode;
        public static readonly Validation Line = CommonValidations.DescriptiveName();
        public static readonly Validation State = CommonValidations.DescriptiveName();
        public static readonly Validation Zip = CommonValidations.DescriptiveName();
    }

    public static class Avatar
    {
        public const long MaxSizeInBytes = 134_217_728; //approx 100MB
        public static readonly IReadOnlyList<string> AllowableContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/gif"
        };
    }
}