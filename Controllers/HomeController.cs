using Microsoft.AspNetCore.Mvc;
using Syncfusion.OCRProcessor;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using Syncfusion_OCR_Docker.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
            _logger.LogInformation("Index page requested");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Error page requested with RequestID: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }

        [HttpPost]
        public async Task<ActionResult> AddTextLayer(IFormFile file)
        {
            _logger.LogInformation("AddTextLayer method called");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload attempt with empty file");
                return BadRequest("Please upload a file");
            }

            // Check if file is a PDF
            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid file type uploaded: {ContentType}", file.ContentType);
                return BadRequest("Please upload a PDF file");
            }

            _logger.LogInformation("Processing PDF file: {FileName}, Size: {FileSize} bytes", file.FileName, file.Length);

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
                        PdfLoadedDocument lDoc = new PdfLoadedDocument(ms);                        // Check if the PDF already has a text layer by attempting to extract text from the first page
                        bool hasTextLayer = false;
                        _logger.LogInformation("PDF document loaded successfully with {PageCount} pages", lDoc.Pages.Count);

                        if (lDoc.Pages.Count > 0)
                        {
                            // Extract text from the first page - if it returns content, it likely has a text layer
                            // TODO: Consider checking all pages or a more robust method to determine if a text layer exists
                            // or just remove this check if you want to always perform OCR
                            string extractedText = lDoc.Pages[0].ExtractText();
                            hasTextLayer = !string.IsNullOrWhiteSpace(extractedText);
                            _logger.LogInformation("Text layer check completed. Document has text layer: {HasTextLayer}", hasTextLayer);
                        }
                        if (hasTextLayer)
                        {
                            _logger.LogInformation("Document already has a text layer, returning it without OCR processing");
                            MemoryStream resultStream = new MemoryStream();
                            lDoc.Save(resultStream);
                            lDoc.Close();

                            // Reset stream position
                            resultStream.Position = 0;

                            string fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_WithTextLayer.pdf";
                            _logger.LogInformation("Returning processed file: {FileName}", fileName);
                            return File(resultStream, "application/pdf", fileName);
                        }
                        else
                        {
                            _logger.LogInformation("Document does not have a text layer, proceeding with OCR");
                            string tesseractPath = GetTesseractPath(); // Get the Tesseract path based on the OS
                            _logger.LogInformation("Using Tesseract path: {TesseractPath}", tesseractPath);

                            // PDF doesn't have text layer, perform OCR
                            using (OCRProcessor processor = new OCRProcessor())
                            {
                                var languages = Languages.Polish + '+' + Languages.English;
                                processor.Settings.Language = languages;
                                string tessDataPath = GetTessDataPath();
                                processor.TessDataPath = tessDataPath;
                                _logger.LogInformation("OCR processor configured with languages: {Languages}, TessData path: {TessDataPath}",
                                    languages, tessDataPath);

                                // Process OCR
                                _logger.LogInformation("Starting OCR processing");
                                var startTime = DateTime.Now;
                                var text = processor.PerformOCR(lDoc);
                                var duration = DateTime.Now - startTime;
                                _logger.LogInformation("OCR processing completed in {Duration} milliseconds", duration.TotalMilliseconds);

                                // Save the processed document
                                MemoryStream resultStream = new MemoryStream();
                                lDoc.Save(resultStream);
                                lDoc.Close();

                                // Reset stream position
                                resultStream.Position = 0;                                // Return the processed file for download
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_WithTextLayer.pdf";
                                _logger.LogInformation("OCR completed successfully, returning file: {FileName}", fileName);
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
            _logger.LogInformation("ProcessImageToOcrPdf method called");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload attempt with empty file");
                return BadRequest("Please upload a file");
            }

            // Check if file is an image
            if (!file.ContentType.StartsWith("image/"))
            {
                _logger.LogWarning("Invalid file type uploaded: {ContentType}", file.ContentType);
                return BadRequest("Please upload an image file");
            }

            _logger.LogInformation("Processing image file: {FileName}, Size: {FileSize} bytes, Type: {ContentType}",
                file.FileName, file.Length, file.ContentType);

            try
            {
                // Create a memory stream from the uploaded image file
                using (MemoryStream imageStream = new MemoryStream())
                {
                    await file.CopyToAsync(imageStream);
                    imageStream.Position = 0; _logger.LogInformation("Converting image to PDF");
                    //Create a new PDF document.
                    PdfDocument document = new PdfDocument();
                    //Add a page to the document.
                    PdfPage page = document.Pages.Add();
                    //Create PDF graphics for a page.
                    PdfGraphics graphics = page.Graphics;
                    //Load the image from the disk.
                    PdfBitmap image = new PdfBitmap(imageStream);
                    _logger.LogInformation("Image loaded with dimensions: {Width}x{Height}", image.Width, image.Height);

                    //Draw the image.
                    graphics.DrawImage(image, 0, 0, page.GetClientSize().Width, page.GetClientSize().Height);
                    _logger.LogInformation("Image drawn on PDF page with size: {Width}x{Height}",
                        page.GetClientSize().Width, page.GetClientSize().Height);

                    //Save the document into the stream.
                    MemoryStream stream = new MemoryStream();
                    document.Save(stream);
                    _logger.LogInformation("Image converted to PDF successfully");                    //Initialize the OCR processor.
                    string tesseractPath = GetTesseractPath(); // Get the Tesseract path based on the OS
                    _logger.LogInformation("Using Tesseract path: {TesseractPath}", tesseractPath);

                    using (OCRProcessor processor = new OCRProcessor())
                    {
                        _logger.LogInformation("OCR processor initialized");

                        //Load a PDF document.
                        PdfLoadedDocument lDoc = new PdfLoadedDocument(stream);
                        _logger.LogInformation("PDF loaded for OCR processing with {PageCount} pages", lDoc.Pages.Count);

                        var languages = Languages.Polish + '+' + Languages.English;
                        processor.Settings.Language = languages;
                        string tessDataPath = GetTessDataPath();
                        processor.TessDataPath = tessDataPath;
                        _logger.LogInformation("OCR processor configured with languages: {Languages}, TessData path: {TessDataPath}",
                            languages, tessDataPath);

                        //Process OCR by providing the PDF document.
                        _logger.LogInformation("Starting OCR processing of image-based PDF");
                        var startTime = DateTime.Now;
                        processor.PerformOCR(lDoc);
                        var duration = DateTime.Now - startTime;
                        _logger.LogInformation("OCR processing completed in {Duration} milliseconds", duration.TotalMilliseconds);


                        // Save the processed document
                        MemoryStream resultStream = new MemoryStream();
                        lDoc.Save(resultStream);
                        lDoc.Close();

                        // Reset stream position
                        resultStream.Position = 0;                        // Return the processed file for download
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_OCR_PDF.pdf";
                        _logger.LogInformation("Image to OCR PDF conversion completed successfully, returning file: {FileName}", fileName);
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
        private string GetTessDataPath()
        {
            string path;
            string platform;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = "Windows";
                path = Path.Combine(_hostingEnvironment.WebRootPath, "Data", "tessdata");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = "Linux";
                path = Path.Combine(_hostingEnvironment.WebRootPath, "Data", "tessdata");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "MacOS";
                path = Path.Combine(_hostingEnvironment.WebRootPath, "Data", "tessdata");
            }
            else
            {
                platform = "Unknown";
                _logger.LogError("Unsupported platform detected");
                throw new PlatformNotSupportedException("The current platform is not supported.");
            }

            _logger.LogInformation("GetTessDataPath: Platform {Platform}, Path {Path}", platform, path);
            return path;
        }
        private string GetTesseractPath()
        {
            string path;
            string platform;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = "Windows";
                path = Path.Combine(_hostingEnvironment.WebRootPath, "Data", "Tesseractbinaries", "Windows");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = "Linux";
                path = Path.Combine(_hostingEnvironment.WebRootPath, "Data", "Tesseractbinaries", "Linux");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "MacOS";
                path = Path.Combine(_hostingEnvironment.WebRootPath, "Data", "Tesseractbinaries", "Mac");
            }
            else
            {
                platform = "Unknown";
                _logger.LogError("Unsupported platform detected when determining Tesseract path");
                throw new PlatformNotSupportedException("The current platform is not supported.");
            }

            _logger.LogInformation("GetTesseractPath: Platform {Platform}, Path {Path}", platform, path);

            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Tesseract path does not exist: {Path}", path);
            }

            return path;
        }
    }
}