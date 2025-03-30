using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using veterans_site.Models;
using veterans_site.Extensions;
using iText.Layout.Borders;
using iText.IO.Font;

namespace veterans_site.Services
{
    public class PDFService : IPDFService
    {
        private readonly ILogger<PDFService> _logger;
        private readonly IWebHostEnvironment _environment;

        public PDFService(ILogger<PDFService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public byte[] GenerateConsultationConfirmation(Consultation consultation, ApplicationUser user)
        {
            using var memoryStream = new MemoryStream();

            try
            {
                var writerProperties = new WriterProperties()
                    .SetPdfVersion(PdfVersion.PDF_2_0);

                var writer = new PdfWriter(memoryStream, writerProperties);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4);

                string regularFontPath = System.IO.Path.Combine(_environment.WebRootPath, "fonts", "Roboto-Regular.ttf");
                string boldFontPath = System.IO.Path.Combine(_environment.WebRootPath, "fonts", "Roboto-Bold.ttf");

                var font = PdfFontFactory.CreateFont("wwwroot/fonts/Arial.ttf", PdfEncodings.IDENTITY_H);
                var boldFont = PdfFontFactory.CreateFont("wwwroot/fonts/Arialbd.ttf", PdfEncodings.IDENTITY_H);

                document.Add(new Paragraph("Запис на консультацію")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20)
                    .SetFontColor(ColorConstants.BLUE));

                var mainInfoTable = new Table(1)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20)
                    .SetBorder(new SolidBorder(1));

                var headerCell = new Cell()
                    .SetBackgroundColor(new DeviceRgb(242, 242, 242))
                    .SetPadding(10)
                    .Add(new Paragraph("Інформація про консультацію")
                        .SetFont(boldFont)
                        .SetFontSize(14));

                mainInfoTable.AddCell(headerCell);

                var infoTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100));


                AddInfoRow(infoTable, "Назва:", consultation.Title, font, boldFont);
                AddInfoRow(infoTable, "Дата та час:", consultation.DateTime.ToString("dd.MM.yyyy HH:mm"), font, boldFont);
                AddInfoRow(infoTable, "Тривалість:", $"{consultation.Duration} хвилин", font, boldFont);
                AddInfoRow(infoTable, "Спеціаліст:", consultation.SpecialistName, font, boldFont);
                AddInfoRow(infoTable, "Тип:", consultation.Type.GetDisplayName(), font, boldFont);
                AddInfoRow(infoTable, "Формат:", consultation.Format.GetDisplayName(), font, boldFont);
                AddInfoRow(infoTable, "Формат проведення:", consultation.Mode.GetDisplayName(), font, boldFont);

                if (consultation.Mode == ConsultationMode.Offline)
                {
                    AddInfoRow(infoTable, "Місце проведення:", consultation.Location, font, boldFont);
                }

                var contentCell = new Cell()
                    .SetPadding(10)
                    .Add(infoTable);

                mainInfoTable.AddCell(contentCell);
                document.Add(mainInfoTable);

                var userInfoTable = new Table(1)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20)
                    .SetBorder(new SolidBorder(1));

                var userHeaderCell = new Cell()
                    .SetBackgroundColor(new DeviceRgb(242, 242, 242))
                    .SetPadding(10)
                    .Add(new Paragraph("Інформація про ветерана")
                        .SetFont(boldFont)
                        .SetFontSize(14));

                userInfoTable.AddCell(userHeaderCell);

                var userDetailsTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100));

                AddInfoRow(userDetailsTable, "Ім'я та прізвище:", $"{user.FirstName} {user.LastName}", font, boldFont);
                AddInfoRow(userDetailsTable, "Email:", user.Email, font, boldFont);

                userInfoTable.AddCell(new Cell().SetPadding(10).Add(userDetailsTable));
                document.Add(userInfoTable);

                var notesTable = new Table(1)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginTop(20);

                var notesCell = new Cell()
                    .SetBackgroundColor(new DeviceRgb(242, 247, 255))
                    .SetPadding(10)
                    .Add(new Paragraph("Важлива інформація")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetMarginBottom(10))
                    .Add(new Paragraph()
                        .SetFont(font)
                        .Add("• Будь ласка, прийдіть за 5-10 хвилин до початку консультації\n")
                        .Add("• Майте при собі документ, що посвідчує особу\n")
                        .Add("• У разі неможливості відвідати консультацію, повідомте про це заздалегідь"));

                notesTable.AddCell(notesCell);
                document.Add(notesTable);

                var footer = new Paragraph()
                    .SetFont(font)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginTop(20)
                    .Add($"Документ згенеровано: {DateTime.Now:dd.MM.yyyy HH:mm}\n");

                document.Add(footer);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating PDF: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void AddInfoRow(Table table, string label, string value, PdfFont regularFont, PdfFont boldFont)
        {
            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetFont(boldFont)
                .SetFontSize(11)
                .Add(new Paragraph(label)));

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetFont(regularFont)
                .SetFontSize(11)
                .Add(new Paragraph(value)));
        }
    }
}
