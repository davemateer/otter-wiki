//-----------------------------------------------------------------------
// <copyright file="Sluggifier.cs" company="Dave Mateer">
// The MIT License (MIT)
//
// Copyright (c) 2014 Dave Mateer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Otter
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

            return slug.ToString().ToLowerInvariant().Trim('-');
        }
    }
}