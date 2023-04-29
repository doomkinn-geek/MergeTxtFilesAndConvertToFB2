using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MergeTxtFilesAndConvertToFB2_.Net_Framework
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
                var detector = new UniversalDetector();
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0 && !detector.IsDone())
                {
                    detector.HandleData(buffer, 0, bytesRead);
                }
                detector.DataEnd();
                fs.Position = 0;

                if (detector.Charset != null)
                {
                    var encoding = Encoding.GetEncoding(detector.Charset);
                    using (var reader = new StreamReader(fs, encoding))
                    {
                        return reader.ReadToEnd();
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Не удалось определить кодировку файла {filePath}.");
                }
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
