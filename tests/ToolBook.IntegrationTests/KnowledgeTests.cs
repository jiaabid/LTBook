using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ToolBook.IntegrationTests.Infrastructure;

namespace ToolBook.IntegrationTests;

[Collection(ApiCollection.Name)]
public class KnowledgeTests(ToolBookApiFactory factory)
{
    private const string Manual = """
        # Laser Cutter Guide

        ## Changing the lens
        To change the focus lens, switch off the laser, unscrew the lens holder anticlockwise and insert the new lens with the curved side up.

        ## Cleaning
        Clean the honeycomb bed weekly with isopropyl alcohol.
        """;

    [Fact]
    public async Task Uploaded_manual_is_ingested_in_background_and_answers_are_grounded_with_citations()
    {
        var admin = await factory.AdminAsync();
        var tool = await admin.CreateToolAsync();

        var upload = await UploadAsync(admin, tool.Id, "laser-guide.md", Manual);
        Assert.Equal(HttpStatusCode.Accepted, upload.StatusCode);
        var documentId = (await upload.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();

        var document = await WaitUntilProcessedAsync(admin, tool.Id, documentId);
        Assert.Equal("Ready", document.GetProperty("status").GetString());
        Assert.True(document.GetProperty("chunkCount").GetInt32() >= 2);

        var list = await (await admin.GetAsync($"/api/tools/{tool.Id}/documents")).ReadAsync<List<JsonElement>>();
        Assert.Equal(documentId, Assert.Single(list).GetProperty("id").GetGuid());

        var user = await factory.NewUserAsync();
        var answer = await (await user.PostAsJsonAsync($"/api/tools/{tool.Id}/ask", new { question = "How do I change the focus lens?" }))
            .ReadAsync<JsonElement>();

        Assert.True(answer.GetProperty("grounded").GetBoolean());
        Assert.Contains("unscrew the lens holder", answer.GetProperty("answer").GetString());
        var citation = answer.GetProperty("citations").EnumerateArray().First();
        Assert.Equal("laser-guide.md", citation.GetProperty("fileName").GetString());
        Assert.Equal("Laser Cutter Guide > Changing the lens", citation.GetProperty("section").GetString());
    }

    [Fact]
    public async Task Questions_are_answered_only_from_the_asked_tools_manuals()
    {
        var admin = await factory.AdminAsync();
        var toolWithManual = await admin.CreateToolAsync();
        var toolWithoutManual = await admin.CreateToolAsync();
        var upload = await UploadAsync(admin, toolWithManual.Id, "laser-guide.md", Manual);
        await WaitUntilProcessedAsync(admin, toolWithManual.Id, (await upload.ReadAsync<JsonElement>()).GetProperty("id").GetGuid());
        var user = await factory.NewUserAsync();

        var answer = await (await user.PostAsJsonAsync($"/api/tools/{toolWithoutManual.Id}/ask", new { question = "How do I change the focus lens?" }))
            .ReadAsync<JsonElement>();

        Assert.False(answer.GetProperty("grounded").GetBoolean());
        Assert.Equal("I couldn't find this in the manual for this tool.", answer.GetProperty("answer").GetString());
        Assert.Empty(answer.GetProperty("citations").EnumerateArray());
    }

    [Fact]
    public async Task Unsupported_file_types_are_rejected()
    {
        var admin = await factory.AdminAsync();
        var tool = await admin.CreateToolAsync();

        var response = await UploadAsync(admin, tool.Id, "manual.docx", "binary");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Knowledge.UnsupportedFileType", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Only_admins_can_upload_manuals()
    {
        var admin = await factory.AdminAsync();
        var tool = await admin.CreateToolAsync();
        var user = await factory.NewUserAsync();

        var response = await UploadAsync(user, tool.Id, "guide.md", Manual);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid toolId, string fileName, string content)
    {
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", fileName },
        };
        return client.PostAsync($"/api/tools/{toolId}/documents", form);
    }

    private static async Task<JsonElement> WaitUntilProcessedAsync(HttpClient client, Guid toolId, Guid documentId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var response = await client.GetAsync($"/api/tools/{toolId}/documents/{documentId}");
            Assert.True(response.IsSuccessStatusCode, $"GET document returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            var document = await response.ReadAsync<JsonElement>();
            if (document.GetProperty("status").GetString() is "Ready" or "Failed")
            {
                return document;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Document {documentId} was not processed in time.");
    }
}
