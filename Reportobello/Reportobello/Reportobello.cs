using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web;

namespace Reportobello
{
    public class ReportobelloException : Exception
    {
        public ReportobelloException(string message) : base(message) { }
    }

    // TODO: only allow to be specified on classes/records (and maybe interfaces?)
    /// <summary>
    /// Set the report name for a given template.
    /// </summary>
    public class TemplateNameAttribute : Attribute
    {
        public string Name { get; set; }

        public TemplateNameAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Set the template content for a given template.
    /// </summary>
    public class TemplateContentAttribute : Attribute
    {
        public string Content { get; set; }

        public TemplateContentAttribute(string content)
        {
            Content = content;
        }
    }

    /// <summary>
    /// Set the template filename for a given template.
    /// </summary>
    public class TemplateFileAttribute : Attribute
    {
        public string FilePath { get; set; }

        public TemplateFileAttribute(string filePath)
        {
            FilePath = filePath;
        }
    }

    // TODO: add env var API support (done)
    // TODO: add template upload API support (done)
    // TODO: add get recent builds support
    // TODO: add get template versions API support
    // TODO: throw custom exception when type doesnt have expected attribute

    // TODO: add custom exceptions for API failures (ie, template not found)
    public class ReportobelloApi
    {
        public const string DefaultHost = "https://reportobello.com";

        private readonly HttpClient _httpClient = new HttpClient();

        public ReportobelloApi(string apiKey, string host = null)
        {
            if (host == null)
            {
                host = DefaultHost;
            }

            _httpClient.BaseAddress = new Uri(host);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task UploadTemplate<T>()
        {
            var nameAttr = typeof(T).GetCustomAttribute<TemplateNameAttribute>();

            var contentAttrs = typeof(T).GetCustomAttributes<TemplateContentAttribute>();

            if (contentAttrs.Any())
            {
                await UploadTemplate(nameAttr.Name, contentAttrs.First().Content);
                return;
            }

            // TODO: check if this is absolute first
            var fileAttr = typeof(T).GetCustomAttribute<TemplateFileAttribute>();

            var fullFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileAttr.FilePath);

            var content = File.ReadAllText(fullFilePath);

            await UploadTemplate(nameAttr.Name, content);
        }

        public async Task UploadTemplate<T>(string templateData)
        {
            var attr = typeof(T).GetCustomAttribute<TemplateNameAttribute>();

            await UploadTemplate(attr.Name, templateData);
        }

        // TODO: add sync version
        public async Task UploadTemplate(string templateName, string templateData)
        {
            var url = $"/api/v1/template/{Uri.EscapeDataString(templateName)}";

            var body = new StringContent(templateData, Encoding.UTF8, "application/x-typst");
            var resp = await _httpClient.PostAsync(url, body);

            if (!resp.IsSuccessStatusCode)
            {
                var output = await resp.Content.ReadAsStringAsync();

                throw new ReportobelloException(output);
            }
        }

        public class Template
        {
            public string Name { get; set; }
            public string TemplateContent { get; set; }
            public int Version { get; set; }
        }

        public async Task<IEnumerable<Template>> GetTemplateVersions<T>()
        {
            var nameAttr = typeof(T).GetCustomAttribute<TemplateNameAttribute>();

            return await GetTemplateVersions(nameAttr.Name);
        }

        public async Task<IEnumerable<Template>> GetTemplateVersions(string templateName)
        {
            var url = $"/api/v1/template/{Uri.EscapeDataString(templateName)}";

            var resp = await _httpClient.GetAsync(url);

            var output = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                throw new ReportobelloException(output);
            }

            return JsonSerializer.Deserialize<IEnumerable<Template>>(output);
        }

        public Task<IEnumerable<Template>> GetTemplateVersionsSync<T>()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Template>> GetTemplateVersionsSync(string templateName)
        {
            throw new NotImplementedException();
        }

        public async Task SetEnvironmentVariables(Dictionary<string, string> envVars)
        {
            var url = "/api/v1/env";

            var resp = await _httpClient.PostAsync(url, JsonContent(JsonSerializer.Serialize(envVars)));

            if (!resp.IsSuccessStatusCode)
            {
                var output = await resp.Content.ReadAsStringAsync();

                throw new ReportobelloException(output);
            }
        }

        public Task SetEnvironmentVariablesSync(Dictionary<string, string> envVars)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteEnvironmentVariables(IEnumerable<string> keys)
        {
            var joinedKeys = string.Join(",", keys.Select(k => Uri.EscapeDataString(k)));

            var url = $"/api/v1/env?keys={joinedKeys}";

            var resp = await _httpClient.DeleteAsync(url);

            if (!resp.IsSuccessStatusCode)
            {
                var output = await resp.Content.ReadAsStringAsync();

                throw new ReportobelloException(output);
            }
        }

        public Task DeleteEnvironmentVariablesSync(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public async Task<Uri> RunReport<T>(T data, bool preview = false)
        {
            var attr = data.GetType().GetCustomAttribute<TemplateNameAttribute>();

            return await RunReport(attr.Name, data, preview);
        }

        private class BuildTemplatePayload
        {
            public object data { get; set; }
            public string content_type { get; set; } = "application/json";
        }

        // TODO: we probably want to move the preview flag to a custom object
        public async Task<Uri> RunReport(string templateName, object data, bool preview = false)
        {
            var queryParams = new Dictionary<string, string> { { "justUrl", "" } };

            if (preview)
            {
                queryParams["preview"] = "";
            }

            var url = $"/api/v1/template/{Uri.EscapeDataString(templateName)}/build?{QueryStringBuilder(queryParams)}";

            var payload = new BuildTemplatePayload() { data = data };

            var resp = await _httpClient.PostAsync(url, JsonContent(JsonSerializer.Serialize(payload)));

            var output = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                throw new ReportobelloException(output);
            }

            return new Uri(output);
        }

        public Uri RunReportSync<T>(T data, bool preview = false)
        {
            return Task.Run(() => RunReport(data, preview)).Result;
        }

        public Uri RunReportSync(string templateName, object data, bool preview = false)
        {
            return Task.Run(() => RunReport(templateName, data, preview)).Result;
        }

        private StringContent JsonContent(string data)
        {
            return new StringContent(data, Encoding.UTF8, "application/json");
        }

        private static string QueryStringBuilder(Dictionary<string, string> q)
        {
            var query = HttpUtility.ParseQueryString("");

            foreach (var kv in q)
            {
                query.Add(kv.Key, kv.Value);
            }

            return query.ToString();
        }
    }
}
