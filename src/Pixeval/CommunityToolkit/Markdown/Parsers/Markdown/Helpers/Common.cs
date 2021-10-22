﻿#region Copyright (c) Pixeval/Pixeval

// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2021 Pixeval/Common.cs
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
using Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Inlines;

namespace Pixeval.CommunityToolkit.Markdown.Parsers.Markdown.Helpers
{
    /// <summary>
    ///     Helpers for Markdown.
    /// </summary>
    internal class Common
    {
        private static readonly List<InlineTripCharHelper> _triggerList = new();
        private static readonly char[] _tripCharacters;

        static Common()
        {
            BoldItalicTextInline.AddTripChars(_triggerList);
            BoldTextInline.AddTripChars(_triggerList);
            ItalicTextInline.AddTripChars(_triggerList);
            MarkdownLinkInline.AddTripChars(_triggerList);
            HyperlinkInline.AddTripChars(_triggerList);
            CommentInline.AddTripChars(_triggerList);
            StrikeThroughTextInline.AddTripChars(_triggerList);
            SuperscriptTextInline.AddTripChars(_triggerList);
            SubscriptTextInline.AddTripChars(_triggerList);
            CodeInline.AddTripChars(_triggerList);
            ImageInline.AddTripChars(_triggerList);
            EmojiInline.AddTripChars(_triggerList);
            LinkAnchorInline.AddTripChars(_triggerList);

            // Create an array of characters to search against using IndexOfAny.
            _tripCharacters = _triggerList.Select(trigger => trigger.FirstChar).Distinct().ToArray();
        }

        /// <summary>
        ///     This function can be called by any element parsing. Given a start and stopping point this will
        ///     parse all found elements out of the range.
        /// </summary>
        /// <returns> A list of parsed inlines. </returns>
        public static List<MarkdownInline> ParseInlineChildren(string markdown, int startingPos, int maxEndingPos, bool ignoreLinks = false)
        {
            var currentParsePosition = startingPos;

            var inlines = new List<MarkdownInline>();
            while (currentParsePosition < maxEndingPos)
            {
                // Find the next inline element.
                var parseResult = FindNextInlineElement(markdown, currentParsePosition, maxEndingPos, ignoreLinks);

                // If the element we found doesn't start at the position we are looking for there
                // is text between the element and the start of the parsed element. We need to wrap
                // it into a text run.
                if (parseResult.Start != currentParsePosition)
                {
                    var textRun = TextRunInline.Parse(markdown, currentParsePosition, parseResult.Start);
                    inlines.Add(textRun);
                }

                // Add the parsed element.
                inlines.Add(parseResult.ParsedElement);

                // Update the current position.
                currentParsePosition = parseResult.End;
            }

            return inlines;
        }

        /// <summary>
        ///     Finds the next inline element by matching trip chars and verifying the match.
        /// </summary>
        /// <param name="markdown"> The markdown text to parse. </param>
        /// <param name="start"> The position to start parsing. </param>
        /// <param name="end"> The position to stop parsing. </param>
        /// <param name="ignoreLinks"> Indicates whether to parse links. </param>
        /// <returns>Returns the next element</returns>
        private static InlineParseResult FindNextInlineElement(string markdown, int start, int end, bool ignoreLinks)
        {
            // Search for the next inline sequence.
            for (var pos = start; pos < end; pos++)
            {
                // IndexOfAny should be the fastest way to skip characters we don't care about.
                pos = markdown.IndexOfAny(_tripCharacters, pos, end - pos);
                if (pos < 0)
                {
                    break;
                }

                // Find the trigger(s) that matched.
                var currentChar = markdown[pos];
                foreach (var currentTripChar in _triggerList)
                {
                    // Check if our current char matches the suffix char.
                    if (currentChar == currentTripChar.FirstChar)
                    {
                        // Don't match if the previous character was a backslash.
                        if (pos > start && markdown[pos - 1] == '\\')
                        {
                            continue;
                        }

                        // If we are here we have a possible match. Call into the inline class to verify.
                        InlineParseResult? parseResult = null;
                        switch (currentTripChar.Method)
                        {
                            case InlineParseMethod.BoldItalic:
                                parseResult = BoldItalicTextInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Comment:
                                parseResult = CommentInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.LinkReference:
                                parseResult = LinkAnchorInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Bold:
                                parseResult = BoldTextInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Italic:
                                parseResult = ItalicTextInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.MarkdownLink:
                                if (!ignoreLinks)
                                {
                                    parseResult = MarkdownLinkInline.Parse(markdown, pos, end);
                                }

                                break;

                            case InlineParseMethod.AngleBracketLink:
                                if (!ignoreLinks)
                                {
                                    parseResult = HyperlinkInline.ParseAngleBracketLink(markdown, pos, end);
                                }

                                break;

                            case InlineParseMethod.Url:
                                if (!ignoreLinks)
                                {
                                    parseResult = HyperlinkInline.ParseUrl(markdown, pos, end);
                                }

                                break;

                            case InlineParseMethod.RedditLink:
                                if (!ignoreLinks)
                                {
                                    parseResult = HyperlinkInline.ParseRedditLink(markdown, pos, end);
                                }

                                break;

                            case InlineParseMethod.PartialLink:
                                if (!ignoreLinks)
                                {
                                    parseResult = HyperlinkInline.ParsePartialLink(markdown, pos, end);
                                }

                                break;

                            case InlineParseMethod.Email:
                                if (!ignoreLinks)
                                {
                                    parseResult = HyperlinkInline.ParseEmailAddress(markdown, start, pos, end);
                                }

                                break;

                            case InlineParseMethod.StrikeThrough:
                                parseResult = StrikeThroughTextInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Superscript:
                                parseResult = SuperscriptTextInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Subscript:
                                parseResult = SubscriptTextInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Code:
                                parseResult = CodeInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Image:
                                parseResult = ImageInline.Parse(markdown, pos, end);
                                break;

                            case InlineParseMethod.Emoji:
                                parseResult = EmojiInline.Parse(markdown, pos, end);
                                break;
                        }

                        if (parseResult != null)
                        {
                            return parseResult;
                        }
                    }
                }
            }

            // If we didn't find any elements we have a normal text block.
            // Let us consume the entire range.
            return new InlineParseResult(TextRunInline.Parse(markdown, start, end), start, end);
        }

