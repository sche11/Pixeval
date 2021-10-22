﻿#region Copyright (c) Pixeval/Pixeval

// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2021 Pixeval/MarkdownLinkInline.cs
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

using System;
using System.Collections.Generic;
using Pixeval.CommunityToolkit.Markdown.Parsers.Core;
using Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Enums;
using Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Helpers;

namespace Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Inlines
{
    /// <summary>
    ///     Represents a type of hyperlink where the text can be different from the target URL.
    /// </summary>
    public class MarkdownLinkInline : MarkdownInline, IInlineContainer, ILinkElement
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MarkdownLinkInline" /> class.
        /// </summary>
        public MarkdownLinkInline()
            : base(MarkdownInlineType.MarkdownLink)
        {
        }

        /// <summary>
        ///     Gets or sets the ID of a reference, if this is a reference-style link.
        /// </summary>
        public string? ReferenceId { get; set; }

        /// <summary>
        ///     Gets or sets the contents of the inline.
        /// </summary>
        public IList<MarkdownInline>? Inlines { get; set; }

        /// <summary>
        ///     Gets or sets the link URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        ///     Gets or sets a tooltip to display on hover.
        /// </summary>
        public string? Tooltip { get; set; }

        /// <summary>
        ///     Returns the chars that if found means we might have a match.
        /// </summary>
        internal static void AddTripChars(List<InlineTripCharHelper> tripCharHelpers)
        {
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = '[', Method = InlineParseMethod.MarkdownLink });
        }

        /// <summary>
        ///     Attempts to parse a markdown link e.g. "[](http://www.reddit.com)".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed markdown link, or <c>null</c> if this is not a markdown link. </returns>
        internal static InlineParseResult? Parse(string markdown, int start, int maxEnd)
        {
            // Expect a '[' character.
            if (start == maxEnd || markdown[start] != '[')
            {
                return null;
            }

            // Find the ']' character, keeping in mind that [test [0-9]](http://www.test.com) is allowed.
            var linkTextOpen = start + 1;
            var pos = linkTextOpen;
            int linkTextClose;
            var openSquareBracketCount = 0;
            while (true)
            {
                linkTextClose = markdown.IndexOfAny(new[] { '[', ']' }, pos, maxEnd - pos);
                if (linkTextClose == -1)
                {
                    return null;
                }

                if (markdown[linkTextClose] == '[')
                {
                    openSquareBracketCount++;
                }
                else if (openSquareBracketCount > 0)
                {
                    openSquareBracketCount--;
                }
                else
                {
                    break;
                }

                pos = linkTextClose + 1;
            }

            // Skip whitespace.
            pos = linkTextClose + 1;
            while (pos < maxEnd && ParseHelpers.IsMarkdownWhiteSpace(markdown[pos]))
            {
                pos++;
            }

            if (pos == maxEnd)
            {
                return null;
            }

            // Expect the '(' character or the '[' character.
            var linkOpen = pos;
            switch (markdown[pos])
            {
                case '(':
                {
                    // Skip whitespace.
                    linkOpen++;
                    while (linkOpen < maxEnd && ParseHelpers.IsMarkdownWhiteSpace(markdown[linkOpen]))
                    {
                        linkOpen++;
                    }

                    // Find the ')' character.
                    pos = linkOpen;
                    var linkClose = -1;
                    var openParenthesis = 0;
                    while (pos < maxEnd)
                    {
                        if (markdown[pos] == ')')
                        {
                            if (openParenthesis == 0)
                            {
                                linkClose = pos;
                                break;
                            }

                            openParenthesis--;
                        }

                        if (markdown[pos] == '(')
                        {
                            openParenthesis++;
                        }

                        pos++;
                    }

                    if (pos >= maxEnd)
                    {
                        return null;
                    }

                    var end = linkClose + 1;

                    // Skip whitespace backwards.
                    while (linkClose > linkOpen && ParseHelpers.IsMarkdownWhiteSpace(markdown[linkClose - 1]))
                    {
                        linkClose--;
                    }

                    // If there is no text whatsoever, then this is not a valid link.
                    if (linkOpen == linkClose)
                    {
                        return null;
                    }

                    // Check if there is tooltip text.
                    string url;
                    string? tooltip = null;
                    var lastUrlCharIsDoubleQuote = markdown[linkClose - 1] == '"';
                    var tooltipStart = Common.IndexOf(markdown, " \"", linkOpen, linkClose - 1);
                    if (tooltipStart == linkOpen)
                    {
                        return null;
                    }

                    if (lastUrlCharIsDoubleQuote && tooltipStart != -1)
                    {
                        // Extract the URL (resolving any escape sequences).
                        url = TextRunInline.ResolveEscapeSequences(markdown, linkOpen, tooltipStart).TrimEnd(' ', '\t', '\r', '\n');
                        tooltip = markdown.Substring(tooltipStart + 2, linkClose - 1 - (tooltipStart + 2));
                    }
                    else
                    {
                        // Extract the URL (resolving any escape sequences).
                        url = TextRunInline.ResolveEscapeSequences(markdown, linkOpen, linkClose);
                    }

                    // Check the URL is okay.
                    if (!url.IsEmail())
                    {
                        if (!Common.IsUrlValid(url))
                        {
                            return null;
                        }
                    }
                    else
                    {
                        tooltip = url = $"mailto:{url}";
                    }

                    // We found a regular stand-alone link.
                    var result = new MarkdownLinkInline
                    {
                        Inlines = Common.ParseInlineChildren(markdown, linkTextOpen, linkTextClose, true),
                        Url = url.Replace(" ", "%20"),
                        Tooltip = tooltip
                    };
                    return new InlineParseResult(result, start, end);
                }
                case '[':
                {
                    // Find the ']' character.
                    var linkClose = Common.IndexOf(markdown, ']', pos + 1, maxEnd);
                    if (linkClose == -1)
                    {
                        return null;
                    }

                    // We found a reference-style link.
                    var result = new MarkdownLinkInline
                    {
                        Inlines = Common.ParseInlineChildren(markdown, linkTextOpen, linkTextClose, true),
                        ReferenceId = markdown.Substring(linkOpen + 1, linkClose - (linkOpen + 1))
                    };
                    if (result.ReferenceId == string.Empty)
                    {
                        result.ReferenceId = markdown.Substring(linkTextOpen, linkTextClose - linkTextOpen);
                    }

                    return new InlineParseResult(result, start, linkClose + 1);
                }
                default:
                    return null;
            }
        }

        /// <summary>
        ///     If this is a reference-style link, attempts to converts it to a regular link.
        /// </summary>
        /// <param name="document"> The document containing the list of references. </param>
        internal void ResolveReference(MarkdownDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (ReferenceId == null)
            {
                return;
            }

            // Look up the reference ID.
            var reference = document.LookUpReference(ReferenceId);
            if (reference == null)
            {
                return;
            }

            // The reference was found. Check the URL is valid.
            if (!Common.IsUrlValid(reference.Url!))
            {
                return;
            }

            // Everything is cool when you're part of a team.
            Url = reference.Url;
            Tooltip = reference.Tooltip;
            ReferenceId = null;
        }

        /// <summary>
        ///     Converts the object into it's textual representation.
        /// </summary>
        /// <returns> The textual representation of this object. </returns>
        public override string? ToString()
        {
            if (Inlines == null || Url == null)
            {
                return base.ToString();
            }

            return ReferenceId != null ? $"[{string.Join(string.Empty, Inlines)}][{ReferenceId}]" : $"[{string.Join(string.Empty, Inlines)}]({Url})";
        }
    }
}