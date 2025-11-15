/* JSON Interactive REPL loop to parse JSON texts, i.e. top-level grammar
 * productions according to RFC 4627.
 *
 * Microsoft Corporation (C) 2009 - All rights reserved.
 *
 * bartde      Sept 09      Created
 */

using System;
using System.Text;

namespace Microsoft.Json.Repl
{
    using Expressions;
    using Parser;

    /// <summary>
    /// JSON Interactive REPL loop to parse JSON texts, i.e. top-level grammar
    /// productions according to RFC 4627.
    /// </summary>
    class Program
    {
        /// <summary>
        /// REPL loop entry-point.
        /// </summary>
        static void Main()
        {
            Console.WriteLine("Microsoft JSON Interactive (C) Microsoft Corporation. All rights reserved.");
            Console.WriteLine("JSON Version {0}, parsing using a handcrafted LL(1) parser in C#.", typeof(Expression).Assembly.GetName().Version);
            Console.WriteLine();
            Console.WriteLine("Terminate JSON text with ;; to parse. To exit, press CTRL-C.");
            Console.WriteLine();

            string input = "";
            int line = 0;
            while (true)
            {
                int currentLineStart = line;

                Console.Write("> ");

                var sb = new StringBuilder();
                bool terminated = false;
                while (!terminated)
                {
                    input = Console.ReadLine();
                    terminated = input.TrimEnd(' ').EndsWith(";;");
                    sb.AppendLine(input);
                    line++;
                    if (!terminated)
                        Console.Write("- ");
                }

                input = sb.ToString().TrimEnd(' ');
                string jsonText = input.Substring(0, input.Length - 4 /* for ;;\r\n */);

                try
                {
                    var ex = Expression.Parse(jsonText);
                    Console.WriteLine(ex.ToString());
                }
                catch (ParseException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine();
                    int col, lne;
                    string lineWithError = GetLineAndColumnWithError(input, ex.Position, out lne, out col);
                    Console.WriteLine("  " + lineWithError);
                    Console.WriteLine("  " + new string('-', col) + "^");
                    Console.WriteLine();
                    Console.WriteLine(
                        "stdin({0},{1}): error JSON{2}: {3}",
                        currentLineStart + lne + 1 /* 1-based for pretty printing */,
                        col + 1 /* 1-based for pretty printing */,
                        ((int)ex.Error).ToString().PadLeft(4, '0'),
                        ex.Message);
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Helper function to map an error location in the input to line and column info.
        /// </summary>
        /// <param name="input">Input containing the error.</param>
        /// <param name="pos">Location of the error.</param>
        /// <param name="lne">Line in the input at which the error occurs.</param>
        /// <param name="col">Column within the line in the input at which the error occurs.</param>
        /// <returns>Text on the line with the error.</returns>
        static string GetLineAndColumnWithError(string input, int pos, out int lne, out int col)
        {
            string[] lines = input.Replace("\r\n", "\n").Split('\n');

            if (pos < 0) // e.g. -1 means last character
                pos = input.Length + pos - 4 /* ;;\r\n */;
           
            int startIndexCurrentLine = 0;
            lne = 0;
            foreach (string line in lines)
            {
                var startIndexNextLine = startIndexCurrentLine + line.Length + 2 /* \r\n */;
                if (pos >= startIndexCurrentLine && pos < startIndexNextLine)
                {
                    col = pos - startIndexCurrentLine;
                    return line;
                }

                startIndexCurrentLine = startIndexNextLine;
                lne++;
            }

            col = 0;
            return "";
        }
    }
}