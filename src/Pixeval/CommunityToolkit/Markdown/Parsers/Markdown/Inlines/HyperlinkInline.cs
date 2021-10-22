﻿#region Copyright (c) Pixeval/Pixeval

// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2021 Pixeval/HyperlinkInline.cs
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
using System.Linq;
using Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Enums;
using Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Helpers;

namespace Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Inlines
{
    /// <summary>
    ///     Represents a type of hyperlink where the text and the target URL cannot be controlled
    ///     independently.
    /// </summary>
    public class HyperlinkInline : MarkdownInline, IInlineLeaf, ILinkElement
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HyperlinkInline" /> class.
        /// </summary>
        public HyperlinkInline()
            : base(MarkdownInlineType.RawHyperlink)
        {
        }

        /// <summary>
        ///     Gets or sets the type of hyperlink.
        /// </summary>
        public HyperlinkType LinkType { get; set; }

        /// <summary>
        ///     Gets or sets the text to display.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        ///     Gets or sets the URL to link to.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        ///     Gets this type of hyperlink does not have a tooltip.
        /// </summary>
        string? ILinkElement.Tooltip => null;

        /// <summary>
        ///     Returns the chars that if found means we might have a match.
        /// </summary>
        internal static void AddTripChars(List<InlineTripCharHelper> tripCharHelpers)
        {
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = '<', Method = InlineParseMethod.AngleBracketLink });
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = ':', Method = InlineParseMethod.Url });
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = '/', Method = InlineParseMethod.RedditLink });
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = '.', Method = InlineParseMethod.PartialLink });
            tripCharHelpers.Add(new InlineTripCharHelper { FirstChar = '@', Method = InlineParseMethod.Email });
        }

        /// <summary>
        ///     Attempts to parse a URL within angle brackets e.g. "http://www.reddit.com".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed URL, or <c>null</c> if this is not a URL. </returns>
        internal static InlineParseResult? ParseAngleBracketLink(string markdown, int start, int maxEnd)
        {
            var innerStart = start + 1;

            // Check for a known scheme e.g. "https://".
            var pos = -1;
            foreach (var scheme in MarkdownDocument.KnownSchemes.Where(scheme => maxEnd - innerStart >= scheme.Length && string.Equals(markdown.Substring(innerStart, scheme.Length), scheme, StringComparison.OrdinalIgnoreCase)))
            {
                // URL scheme found.
                pos = innerStart + scheme.Length;
                break;
            }

            if (pos == -1)
            {
                return null;
            }

            // Angle bracket links should not have any whitespace.
            var innerEnd = markdown.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '>' }, pos, maxEnd - pos);
            if (innerEnd == -1 || markdown[innerEnd] != '>')
            {
                return null;
            }

            // There should be at least one character after the http://.
            if (innerEnd == pos)
            {
                return null;
            }

            var url = markdown.Substring(innerStart, innerEnd - innerStart);
            return new InlineParseResult(new HyperlinkInline { Url = url, Text = url, LinkType = HyperlinkType.BracketedUrl }, start, innerEnd + 1);
        }

        /// <summary>
        ///     Attempts to parse a URL e.g. "http://www.reddit.com".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="tripPos"> The location of the colon character. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed URL, or <c>null</c> if this is not a URL. </returns>
        internal static InlineParseResult? ParseUrl(string markdown, int tripPos, int maxEnd)
        {
            var start = -1;

            // Check for a known scheme e.g. "https://".
            foreach (var schemeStart in from scheme in MarkdownDocument.KnownSchemes
                let schemeStart = tripPos - scheme.Length
                where schemeStart >= 0 && schemeStart <= maxEnd - scheme.Length && string.Equals(markdown.Substring(schemeStart, scheme.Length), scheme, StringComparison.OrdinalIgnoreCase)
                select schemeStart)
            {
                // URL scheme found.
                start = schemeStart;
                break;
            }

            switch (start)
            {
                case -1:
                // The previous character must be non-alphanumeric i.e. "ahttp://t.co" is not a valid URL.
                case > 0 when char.IsLetter(markdown[start - 1]):
                    return null;
            }

            // The URL must have at least one character after the http:// and at least one dot.
            var pos = tripPos + 3;
            if (pos > maxEnd)
            {
                return null;
            }

            var dotIndex = markdown.IndexOf('.', pos, maxEnd - pos);
            if (dotIndex == -1 || dotIndex == pos)
            {
                return null;
            }

            // Find the end of the URL.
            var end = FindUrlEnd(markdown, dotIndex + 1, maxEnd);

            var url = markdown.Substring(start, end - start);
            return new InlineParseResult(new HyperlinkInline { Url = url, Text = url, LinkType = HyperlinkType.FullUrl }, start, end);
        }

        /// <summary>
        ///     Attempts to parse a subreddit link e.g. "/r/news" or "r/news".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed subreddit or user link, or <c>null</c> if this is not a subreddit link. </returns>
        internal static InlineParseResult? ParseRedditLink(string markdown, int start, int maxEnd)
        {
            var result = ParseDoubleSlashLink(markdown, start, maxEnd);
            return result ?? ParseSingleSlashLink(markdown, start, maxEnd);
        }

        /// <summary>
        ///     Parse a link of the form "/r/news" or "/u/quinbd".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed subreddit or user link, or <c>null</c> if this is not a subreddit link. </returns>
        private static InlineParseResult? ParseDoubleSlashLink(string markdown, int start, int maxEnd)
        {
            // The minimum length is 4 characters ("/u/u").
            if (start > maxEnd - 4)
            {
                return null;
            }

            // Determine the type of link (subreddit or user).
            HyperlinkType linkType;
            switch (markdown[start + 1])
            {
                case 'r':
                    linkType = HyperlinkType.Subreddit;
                    break;
                case 'u':
                    linkType = HyperlinkType.User;
                    break;
                default:
                    return null;
            }

            // Check that there is another slash.
            if (markdown[start + 2] != '/')
            {
                return null;
            }

            // Find the end of the link.
            var end = FindEndOfRedditLink(markdown, start + 3, maxEnd);

            // Subreddit names must be at least two characters long, users at least one.
            if (end - start < (linkType == HyperlinkType.User ? 4 : 5))
            {
                return null;
            }

            // We found something!
            var text = markdown.Substring(start, end - start);
            return new InlineParseResult(new HyperlinkInline { Text = text, Url = text, LinkType = linkType }, start, end);
        }

        /// <summary>
        ///     Parse a link of the form "r/news" or "u/quinbd".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start parsing. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed subreddit or user link, or <c>null</c> if this is not a subreddit link. </returns>
        private static InlineParseResult? ParseSingleSlashLink(string markdown, int start, int maxEnd)
        {
            // The minimum length is 3 characters ("u/u").
            start--;
            if (start < 0 || start > maxEnd - 3)
            {
                return null;
            }

            // Determine the type of link (subreddit or user).
            HyperlinkType linkType;
            switch (markdown[start])
            {
                case 'r':
                    linkType = HyperlinkType.Subreddit;
                    break;
                case 'u':
                    linkType = HyperlinkType.User;
                    break;
                default:
                    return null;
            }

            // If the link doesn't start with '/', then the previous character must be
            // non-alphanumeric i.e. "bear/trap" is not a valid subreddit link.
            if (start >= 1 && (char.IsLetterOrDigit(markdown[start - 1]) || markdown[start - 1] == '/'))
            {
                return null;
            }

            // Find the end of the link.
            var end = FindEndOfRedditLink(markdown, start + 2, maxEnd);

            // Subreddit names must be at least two characters long, users at least one.
            if (end - start < (linkType == HyperlinkType.User ? 3 : 4))
            {
                return null;
            }

            // We found something!
            var text = markdown.Substring(start, end - start);
            return new InlineParseResult(new HyperlinkInline { Text = text, Url = "/" + text, LinkType = linkType }, start, end);
        }

        /// <summary>
        ///     Attempts to parse a URL without a scheme e.g. "www.reddit.com".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="tripPos"> The location of the dot character. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed URL, or <c>null</c> if this is not a URL. </returns>
        internal static InlineParseResult? ParsePartialLink(string markdown, int tripPos, int maxEnd)
        {
            var start = tripPos - 3;
            if (start < 0 || markdown[start] != 'w' || markdown[start + 1] != 'w' || markdown[start + 2] != 'w')
            {
                return null;
            }

            // The character before the "www" must be non-alphanumeric i.e. "bwww.reddit.com" is not a valid URL.
            if (start >= 1 && (char.IsLetterOrDigit(markdown[start - 1]) || markdown[start - 1] == '<'))
            {
                return null;
            }

            // The URL must have at least one character after the www.
            if (start >= maxEnd - 4)
            {
                return null;
            }

            // Find the end of the URL.
            var end = FindUrlEnd(markdown, start + 4, maxEnd);

            var url = markdown.Substring(start, end - start);
            return new InlineParseResult(new HyperlinkInline { Url = "http://" + url, Text = url, LinkType = HyperlinkType.PartialUrl }, start, end);
        }

        /// <summary>
        ///     Attempts to parse an email address e.g. "test@reddit.com".
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="minStart"> The minimum start position to return. </param>
        /// <param name="tripPos"> The location of the at character. </param>
        /// <param name="maxEnd"> The location to stop parsing. </param>
        /// <returns> A parsed URL, or <c>null</c> if this is not a URL. </returns>
        internal static InlineParseResult? ParseEmailAddress(string markdown, int minStart, int tripPos, int maxEnd)
        {
            // Search backwards until we find a character which is not a letter, digit, or one of
            // these characters: '+', '-', '_', '.'.
            // Note: it is intended that this code match the reddit.com markdown parser; there are
            // many characters which are legal in email addresses but which aren't picked up by
            // reddit (for example: '$' and '!').

            // Special characters as per https://en.wikipedia.org/wiki/Email_address#Local-part allowed
            char[] allowedChars = { '!', '#', '$', '%', '&', '\'', '*', '+', '-', '/', '=', '?', '^', '_', '`', '{', '|', '}', '~' };

            var start = tripPos;
            while (start > minStart)
            {
                var c = markdown[start - 1];
                if (c is (< 'a' or > 'z') and (< 'A' or > 'Z') and (< '0' or > '9') && !allowedChars.Contains(c))
                {
                    break;
                }

                start--;
            }

            // There must be at least one character before the '@'.
            if (start == tripPos)
            {
                return null;
            }

            // Search forwards until we find a character which is not a letter, digit, or one of
            // these characters: '-', '_'.
            // Note: it is intended that this code match the reddit.com markdown parser;
            // technically underscores ('_') aren't allowed in a host name.
            var dotIndex = tripPos + 1;
            while (dotIndex < maxEnd)
            {
                var c = markdown[dotIndex];
                if (c is (< 'a' or > 'z') and (< 'A' or > 'Z') and (< '0' or > '9') && c != '-' && c != '_')
                {
                    break;
                }

                dotIndex++;
            }

            // We are expecting a dot.
            if (dotIndex == maxEnd || markdown[dotIndex] != '.')
            {
                return null;
            }

            // Search forwards until we find a character which is not a letter, digit, or one of
            // these characters: '.', '-', '_'.
            // Note: it is intended that this code match the reddit.com markdown parser;
            // technically underscores ('_') aren't allowed in a host name.
            var end = dotIndex + 1;
            while (end < maxEnd)
            {
                var c = markdown[end];
                if (c is (< 'a' or > 'z') and (< 'A' or > 'Z') and (< '0' or > '9') && c != '.' && c != '-' && c != '_')
                {
                    break;
                }

                end++;
            }

            // There must be at least one character after the dot.
            if (end == dotIndex + 1)
            {
                return null;
            }

            // We found an email address!
            var emailAddress = markdown.Substring(start, end - start);
            return new InlineParseResult(new HyperlinkInline { Url = "mailto:" + emailAddress, Text = emailAddress, LinkType = HyperlinkType.Email }, start, end);
        }

        /// <summary>
        ///     Converts the object into it's textual representation.
        /// </summary>
        /// <returns> The textual representation of this object. </returns>
        public override string? ToString()
        {
            return Text ?? base.ToString();
        }

        /// <summary>
        ///     Finds the next character that is not a letter, digit or underscore in a range.
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start searching. </param>
        /// <param name="end"> The location to stop searching. </param>
        /// <returns> The location of the next character that is not a letter, digit or underscore. </returns>
        private static int FindEndOfRedditLink(string markdown, int start, int end)
        {
            var pos = start;
            while (pos < markdown.Length && pos < end)
            {
                var c = markdown[pos];
                if (c is (< 'a' or > 'z') and (< 'A' or > 'Z') and (< '0' or > '9') && c != '_' && c != '/')
                {
                    return pos;
                }

                pos++;
            }

            return end;
        }

        /// <summary>
        ///     Finds the end of a URL.
        /// </summary>
        /// <param name="markdown"> The markdown text. </param>
        /// <param name="start"> The location to start searching. </param>
        /// <param name="maxEnd"> The location to stop searching. </param>
        /// <returns> The location of the end of the URL. </returns>
        private static int FindUrlEnd(string markdown, int start, int maxEnd)
        {
            // For some reason a less than character ends a URL...
            var end = markdown.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '<' }, start, maxEnd - start);
            if (end == -1)
            {
                end = maxEnd;
            }

            // URLs can't end on a punctuation character.
            while (end - 1 > start)
            {
                if (Array.IndexOf(new[] { ')', '}', ']', '!', ';', '.', '?', ',' }, markdown[end - 1]) < 0)
                {
                    break;
                }

                end--;
            }

            return end;
        }
    }
}