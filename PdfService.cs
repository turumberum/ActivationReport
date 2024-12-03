namespace ActivationReport
{
    using iText.Kernel.Font;
    using iText.Kernel.Pdf;
    using iText.Layout;
    using iText.Layout.Element;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class PdfService
    {
        /// <summary>
        /// Создает PDF-файл с содержимым из списка строк.
        /// </summary>
        /// <param name="lines">Список строк для добавления в PDF.</param>
        /// <param name="outputPath">Путь для сохранения PDF-файла.</param>
        public void CreatePdfFromList(List<string> lines, string outputPath)
        {
            try
            {
                // Создаем поток для записи файла
                using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    // Создаем PDF-документ
                    PdfWriter writer = new PdfWriter(stream);
                    PdfDocument pdf = new PdfDocument(writer);
                    Document document = new Document(pdf);

                    // строчки кода где используется шрифт поддерживающий русский язык
                    var font = PdfFontFactory.CreateFont("C:\\Windows\\Fonts\\arial.ttf", "Identity-H");
                    document.SetFont(font);

                    // Добавляем строки в документ
                    foreach (string line in lines)
                    {
                        document.Add(new Paragraph(line));
                        //document.Add(new AreaBreak()); // Разрыв страницы
                    }

                    // Закрываем документ
                    document.Close();
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("Файл не создан или занят другим приложением");
            }
        }
    }
}
