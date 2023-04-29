using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using static UnicodeCharsetDetector.CharsetDetector;
using static System.Collections.Specialized.BitVector32;
using UnicodeCharsetDetector;

namespace MergeTxtFilesAndConvertToFB2
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine("Введите путь к каталогу с текстовыми файлами:");
            string inputPath = Console.ReadLine();
            Console.WriteLine("Введите путь и имя выходного файла:");
            string outputPath = Console.ReadLine();
            Console.WriteLine("Введите название книги:");
            string bookTitle = Console.ReadLine();
            Console.WriteLine("Введите имя автора:");
            string authorName = Console.ReadLine();

            try
            {
                string mergedText = MergeTextFiles(inputPath);
                CreateFb2File(mergedText, outputPath, bookTitle, authorName);
                Console.WriteLine("Файлы успешно объединены и преобразованы в формат FB2.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Произошла ошибка: {e.Message}");
            }
        }

        static string MergeTextFiles(string inputPath)
        {
            if (!Directory.Exists(inputPath))
            {
                throw new DirectoryNotFoundException("Каталог не найден.");
            }

            string[] textFiles = Directory.GetFiles(inputPath, "*.txt");

            if (textFiles.Length == 0)
            {
                throw new FileNotFoundException("Текстовые файлы не найдены.");
            }

            using (var sw = new StringWriter())
            {
                foreach (string file in textFiles)
                {
                    string content = ReadFileWithDetectedEncoding(file);
                    sw.Write(content);
                    sw.Write(Environment.NewLine);
                }

                return sw.ToString();
            }
        }

        static string ReadFileWithDetectedEncoding(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[4096];
                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                fs.Position = 0;

                Encoding encoding = DetectEncoding(buffer, bytesRead);
                using (var reader = new StreamReader(fs, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        static Encoding DetectEncoding(byte[] buffer, int bytesRead)
        {
            if (bytesRead > 2 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            {
                return Encoding.UTF8;
            }
            else if (bytesRead > 1 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode;
            }
            else if (bytesRead > 1 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            {
                return Encoding.Unicode;
            }
            else if (bytesRead > 3 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
            {
                return Encoding.UTF32;
            }
            else if (bytesRead > 3 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
            {
                return Encoding.GetEncoding("utf-32");
            }
            else
            {
                // Если есть нулевые байты, считаем, что это Unicode, иначе - кодировка по умолчанию (обычно это UTF-8 или Windows-1252)
                return buffer.Take(bytesRead).Any(b => b == 0) ? Encoding.Unicode : Encoding.Default;
            }
        }
        static void CreateFb2File(string text, string outputPath, string bookTitle, string authorName)
        {
            XNamespace xmlns = "http://www.gribuser.ru/xml/fictionbook/2.0";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace l = "http://www.w3.org/1999/xlink";

            var firstName = authorName.Split(' ')[0];
            var lastName = authorName.Contains(' ') ? authorName.Split(' ')[1] : "";

            var description = new XElement(xmlns + "description",
                new XElement(xmlns + "title-info",
                    new XElement(xmlns + "author",
                        new XElement(xmlns + "first-name", firstName),
                        new XElement(xmlns + "last-name", lastName)
                    ),
                    new XElement(xmlns + "book-title", bookTitle)
                )
            );

            var body = new XElement(xmlns + "body",
                text.Split(Environment.NewLine).Select(line =>
                    new XElement(xmlns + "p", line)
                )
            );

            var fb2 = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(xmlns + "FictionBook",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "l", l.NamespaceName),
                    description,
                    body
                )
            );

            fb2.Save(outputPath);
        }
    }
}