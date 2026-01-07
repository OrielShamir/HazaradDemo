using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DatwiseSafetyDemo.Services
{
    /// <summary>
    /// Tiny PDF writer for simple text reports (no external dependencies).
    /// Produces a single-page PDF with monospaced-like lines.
    /// </summary>
    public static class SimplePdfWriter
    {
        public static byte[] CreateSinglePageTextPdf(IEnumerable<string> lines, string title)
        {
            var safeLines = (lines ?? Enumerable.Empty<string>()).Select(l => l ?? string.Empty).ToList();

            // PDF content stream using text commands (BT/ET). Using Helvetica.
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine("50 780 Td");

            if (!string.IsNullOrWhiteSpace(title))
            {
                sb.AppendLine($"({Escape(title)}) Tj");
                sb.AppendLine("0 -16 Td");
                sb.AppendLine("0 -8 Td");
            }

            foreach (var line in safeLines.Take(60)) // keep within one page
            {
                sb.AppendLine($"({Escape(TrimTo(line, 120))}) Tj");
                sb.AppendLine("0 -12 Td");
            }

            sb.AppendLine("ET");

            var contentBytes = Encoding.ASCII.GetBytes(sb.ToString());

            // Build minimal PDF structure
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, Encoding.ASCII))
            {
                var xref = new List<long>();

                writer.WriteLine("%PDF-1.4");

                // 1: catalog
                xref.Add(ms.Position);
                writer.WriteLine("1 0 obj");
                writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
                writer.WriteLine("endobj");

                // 2: pages
                xref.Add(ms.Position);
                writer.WriteLine("2 0 obj");
                writer.WriteLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
                writer.WriteLine("endobj");

                // 3: page
                xref.Add(ms.Position);
                writer.WriteLine("3 0 obj");
                writer.WriteLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 5 0 R /Resources << /Font << /F1 4 0 R >> >> >>");
                writer.WriteLine("endobj");

                // 4: font
                xref.Add(ms.Position);
                writer.WriteLine("4 0 obj");
                writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
                writer.WriteLine("endobj");

                // 5: content stream
                xref.Add(ms.Position);
                writer.WriteLine("5 0 obj");
                writer.WriteLine($"<< /Length {contentBytes.Length} >>");
                writer.WriteLine("stream");
                writer.Flush();
                ms.Write(contentBytes, 0, contentBytes.Length);
                writer.WriteLine();
                writer.WriteLine("endstream");
                writer.WriteLine("endobj");

                // xref table
                var xrefPos = ms.Position;
                writer.WriteLine("xref");
                writer.WriteLine($"0 {xref.Count + 1}");
                writer.WriteLine("0000000000 65535 f ");

                foreach (var pos in xref)
                {
                    writer.WriteLine(pos.ToString("0000000000") + " 00000 n ");
                }

                // trailer
                writer.WriteLine("trailer");
                writer.WriteLine($"<< /Size {xref.Count + 1} /Root 1 0 R >>");
                writer.WriteLine("startxref");
                writer.WriteLine(xrefPos);
                writer.WriteLine("%%EOF");

                writer.Flush();
                return ms.ToArray();
            }
        }

        private static string Escape(string s)
        {
            return (s ?? string.Empty).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private static string TrimTo(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }
    }
}
