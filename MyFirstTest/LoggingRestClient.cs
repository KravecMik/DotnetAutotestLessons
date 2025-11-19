using MongoDB.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstTest
{
    public class LoggingRestClient
    {
        private readonly RestClient _client;

        public LoggingRestClient(RestClient client)
        {
            _client = client;
        }

        public async Task<RestResponse> ExecuteAsync(RestRequest request)
        {
            LogRequest(request);
            var response = await _client.ExecuteAsync(request);
            LogResponse(response);
            return response;
        }

        private void LogRequest(RestRequest request)
        {
            var uri = _client.BuildUri(request);

            Console.WriteLine("=== HTTP REQUEST ===");
            Console.WriteLine($"URL: {request.Method} {uri}");

            // Заголовки
            if (request.Parameters.Any(p => p.Type == ParameterType.HttpHeader))
            {
                Console.WriteLine("Headers:");
                foreach (var header in request.Parameters.Where(p => p.Type == ParameterType.HttpHeader))
                {
                    Console.WriteLine($"{header.Name}: {header.Value}");
                }
            }

            // Тело запроса
            var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
            if (bodyParam != null)
            {
                Console.WriteLine("Body:");
                // Убираем лишние пробелы и выравниваем по левому краю
                var cleanBody = RemoveExtraIndentation(bodyParam.Value?.ToString());
                Console.WriteLine(cleanBody);
            }

            // Генерируем curl команду
            Console.WriteLine("CURL:");
            Console.WriteLine(GenerateCurlCommand(request, uri));
            Console.WriteLine("====================");
            Console.WriteLine(); // пустая строка для разделения
        }

        private void LogResponse(RestResponse response)
        {
            Console.WriteLine("=== HTTP RESPONSE ===");
            Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"Time: {response.ResponseUri} ms");

            if (!string.IsNullOrEmpty(response.Content))
            {
                Console.WriteLine("Body:");
                var cleanBody = RemoveExtraIndentation(response.Content);
                Console.WriteLine(cleanBody);
            }

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                Console.WriteLine($"Error: {response.ErrorMessage}");
            }
            Console.WriteLine("=====================");
            Console.WriteLine(); // пустая строка для разделения
        }

        private string GenerateCurlCommand(RestRequest request, Uri uri)
        {
            var curl = new List<string> { "curl" };

            // Метод
            if (request.Method != Method.Get)
            {
                curl.Add($"-X {request.Method}");
            }

            // Заголовки
            foreach (var header in request.Parameters.Where(p => p.Type == ParameterType.HttpHeader))
            {
                curl.Add($"-H \"{header.Name}: {header.Value}\"");
            }

            // Тело
            var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
            if (bodyParam != null)
            {
                var cleanBody = RemoveExtraIndentation(bodyParam.Value?.ToString());
                var escapedBody = cleanBody?.Replace("\"", "\\\"");
                curl.Add($"-d \"{escapedBody}\"");
            }

            // URL
            curl.Add($"\"{uri}\"");

            return string.Join(" ", curl);
        }

        private string RemoveExtraIndentation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Разделяем на строки
            var lines = text.Split('\n');

            // Находим минимальное количество пробелов в начале строк
            var minIndent = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.TakeWhile(char.IsWhiteSpace).Count())
                .DefaultIfEmpty(0)
                .Min();

            // Убираем лишние пробелы
            var cleanedLines = lines.Select(line =>
            {
                if (line.Length >= minIndent)
                    return line.Substring(minIndent);
                return line;
            });

            return string.Join("\n", cleanedLines);
        }
    }
}
