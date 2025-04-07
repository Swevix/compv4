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

        /// <summary>
        /// Анализирует текст и возвращает список ошибок (без исправления текста).
        /// </summary>
        public List<ParsingError> Parse(string text)
        {
            // Можно оставить старую логику для отчёта ошибок,
            // если требуется отдельно выводить ошибки без исправления.
            // Здесь вызовем Correct, но проигнорируем исправленный текст.
            Correct(text);
            return Errors;
        }

        /// <summary>
        /// Выполняет нейтрализацию синтаксических ошибок (метод Айронса) 
        /// с автоматическим исправлением: если открывающий или закрывающий токен 
        /// многострочного комментария имеет неверное количество кавычек,
        /// недостающие кавычки добавляются, лишние – отброшены.
        /// Возвращает кортеж: исправленный текст и список ошибок.
        /// </summary>
        public (string correctedText, List<ParsingError> errors) Correct(string text)
        {
            Errors.Clear();
            _errorNumber = 1;

            int i = 0;
            int line = 1;
            int col = 1;
            int length = text.Length;
            StringBuilder sb = new StringBuilder();

            // Стек для отслеживания начала многострочных комментариев:
            // (startLine, startCol, quoteChar, outputIndex)
            var multiLineStack = new Stack<(int startLine, int startCol, char quoteChar, int outputIndex)>();

            while (i < length)
            {
                char current = text[i];

                // Обработка перевода строки
                if (current == '\n')
                {
                    sb.Append('\n');
                    line++;
                    col = 1;
                    i++;
                    continue;
                }

                // Однострочный комментарий: начинается с '#' – копируем до конца строки
                if (current == '#')
                {
                    sb.Append(current);
                    i++;
                    col++;
                    while (i < length && text[i] != '\n')
                    {
                        sb.Append(text[i]);
                        i++;
                        col++;
                    }
                    continue;
                }

                // Обработка кавычек (одинарная или двойная)
                if (current == '\'' || current == '"')
                {
                    char quoteChar = current;

                    // Если мы уже внутри многострочного комментария с таким же типом кавычки
                    if (multiLineStack.Count > 0 && multiLineStack.Peek().quoteChar == quoteChar)
                    {
                        int tokenStartCol = col;
                        int count = 0;
                        int j = i;
                        while (j < length && text[j] == quoteChar)
                        {
                            count++;
                            j++;
                        }
                        // Если закрывающий токен имеет неверное количество кавычек
                        if (count < 3)
                        {
                            AddError("Недостаточно кавычек для закрытия многострочного комментария",
                                     new string(quoteChar, 3), line, tokenStartCol);
                            // Добавляем недостающие кавычки – итог ровно 3
                            sb.Append(new string(quoteChar, 3));
                        }
                        else if (count > 3)
                        {
                            AddError("Лишние кавычки в закрывающем токене многострочного комментария",
                                     new string(quoteChar, 3), line, tokenStartCol);
                            // Добавляем ровно 3 кавычки
                            sb.Append(new string(quoteChar, 3));
                        }
                        else // count == 3
                        {
                            sb.Append(new string(quoteChar, 3));
                        }
                        multiLineStack.Pop();
                        i = j;
                        col += count;
                        continue;
                    }
                    else // Не внутри многострочного комментария – попытка открыть новый
                    {
                        int tokenStartCol = col;
                        int count = 0;
                        int j = i;
                        while (j < length && text[j] == quoteChar)
                        {
                            count++;
                            j++;
                        }
                        if (count < 3)
                        {
                            AddError("Недостаточно кавычек для открытия многострочного комментария",
                                     new string(quoteChar, 3), line, tokenStartCol);
                            // Добавляем недостающие кавычки, чтобы получилось ровно 3
                            count = 3;
                            sb.Append(new string(quoteChar, count));
                        }
                        else if (count > 3)
                        {
                            AddError("Лишние кавычки в открывающем токене многострочного комментария",
                                     new string(quoteChar, 3), line, tokenStartCol);
                            // Добавляем ровно 3 кавычки
                            count = 3;
                            sb.Append(new string(quoteChar, count));
                        }
                        else
                        {
                            sb.Append(new string(quoteChar, 3));
                        }
                        // Запоминаем открытие комментария с позицией в исходном тексте и в выводе
                        multiLineStack.Push((line, tokenStartCol, quoteChar, sb.Length - 3));
                        i = j;
                        col += count;
                        continue;
                    }
                }

                // Для всех остальных символов просто копируем
                sb.Append(current);
                i++;
                col++;
            }

            // Если остались незакрытые многострочные комментарии, добавляем закрывающий токен
            while (multiLineStack.Count > 0)
            {
                var (startLine, startCol, quoteChar, outputIndex) = multiLineStack.Pop();
                AddError("Незакрытый многострочный комментарий", new string(quoteChar, 3), startLine, startCol);
                sb.Append(new string(quoteChar, 3));
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
