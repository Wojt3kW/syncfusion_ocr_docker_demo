# Syncfusion OCR Docker Demo

This is an ASP.NET Core web application that demonstrates Optical Character Recognition (OCR) capabilities using Syncfusion's PDF OCR library in a Docker container.

## Features

- **Add Text Layer to PDF**: Upload scanned PDF documents and add a searchable text layer using OCR technology.
- **Convert Images to Searchable PDFs**: Upload image files (.jpg, .png) and convert them into searchable PDF documents with OCR-processed text layers.
- **Multi-language OCR Support**: Process documents with both English and Polish language recognition.
- **Containerized Application**: Runs in a Docker container with all necessary dependencies.

## Technologies Used

- **ASP.NET Core 8.0**: Web application framework
- **Syncfusion.PDF.OCR.NET**: For OCR processing and PDF manipulation
- **Docker**: For containerized deployment
- **Tesseract OCR**: The underlying OCR engine (via Syncfusion's wrapper)

## Prerequisites

- [Docker](https://www.docker.com/products/docker-desktop)
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (for development)
- [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) (for development)

## Getting Started

### Running with Docker

1. Clone this repository
2. Navigate to the project directory
3. Build and run the Docker container:

```powershell
docker build -t syncfusion-ocr-app .
docker run -p 8080:80 syncfusion-ocr-app
```

4. Open your browser and navigate to `http://localhost:8080`

### Running Locally (Development)

1. Clone this repository
2. Navigate to the project directory
3. Restore dependencies and run the application:

```powershell
dotnet restore
dotnet run
```

4. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## Usage

### Adding a Text Layer to a PDF

1. Navigate to the "Add Text Layer to PDF" section
2. Click "Choose File" and select a PDF document
3. Click "Add Text Layer" button
4. The processed PDF with a searchable text layer will be downloaded automatically

### Converting an Image to a Searchable PDF

1. Navigate to the "Convert image to PDF with Text Layer" section
2. Click "Choose File" and select an image file (.jpg or .png)
3. Click "Process image" button
4. The generated searchable PDF will be downloaded automatically

## Project Structure

- **Controllers/**
  - `HomeController.cs`: Contains the main application logic for OCR processing
- **Models/**
  - `ErrorViewModel.cs`: Model for error handling
- **Views/**
  - `Home/Index.cshtml`: Main application interface
- **wwwroot/**
  - `Data/tessdata/`: Contains Tesseract language data files for OCR
  - `Data/Tesseractbinaries/`: Contains platform-specific binary files for Tesseract

## Deployment Notes

- The application is configured to use the Docker container path `/app/wwwroot/Data/tessdata` for OCR language data.
- The Dockerfile installs necessary dependencies for running Tesseract on Linux.
- The Docker configuration sets up both port 80 (HTTP) and 443 (HTTPS) for the application.

## Dependencies

- `Syncfusion.PDF.OCR.NET`: For OCR processing
- `SkiaSharp`: For image processing
- `SkiaSharp.NativeAssets.Linux.NoDependencies`: For cross-platform image support in Linux containers
- `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`: For container support in Visual Studio

## License

Ensure you have the appropriate license for using Syncfusion components in your application.

## Notes

- This application can process PDF files that don't have a text layer (scanned documents) and add a searchable text layer.
- It can also convert image files to PDF documents with searchable text.
- The OCR engine is configured to recognize both English and Polish text.
