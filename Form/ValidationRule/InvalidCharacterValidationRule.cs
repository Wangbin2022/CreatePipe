namespace CreatePipe.Form.ValidationRule
{
    using System.Globalization;
    using System.Linq;
    using System.Windows.Controls;

    public class InvalidCharacterValidationRule : ValidationRule
    {
        private char[] invalidFileChars = { '\\', '/', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '\'', '~' };
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = (string)value;
            if (input == null)
            {
                return new ValidationResult(false, "输入不能为空");
            }
            foreach (char c in invalidFileChars)
            {
                if (input.Contains(c))
                {
                    return new ValidationResult(false, $"输入包含非法字符: {c}");
                }
            }
            return ValidationResult.ValidResult;
        }
    }
}
