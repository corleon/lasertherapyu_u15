using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace LTU_U15.Services
{
    public class WebinarImportService
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IContentService _contentService;
        private readonly IDataTypeService _dataTypeService;
        private readonly ILogger<WebinarImportService> _logger;
        private readonly IShortStringHelper _shortStringHelper;

        public WebinarImportService(
            IContentTypeService contentTypeService,
            IContentService contentService,
            IDataTypeService dataTypeService,
            ILogger<WebinarImportService> logger,
            IShortStringHelper shortStringHelper)
        {
            _contentTypeService = contentTypeService;
            _contentService = contentService;
            _dataTypeService = dataTypeService;
            _logger = logger;
            _shortStringHelper = shortStringHelper;
        }

        public async Task<bool> CreateWebinarDocumentType()
        {
            //    try
            //    {
            //        // Check if document type already exists
            //        var existingDocType = _contentTypeService.Get("veterinaryWebinar");
            //        if (existingDocType != null)
            //        {
            //            _logger.LogInformation("Veterinary Webinar document type already exists");
            //            return true;
            //        }

            //        // Look for common base page types
            //        var basePage = _contentTypeService.Get("master")
            //                    ?? _contentTypeService.Get("home");

            //        // Create the document type - use -1 if no base page found
            //        var webinarDocType = new ContentType(_shortStringHelper, basePage?.Id ?? -1)
            //        {
            //            Name = "Veterinary Webinar",
            //            Alias = "veterinaryWebinar",
            //            Icon = "icon-movie-alt",
            //            Description = "Document type for veterinary webinars"
            //        };

            //        // Get data types using the built-in constants/GUIDs - more reliable approach
            //        var textboxDataType = GetDataTypeByAlias("Umbraco.Textbox") ?? GetDataTypeByAlias("Umbraco.TextBox");
            //        var textareaDataType = GetDataTypeByAlias("Umbraco.Textarea") ?? GetDataTypeByAlias("Umbraco.TextArea");
            //        var datetimeDataType = GetDataTypeByAlias("Umbraco.DateTime");
            //        var booleanDataType = GetDataTypeByAlias("Umbraco.TrueFalse");
            //        var richtextDataType = GetDataTypeByAlias("Umbraco.TinyMCE") ?? GetDataTypeByAlias("Umbraco.RichText");

            //        // Fallback to default data types if needed
            //        if (textboxDataType == null)
            //            textboxDataType = _dataTypeService.GetAll().FirstOrDefault(dt => dt.EditorAlias.Contains("Textbox") || dt.EditorAlias.Contains("TextBox"));

            //        if (textareaDataType == null)
            //            textareaDataType = _dataTypeService.GetAll().FirstOrDefault(dt => dt.EditorAlias.Contains("Textarea") || dt.EditorAlias.Contains("TextArea"));

            //        if (datetimeDataType == null)
            //            datetimeDataType = _dataTypeService.GetAll().FirstOrDefault(dt => dt.EditorAlias.Contains("DateTime"));

            //        if (booleanDataType == null)
            //            booleanDataType = _dataTypeService.GetAll().FirstOrDefault(dt => dt.EditorAlias.Contains("TrueFalse"));

            //        if (richtextDataType == null)
            //            richtextDataType = _dataTypeService.GetAll().FirstOrDefault(dt => dt.EditorAlias.Contains("TinyMCE") || dt.EditorAlias.Contains("RichText"));

            //        // Log what data types we found
            //        _logger.LogInformation($"Found data types - Textbox: {textboxDataType?.Name}, Textarea: {textareaDataType?.Name}, DateTime: {datetimeDataType?.Name}, Boolean: {booleanDataType?.Name}, RichText: {richtextDataType?.Name}");

            //        // Create property group first
            //        var generalGroup = new PropertyGroup(true)
            //        {
            //            Name = "General",
            //            Alias = "general",
            //            SortOrder = 1
            //        };

            //        // Add properties to the group
            //        var properties = new List<PropertyType>
            //{
            //    new PropertyType(_shortStringHelper, textboxDataType, "title")
            //    {
            //        Name = "Title",
            //        Description = "Webinar title",
            //        Mandatory = true,
            //        SortOrder = 1
            //    },
            //    new PropertyType(_shortStringHelper, richtextDataType, "description")
            //    {
            //        Name = "Description",
            //        Description = "Webinar description",
            //        SortOrder = 2
            //    },
            //    new PropertyType(_shortStringHelper, datetimeDataType, "date")
            //    {
            //        Name = "Date",
            //        Description = "Webinar date",
            //        SortOrder = 3
            //    },
            //    new PropertyType(_shortStringHelper, textboxDataType, "length")
            //    {
            //        Name = "Length",
            //        Description = "Webinar duration",
            //        SortOrder = 4
            //    },
            //    new PropertyType(_shortStringHelper, textareaDataType, "videoEmbedCode")
            //    {
            //        Name = "Video Embed Code",
            //        Description = "HTML embed code for video",
            //        SortOrder = 5
            //    }
            //};

            //        // Add SEO properties
            //        var seoGroup = new PropertyGroup(true)
            //        {
            //            Name = "SEO",
            //            Alias = "seo",
            //            SortOrder = 2
            //        };

            //        var seoProperties = new List<PropertyType>
            //{
            //    new PropertyType(_shortStringHelper, textboxDataType, "pageTitle")
            //    {
            //        Name = "Page Title",
            //        Description = "SEO page title",
            //        SortOrder = 1
            //    },
            //    new PropertyType(_shortStringHelper, textareaDataType, "metaDescription")
            //    {
            //        Name = "Meta Description",
            //        Description = "SEO meta description",
            //        SortOrder = 2
            //    },
            //    new PropertyType(_shortStringHelper, textboxDataType, "facebookTitle")
            //    {
            //        Name = "Facebook Title",
            //        Description = "Facebook share title",
            //        SortOrder = 3
            //    },
            //    new PropertyType(_shortStringHelper, textareaDataType, "facebookDescription")
            //    {
            //        Name = "Facebook Description",
            //        Description = "Facebook share description",
            //        SortOrder = 4
            //    }
            //};

            //        // Add settings properties
            //        var settingsGroup = new PropertyGroup(true)
            //        {
            //            Name = "Settings",
            //            Alias = "settings",
            //            SortOrder = 3
            //        };

            //        var settingsProperties = new List<PropertyType>
            //{
            //    new PropertyType(_shortStringHelper, booleanDataType, "hideFromFlyout")
            //    {
            //        Name = "Hide From Flyout",
            //        Description = "Hide from flyout navigation",
            //        SortOrder = 1
            //    },
            //    new PropertyType(_shortStringHelper, booleanDataType, "active")
            //    {
            //        Name = "Active",
            //        Description = "Is webinar active",
            //        SortOrder = 2
            //    },
            //    new PropertyType(_shortStringHelper, textboxDataType, "product")
            //    {
            //        Name = "Product",
            //        Description = "Associated product ID",
            //        SortOrder = 3
            //    },
            //    new PropertyType(_shortStringHelper, textareaDataType, "products")
            //    {
            //        Name = "Products",
            //        Description = "Associated products (pipe separated)",
            //        SortOrder = 4
            //    },
            //    new PropertyType(_shortStringHelper, textareaDataType, "tags")
            //    {
            //        Name = "Tags",
            //        Description = "Content tags (pipe separated)",
            //        SortOrder = 5
            //    }
            //};

            //        // Add properties to their respective groups
            //        foreach (var prop in properties)
            //        {
            //            generalGroup.PropertyTypes.Add(prop);
            //        }

            //        foreach (var prop in seoProperties)
            //        {
            //            seoGroup.PropertyTypes.Add(prop);
            //        }

            //        foreach (var prop in settingsProperties)
            //        {
            //            settingsGroup.PropertyTypes.Add(prop);
            //        }

            //        // Add groups to content type
            //        webinarDocType.PropertyGroups.Add(generalGroup);
            //        webinarDocType.PropertyGroups.Add(seoGroup);
            //        webinarDocType.PropertyGroups.Add(settingsGroup);

            //        // Save the content type
            //        _contentTypeService.Save(webinarDocType);
                  var result = true;
            //        //_logger.LogInformation($"Veterinary Webinar document type created successfully. Result: {result.Success}");

            //        //if (!result.Success)
            //        //{
            //        //    foreach (var message in result.EventMessages)
            //        //    {
            //        //        _logger.LogError($"Content type creation error: {message.Message}");
            //        //    }
            //        //}

                    return result;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Error creating Veterinary Webinar document type");
            //        return false;
            //    }
        }

        //private IDataType GetDataTypeByAlias(string alias)
        //{
        //    try
        //    {
        //        return _dataTypeService.GetByEditorAlias(alias)?.FirstOrDefault();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        public async Task<bool> ImportWebinars(List<WebinarData> webinars, int parentNodeId = -1)
        {
            //    try
            //    {
            //        var contentType = _contentTypeService.Get("veterinaryWebinar");
            //        if (contentType == null)
            //        {
            //            _logger.LogError("Veterinary Webinar document type not found");
            //            return false;
            //        }

            //        foreach (var webinar in webinars)
            //        {
            //            try
            //            {
            //                // Create content node
            //                var content = _contentService.Create(
            //                    webinar.DisplayName ?? webinar.Title ?? "Untitled Webinar",
            //                    FindVeterinaryWebinarsNode(),
            //                    "veterinaryWebinar"
            //                );

            //                // Set properties
            //                SetPropertyValue(content, "title", webinar.Title);
            //                SetPropertyValue(content, "description", webinar.Description);
            //                SetPropertyValue(content, "pageTitle", webinar.PageTitle);
            //                SetPropertyValue(content, "metaDescription", webinar.MetaDescription);
            //                SetPropertyValue(content, "facebookTitle", webinar.FacebookTitle);
            //                SetPropertyValue(content, "facebookDescription", webinar.FacebookDescription);
            //                SetPropertyValue(content, "videoEmbedCode", webinar.VideoEmbedCode);
            //                SetPropertyValue(content, "length", webinar.Length);
            //                SetPropertyValue(content, "product", webinar.Product);
            //                SetPropertyValue(content, "products", webinar.Products);
            //                SetPropertyValue(content, "tags", webinar.Tags);

            //                // Handle date
            //                if (DateTime.TryParseExact(webinar.Date, "yyyyMMddTHHmmss",
            //                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            //                {
            //                    content.SetValue("date", parsedDate);
            //                }

            //                // Handle boolean values
            //                content.SetValue("hideFromFlyout", webinar.HideFromFlyout == "1");
            //                content.SetValue("active", webinar.Active == "1");

            //                // Save and publish
            //                var saveResult = _contentService.Save(content);

            //                var publishResult = _contentService.Publish(content);


            //            }
            //            catch (Exception ex)
            //            {
            //                _logger.LogError(ex, $"Error importing webinar: {webinar.Title}");
            //            }
            //        }

                    return true;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Error importing webinars");
            //        return false;
            //    }
            }

            //private void SetPropertyValue(IContent content, string alias, string value)
            //{
            //    if (!string.IsNullOrWhiteSpace(value))
            //    {
            //        content.SetValue(alias, value);
            //    }
            //}

            //private int FindVeterinaryWebinarsNode()
            //{
            //    try
            //    {
            //        // Get the Home node (root level)
            //        var homeNode = _contentService.GetRootContent().FirstOrDefault();
            //        if (homeNode == null)
            //        {
            //            _logger.LogError("Could not find Home node");
            //            return -1;
            //        }

            //        _logger.LogInformation($"Found Home node: {homeNode.Name} (ID: {homeNode.Id})");

            //        // Get children of Home to find "Webinars"
            //        var homeChildren = _contentService.GetPagedChildren(homeNode.Id, 0, 100, out long totalChildren);
            //        var webinarsNode = homeChildren.FirstOrDefault(c =>
            //            c.Name.Equals("Webinars", StringComparison.OrdinalIgnoreCase));

            //        if (webinarsNode == null)
            //        {
            //            _logger.LogWarning("'Webinars' node not found under Home - creating it");
            //            // Create Webinars folder if it doesn't exist
            //            webinarsNode = _contentService.Create("Webinars", homeNode.Id, "Folder"); // Assuming you have a Folder content type
            //            var saveResult = _contentService.SaveAndPublish(webinarsNode);
            //            if (!saveResult.Success)
            //            {
            //                _logger.LogError("Failed to create Webinars folder");
            //                return -1;
            //            }
            //        }

            //        _logger.LogInformation($"Found/Created Webinars node: {webinarsNode.Name} (ID: {webinarsNode.Id})");

            //        // Get children of Webinars to find "Veterinary Webinars"
            //        var webinarsChildren = _contentService.GetPagedChildren(webinarsNode.Id, 0, 100, out long totalWebinarsChildren);
            //        var veterinaryWebinarsNode = webinarsChildren.FirstOrDefault(c =>
            //            c.Name.Equals("Veterinary Webinars", StringComparison.OrdinalIgnoreCase));

            //        if (veterinaryWebinarsNode == null)
            //        {
            //            _logger.LogWarning("'Veterinary Webinars' node not found under Webinars - creating it");
            //            // Create Veterinary Webinars folder if it doesn't exist
            //            veterinaryWebinarsNode = _contentService.Create("Veterinary Webinars", webinarsNode.Id, "Folder");
            //            var saveResult = _contentService.SaveAndPublish(veterinaryWebinarsNode);
            //            if (!saveResult.Success)
            //            {
            //                _logger.LogError("Failed to create Veterinary Webinars folder");
            //                return -1;
            //            }
            //        }

            //        _logger.LogInformation($"Found/Created Veterinary Webinars node: {veterinaryWebinarsNode.Name} (ID: {veterinaryWebinarsNode.Id})");
            //        return veterinaryWebinarsNode.Id;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Error finding/creating Veterinary Webinars node");
            //        return -1;
            //    }
            //}
        }

    public class WebinarData
    {
        public string ItemName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string Length { get; set; }
        public string VideoEmbedCode { get; set; }
        public string PageTitle { get; set; }
        public string MetaDescription { get; set; }
        public string FacebookTitle { get; set; }
        public string FacebookDescription { get; set; }
        public string HideFromFlyout { get; set; }
        public string Active { get; set; }
        public string Product { get; set; }
        public string Products { get; set; }
        public string Tags { get; set; }
        public string DisplayName { get; set; }
    }


    public class CsvImportHelper
    {
        public static List<WebinarData> ParseCsvData(string csvContent)
        {
            var webinars = new Dictionary<string, WebinarData>();
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = ParseCsvLine(lines[i]);
                if (parts.Length >= 3)
                {
                    var itemName = parts[0].Trim();
                    var fieldName = parts[1].Trim();
                    var value = parts[2].Trim().Trim('"');

                    if (!webinars.ContainsKey(itemName))
                    {
                        webinars[itemName] = new WebinarData { ItemName = itemName };
                    }

                    var webinar = webinars[itemName];

                    // Map fields to properties
                    switch (fieldName.ToLower())
                    {
                        case "title":
                            webinar.Title = value;
                            break;
                        case "description":
                            webinar.Description = value;
                            break;
                        case "date":
                            webinar.Date = value;
                            break;
                        case "length":
                            webinar.Length = value;
                            break;
                        case "video embed code":
                            webinar.VideoEmbedCode = value;
                            break;
                        case "page title":
                            webinar.PageTitle = value;
                            break;
                        case "meta description":
                            webinar.MetaDescription = value;
                            break;
                        case "facebook title":
                            webinar.FacebookTitle = value;
                            break;
                        case "facebook description":
                            webinar.FacebookDescription = value;
                            break;
                        case "hide from flyout":
                            webinar.HideFromFlyout = value;
                            break;
                        case "active":
                            webinar.Active = value;
                            break;
                        case "product":
                            webinar.Product = value;
                            break;
                        case "products":
                            webinar.Products = value;
                            break;
                        case "tags":
                            webinar.Tags = value;
                            break;
                        case "__display name":
                            webinar.DisplayName = value;
                            break;
                    }
                }
            }

            return webinars.Values.ToList();
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}