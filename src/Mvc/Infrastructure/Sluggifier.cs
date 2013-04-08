namespace Otter.Mvc.Infrastructure
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class Sluggifier
    {
        public static string GenerateSlug(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var slug = new StringBuilder(value.Length);
            bool previousDash = false;

            var validCharsRegex = new Regex("[a-zA-Z0-9]");

            var enumerator = StringInfo.GetTextElementEnumerator(value);
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                Debug.WriteLine(enumerator.GetTextElement());
                string element = enumerator.GetTextElement();
                string normalized = element.Normalize(NormalizationForm.FormKD);
                if (validCharsRegex.IsMatch(normalized))
                {
                    foreach (char c in normalized)
                    {
                        if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z'))
                        {
                            slug.Append(c);
                            previousDash = false;
                        }
                    }
                }
                else
                {
                    if (!previousDash && slug.Length > 0)
                    {
                        slug.Append("-");
                        previousDash = true;
                    }
                }
            }

            return slug.ToString().ToLowerInvariant();
        }
    }
}