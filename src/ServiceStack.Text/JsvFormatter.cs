//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//	 Peter Townsend (townsend.pete@gmail.com)
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2011 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Text
{
	public static class JsvFormatter
	{
		public static string Format(string serializedText)
		{
			if (string.IsNullOrEmpty(serializedText)) return null;

			var tabCount = 0;
			var sb = new StringBuilder();
            var firstKeySeparator = true;
            char? quoteChar = null;

			for (var i = 0; i < serializedText.Length; i++)
			{
				var current = serializedText[i];

                if (quoteChar.HasValue)
                {
                    sb.Append(current);
                    if (quoteChar.Value == current)
                        quoteChar = null;
                    continue;
                }
                else if (current == JsWriter.QuoteChar)
                {
                    sb.Append(current);
                    quoteChar = current;
                    continue;
                }

				var previous = i - 1 >= 0 ? serializedText[i - 1] : 0;
				var next = i < serializedText.Length - 1 ? serializedText[i + 1] : 0;

				if (current == JsWriter.MapStartChar || current == JsWriter.ListStartChar)
				{
					if (previous == JsWriter.MapKeySeperator)
					{
						if (next == JsWriter.MapEndChar || next == JsWriter.ListEndChar)
						{
							sb.Append(current);
							sb.Append(serializedText[++i]); //eat next
							continue;
						}

						AppendTabLine(sb, tabCount);
					}

					sb.Append(current);
					AppendTabLine(sb, ++tabCount);
					firstKeySeparator = true;
					continue;
				}

				if (current == JsWriter.MapEndChar || current == JsWriter.ListEndChar)
				{
					AppendTabLine(sb, --tabCount);
					sb.Append(current);
					firstKeySeparator = true;
					continue;
				}

				if (current == JsWriter.ItemSeperator)
				{
					sb.Append(current);
					AppendTabLine(sb, tabCount);
					firstKeySeparator = true;
					continue;
				}

				sb.Append(current);

				if (current == JsWriter.MapKeySeperator && firstKeySeparator)
				{
					sb.Append(" ");
					firstKeySeparator = false;
				}
			}

			return sb.ToString();
		}

        static void EatWhiteSpace(string serializedText, ref int pos)
        {
            pos++;
            while (pos < serializedText.Length)
            {
                switch (serializedText[pos])
                {
                    case JsWriter.SpaceChar:
                    case JsWriter.TabChar:
                    case JsWriter.LineFeedChar:
                    case JsWriter.ReturnChar:
                        pos++;
                        break;

                    default:
                        return;
                }
            }
        }

        public static string UnFormat(string serializedText)
        {
            if (serializedText == null || serializedText.Length == 0)
                return serializedText;

            var sb = new StringBuilder();
            var wsStart = 0;
            var wsLength = 0;

            var pos = 0;
            char? quoteChar = null;
            char current;

            EatWhiteSpace(serializedText, ref pos);

            do
            {
                current = serializedText[pos];

                if (quoteChar.HasValue)
                {
                    sb.Append(current);
                    pos++;
                    if (quoteChar.Value == current)
                        quoteChar = null;
                    continue;
                }

                if (current == JsWriter.EscapeChar)
                {
                    sb.Append(current);
                    sb.Append(serializedText[++pos]);
                    pos++;
                    wsLength = 0;
                    wsStart = pos;
                    continue;
                }

                switch (current)
                {
                    case JsWriter.QuoteChar:
                        sb.Append(current);
                        pos++;
                        quoteChar = current;
                        break;

                    case JsWriter.SpaceChar:
                    case JsWriter.TabChar:
                    case JsWriter.LineFeedChar:
                    case JsWriter.ReturnChar:
                        wsLength++;
                        pos++;
                        break;

                    case JsWriter.MapStartChar:
                    case JsWriter.ListStartChar:
                    case JsWriter.MapKeySeperator:
                    case JsWriter.ItemSeperator:
                    case JsWriter.MapEndChar:
                    case JsWriter.ListEndChar:
                        sb.Append(current);
                        EatWhiteSpace(serializedText, ref pos);
                        wsLength = 0;
                        break;

                    default:
                        if (wsLength > 0)
                            sb.Append(serializedText.Substring(wsStart, wsLength));
                        sb.Append(current);
                        pos++;
                        wsStart = pos;
                        wsLength = 0;
                        break;

                }
            } while (pos < serializedText.Length);

            return sb.ToString();
        }

		private static void AppendTabLine(StringBuilder sb, int tabCount)
		{
			sb.AppendLine();

			if (tabCount > 0)
			{
				sb.Append(new string('\t', tabCount));
			}
		}
	}
}