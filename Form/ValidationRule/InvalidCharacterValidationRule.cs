namespace CreatePipe.Form.ValidationRule
{
    using System.Globalization;
    using System.Linq;
    using System.Windows.Controls;

    public class InvalidCharacterValidationRule : ValidationRule
    {
        private char[] invalidFileChars = { '\\', '/', ':', '{', '}', '[', ']', ';', '<', '>', '?', '\'', '~' };
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
    public class GuidanceContentValidationRule : ValidationRule
    {
        public int MaxSemicolonPerBarSection { get; set; } = 2;
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var text = (value ?? string.Empty).ToString();
            // 1) 空字符串
            if (string.IsNullOrWhiteSpace(text))
                return new ValidationResult(false, "牌面内容不能为空");
            // 2) 长度
            if (text.Length > 127)
                return new ValidationResult(false, "牌面长度不能超过 127");
            // 3) 非法字符：半角逗号 或 | 出现>1次
            if (text.Contains(',') || text.Count(c => c == '|') > 1)
                return new ValidationResult(false, "牌面不能包含半角逗号，且竖线|最多出现一次");
            int semicolonCount = text.Count(c => c == '；' || c == ';');
            if (semicolonCount > 4)
                return new ValidationResult(false, "牌面数超限，请复核分号分隔符数量");
            // 检查每个竖线部分的分号数量
            if (text.Contains('|'))
            {
                // 按竖线分割字符串
                var sections = text.Split('|');
                foreach (var section in sections)
                {
                    int sectionSemicolonCount = section.Count(c => c == '；' || c == ';');
                    if (sectionSemicolonCount > MaxSemicolonPerBarSection)
                    {
                        return new ValidationResult(false, $"每个部分的分号不能超过{MaxSemicolonPerBarSection}个");
                    }
                }
            }
            else
            {
                if (semicolonCount > 2)
                {
                    return new ValidationResult(false, $"内容分号不能超过{MaxSemicolonPerBarSection}个");
                }
            }
            // 全部通过
            return ValidationResult.ValidResult;
        }
    }
}
