// Prints the castle journal / end-card layout (Core/DocLayout.cs) as JSON, for tools/docui_journal_mock.py.
// Built by that script with the Unity-bundled csc and run with the Unity-bundled mono (same as tools/check_source.py).
using System;
using System.Collections.Generic;
using System.Text;
using HollowCurrent;

static class DocLayoutDump
{
    static string R(LRect r) => "[" + r.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "," + r.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "," + r.W.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "," + r.H.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "]";
    static string Screen(DocLayout l, string extra)
    {
        var sb = new StringBuilder("{\"screen\":\"" + l.Screen + "\",\"frames\":[");
        for (int i = 0; i < l.Frames.Count; i++) sb.Append((i > 0 ? "," : "") + R(l.Frames[i]));
        sb.Append("],\"ornaments\":[");
        for (int f = 0; f < l.Frames.Count; f++) for (int c = 0; c < 4; c++) sb.Append((f + c > 0 ? "," : "") + R(DocPanel.OrnamentRect(l.Frames[f], c)));
        sb.Append("],\"paper\":[");
        for (int i = 0; i < l.Frames.Count; i++) sb.Append((i > 0 ? "," : "") + R(DocPanel.Paper(l.Frames[i])));
        sb.Append("],\"safe\":[");
        for (int i = 0; i < l.Frames.Count; i++) sb.Append((i > 0 ? "," : "") + R(DocPanel.Safe(l.Frames[i])));
        sb.Append("],\"texts\":{");
        for (int i = 0; i < l.Texts.Count; i++) sb.Append((i > 0 ? "," : "") + "\"" + l.Names[i] + "\":" + R(l.Texts[i]));
        var bad = l.Verify();
        sb.Append("},\"overlaps\":" + bad.Count + ",\"violations\":[");
        for (int i = 0; i < bad.Count; i++) sb.Append((i > 0 ? "," : "") + "\"" + bad[i].Replace("\"", "'") + "\"");
        sb.Append("]," + extra + "}");
        return sb.ToString();
    }
    static void Main()
    {
        var j = DocLayout.Journal(); var e = DocLayout.Ending();
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        string je = "\"colL\":" + j.ColL.ToString(ci) + ",\"colR\":" + j.ColR.ToString(ci) + ",\"lowerR\":" + j.LowerR.ToString(ci) + ",\"x0\":" + j.X0.ToString(ci) + ",\"choiceStep\":" + j.ChoiceStep.ToString(ci) + ",\"choiceW\":" + j.ChoiceW.ToString(ci);
        string ee = "\"x\":" + e.X.ToString(ci) + ",\"metricX\":[" + e.MetricX[0].ToString(ci) + "," + e.MetricX[1].ToString(ci) + "," + e.MetricX[2].ToString(ci) + "],\"metricY\":" + e.MetricY.ToString(ci) + ",\"sep\":[" + e.Separator[0].ToString(ci) + "," + e.Separator[1].ToString(ci) + "]";
        Console.WriteLine("{\"journal\":" + Screen(j.Layout, je) + ",\"ending\":" + Screen(e.Layout, ee) + "}");
    }
}
