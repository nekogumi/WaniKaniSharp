﻿namespace Nekogumi.WaniKani.Services;

public class LowerSnakeCaseNamingPolicy : JsonNamingPolicy
{
    private static readonly Regex SnakeCaseConverter = new(@"([A-Z])", RegexOptions.Compiled);

    public override string ConvertName(string name)
        => SnakeCaseConverter.Replace(name, "_$1")[1..].ToLower();
}

public partial class WaniKaniConnection : IDisposable
{
    private const string APIURL = "https://api.wanikani.com/v2/";
    private readonly HttpConnection connection;
    private bool disposed;

    public WaniKaniConnection(string apiKey, IETagCache? cache)
    {
        connection = new HttpConnection(60, cache)
        {
            BaseAddress = new Uri(APIURL)
        };
        connection.Client.DefaultRequestHeaders.Add("Wanikani-Revision", "20170710");
        connection.Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing) connection.Dispose();
            disposed = true;
        }
    }

    private Task<TJson> GetJsonAsync<TJson>(
        string endpoint,
        CacheStrategy cacheStrategy,
        CancellationToken cancellationToken,
        params (string name, object? value)[] parameters)
        => connection.GetJsonAsync<TJson>(endpoint, cacheStrategy, JsonOptions, cancellationToken, parameters);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = new LowerSnakeCaseNamingPolicy(),
        WriteIndented = true,
        Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
    };

    private async Task<ResponseCollection<TData>> QueryCollectionAsync<TData>(string endpoint
        , CacheStrategy cacheStrategy = CacheStrategy.Cache
        , CancellationToken cancellationToken = default
        , params (string name, object? value)[] parameters)
    {
        cancellationToken.ThrowIfCancellationRequested();
        {
            var response = await GetJsonAsync<ResponseCollection<TData>>(endpoint, cacheStrategy, cancellationToken, parameters).ConfigureAwait(false);
            if (response.Pages.NextUrl is null) return response;
            else
            {
                var obj = response.Object;
                var url = response.Url;
                var updatedAt = response.DataUpdatedAt;
                var totalCount = response.TotalCount;

                var items = new List<Resource<TData>>(response.Data);
                var localParameters = new (string name, object? value)[(parameters?.Length ?? 0) + 1];
                if (parameters != null) Array.Copy(parameters, localParameters, parameters.Length);

                do
                {
                    Debug.WriteLine(response.Pages.NextUrl);
                    localParameters[^1] = ("page_after_id", response.Data[^1].Id);
                    response = await GetJsonAsync<ResponseCollection<TData>>(endpoint, cacheStrategy, cancellationToken, localParameters).ConfigureAwait(false);
                    if (response is null) throw new NotImplementedException("Null response in collection walk");
                    items.AddRange(response.Data);
                } while (response.Pages.NextUrl is not null);

                return new ResponseCollection<TData>(
                    obj, url, updatedAt,
                    new Pages(null, null, totalCount),
                    totalCount,
                    items.ToArray());
            }
        }
    }

    /// <summary>
    /// Creates a study material for a specific `subject_id`.
    /// </summary>
    /// <param name="subjectId">Unique identifier of the subject.</param>
    /// <param name="meaningNote">Meaning notes specific for the subject.</param>
    /// <param name="readingNote">Reading notes specific for the subject.</param>
    /// <param name="meaningSynonyms">Meaning synonyms for the subject.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token</param>
    /// <returns></returns>
    public Task CreateStudyMaterialAsync(
            long subjectId,
            string? meaningNote = null,
            string? readingNote = null,
            IEnumerable<string>? meaningSynonyms = null,
            CancellationToken cancellationToken = default)
        => connection.PostAsync(
            "study_materials",
             new
             {
                 study_material = new
                 {
                     subject_id = subjectId,
                     meaning_note = meaningNote,
                     reading_note = readingNote,
                     meaning_synonyms = meaningSynonyms?.ToArray()
                 }
             },
             new JsonSerializerOptions
             {
                 DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
             },
             cancellationToken);

    /// <summary>
    /// Updates a study material for a specific id.
    /// </summary>
    /// <param name="id">Unique identifier of the study material.</param>
    /// <param name="meaningNote">Meaning notes specific for the subject.</param>
    /// <param name="readingNote">Reading notes specific for the subject.</param>
    /// <param name="meaningSynonyms">Meaning synonyms for the subject.</param>
    /// <param name="cancellationToken">Asynchronous operation cancellation token</param>
    /// <returns></returns>
    public Task UpdateStudyMaterialAsync(
            long id,
            string? meaningNote = null,
            string? readingNote = null,
            IEnumerable<string>? meaningSynonyms = null,
            CancellationToken cancellationToken = default)
        => connection.PutJSonAsync(
            $"study_materials/{id}",
            new
            {
                study_material = new
                {
                    meaning_note = meaningNote,
                    reading_note = readingNote,
                    meaning_synonyms = meaningSynonyms?.ToArray()
                }
            },
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            },
            cancellationToken);
}
