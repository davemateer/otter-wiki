//-----------------------------------------------------------------------
// <copyright file="SecurityEntity.cs" company="Dave Mateer">
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
namespace Otter.Domain
{
    using System;
    using System.DirectoryServices;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    public sealed class SecurityEntity
    {
        public string EntityId { get; set; }

        public SecurityEntityTypes EntityType { get; set; }

        public string Name { get; set; }

        public static SecurityEntity FromSearchResult(SearchResult result, SecurityEntityTypes type)
        {
            var entity = new SecurityEntity();
            entity.EntityId = result.Properties["sAMAccountName"][0].ToString();
            entity.EntityType = type;
            entity.Name = result.Properties["displayName"].Count > 0 ? result.Properties["displayName"][0].ToString() : entity.EntityId;
            return entity;
        }

        public static bool TryParse(string s, out SecurityEntity result)
        {
            if (string.IsNullOrEmpty(s))
            {
                result = null;
                return false;
            }

            result = new SecurityEntity();

            if (s.EndsWith("(Group)", StringComparison.OrdinalIgnoreCase))
            {
                result.EntityType = SecurityEntityTypes.Group;
                s = s.Substring(0, s.Length - "(Group)".Length).TrimEnd(null);
            }
            else
            {
                result.EntityType = SecurityEntityTypes.User;
            }

            Regex idExpression = new Regex(@"\[(?<id>[^\]]+)\]$", RegexOptions.ExplicitCapture);
            var match = idExpression.Match(s);
            if (match.Success)
            {
                result.EntityId = match.Groups["id"].Value;
                result.Name = s.Substring(0, match.Index).TrimEnd(null);
            }
            else
            {
                result.EntityId = s.TrimEnd(null);
                result.Name = result.EntityId;
            }

            return true;
        }

        public override string ToString()
        {
            var text = new StringBuilder(this.Name);

            if (!StringComparer.OrdinalIgnoreCase.Equals(this.EntityId, this.Name))
            {
                text.AppendFormat(CultureInfo.InvariantCulture, " [{0}]", this.EntityId);
            }

            if (this.EntityType == SecurityEntityTypes.Group)
            {
                text.Append(" (Group)");
            }

            return text.ToString();
        }
    }
}