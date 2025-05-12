using Microsoft.AspNetCore.Mvc;
using Syncfusion.OCRProcessor;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using Syncfusion_OCR_Docker.Models;
using System.Diagnostics;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

namespace Syncfusion_OCR_Docker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(IWebHostEnvironment hostingEnvironment, ILogger<HomeController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<ActionResult> AddTextLayer(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload a file");
            }

            // Check if file is a PDF
            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Please upload a PDF file");
            }

            try
            {
                // Create a memory stream from the uploaded file
                using (MemoryStream ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    ms.Position = 0;

                    try
                    {
                        // Attempt to load the file as a PDF
                        PdfLoadedDocument lDoc = new PdfLoadedDocument(ms);

                        // Check if the PDF already has a text layer by attempting to extract text from the first page
                        bool hasTextLayer = false;
                        if (lDoc.Pages.Count > 0)
                        {
                            // Extract text from the first page - if it returns content, it likely has a text layer
                            // This is a simple check - a more robust solution would check all pages
                            string extractedText = lDoc.Pages[0].ExtractText();
                            hasTextLayer = !string.IsNullOrWhiteSpace(extractedText);
                        }

                        if (hasTextLayer)
                        {
                            MemoryStream resultStream = new MemoryStream();
                            lDoc.Save(resultStream);
                            lDoc.Close();

                            // Reset stream position
                            resultStream.Position = 0;

                            string fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_WithTextLayer.pdf";
                            return File(resultStream, "application/pdf", fileName);
                        }
                        else
                        {
                            // PDF doesn't have text layer, perform OCR
                            using (OCRProcessor processor = new OCRProcessor())
                            {
                                processor.Settings.Language = Languages.Polish + '+' + Languages.English;
                                processor.TessDataPath = "/app/wwwroot/Data/tessdata";

                                // Process OCR
                                var text = processor.PerformOCR(lDoc);

                                // Save the processed document
                                MemoryStream resultStream = new MemoryStream();
                                lDoc.Save(resultStream);
                                lDoc.Close();

                                // Reset stream position
                                resultStream.Position = 0;

                                // Return the processed file for download
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_WithTextLayer.pdf";
                                return File(resultStream, "application/pdf", fileName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading PDF document: {Error}", ex.Message);
                        // If we can't load it as a PDF, it's not a valid PDF file
                        return BadRequest("The uploaded file is not a valid PDF");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file for text layer addition");
                return StatusCode(500, "An error occurred while processing your file");
            }
        }
        [HttpPost]
        public async Task<ActionResult> ProcessImageToOcrPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload a file");
            }

            // Check if file is an image
            if (!file.ContentType.StartsWith("image/"))
            {
                return BadRequest("Please upload an image file");
            }

            try
            {
                // Create a memory stream from the uploaded image file
                using (MemoryStream imageStream = new MemoryStream())
                {
                    await file.CopyToAsync(imageStream);
                    imageStream.Position = 0;
                    //Create a new PDF document.
                    PdfDocument document = new PdfDocument();
                    //Add a page to the document.
                    PdfPage page = document.Pages.Add();
                    //Create PDF graphics for a page.
                    PdfGraphics graphics = page.Graphics;
                    //Load the image from the disk.
                    PdfBitmap image = new PdfBitmap(imageStream);
                    //Draw the image.
                    graphics.DrawImage(image, 0, 0, page.GetClientSize().Width, page.GetClientSize().Height);
                    //Save the document into the stream.
                    MemoryStream stream = new MemoryStream();
                    document.Save(stream);
                    //Initialize the OCR processor.
                    using (OCRProcessor processor = new OCRProcessor())
                    {
                        //Load a PDF document.
                        PdfLoadedDocument lDoc = new PdfLoadedDocument(stream);
                        //Set OCR language to process.
                        processor.Settings.Language = Languages.Polish + '+' + Languages.English;
                        //Process OCR by providing the PDF document.
                        processor.PerformOCR(lDoc);


                        // Save the processed document
                        MemoryStream resultStream = new MemoryStream();
                        lDoc.Save(resultStream);
                        lDoc.Close();

                        // Reset stream position
                        resultStream.Position = 0;

                        // Return the processed file for download
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_OCR_PDF.pdf";
                        return File(resultStream, "application/pdf", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image file for OCR: {Error}", ex.Message);
                return StatusCode(500, "An error occurred while processing your file");
            }
        }
    }
}