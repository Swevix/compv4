using System;
using System.Collections.Generic;
using System.Text;

namespace lab1_compiler.Bar
{
    public class ParsingError
    {
        public int NumberOfError { get; set; }
        public string Message { get; set; }
        public string ExpectedToken { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class RawTextParser
    {
        private List<ParsingError> Errors = new List<ParsingError>();
        private int _errorNumber = 1;

        public List<ParsingError> Parse(string text)
        {
            Correct(text);
            return Errors;
        }

        public (string correctedText, List<ParsingError> errors) Correct(string text)
        {
            Errors.Clear();
            _errorNumber = 1;

            int i = 0, line = 1, col = 1, length = text.Length;
            var sb = new StringBuilder();

            // Стек (startLine, startCol, quoteChar)
            var multiLineStack = new Stack<(int, int, char)>();

            // Первая попытка «1–2 кавычек» внутри тела
            bool hasStrayClosingAttempt = false;
            int strayLine = 0, strayCol = 0;

            while (i < length)
            {
                char c = text[i];

                // перевод строки
                if (c == '\n')
                {
                    sb.Append('\n');
                    line++; col = 1; i++;
                    continue;
                }

                // однострочный комментарий
                if (c == '#' && multiLineStack.Count == 0)
                {
                    sb.Append('#');
                    i++; col++;
                    while (i < length && text[i] != '\n')
                    {
                        sb.Append(text[i]);
                        i++; col++;
                    }
                    continue;
                }

                // кавычки
                if (c == '\'' || c == '"')
                {
                    char qc = c;

                    // закрытие многострочного?
                    if (multiLineStack.Count > 0 && multiLineStack.Peek().Item3 == qc)
                    {
                        int j = i, cnt = 0;
                        while (j < length && text[j] == qc)
                        {
                            cnt++; j++;
                        }

                        if (cnt >= 3)
                        {
                            // закрываем ровно тремя кавычками
                            sb.Append(new string(qc, 3));
                            multiLineStack.Pop();
                            i += 3; col += 3;
                            continue;
                        }
                        else
                        {
                            // 1–2 кавычки внутри тела
                            if (!hasStrayClosingAttempt)
                            {
                                hasStrayClosingAttempt = true;
                                strayLine = line;
                                strayCol = col;
                            }
                            sb.Append(new string(qc, cnt));
                            i += cnt; col += cnt;
                            continue;
                        }
                    }

                    // открытие многострочного
                    {
                        int tokenCol = col;
                        int j = i, cnt = 0;
                        while (j < length && text[j] == qc)
                        {
                            cnt++; j++;
                        }

                        if (cnt < 3)
                        {
                            AddError(
                                "Недостаточно кавычек для открытия многострочного комментария",
                                new string(qc, 3),
                                line, tokenCol
                            );
                        }

                        // всегда берём ровно три кавычки
                        sb.Append(new string(qc, 3));
                        multiLineStack.Push((line, tokenCol, qc));

                        i += 3; col += 3;
                        continue;
                    }
                }

                // всё остальное
                sb.Append(c);
                i++; col++;
            }

            // незакрытые многострочные
            while (multiLineStack.Count > 0)
            {
                var (sLine, sCol, qc) = multiLineStack.Pop();
                int eLine = hasStrayClosingAttempt ? strayLine : sLine;
                int eCol = hasStrayClosingAttempt ? strayCol : sCol;

                AddError(
                    "Незакрытый многострочный комментарий",
                    new string(qc, 3),
                    eLine, eCol
                );
                sb.Append(new string(qc, 3));
            }

            return (sb.ToString(), Errors);
        }

        private void AddError(string message, string expected, int line, int col)
        {
            Errors.Add(new ParsingError
            {
                NumberOfError = _errorNumber++,
                Message = message,
                ExpectedToken = expected,
                Line = line,
                Column = col
            });
        }
    }
}
