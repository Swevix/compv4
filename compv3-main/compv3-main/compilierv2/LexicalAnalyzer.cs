using System;
using System.Collections.Generic;

namespace lab1_compiler.Bar
{
    public class LexicalToken
    {
        public int Code { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string Position { get; set; }
    }

    internal class LexicalAnalyzer
    {
        private readonly Dictionary<string, int> _tokenTypes = new Dictionary<string, int>
    {
        { "SingleLineCommentStart", 1 },
        { "MultiLineCommentStart", 2 },
        { "MultiLineCommentEnd", 3 },
        { "CommentText", 4 },
        { "Error", 5 }
    };

        public List<LexicalToken> Tokens { get; } = new List<LexicalToken>();
        public List<string> Errors { get; } = new List<string>();

        public void Analyze(string text)
        {
            Tokens.Clear();
            Errors.Clear();

            int i = 0;
            int line = 1;
            int col = 1;
            int length = text.Length;

            while (i < length)
            {
                char current = text[i];

                // Переход на новую строку
                if (current == '\n')
                {
                    line++;
                    col = 1;
                    i++;
                    continue;
                }

                // Однострочный комментарий Python: #
                if (current == '#')
                {
                    int startLine = line, startCol = col;
                    Tokens.Add(new LexicalToken
                    {
                        Code = _tokenTypes["SingleLineCommentStart"],
                        Type = "Начало однострочного комментария",
                        Value = "#",
                        Position = $"Строка {startLine}, Позиция {startCol}"
                    });

                    i++;
                    col++;
                    int commentStart = i;
                    // Считываем до конца строки или до конца файла
                    while (i < length && text[i] != '\n')
                    {
                        i++;
                        col++;
                    }
                    string commentText = text.Substring(commentStart, i - commentStart).Trim();
                    Tokens.Add(new LexicalToken
                    {
                        Code = _tokenTypes["CommentText"],
                        Type = "Текст комментария",
                        Value = commentText,
                        Position = $"Строка {startLine}, Позиция {startCol + 1}"
                    });
                    continue;
                }

                // Многострочный комментарий Python: """ или '''
                if ((current == '"' || current == '\'') &&
                    (i + 2 < length) &&
                    text[i + 1] == current &&
                    text[i + 2] == current)
                {
                    char quoteType = current;
                    string tripleQuote = new string(quoteType, 3);
                    int startLine = line, startCol = col;

                    Tokens.Add(new LexicalToken
                    {
                        Code = _tokenTypes["MultiLineCommentStart"],
                        Type = "Начало многострочного комментария",
                        Value = tripleQuote,
                        Position = $"Строка {startLine}, Позиция {startCol}"
                    });

                    i += 3;
                    col += 3;
                    int commentStart = i;
                    bool endFound = false;

                    while (i + 2 < length)
                    {
                        if (text[i] == '\n')
                        {
                            line++;
                            col = 1;
                            i++;
                            continue;
                        }

                        if (text[i] == quoteType &&
                            text[i + 1] == quoteType &&
                            text[i + 2] == quoteType)
                        {
                            endFound = true;
                            break;
                        }

                        i++;
                        col++;
                    }

                    string multiText = text.Substring(commentStart, i - commentStart).Trim();

                    Tokens.Add(new LexicalToken
                    {
                        Code = _tokenTypes["CommentText"],
                        Type = "Текст комментария",
                        Value = multiText,
                        Position = $"Строка {startLine}, Позиция {startCol + 3}"
                    });

                    if (endFound)
                    {
                        Tokens.Add(new LexicalToken
                        {
                            Code = _tokenTypes["MultiLineCommentEnd"],
                            Type = "Конец многострочного комментария",
                            Value = tripleQuote,
                            Position = $"Строка {line}, Позиция {col}"
                        });

                        i += 3;
                        col += 3;
                    }
                    else
                    {
                        Errors.Add($"Не найден конец многострочного комментария, начатого на строке {startLine}, позиция {startCol}");
                    }
                    continue;
                }
                i++;
                col++;

            }
        }
    }
}