        /// <summary>
        ///     Returns the next \n or \r\n in the markdown.
        /// </summary>
        /// <returns>the next single line</returns>
        public static int FindNextSingleNewLine(string markdown, int startingPos, int endingPos, out int startOfNextLine)
        {
            // A line can end with CRLF (\r\n) or just LF (\n).
            var lineFeedPos = markdown.IndexOf('\n', startingPos);
            if (lineFeedPos == -1)
            {
                // Trying with /r now
                lineFeedPos = markdown.IndexOf('\r', startingPos);
                if (lineFeedPos == -1)
                {
                    startOfNextLine = endingPos;
                    return endingPos;
                }
            }

            startOfNextLine = lineFeedPos + 1;

            // Check if it was a CRLF.
            if (lineFeedPos > startingPos && markdown[lineFeedPos - 1] == '\r')
            {
                return lineFeedPos - 1;
            }

            return lineFeedPos;
        }

        /// <summary>
        ///     Helper function for index of with a start and an ending.
        /// </summary>
        /// <returns>Pos of the searched for item</returns>
        public static int IndexOf(string markdown, string search, int startingPos, int endingPos, bool reverseSearch = false)
        {
            // Check the ending isn't out of bounds.
            if (endingPos > markdown.Length)
            {
                endingPos = markdown.Length;
                DebuggingReporter.ReportCriticalError("IndexOf endingPos > string length");
            }

            // Figure out how long to go
            var count = endingPos - startingPos;
            if (count < 0)
            {
                return -1;
            }

            // Make sure we don't go too far.
            var remainingCount = markdown.Length - startingPos;
            if (count > remainingCount)
            {
                DebuggingReporter.ReportCriticalError("IndexOf count > remaining count");
                count = remainingCount;
            }

            // Check the ending. Since we use inclusive ranges we need to -1 from this for
            // reverses searches.
            if (reverseSearch && endingPos > 0)
            {
                endingPos -= 1;
            }

            return reverseSearch ? markdown.LastIndexOf(search, endingPos, count, StringComparison.OrdinalIgnoreCase) : markdown.IndexOf(search, startingPos, count, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Helper function for index of with a start and an ending.
        /// </summary>
        /// <returns>Pos of the searched for item</returns>
        public static int IndexOf(string markdown, char search, int startingPos, int endingPos, bool reverseSearch = false)
        {
            // Check the ending isn't out of bounds.
            if (endingPos > markdown.Length)
            {
                endingPos = markdown.Length;
                DebuggingReporter.ReportCriticalError("IndexOf endingPos > string length");
            }

            // Figure out how long to go
            var count = endingPos - startingPos;
            if (count < 0)
            {
                return -1;
            }

            // Make sure we don't go too far.
            var remainingCount = markdown.Length - startingPos;
            if (count > remainingCount)
            {
                DebuggingReporter.ReportCriticalError("IndexOf count > remaining count");
                count = remainingCount;
            }

            // Check the ending. Since we use inclusive ranges we need to -1 from this for
            // reverses searches.
            if (reverseSearch && endingPos > 0)
            {
                endingPos -= 1;
            }

            return reverseSearch ? markdown.LastIndexOf(search, endingPos, count) : markdown.IndexOf(search, startingPos, count);
        }

        /// <summary>
        ///     Finds the next whitespace in a range.
        /// </summary>
        /// <returns>pos of the white space</returns>
        public static int FindNextWhiteSpace(string markdown, int startingPos, int endingPos, bool ifNotFoundReturnLength)
        {
            var currentPos = startingPos;
            while (currentPos < markdown.Length && currentPos < endingPos)
            {
                if (char.IsWhiteSpace(markdown[currentPos]))
                {
                    return currentPos;
                }

                currentPos++;
            }

            return ifNotFoundReturnLength ? endingPos : -1;
        }

        /// <summary>
        ///     Parses lines.
        /// </summary>
        /// <returns>LineInfo</returns>
        public static IEnumerable<LineInfo> ParseLines(string markdown, int start, int end, int quoteDepth)
        {
            var pos = start;
            var lineStartsNewParagraph = true;

            while (pos < end)
            {
                var startOfLine = pos;
                var expectedQuotesRemaining = quoteDepth;
                var nonSpacePos = pos;
                var nonSpaceChar = '\0';
                while (true)
                {
                    // Find the next non-space char.
                    while (nonSpacePos < end)
                    {
                        var c = markdown[nonSpacePos];
                        if (c is '\r' or '\n')
                        {
                            // The line is either entirely whitespace, or is empty.
                            break;
                        }

                        if (c != ' ' && c != '\t')
                        {
                            // The line has content.
                            nonSpaceChar = c;
                            break;
                        }

                        nonSpacePos++;
                    }

                    // When parsing blocks in a block quote context, we need to count the number of
                    // quote characters ('>').  If there are less than expected AND this is the
                    // start of a new paragraph, then stop parsing.
                    if (expectedQuotesRemaining == 0)
                    {
                        break;
                    }

                    if (nonSpaceChar == '>')
                    {
                        // Expected block quote characters should be ignored.
                        expectedQuotesRemaining--;
                        nonSpacePos++;
                        nonSpaceChar = '\0';
                        startOfLine = nonSpacePos;

                        // Ignore the first space after the quote character, if there is one.
                        if (startOfLine < end && markdown[startOfLine] == ' ')
                        {
                            startOfLine++;
                            nonSpacePos++;
                        }
                    }
                    else
                    {
                        // There were less block quote characters than expected.
                        // But it doesn't matter if this is not the start of a new paragraph.
                        if (!lineStartsNewParagraph || nonSpaceChar == '\0')
                        {
                            break;
                        }

                        // This must be the end of the block quote.  End the current paragraph, if any.
                        yield break;
                    }
                }

                // Find the end of the current line.
                var endOfLine = FindNextSingleNewLine(markdown, nonSpacePos, end, out var startOfNextLine);

                // Return the line info to the caller.
                yield return new LineInfo
                {
                    StartOfLine = startOfLine,
                    FirstNonWhitespaceChar = nonSpacePos,
                    EndOfLine = endOfLine,
                    StartOfNextLine = startOfNextLine
                };

                if (nonSpaceChar == '\0')
                {
                    // The line is empty or nothing but whitespace.
                    lineStartsNewParagraph = true;
                }

                // Repeat.
                pos = startOfNextLine;
            }
        }

        /// <summary>
        ///     Skips a certain number of quote characters (>).
        /// </summary>
        /// <returns>Skip Quote Chars</returns>
        public static int SkipQuoteCharacters(string markdown, int start, int end, int quoteDepth)
        {
            if (quoteDepth == 0)
            {
                return start;
            }

            var startOfLine = start;
            var nonSpacePos = start;
            var nonSpaceChar = '\0';

            while (true)
            {
                // Find the next non-space char.
                while (nonSpacePos < end)
                {
                    var c = markdown[nonSpacePos];
                    if (c is '\r' or '\n')
                    {
                        // The line is either entirely whitespace, or is empty.
                        break;
                    }

                    if (c != ' ' && c != '\t')
                    {
                        // The line has content.
                        nonSpaceChar = c;
                        break;
                    }

                    nonSpacePos++;
                }

                // When parsing blocks in a block quote context, we need to count the number of
                // quote characters ('>').  If there are less than expected AND this is the
                // start of a new paragraph, then stop parsing.
                if (quoteDepth == 0)
                {
                    break;
                }

                if (nonSpaceChar == '>')
                {
                    // Expected block quote characters should be ignored.
                    quoteDepth--;
                    nonSpacePos++;
                    nonSpaceChar = '\0';
                    startOfLine = nonSpacePos;

                    // Ignore the first space after the quote character, if there is one.
                    if (startOfLine < end && markdown[startOfLine] == ' ')
                    {
                        startOfLine++;
                        nonSpacePos++;
                    }
                }
                else
                {
                    // There were less block quote characters than expected.
                    break;
                }
            }

            return startOfLine;
        }

        /// <summary>
        ///     Checks if the given URL is allowed in a markdown link.
        /// </summary>
        /// <param name="url"> The URL to check. </param>
        /// <returns> <c>true</c> if the URL is valid; <c>false</c> otherwise. </returns>
        public static bool IsUrlValid(string url)
        {
            // URLs can be relative.
            return !Uri.TryCreate(url, UriKind.Absolute, out var result) || MarkdownDocument.KnownSchemes.Any(scheme => result.Scheme.Equals(scheme));
        }
    }
}