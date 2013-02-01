namespace ShareDeployed.Common.Extensions
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	public static class Tokenizer
	{
		public static IEnumerable<KeyValuePair<string, int>> Tokenize(string content)
		{
			string[] tokens = { "===", "!==", "==", "<=", ">=", "!=", "-=", "+=", "*=", "/=", "|=", "%=", "^=", ">>=", ">>>=", "<<=",
                "++", "--", "+", "-", "*", "\\", "/", "&&", "||", "&", "|", "%", "^", "~", "<<", ">>>", ">>",
                "[", "]", "(", ")", ";", ".", "!", "?", ":", ",", "'", "\"", "{", "}" };
			var escapedTokens = from token in tokens
								select ("\\" + string.Join("\\",
								(from c in token.ToCharArray() select c.ToString()).ToArray()));

			string pattern = "[a-zA-Z_0-9\\$]+|" + string.Join("|", escapedTokens.ToArray());
			var r = new Regex(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
			
			return from m in r.Matches(content).Cast<Match>()
				   group m by m.Value into g
				   orderby g.Count() descending
				   select new KeyValuePair<string, int>(g.Key, g.Count());
		}
	}
}