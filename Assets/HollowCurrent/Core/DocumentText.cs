using System;
using System.Collections.Generic;
using System.Text;

namespace HollowCurrent
{
    public enum DocKind { Ledger, Letter, Inscription, Tally, FieldReport }

    // Engine-independent reading of a note: which kind of found document it is, the kicker / heading / subject
    // shown ONCE, and the body as logical lines ("" = paragraph break). Piece fb-docui (owner feedback 10): the baked
    // castle bodies begin with a header line ("BLOOD LEDGER / THE STEWARD") that used to be drawn a second time under the
    // card title. The header is now consumed here and becomes the heading; the card title is never repeated.
    public sealed class DocumentText
    {
        public DocKind Kind;
        public string Kicker = "", Heading = "", Subject = "", Lead = "";
        public readonly List<string> Lines = new List<string>();

        public static DocumentText Parse(bool castle, string title, string body)
        {
            title = (title ?? "").Trim();
            var doc = new DocumentText();
            var raw = new List<string>((body ?? "").Replace("\r", "").Split('\n'));
            for (int i = 0; i < raw.Count; i++) raw[i] = raw[i].TrimEnd();
            while (raw.Count > 0 && raw[0].Length == 0) raw.RemoveAt(0);

            string header = null, lead = null;
            if (raw.Count > 0)
            {
                bool followedByGap = raw.Count == 1 || raw[1].Length == 0;
                if (followedByGap && IsHeader(raw[0])) { header = raw[0]; raw.RemoveAt(0); }
                else if (followedByGap && raw[0].EndsWith(":", StringComparison.Ordinal)) { lead = raw[0].Substring(0, raw[0].Length - 1); raw.RemoveAt(0); }
            }
            while (raw.Count > 0 && raw[0].Length == 0) raw.RemoveAt(0);
            bool gap = false;
            foreach (var line in raw)
            {
                if (line.Length == 0) { gap = doc.Lines.Count > 0; continue; }
                if (gap) { doc.Lines.Add(""); gap = false; }
                doc.Lines.Add(line);
            }
            doc.Lead = lead ?? "";

            string lowerTitle = title.ToLowerInvariant();
            if (!castle)
            {
                // Bellwether: a typed field report. Kicker names the exhibit, the heading is the card title, the typed
                // header line becomes the SUBJECT line (it never equals the heading).
                doc.Kind = DocKind.FieldReport;
                doc.Kicker = "EXHIBIT";
                doc.Heading = title.Length > 0 ? title : Sentence(header ?? "Note");
                if (header != null && !SameWords(header, title)) doc.Subject = header;
                return doc;
            }

            bool ledger = lowerTitle.StartsWith("ledger", StringComparison.Ordinal) || lowerTitle.Contains("blood ledger") ||
                          lowerTitle.Contains("last entry") || (header != null && header.StartsWith("BLOOD LEDGER", StringComparison.Ordinal));
            if (lowerTitle.Contains("inscription")) doc.Kind = DocKind.Inscription;
            else if (lowerTitle.Contains("tally")) doc.Kind = DocKind.Tally;
            else if (ledger) doc.Kind = DocKind.Ledger;
            else doc.Kind = DocKind.Letter;

            switch (doc.Kind)
            {
                case DocKind.Ledger:
                    doc.Kicker = "BLOOD LEDGER";
                    string rest = null;
                    if (header != null && header.StartsWith("BLOOD LEDGER", StringComparison.Ordinal))
                    {
                        rest = header.Substring("BLOOD LEDGER".Length).TrimStart(' ', '/', ',', ':', '-');
                    }
                    else if (header != null) rest = header;
                    if (string.IsNullOrEmpty(rest))
                    {
                        int colon = title.IndexOf(':');
                        rest = colon >= 0 ? title.Substring(colon + 1).Trim() : title;
                        if (rest.ToLowerInvariant() == "blood ledger page") rest = "";
                    }
                    doc.Heading = rest.Length == 0 ? "A page of the ledger" : Sentence(rest);
                    break;
                case DocKind.Letter:
                    doc.Kicker = title.Length > 0 ? title.ToUpperInvariant() : "LETTER";
                    doc.Heading = header != null ? Sentence(header) : "";
                    if (doc.Heading.Length == 0) { doc.Heading = title; doc.Kicker = "LETTER"; }
                    break;
                case DocKind.Inscription:
                    doc.Kicker = doc.Lead.Length > 0 ? doc.Lead.ToUpperInvariant() : "CUT INTO STONE";
                    doc.Heading = title;
                    break;
                default: // Tally
                    doc.Kicker = doc.Lead.Length > 0 ? doc.Lead.ToUpperInvariant() : "SCRATCHED ON A TABLE";
                    doc.Heading = title;
                    break;
            }
            return doc;
        }

        // A typed header: has letters, no lower-case letters (digits and punctuation allowed), short.
        public static bool IsHeader(string line)
        {
            if (line.Length == 0 || line.Length > 70) return false;
            bool letters = false;
            foreach (char c in line)
            {
                if (char.IsLower(c)) return false;
                if (char.IsLetter(c)) letters = true;
            }
            return letters;
        }

        static string Normal(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }
        public static bool SameWords(string a, string b) => Normal(a) == Normal(b);

        static readonly string[] Proper = { "Margrave", "Bellwether", "Harrow", "Harrows", "Count" };

        // "THE MARGRAVE'S GATE" -> "The Margrave's gate"; "LAST PAGE IN THE STEWARD'S HAND" -> "Last page in the steward's hand".
        public static string Sentence(string caps)
        {
            if (string.IsNullOrEmpty(caps)) return "";
            var words = caps.Trim().Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i].ToLowerInvariant();
                foreach (var p in Proper)
                {
                    string pl = p.ToLowerInvariant();
                    if (w == pl || w == pl + "'s") { w = p + w.Substring(pl.Length); break; }
                }
                if (i == 0 && w.Length > 0) w = char.ToUpperInvariant(w[0]) + w.Substring(1);
                words[i] = w;
            }
            return string.Join(" ", words);
        }
    }
}
