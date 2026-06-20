using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using ResumeTemplateService.Application.Interfaces;

namespace ResumeTemplateService.Infrastructure.TemplateRendering;

public class ChromiumPdfRenderer : IPdfRenderer
{
    private static readonly string[] CandidateExecutablePaths =
    {
        "/usr/bin/chromium",
        "/usr/bin/chromium-browser",
        "/usr/bin/google-chrome",
        "/usr/bin/google-chrome-stable",
        "chromium",
        "chromium-browser",
        "google-chrome",
        "msedge"
    };

    private readonly string? _configuredExecutablePath;
    private readonly ILogger<ChromiumPdfRenderer> _logger;

    public ChromiumPdfRenderer(string? configuredExecutablePath, ILogger<ChromiumPdfRenderer> logger)
    {
        _configuredExecutablePath = configuredExecutablePath;
        _logger = logger;
    }

    public async Task<byte[]> RenderPdfAsync(string html, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("Cannot render an empty HTML document as PDF.");
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"resume-pdf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        var htmlPath = Path.Combine(tempDirectory, "resume.html");
        var pdfPath = Path.Combine(tempDirectory, "resume.pdf");

        try
        {
            await File.WriteAllTextAsync(htmlPath, EnsureHtmlDocument(html), Encoding.UTF8, cancellationToken);

            var executablePath = ResolveExecutablePath();
            var arguments = string.Join(" ", new[]
            {
                "--headless=new",
                "--disable-gpu",
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--run-all-compositor-stages-before-draw",
                "--virtual-time-budget=1000",
                "--no-pdf-header-footer",
                "--print-to-pdf-no-header",
                $"--print-to-pdf=\"{pdfPath}\"",
                $"\"file:///{htmlPath.Replace("\\", "/")}\""
            });

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _logger.LogInformation("Generating PDF with Chromium executable: {ExecutablePath}", executablePath);
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0 || !File.Exists(pdfPath))
            {
                throw new InvalidOperationException(
                    $"Chromium PDF generation failed with exit code {process.ExitCode}. {stderr} {stdout}".Trim());
            }

            var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
            if (pdfBytes.Length == 0)
            {
                throw new InvalidOperationException("Chromium generated an empty PDF.");
            }

            return pdfBytes;
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to delete temporary PDF directory: {TempDirectory}", tempDirectory);
            }
        }
    }

    private string ResolveExecutablePath()
    {
        if (!string.IsNullOrWhiteSpace(_configuredExecutablePath))
        {
            return _configuredExecutablePath;
        }

        foreach (var path in CandidateExecutablePaths)
        {
            if (Path.IsPathRooted(path) && File.Exists(path))
            {
                return path;
            }

            if (!Path.IsPathRooted(path))
            {
                return path;
            }
        }

        throw new InvalidOperationException("No Chromium-compatible browser executable was found for PDF generation.");
    }

    private static string EnsureHtmlDocument(string html)
    {
        if (html.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }

        return $$"""
            <!doctype html>
            <html>
              <head>
                <meta charset="utf-8">
                <style>
                  @page { size: A4; margin: 30pt 34pt; }
                  html, body { margin: 0; background: #ffffff; }
                  * { box-sizing: border-box; }
                  body {
                    -webkit-print-color-adjust: exact;
                    print-color-adjust: exact;
                  }
                  body > div:first-child {
                    background: #ffffff !important;
                    padding: 0 !important;
                  }
                  body > div:first-child > div:first-child {
                    max-width: none !important;
                    width: 100% !important;
                    margin: 0 !important;
                    box-shadow: none !important;
                    border: 0 !important;
                    overflow: visible !important;
                  }
                  .resume-section-title {
                    break-after: avoid;
                    page-break-after: avoid;
                    break-inside: avoid;
                    page-break-inside: avoid;
                  }
                  .resume-item {
                    break-inside: auto;
                    page-break-inside: auto;
                  }
                  .resume-item-lead {
                    break-inside: avoid;
                    page-break-inside: avoid;
                  }
                  .resume-line {
                    break-inside: avoid;
                    page-break-inside: avoid;
                  }
                </style>
              </head>
              <body>
                {{html}}
              </body>
            </html>
            """;
    }
}
