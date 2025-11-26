using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using LTU_U15.Services;

namespace LTU_U15.Controllers
{
    [PluginController("WebinarImport")]
    [Route("umbraco/backoffice/api/[controller]")]
    public class WebinarImportController : UmbracoApiController
    {
        private readonly WebinarImportService _importService;
        private readonly ILogger<WebinarImportController> _logger;

        public WebinarImportController(
            WebinarImportService importService,
            ILogger<WebinarImportController> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        [HttpPost("CreateDocumentType")]
        public async Task<IActionResult> CreateDocumentType()
        {
            try
            {
                var result = await _importService.CreateWebinarDocumentType();
                if (result)
                {
                    return Ok(new { success = true, message = "Document type created successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to create document type" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document type");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ImportWebinars")]
        public async Task<IActionResult> ImportWebinars([FromBody] ImportRequest request)
        {
            try
            {
                // Parse CSV content
                var webinars = CsvImportHelper.ParseCsvData(request.CsvContent);

                _logger.LogInformation($"Parsed {webinars.Count} webinars from CSV");

                // Import webinars
                var result = await _importService.ImportWebinars(webinars, request.ParentNodeId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Successfully imported {webinars.Count} webinars",
                        count = webinars.Count
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to import webinars" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing webinars");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("ParseCsv")]
        public IActionResult ParseCsv([FromQuery] string csvContent)
        {
            try
            {
                var webinars = CsvImportHelper.ParseCsvData(csvContent);
                return Ok(new
                {
                    success = true,
                    data = webinars,
                    count = webinars.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CSV");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class ImportRequest
    {
        public string CsvContent { get; set; }
        public int ParentNodeId { get; set; } = -1;
    }
}