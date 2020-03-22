using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AddSub1000
{
    class Sheet
    {
        const int rows = 18;
        const int cols = 4;
        private int _numberOfSheets;
        private List<(int, int, char op, int)> _sums = new List<(int, int, char op, int)>();

        IEnumerable<(int n1, int n2)> GetNums()
        {
            const int length = 1_000_000;
            var arr = Enumerable.Range(1, length).ToArray();
            FisherShuffle(arr);

            foreach (var a in arr)
            {
                var n1 = a % 1000;
                var n2 = a / 1000;

                if (n1 < 10 || n2 < 10 || n1 == n2)
                {
                    continue;
                }
                if (n1 < 100 && n2 < 100)
                {
                    continue;
                }
                yield return (n1, n2);
            }
        }

        public Sheet(int numberOfSheets, bool across = true)
        {
            _numberOfSheets = numberOfSheets;
            MakeSums();
            MakeSheets();
        }

        private void MakeSums()
        {
            var rnd = new Random();

            for (int sheet = 0; sheet < _numberOfSheets; sheet++)
            {
                var count = 0;
                foreach (var (n1, n2) in GetNums())
                {
                    var op = rnd.Next(2);
                    if (op == 1) // minus
                    {
                        if (n1 < n2)
                        {
                            continue;
                        }
                        _sums.Add((n1, n2, '-', n1 - n2));
                    }
                    else
                    {
                        _sums.Add((n1, n2, '+', n1 + n2));
                    }

                    if (count++ == rows * cols * _numberOfSheets)
                    {
                        break;
                    }
                }
            }
        }

        void MakeSheets()
        {
            var batchConverterName = "makepdf.bat";
            using (var batchFileStream = new StreamWriter(batchConverterName))
            {
                batchFileStream.WriteLine("@echo off");
                batchFileStream.WriteLine("pskill -nobanner foxitreader >nul");
                for (var sheet = 0; sheet < _numberOfSheets; sheet++)
                {
                    var filename = MakeFilename(sheet);
                    batchFileStream.WriteLine($"wkhtmltopdf --dpi 400 {filename} {Path.ChangeExtension(filename, "pdf")}");
                }
            }

            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const int columns = 4;
            const int rows = 18;
            const int sumsPerPage = rows * columns;
            for (int sheet = 0; sheet < _numberOfSheets; sheet++)
            {
                var answers = new List<int>();
                var filename = MakeFilename(sheet);
                using (var stream = new StreamWriter(filename))
                {
                    stream.WriteLine("<!doctype html>");
                    stream.WriteLine("<html lang=\"en\">");
                    stream.WriteLine("<head>");
                    stream.WriteLine("<title>Nothing</title>");
                    stream.WriteLine("<style type='text/css'>");
                    stream.WriteLine("body {");
                    stream.WriteLine("font-size:20pt;");
                    stream.WriteLine("}");
                    stream.WriteLine(".sumstable {");
                    stream.WriteLine("text-align:center;");
                    stream.WriteLine("margin:0;");
                    stream.WriteLine("border-collapse:collapse;");
                    stream.WriteLine("}");
                    stream.WriteLine(".sumstable,.sumstable td + td {");
                    stream.WriteLine("padding-left:20px;");
                    stream.WriteLine("padding-top:20px;");
                    stream.WriteLine("padding-right:100px;");
                    stream.WriteLine("padding-bottom:20px;");
                    stream.WriteLine("color:black;");
                    stream.WriteLine("white-space: nowrap;");
                    stream.WriteLine("border:solid 1px #dddddd;");
                    stream.WriteLine("}");
                    stream.WriteLine(".sumstable,.sumstable td {");
                    stream.WriteLine("padding:0px;");
                    stream.WriteLine("padding-right:20px;");
                    stream.WriteLine("color:#dddddd;");
                    stream.WriteLine("white-space: nowrap;");
                    stream.WriteLine("border:solid 1px #dddddd;");
                    stream.WriteLine("}");
                    stream.WriteLine(".answerstable {");
                    stream.WriteLine("border-collapse:collapse;");
                    stream.WriteLine("text-align:center;");
                    stream.WriteLine("}");
                    stream.WriteLine(".answerstable, .answerstable td {");
                    stream.WriteLine("padding-left:20px;");
                    stream.WriteLine("padding-right:20px;");
                    stream.WriteLine("color:black;");
                    stream.WriteLine("white-space: nowrap;");
                    stream.WriteLine("border:solid 1px #dddddd;");
                    stream.WriteLine("}");
                    stream.WriteLine(".pageDivider {");
                    stream.WriteLine("page-break-before: always;");
                    stream.WriteLine("}");
                    stream.WriteLine(".tickslabel {");
                    stream.WriteLine("font-size: 12pt;");
                    stream.WriteLine("}");
                    stream.WriteLine("</style>");
                    stream.WriteLine("<script>");
                    stream.WriteLine("</script>");
                    stream.WriteLine("</head>");


                    var guidList = new List<Guid>();

                    var sumIndex = 0;
                    // first print all the question pages - answer pages will follow all the question 
                    // pages. Note that for double-sided printing, the number of sheets should ideally
                    // be even:
                    for (int page = 0; page < _numberOfSheets; page++)
                    {
                        var pageGuid = Guid.NewGuid();
                        guidList.Add(pageGuid);
                        stream.WriteLine($"<p class=\"tickslabel\">Page {page + 1} - {pageGuid}</p>");
                        stream.WriteLine($"<table class=\"sumstable\">");
                        
                        for (int i = 0; i < rows * columns; i++)
                        {
                            if (i % columns == 0)
                            {
                                stream.WriteLine($"<tr>");
                                stream.WriteLine($"<td class=\"col1\">{alphabet[i / columns]}</td>");
                            }

                            var (first, second, op, answer) = _sums[sumIndex++];
                            answers.Add(answer);

                            if (answer < 0)
                            {
                                Console.WriteLine();
                            }

                            stream.WriteLine($"<td>{first} {op} {second} = </td>");
                            if ((i + 1) % columns == 0)
                            {
                                stream.WriteLine($"</tr>");
                            }
                        }
                        stream.WriteLine($"</table>");
                        stream.WriteLine("<div class=\"pageDivider\"/>");
                    }

                    for (int page = 0; page < _numberOfSheets; page++)
                    {
                        stream.WriteLine("<div class=\"answers\">");
                        stream.WriteLine($"<p class=\"tickslabel\">Page {page + 1} - {guidList[page]}</p>");
                        stream.WriteLine("<h1>Answers</h1>");

                        stream.WriteLine($"<table class=\"answerstable\">");
                        for (int i = 0; i < sumsPerPage; i++)
                        {
                            if (i % columns == 0)
                            {
                                stream.WriteLine($"<tr>");
                                stream.WriteLine($"<td class=\"col1\">{alphabet[i / columns]}</td>");
                            }

                            stream.WriteLine($"<td>{answers[i + page * sumsPerPage]}</td>");

                            if ((i + 1) % columns == 0)
                            {
                                stream.WriteLine($"</tr>");
                            }

                        }
                        stream.WriteLine("</div>");
                        stream.WriteLine($"</table>");
                        stream.WriteLine("<div class=\"pageDivider\"/>");
                    }
                    stream.WriteLine($"</body>");
                    stream.WriteLine($"</html>");
                }
            }

        }

        private static void Shuffle<T>(IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        static void FisherShuffle<T>(IList<T> list)
        {
            var rnd = new Random();

            for (int i = list.Count() - 1; i > 0; i--)
            {
                // random from zero to i:
                var j = rnd.Next(i + 1);

                // swap:
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }


        private static string MakeFilename(int sheet)
        {
            return $"maths-worksheet-{sheet + 1:D3}.html";
        }
    }
    class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var sheets = new Sheet(2);
            }
            catch (Exception ex)
            {
                var fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
                var progname = Path.GetFileNameWithoutExtension(fullname);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
