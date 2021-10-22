﻿#region Copyright (c) Pixeval/Pixeval

// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2021 Pixeval/SuperscriptTextInline.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.Collections.Generic;
using Pixeval.CommunityToolkit.Markdown.Parsers.Core;
using Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Enums;
using Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Helpers;

namespace Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Inlines
{
    /// <summary>
    ///     Represents a span containing superscript text.
    /// </summary>
    public class SuperscriptTextInline : MarkdownInline, IInlineContainer
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SuperscriptTextInline" /> class.
        /// </summary>
        public SuperscriptTextInline()
            : base(MarkdownInlineType.Superscript)
        {
        }

        /// <summary>
        ///     Gets or sets the contents of the inline.
        /// </summary>
        public IList<MarkdownInline>? Inlines { get; set; }

        /// <summary>
        ///     Returns the chars that if found means we might have a match.
        /// </summary>
        internal static void AddTripChars(List<InlineTripCharHelper> tripCharHelpers)
        {
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = '^', Method = InlineParseMethod.Superscript });
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = '<', Method = InlineParseMethod.Superscript });
        }

        /// <summary>
        ///     Attempts to parse a superscript text span.
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed superscript text span, or <c>null</c> if this is not a superscript text span. </returns>
        internal static InlineParseResult? Parse(string markdown, int start, int maxEnd)
        {
            // Check the first character.
            var isHTMLSequence = false;
            if (start == maxEnd || markdown[start] != '^' && markdown[start] != '<')
            {
                return null;
            }

            if (markdown[start] != '^')
            {
                if (maxEnd - start < 5)
                {
                    return null;
                }

                if (markdown.Substring(start, 5) != "<sup>")
                {
                    return null;
                }

                isHTMLSequence = true;
            }

            if (isHTMLSequence)
            {
                var innerStart = start + 5;
                var innerEnd = Common.IndexOf(markdown, "</sup>", innerStart, maxEnd);
                if (innerEnd == -1)
                {
                    return null;
                }

                if (innerEnd == innerStart)
                {
                    return null;
                }

                if (ParseHelpers.IsMarkdownWhiteSpace(markdown[innerStart]) || ParseHelpers.IsMarkdownWhiteSpace(markdown[innerEnd - 1]))
                {
                    return null;
                }

                // We found something!
                var end = innerEnd + 6;
                var result = new SuperscriptTextInline
                {
                    Inlines = Common.ParseInlineChildren(markdown, innerStart, innerEnd)
                };
                return new InlineParseResult(result, start, end);
            }
            else
            {
                // The content might be enclosed in parentheses.
                var innerStart = start + 1;
                int innerEnd, end;
                if (innerStart < maxEnd && markdown[innerStart] == '(')
                {
                    // Find the end parenthesis.
                    innerStart++;
                    innerEnd = Common.IndexOf(markdown, ')', innerStart, maxEnd);
                    if (innerEnd == -1)
                    {
                        return null;
                    }

                    end = innerEnd + 1;
                }
                else
                {
                    // Search for the next whitespace character.
                    innerEnd = Common.FindNextWhiteSpace(markdown, innerStart, maxEnd, true);
                    if (innerEnd == innerStart)
                    {
                        // No match if the character after the caret is a space.
                        return null;
                    }

                    end = innerEnd;
                }

                // We found something!
                var result = new SuperscriptTextInline();
                result.Inlines = Common.ParseInlineChildren(markdown, innerStart, innerEnd);
                return new InlineParseResult(result, start, end);
            }
        }

        /// <summary>
        ///     Converts the object into it's textual representation.
        /// </summary>
        /// <returns> The textual representation of this object. </returns>
        public override string? ToString()
        {
            if (Inlines == null)
            {
                return base.ToString();
            }

            return "^(" + string.Join(string.Empty, Inlines) + ")";
        }
    }
}